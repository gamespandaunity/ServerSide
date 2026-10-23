using System;
using System.Linq;
using GamesPanda.Tables;

static partial class Program
{
    static readonly DateTime T0 = new DateTime(2026, 10, 23, 10, 0, 0, DateTimeKind.Utc);

    static void Seats()
    {
        Test("seats: backend seat and stack; a taken seat is refused; a reconnect keeps the seat", () =>
        {
            var t = new TableSeats(5, 50);
            Ok(t.Sit("A", "Ali", 0, 2000, T0));
            Check(t.Sit("B", "Bo", 0, 2000, T0) != null, "seat 0 taken");
            Ok(t.Sit("B", "Bo", 3, 1500, T0));
            t.SetConnected("A", false, T0);
            Ok(t.Sit("A", "Ali", 4, 2500, T0));   // reconnect / top-up ticket between hands
            Eq(0, t.Find("A").Number, "A keeps seat 0");
            Eq(2500L, t.Find("A").Stack, "stack from the new ticket");
            Check(t.Find("A").Connected, "connected again");
        });
        Test("seats: next hand in seat order after the dealer; the dealer moves round", () =>
        {
            var t = new TableSeats(5, 50);
            Ok(t.Sit("A", "", 0, 1000, T0)); Ok(t.Sit("B", "", 2, 1000, T0)); Ok(t.Sit("C", "", 4, 1000, T0));
            int d = t.NextDealer(-1);
            Eq(0, d, "first dealer");
            Eq("B,C,A", string.Join(",", t.NextHand(d, false).Select(s => s.UserId)), "teen patti order");
            Eq("A,B,C", string.Join(",", t.NextHand(d, true).Select(s => s.UserId)), "poker order from the button");
            Eq(2, t.NextDealer(d), "next dealer");
            Eq(0, t.NextDealer(4), "wraps round");
        });
        Test("seats: a disconnected or short player is left out of the next hand", () =>
        {
            var t = new TableSeats(5, 50);
            Ok(t.Sit("A", "", 0, 1000, T0)); Ok(t.Sit("B", "", 1, 40, T0)); Ok(t.Sit("C", "", 2, 1000, T0));
            t.SetConnected("C", false, T0);
            Eq("A", string.Join(",", t.NextHand(0, true).Select(s => s.UserId)), "only A can play");
        });
        Test("seats: leave during a hand waits for the end of it", () =>
        {
            var t = new TableSeats(5, 50);
            Ok(t.Sit("A", "", 0, 1000, T0)); Ok(t.Sit("B", "", 1, 1000, T0));
            t.HandStarted(new[] { "A", "B" });
            Check(!t.RequestLeave("B"), "B is in a hand");
            var go = t.HandFinished(new System.Collections.Generic.Dictionary<string, long> { ["A"] = 1100, ["B"] = 895 }, T0);
            Eq("B:895:left", string.Join(",", go.Select(g => $"{g.userId}:{g.stack}:{g.reason}")), "B leaves with the new stack");
            Eq(1100L, t.Find("A").Stack, "A's stack updated");
        });
        Test("seats: away for 2 hands, or 90 s, is stood up", () =>
        {
            var t = new TableSeats(5, 50);
            Ok(t.Sit("A", "", 0, 1000, T0)); Ok(t.Sit("B", "", 1, 1000, T0)); Ok(t.Sit("C", "", 2, 1000, T0));
            t.SetConnected("C", false, T0);
            Eq(0, t.HandFinished(null, T0.AddSeconds(30)).Count, "first missed hand");
            Eq("C:away", string.Join(",", t.HandFinished(null, T0.AddSeconds(60)).Select(g => g.userId + ":" + g.reason)), "second missed hand");
            var u = new TableSeats(5, 50);
            Ok(u.Sit("A", "", 0, 1000, T0));
            u.SetConnected("A", false, T0);
            Eq("A:away", string.Join(",", u.HandFinished(null, T0.AddSeconds(91), aHandWasPlayed: false).Select(g => g.userId + ":" + g.reason)), "90 s away");
        });
        Test("seats: below the minimum for 2 hands without a top-up is busted", () =>
        {
            var t = new TableSeats(5, 50);
            Ok(t.Sit("A", "", 0, 1000, T0)); Ok(t.Sit("B", "", 1, 1000, T0));
            var after = new System.Collections.Generic.Dictionary<string, long> { ["A"] = 1980, ["B"] = 20 };
            Eq(0, t.HandFinished(after, T0).Count, "one short hand");
            Eq("B:20:busted", string.Join(",", t.HandFinished(null, T0).Select(g => $"{g.userId}:{g.stack}:{g.reason}")), "busted");
        });
    }
}
