using System;
using System.Collections.Generic;
using System.Linq;

namespace GamesPanda.Tables
{
    /// <summary>The backend calls a table makes (CasinoTableApi on the match server; a fake in tests). Every call answers
    /// through its callback; error = null means it worked.</summary>
    public interface ITableBank
    {
        void VerifyTicket(string ticket, Action<string, int, long, string> done);                 // user, seat, stack, error
        void OpenHand(int handNo, IList<string> players, long boot, Action<string, string> done); // round id, error
        void SettleHand(string roundId, int handNo, string reason, HandLedger.Result result, IList<Card> board,
                        IDictionary<string, Card[]> shown, Action<Dictionary<string, long>, Dictionary<string, long>, string> done); // stacks, expected, error
        void Leave(string userId, long stack, string reason, int attempt, Action<string> done);
        void Close(string reason, IDictionary<string, long> stacks, Action<string> done);
    }

    /// <summary>What the table loop sends out; the Mirror glue (TableServer) turns it into RPCs.</summary>
    public interface ITableOutput
    {
        void Broadcast(object message);
        void Send(string userId, object message);
        void SendToConnection(object connection, object message);
        void Log(string line);
    }

    public sealed class TableRules
    {
        public bool Poker;
        public int Seats;
        public long Boot, SmallBlind, BigBlind;
        public int CommissionPct;
        public long? CommissionCap;
        public DateTime CloseAtUtc = DateTime.MaxValue;
        public double TurnSeconds = 15, SideShowSeconds = 10, StartDelaySeconds = 4, ResultSeconds = 5, RetrySeconds = 5;
        public TimeSpan NoNewHandBefore = TimeSpan.FromMinutes(3);
        public long MinStack => Poker ? BigBlind : Boot;
    }

    /// <summary>
    /// The whole table on the match server, between hands and during them. Plain C#: the Mirror glue feeds it
    /// connections, phone messages and the clock (<see cref="Tick"/>); it answers through <see cref="ITableOutput"/> and
    /// moves money only through <see cref="ITableBank"/>.
    ///
    /// Loop: waiting → (2+ can play, not in the last 3 minutes) open the hand with the backend → deal → turns with a
    /// timer (out of time: Teen Patti packs, Poker checks or folds; an unanswered side-show is declined) → settle with the
    /// backend → stand up whoever has to go → result pause → waiting. At CloseAt (after a running hand) the table closes
    /// and everyone is paid. A stack the backend does not agree with closes the table at once.
    /// </summary>
    public sealed class TableRunner
    {
        public enum Phase { Waiting, Opening, Playing, Settling, Result, Closing, Closed }

        readonly TableRules rules;
        readonly ITableBank bank;
        readonly ITableOutput output;
        readonly Func<Deck> newDeck;
        public readonly TableSeats Seats;

        readonly Dictionary<object, string> userOfConnection = new Dictionary<object, string>();
        readonly Dictionary<string, int> leaveAttempts = new Dictionary<string, int>();
        readonly HashSet<string> leaving = new HashSet<string>();

        public Phase State { get; private set; } = Phase.Waiting;
        public IHandEngine Hand { get; private set; }
        public int HandNo { get; private set; }
        public int Dealer { get; private set; } = -1;
        DateTime now, nextHandAt = DateTime.MinValue, turnEndsAt, resultEndsAt;
        string turnOf;
        bool turnWasSideShow;
        int openFailures;
        string closeReason;
        bool closeWarned;
        bool dirty = true;

        public TableRunner(TableRules rules, ITableBank bank, ITableOutput output, Func<Deck> newDeck = null)
        {
            this.rules = rules;
            this.bank = bank;
            this.output = output;
            this.newDeck = newDeck ?? (() => new Deck());
            Seats = new TableSeats(rules.Seats, rules.MinStack);
        }

        // ── connections ─────────────────────────────────────────────────────────────────────────

