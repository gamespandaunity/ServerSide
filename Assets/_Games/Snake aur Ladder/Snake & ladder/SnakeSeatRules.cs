namespace Snake_Ladder
{
    /// <summary>
    /// Seat rules of a Snake &amp; Ladder table with 2–4 seats (docs/multiplayer-plans/snake-ladder-4-player.md, product
    /// selections of 14 Oct 2026): turns go round in seat order skipping seats that are out, 3 missed turns in a row put
    /// a seat out, at time-out the token furthest ahead wins (a tie goes to whoever reached that square first, then to
    /// the lower seat), and the last seat still playing wins. The board itself (nodes, snakes, ladders) stays in
    /// GameControllerNew; this only tracks where each seat stands. Pure C#, so it runs the same on the headless server.
    /// </summary>
    public class SnakeSeatRules
    {
        public enum State { Playing, Left, Abandoned, Eliminated }

        /// <summary>Missed turns in a row that put a seat out.</summary>
        public const int MissLimit = 3;

        public readonly int Seats;
        public readonly State[] States;
        /// <summary>Square of each seat's token: 0 = start, 100 = home.</summary>
        public readonly int[] Square;
        public readonly int[] Missed;
        readonly long[] arrivedAt;
        long clock;

        /// <summary>Whose turn.</summary>
        public int Turn;

        public SnakeSeatRules(int seats, int firstTurn)
        {
            if (seats < 2 || seats > 4) throw new System.ArgumentOutOfRangeException(nameof(seats), "2 to 4 players");
            Seats = seats;
            States = new State[seats];
            Square = new int[seats];
            Missed = new int[seats];
            arrivedAt = new long[seats];
            Turn = firstTurn;
        }

        public bool IsPlaying(int seat) => seat >= 0 && seat < Seats && States[seat] == State.Playing;

        public int PlayingCount
        {
            get
            {
                int n = 0;
                for (int s = 0; s < Seats; s++) if (States[s] == State.Playing) n++;
                return n;
            }
        }

        /// <summary>The next seat after <see cref="Turn"/> still playing (2 seats: the other one); -1 when none.</summary>
        public int NextTurn()
        {
            for (int i = 1; i <= Seats; i++)
            {
                int s = (Turn + i) % Seats;
                if (States[s] == State.Playing) return s;
            }
            return -1;
        }

        /// <summary>The seat rolled: its missed-turn count starts again.</summary>
        public void Rolled(int seat) => Missed[seat] = 0;

        /// <summary>The seat's turn timer ran out. Returns true when that was its <see cref="MissLimit"/>th miss in a
        /// row and the seat is now out.</summary>
        public bool Miss(int seat)
        {
            if (!IsPlaying(seat)) return false;
            Missed[seat]++;
            if (Missed[seat] < MissLimit) return false;
            States[seat] = State.Eliminated;
            return true;
        }

        /// <summary>The seat's token now stands on <paramref name="square"/> (after the walk and any snake or ladder).</summary>
        public void Moved(int seat, int square)
        {
            if (Square[seat] == square) return;
            Square[seat] = square;
            arrivedAt[seat] = ++clock;
        }

        /// <summary>The seat is out (left, abandoned, eliminated); its turns are skipped from now on.</summary>
        public bool Out(int seat, State state)
        {
            if (!IsPlaying(seat) || state == State.Playing) return false;
            States[seat] = state;
            return true;
        }

        /// <summary>The only seat still playing, or -1.</summary>
        public int LastPlaying()
        {
            int last = -1;
            for (int s = 0; s < Seats; s++)
            {
                if (States[s] != State.Playing) continue;
                if (last >= 0) return -1;
                last = s;
            }
            return last;
        }

        /// <summary>Time-out winner among the seats still playing: furthest square, then whoever got there first, then
        /// the lower seat. -1 when nobody is playing.</summary>
        public int Leader()
        {
            int best = -1;
            for (int s = 0; s < Seats; s++)
            {
                if (States[s] != State.Playing) continue;
                if (best < 0 || Square[s] > Square[best] ||
                    (Square[s] == Square[best] && arrivedAt[s] < arrivedAt[best]))
                    best = s;
            }
            return best;
        }
    }
}
