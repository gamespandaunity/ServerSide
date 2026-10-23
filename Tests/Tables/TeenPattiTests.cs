using System;
using System.Collections.Generic;
using System.Linq;
using GamesPanda.Tables;

static partial class Program
{
    /// <summary>Stacked deal: hands[i] goes to player i (cards are drawn round by round: 1st card of each, 2nd, 3rd).</summary>
    static TeenPattiHandEngine Hand(long boot, long[] stacks, params string[] hands)
    {
        var cards = hands.Select(C).ToList();
        var top = new List<Card>();
        for (int k = 0; k < 3; k++) foreach (var h in cards) top.Add(h[k]);
        var players = stacks.Select((s, i) => ($"P{i}", i, s)).ToList();
        // Engine draws three cards per player in turn, so stack in that order.
        var order = new List<Card>();
        foreach (var h in cards) order.AddRange(h);
        return new TeenPattiHandEngine(1, TeenPattiHandEngine.Config.ForBoot(boot, 5), players, Deck.Stacked(order));
    }

    static void Ok(string why) { if (why != null) throw new Exception("refused: " + why); }

    static void TeenPattiHands()
    {
        Test("teen patti: boot from everyone, first player after the dealer starts", () =>
        {
            var h = Hand(50, new long[] { 2000, 2000, 2000 }, "Ah Ad Ac", "2c 7d 9h", "3c 8d Th");
            Eq(150L, h.Ledger.Pot, "pot after boots");
            Eq("P0", h.OnTurn, "turn");
            Eq("Ah Ad Ac", string.Join(" ", h.SeatOf("P0").Cards.Select(c => c.ToString())), "stacked cards");
        });
        Test("teen patti: blind pays the stake, seen pays double, raise doubles the stake", () =>
        {
            var h = Hand(50, new long[] { 2000, 2000, 2000 }, "Ah Ad Ac", "2c 7d 9h", "3c 8d Th");
            Ok(h.Chaal("P0", false));                 // blind 50
            Ok(h.See("P1")); Ok(h.Chaal("P1", false)); // seen 100
            Ok(h.Chaal("P2", true));                  // blind raise: stake 100, pays 100
            Eq(100L, h.Stake, "stake");
            Eq(50L + 50, h.Ledger.Get("P0").Contributed, "P0");
            Eq(50L + 100, h.Ledger.Get("P1").Contributed, "P1");
            Eq(50L + 100, h.Ledger.Get("P2").Contributed, "P2");
            Eq(200L, h.ChaalCost("P1"), "seen cost at stake 100");
        });
        Test("teen patti: out of turn is refused", () =>
        {
            var h = Hand(50, new long[] { 2000, 2000 }, "Ah Ad Ac", "2c 7d 9h");
            Check(h.Chaal("P1", false) != null, "P1 played on P0's turn");
        });
        Test("teen patti: everyone packs to one player (all_folded) — winner takes the pot less commission", () =>
        {
            var h = Hand(50, new long[] { 2000, 2000, 2000 }, "2c 7d 9h", "Ah Ad Ac", "3c 8d Th");
            Ok(h.Chaal("P0", false));
            Ok(h.Pack("P1"));
            Ok(h.Pack("P2"));
            Eq(TeenPattiHandEngine.Phase.Over, h.State, "over");
            Eq("all_folded", h.Reason, "reason");
            // P0's second 50 was never matched, so it goes back first: pot 150, commission 7.
            var p0 = h.Result.Players.Single(p => p.user_id == "P0");
            Eq(50L, p0.contributed, "P0's unmatched chaal returned");
            Eq(150L - 7, p0.won, "150 pot, 7 commission");
            Eq(2000L - 50 + 143, p0.stack_after, "stack after");
        });
        Test("teen patti: show only with 2 left; the better hand wins", () =>
        {
            var h = Hand(50, new long[] { 2000, 2000, 2000 }, "2c 7d 9h", "Ah Ad Ac", "3c 8d Th");
            Check(h.Show("P0") != null, "show with 3 players");
            Ok(h.Pack("P0"));
            Ok(h.Chaal("P1", false));
            Ok(h.Show("P2"));                          // P2 blind pays 50
            Eq("showdown", h.Reason, "reason");
            Check(h.Result.Players.Single(p => p.user_id == "P1").won > 0, "trail of aces wins");
        });
        Test("teen patti: side-show — accepted, the lower hand packs", () =>
        {
            var h = Hand(50, new long[] { 2000, 2000, 2000 }, "Kh Kd 2s", "2c 7d 9h", "3c 8d Th");
            Ok(h.See("P0")); Ok(h.Chaal("P0", false));
            Ok(h.See("P1")); Ok(h.Chaal("P1", false));
            Ok(h.See("P2"));
            Check(h.SideShowTarget("P2") == "P1", "target is the previous player");
            Ok(h.AskSideShow("P2"));
            Eq(TeenPattiHandEngine.Phase.SideShow, h.State, "waiting for the answer");
            Check(h.Chaal("P0", false) != null, "nobody plays during a side-show");
            Ok(h.AnswerSideShow("P1", true));          // P1 (9 high) vs P2 (T high): P1 packs
            Check(h.Ledger.Get("P1").Folded, "P1 packed");
            Eq("P0", h.OnTurn, "turn goes on after the asker");
        });
        Test("teen patti: side-show — equal hands, the asker packs; declined, nothing changes", () =>
        {
            var h = Hand(50, new long[] { 2000, 2000, 2000 }, "Kh Kd 2s", "2c 7d 9h", "2d 7c 9s");
            Ok(h.See("P0")); Ok(h.Chaal("P0", false));
            Ok(h.See("P1")); Ok(h.Chaal("P1", false));
            Ok(h.See("P2")); Ok(h.AskSideShow("P2"));
            Ok(h.AnswerSideShow("P1", true));
            Check(h.Ledger.Get("P2").Folded, "equal hands: asker packs");

            var g = Hand(50, new long[] { 2000, 2000, 2000 }, "Kh Kd 2s", "2c 7d 9h", "3c 8d Th");
            Ok(g.See("P0")); Ok(g.Chaal("P0", false));
            Ok(g.See("P1")); Ok(g.Chaal("P1", false));
            Ok(g.See("P2")); Ok(g.AskSideShow("P2"));
            Ok(g.AnswerSideShow("P1", false));
            Check(!g.Ledger.Get("P1").Folded && !g.Ledger.Get("P2").Folded, "declined: both still in");
            Eq("P0", g.OnTurn, "turn");
        });
        Test("teen patti: no side-show for a blind player or with 2 left", () =>
        {
            var h = Hand(50, new long[] { 2000, 2000, 2000 }, "Kh Kd 2s", "2c 7d 9h", "3c 8d Th");
            Ok(h.Chaal("P0", false)); Ok(h.Chaal("P1", false));
            Check(h.AskSideShow("P2") != null, "blind asker");
            var g = Hand(50, new long[] { 2000, 2000 }, "Kh Kd 2s", "2c 7d 9h");
            Ok(g.See("P0"));
            Check(g.AskSideShow("P0") != null, "two players");
        });
        Test("teen patti: after 4 blind chaals a player is seen", () =>
        {
            var h = Hand(10, new long[] { 5000, 5000 }, "Kh Kd 2s", "2c 7d 9h");
            for (int i = 0; i < 4; i++) { Ok(h.Chaal("P0", false)); Ok(h.Chaal("P1", false)); }
            Check(h.SeatOf("P0").Seen, "seen after 4 blinds");
            Eq(20L, h.ChaalCost("P0"), "now pays the seen rate");
        });
        Test("teen patti: the pot limit shows every hand", () =>
        {
            var h = Hand(10, new long[] { 100000, 100000, 100000 }, "Kh Kd 2s", "2c 7d 9h", "Ah Ad Ac");
            int guard = 0;
            while (h.State != TeenPattiHandEngine.Phase.Over && guard++ < 200) Ok(h.Chaal(h.OnTurn, h.CanRaise));
            Eq("pot_limit", h.Reason, "reason");
            Check(h.Ledger.Pot >= 10 * 2048, "pot reached the limit");
            Check(h.Result.Players.Single(p => p.user_id == "P2").won > 0, "aces win");
            Check(h.Stake <= 10 * 128, "stake capped");
        });
        Test("teen patti: a short stack goes all-in and can still win only its share", () =>
        {
            var h = Hand(50, new long[] { 120, 2000, 2000 }, "Ah Ad Ac", "Kh Kd 2s", "2c 7d 9h");
            Ok(h.See("P0")); Ok(h.Chaal("P0", false));   // seen 100, has 70 → all-in for 70
            Check(h.Ledger.Get("P0").AllIn, "P0 all-in");
            Ok(h.See("P1")); Ok(h.Chaal("P1", true));     // stake 100, seen pays 200
            Ok(h.Pack("P2"));
            // Only P1 can still act, P0 is all-in: showdown.
            Eq(TeenPattiHandEngine.Phase.Over, h.State, "over");
            var r = h.Result;
            Eq(r.Pot, r.Players.Sum(p => p.won) + r.Commission, "invariant");
            var p0 = r.Players.Single(p => p.user_id == "P0");
            Check(p0.won > 0 && p0.won < r.Pot, "P0 wins the main pot only");
            Check(r.Players.Single(p => p.user_id == "P1").stack_after > 2000 - 250, "P1 gets the unmatched part back");
        });
        Test("teen patti: timeout packs the player on turn", () =>
        {
            var h = Hand(50, new long[] { 2000, 2000, 2000 }, "Ah Ad Ac", "2c 7d 9h", "3c 8d Th");
            Ok(h.Timeout("P0"));
            Check(h.Ledger.Get("P0").Folded, "packed");
            Eq("P1", h.OnTurn, "next");
            Ok(h.Timeout("P1"));
            Eq("timeout", h.Reason, "reason");
        });
    }
}
