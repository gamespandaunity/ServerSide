using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// This phone's place at a Teen Patti 5 / Poker 6 table, from the lobby to leaving:
/// sit (buy-in) → wait until the table's server is up (GET /casinos/tables/{tx} until it gives an address and a
/// ticket) → connect with Mirror → the table screen (CasinoTableScreen) sends the ticket to the match server, which
/// seats us. A reconnect fetches a fresh ticket the same way (every ticket works once).
/// Leaving while connected goes through the match server (it packs / folds us and pays the stack back after the hand);
/// before connecting, through PATCH /casinos/tables/{tx}/leave.
/// </summary>
public static class CasinoTableSession
{
    public static CasinoTablesApi.Table Table { get; private set; }
    public static string Tx => Table?.transaction_id;
    public static bool Active => Table != null;
    /// <summary>The ticket for the next connection (handed to the match server once, then cleared).</summary>
    public static string PendingTicket { get; private set; }
    public static long Stack { get; private set; }
    public static int Seat { get; private set; } = -1;

    public static event Action<string> Status;   // "Starting the table…", errors, for the lobby panel

    const float ServerWaitSeconds = 90f, PollSeconds = 2f;
    public static readonly string[] TableScenes = { "TeenPattiTable", "PokerTable" };
    static Coroutine waiting;

    /// <summary>Sit at <paramref name="table"/> with <paramref name="buyIn"/>, then connect.</summary>
    public static void Join(CasinoTablesApi.Table table, long buyIn, Action<string> fail)
    {
        CasinoTablesApi.Sit(table.transaction_id, buyIn, seated =>
        {
            Table = table;
            RememberTable(table.transaction_id);
            Seat = seated.seat;
            Stack = seated.stack;
            ConstantsData_M.Log($"[Table] seated at {table.transaction_id}, seat {seated.seat}, stack {seated.stack}");
            if (seated.ServerReady) Connect(seated.server_address, seated.join_ticket);
            else WaitForServer(fail);
        }, fail);
    }

    /// <summary>Back to a table we sit at (app restarted, connection lost): a fresh ticket, then connect.</summary>
    public static void Rejoin(string tx, Action<string> fail)
    {
        CasinoTablesApi.Get(tx, table =>
        {
            if (table.Status == "closed") { Clear(); fail?.Invoke("The table has closed. Your chips are back in your wallet."); return; }
            if (table.me == null || table.me.seat < 0) { Clear(); fail?.Invoke("You are no longer seated at this table."); return; }
            Table = table;
            Seat = table.me.seat;
            Stack = table.me.stack;
            if (!string.IsNullOrEmpty(table.me.server_address) && !string.IsNullOrEmpty(table.me.join_ticket))
                Connect(table.me.server_address, table.me.join_ticket);
            else WaitForServer(fail);
        }, fail);
    }

    /// <summary>A fresh ticket for the connection we already have (after a top-up, or a Mirror reconnect).</summary>
    public static void RefreshTicket(Action<string> gotTicket, Action<string> fail)
    {
        if (!Active) { fail?.Invoke("Not at a table"); return; }
        CasinoTablesApi.Get(Tx, table =>
        {
            if (table.me == null || string.IsNullOrEmpty(table.me.join_ticket)) { fail?.Invoke("No ticket"); return; }
            Stack = table.me.stack;
            gotTicket?.Invoke(table.me.join_ticket);
        }, fail);
    }

    /// <summary>Top up between hands, then let the match server read the new stack from a fresh ticket.</summary>
    public static void TopUp(long amount, Action<string> sendTicket, Action<string> fail)
    {
        if (!Active) { fail?.Invoke("Not at a table"); return; }
        CasinoTablesApi.Sit(Tx, amount, seated =>
        {
            Stack = seated.stack;
            if (!string.IsNullOrEmpty(seated.join_ticket)) sendTicket?.Invoke(seated.join_ticket);
            else RefreshTicket(sendTicket, fail);
        }, fail);
    }

    /// <summary>The table screen takes the ticket to send it to the match server.</summary>
    public static string TakeTicket()
    {
        string t = PendingTicket;
        PendingTicket = null;
        return t;
    }

    static void WaitForServer(Action<string> fail)
    {
        Status?.Invoke("Starting the table…");
        if (waiting != null && ApiAndRoomManager._instance != null) ApiAndRoomManager._instance.StopCoroutine(waiting);
        waiting = ApiAndRoomManager._instance != null ? ApiAndRoomManager._instance.StartCoroutine(Poll(fail)) : null;
    }

    static IEnumerator Poll(Action<string> fail)
    {
        float until = Time.realtimeSinceStartup + ServerWaitSeconds;
        while (Time.realtimeSinceStartup < until && Active)
        {
            yield return new WaitForSecondsRealtime(PollSeconds);
            bool done = false;
            CasinoTablesApi.Get(Tx, table =>
            {
                if (table.me != null && !string.IsNullOrEmpty(table.me.server_address) && !string.IsNullOrEmpty(table.me.join_ticket))
                {
                    done = true;
                    Connect(table.me.server_address, table.me.join_ticket);
                }
                else if (table.Status == "closed") { done = true; Clear(); fail?.Invoke("The table has closed."); }
            }, _ => { });
            if (done) { waiting = null; yield break; }
        }
        waiting = null;
        if (Active) fail?.Invoke("The table did not start. Please try again.");
    }

    static void Connect(string address, string ticket)
    {
        PendingTicket = ticket;
        Status?.Invoke("Joining the table…");
        ConstantsData_M.Log($"[Table] connecting to {address}");
        // Not JoinTicket.Remember: the table's ticket is checked by the table itself (CasinoTableScreen sends it),
        // and a ticket works only once.
        MirrorNetwork.Instance.isMasterClient = false;
        // The table scenes count as game scenes for Mirror's reconnect (the list itself is in the APK's LoadingScene).
        foreach (string scene in TableScenes)
            if (!MirrorNetwork.Instance.gameScenes.Contains(scene)) MirrorNetwork.Instance.gameScenes.Add(scene);
        MirrorNetwork.Instance.edgegapAPIClient.JoinGame(Tx, null, address);
    }

    /// <summary>Leave the table. Connected: the match server stands us up (after the hand we are in). Otherwise the
    /// backend pays the stack back now.</summary>
    public static void Leave(Action<string> done)
    {
        if (!Active) { done?.Invoke(null); return; }
        if (CasinoTableNet.Instance != null && Mirror.NetworkClient.isConnected)
        {
            CasinoTableNet.Instance.Send("{\"t\":\"leave\"}");
            done?.Invoke(null);
            return;
        }
        CasinoTablesApi.Leave(Tx, left =>
        {
            Clear();
            done?.Invoke(null);
        }, error => done?.Invoke(error));
    }

    const string LastTablePref = "casino_table_tx";

    /// <summary>The table this phone last sat at, to offer "Return to your table" (CasinoTablesLobby).</summary>
    public static string LastTable => PlayerPrefs.GetString(LastTablePref, "");

    public static void RememberTable(string tx) { PlayerPrefs.SetString(LastTablePref, tx ?? ""); PlayerPrefs.Save(); }

    public static void ForgetLastTable() { PlayerPrefs.DeleteKey(LastTablePref); PlayerPrefs.Save(); }

    public static void Clear()
    {
        Table = null;
        PendingTicket = null;
        Seat = -1;
        Stack = 0;
    }
}
