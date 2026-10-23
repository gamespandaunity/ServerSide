using System;
using System.Collections.Generic;

namespace GamesPanda.Tables
{
    /// <summary>
    /// What the phones and the match server say to each other at a Teen Patti 5 / Poker 6 table. Everything travels as
    /// JSON through three Mirror messages that never change (CasinoTableNet: CmdTable, RpcTable, TargetTable), so rules,
    /// screens and these messages can grow with a hot update and no new APK. Unknown fields are ignored on both sides.
    /// Cards are indexes 0..51 (Card.FromIndex).
    /// </summary>
    public static class TableProtocol
    {
        public const int Version = 1;

        // ── phone → server ──────────────────────────────────────────────────────────────────────

        /// <summary>
        /// t = "ticket" (sit / reconnect / after a top-up: the backend's one-use join ticket), "act", "leave".
        /// Actions (a): Teen Patti "see", "chaal", "raise", "pack", "show", "sideshow", "answer" (yes);
        /// Poker "fold", "check", "call", "raise" (amount = total bet this street), "allin".
        /// </summary>
        [Serializable]
        public class In
        {
            public string t;
            public string ticket;
            public string name;
            public int hand;
            public string a;
            public long amount;
            public bool yes;
        }

        // ── server → phones ─────────────────────────────────────────────────────────────────────

        [Serializable]
        public class SeatView
        {
            public int seat;
            public string user;
            public string name;
            public long stack;            // chips not in the pot
            public long bet;              // Teen Patti: in the pot this hand; Poker: this street
            /// <summary>waiting (sits out this hand) | in | packed | allin | away | leaving</summary>
            public string state;
            public bool seen;             // Teen Patti
            public bool connected;
        }

        /// <summary>t = "state": the whole table, sent after every change.</summary>
        [Serializable]
        public class View
        {
            public string t = "state";
            public int v = Version;
            public string game;           // "teenpatti" | "poker"
            public int seats;
            /// <summary>waiting | playing | sideshow | result | closed</summary>
            public string phase;
            public int hand;
            public int dealer = -1;
            public int turn = -1;         // seat whose timer runs (the side-show target while one is asked)
            public float turnLeft;        // seconds
            public float turnTotal;
            public long pot;
            public long stake;            // Teen Patti blind stake
            public long currentBet;       // Poker
            public long minRaiseTo;       // Poker
            public long boot, smallBlind, bigBlind;
            public int[] board = Array.Empty<int>();
            public string street;         // Poker: Preflop | Flop | Turn | River
            public int sideShowFrom = -1, sideShowTo = -1;
            public string closeAt;
            public float nextHandIn;      // seconds until the next deal, while waiting
            public string message;        // one line for the table ("Waiting for players", "Table closes in 3 minutes")
            public List<SeatView> players = new List<SeatView>();
        }

        /// <summary>t = "you": this connection's seat. t = "hole": my own cards (only ever sent to their owner).</summary>
        [Serializable]
        public class Private
        {
            public string t;
            public int seat = -1;
            public string user;
            public int hand;
            public int[] cards;
            /// <summary>Side-show: the other player's cards, to the two players of it only.</summary>
            public string otherUser;
            public int[] otherCards;
            public string text;           // t = "error"
        }

        [Serializable]
        public class ResultRow
        {
            public string user;
            public int seat;
            public long contributed;
            public long won;
            public long stack;
        }

        /// <summary>t = "result": a hand is over and settled.</summary>
        [Serializable]
        public class Result
        {
            public string t = "result";
            public int hand;
            public string reason;
            public long pot;
            public long commission;
            public List<ResultRow> players = new List<ResultRow>();
            public List<string> winners = new List<string>();
            public Dictionary<string, int[]> shown = new Dictionary<string, int[]>();
            public int[] board = Array.Empty<int>();
            public Dictionary<string, string> hands = new Dictionary<string, string>();   // user → "Pair K-K-4", for the banner
        }

        /// <summary>t = "event": one line for the table log ("Ali raises to 400").</summary>
        [Serializable]
        public class Event
        {
            public string t = "event";
            public string text;
        }
    }
}
