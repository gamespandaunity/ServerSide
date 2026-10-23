using System;
using System.Collections.Generic;
using System.Linq;

namespace GamesPanda.Tables
{
    /// <summary>
    /// One hand of Teen Patti at a table of 2-5, run only on the match server. Phones send intents (see, chaal, raise,
    /// pack, show, side-show, answer); this decides what is allowed, moves the chips (HandLedger) and who wins.
    ///
    /// Rules (product defaults, docs/multiplayer-plans/teen-patti-max-players.md):
    /// - Every player puts the boot in; the stake starts at the boot.
    /// - Blind pays the stake, seen pays twice the stake. A raise doubles the stake first. The stake never goes above
    ///   <see cref="Config.StakeCap"/> (so a seen chaal is at most twice that).
    /// - After <see cref="Config.MaxBlinds"/> blind chaals a player is seen.
    /// - Show: only with exactly 2 players left; the asker pays one chaal, then both hands are compared.
    /// - Side-show: only with 3 or more left, the asker is seen and asks the previous player still in, who is seen too;
    ///   the asker pays one chaal, the other answers yes or no. On yes the lower hand packs; equal hands: the asker packs.
    /// - Pot limit: once the pot reaches <see cref="Config.PotLimit"/>, every hand still in is shown.
    /// - A player who cannot cover a chaal puts in the rest of the stack (all-in) and waits for the showdown; side pots
    ///   keep the others' extra bets apart.
    /// - Exact tie at a showdown: the pot is split.
    /// </summary>
    public sealed class TeenPattiHandEngine
    {
        public sealed class Config
        {
            public long Boot;
            public long StakeCap;       // highest blind stake; default 128 x boot (a seen chaal of 256 x boot)
            public long PotLimit;       // default 2048 x boot
            public int MaxBlinds = 4;
            public int CommissionPct;
            public long? CommissionCap;

            public static Config ForBoot(long boot, int commissionPct, long? commissionCap = null) => new Config
            {
                Boot = boot, StakeCap = boot * 128, PotLimit = boot * 2048, CommissionPct = commissionPct, CommissionCap = commissionCap,
            };
        }

        public sealed class Seat
        {
            public string UserId;
            public int Number;
            public Card[] Cards;
            public bool Seen;
            public int BlindChaals;
        }

        public enum Phase { Betting, SideShow, Over }

        public readonly Config Rules;
        public readonly int HandNo;
        public readonly HandLedger Ledger;
        readonly List<Seat> seats;                  // seat order around the table, starting left of the dealer
        public IReadOnlyList<Seat> Seats => seats;

        public long Stake { get; private set; }
        public int TurnIndex { get; private set; }  // index into Seats
        public Phase State { get; private set; }
        public string Reason { get; private set; }  // showdown | all_folded | side_show | pot_limit | timeout
        public HandLedger.Result Result { get; private set; }

        /// <summary>The side-show waiting for an answer: asker and target user ids.</summary>
        public (string asker, string target)? PendingSideShow { get; private set; }

        /// <summary>Everything that happened, for the match log and for the phones (one line per event).</summary>
        public readonly List<string> Log = new List<string>();

        string lastAction;

        /// <param name="players">(user id, seat number, stack) of everyone in the hand, in seat order starting with the
        /// first player after the dealer. Each must have at least one boot.</param>
        public TeenPattiHandEngine(int handNo, Config rules, IList<(string userId, int seat, long stack)> players, Deck deck)
        {
            if (players == null || players.Count < 2) throw new ArgumentException("A hand needs at least 2 players");
            if (rules.Boot <= 0) throw new ArgumentException("boot");
            HandNo = handNo;
            Rules = rules;
            Ledger = new HandLedger(rules.CommissionPct, rules.CommissionCap);
            seats = new List<Seat>();
            foreach (var p in players)
            {
                if (p.stack < rules.Boot) throw new ArgumentException($"{p.userId} has less than one boot");
                Ledger.Join(p.userId, p.seat, p.stack);
                Ledger.Put(p.userId, rules.Boot);
                seats.Add(new Seat { UserId = p.userId, Number = p.seat });
            }
            foreach (var s in seats) s.Cards = new[] { deck.Draw(), deck.Draw(), deck.Draw() };
            Stake = rules.Boot;
            TurnIndex = 0;
            State = Phase.Betting;
            Log.Add($"hand {handNo}: {seats.Count} players, boot {rules.Boot}, pot {Ledger.Pot}");
            SkipToActor();
        }

