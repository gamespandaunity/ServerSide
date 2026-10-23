using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

/// <summary>
/// The app's calls for Teen Patti 5 Players (game 60) and Poker 6 Players (game 61) tables ("Teen Patti and Poker
/// Tables", backend, 22 Oct 2026). Players sit with a buy-in that becomes their stack; the wallet only changes on sit,
/// top-up and leave. Teen Patti (3) and Poker (8) stay the 2-player games.
///  - create:     POST  /casinos/create-new-session        { game_id, coins_type, tier, visibility, app_version }
///  - sit/top-up: POST  /casinos/tables/{tx}/sit           { buy_in, app_version }
///  - reconnect:  GET   /casinos/tables/{tx}               (me: seat, stack, server_address, a fresh join_ticket)
///  - leave:      PATCH /casinos/tables/{tx}/leave         {} (only between hands)
///  - history:    GET   /casinos/tables/{tx}/history
/// Every failure hands back the backend's own message ("table full", "buy-in must be between 1000 and 5000", …).
/// </summary>
public static class CasinoTablesApi
{
    public const int TeenPattiTableGame = 60, PokerTableGame = 61;

    public static bool IsTableGame(int gameId) => gameId == TeenPattiTableGame || gameId == PokerTableGame;
    /// <summary>The 2-player game whose board a table plays: 3 (Teen Patti) or 8 (Poker).</summary>
    public static int BoardOf(int gameId) => gameId == TeenPattiTableGame ? 3 : gameId == PokerTableGame ? 8 : gameId;
    public static int SeatsOf(int gameId) => gameId == TeenPattiTableGame ? 5 : gameId == PokerTableGame ? 6 : 2;

    // ── models ──────────────────────────────────────────────────────────────────────────────────

    [Serializable]
    public class TablePlayer
    {
        public string user_id;
        public int seat;
        public string name;
        public string file_url;
    }

    [Serializable]
    public class Me
    {
        public int seat = -1;
        public long stack;
        public long buy_in;
        public string server_address;
        public string join_ticket;
        public string listener;
    }

    /// <summary>A table as the create answer, the lobby packet and GET /casinos/tables/{tx} describe it.</summary>
    [Serializable]
    public class Table
    {
        public string transaction_id;
        public int game_id;
        public string coins_type;
        public string tier;
        public int max_players;
        public long boot;
        public long? small_blind;
        public long? big_blind;
        public long buy_in_min;
        public long buy_in_max;
        public int commission_pct;
        public long? commission_cap;
        public string close_at;
        public string visibility;
        public int player_count;
        public List<TablePlayer> players = new List<TablePlayer>();
        public Me me;
        /// <summary>"open" or "closed" (GET's message).</summary>
        [JsonIgnore] public string Status;

        public bool IsPoker => game_id == PokerTableGame;
        public bool IsGold => !string.Equals(coins_type, "silver", StringComparison.OrdinalIgnoreCase);
        public DateTime CloseAtUtc => DateTime.TryParse(close_at, CultureInfo.InvariantCulture,
            DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var t) ? t : DateTime.MaxValue;
        public bool Full => player_count >= max_players;
        public string StakeText => IsPoker ? $"{small_blind}/{big_blind}" : $"Boot {boot}";
    }

    [Serializable]
    public class Seated
    {
        public int seat;
        public long stack;
        public long buy_in;
        public int player_count;
        public int max_players;
        public string server_address;
        public string join_ticket;
        public string listener;
        public long balance;
        /// <summary>"seated" or "topped_up".</summary>
        [JsonIgnore] public string Message;
        public bool ServerReady => !string.IsNullOrEmpty(server_address) && !string.IsNullOrEmpty(join_ticket);
    }

    [Serializable]
    public class Left
    {
        public long returned;
        public string coins_type;
        public long wallet_balance;
    }

    [Serializable]
    public class HistoryRow
    {
        public long round_id;
        public int hand_no;
        public long pot;
        public long commission;
        public string reason;
        public JArray players;
        public JArray board;
        public JObject shown;
    }

    [Serializable]
    public class Visit
    {
        public string visit;
        public long buy_in;
        public long cash_out;
        public long net;
    }

    [Serializable]
    public class History
    {
        public List<HistoryRow> hands = new List<HistoryRow>();
        public List<Visit> visits = new List<Visit>();
    }

    // ── calls ───────────────────────────────────────────────────────────────────────────────────

    static string AppVersion => Application.version;

