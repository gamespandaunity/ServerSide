using UnityEngine;
using Mirror;

// Zero-setup FPS + PING overlay — no canvas, no TextMesh, no scene references, nothing to wire up.
// Auto-spawns itself once after the first scene loads (RuntimeInitializeOnLoadMethod) when
// ConstantsData_M.MpVerboseLogs (the dev flag, OFF in release) is on, and survives scene loads via
// DontDestroyOnLoad. Draws with OnGUI so it works in any scene with zero setup.
//
// Shows:  FPS (smoothed)  |  current frame ms  |  worst frame spike in the last 2s window
//         (the boundary-shot hitch hunter — a big spike here = the frame that causes ball jerks)
//         |  Mirror RTT ping when connected.
public class FPSDisplay : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoSpawn()
    {
        if (!ConstantsData_M.MpVerboseLogs) return;          // release: dev flag off -> no overlay, no object
        if (Object.FindFirstObjectByType<FPSDisplay>() != null) return;
        var go = new GameObject("FPSDisplay");
        Object.DontDestroyOnLoad(go);
        go.AddComponent<FPSDisplay>();
    }

    private float _smoothedDt = 1f / 60f;   // EMA of the frame time (stable readout, no flicker)
    private float _spikeMs;                 // worst frame collected in the current 2s window
    private float _spikeShownMs;            // worst frame of the LAST window (what's displayed)
    private float _windowEnd;
    private GUIStyle _style;

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;
        _smoothedDt = Mathf.Lerp(_smoothedDt, dt, 0.1f);
        float ms = dt * 1000f;
        if (ms > _spikeMs) _spikeMs = ms;
        if (Time.unscaledTime > _windowEnd)
        {
            _spikeShownMs = _spikeMs;
            _spikeMs = 0f;
            _windowEnd = Time.unscaledTime + 2f;
        }
    }

    private void OnGUI()
    {
        if (_style == null)
        {
            _style = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.Max(9, Screen.height / 72),   // small, unobtrusive
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.LowerRight,
            };
        }
        float fps = 1f / Mathf.Max(_smoothedDt, 0.0001f);
        string ping = NetworkClient.active ? $"{NetworkTime.rtt * 1000.0:F0}ms" : "--";
        string text = $"FPS {fps:F0} | spike {_spikeShownMs:F0}ms | ping {ping}";
        // bottom-right corner, right-aligned
        Rect r = new Rect(Screen.width - 328f, Screen.height - 26f, 320f, 22f);
        // 1px shadow so it reads on grass, pitch, and panels alike
        _style.normal.textColor = Color.black;
        GUI.Label(new Rect(r.x + 1f, r.y + 1f, r.width, r.height), text, _style);
        _style.normal.textColor = (fps < 25f) ? Color.red : ((fps < 45f) ? Color.yellow : Color.green);
        GUI.Label(r, text, _style);
    }
}
