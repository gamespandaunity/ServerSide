using System;
using System.Collections.Generic;
using System.Linq;
using GamesPanda.Tables;

static partial class Program
{
    static long P(string cards) => PokerHand.Best(C(cards));

    /// <summary>Stacked hold'em deal. holes[i] = player i (P0 = button); hole cards are dealt one at a time starting
    /// left of the button, then burn + flop, burn + turn, burn + river.</summary>
    static PokerHandEngine Holdem(long sb, long bb, long[] stacks, string[] holes, string board)
    {
        int n = holes.Length;
        var h = holes.Select(C).ToArray();
        var order = new List<Card>();
        for (int round = 0; round < 2; round++)
            for (int k = 1; k <= n; k++) order.Add(h[k % n][round]);
        var b = C(board);
        var used = new HashSet<int>(order.Concat(b).Select(c => c.Index));
        var burns = Enumerable.Range(0, 52).Where(i => !used.Contains(i)).Take(3).Select(Card.FromIndex).ToArray();
        order.Add(burns[0]); order.AddRange(b.Take(3));
        order.Add(burns[1]); order.Add(b[3]);
        order.Add(burns[2]); order.Add(b[4]);
        var players = stacks.Select((s, i) => ($"P{i}", i, s)).ToList();
        return new PokerHandEngine(1, new PokerHandEngine.Config { SmallBlind = sb, BigBlind = bb, CommissionPct = 5 }, players, Deck.Stacked(order));
    }