        // ── queries ─────────────────────────────────────────────────────────────────────────────

        public Seat SeatOf(string userId) => seats.FirstOrDefault(s => s.UserId == userId);
        public string OnTurn => State == Phase.Betting ? seats[TurnIndex].UserId : null;
        bool InHand(Seat s) => !Ledger.Get(s.UserId).Folded;
        bool CanAct(Seat s) => InHand(s) && !Ledger.Get(s.UserId).AllIn;
        public int PlayersIn => seats.Count(InHand);

        /// <summary>What a chaal costs this player now (raise: after doubling the stake).</summary>
        public long ChaalCost(string userId, bool raise = false)
        {
            var s = SeatOf(userId);
            long stake = raise ? Math.Min(Stake * 2, Rules.StakeCap) : Stake;
            return s.Seen ? stake * 2 : stake;
        }

        public bool CanRaise => Stake < Rules.StakeCap;
        public bool CanShow(string userId) => State == Phase.Betting && OnTurn == userId && PlayersIn == 2;

        /// <summary>The player a side-show from <paramref name="userId"/> goes to (the previous one still in), or null.</summary>
        public string SideShowTarget(string userId)
        {
            if (State != Phase.Betting || OnTurn != userId || PlayersIn < 3) return null;
            var me = SeatOf(userId);
            if (!me.Seen) return null;
            int i = seats.IndexOf(me);
            for (int k = 1; k < seats.Count; k++)
            {
                var prev = seats[(i - k + seats.Count) % seats.Count];
                if (!InHand(prev)) continue;
                return prev.Seen && !Ledger.Get(prev.UserId).AllIn ? prev.UserId : null;
            }
            return null;
        }

        // ── actions (each returns null when done, or why it was refused) ───────────────────────

        public string See(string userId)
        {
            var s = SeatOf(userId);
            if (s == null || State == Phase.Over || !InHand(s)) return "not in the hand";
            if (s.Seen) return null;
            s.Seen = true;
            Log.Add($"{userId} sees the cards");
            return null;
        }

        public string Chaal(string userId, bool raise)
        {
            string why = CheckTurn(userId);
            if (why != null) return why;
            if (raise && !CanRaise) return "the stake is at its limit";
            var s = SeatOf(userId);
            if (raise) Stake = Math.Min(Stake * 2, Rules.StakeCap);
            long cost = s.Seen ? Stake * 2 : Stake;
            long paid = Ledger.Put(userId, cost);
            if (!s.Seen && ++s.BlindChaals >= Rules.MaxBlinds) s.Seen = true;
            Log.Add($"{userId} {(raise ? "raises" : "chaal")} {paid}{(paid < cost ? " (all-in)" : "")}, stake {Stake}, pot {Ledger.Pot}");
            lastAction = "chaal";
            AfterBet();
            return null;
        }

        public string Pack(string userId, string why = "packs")
        {
            var s = SeatOf(userId);
            if (s == null || State == Phase.Over || !InHand(s)) return "not in the hand";
            bool wasTurn = State == Phase.Betting && OnTurn == userId;
            if (State == Phase.SideShow && PendingSideShow.HasValue &&
                (PendingSideShow.Value.asker == userId || PendingSideShow.Value.target == userId))
            {
                PendingSideShow = null;
                State = Phase.Betting;
            }
            Ledger.Fold(userId);
            Log.Add($"{userId} {why}");
            lastAction = why == "timed out" ? "timeout" : "pack";
            if (PlayersIn == 1) { Finish(lastAction == "timeout" ? "timeout" : "all_folded"); return null; }
            if (wasTurn || !CanAct(seats[TurnIndex])) NextTurn();
            CheckEndOfBetting();
            return null;
        }

