using System.Collections.Generic;

/// <summary>
/// The match server's own Ludo board (2–4 players) — the rules exactly as the client plays them today
/// (LudoPawnController.CheckIfCanMove / MoveFinished, GameDiceController.RollDice), so the server can roll the dice,
/// check every move and decide the result instead of trusting what a phone says.
///
/// Positions are each player's own path index: -1 = base, 0 = own start square, 0–50 = the shared track, 51–55 = the
/// home column, 56 = home. Every player sits on a board corner (<see cref="Corner"/>); corners are numbered in the
/// direction tokens move and each start square is 13 squares after the previous corner's. Two pawns of players A and
/// B are on the same track square when both are 0–50 and (a − b) mod 52 == 13 × (corner B − corner A) mod 52 — for the
/// 2-player board (corners 0 and 2) that is the old "opposite" offset of 26.
///
/// 2 players: player index = index into the clients' playerObjects, which every client sorts by user id
/// (string.Compare), creator/joiner mapped the same way. 4 players: player index = seat (0–3), seat = corner.
/// Pure C#: no Unity objects, so it runs the same on the headless server.
/// </summary>
public class LudoServerBoard
{
    public const int Home = 56;
    const int LastTrack = 50;
    const int TrackLength = 52;
    const int CornerSquares = 13;
    // Star / start squares, the same relative squares for every colour. The set repeats every 13 squares
    // (0/13/26/39 and 8/21/34/47), so it is the same set from every corner.
    static readonly HashSet<int> Safe = new HashSet<int> { 0, 8, 13, 21, 26, 34, 39, 47 };

    public enum SeatState { Playing, Finished, Left }

    /// <summary>Number of players on this board (2–4).</summary>
    public readonly int Seats;
    /// <summary>Board corner of each player: {0, 2} with 2 players (as today), {0, 1, 2} / {0, 1, 2, 3} otherwise.</summary>
    public readonly int[] Corner;
    public readonly int[][] Pos;
    public readonly bool[] CanEnterHome;
    public readonly SeatState[] State;
    /// <summary>Players in the order they got all 4 tokens home.</summary>
    public readonly List<int> FinishOrder = new List<int>();

    /// <summary>Whose turn.</summary>
    public int Turn;
    /// <summary>The roll waiting for a move this turn; 0 = waiting for a roll.</summary>
    public int PendingSteps;
    /// <summary>6s rolled in a row this turn (the third loses the turn).</summary>
    public int SixesThisTurn;

    // Each player's dice, like each player's own GameDiceController (pity 6 + no two 6s in a row).
    readonly int[] diceIndex;
    readonly int[] nextSix;
    readonly int[] sixStreak;
    readonly System.Random rng;

    public LudoServerBoard(int firstTurn, int seed) : this(firstTurn, seed, 2) { }

    public LudoServerBoard(int firstTurn, int seed, int seats)
    {
        if (seats < 2 || seats > 4) throw new System.ArgumentOutOfRangeException(nameof(seats), "2 to 4 players");
        Seats = seats;
        Corner = new int[seats];
        for (int s = 0; s < seats; s++) Corner[s] = seats == 2 ? s * 2 : s;
        Pos = new int[seats][];
        for (int s = 0; s < seats; s++) Pos[s] = new[] { -1, -1, -1, -1 };
        CanEnterHome = new bool[seats];
        State = new SeatState[seats];
        diceIndex = new int[seats];
        nextSix = new int[seats];
        sixStreak = new int[seats];

        Turn = firstTurn;
        rng = new System.Random(seed);
        // One draw per player in seat order: with 2 players the same draws as before, so a seed plays the same game.
        for (int s = 0; s < seats; s++) nextSix[s] = rng.Next(3, 8);
    }

