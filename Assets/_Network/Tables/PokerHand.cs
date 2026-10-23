using System;
using System.Collections.Generic;
using System.Linq;

namespace GamesPanda.Tables
{
    /// <summary>
    /// Texas Hold'em hand strength: the best five of the seven cards (two hole cards + five on the board). Ties are
    /// broken by the five cards that make the hand, never by unused hole cards (the old PokerPlayerComparer did that).
    /// </summary>
    public static class PokerHand
    {
        public enum Category { HighCard = 1, Pair, TwoPair, Trips, Straight, Flush, FullHouse, Quads, StraightFlush }

        /// <summary>Best score of any 5 of the given 5-7 cards; higher wins, equal is a split.</summary>
        public static long Best(IList<Card> cards) => Best(cards, out _, out _);

        public static long Best(IList<Card> cards, out Category category, out Card[] five)
        {
            if (cards == null || cards.Count < 5 || cards.Count > 7) throw new ArgumentException("5 to 7 cards");
            long best = -1;
            category = Category.HighCard;
            five = null;
            int n = cards.Count;
            var pick = new Card[5];
            for (int a = 0; a < n; a++)
            for (int b = a + 1; b < n; b++)
            for (int c = b + 1; c < n; c++)
            for (int d = c + 1; d < n; d++)
            for (int e = d + 1; e < n; e++)
            {
                pick[0] = cards[a]; pick[1] = cards[b]; pick[2] = cards[c]; pick[3] = cards[d]; pick[4] = cards[e];
                long s = Five(pick, out var cat);
                if (s > best) { best = s; category = cat; five = (Card[])pick.Clone(); }
            }
            return best;
        }

        /// <summary>Score of exactly five cards: category, then up to five ranks in order of importance (base 15).</summary>
        public static long Five(Card[] five, out Category category)
        {
            var ranks = five.Select(c => c.Rank).OrderByDescending(r => r).ToArray();
            bool flush = five.All(c => c.Suit == five[0].Suit);
            int straightHigh = StraightHigh(ranks);

            // Ranks grouped by count, then by rank: e.g. full house K-K-K-4-4 → [K, 4].
            var groups = ranks.GroupBy(r => r).OrderByDescending(g => g.Count()).ThenByDescending(g => g.Key)
                              .Select(g => (rank: g.Key, count: g.Count())).ToList();

            if (straightHigh > 0 && flush) { category = Category.StraightFlush; return Pack(category, straightHigh); }
            if (groups[0].count == 4) { category = Category.Quads; return Pack(category, groups[0].rank, groups[1].rank); }
            if (groups[0].count == 3 && groups[1].count == 2) { category = Category.FullHouse; return Pack(category, groups[0].rank, groups[1].rank); }
            if (flush) { category = Category.Flush; return Pack(category, ranks); }
            if (straightHigh > 0) { category = Category.Straight; return Pack(category, straightHigh); }
            if (groups[0].count == 3) { category = Category.Trips; return Pack(category, groups.Select(g => g.rank).ToArray()); }
            if (groups[0].count == 2 && groups[1].count == 2) { category = Category.TwoPair; return Pack(category, groups.Select(g => g.rank).ToArray()); }
            if (groups[0].count == 2) { category = Category.Pair; return Pack(category, groups.Select(g => g.rank).ToArray()); }
            category = Category.HighCard;
            return Pack(category, ranks);
        }

        /// <summary>High card of a straight (5 for A-2-3-4-5), 0 when not a straight. Ranks high to low.</summary>
        static int StraightHigh(int[] high)
        {
            if (high.Distinct().Count() != 5) return 0;
            if (high[0] - high[4] == 4) return high[0];
            if (high[0] == 14 && high[1] == 5 && high[4] == 2) return 5;   // the wheel
            return 0;
        }

        static long Pack(Category c, params int[] ranks)
        {
            long v = (long)c;
            for (int i = 0; i < 5; i++) v = v * 15 + (i < ranks.Length ? ranks[i] : 0);
            return v;
        }

        public static string Describe(Card[] five, Category category) =>
            category + " " + string.Join(" ", five.Select(c => c.ToString()));
    }
}
