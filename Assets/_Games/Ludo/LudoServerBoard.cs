using System.Collections.Generic;

/// <summary>
/// The match server's own Ludo board (2 players) — the rules exactly as the client plays them today
/// (LudoPawnController.CheckIfCanMove / MoveFinished, GameDiceController.RollDice), so the server can roll the dice,
/// check every move and decide the result instead of trusting what a phone says.
///
/// Positions are each player's own path index: -1 = base, 0 = own start square, 0–50 = the shared track, 51–55 = the
/// home column, 56 = home. Two pawns of different players are on the same track square when both are 0–50 and
/// (a − b) mod 52 == 26 (the opponent sits opposite). Player index = index into the clients' playerObjects, which every
/// client sorts by user id (string.Compare), creator/joiner mapped the same way.
/// Pure C#: no Unity objects, so it runs the same on the headless server.
/// </summary>
public class LudoServerBoard
{
    public const int Home = 56;
    const int LastTrack = 50;
    const int TrackLength = 52;
    const int Opposite = 26;
    // Star / start squares, the same relative squares for both colours (and 26 apart, so they line up).
    static readonly HashSet<int> Safe = new HashSet<int> { 0, 8, 13, 21, 26, 34, 39, 47 };

    public readonly int[][] Pos = { new[] { -1, -1, -1, -1 }, new[] { -1, -1, -1, -1 } };
    public readonly bool[] CanEnterHome = new bool[2];

    /// <summary>Whose turn (0/1).</summary>
    public int Turn;
    /// <summary>The roll waiting for a move this turn; 0 = waiting for a roll.</summary>
    public int PendingSteps;
    /// <summary>6s rolled in a row this turn (the third loses the turn).</summary>
    public int SixesThisTurn;

    // Each player's dice, like each player's own GameDiceController (pity 6 + no two 6s in a row).
    readonly int[] diceIndex = new int[2];
    readonly int[] nextSix = new int[2];
    readonly int[] sixStreak = new int[2];
    readonly System.Random rng;

    public LudoServerBoard(int firstTurn, int seed)
    {
        Turn = firstTurn;
        rng = new System.Random(seed);
        nextSix[0] = rng.Next(3, 8);
        nextSix[1] = rng.Next(3, 8);
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

    public struct MoveResult
    {
        public int From, To;
        /// <summary>Opponent pawn sent back to base, or -1.</summary>
        public int Captured;
        public bool ReachedHome, ExtraRoll, Won;
    }

    /// <summary>Apply a move already checked with <see cref="CanMove"/>.</summary>
    public MoveResult Apply(int pl, int pawn, int steps)
    {
        var r = new MoveResult { From = Pos[pl][pawn], Captured = -1 };
        r.To = r.From < 0 ? 0 : r.From + steps;   // leaving base uses the whole 6 and lands on the start square
        Pos[pl][pawn] = r.To;

        // A single opponent pawn on a non-safe track square goes back to base; two together are safe.
        if (r.To <= LastTrack && !Safe.Contains(r.To))
        {
            int opp = 1 - pl, count = 0, which = -1;
            for (int q = 0; q < 4; q++)
            {
                int op = Pos[opp][q];
                if (op >= 0 && op <= LastTrack && Mod(r.To - op, TrackLength) == Opposite) { count++; which = q; }
            }
            if (count == 1)
            {
                Pos[opp][which] = -1;
                r.Captured = which;
                CanEnterHome[pl] = true;
            }
        }

        r.ReachedHome = r.To == Home;
        r.Won = true;
        for (int q = 0; q < 4; q++) if (Pos[pl][q] != Home) r.Won = false;
        r.ExtraRoll = !r.Won && (steps == 6 || r.Captured >= 0 || r.ReachedHome);
        return r;
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

    /// <summary>The reconnect snapshot clients already understand:
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

    static int Mod(int a, int m) => ((a % m) + m) % m;
}
