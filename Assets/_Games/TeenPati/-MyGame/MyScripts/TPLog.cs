using System.Collections.Generic;
using Mirror;
using UnityEngine;

/// <summary>
/// Single switch for every TeenPatti multiplayer flow log.
/// Every log line looks like:  [TP-MP][CLIENT][GameStartManager] ...
/// so the whole flow can be filtered in the console with "TP-MP".
/// Set <see cref="Enabled"/> to false to silence all of them at once.
/// </summary>
public static class TPLog
{
    public static bool Enabled = true;

    // Remembers the last text logged per key so per-frame conditions
    // (Update loops) only print when something actually changes.
    static readonly Dictionary<string, string> lastByKey = new Dictionary<string, string>();

    static string Side()
    {
        if (NetworkServer.active && NetworkClient.active) return "HOST";
        if (NetworkServer.active) return "SERVER";
        if (NetworkClient.active) return "CLIENT";
        return "LOCAL";
    }

    static string Master()
    {
        MirrorNetwork mn = MirrorNetwork.Instance;
        if (mn == null) return "?";
        return mn.isMasterClient ? "M" : "m";
    }

    static string Line(string where, string what)
    {
        return "[TP-MP][" + Side() + Master() + "][" + where + "] " + what;
    }

    /// <summary>Normal one-shot flow log.</summary>
    public static void Flow(string where, string what)
    {
        if (!Enabled) return;
        Debug.Log(Line(where, what));
    }

    /// <summary>Something unexpected / a flow that got blocked.</summary>
    public static void Warn(string where, string what)
    {
        if (!Enabled) return;
        Debug.LogWarning(Line(where, what));
    }

    /// <summary>
    /// For conditions inside Update()/coroutine loops: prints only when the
    /// message for this key changes, so the console does not flood every frame.
    /// </summary>
    public static void Change(string where, string key, string what)
    {
        if (!Enabled) return;
        string previous;
        if (lastByKey.TryGetValue(key, out previous) && previous == what) return;
        lastByKey[key] = what;
        Debug.Log(Line(where, what));
    }
}
