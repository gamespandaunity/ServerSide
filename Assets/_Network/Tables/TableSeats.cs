using System;
using System.Collections.Generic;
using System.Linq;

namespace GamesPanda.Tables
{
    /// <summary>
    /// Who sits where at a Teen Patti 5 / Poker 6 table, between hands. The backend decides the seat and the stack when
    /// a player sits (the join ticket says both); this keeps them for the server, picks who plays the next hand and who
    /// has to be stood up:
    /// - a player plays a hand when seated, connected and holding at least <see cref="minStackToPlay"/>;
    /// - away (disconnected) for <see cref="AwayHandsLimit"/> hands in a row or <see cref="AwayLimit"/> → stood up, reason "away";
    /// - below the minimum for <see cref="ShortHandsLimit"/> hands in a row (no top-up) → stood up, reason "busted";
    /// - asked to leave → stood up after the hand they are in (reason "left").
    /// Standing up = the server calls PATCH /casinos/tables/{tx}/leave with the stack; <see cref="Remove"/> once it is done.
    /// </summary>
    public sealed class TableSeats
    {
        public enum State { Seated, Leaving }

        public sealed class Seat
        {
            public int Number;
            public string UserId;
            public string Name;
            public long Stack;
            public State State;
            public bool Connected;
            public bool InHand;
            public int AwayHands;
            public int ShortHands;
            public DateTime? AwaySince;
        }

        public const int AwayHandsLimit = 2, ShortHandsLimit = 2;
        public static readonly TimeSpan AwayLimit = TimeSpan.FromSeconds(90);

        readonly Seat[] seats;
        readonly long minStackToPlay;

        public TableSeats(int maxSeats, long minStackToPlay)
        {
            if (maxSeats < 2 || maxSeats > 9) throw new ArgumentOutOfRangeException(nameof(maxSeats));
            seats = new Seat[maxSeats];
            this.minStackToPlay = minStackToPlay;
        }

        public int MaxSeats => seats.Length;
        public IEnumerable<Seat> Occupied => seats.Where(s => s != null);
        public Seat Find(string userId) => seats.FirstOrDefault(s => s != null && s.UserId == userId);
        public Seat At(int seat) => seat >= 0 && seat < seats.Length ? seats[seat] : null;

        /// <summary>
        /// A verified ticket: the user sits in the backend's seat with the backend's stack. A player already seated (a
        /// reconnect, or a top-up ticket) keeps their seat; the stack is only taken from the ticket between hands.
        /// Returns why it was refused, or null.
        /// </summary>
        public string Sit(string userId, string name, int seat, long stack, DateTime now)
        {
            var mine = Find(userId);
            if (mine != null)
            {
                mine.Connected = true;
                mine.AwaySince = null;
                if (!string.IsNullOrEmpty(name)) mine.Name = name;
                if (!mine.InHand && stack > 0) mine.Stack = stack;
                return null;
            }
            if (seat < 0 || seat >= seats.Length) return $"seat {seat} does not exist";
            if (seats[seat] != null) return $"seat {seat} is taken by {seats[seat].UserId}";
            seats[seat] = new Seat { Number = seat, UserId = userId, Name = name, Stack = stack, State = State.Seated, Connected = true };
            return null;
        }

        public void SetConnected(string userId, bool connected, DateTime now)
        {
            var s = Find(userId);
            if (s == null) return;
            s.Connected = connected;
            s.AwaySince = connected ? (DateTime?)null : s.AwaySince ?? now;
            if (connected) s.AwayHands = 0;
        }

        /// <summary>The player asked to leave. True when they can go now (not in a hand); otherwise they leave after it.</summary>
        public bool RequestLeave(string userId)
        {
            var s = Find(userId);
            if (s == null) return false;
            s.State = State.Leaving;
            return !s.InHand;
        }

        public void Remove(string userId)
        {
            var s = Find(userId);
            if (s != null) seats[s.Number] = null;
        }

        bool CanPlay(Seat s) => s != null && s.State == State.Seated && s.Connected && s.Stack >= minStackToPlay;

        /// <summary>The dealer (button) of the next hand: the next seat after <paramref name="previous"/> that will play.</summary>
        public int NextDealer(int previous)
        {
            for (int k = 1; k <= seats.Length; k++)
            {
                int i = ((previous + k) % seats.Length + seats.Length) % seats.Length;
                if (CanPlay(seats[i])) return i;
            }
            return -1;
        }

        /// <summary>
        /// Players of the next hand in seat order, starting with the dealer (Poker: the button) when
        /// <paramref name="fromDealer"/>, else with the first seat after the dealer (Teen Patti). Fewer than 2 = no hand.
        /// </summary>
        public List<Seat> NextHand(int dealer, bool fromDealer)
        {
            var list = new List<Seat>();
            int start = fromDealer ? 0 : 1;
            for (int k = start; k < start + seats.Length; k++)
            {
                var s = seats[(dealer + k) % seats.Length];
                if (CanPlay(s)) list.Add(s);
            }
            return list;
        }

        public void HandStarted(IEnumerable<string> players)
        {
            foreach (var id in players) { var s = Find(id); if (s != null) s.InHand = true; }
        }

        /// <summary>
        /// After a hand (or between hands, when no hand can start): stacks from the backend's answer, then who must go.
        /// Returns the players to stand up with their reason (left | away | busted).
        /// </summary>
        public List<(string userId, long stack, string reason)> HandFinished(IDictionary<string, long> stacks, DateTime now, bool aHandWasPlayed = true)
        {
            if (stacks != null)
                foreach (var kv in stacks) { var s = Find(kv.Key); if (s != null) s.Stack = kv.Value; }
            var go = new List<(string, long, string)>();
            foreach (var s in Occupied.ToList())
            {
                s.InHand = false;
                if (s.State == State.Leaving) { go.Add((s.UserId, s.Stack, "left")); continue; }
                if (!s.Connected)
                {
                    if (aHandWasPlayed) s.AwayHands++;
                    if (s.AwayHands >= AwayHandsLimit || (s.AwaySince.HasValue && now - s.AwaySince.Value >= AwayLimit))
                    { go.Add((s.UserId, s.Stack, "away")); continue; }
                }
                if (s.Stack < minStackToPlay)
                {
                    if (aHandWasPlayed) s.ShortHands++;
                    if (s.ShortHands >= ShortHandsLimit) go.Add((s.UserId, s.Stack, "busted"));
                }
                else s.ShortHands = 0;
            }
            return go;
        }

        /// <summary>Everyone's stack, for the table close.</summary>
        public Dictionary<string, long> Stacks() => Occupied.ToDictionary(s => s.UserId, s => s.Stack);
    }
}