    /// <summary>Roll for <paramref name="pl"/> with the client's rules: 1–6; a 6 is forced when none came for
    /// 3–7 rolls (4–7 after each 6); a second 6 in a row is re-rolled as 1–5.</summary>
    public int Roll(int pl)
    {
        int steps = rng.Next(1, 7);
        if (steps == 6 || diceIndex[pl] == nextSix[pl])
        {
            nextSix[pl] = rng.Next(4, 8);
            steps = 6;
            diceIndex[pl] = 0;
        }
        if (steps == 6)
        {
            sixStreak[pl]++;
            if (sixStreak[pl] == 2)
            {
                steps = rng.Next(1, 6);
                sixStreak[pl] = 0;
            }
        }
        else
        {
            sixStreak[pl] = 0;
        }
        diceIndex[pl]++;
        return steps;
    }

    public bool CanMove(int pl, int pawn, int steps)
    {
        if (pl < 0 || pl >= Seats || State[pl] != SeatState.Playing) return false;
        if (pawn < 0 || pawn > 3 || steps < 1 || steps > 6) return false;
        int from = Pos[pl][pawn];
        if (from < 0) return steps == 6;
        if (from >= Home) return false;
        int to = from + steps;
        if (to > Home) return false;
        if (to > LastTrack && !CanEnterHome[pl]) return false;
        return true;
    }

    public List<int> LegalPawns(int pl, int steps)
    {
        var list = new List<int>();
        for (int p = 0; p < 4; p++)
            if (CanMove(pl, p, steps)) list.Add(p);
        return list;
    }

    public struct Capture
    {
        public int Seat, Pawn;
    }

    public struct MoveResult
    {
        public int From, To;
        /// <summary>A captured pawn (the first one, see <see cref="Captures"/>), or -1.</summary>
        public int Captured;
        /// <summary>The player whose pawn <see cref="Captured"/> is, or -1.</summary>
        public int CapturedSeat;
        /// <summary>Every pawn sent back to base — one landing can capture a single pawn of several colours.</summary>
        public List<Capture> Captures;
        public bool ReachedHome, ExtraRoll, Won;
    }

    /// <summary>Same track square for pawn positions <paramref name="a"/> (player <paramref name="pa"/>) and
    /// <paramref name="b"/> (player <paramref name="pb"/>); false off the shared track.</summary>
    public bool SameSquare(int pa, int a, int pb, int b)
    {
        if (a < 0 || a > LastTrack || b < 0 || b > LastTrack) return false;
        return Mod(a - b, TrackLength) == Mod(CornerSquares * (Corner[pb] - Corner[pa]), TrackLength);
    }

    /// <summary>Apply a move already checked with <see cref="CanMove"/>.</summary>
    public MoveResult Apply(int pl, int pawn, int steps)
    {
        var r = new MoveResult { From = Pos[pl][pawn], Captured = -1, CapturedSeat = -1, Captures = new List<Capture>() };
        r.To = r.From < 0 ? 0 : r.From + steps;   // leaving base uses the whole 6 and lands on the start square
        Pos[pl][pawn] = r.To;

        // A single pawn of any other colour on a non-safe track square goes back to base; two of one colour are safe.
        if (r.To <= LastTrack && !Safe.Contains(r.To))
        {
            for (int opp = 0; opp < Seats; opp++)
            {
                if (opp == pl || State[opp] != SeatState.Playing) continue;
                int count = 0, which = -1;
                for (int q = 0; q < 4; q++)
                    if (SameSquare(pl, r.To, opp, Pos[opp][q])) { count++; which = q; }
                if (count != 1) continue;
                Pos[opp][which] = -1;
                r.Captures.Add(new Capture { Seat = opp, Pawn = which });
                if (r.Captured < 0) { r.Captured = which; r.CapturedSeat = opp; }
            }
            if (r.Captures.Count > 0) CanEnterHome[pl] = true;
        }

        r.ReachedHome = r.To == Home;
        r.Won = HomeCount(pl) == 4;
        if (r.Won && State[pl] == SeatState.Playing)
        {
            State[pl] = SeatState.Finished;
            FinishOrder.Add(pl);
        }
        r.ExtraRoll = !r.Won && (steps == 6 || r.Captures.Count > 0 || r.ReachedHome);
        return r;
    }

