using System;
using System.Collections.Generic;
using System.Linq;

namespace GamesPanda.Tables
{
    /// <summary>
    /// One hand of No-Limit Texas Hold'em at a table of 2-6, run only on the match server. The deck, the hole cards and
    /// the board live here; phones send intents (fold, check, call, bet / raise to an amount, all-in) and get told what
    /// they may see. Chips move through HandLedger, which pays side pots and the commission.
    ///
    /// Seat order: players are given starting with the button. Heads-up the button is the small blind and acts first
    /// before the flop, last after it. With 3+ the next two seats post the blinds and the seat after the big blind
    /// starts; after the flop the first player still in left of the button starts.
    /// Betting: a bet is at least the big blind; a raise is at least the last full raise. A short all-in raise does not
    /// let players who already acted raise again (they may only call or fold).
    /// </summary>
    public sealed class PokerHandEngine
    {
        public sealed class Config
        {
            public long SmallBlind, BigBlind;
            public int CommissionPct;
            public long? CommissionCap;
        }

        public enum Street { Preflop, Flop, Turn, River, Over }

        public sealed class Seat
        {
            public string UserId;
            public int Number;
            public Card[] Hole;
            public long StreetBet;     // chips put in on this street
        }

        public readonly Config Rules;
        public readonly int HandNo;
        public readonly HandLedger Ledger;
        readonly List<Seat> seats;     // [0] = button
        readonly Deck deck;
        readonly HashSet<string> acted = new HashSet<string>();
        public readonly List<Card> Board = new List<Card>();

        public IReadOnlyList<Seat> Seats => seats;
        public Street Phase { get; private set; }
        public long CurrentBet { get; private set; }     // highest StreetBet this street
        public long MinRaise { get; private set; }       // size of the last full raise
        public int TurnIndex { get; private set; }
        public string Reason { get; private set; }
        public HandLedger.Result Result { get; private set; }
        public readonly List<string> Log = new List<string>();

        /// <summary>Hole cards to show at the end (players still in at a showdown), user id → two cards.</summary>
        public Dictionary<string, Card[]> Shown { get; } = new Dictionary<string, Card[]>();

        public PokerHandEngine(int handNo, Config rules, IList<(string userId, int seat, long stack)> playersFromButton, Deck deck)
        {
            if (playersFromButton == null || playersFromButton.Count < 2) throw new ArgumentException("A hand needs at least 2 players");
            if (rules.BigBlind <= 0 || rules.SmallBlind <= 0 || rules.SmallBlind > rules.BigBlind) throw new ArgumentException("blinds");
            HandNo = handNo;
            Rules = rules;
            this.deck = deck;
            Ledger = new HandLedger(rules.CommissionPct, rules.CommissionCap);
            seats = new List<Seat>();
            foreach (var p in playersFromButton)
            {
                if (p.stack <= 0) throw new ArgumentException($"{p.userId} has no chips");
                Ledger.Join(p.userId, p.seat, p.stack);
                seats.Add(new Seat { UserId = p.userId, Number = p.seat });
            }
            // Two hole cards each, one at a time, starting left of the button.
            for (int round = 0; round < 2; round++)
                for (int k = 1; k <= seats.Count; k++)
                {
                    var s = seats[k % seats.Count];
                    s.Hole = s.Hole ?? new Card[2];
                    s.Hole[round] = deck.Draw();
                }

            bool headsUp = seats.Count == 2;
            int sb = headsUp ? 0 : 1, bb = headsUp ? 1 : 2;
            Phase = Street.Preflop;
            Post(seats[sb], rules.SmallBlind, "small blind");
            Post(seats[bb % seats.Count], rules.BigBlind, "big blind");
            CurrentBet = Math.Max(rules.BigBlind, seats.Max(s => s.StreetBet));
            MinRaise = rules.BigBlind;
            TurnIndex = headsUp ? 0 : (bb + 1) % seats.Count;
            Log.Add($"hand {handNo}: {seats.Count} players, blinds {rules.SmallBlind}/{rules.BigBlind}, button {seats[0].UserId}");
            if (!CanAct(seats[TurnIndex])) Advance();
            else CheckStreetEnd();
        }

