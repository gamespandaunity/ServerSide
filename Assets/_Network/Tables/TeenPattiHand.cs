using System;
using System.Linq;

namespace GamesPanda.Tables
{
    /// <summary>
    /// Teen Patti hand strength, the same order as the 2-player game (PlayerCardsRankAndScoreCalc):
    /// trail &gt; pure sequence &gt; sequence &gt; colour &gt; pair &gt; high card. A-K-Q is the best sequence, A-2-3 the second
    /// best, then K-Q-J down to 4-3-2. One difference: a pair is compared by the pair first, then the third card (the
    /// old code compared the three cards high to low, so A-2-2 beat K-K-3).
    /// </summary>
    public static class TeenPattiHand
    {
        public enum Category { HighCard = 1, Pair = 2, Colour = 3, Sequence = 4, PureSequence = 5, Trail = 6 }

        /// <summary>A comparable strength: higher wins, equal is an exact tie (suits never break a tie).</summary>
        public static long Score(Card a, Card b, Card c) => Evaluate(a, b, c, out _);

        public static long Evaluate(Card a, Card b, Card c, out Category category)
        {
            int[] r = { a.Rank, b.Rank, c.Rank };
            Array.Sort(r);
            Array.Reverse(r);                       // high to low
            bool flush = a.Suit == b.Suit && b.Suit == c.Suit;
            int seq = SequenceStrength(r);          // 0 = not a sequence

            if (r[0] == r[1] && r[1] == r[2]) { category = Category.Trail; return Pack(category, r[0], 0, 0); }
            if (seq > 0) { category = flush ? Category.PureSequence : Category.Sequence; return Pack(category, seq, 0, 0); }
            if (flush) { category = Category.Colour; return Pack(category, r[0], r[1], r[2]); }
            if (r[0] == r[1]) { category = Category.Pair; return Pack(category, r[0], r[2], 0); }
            if (r[1] == r[2]) { category = Category.Pair; return Pack(category, r[1], r[0], 0); }
            category = Category.HighCard;
            return Pack(category, r[0], r[1], r[2]);
        }

        /// <summary>A-K-Q = 15, A-2-3 = 14, K-Q-J = 13 ... 4-3-2 = 4; 0 when the ranks are not consecutive.</summary>
        static int SequenceStrength(int[] high)
        {
            if (high[0] == 14 && high[1] == 13 && high[2] == 12) return 15;
            if (high[0] == 14 && high[1] == 3 && high[2] == 2) return 14;
            if (high[0] == high[1] + 1 && high[1] == high[2] + 1) return high[0];
            return 0;
        }

        static long Pack(Category c, int k1, int k2, int k3) => (long)c * 1_000_000 + k1 * 10_000 + k2 * 100 + k3;

        public static long Score(Card[] hand)
        {
            if (hand == null || hand.Length != 3) throw new ArgumentException("A Teen Patti hand has 3 cards");
            return Score(hand[0], hand[1], hand[2]);
        }

        public static string Describe(Card[] hand)
        {
            Evaluate(hand[0], hand[1], hand[2], out var cat);
            return cat + " " + string.Join(" ", hand.Select(x => x.ToString()));
        }
    }
}
