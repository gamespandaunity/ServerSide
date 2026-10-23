using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

/// <summary>
/// The only network messages of a Teen Patti 5 / Poker 6 table: one Command and two RPCs carrying JSON
/// (GamesPanda.Tables.TableProtocol). Their signatures never change, so the table's rules, messages and screens can
/// change with a hot update; only this object needs to be in the APK and in the server build.
///
/// Lives on a scene object of the table scenes (TeenPattiTable, PokerTable), the same file in both repositories.
/// Server: TableServer handles what arrives (<see cref="ServerHandler"/>). Phones: the table screen listens to
/// <see cref="ClientMessage"/>; the last table state and the private messages that came before the screen was ready
/// are kept for it (<see cref="LastState"/>, <see cref="TakeEarly"/>).
/// </summary>
public class CasinoTableNet : NetworkBehaviour
{
    public const int MaxMessageLength = 4096;

    public static CasinoTableNet Instance { get; private set; }

    /// <summary>Server: (connection, json) for every phone message.</summary>
    public static Action<NetworkConnectionToClient, string> ServerHandler;
    /// <summary>Server: the table object is live (TableServer starts the table here).</summary>
    public static event Action<CasinoTableNet> ServerStarted;

    /// <summary>Phones: every message from the server, in order.</summary>
    public static event Action<string> ClientMessage;
    /// <summary>Phones: the last full table state ("t":"state"), for a screen that opens late.</summary>
    public static string LastState { get; private set; }
    static readonly List<string> early = new List<string>();

    void Awake() => Instance = this;

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        LastState = null;
        early.Clear();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        ServerStarted?.Invoke(this);
    }

    // ── phone → server ──────────────────────────────────────────────────────────────────────────

    /// <summary>Phones: send one message (sit with a ticket, an action, leave).</summary>
    public void Send(string json)
    {
        if (string.IsNullOrEmpty(json) || json.Length > MaxMessageLength) return;
        if (!NetworkClient.active) { Debug.LogWarning("[Table] not connected - message dropped"); return; }
        CmdTable(json);
    }

    [Command(requiresAuthority = false)]
    void CmdTable(string json, NetworkConnectionToClient sender = null)
    {
        if (sender == null || string.IsNullOrEmpty(json) || json.Length > MaxMessageLength) return;
        ServerHandler?.Invoke(sender, json);
    }

    // ── server → phones ─────────────────────────────────────────────────────────────────────────

    [Server]
    public void Broadcast(string json) => RpcTable(json);

    [Server]
    public void SendTo(NetworkConnectionToClient conn, string json)
    {
        if (conn != null) TargetTable(conn, json);
    }

    [ClientRpc]
    void RpcTable(string json) => Deliver(json);

    [TargetRpc]
    void TargetTable(NetworkConnectionToClient target, string json) => Deliver(json);

    static void Deliver(string json)
    {
        if (json.Contains("\"t\":\"state\"")) LastState = json;
        if (ClientMessage == null)
        {
            if (!json.Contains("\"t\":\"state\"") && !json.Contains("\"t\":\"event\"")) early.Add(json);
            return;
        }
        ClientMessage.Invoke(json);
    }

    /// <summary>Private messages (my seat, my cards) that arrived before anyone listened.</summary>
    public static List<string> TakeEarly()
    {
        var list = new List<string>(early);
        early.Clear();
        return list;
    }
}