    /// <summary>Open a table. Nobody is seated afterwards, the creator included: call <see cref="Sit"/> next.</summary>
    public static void Create(int gameId, bool gold, string tier, bool isPrivate, Action<Table> ok, Action<string> fail)
    {
        if (!IsTableGame(gameId)) { fail?.Invoke("Not a table game"); return; }
        var body = new Dictionary<string, object>
        {
            ["game_id"] = gameId.ToString(CultureInfo.InvariantCulture),
            ["coins_type"] = gold ? "gold" : "silver",
            ["tier"] = tier,
            ["visibility"] = isPrivate ? "private" : "public",
            ["app_version"] = AppVersion,
        };
        Send("POST", ServerConnection.Create_casino_Url, body, j => ok?.Invoke(ReadTable(j)), fail);
    }

    /// <summary>Sit down with <paramref name="buyIn"/> (or top up while seated, between hands).</summary>
    public static void Sit(string tx, long buyIn, Action<Seated> ok, Action<string> fail)
    {
        var body = new Dictionary<string, object> { ["buy_in"] = buyIn, ["app_version"] = AppVersion };
        Send("POST", $"/casinos/tables/{tx}/sit", body, j =>
        {
            var s = (j["data"] as JObject ?? j).ToObject<Seated>();
            s.Message = j["message"]?.ToString();
            ok?.Invoke(s);
        }, fail);
    }

    /// <summary>The table now; for a seated caller <see cref="Table.me"/> has a fresh one-use ticket (fetch it right
    /// before connecting).</summary>
    public static void Get(string tx, Action<Table> ok, Action<string> fail) =>
        Send("GET", $"/casinos/tables/{tx}", null, j =>
        {
            var t = ReadTable(j);
            t.Status = j["message"]?.ToString();
            ok?.Invoke(t);
        }, fail);

    /// <summary>Leave between hands: the whole stack goes back to the wallet. During a hand: "hand in progress".</summary>
    public static void Leave(string tx, Action<Left> ok, Action<string> fail) =>
        Send("PATCH", $"/casinos/tables/{tx}/leave", new Dictionary<string, object>(), j => ok?.Invoke((j["data"] as JObject ?? j).ToObject<Left>()), fail);

    public static void GetHistory(string tx, Action<History> ok, Action<string> fail) =>
        Send("GET", $"/casinos/tables/{tx}/history", null, j => ok?.Invoke((j["data"] as JObject ?? j).ToObject<History>()), fail);

    /// <summary>A lobby packet (roomCreated / publicRooms) for a table: table: true, bet_type "casino_table".</summary>
    public static bool IsTablePacket(JObject room) =>
        room != null && (room["table"]?.Type == JTokenType.Boolean && (bool)room["table"] ||
                         string.Equals(room["bet_type"]?.ToString(), "casino_table", StringComparison.OrdinalIgnoreCase));

    public static Table ReadTable(JObject j) => (j["data"] as JObject ?? j).ToObject<Table>();

    // ── transport ───────────────────────────────────────────────────────────────────────────────

    static void Send(string method, string url, Dictionary<string, object> body, Action<JObject> ok, Action<string> fail)
    {
        if (ApiAndRoomManager._instance == null) { fail?.Invoke("Please try again."); return; }
        ApiAndRoomManager._instance.StartCoroutine(ServerConnection.PlayerApiRequest(method, url,
            body == null ? null : JsonConvert.SerializeObject(body),
            text =>
            {
                JObject j;
                try { j = JObject.Parse(text); }
                catch (JsonException) { fail?.Invoke("Unexpected answer from the server."); return; }
                if (j["status"]?.Type == JTokenType.Boolean && !(bool)j["status"]) { fail?.Invoke(j["message"]?.ToString() ?? "Refused"); return; }
                try { ok?.Invoke(j); }
                catch (Exception e) { Debug.LogException(e); fail?.Invoke("Unexpected answer from the server."); }
            },
            error => fail?.Invoke(Explain(error))));
    }

    /// <summary>The backend's message from an error body ("table full"), else the text as it came.</summary>
    static string Explain(string error)
    {
        if (string.IsNullOrWhiteSpace(error)) return "No connection. Please try again.";
        try
        {
            var j = JObject.Parse(error);
            string m = j["message"]?.ToString();
            if (!string.IsNullOrWhiteSpace(m)) return m;
        }
        catch (JsonException) { }
        return error;
    }
}
