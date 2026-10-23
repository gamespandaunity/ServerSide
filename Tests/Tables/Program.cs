using System;
using System.Collections.Generic;
using System.Linq;
using GamesPanda.Tables;

static partial class Program
{
    static int passed, failed;

    static void Check(bool ok, string detail)
    {
        if (!ok) throw new Exception(detail);
    }

    static void Eq<T>(T expected, T actual, string what)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new Exception($"{what}: expected {expected}, got {actual}");
    }

    static void Test(string name, Action body)
    {
        try { body(); passed++; Console.WriteLine("PASS " + name); }
        catch (Exception e) { failed++; Console.WriteLine("FAIL " + name + " — " + e.Message); }
    }

    /// <summary>"Ah Kd 2c" → cards.</summary>
    static Card[] C(string s) => s.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(Parse).ToArray();

    static Card Parse(string t)
    {
        int rank = "23456789TJQKA".IndexOf(t[0]) + 2;
        int suit = "cdhs".IndexOf(t[1]);
        return new Card(rank, suit);
    }

    static int Main()
    {
        CardsAndDeck();
        TeenPattiRanking();
        Ledger();
        TeenPattiHands();
        PokerHands();
        Seats();
        Runner();
        Console.WriteLine($"\n{passed} passed, {failed} failed");
        return failed == 0 ? 0 : 1;
    }

    static void CardsAndDeck()
    {
        Test("card index round trip", () =>
        {
            for (int i = 0; i < 52; i++) Eq(i, Card.FromIndex(i).Index, "index");
            Eq("Ah", Parse("Ah").ToString(), "text");
        });
        Test("deck deals 52 different cards", () =>
        {
            var d = new Deck();
            var seen = new HashSet<int>();
            for (int i = 0; i < 52; i++) Check(seen.Add(d.Draw().Index), "duplicate card");
            Eq(0, d.Left, "left");
        });
        Test("shuffle is unbiased (every card can stay in place)", () =>
        {
            // Over many shuffles each card is first about 1/52 of the time; the old Sattolo shuffle never left the
            // last card last.
            var first = new int[52];
            var lastStays = 0;
            const int n = 52000;
            for (int k = 0; k < n; k++)
            {
                var d = new Deck();
                first[d.Draw().Index]++;
                for (int i = 1; i < 51; i++) d.Draw();
                if (d.Draw().Index == 51) lastStays++;
            }
            Check(first.All(c => c > 700 && c < 1300), "first-card counts are uneven: " + string.Join(",", first));
            Check(lastStays > 700 && lastStays < 1300, "last card stayed in place " + lastStays + " times");
        });
    }

    static long S(string hand) => TeenPattiHand.Score(C(hand));

    static void TeenPattiRanking()
    {
        Test("category order: trail > pure sequence > sequence > colour > pair > high card", () =>
        {
            var order = new[] { "2c 2d 2h", "4h 5h 6h", "4h 5d 6h", "2s 7s 9s", "Ac Ad Kh", "Ah Kd Jc" };
            for (int i = 0; i + 1 < order.Length; i++)
                Check(S(order[i]) > S(order[i + 1]), $"{order[i]} should beat {order[i + 1]}");
        });
        Test("sequences: A-K-Q > A-2-3 > K-Q-J > 4-3-2", () =>
        {
            Check(S("Ac Kd Qh") > S("Ac 2d 3h"), "AKQ > A23");
            Check(S("Ac 2d 3h") > S("Kc Qd Jh"), "A23 > KQJ");
            Check(S("Kc Qd Jh") > S("4c 3d 2h"), "KQJ > 432");
            Check(S("Qc Kd Ah") == S("Ad Kh Qs"), "order of cards does not matter");
            Check(S("Ah Kh Qh") > S("Ac 2c 3c"), "pure AKQ > pure A23");
        });
        Test("pair: the pair first, then the third card (A-2-2 < K-K-3)", () =>
        {
            Check(S("Ah 2c 2d") < S("Kh Kc 3d"), "A22 < KK3");
            Check(S("Kh Kc 4d") > S("Ks Kd 3c"), "KK4 > KK3");
        });
        Test("trail of aces is the best hand; equal hands tie whatever the suits", () =>
        {
            Check(S("Ac Ad Ah") > S("Kc Kd Kh"), "AAA > KKK");
            Eq(S("Ah Kd 9c"), S("As Kc 9d"), "same ranks tie");
        });
        Test("high card compares all three cards", () =>
        {
            Check(S("Ah Kd 9c") > S("Ah Kd 8c"), "AK9 > AK8");
            Check(S("Ah Kd 9c") > S("Ah Qd Jc"), "AK9 > AQJ");
        });
    }

    static void Ledger()
    {
        Test("backend worked example: pot 950, commission 47, stacks 2503 / 1600 / 1850", () =>
        {
            var l = new HandLedger(5);
            l.Join("A", 0, 2000); l.Join("B", 1, 2000); l.Join("C", 3, 2000);
            l.Put("A", 400); l.Put("B", 400); l.Put("C", 150);
            l.Fold("C");
            var r = l.Settle((a, b) => a == "A" ? 1 : b == "A" ? -1 : 0);
            Eq(950L, r.Pot, "pot"); Eq(47L, r.Commission, "commission");
            Eq(903L, r.Players.Single(p => p.user_id == "A").won, "A won");
            Eq("2503,1600,1850", string.Join(",", r.Players.Select(p => p.stack_after)), "stacks");
            Eq(r.Players.Sum(p => p.contributed), r.Players.Sum(p => p.won) + r.Commission, "invariant");
        });
        Test("silver table (0 %): no commission", () =>
        {
            var l = new HandLedger(0);
            l.Join("A", 0, 500); l.Join("B", 1, 500);
            l.Put("A", 100); l.Put("B", 100); l.Fold("B");
            var r = l.Settle((a, b) => 0);
            Eq(0L, r.Commission, "commission"); Eq(200L, r.Players[0].won, "A won");
        });
        Test("commission is rounded down", () =>
        {
            var l = new HandLedger(5);
            l.Join("A", 0, 1000); l.Join("B", 1, 1000);
            l.Put("A", 19); l.Put("B", 19);   // pot 38 → 1.9 → 1
            l.Fold("B");
            Eq(1L, l.Settle((a, b) => 0).Commission, "commission");
        });
        Test("split pot: equal hands share, the odd chip goes to the first in order", () =>
        {
            var l = new HandLedger(0);
            l.Join("A", 0, 100); l.Join("B", 1, 100); l.Join("C", 2, 100);
            l.Put("A", 51); l.Put("B", 51); l.Put("C", 1); l.Fold("C");   // pot 103
            var r = l.Settle((a, b) => 0, new[] { "B", "A" });
            Eq(52L, r.Players.Single(p => p.user_id == "B").won, "B gets the odd chip");
            Eq(51L, r.Players.Single(p => p.user_id == "A").won, "A");
        });
        Test("side pots: a short all-in wins only what it could match", () =>
        {
            var l = new HandLedger(0);
            l.Join("A", 0, 100); l.Join("B", 1, 1000); l.Join("C", 2, 1000);
            l.Put("A", 100); l.Put("B", 300); l.Put("C", 300);
            // A best, then B, then C.
            int Rank(string x) => x == "A" ? 3 : x == "B" ? 2 : 1;
            var r = l.Settle((a, b) => Rank(a).CompareTo(Rank(b)));
            Eq(300L, r.Players.Single(p => p.user_id == "A").won, "A wins the main pot 3 x 100");
            Eq(400L, r.Players.Single(p => p.user_id == "B").won, "B wins the side pot 2 x 200");
            Eq(0L, r.Players.Single(p => p.user_id == "C").won, "C");
        });
        Test("side pots with commission still add up", () =>
        {
            var l = new HandLedger(5);
            l.Join("A", 0, 137); l.Join("B", 1, 1000); l.Join("C", 2, 1000);
            l.Put("A", 137); l.Put("B", 333); l.Put("C", 333);
            int Rank(string x) => x == "A" ? 3 : x == "B" ? 2 : 1;
            var r = l.Settle((a, b) => Rank(a).CompareTo(Rank(b)));
            Eq(803L * 5 / 100, r.Commission, "commission on the whole pot");
            Eq(r.Pot, r.Players.Sum(p => p.won) + r.Commission, "invariant");
            foreach (var p in r.Players) Eq(l.Get(p.user_id).StackBefore - p.contributed + p.won, p.stack_after, "stack_after " + p.user_id);
        });
        Test("an uncalled bet goes back before the pot is built", () =>
        {
            var l = new HandLedger(5);
            l.Join("A", 0, 1000); l.Join("B", 1, 60);
            l.Put("A", 500); l.Put("B", 60);   // B all-in for 60; 440 of A's 500 was never matched
            var r = l.Settle((a, b) => b == "A" ? 1 : -1);   // B wins
            Eq(60L, r.Players.Single(p => p.user_id == "A").contributed, "A's contribution after the refund");
            Eq(120L, r.Pot, "pot"); Eq(6L, r.Commission, "commission");
            Eq(114L, r.Players.Single(p => p.user_id == "B").won, "B");
            Eq(940L, r.Players.Single(p => p.user_id == "A").stack_after, "A keeps the rest");
        });
    }
}
