using System;
using System.Collections.Generic;
using System.Linq;
using GamesPanda.Tables;

static partial class Program
{
    /// <summary>The backend as the contract describes it: seats from tickets, escrow per player, every settle checked.</summary>
    sealed class FakeBank : ITableBank
    {
        public readonly Dictionary<string, (string user, int seat, long stack)> Tickets = new Dictionary<string, (string, int, long)>();
        public readonly Dictionary<string, long> Escrow = new Dictionary<string, long>();
        public readonly List<string> Calls = new List<string>();
        public int CommissionPct = 5;
        public long House;
        public bool LieAboutStacks;
        public Dictionary<string, long> Wallet = new Dictionary<string, long>();
        int round = 8800;

        public void VerifyTicket(string ticket, Action<string, int, long, string> done)
        {
            Calls.Add("verify " + ticket);
            if (!Tickets.TryGetValue(ticket, out var t)) { done(null, -1, 0, "invalid ticket"); return; }
            Tickets.Remove(ticket);
            if (!Escrow.ContainsKey(t.user)) Escrow[t.user] = t.stack;
            done(t.user, t.seat, Escrow[t.user], null);
        }

        public void OpenHand(int handNo, IList<string> players, long boot, Action<string, string> done)
        {
            Calls.Add($"open {handNo} {string.Join(",", players)}");
            if (players.Count < 2) { done(null, "fewer than 2 players"); return; }
            done((++round).ToString(), null);
        }

        public void SettleHand(string roundId, int handNo, string reason, HandLedger.Result r, IList<Card> board,
                               IDictionary<string, Card[]> shown, Action<Dictionary<string, long>, Dictionary<string, long>, string> done)
        {
            Calls.Add($"settle {handNo} {reason}");
            long pot = r.Players.Sum(p => p.contributed);
            if (pot != r.Players.Sum(p => p.won) + r.Commission) { done(null, null, "amounts do not add up"); return; }
            if (r.Commission != pot * CommissionPct / 100) { done(null, null, "commission must be " + pot * CommissionPct / 100); return; }
            var expected = r.Players.ToDictionary(p => p.user_id, p => Escrow[p.user_id] - p.contributed + p.won);
            if (LieAboutStacks || r.Players.Any(p => p.stack_after != expected[p.user_id])) { done(null, expected, "stack mismatch"); return; }
            foreach (var p in r.Players) Escrow[p.user_id] = p.stack_after;
            House += r.Commission;
            done(new Dictionary<string, long>(Escrow), null, null);
        }

        public void Leave(string userId, long stack, string reason, int attempt, Action<string> done)
        {
            Calls.Add($"leave {userId} {stack} {reason}");
            if (!Escrow.TryGetValue(userId, out long e)) { done("not seated"); return; }
            if (e != stack) { done("stack mismatch"); return; }
            Wallet[userId] = (Wallet.TryGetValue(userId, out long w) ? w : 0) + stack;
            Escrow.Remove(userId);
            done(null);
        }

        public void Close(string reason, IDictionary<string, long> stacks, Action<string> done)
        {
            Calls.Add($"close {reason}");
            foreach (var kv in Escrow) Wallet[kv.Key] = (Wallet.TryGetValue(kv.Key, out long w) ? w : 0) + kv.Value;
            Escrow.Clear();
            done(null);
        }
    }

    sealed class FakeOut : ITableOutput
    {
        public readonly List<object> All = new List<object>();
        public readonly Dictionary<string, List<object>> To = new Dictionary<string, List<object>>();
        public readonly List<string> Lines = new List<string>();
        public void Broadcast(object m) => All.Add(m);
        public void Send(string u, object m) { if (!To.TryGetValue(u, out var l)) To[u] = l = new List<object>(); l.Add(m); }
        public void SendToConnection(object c, object m) => Send("conn:" + c, m);
        public void Log(string line) => Lines.Add(line);
        public TableProtocol.View LastView => All.OfType<TableProtocol.View>().Last();
        public T Last<T>(string user) where T : class => To.TryGetValue(user, out var l) ? l.OfType<T>().LastOrDefault() : null;
    }

    sealed class Rig
    {
        public FakeBank Bank = new FakeBank();
        public FakeOut Out = new FakeOut();
        public TableRunner Table;
        public DateTime Now = T0;
        public Rig(bool poker, DateTime? closeAt = null)
        {
            var rules = new TableRules
            {
                Poker = poker, Seats = poker ? 6 : 5, Boot = 50, SmallBlind = 50, BigBlind = 100, CommissionPct = 5,
                CloseAtUtc = closeAt ?? T0.AddMinutes(60),
            };
            Table = new TableRunner(rules, Bank, Out);
        }
        public void Sit(string user, int seat, long stack)
        {
            Bank.Tickets["t-" + user] = (user, seat, stack);
            Table.Message("c-" + user, new TableProtocol.In { t = "ticket", ticket = "t-" + user, name = user });
        }
        public void Wait(double seconds) { for (double s = 0; s < seconds; s += 0.5) { Now = Now.AddSeconds(0.5); Table.Tick(Now); } }
        public string Act(string user, string a, long amount = 0, bool yes = false)
        {
            int before = Out.To.TryGetValue(user, out var l) ? l.Count : 0;
            Table.Message("c-" + user, new TableProtocol.In { t = "act", a = a, amount = amount, yes = yes });
            Table.Tick(Now);
            var err = Out.To.TryGetValue(user, out l) ? l.Skip(before).OfType<TableProtocol.Private>().FirstOrDefault(m => m.t == "error") : null;
            return err?.text;
        }
        public string OnTurn => Table.Hand?.WaitingFor;
    }

