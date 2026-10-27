using System.Collections.Generic;
using Mirror;
using UnityEngine;

/// <summary>
/// Shows the other player the waiting panel as soon as a player's phone goes silent in the middle of a 2-player match,
/// instead of after the transport's 20 s timeout - and takes it away again when that phone talks again.
///
/// History: the KCP timeout went from 5 s to 20 s on 16 Oct (a phone loading the game scene is silent for ~11 s), so a
/// player who lost the network was only noticed after 20 s. The first fix (eefc4d716) DROPPED a connection silent for
/// 5 s; but the dropped phone does not learn of it until its own 20 s timeout, so a 5-7 s internet cut turned into a
/// waiting panel that never ended and a lost match (QA 27 Oct, bugs 1 and 2). Now nothing is dropped: after
/// <see cref="SilentSeconds"/> the others get RpcShowWaitingPanel(true) (the app shows a panel without the 30 s win
/// countdown while both players still count as connected), and RpcShowWaitingPanel(false) the moment the phone is heard
/// again. A phone that stays away is still dropped by the transport at 20 s, which starts the usual reconnect path.
/// Mirror's client pings every 0.1 s, so 5 s of silence on a ready, spawned connection means its network is gone.
/// Tables and 3-4 player matches have their own away handling and are left alone.
/// </summary>
public class SilentConnectionWatch : MonoBehaviour
{
    public const float SilentSeconds = 5f;
    const float CheckEvery = 0.5f;
    float next;
    readonly HashSet<int> silent = new HashSet<int>();
    readonly List<int> gone = new List<int>();

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
        if (TableEnv.IsTable || MatchSeats.Active) return;

        bool changed = false;
        foreach (var conn in NetworkServer.connections.Values)
        {
            if (conn == null || conn is LocalConnectionToClient) continue;
            // In a game scene with a player: ready (not loading a scene) and spawned.
            if (!conn.isReady || conn.identity == null) { if (silent.Remove(conn.connectionId)) changed = true; continue; }
            float quiet = Time.time - conn.lastMessageTime;
            if (quiet > SilentSeconds && silent.Add(conn.connectionId))
            {
                Debug.Log($"[SilentWatch] conn {conn.connectionId} silent for {quiet:0.0} s - waiting panel for the other player");
                changed = true;
            }
            else if (quiet <= SilentSeconds && silent.Remove(conn.connectionId))
            {
                Debug.Log($"[SilentWatch] conn {conn.connectionId} is back - waiting panel off");
                changed = true;
            }
        }
        // Connections that left meanwhile (the transport dropped them: the usual disconnect path takes over).
        gone.Clear();
        foreach (int id in silent) if (!NetworkServer.connections.ContainsKey(id)) gone.Add(id);
        foreach (int id in gone) silent.Remove(id);

        if (!changed || NetworkGameManager.Instance == null) return;
        NetworkGameManager.Instance.RpcShowWaitingPanel(silent.Count > 0);
    }
}