    /// <summary>A player left or was abandoned: their pawns go back to base (they no longer block or can be
    /// captured) and their turns are skipped.</summary>
    public void Leave(int pl)
    {
        if (State[pl] != SeatState.Playing) return;
        State[pl] = SeatState.Left;
        for (int q = 0; q < 4; q++) Pos[pl][q] = -1;
    }

    public int PlayingCount
    {
        get
        {
            int n = 0;
            for (int s = 0; s < Seats; s++) if (State[s] == SeatState.Playing) n++;
            return n;
        }
    }

    /// <summary>The next player after <see cref="Turn"/>, in move direction, who is still playing
    /// (2 players: the other one). -1 when nobody else is playing.</summary>
    public int NextTurn()
    {
        for (int i = 1; i <= Seats; i++)
        {
            int s = (Turn + i) % Seats;
            if (State[s] == SeatState.Playing) return s;
        }
        return -1;
    }

    public int HomeCount(int pl)
    {
        int n = 0;
        for (int q = 0; q < 4; q++) if (Pos[pl][q] == Home) n++;
        return n;
    }

    public int Progress(int pl)
    {
        int n = 0;
        for (int q = 0; q < 4; q++) if (Pos[pl][q] > 0) n += Pos[pl][q];
        return n;
    }

    /// <summary>Players best first: finished in finish order, then those still playing by tokens home, then progress
    /// (a full tie keeps seat order — the tie rule is a product decision), then those who left.</summary>
    public List<int> Ranking()
    {
        var playing = new List<int>();
        var left = new List<int>();
        for (int s = 0; s < Seats; s++)
        {
            if (State[s] == SeatState.Playing) playing.Add(s);
            else if (State[s] == SeatState.Left) left.Add(s);
        }
        playing.Sort((a, b) =>
        {
            int c = HomeCount(b).CompareTo(HomeCount(a));
            if (c == 0) c = Progress(b).CompareTo(Progress(a));
            return c != 0 ? c : a.CompareTo(b);
        });
        var order = new List<int>(FinishOrder);
        order.AddRange(playing);
        order.AddRange(left);
        return order;
    }

    /// <summary>The 2-player reconnect snapshot clients already understand:
    /// "c0;c1;c2;c3;j0;j1;j2;j3;currentPlayerIndex;creatorCanEnterHome;joinerCanEnterHome;".</summary>
    public string Snapshot(int creatorPl)
    {
        int joinerPl = 1 - creatorPl;
        var sb = new System.Text.StringBuilder();
        for (int q = 0; q < 4; q++) sb.Append(Pos[creatorPl][q]).Append(';');
        for (int q = 0; q < 4; q++) sb.Append(Pos[joinerPl][q]).Append(';');
        sb.Append(Turn).Append(';');
        sb.Append(CanEnterHome[creatorPl] ? 1 : 0).Append(';');
        sb.Append(CanEnterHome[joinerPl] ? 1 : 0).Append(';');
        return sb.ToString();
    }

    /// <summary>The N-player reconnect snapshot (ludo-4-player plan §3.3):
    /// "v2;seats;turn;pendingSteps;p00;p01;p02;p03;…;eh0..ehN;state0..stateN;" with positions in each seat's own path
    /// index and state 0 playing / 1 finished / 2 left.</summary>
    public string SnapshotV2()
    {
        var sb = new System.Text.StringBuilder("v2;");
        sb.Append(Seats).Append(';').Append(Turn).Append(';').Append(PendingSteps).Append(';');
        for (int s = 0; s < Seats; s++)
            for (int q = 0; q < 4; q++) sb.Append(Pos[s][q]).Append(';');
        for (int s = 0; s < Seats; s++) sb.Append(CanEnterHome[s] ? 1 : 0).Append(';');
        for (int s = 0; s < Seats; s++) sb.Append((int)State[s]).Append(';');
        return sb.ToString();
    }

    static int Mod(int a, int m) => ((a % m) + m) % m;
}
