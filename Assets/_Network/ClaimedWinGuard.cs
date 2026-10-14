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
/// pressed Leave (their connection may still be closing).
/// </summary>
public static class ClaimedWinGuard
{
    public static bool Allow(int claimedWinnerId, string game, string reason)
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

        // The opponent pressed Leave: the remaining player's app claims the win at once, while the leaver's
        // connection is still closing. Accept it for the other player of this match only.
        var ngm = NetworkGameManager.Instance;
        string leaver = ngm != null ? ngm.ExplicitLeaverId : null;
        if (!string.IsNullOrEmpty(leaver) && leaver != claimed && ngm.creatorData != null && ngm.joinerData != null
            && (claimed == ngm.creatorData.playerId || claimed == ngm.joinerData.playerId)
            && (leaver == ngm.creatorData.playerId || leaver == ngm.joinerData.playerId))
        {
            Debug.Log($"[ClaimedWin][{game}] Accepted the win claim for {claimed}: {leaver} pressed Leave.");
            return true;
        }

        Debug.LogError($"[ClaimedWin][{game}] REFUSED a client's win claim for {claimed} ('{reason}'): connected players = [{string.Join(", ", connected)}]. " +
                       "A client may only claim the win when its opponent is gone.");
        return false;
    }
}
