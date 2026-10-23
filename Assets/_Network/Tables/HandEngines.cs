using System;
using System.Collections.Generic;
using System.Linq;

namespace GamesPanda.Tables
{
    /// <summary>One running hand, Teen Patti or Poker, as the table loop (TableRunner) sees it.</summary>
    public interface IHandEngine
    {
        int HandNo { get; }
        bool Over { get; }
        string Reason { get; }
        HandLedger Ledger { get; }
        HandLedger.Result Result { get; }
        /// <summary>The player whose timer runs (side-show: the asked player).</summary>
        string WaitingFor { get; }
        bool IsSideShowAnswer { get; }
        IList<Card> Board { get; }
        IDictionary<string, Card[]> Shown { get; }
        Card[] HoleOf(string userId);
        /// <summary>null when done, else why it was refused.</summary>
        string Act(string userId, string action, long amount, bool yes);
        /// <summary>The timer ran out for <see cref="WaitingFor"/>.</summary>
        void TimeOut();
        /// <summary>The player leaves the table or was removed: out of this hand now.</summary>
        void Abandon(string userId);
        void Fill(TableProtocol.View view);
        /// <summary>Log lines added since the last call.</summary>
        IEnumerable<string> TakeLog();
        /// <summary>Side-show done: both players see each other's cards (user → the other's user and cards).</summary>
        IEnumerable<(string to, string otherUser, Card[] otherCards)> TakeReveals();
        string Describe(string userId);
    }

    public sealed class TeenPattiAdapter : IHandEngine
    {
        readonly TeenPattiHandEngine h;
        int logged;
        readonly List<(string, string, Card[])> reveals = new List<(string, string, Card[])>();

        public TeenPattiAdapter(TeenPattiHandEngine engine) { h = engine; }

        public int HandNo => h.HandNo;
        public bool Over => h.State == TeenPattiHandEngine.Phase.Over;
        public string Reason => h.Reason;
        public HandLedger Ledger => h.Ledger;
        public HandLedger.Result Result => h.Result;
        public string WaitingFor => Over ? null : h.WaitingFor;
        public bool IsSideShowAnswer => h.State == TeenPattiHandEngine.Phase.SideShow;
        public IList<Card> Board => Array.Empty<Card>();
        public Card[] HoleOf(string userId) => h.SeatOf(userId)?.Cards;

        public IDictionary<string, Card[]> Shown
        {
            get
            {
                var d = new Dictionary<string, Card[]>();
                if (Over && (Reason == "showdown" || Reason == "pot_limit"))
                    foreach (var s in h.Seats) if (!h.Ledger.Get(s.UserId).Folded) d[s.UserId] = s.Cards;
                return d;
            }
        }

        public string Act(string userId, string action, long amount, bool yes)
        {
            switch (action)
            {
                case "see": return h.See(userId);
                case "chaal": return h.Chaal(userId, false);
                case "raise": return h.Chaal(userId, true);
                case "pack": return h.Pack(userId);
                case "show": return h.Show(userId);
                case "sideshow": return h.AskSideShow(userId);
                case "answer":
                {
                    var pending = h.PendingSideShow;
                    string why = h.AnswerSideShow(userId, yes);
                    if (why == null && yes && pending.HasValue)
                    {
                        var (a, b) = pending.Value;
                        reveals.Add((a, b, h.SeatOf(b).Cards));
                        reveals.Add((b, a, h.SeatOf(a).Cards));
                    }
                    return why;
                }
                default: return "unknown action " + action;
            }
        }

        public void TimeOut()
        {
            if (Over) return;
            if (h.State == TeenPattiHandEngine.Phase.SideShow) h.AnswerSideShow(h.WaitingFor, false);
            else h.Timeout(h.OnTurn);
        }

        public void Abandon(string userId)
        {
            if (Over || h.SeatOf(userId) == null) return;
            h.Pack(userId, "left the table");
        }

