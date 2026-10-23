using System;
using System.Collections.Generic;
using System.Linq;

namespace GamesPanda.Tables
{
    /// <summary>
    /// The money of one hand at a Teen Patti or Poker table, kept on the match server. Players put chips from their
    /// stack into the pot; at the end the pot (less the commission) goes to the winners, side pot by side pot. The
    /// result is exactly what POST /casinos/handle-rounds checks ("Teen Patti and Poker Tables", backend, 22 Oct 2026):
    /// Σ contributed = Σ won + commission, commission = floor(pct% of the pot) (0 on silver), and every
    /// stack_after = stack before - contributed + won.
    /// </summary>
    public sealed class HandLedger
    {
        public sealed class Player
        {
            public string UserId;
            public int Seat;
            public long StackBefore;
            public long Contributed;
            public bool Folded;
            public long Stack => StackBefore - Contributed;
            public bool AllIn => !Folded && Stack == 0;
        }

        public sealed class Row
        {
            public string user_id;
            public int seat;
            public long contributed;
            public long won;
            public long stack_after;
        }

        public sealed class Result
        {
            public List<Row> Players = new List<Row>();
            public long Pot;
            public long Commission;
            /// <summary>Players who won something, best first (for the result screen).</summary>
            public List<string> Winners = new List<string>();
        }

        readonly List<Player> players = new List<Player>();
        readonly int commissionPct;
        readonly long? commissionCap;

        public HandLedger(int commissionPct, long? commissionCap = null)
        {
            if (commissionPct < 0 || commissionPct > 100) throw new ArgumentOutOfRangeException(nameof(commissionPct));
            this.commissionPct = commissionPct;
            this.commissionCap = commissionCap;
        }

        public IReadOnlyList<Player> Players => players;
        public long Pot => players.Sum(p => p.Contributed);

        public Player Join(string userId, int seat, long stack)
        {
            if (string.IsNullOrEmpty(userId)) throw new ArgumentException("user id");
            if (stack < 0) throw new ArgumentOutOfRangeException(nameof(stack));
            if (players.Any(p => p.UserId == userId || p.Seat == seat)) throw new InvalidOperationException($"{userId} / seat {seat} is already in this hand");
            var p = new Player { UserId = userId, Seat = seat, StackBefore = stack };
            players.Add(p);
            return p;
        }

        public Player Get(string userId) => players.FirstOrDefault(p => p.UserId == userId)
                                            ?? throw new InvalidOperationException($"{userId} is not in this hand");

        /// <summary>Moves chips from the player's stack to the pot; never more than the stack (the rest is an all-in).
        /// Returns what was actually put in.</summary>
        public long Put(string userId, long amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            var p = Get(userId);
            if (p.Folded) throw new InvalidOperationException($"{userId} has folded");
            long paid = Math.Min(amount, p.Stack);
            p.Contributed += paid;
            return paid;
        }

        public void Fold(string userId) => Get(userId).Folded = true;

        public IEnumerable<Player> Live => players.Where(p => !p.Folded);

        public long CommissionFor(long pot)
        {
            long c = pot * commissionPct / 100;      // rounded down
            if (commissionCap.HasValue && c > commissionCap.Value) c = commissionCap.Value;
            return c;
        }

        /// <summary>
        /// Pays the pot. <paramref name="compare"/>(a, b) &gt; 0 when a's hand beats b's (only called for live players at a
        /// showdown). <paramref name="oddChipOrder"/> lists user ids in the order odd chips of a split go to (Poker: first
        /// seat left of the button; Teen Patti: seat order from the dealer); players not listed come last.
        /// </summary>
        public Result Settle(Func<string, string, int> compare, IList<string> oddChipOrder = null)
        {
            var live = Live.ToList();
            if (live.Count == 0) throw new InvalidOperationException("Nobody is left in the hand");

            ReturnUncalled();

            long pot = Pot;
            long commission = CommissionFor(pot);
            var won = players.ToDictionary(p => p.UserId, p => 0L);
            var result = new Result { Pot = pot, Commission = commission };

            int Order(string id)
            {
                int i = oddChipOrder?.IndexOf(id) ?? -1;
                return i < 0 ? int.MaxValue : i;
            }

            if (live.Count == 1)
            {
                won[live[0].UserId] = pot - commission;
                result.Winners.Add(live[0].UserId);
            }
            else
            {
                // Side pots: one layer per distinct contribution level of a live player.
                var levels = live.Select(p => p.Contributed).Where(x => x > 0).Distinct().OrderBy(x => x).ToList();
                var pots = new List<(long amount, List<Player> eligible)>();
                long below = 0;
                foreach (long level in levels)
                {
                    long amount = players.Sum(p => Math.Max(0, Math.Min(p.Contributed, level) - below));
                    var eligible = live.Where(p => p.Contributed >= level).ToList();
                    if (amount > 0) pots.Add((amount, eligible));
                    below = level;
                }
                // Chips above the highest live level (a folded player's bet nobody live matched) join the top pot.
                long rest = pot - pots.Sum(x => x.amount);
                if (rest > 0)
                {
                    if (pots.Count == 0) pots.Add((rest, live));
                    else pots[pots.Count - 1] = (pots[pots.Count - 1].amount + rest, pots[pots.Count - 1].eligible);
                }

                // Commission taken from the pots in proportion; the rounding remainder from the main pot.
                var net = pots.Select(x => x.amount - x.amount * commission / Math.Max(1, pot)).ToList();
                long taken = pots.Sum(x => x.amount) - net.Sum();
                net[0] -= commission - taken;

                for (int i = 0; i < pots.Count; i++)
                {
                    var best = new List<Player> { pots[i].eligible[0] };
                    foreach (var p in pots[i].eligible.Skip(1))
                    {
                        int c = compare(p.UserId, best[0].UserId);
                        if (c > 0) best = new List<Player> { p };
                        else if (c == 0) best.Add(p);
                    }
                    best = best.OrderBy(p => Order(p.UserId)).ThenBy(p => p.Seat).ToList();
                    long share = net[i] / best.Count, odd = net[i] % best.Count;
                    for (int k = 0; k < best.Count; k++) won[best[k].UserId] += share + (k < odd ? 1 : 0);
                }
                result.Winners = won.Where(x => x.Value > 0).OrderByDescending(x => x.Value).Select(x => x.Key).ToList();
            }

            foreach (var p in players.OrderBy(p => p.Seat))
                result.Players.Add(new Row
                {
                    user_id = p.UserId, seat = p.Seat, contributed = p.Contributed, won = won[p.UserId],
                    stack_after = p.StackBefore - p.Contributed + won[p.UserId],
                });

            long totalWon = result.Players.Sum(r => r.won);
            if (result.Players.Sum(r => r.contributed) != totalWon + commission)
                throw new InvalidOperationException($"Hand does not add up: contributed {pot}, won {totalWon}, commission {commission}");
            return result;
        }

        /// <summary>The part of the biggest bet that nobody else put in goes back to its owner before the pots are built.</summary>
        void ReturnUncalled()
        {
            var bySize = players.OrderByDescending(p => p.Contributed).ToList();
            if (bySize.Count < 2 || bySize[0].Folded) return;
            long extra = bySize[0].Contributed - bySize[1].Contributed;
            if (extra > 0) bySize[0].Contributed -= extra;
        }
    }
}
