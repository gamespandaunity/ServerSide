#if UNITY_SERVER
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling;

// Opt-in release-player diagnostics; no object scan on normal match servers.
public sealed class ServerMemoryDiagnostics : MonoBehaviour
{
    [Serializable]
    private sealed class Entry
    {
        public string name;
        public string type;
        public int count;
        public long bytes;
    }

    [Serializable]
    private sealed class Report
    {
        public long monoUsed;
        public long monoHeap;
        public long unityAllocated;
        public long unityReserved;
        public int loadedObjects;
        public Entry[] types;
        public Entry[] largestObjects;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        if (!Environment.GetCommandLineArgs().Contains("-serverMemoryReport")) return;
        var host = new GameObject("ServerMemoryDiagnostics");
        DontDestroyOnLoad(host);
        host.AddComponent<ServerMemoryDiagnostics>();
    }

    private IEnumerator Start()
    {
        yield return new WaitForSecondsRealtime(10);
        var report = new Report
        {
            monoUsed = Profiler.GetMonoUsedSizeLong(),
            monoHeap = Profiler.GetMonoHeapSizeLong(),
            unityAllocated = Profiler.GetTotalAllocatedMemoryLong(),
            unityReserved = Profiler.GetTotalReservedMemoryLong()
        };
        UnityEngine.Object[] objects = Resources.FindObjectsOfTypeAll<UnityEngine.Object>();
        report.loadedObjects = objects.Length;
        var entries = new List<Entry>(objects.Length);
        foreach (UnityEngine.Object obj in objects)
        {
            if (obj == null) continue;
            entries.Add(new Entry { name = obj.name, type = obj.GetType().Name,
                count = 1, bytes = Profiler.GetRuntimeMemorySizeLong(obj) });
        }
        report.types = entries.GroupBy(e => e.type).Select(g => new Entry
        {
            type = g.Key, count = g.Count(), bytes = g.Sum(e => e.bytes)
        }).OrderByDescending(e => e.bytes).Take(20).ToArray();
        report.largestObjects = entries.OrderByDescending(e => e.bytes).Take(20).ToArray();
        Debug.Log("[ServerMemoryReport] " + JsonUtility.ToJson(report));
        Destroy(gameObject);
    }
}
#endif