    static void PokerHands()
    {
        Test("poker: category order", () =>
        {
            var order = new[]
            {
                "Ah Kh Qh Jh Th", "9c 9d 9h 9s 2c", "Kc Kd Kh 4s 4c", "2h 7h 9h Jh Ah", "5c 6d 7h 8s 9c",
                "Qc Qd Qh 4s 2c", "Jc Jd 4h 4s Ac", "Tc Td 4h 3s 2c", "Ac Jd 9h 6s 3c",
            };
            for (int i = 0; i + 1 < order.Length; i++)
                Check(P(order[i]) > P(order[i + 1]), $"{order[i]} should beat {order[i + 1]}");
        });
        Test("poker: the wheel A-2-3-4-5 is the lowest straight", () =>
        {
            Check(P("Ac 2d 3h 4s 5c") < P("2c 3d 4h 5s 6c"), "wheel < 6-high");
            Check(P("Ac 2d 3h 4s 5c") > P("Ac Ad Ah 4s 2d"), "wheel > trips");
            Check(P("Ah 2h 3h 4h 5h") > P("Kc Kd Kh Ks 2c"), "steel wheel > quads");
        });
        Test("poker: best five of seven; unused hole cards never break a tie", () =>
        {
            // Board plays a straight for both: split.
            Eq(P("2c 3d 9h Ts Jc Qd Kh"), P("2h 4d 9c Ts Jd Qs Kc"), "board straight ties");
            // Kickers count only within the best five.
            Check(P("Ac Kd 7h 7s 2c 3d 9h") > P("Ac Qd 7h 7s 2c 3d 9h"), "pair of 7s, K kicker > Q kicker");
            Eq(P("Ac Ad Kh Ks Qc Qd 2h"), P("Ah As Kc Kd Qh Qs 3c"), "two pair + Q kicker, 2 vs 3 unused");
        });
        Test("poker: full house compares trips first", () =>
        {
            Check(P("Kc Kd Kh 2s 2c") > P("Qc Qd Qh As Ac"), "KKK22 > QQQAA");
        });

        Test("poker heads-up: button posts the small blind and acts first before the flop", () =>
        {
            var h = Holdem(50, 100, new long[] { 4000, 4000 }, new[] { "Ah Ad", "7c 2d" }, "Kc 8h 3s 4d 9c");
            Eq(150L, h.Ledger.Pot, "blinds");
            Eq("P0", h.OnTurn, "button first");
            Ok(h.Call("P0"));
            Eq("P1", h.OnTurn, "big blind has the option");
            Ok(h.Check("P1"));
            Eq(PokerHandEngine.Street.Flop, h.Phase, "flop");
            Eq("P1", h.OnTurn, "after the flop the big blind acts first");
            Eq(3, h.Board.Count, "three on the board");
        });
        Test("poker 3-way: seat after the big blind starts; check-around to a showdown", () =>
        {
            var h = Holdem(50, 100, new long[] { 4000, 4000, 4000 }, new[] { "Ah Ad", "7c 2d", "Kh Qh" }, "Kc 8h 3s 4d 9c");
            Eq("P0", h.OnTurn, "under the gun is the button with 3");
            Ok(h.Call("P0")); Ok(h.Call("P1")); Ok(h.Check("P2"));
            for (int street = 0; street < 3; street++) { Ok(h.Check("P1")); Ok(h.Check("P2")); Ok(h.Check("P0")); }
            Eq(PokerHandEngine.Street.Over, h.Phase, "over");
            Eq("showdown", h.Reason, "reason");
            Eq(5, h.Board.Count, "board");
            Eq(3, h.Shown.Count, "three hands shown");
            var win = h.Result.Players.Single(p => p.user_id == "P0");
            Eq(300L - 15, win.won, "aces win 300 less 15");
        });
        Test("poker: bets below the minimum are refused; a raise must be at least the last raise", () =>
        {
            var h = Holdem(50, 100, new long[] { 4000, 4000, 4000 }, new[] { "Ah Ad", "7c 2d", "Kh Qh" }, "Kc 8h 3s 4d 9c");
            Check(h.RaiseTo("P0", 150) != null, "raise to 150 (raise of 50) refused");
            Ok(h.RaiseTo("P0", 300));                 // raise of 200
            Check(h.RaiseTo("P1", 400) != null, "re-raise of 100 refused");
            Ok(h.RaiseTo("P1", 500));                 // re-raise of 200
            Eq(700L, h.MinRaiseTo, "next minimum");
        });
        Test("poker: everyone folds to a raise", () =>
        {
            var h = Holdem(50, 100, new long[] { 4000, 4000, 4000 }, new[] { "Ah Ad", "7c 2d", "Kh Qh" }, "Kc 8h 3s 4d 9c");
            Ok(h.RaiseTo("P0", 300));
            Ok(h.Fold("P1")); Ok(h.Fold("P2"));
            Eq("all_folded", h.Reason, "reason");
            Eq(0, h.Shown.Count, "nobody shows");
            // P0's raise above the big blind was not called: 200 back, pot 300 → commission 15.
            var r = h.Result;
            Eq(r.Pot, r.Players.Sum(p => p.won) + r.Commission, "invariant");
            Eq(4000L - 100 + (r.Pot - r.Commission) - 0, r.Players.Single(p => p.user_id == "P0").stack_after, "P0 stack");
        });
        Test("poker: a short all-in raise does not reopen the betting", () =>
        {
            var h = Holdem(50, 100, new long[] { 4000, 4000, 350 }, new[] { "Ah Ad", "7c 2d", "Kh Qh" }, "Kc 8h 3s 4d 9c");
            Ok(h.RaiseTo("P0", 300));                 // full raise
            Ok(h.Call("P1"));
            Ok(h.AllIn("P2"));                        // to 350: only 50 more, not a full raise
            Eq("P0", h.OnTurn, "P0 must answer the all-in");
            Check(h.RaiseTo("P0", 1000) != null, "P0 may not re-raise");
            Ok(h.Call("P0"));
            Check(h.RaiseTo("P1", 1000) != null, "P1 may not re-raise either");
            Ok(h.Call("P1"));
            Eq(PokerHandEngine.Street.Flop, h.Phase, "flop");
        });
        Test("poker: three all-ins build side pots and the board runs out", () =>
        {
            var h = Holdem(50, 100, new long[] { 1000, 300, 600 }, new[] { "7c 2d", "Ah Ad", "Kh Kd" }, "Qc 8h 3s 4d 9c");
            Ok(h.AllIn("P0"));                        // 1000
            Ok(h.AllIn("P1"));                        // 300
            Ok(h.AllIn("P2"));                        // 600
            Eq(PokerHandEngine.Street.Over, h.Phase, "over");
            Eq(5, h.Board.Count, "board ran out");
            var r = h.Result;
            Eq(r.Pot, r.Players.Sum(p => p.won) + r.Commission, "invariant");
            // P0's 400 above P2's 600 is returned. Main pot 900 → aces; side pot 600 (P0, P2) → kings.
            Eq(1500L, r.Pot, "pot after the refund");
            Check(r.Players.Single(p => p.user_id == "P1").won > 800, "aces take the main pot");
            Check(r.Players.Single(p => p.user_id == "P2").won > 500, "kings take the side pot");
            Eq(400L, r.Players.Single(p => p.user_id == "P0").stack_after, "P0 keeps the unmatched 400");
        });
        Test("poker: timeout checks when free, folds when there is a bet", () =>
        {
            var h = Holdem(50, 100, new long[] { 4000, 4000, 4000 }, new[] { "Ah Ad", "7c 2d", "Kh Qh" }, "Kc 8h 3s 4d 9c");
            Ok(h.Timeout("P0"));                      // owes 100: folds
            Check(h.Ledger.Get("P0").Folded, "P0 folded");
            Ok(h.Call("P1"));
            Ok(h.Timeout("P2"));                      // big blind owes nothing: checks
            Check(!h.Ledger.Get("P2").Folded, "P2 checked");
            Eq(PokerHandEngine.Street.Flop, h.Phase, "flop");
        });
        Test("poker: split pot on the board", () =>
        {
            var h = Holdem(50, 100, new long[] { 4000, 4000 }, new[] { "2c 3d", "2h 3s" }, "Tc Jd Qh Ks Ac");
            Ok(h.Call("P0")); Ok(h.Check("P1"));
            for (int s = 0; s < 3; s++) { Ok(h.Check("P1")); Ok(h.Check("P0")); }
            var r = h.Result;
            Eq(r.Players[0].won, r.Players[1].won, "even split of 190");
        });
    }
}