        /// <summary>The turn timer ran out (or the player is away): the server packs them.</summary>
        public string Timeout(string userId) => Pack(userId, "timed out");

        public string Show(string userId)
        {
            string why = CheckTurn(userId);
            if (why != null) return why;
            if (PlayersIn != 2) return "show needs exactly 2 players";
            long cost = ChaalCost(userId);
            Ledger.Put(userId, cost);
            Log.Add($"{userId} asks for a show ({cost})");
            Finish("showdown");
            return null;
        }

        public string AskSideShow(string userId)
        {
            string why = CheckTurn(userId);
            if (why != null) return why;
            string target = SideShowTarget(userId);
            if (target == null) return "side-show is not possible now";
            long cost = ChaalCost(userId);
            Ledger.Put(userId, cost);
            PendingSideShow = (userId, target);
            State = Phase.SideShow;
            Log.Add($"{userId} pays {cost} and asks {target} for a side-show");
            return null;
        }

        /// <summary>The target's answer (no answer in time = decline).</summary>
        public string AnswerSideShow(string userId, bool accept)
        {
            if (State != Phase.SideShow || !PendingSideShow.HasValue || PendingSideShow.Value.target != userId) return "no side-show to answer";
            var (asker, target) = PendingSideShow.Value;
            PendingSideShow = null;
            State = Phase.Betting;
            if (!accept)
            {
                Log.Add($"{target} declines the side-show");
            }
            else
            {
                int c = Compare(asker, target);
                string loser = c > 0 ? target : asker;          // equal: the asker loses
                Log.Add($"side-show {asker} vs {target}: {loser} packs");
                Ledger.Fold(loser);
                if (PlayersIn == 1) { Finish("side_show"); return null; }
            }
            // The asker's turn is over either way.
            TurnIndex = seats.IndexOf(SeatOf(asker));
            NextTurn();
            CheckEndOfBetting();
            return null;
        }

        /// <summary>For the turn clock: whom to time out when the side-show answer does not come.</summary>
        public string WaitingFor => State == Phase.SideShow ? PendingSideShow?.target : OnTurn;

        // ── internals ───────────────────────────────────────────────────────────────────────────

        string CheckTurn(string userId)
        {
            if (State == Phase.Over) return "the hand is over";
            if (State == Phase.SideShow) return "waiting for the side-show answer";
            if (OnTurn != userId) return "not your turn";
            return null;
        }

        void AfterBet()
        {
            if (Ledger.Pot >= Rules.PotLimit) { Finish("pot_limit"); return; }
            NextTurn();
            CheckEndOfBetting();
        }

        void CheckEndOfBetting()
        {
            if (State == Phase.Over) return;
            // At most one player can still bet and the rest are all-in: nobody is left to bet against, so show
            // everything. (Chips above what the others could match go back to their owner, HandLedger.ReturnUncalled.)
            if (seats.Count(CanAct) <= 1 && PlayersIn >= 2) Finish("showdown");
        }

        void NextTurn()
        {
            for (int k = 1; k <= seats.Count; k++)
            {
                int i = (TurnIndex + k) % seats.Count;
                if (CanAct(seats[i])) { TurnIndex = i; return; }
            }
        }

        void SkipToActor()
        {
            if (!CanAct(seats[TurnIndex])) NextTurn();
        }

        int Compare(string a, string b) => TeenPattiHand.Score(SeatOf(a).Cards).CompareTo(TeenPattiHand.Score(SeatOf(b).Cards));

        void Finish(string reason)
        {
            State = Phase.Over;
            Reason = reason;
            // Odd chips of a split go round the table from the first seat after the dealer.
            Result = Ledger.Settle(Compare, seats.Select(s => s.UserId).ToList());
            Log.Add($"hand {HandNo} over ({reason}): pot {Result.Pot}, commission {Result.Commission}, " +
                    string.Join(", ", Result.Players.Where(r => r.won > 0).Select(r => $"{r.user_id} wins {r.won}")));
        }
    }
}
