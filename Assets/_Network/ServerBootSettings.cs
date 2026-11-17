using System;
using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.SceneManagement;

// Keeps the global timing previously initialized by the car UI/settings graph,
// without loading that graph for every board-game server.
public sealed class ServerBootSettings : MonoBehaviour
{
    public bool overrideFixedTimeStep;
    public float fixedTimeStep;
    public bool overrideFrameRate;
    public int frameRate;

#if UNITY_STANDALONE_LINUX && !UNITY_EDITOR
    [DllImport("libc.so.6", EntryPoint = "malloc_trim")]
    private static extern int TrimHeap(UIntPtr pad);
#endif

    private void Awake()
    {
        if (overrideFixedTimeStep) Time.fixedDeltaTime = fixedTimeStep;
        if (overrideFrameRate) Application.targetFrameRate = frameRate;
        DontDestroyOnLoad(gameObject);
    }

    private IEnumerator Start()
    {
        // Reclaim temporary boot allocations once, while the lobby is empty.
        yield return new WaitUntil(() => SceneManager.GetActiveScene().name == "Home" &&
            Mirror.NetworkServer.connections.Count == 0);
        yield return Resources.UnloadUnusedAssets();
        if (SceneManager.GetActiveScene().name == "Home" && Mirror.NetworkServer.connections.Count == 0)
        {
            GC.Collect();
#if UNITY_STANDALONE_LINUX && !UNITY_EDITOR
            TrimHeap(UIntPtr.Zero);
#endif
        }
        Destroy(gameObject);
    }
}