        // ── queries ─────────────────────────────────────────────────────────────────────────────

        public Seat SeatOf(string userId) => seats.FirstOrDefault(s => s.UserId == userId);
        public string OnTurn => Phase == Street.Over ? null : seats[TurnIndex].UserId;
        bool InHand(Seat s) => !Ledger.Get(s.UserId).Folded;
        bool CanAct(Seat s) => InHand(s) && !Ledger.Get(s.UserId).AllIn;
        public int PlayersIn => seats.Count(InHand);
        public long StackOf(string userId) => Ledger.Get(userId).Stack;
        public long ToCall(string userId) => Math.Max(0, CurrentBet - SeatOf(userId).StreetBet);

        /// <summary>Smallest total street bet a bet or raise may go to (an all-in for less is always allowed).</summary>
        public long MinRaiseTo => CurrentBet == 0 ? Rules.BigBlind : CurrentBet + MinRaise;

        /// <summary>Most a player can bet this street: everything they have.</summary>
        public long MaxRaiseTo(string userId) => SeatOf(userId).StreetBet + StackOf(userId);

        /// <summary>Whether this player may still raise (not after a short all-in raise once they had acted).</summary>
        public bool MayRaise(string userId) => !acted.Contains(userId) && MaxRaiseTo(userId) > CurrentBet;

        // ── actions (null when done, else why it was refused) ───────────────────────────────────

        public string Fold(string userId, string why = "folds")
        {
            var s = SeatOf(userId);
            if (s == null || Phase == Street.Over || !InHand(s)) return "not in the hand";
            bool wasTurn = OnTurn == userId;
            Ledger.Fold(userId);
            Log.Add($"{userId} {why}");
            if (PlayersIn == 1) { Finish(why == "timed out" ? "timeout" : "all_folded"); return null; }
            if (wasTurn) Advance(); else CheckStreetEnd();
            return null;
        }

        public string Check(string userId)
        {
            string why = CheckTurn(userId);
            if (why != null) return why;
            if (ToCall(userId) > 0) return "there is a bet to call";
            acted.Add(userId);
            Log.Add($"{userId} checks");
            Advance();
            return null;
        }

        public string Call(string userId)
        {
            string why = CheckTurn(userId);
            if (why != null) return why;
            long owe = ToCall(userId);
            if (owe == 0) return "nothing to call";
            long paid = Pay(SeatOf(userId), owe);
            acted.Add(userId);
            Log.Add($"{userId} calls {paid}{(Ledger.Get(userId).AllIn ? " (all-in)" : "")}");
            Advance();
            return null;
        }

        /// <summary>Bet or raise so that this player's chips on this street total <paramref name="to"/>.</summary>
        public string RaiseTo(string userId, long to)
        {
            string why = CheckTurn(userId);
            if (why != null) return why;
            var s = SeatOf(userId);
            long max = MaxRaiseTo(userId);
            if (to > max) return "more than your stack";
            if (to <= CurrentBet) return "a raise must be above the current bet";
            if (acted.Contains(userId)) return "you can only call or fold";
            bool allIn = to == max;
            if (to < MinRaiseTo && !allIn) return $"the minimum is {MinRaiseTo}";

            long raiseSize = to - CurrentBet;
            bool full = raiseSize >= MinRaise || CurrentBet == 0 && to >= Rules.BigBlind;
            Pay(s, to - s.StreetBet);
            string word = CurrentBet == 0 ? "bets" : "raises to";
            if (full)
            {
                MinRaise = Math.Max(raiseSize, Rules.BigBlind);
                acted.Clear();          // everyone gets to act again
            }
            CurrentBet = to;
            acted.Add(userId);
            Log.Add($"{userId} {word} {to}{(allIn ? " (all-in)" : "")}{(full ? "" : ", not a full raise")}");
            Advance();
            return null;
        }

        public string AllIn(string userId)
        {
            long max = MaxRaiseTo(userId);
            return max > CurrentBet && !acted.Contains(userId) ? RaiseTo(userId, max) : Call(userId);
        }