        public void Fill(TableProtocol.View v)
        {
            v.pot = h.Ledger.Pot;
            v.stake = h.Stake;
            foreach (var s in h.Seats)
            {
                var row = v.players.FirstOrDefault(p => p.user == s.UserId);
                if (row == null) continue;
                var p = h.Ledger.Get(s.UserId);
                row.stack = p.Stack;
                row.bet = p.Contributed;
                row.seen = s.Seen;
                row.state = p.Folded ? "packed" : p.AllIn ? "allin" : "in";
            }
            if (h.PendingSideShow.HasValue)
            {
                v.sideShowFrom = h.SeatOf(h.PendingSideShow.Value.asker).Number;
                v.sideShowTo = h.SeatOf(h.PendingSideShow.Value.target).Number;
            }
        }

        public IEnumerable<string> TakeLog()
        {
            var lines = h.Log.Skip(logged).ToList();
            logged = h.Log.Count;
            return lines;
        }

        public IEnumerable<(string to, string otherUser, Card[] otherCards)> TakeReveals()
        {
            var r = reveals.ToList();
            reveals.Clear();
            return r;
        }

        public string Describe(string userId)
        {
            var c = HoleOf(userId);
            if (c == null) return null;
            TeenPattiHand.Evaluate(c[0], c[1], c[2], out var cat);
            return cat.ToString();
        }
    }

    public sealed class PokerAdapter : IHandEngine
    {
        readonly PokerHandEngine h;
        int logged;

        public PokerAdapter(PokerHandEngine engine) { h = engine; }

        public int HandNo => h.HandNo;
        public bool Over => h.Phase == PokerHandEngine.Street.Over;
        public string Reason => h.Reason;
        public HandLedger Ledger => h.Ledger;
        public HandLedger.Result Result => h.Result;
        public string WaitingFor => h.OnTurn;
        public bool IsSideShowAnswer => false;
        public IList<Card> Board => h.Board;
        public IDictionary<string, Card[]> Shown => h.Shown;
        public Card[] HoleOf(string userId) => h.SeatOf(userId)?.Hole;

        public string Act(string userId, string action, long amount, bool yes)
        {
            switch (action)
            {
                case "fold": return h.Fold(userId);
                case "check": return h.Check(userId);
                case "call": return h.Call(userId);
                case "raise": return h.RaiseTo(userId, amount);
                case "allin": return h.AllIn(userId);
                default: return "unknown action " + action;
            }
        }

        public void TimeOut()
        {
            if (!Over) h.Timeout(h.OnTurn);
        }

        public void Abandon(string userId)
        {
            if (Over || h.SeatOf(userId) == null) return;
            h.Fold(userId, "left the table");
        }

        public void Fill(TableProtocol.View v)
        {
            v.pot = h.Ledger.Pot;
            v.currentBet = h.CurrentBet;
            v.minRaiseTo = h.MinRaiseTo;
            v.board = h.Board.Select(c => c.Index).ToArray();
            v.street = h.Phase == PokerHandEngine.Street.Over ? null : h.Phase.ToString();
            foreach (var s in h.Seats)
            {
                var row = v.players.FirstOrDefault(p => p.user == s.UserId);
                if (row == null) continue;
                var p = h.Ledger.Get(s.UserId);
                row.stack = p.Stack;
                row.bet = s.StreetBet;
                row.state = p.Folded ? "packed" : p.AllIn ? "allin" : "in";
            }
        }

        public IEnumerable<string> TakeLog()
        {
            var lines = h.Log.Skip(logged).ToList();
            logged = h.Log.Count;
            return lines;
        }

        public IEnumerable<(string to, string otherUser, Card[] otherCards)> TakeReveals() => Array.Empty<(string, string, Card[])>();

        public string Describe(string userId)
        {
            var hole = HoleOf(userId);
            if (hole == null || h.Board.Count < 3) return null;
            PokerHand.Best(hole.Concat(h.Board).ToList(), out var cat, out _);
            return cat.ToString();
        }
    }
}
