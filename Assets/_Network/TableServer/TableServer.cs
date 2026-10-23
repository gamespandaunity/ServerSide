using System;
using System.Collections.Generic;
using System.Linq;
using GamesPanda.Tables;
using Mirror;
using Newtonsoft.Json;
using UnityEngine;

/// <summary>
/// Runs a Teen Patti 5 / Poker 6 table on the match server: connects Mirror (CasinoTableNet) and the backend
/// (CasinoTableApi) to the table loop (TableRunner), and ticks it. Starts by itself when the table object comes up on a
/// server whose deployment is a table (TableEnv.IsTable).
///
/// Outside an Edgegap deployment (no MatchKey: editor, local server) the backend is replaced by an in-memory one that
/// seats "local:&lt;user id&gt;:&lt;seat&gt;:&lt;stack&gt;" tickets, so a table can be tried with the editor and a clone.
/// </summary>
public class TableServer : MonoBehaviour
{
    public static TableServer Instance { get; private set; }
    public TableRunner Table { get; private set; }

    CasinoTableNet net;
    readonly Dictionary<string, NetworkConnectionToClient> connectionOf = new Dictionary<string, NetworkConnectionToClient>();
    float nextTick;

    static readonly JsonSerializerSettings Json = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Register()
    {
        CasinoTableNet.ServerStarted -= OnTableObject;
        CasinoTableNet.ServerStarted += OnTableObject;
    }

    static void OnTableObject(CasinoTableNet table)
    {
        if (!NetworkServer.active) return;
        if (!TableEnv.IsTable && MatchServerApi.IsMatchServer)
        {
            Debug.LogWarning("[Table] A table scene on a server whose deployment is not a table - nothing started.");
            return;
        }
        if (Instance != null) return;
        var go = new GameObject("TableServer");
        Instance = go.AddComponent<TableServer>();
        Instance.Begin(table);
    }

    void Begin(CasinoTableNet table)
    {
        net = table;
        if (!TableEnv.IsTable) LocalDefaults();
        var rules = new TableRules
        {
            Poker = TableEnv.IsPoker,
            Seats = TableEnv.MaxPlayers,
            Boot = TableEnv.Boot,
            SmallBlind = TableEnv.SmallBlind,
            BigBlind = TableEnv.BigBlind,
            CommissionPct = TableEnv.CommissionPct,
            CommissionCap = TableEnv.CommissionCap,
            CloseAtUtc = TableEnv.CloseAtUtc,
        };
        ITableBank bank = MatchServerApi.IsMatchServer ? new ApiBank() : (ITableBank)new LocalBank();
        Table = new TableRunner(rules, bank, new Output(this));
        CasinoTableNet.ServerHandler = OnMessage;
        Debug.Log("[Table] " + TableEnv.Describe() + (MatchServerApi.IsMatchServer ? "" : " (local backend)"));
        MatchFlow.Log(rules.Poker ? "Poker table" : "Teen Patti table", TableEnv.Describe());
    }

    /// <summary>A table scene played without a table deployment (editor / local server): Teen Patti or Poker by scene name.</summary>
    static void LocalDefaults()
    {
        bool poker = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.IndexOf("Poker", StringComparison.OrdinalIgnoreCase) >= 0;
        if (poker) TableEnv.Override(TableEnv.PokerBoard, 6, 50, 100, 4000, 10000, 5, DateTime.UtcNow.AddMinutes(60), "blinds_50_100");
        else TableEnv.Override(TableEnv.TeenPattiBoard, 5, 50, 0, 1000, 5000, 5, DateTime.UtcNow.AddMinutes(60), "boot_50");
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (CasinoTableNet.ServerHandler == (Action<NetworkConnectionToClient, string>)OnMessage) CasinoTableNet.ServerHandler = null;
    }

    void Update()
    {
        if (Table == null || Time.unscaledTime < nextTick) return;
        nextTick = Time.unscaledTime + 0.25f;
        try { Table.Tick(DateTime.UtcNow); }
        catch (Exception e) { Debug.LogException(e); }
    }

    void OnMessage(NetworkConnectionToClient conn, string json)
    {
        TableProtocol.In msg;
        try { msg = JsonConvert.DeserializeObject<TableProtocol.In>(json); }
        catch (JsonException) { Debug.LogWarning($"[Table] unreadable message from conn {conn.connectionId}"); return; }
        if (msg == null) return;
        try
        {
            Table.Message(conn, msg);
            string user = Table.UserOf(conn);
            if (user != null) connectionOf[user] = conn;
            Table.Tick(DateTime.UtcNow);
        }
        catch (Exception e) { Debug.LogException(e); }
    }

