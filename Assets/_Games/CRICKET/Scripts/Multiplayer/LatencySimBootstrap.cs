using Mirror;
using UnityEngine;

/// <summary>
/// Acceptance-rig helper (Docs/DETERMINISTIC_LOCKSTEP.md): when ConstantsData_M.useSimulatedLatency is on,
/// wraps the NetworkManager's transport in Mirror's LatencySimulation at boot so a LOCAL 2-editor match
/// behaves like a 250ms-RTT real-network match. Runs before the manager connects (scene-load hook, editor/
/// dev-flag gated); does nothing in release or when the flag is off. If the rewire ever fails on a Mirror
/// version mismatch, add the LatencySimulation component manually on the NetworkManager and point
/// NetworkManager.transport at it — this bootstrap just automates that.
/// </summary>
public static class LatencySimBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (!ConstantsData_M.useSimulatedLatency) return;
        var manager = Object.FindFirstObjectByType<NetworkManager>();
        if (manager == null)
        {
            Debug.LogWarning("[LatencySim] No NetworkManager in the boot scene — will not simulate latency.");
            return;
        }
        if (manager.transport is LatencySimulation)
        {
            Debug.Log("[LatencySim] Already installed.");
            return;
        }
        var inner = manager.transport;
        var sim = manager.gameObject.AddComponent<LatencySimulation>();
        sim.wrap = inner;
        // This Mirror version's LatencySimulation takes latency in MILLISECONDS (one way) + jitter [0..1].
        sim.latency = Mathf.Max(0f, ConstantsData_M.simulatedLatencyMs);
        sim.jitter = 0.05f;
        manager.transport = sim;
        Transport.active = sim;
        Debug.Log($"[LatencySim] Installed — {ConstantsData_M.simulatedLatencyMs:F0}ms each way (~{ConstantsData_M.simulatedLatencyMs * 2f:F0}ms RTT) wrapping {inner.GetType().Name}.");
    }
}
