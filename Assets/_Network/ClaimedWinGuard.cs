using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

/// <summary>
/// Snooker, Cricket and Highway Racer let a CLIENT declare the winner (CmdAnnounceVictory, requiresAuthority = false):
/// the server reported whatever player id it was sent as the winner and paid out. The legitimate use is "my opponent
/// disconnected, I win"; Snooker's only caller was a Space-key debug shortcut. Anyone could win a paid match by sending
/// their own id.
///
/// The Command signatures stay as they are (Mirror hashes the full signature, so changing one would make every
/// installed APK's call unknown to this server). Instead a claim is accepted only when it is what it says it is:
/// exactly one player is still connected and the claimed winner is that player, or the other player of the match
/// announced they are leaving (Leave or app quit) - their connection may still be closing. A claim refused because
/// the opponent is still connected is re-checked for a few seconds (Accept).
/// </summary>
public static class ClaimedWinGuard
{
    /// <summary>The opponent's connection stays open up to the transport timeout (5 s) after their app quits, and the
    /// remaining player's app claims the win at once. A refused claim is checked again for this long.</summary>
    const float RecheckSeconds = 8f;

    /// <summary>Run <paramref name="accept"/> when the claim holds now, or once the opponent has gone within
    /// RecheckSeconds; otherwise log the refusal. Each claim is accepted at most once.</summary>
    public static void Accept(int claimedWinnerId, string game, string reason, Action accept)
    {
        if (Allow(claimedWinnerId, game, reason, logRefusal: false)) { accept(); return; }
        Debug.Log($"[ClaimedWin][{game}] Win claim for {claimedWinnerId} held: the opponent is still connected; re-checking for {RecheckSeconds:0} s.");
        MatchServerApi.Run(RecheckClaim(claimedWinnerId, game, reason, accept));
    }

    static IEnumerator RecheckClaim(int claimedWinnerId, string game, string reason, Action accept)
    {
        float until = Time.realtimeSinceStartup + RecheckSeconds;
        while (Time.realtimeSinceStartup < until)
        {
            yield return new WaitForSecondsRealtime(0.5f);
            if (Allow(claimedWinnerId, game, reason, logRefusal: false))
            {
                Debug.Log($"[ClaimedWin][{game}] Win claim for {claimedWinnerId} accepted: the opponent has gone.");
                accept();
                yield break;
            }
        }
        Allow(claimedWinnerId, game, reason, logRefusal: true);
    }

    public static bool Allow(int claimedWinnerId, string game, string reason, bool logRefusal = true)
    {
        var connected = new List<string>();
        foreach (NetworkConnectionToClient conn in NetworkServer.connections.Values)
        {
            if (conn == null || conn.identity == null) continue;
            var player = conn.identity.GetComponent<MirrorPlayerPrefab>();
            if (player != null && !string.IsNullOrEmpty(player.playerId)) connected.Add(player.playerId);
        }

        string claimed = claimedWinnerId.ToString();
        if (connected.Count == 1 && connected[0] == claimed) return true;

        // The opponent announced they are going (Leave, or their app quitting): the remaining player's app claims the
        // win at once, while that connection is still closing. Accept it for the other player of this match only.
        var ngm = NetworkGameManager.Instance;
        string leaver = ngm != null ? ngm.ExplicitLeaverId : null;
        if (!string.IsNullOrEmpty(leaver) && leaver != claimed && ngm.creatorData != null && ngm.joinerData != null
            && (claimed == ngm.creatorData.playerId || claimed == ngm.joinerData.playerId)
            && (leaver == ngm.creatorData.playerId || leaver == ngm.joinerData.playerId))
        {
            Debug.Log($"[ClaimedWin][{game}] Accepted the win claim for {claimed}: {leaver} announced they are leaving.");
            return true;
        }

        if (!logRefusal) return false;
        Debug.LogError($"[ClaimedWin][{game}] REFUSED a client's win claim for {claimed} ('{reason}'): connected players = [{string.Join(", ", connected)}]. " +
                       "A client may only claim the win when its opponent is gone.");
        return false;
    }
}