        /// <summary>A phone sent its join ticket (sitting down, reconnecting, or after a top-up).</summary>
        public void Ticket(object connection, string ticket, string name)
        {
            if (State == Phase.Closed || State == Phase.Closing)
            {
                output.SendToConnection(connection, new TableProtocol.Private { t = "error", text = "table closed" });
                return;
            }
            bank.VerifyTicket(ticket, (user, seat, stack, error) =>
            {
                if (error != null || string.IsNullOrEmpty(user))
                {
                    output.Log($"ticket refused: {error}");
                    output.SendToConnection(connection, new TableProtocol.Private { t = "error", text = error ?? "ticket refused" });
                    return;
                }
                string why = Seats.Sit(user, name, seat, stack, now);
                if (why != null)
                {
                    output.Log($"{user} could not sit: {why}");
                    output.SendToConnection(connection, new TableProtocol.Private { t = "error", text = why });
                    return;
                }
                foreach (var old in userOfConnection.Where(x => x.Value == user).Select(x => x.Key).ToList()) userOfConnection.Remove(old);
                userOfConnection[connection] = user;
                var s = Seats.Find(user);
                output.Log($"{user} sits in seat {s.Number} with {s.Stack}");
                output.Send(user, new TableProtocol.Private { t = "you", seat = s.Number, user = user });
                var hole = Hand?.HoleOf(user);
                if (hole != null && !Hand.Over)
                    output.Send(user, new TableProtocol.Private { t = "hole", hand = Hand.HandNo, cards = hole.Select(c => c.Index).ToArray() });
                dirty = true;
            });
        }

        public void Disconnected(object connection)
        {
            if (!userOfConnection.TryGetValue(connection, out string user)) return;
            userOfConnection.Remove(connection);
            if (userOfConnection.ContainsValue(user)) return;   // already back on a new connection
            Seats.SetConnected(user, false, now);
            output.Log($"{user} disconnected");
            dirty = true;
        }

        public string UserOf(object connection) => userOfConnection.TryGetValue(connection, out string u) ? u : null;

        // ── phone messages ──────────────────────────────────────────────────────────────────────

        public void Message(object connection, TableProtocol.In msg)
        {
            if (msg == null) return;
            if (msg.t == "ticket") { Ticket(connection, msg.ticket, msg.name); return; }
            string user = UserOf(connection);
            if (user == null) { output.SendToConnection(connection, new TableProtocol.Private { t = "error", text = "not seated" }); return; }

            if (msg.t == "leave") { RequestLeave(user); return; }
            if (msg.t != "act") return;

            if (State != Phase.Playing || Hand == null || Hand.Over) { Refuse(user, "no hand is running"); return; }
            if (msg.hand != 0 && msg.hand != Hand.HandNo) { Refuse(user, "that hand is over"); return; }
            string why = Hand.Act(user, msg.a, msg.amount, msg.yes);
            if (why != null) { Refuse(user, why); return; }
            AfterHandChange(acted: true);
        }

        void Refuse(string user, string why)
        {
            output.Send(user, new TableProtocol.Private { t = "error", text = why });
        }

        void RequestLeave(string user)
        {
            bool now_ = Seats.RequestLeave(user);
            output.Log($"{user} asks to leave");
            if (now_) StandUp(user, Seats.Find(user).Stack, "left");
            else if (Hand != null && !Hand.Over)
            {
                Hand.Abandon(user);
                AfterHandChange();
            }
            dirty = true;
        }

        readonly Dictionary<string, DateTime> leaveRetryAt = new Dictionary<string, DateTime>();

        void StandUp(string user, long stack, string reason)
        {
            if (leaveRetryAt.TryGetValue(user, out DateTime retry) && now < retry) return;
            if (!leaving.Add(user)) return;
            int attempt = leaveAttempts.TryGetValue(user, out int a) ? a + 1 : 1;
            leaveAttempts[user] = attempt;
            bank.Leave(user, stack, reason, attempt, error =>
            {
                leaving.Remove(user);
                if (error != null && !error.Contains("not seated"))
                {
                    output.Log($"leave of {user} refused: {error}");
                    leaveRetryAt[user] = now.AddSeconds(10);   // asked again later (still in HandFinished's list)
                    return;
                }
                Seats.Remove(user);
                output.Log($"{user} stood up ({reason}, {stack})");
                output.Broadcast(new TableProtocol.Event { text = $"{user} left the table" });
                dirty = true;
            });
        }

        // ── clock ───────────────────────────────────────────────────────────────────────────────

        public void Tick(DateTime utcNow)
        {
            now = utcNow;
            switch (State)
            {
                case Phase.Waiting: TickWaiting(); break;
                case Phase.Playing:
                    if (turnOf != null && now >= turnEndsAt)
                    {
                        output.Log($"timer ran out for {turnOf}");
                        Hand.TimeOut();
                        AfterHandChange(acted: true);
                    }
                    break;
                case Phase.Result:
                    if (now >= resultEndsAt) { State = Phase.Waiting; Hand = null; dirty = true; }
                    break;
            }
            if (!closeWarned && rules.CloseAtUtc != DateTime.MaxValue && now >= rules.CloseAtUtc - rules.NoNewHandBefore)
            {
                closeWarned = true;
                output.Broadcast(new TableProtocol.Event { text = "The table closes soon: no new hands." });
                dirty = true;
            }
            if (dirty) { dirty = false; Publish(); }
        }