    /// <summary>Called by MirrorNetwork.OnServerDisconnect on a table server.</summary>
    public void Disconnected(NetworkConnectionToClient conn)
    {
        if (Table == null || conn == null) return;
        string user = Table.UserOf(conn);
        Table.Disconnected(conn);
        if (user != null && connectionOf.TryGetValue(user, out var c) && c == conn) connectionOf.Remove(user);
    }

    // ── output ──────────────────────────────────────────────────────────────────────────────────

    sealed class Output : ITableOutput
    {
        readonly TableServer s;
        public Output(TableServer server) { s = server; }

        public void Broadcast(object message)
        {
            if (s.net != null && NetworkServer.active) s.net.Broadcast(JsonConvert.SerializeObject(message, Json));
        }

        public void Send(string userId, object message)
        {
            if (s.net == null || !NetworkServer.active) return;
            NetworkConnectionToClient conn = null;
            foreach (var c in NetworkServer.connections.Values)
                if (s.Table != null && s.Table.UserOf(c) == userId) { conn = c; break; }
            if (conn != null) s.net.SendTo(conn, JsonConvert.SerializeObject(message, Json));
        }

        public void SendToConnection(object connection, object message)
        {
            if (s.net != null && NetworkServer.active && connection is NetworkConnectionToClient conn)
                s.net.SendTo(conn, JsonConvert.SerializeObject(message, Json));
        }

        public void Log(string line)
        {
            Debug.Log("[Table] " + line);
            MatchFlow.Log(TableEnv.IsPoker ? "Poker table" : "Teen Patti table", line);
        }
    }

    // ── backend ─────────────────────────────────────────────────────────────────────────────────

    sealed class ApiBank : ITableBank
    {
        public void VerifyTicket(string ticket, Action<string, int, long, string> done) =>
            MatchServerApi.Run(CasinoTableApi.VerifyTicket(ticket, (user, seat, stack, a) => done(user, seat, stack, a.Ok ? null : a.Message)));

        public void OpenHand(int handNo, IList<string> players, long boot, Action<string, string> done) =>
            MatchServerApi.Run(CasinoTableApi.OpenHand(handNo, players, boot, (id, a) => done(id, a.Ok ? null : a.Message)));

        public void SettleHand(string roundId, int handNo, string reason, HandLedger.Result result, IList<Card> board,
                               IDictionary<string, Card[]> shown, Action<Dictionary<string, long>, Dictionary<string, long>, string> done) =>
            MatchServerApi.Run(CasinoTableApi.SettleHand(roundId, handNo, reason, result, board, shown,
                (stacks, expected, a) => done(stacks, expected, a.Ok ? null : a.Message)));

        public void Leave(string userId, long stack, string reason, int attempt, Action<string> done) =>
            MatchServerApi.Run(CasinoTableApi.Leave(userId, stack, reason, attempt, a => done(a.Ok ? null : a.Message)));

        public void Close(string reason, IDictionary<string, long> stacks, Action<string> done) =>
            MatchServerApi.Run(CasinoTableApi.Close(reason, stacks, a => done(a.Ok ? null : a.Message)));
    }

    /// <summary>Editor / local server only: tickets "local:user:seat:stack", every hand accepted.</summary>
    sealed class LocalBank : ITableBank
    {
        readonly Dictionary<string, long> escrow = new Dictionary<string, long>();
        int round;

        public void VerifyTicket(string ticket, Action<string, int, long, string> done)
        {
            var p = (ticket ?? "").Split(':');
            if (p.Length != 4 || p[0] != "local" || !int.TryParse(p[2], out int seat) || !long.TryParse(p[3], out long stack))
            { done(null, -1, 0, "local tickets look like local:user:seat:stack"); return; }
            if (!escrow.ContainsKey(p[1])) escrow[p[1]] = stack;
            done(p[1], seat, escrow[p[1]], null);
        }

        public void OpenHand(int handNo, IList<string> players, long boot, Action<string, string> done) => done((++round).ToString(), null);

        public void SettleHand(string roundId, int handNo, string reason, HandLedger.Result r, IList<Card> board,
                               IDictionary<string, Card[]> shown, Action<Dictionary<string, long>, Dictionary<string, long>, string> done)
        {
            foreach (var p in r.Players) escrow[p.user_id] = p.stack_after;
            done(new Dictionary<string, long>(escrow), null, null);
        }

        public void Leave(string userId, long stack, string reason, int attempt, Action<string> done) { escrow.Remove(userId); done(null); }
        public void Close(string reason, IDictionary<string, long> stacks, Action<string> done) { escrow.Clear(); done(null); }
    }
}