        /// <summary>The turn timer ran out: check when nothing is owed, else fold.</summary>
        public string Timeout(string userId)
        {
            if (OnTurn != userId) return "not on turn";
            return ToCall(userId) == 0 ? Check(userId) : Fold(userId, "timed out");
        }

        // ── internals ───────────────────────────────────────────────────────────────────────────

        string CheckTurn(string userId)
        {
            if (Phase == Street.Over) return "the hand is over";
            if (OnTurn != userId) return "not your turn";
            return null;
        }

        void Post(Seat s, long amount, string what)
        {
            long paid = Pay(s, amount);
            Log.Add($"{s.UserId} posts {what} {paid}");
        }

        long Pay(Seat s, long amount)
        {
            long paid = Ledger.Put(s.UserId, amount);
            s.StreetBet += paid;
            return paid;
        }

        /// <summary>Next player to act, or the end of the street.</summary>
        void Advance()
        {
            if (Phase == Street.Over) return;
            if (CheckStreetEnd()) return;
            for (int k = 1; k <= seats.Count; k++)
            {
                int i = (TurnIndex + k) % seats.Count;
                if (CanAct(seats[i]) && (!acted.Contains(seats[i].UserId) || seats[i].StreetBet < CurrentBet))
                {
                    TurnIndex = i;
                    return;
                }
            }
            NextStreet();
        }

        /// <summary>True when the street (or the hand) ended here.</summary>
        bool CheckStreetEnd()
        {
            if (Phase == Street.Over) return true;
            if (PlayersIn == 1) { Finish("all_folded"); return true; }
            var canAct = seats.Where(CanAct).ToList();
            bool everyoneDone = canAct.All(s => acted.Contains(s.UserId) && s.StreetBet == CurrentBet);
            // Only one player can still bet and has matched everything: no more betting is possible.
            bool nobodyToBetAgainst = canAct.Count <= 1 && canAct.All(s => s.StreetBet >= CurrentBet);
            if (everyoneDone || nobodyToBetAgainst)
            {
                NextStreet();
                return true;
            }
            return false;
        }

        void NextStreet()
        {
            foreach (var s in seats) s.StreetBet = 0;
            acted.Clear();
            CurrentBet = 0;
            MinRaise = Rules.BigBlind;
            switch (Phase)
            {
                case Street.Preflop: deck.Draw(); Board.Add(deck.Draw()); Board.Add(deck.Draw()); Board.Add(deck.Draw()); Phase = Street.Flop; break;
                case Street.Flop: deck.Draw(); Board.Add(deck.Draw()); Phase = Street.Turn; break;
                case Street.Turn: deck.Draw(); Board.Add(deck.Draw()); Phase = Street.River; break;
                default: Finish("showdown"); return;
            }
            Log.Add($"{Phase}: {string.Join(" ", Board)}");
            // Run the board out when fewer than two players can still bet.
            if (seats.Count(CanAct) < 2) { NextStreet(); return; }
            for (int k = 1; k <= seats.Count; k++)
            {
                int i = k % seats.Count;
                if (CanAct(seats[i])) { TurnIndex = i; return; }
            }
        }

        long Strength(string userId) => PokerHand.Best(SeatOf(userId).Hole.Concat(Board).ToList());

        void Finish(string reason)
        {
            Phase = Street.Over;
            Reason = reason;
            if (reason == "showdown")
                foreach (var s in seats.Where(InHand)) Shown[s.UserId] = s.Hole;
            // Odd chips of a split go to the first winner left of the button.
            var order = Enumerable.Range(1, seats.Count).Select(k => seats[k % seats.Count].UserId).ToList();
            Result = Ledger.Settle((a, b) => Strength(a).CompareTo(Strength(b)), order);
            Log.Add($"hand {HandNo} over ({reason}): pot {Result.Pot}, commission {Result.Commission}, " +
                    string.Join(", ", Result.Players.Where(r => r.won > 0).Select(r => $"{r.user_id} wins {r.won}")));
        }
    }
}
