using CarRace;
using UnityEngine;

/// <summary>
/// Chooses the RCC control mode by platform at runtime:
///   Unity Editor / PC  -> keyboard (W/S/A/D or arrows, Space handbrake, F boost, L lights, H horn)
///   Android / iOS      -> on-screen mobile buttons
/// The RCC_Settings asset is NOT changed permanently (in the Editor the original value is restored
/// when Play mode ends), so mobile builds keep working exactly as before.
/// Editor override: Tools > Car Graphics > Editor Input: Use Mobile Buttons.
/// </summary>
public static class CarInputMode
{
    public const string EditorPrefKey = "CarInput_MobileButtonsInEditor";

#if UNITY_EDITOR
    static bool _hooked;
    static bool _original;
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init() => Apply();

    public static bool WantMobileButtons()
    {
#if UNITY_EDITOR
        return UnityEditor.EditorPrefs.GetBool(EditorPrefKey, false);
#else
        return Application.isMobilePlatform;
#endif
    }

    /// Safe to call many times (also called by every car on spawn).
    public static void Apply()
    {
        var settings = RCC_Settings.Instance;
        if (settings == null) return;
        bool mobile = WantMobileButtons();
        if (settings.mobileControllerEnabled == mobile) return;

#if UNITY_EDITOR
        if (!_hooked)
        {
            _hooked = true;
            _original = settings.mobileControllerEnabled;
            UnityEditor.EditorApplication.playModeStateChanged += state =>
            {
                if (state == UnityEditor.PlayModeStateChange.EnteredEditMode && settings != null)
                    settings.mobileControllerEnabled = _original;   // never leave the asset changed
            };
        }
#endif
        settings.mobileControllerEnabled = mobile;
        Debug.Log("[CarInput] Controls: " + (mobile ? "mobile buttons" : "keyboard"));
    }
}