        void TickWaiting()
        {
            if (now >= rules.CloseAtUtc) { Close("time"); return; }
            // Players who were away too long while nobody played.
            foreach (var go in Seats.HandFinished(null, now, aHandWasPlayed: false)) StandUp(go.userId, go.stack, go.reason);

            int dealer = Seats.NextDealer(Dealer);
            int ready = dealer < 0 ? 0 : Seats.NextHand(dealer, true).Count;
            bool mayDeal = now < rules.CloseAtUtc - rules.NoNewHandBefore;
            if (ready < 2 || !mayDeal) { nextHandAt = DateTime.MinValue; return; }
            if (nextHandAt == DateTime.MinValue) { nextHandAt = now.AddSeconds(rules.StartDelaySeconds); dirty = true; return; }
            if (now < nextHandAt) return;
            OpenHand(dealer);
        }

        void OpenHand(int dealer)
        {
            var players = Seats.NextHand(dealer, rules.Poker);
            int handNo = HandNo + 1;
            State = Phase.Opening;
            dirty = true;
            bank.OpenHand(handNo, players.Select(p => p.UserId).ToList(), rules.Poker ? 0 : rules.Boot, (roundId, error) =>
            {
                if (error != null || string.IsNullOrEmpty(roundId))
                {
                    output.Log($"hand {handNo} could not open: {error}");
                    State = Phase.Waiting;
                    nextHandAt = now.AddSeconds(rules.RetrySeconds);
                    if (++openFailures >= 5) Close("error");
                    dirty = true;
                    return;
                }
                openFailures = 0;
                // Someone may have left while the backend answered.
                var still = players.Where(p => Seats.Find(p.UserId) != null && p.Connected).ToList();
                if (still.Count < 2) { State = Phase.Waiting; dirty = true; return; }
                HandNo = handNo;
                Dealer = dealer;
                Start(roundId, still);
            });
        }

        string roundId;

        void Start(string round, List<TableSeats.Seat> players)
        {
            roundId = round;
            var list = players.Select(p => (p.UserId, p.Number, p.Stack)).ToList();
            if (rules.Poker)
                Hand = new PokerAdapter(new PokerHandEngine(HandNo, new PokerHandEngine.Config
                {
                    SmallBlind = rules.SmallBlind, BigBlind = rules.BigBlind, CommissionPct = rules.CommissionPct, CommissionCap = rules.CommissionCap,
                }, list, newDeck()));
            else
                Hand = new TeenPattiAdapter(new TeenPattiHandEngine(HandNo,
                    TeenPattiHandEngine.Config.ForBoot(rules.Boot, rules.CommissionPct, rules.CommissionCap), list, newDeck()));
            Seats.HandStarted(players.Select(p => p.UserId));
            State = Phase.Playing;
            turnOf = null;
            foreach (var p in players)
                output.Send(p.UserId, new TableProtocol.Private { t = "hole", hand = HandNo, cards = Hand.HoleOf(p.UserId).Select(c => c.Index).ToArray() });
            AfterHandChange();
        }

        /// <param name="acted">The player on turn just did something (or ran out of time): their next turn, even right
        /// away (Poker: first to act on the new street), gets a full timer.</param>
        void AfterHandChange(bool acted = false)
        {
            foreach (var line in Hand.TakeLog())
            {
                output.Log(line);
                output.Broadcast(new TableProtocol.Event { text = line });
            }
            foreach (var (to, other, cards) in Hand.TakeReveals())
                output.Send(to, new TableProtocol.Private { t = "reveal", hand = Hand.HandNo, otherUser = other, otherCards = cards.Select(c => c.Index).ToArray() });

            if (Hand.Over) { Settle(); return; }
            string waiting = Hand.WaitingFor;
            if (acted || waiting != turnOf || Hand.IsSideShowAnswer != turnWasSideShow)
            {
                turnOf = waiting;
                turnWasSideShow = Hand.IsSideShowAnswer;
                double secs = turnWasSideShow ? rules.SideShowSeconds : rules.TurnSeconds;
                // A player who is not connected gets a short timer: nobody waits for an empty chair.
                var seat = Seats.Find(waiting);
                if (seat != null && !seat.Connected) secs = Math.Min(secs, 3);
                turnEndsAt = now.AddSeconds(secs);
            }
            dirty = true;
        }

