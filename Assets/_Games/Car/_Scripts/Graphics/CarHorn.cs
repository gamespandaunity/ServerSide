using CarRace;
using Mirror;
using UnityEngine;

/// <summary>
/// Car horn. Keyboard H (editor/PC) or call CarHorn.PlayLocal() from a UI button (mobile HUD).
/// Only the car controlled by this device honks.
/// </summary>
public class CarHorn : MonoBehaviour
{
    public AudioClip hornClip;
    [Range(0f, 1f)] public float volume = 0.9f;
    public KeyCode key = KeyCode.H;

    public static CarHorn Local { get; private set; }

    RCC_CarControllerV3 _car;
    NetworkIdentity _net;
    AudioSource _src;

    void Awake()
    {
        _car = GetComponentInParent<RCC_CarControllerV3>();
        _net = GetComponentInParent<NetworkIdentity>();
        _src = gameObject.AddComponent<AudioSource>();
        _src.clip = hornClip;
        _src.playOnAwake = false;
        _src.loop = false;
        _src.spatialBlend = 1f;
        _src.minDistance = 6f;
        _src.maxDistance = 120f;
        _src.dopplerLevel = 0f;
        _src.volume = volume;
    }

    bool IsLocalCar()
    {
        if (_car == null || _car.externalController) return false; // AI never honks by input
        if (_net != null && _net.netId != 0) return _net.isOwned;   // multiplayer: only my car
        return _car.canControl;                                     // offline
    }

    void Update()
    {
        if (!IsLocalCar()) return;
        Local = this;
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(key)) Play();
#endif
    }

    public void Play()
    {
        if (_src != null && hornClip != null && !_src.isPlaying) _src.Play();
    }

    /// Called by CarHornButton (HUD) or any script.
    public static void PlayLocal()
    {
        if (Local != null) Local.Play();
    }

    void OnDestroy()
    {
        if (Local == this) Local = null;
    }
}
