using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace GamesPanda.Tables
{
    /// <summary>A playing card: rank 2..14 (14 = ace), suit 0..3. Index = suit * 13 + (rank - 2), 0..51.</summary>
    [Serializable]
    public struct Card : IEquatable<Card>
    {
        public int Rank;
        public int Suit;

        public Card(int rank, int suit)
        {
            if (rank < 2 || rank > 14) throw new ArgumentOutOfRangeException(nameof(rank));
            if (suit < 0 || suit > 3) throw new ArgumentOutOfRangeException(nameof(suit));
            Rank = rank;
            Suit = suit;
        }

        public int Index => Suit * 13 + (Rank - 2);

        public static Card FromIndex(int index)
        {
            if (index < 0 || index > 51) throw new ArgumentOutOfRangeException(nameof(index));
            return new Card(index % 13 + 2, index / 13);
        }

        public bool Equals(Card other) => Rank == other.Rank && Suit == other.Suit;
        public override bool Equals(object obj) => obj is Card c && Equals(c);
        public override int GetHashCode() => Index;

        static readonly string Ranks = "23456789TJQKA", Suits = "cdhs";
        /// <summary>"Ah", "Td", "2c" - the form the backend history keeps for Poker boards.</summary>
        public override string ToString() => $"{Ranks[Rank - 2]}{Suits[Suit]}";
    }

    /// <summary>
    /// A 52-card deck shuffled with a cryptographic generator and an unbiased Fisher-Yates (every position equally
    /// likely; the old Poker shuffle could never leave a card where it was). Lives only on the match server.
    /// </summary>
    public sealed class Deck
    {
        readonly int[] order = new int[52];
        int next;

        public Deck(Func<int, int> randomBelow = null)
        {
            var rnd = randomBelow ?? SecureRandomBelow;
            for (int i = 0; i < 52; i++) order[i] = i;
            for (int i = 51; i > 0; i--)
            {
                int j = rnd(i + 1);
                (order[i], order[j]) = (order[j], order[i]);
            }
        }

        /// <summary>A stacked deck for tests: the cards come out in this order.</summary>
        public static Deck Stacked(IEnumerable<Card> top)
        {
            var d = new Deck(n => n - 1);   // any order; overwritten below
            var used = new HashSet<int>();
            int k = 0;
            foreach (var c in top)
            {
                if (!used.Add(c.Index)) throw new ArgumentException("The same card twice in a stacked deck: " + c);
                d.order[k++] = c.Index;
            }
            for (int i = 0; i < 52; i++) if (!used.Contains(i)) d.order[k++] = i;
            return d;
        }

        public int Left => 52 - next;

        public Card Draw()
        {
            if (next >= 52) throw new InvalidOperationException("The deck is empty");
            return Card.FromIndex(order[next++]);
        }

        public static int SecureRandomBelow(int n) => RandomNumberGenerator.GetInt32(n);
    }
}