        void Settle()
        {
            State = Phase.Settling;
            turnOf = null;
            var hand = Hand;
            var result = hand.Result;
            dirty = true;
            bank.SettleHand(roundId, hand.HandNo, hand.Reason, result, hand.Board, hand.Shown, (stacks, expected, error) =>
            {
                if (error != null)
                {
                    output.Log($"hand {hand.HandNo} NOT settled: {error}");
                    // A stack mismatch (nothing was saved) or no answer at all: close, so everyone is paid what the
                    // backend holds as of the last settled hand; this hand is cancelled.
                    Close("error");
                    return;
                }
                var final = stacks ?? result.Players.ToDictionary(p => p.user_id, p => p.stack_after);
                var msg = new TableProtocol.Result
                {
                    hand = hand.HandNo, reason = hand.Reason, pot = result.Pot, commission = result.Commission,
                    winners = result.Winners,
                    board = hand.Board.Select(c => c.Index).ToArray(),
                    shown = hand.Shown.ToDictionary(x => x.Key, x => x.Value.Select(c => c.Index).ToArray()),
                };
                foreach (var r in result.Players)
                {
                    msg.players.Add(new TableProtocol.ResultRow { user = r.user_id, seat = r.seat, contributed = r.contributed, won = r.won, stack = final.TryGetValue(r.user_id, out long st) ? st : r.stack_after });
                    if (hand.Shown.ContainsKey(r.user_id)) msg.hands[r.user_id] = hand.Describe(r.user_id);
                }
                output.Broadcast(msg);
                foreach (var go in Seats.HandFinished(final, now)) StandUp(go.userId, go.stack, go.reason);
                State = Phase.Result;
                resultEndsAt = now.AddSeconds(rules.ResultSeconds);
                nextHandAt = DateTime.MinValue;
                dirty = true;
            });
        }

        public void Close(string reason)
        {
            if (State == Phase.Closing || State == Phase.Closed) return;
            State = Phase.Closing;
            closeReason = reason;
            dirty = true;
            var stacks = Seats.Stacks();
            output.Log($"closing the table ({reason}): " + string.Join(", ", stacks.Select(x => $"{x.Key} {x.Value}")));
            bank.Close(reason, stacks, error =>
            {
                if (error != null) output.Log($"close refused: {error} (the backend pays the last settled stacks itself)");
                State = Phase.Closed;
                output.Broadcast(new TableProtocol.Event { text = "The table is closed." });
                dirty = true;
            });
        }

        // ── what the phones see ─────────────────────────────────────────────────────────────────

        public TableProtocol.View View()
        {
            var v = new TableProtocol.View
            {
                game = rules.Poker ? "poker" : "teenpatti",
                seats = rules.Seats,
                hand = HandNo,
                dealer = Dealer,
                boot = rules.Boot, smallBlind = rules.SmallBlind, bigBlind = rules.BigBlind,
                closeAt = rules.CloseAtUtc == DateTime.MaxValue ? null : rules.CloseAtUtc.ToString("o"),
                phase = State == Phase.Playing ? (Hand != null && Hand.IsSideShowAnswer ? "sideshow" : "playing")
                      : State == Phase.Result ? "result"
                      : State == Phase.Closed || State == Phase.Closing ? "closed" : "waiting",
            };
            foreach (var s in Seats.Occupied.OrderBy(s => s.Number))
                v.players.Add(new TableProtocol.SeatView
                {
                    seat = s.Number, user = s.UserId, name = s.Name, stack = s.Stack, connected = s.Connected,
                    state = s.State == TableSeats.State.Leaving ? "leaving" : !s.Connected ? "away" : "waiting",
                });
            if (Hand != null && (State == Phase.Playing || State == Phase.Settling || State == Phase.Result))
            {
                Hand.Fill(v);
                if (State == Phase.Playing && turnOf != null)
                {
                    v.turn = Seats.Find(turnOf)?.Number ?? -1;
                    v.turnTotal = (float)(Hand.IsSideShowAnswer ? rules.SideShowSeconds : rules.TurnSeconds);
                    v.turnLeft = (float)Math.Max(0, (turnEndsAt - now).TotalSeconds);
                }
            }
            if (State == Phase.Waiting)
            {
                v.nextHandIn = nextHandAt == DateTime.MinValue ? 0 : (float)Math.Max(0, (nextHandAt - now).TotalSeconds);
                v.message = nextHandAt != DateTime.MinValue ? "Next hand starting" :
                            now >= rules.CloseAtUtc - rules.NoNewHandBefore ? "The table is closing" : "Waiting for players";
            }
            return v;
        }

        void Publish() => output.Broadcast(View());
    }
}
