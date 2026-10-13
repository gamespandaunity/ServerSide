using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Mirror;
using Newtonsoft.Json.Linq;
using UnityEngine;

/// <summary>
/// Join tickets (Edgegap contract, 12 Oct 2026): the backend gives each player a one-use ticket for this match —
/// the creator in the notify-to-play answer (data.join_ticket), the joiner in the childActivated socket event.
/// The app sends it with CmdJoinTicket when it connects; the server checks it with /match-server/join-ticket/verify
/// and that the user it belongs to is the player the connection says it is (CmdSetPlayerData). A refused ticket,
/// or a ticket for another user, disconnects that connection.
///
/// Soft for now: an app that sends no ticket (APKs before this change) is let in, with a warning in the log.
/// Turn RequireTicket on once every live app sends one.
/// </summary>
public static class JoinTicketGate
{
    public const bool RequireTicket = false;
    const float TicketWaitSeconds = 15f;

    // A ticket works once, so a player who reconnects with the same ticket is matched here instead of asking again.
    static readonly Dictionary<string, string> userByTicket = new Dictionary<string, string>();
    static readonly Dictionary<int, string> verifiedUser = new Dictionary<int, string>();
    static readonly Dictionary<int, string> claimedUser = new Dictionary<int, string>();
    static readonly HashSet<int> pending = new HashSet<int>();

    /// <summary>The app sent its ticket.</summary>
    public static void OnTicket(NetworkConnectionToClient conn, string ticket)
    {
        if (conn == null) return;
        if (!MatchServerApi.IsMatchServer) { Debug.Log("[JoinTicket] Not an Edgegap match server - ticket not checked."); return; }
        if (string.IsNullOrWhiteSpace(ticket)) { Debug.LogWarning($"[JoinTicket] conn={conn.connectionId} sent an empty ticket."); return; }
        ticket = ticket.Trim();

        // The app keeps the last ticket it was given; one for another match is just old - treat it as no ticket.
        string ticketTx = TicketTransaction(ticket);
        if (ticketTx != null && !string.IsNullOrEmpty(MatchServerApi.TransactionId) && ticketTx != MatchServerApi.TransactionId)
        {
            Debug.LogWarning($"[JoinTicket] conn={conn.connectionId} sent a ticket for another match ({ticketTx}) - ignored.");
            return;
        }

        if (userByTicket.TryGetValue(ticket, out string known))
        {
            Debug.Log($"[JoinTicket] conn={conn.connectionId} reconnected with the ticket of user {known}.");
            verifiedUser[conn.connectionId] = known;
            Compare(conn);
            return;
        }

        int id = conn.connectionId;
        pending.Add(id);
        MatchServerApi.Run(MatchServerApi.VerifyJoinTicket(ticket, (ok, answered, userId, message) =>
        {
            pending.Remove(id);
            if (!NetworkServer.connections.TryGetValue(id, out NetworkConnectionToClient live) || live != conn) return;   // left meanwhile
            if (!answered)
            {
                // The API did not answer: let the player in rather than lock a paid match out over a network blip.
                Debug.LogWarning($"[JoinTicket] conn={id}: could not reach the API to verify the ticket ({message}) - let in.");
                return;
            }
            if (!ok || string.IsNullOrEmpty(userId))
            {
                Refuse(conn, "ticket refused: " + message);
                return;
            }
            userByTicket[ticket] = userId;
            verifiedUser[id] = userId;
            Compare(conn);
        }));
    }

    /// <summary>CmdSetPlayerData: the player this connection says it is.</summary>
    public static void OnClaim(NetworkConnectionToClient conn, string playerId)
    {
        if (conn == null || !MatchServerApi.IsMatchServer) return;
        claimedUser[conn.connectionId] = playerId;
        Compare(conn);
        if (!verifiedUser.ContainsKey(conn.connectionId)) MatchServerApi.Run(WaitForTicket(conn));
    }

    public static void Forget(NetworkConnectionToClient conn)
    {
        if (conn == null) return;
        verifiedUser.Remove(conn.connectionId);
        claimedUser.Remove(conn.connectionId);
        pending.Remove(conn.connectionId);
    }

    static IEnumerator WaitForTicket(NetworkConnectionToClient conn)
    {
        int id = conn.connectionId;
        float until = Time.realtimeSinceStartup + TicketWaitSeconds;
        while (Time.realtimeSinceStartup < until && (pending.Contains(id) || !verifiedUser.ContainsKey(id)))
        {
            if (!NetworkServer.connections.ContainsKey(id)) yield break;
            if (!pending.Contains(id) && verifiedUser.ContainsKey(id)) yield break;
            yield return new WaitForSecondsRealtime(0.5f);
        }
        if (verifiedUser.ContainsKey(id) || !NetworkServer.connections.ContainsKey(id)) yield break;
        claimedUser.TryGetValue(id, out string who);
        if (RequireTicket) Refuse(conn, $"no join ticket from user {who}");
        else Debug.LogWarning($"[JoinTicket] conn={id} (user {who}) sent no join ticket - let in (an app from before join tickets).");
    }

    static void Compare(NetworkConnectionToClient conn)
    {
        int id = conn.connectionId;
        if (!verifiedUser.TryGetValue(id, out string verified) || !claimedUser.TryGetValue(id, out string claimed)) return;
        if (verified == claimed) Debug.Log($"[JoinTicket] conn={id}: ticket verified for user {verified}.");
        else Refuse(conn, $"the ticket is for user {verified} but the player says it is {claimed}");
    }

    static void Refuse(NetworkConnectionToClient conn, string why)
    {
        Debug.LogError($"[JoinTicket] conn={conn.connectionId} refused - {why}. Disconnecting.");
        Forget(conn);
        conn.Disconnect();
    }

    /// <summary>The match id inside a ticket ("t" in the payload before the dot), or null when it can't be read.</summary>
    static string TicketTransaction(string ticket)
    {
        try
        {
            string part = ticket.Split('.')[0].Replace('-', '+').Replace('_', '/');
            part = part.PadRight(part.Length + (4 - part.Length % 4) % 4, '=');
            return JObject.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(part)))["t"]?.ToString();
        }
        catch (Exception) { return null; }
    }
}