    static void Runner()
    {
        Test("table: two players sit, the first hand is dealt after the start delay", () =>
        {
            var r = new Rig(false);
            r.Sit("A", 0, 2000); r.Sit("B", 2, 2000);
            r.Wait(1);
            Eq(TableRunner.Phase.Waiting, r.Table.State, "waiting during the delay");
            Eq("Next hand starting", r.Out.LastView.message, "message");
            r.Wait(4);
            Eq(TableRunner.Phase.Playing, r.Table.State, "playing");
            Check(r.Bank.Calls.Contains("open 1 B,A"), "opened hand 1 with B after the dealer A: " + string.Join(" | ", r.Bank.Calls));
            Eq(3, r.Out.Last<TableProtocol.Private>("A").cards.Length, "A got only their own 3 cards");
            Check(r.Out.To["B"].OfType<TableProtocol.Private>().All(p => p.t != "hole" || p.cards.Length == 3), "B too");
            Check(r.Out.All.OfType<TableProtocol.View>().All(v => v.players.All(p => p.GetType().GetField("cards") == null)), "no cards in the public view");
        });
        Test("table: a hand is played, settled with the backend, and the next one starts", () =>
        {
            var r = new Rig(false);
            r.Sit("A", 0, 2000); r.Sit("B", 1, 2000); r.Sit("C", 2, 2000);
            r.Wait(5);
            Eq("B", r.OnTurn, "B after dealer A");
            Ok(r.Act("B", "chaal"));
            Ok(r.Act("C", "pack"));
            Ok(r.Act("A", "pack"));
            Check(r.Bank.Calls.Any(c => c == "settle 1 all_folded"), "settled");
            var res = r.Out.All.OfType<TableProtocol.Result>().Single();
            Eq("B", res.winners.Single(), "B wins");
            Eq(r.Bank.Escrow["B"], res.players.Single(p => p.user == "B").stack, "B's stack from the backend");
            Eq(TableRunner.Phase.Result, r.Table.State, "result pause");
            r.Wait(5 + 4 + 1);
            Eq(TableRunner.Phase.Playing, r.Table.State, "hand 2");
            Eq(2, r.Table.HandNo, "hand number");
            Eq(1, r.Table.Dealer, "dealer moved to B");
            Eq(6000L - r.Bank.House, r.Bank.Escrow.Values.Sum(), "chips only leave the table as commission");
        });
        Test("table: out of time — Teen Patti packs the player on turn", () =>
        {
            var r = new Rig(false);
            r.Sit("A", 0, 2000); r.Sit("B", 1, 2000); r.Sit("C", 2, 2000);
            r.Wait(5);
            Eq("B", r.OnTurn, "B");
            r.Wait(16);
            Check(r.Table.Hand.Ledger.Get("B").Folded, "B packed on time");
            Eq("C", r.OnTurn, "C now");
        });
        Test("table: refusals go back to the player only", () =>
        {
            var r = new Rig(false);
            r.Sit("A", 0, 2000); r.Sit("B", 1, 2000);
            r.Wait(5);
            Eq("not your turn", r.Act("A", "chaal"), "A out of turn");
            Eq("side-show is not possible now", r.Act("B", "sideshow"), "side-show with 2");
        });
        Test("table: leaving mid-hand packs now and stands up after the settle", () =>
        {
            var r = new Rig(false);
            r.Sit("A", 0, 2000); r.Sit("B", 1, 2000); r.Sit("C", 2, 2000);
            r.Wait(5);
            r.Table.Message("c-C", new TableProtocol.In { t = "leave" });
            r.Table.Tick(r.Now);
            Check(r.Table.Hand.Ledger.Get("C").Folded, "C packed");
            Ok(r.Act("B", "pack"));
            Check(r.Bank.Calls.Any(c => c.StartsWith("leave C 1950 left")), "C cashed out after the hand: " + string.Join(" | ", r.Bank.Calls));
            Eq(1950L, r.Bank.Wallet["C"], "C got the stack back");
            Check(r.Table.Seats.Find("C") == null, "seat free");
        });
        Test("table: a disconnected player is skipped, then stood up as away", () =>
        {
            var r = new Rig(false);
            r.Sit("A", 0, 2000); r.Sit("B", 1, 2000); r.Sit("C", 2, 2000);
            r.Table.Disconnected("c-C");
            r.Wait(5);
            Check(r.Bank.Calls.Contains("open 1 B,A"), "C not dealt in");
            Ok(r.Act("B", "pack"));
            r.Wait(10);
            Ok(r.Act(r.OnTurn, "pack"));
            Check(r.Bank.Calls.Any(c => c.StartsWith("leave C 2000 away")), "C stood up after 2 hands away: " + string.Join(" | ", r.Bank.Calls));
        });
        Test("table: a reconnect keeps the seat and gets the own cards again", () =>
        {
            var r = new Rig(false);
            r.Sit("A", 0, 2000); r.Sit("B", 1, 2000);
            r.Wait(5);
            r.Table.Disconnected("c-A");
            r.Bank.Tickets["t-A2"] = ("A", 4, 0);
            r.Table.Message("c-A2", new TableProtocol.In { t = "ticket", ticket = "t-A2" });
            Eq(0, r.Table.Seats.Find("A").Number, "same seat");
            Eq("hole", r.Out.Last<TableProtocol.Private>("A").t, "cards sent again");
            Eq("A", r.Table.UserOf("c-A2"), "new connection");
        });
        Test("table: a stack mismatch closes the table and pays the backend's stacks", () =>
        {
            var r = new Rig(false);
            r.Sit("A", 0, 2000); r.Sit("B", 1, 2000);
            r.Wait(5);
            r.Bank.LieAboutStacks = true;
            Ok(r.Act("B", "pack"));
            Check(r.Bank.Calls.Contains("close error"), "closed");
            Eq(TableRunner.Phase.Closed, r.Table.State, "closed");
            Eq(4000L, r.Bank.Wallet.Values.Sum(), "everyone paid as of the last settled hand");
        });
        Test("table: no new hand in the last 3 minutes; closes at CloseAt", () =>
        {
            var r = new Rig(false, T0.AddMinutes(4));
            r.Sit("A", 0, 2000); r.Sit("B", 1, 2000);
            r.Wait(5);
            Ok(r.Act("B", "pack"));
            r.Wait(70);   // past CloseAt - 3 min
            Eq(TableRunner.Phase.Waiting, r.Table.State, "no second hand");
            Eq("The table is closing", r.Out.LastView.message, "message");
            r.Wait(200);
            Check(r.Bank.Calls.Contains("close time"), "closed on time");
            Eq(4000L - r.Bank.House, r.Bank.Wallet.Values.Sum(), "all paid");
        });
        Test("poker table: blinds, actions, showdown, settle", () =>
        {
            var r = new Rig(true);
            r.Sit("A", 0, 4000); r.Sit("B", 1, 4000); r.Sit("C", 3, 4000);
            r.Wait(5);
            Check(r.Bank.Calls.Contains("open 1 A,B,C"), "poker order from the button A: " + string.Join(" | ", r.Bank.Calls));
            Eq(2, r.Out.Last<TableProtocol.Private>("B").cards.Length, "2 hole cards");
            Eq("A", r.OnTurn, "under the gun (button with 3)");
            Ok(r.Act("A", "call")); Ok(r.Act("B", "call")); Ok(r.Act("C", "check"));
            Eq("Flop", r.Out.LastView.street, "flop");
            Eq(3, r.Out.LastView.board.Length, "board in the view");
            for (int i = 0; i < 9; i++) Ok(r.Act(r.OnTurn, "check"));
            var res = r.Out.All.OfType<TableProtocol.Result>().Single();
            Eq("showdown", res.reason, "showdown");
            Eq(3, res.shown.Count, "three hands shown");
            Eq(5, res.board.Length, "board");
            Eq(12000L - r.Bank.House, r.Bank.Escrow.Values.Sum(), "chips kept");
        });
        Test("poker table: a raise below the minimum is refused, all-in works", () =>
        {
            var r = new Rig(true);
            r.Sit("A", 0, 4000); r.Sit("B", 1, 4000);
            r.Wait(5);
            Eq("A", r.OnTurn, "heads-up: button first");
            Check(r.Act("A", "raise", 150) != null, "raise to 150 refused");
            Ok(r.Act("A", "allin"));
            Ok(r.Act("B", "call"));
            Check(r.Bank.Calls.Any(c => c == "settle 1 showdown"), "run out and settled");
            Eq(8000L - r.Bank.House, r.Bank.Escrow.Values.Sum(), "chips kept");
        });
        Test("table: a refused ticket is answered on that connection only", () =>
        {
            var r = new Rig(false);
            r.Table.Message("c-X", new TableProtocol.In { t = "ticket", ticket = "nope" });
            Eq("invalid ticket", r.Out.Last<TableProtocol.Private>("conn:c-X").text, "error");
            Check(r.Table.Seats.Occupied.Count() == 0, "nobody seated");
        });
    }
}
