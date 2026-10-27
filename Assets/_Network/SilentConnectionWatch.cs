using System.Collections.Generic;
using Mirror;
using UnityEngine;

/// <summary>
/// Drops a player whose phone has gone silent in the middle of a match, after <see cref="SilentSeconds"/> instead of
/// the transport's 20 s timeout.
///
/// The KCP timeout went from 5 s to 20 s on 16 Oct (a phone loading the game scene from Addressables is silent for
/// ~11 s, and 5 s turned every load into a disconnect). Since then a player who lost the network was only noticed
/// after 20 s, so the other player's waiting panel came 10-20 s late (QA 27 Oct). Mirror's client pings every 0.1 s,
/// so a ready, in-game connection that has said nothing for 5 s is gone; dropping it here starts the usual
/// disconnect path (waiting panel, reconnect, 30 s). A connection that is loading a scene is not ready, so loads keep
/// the full 20 s.
/// </summary>
public class SilentConnectionWatch : MonoBehaviour
{
    public const float SilentSeconds = 5f;
    const float CheckEvery = 0.5f;
    float next;
    readonly List<NetworkConnectionToClient> drop = new List<NetworkConnectionToClient>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        var go = new GameObject(nameof(SilentConnectionWatch));
        DontDestroyOnLoad(go);
        go.AddComponent<SilentConnectionWatch>();
    }

    void Update()
    {
        if (!NetworkServer.active || Time.unscaledTime < next) return;
        next = Time.unscaledTime + CheckEvery;
        drop.Clear();
        foreach (var conn in NetworkServer.connections.Values)
        {
            if (conn == null || conn is LocalConnectionToClient) continue;
            // In a game scene with a player: ready (not loading) and spawned.
            if (!conn.isReady || conn.identity == null) continue;
            if (Time.time - conn.lastMessageTime > SilentSeconds) drop.Add(conn);
        }
        foreach (var conn in drop)
        {
            Debug.Log($"[SilentWatch] conn {conn.connectionId} silent for {Time.time - conn.lastMessageTime:0.0} s in a match - dropping it now (transport would wait 20 s)");
            conn.Disconnect();
        }
    }
}
