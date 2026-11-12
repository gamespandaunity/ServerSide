using System;
using CarRace;
using Mirror;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Professional, FPS-friendly car lights.
/// - Headlights: ON/OFF via button (ToggleLocalHeadlights). Drives RCC's lowBeamHeadLightsOn, so RCC's real
///   headlight beams (player car only) and the glowing lamp material switch together.
/// - Brake lights: glow bright red while braking (RCC brakeInput / handbrake), dim red as tail lights.
/// Lamps are driven through a MaterialPropertyBlock (_EmissionColor) on the "GamesPanda/Car Light" shader.
/// </summary>
public class CarLightFX : MonoBehaviour
{
    public RCC_CarControllerV3 car;
    public Renderer target;
    public int[] headLightSlots;
    public int[] tailLightSlots;

    [Tooltip("Material slot whose livery texture has painted lamps (Mobile Car Paint _LampMask). -1 = none")]
    public int lampMaskSlot = -1;
    [Tooltip("More material slots that carry painted lamps (e.g. chrome lenses)")]
    public int[] extraLampMaskSlots;
    [ColorUsage(false, true)] public Color paintedHeadColor  = new Color(5f, 4.8f, 4.2f);
    [ColorUsage(false, true)] public Color paintedTailOff    = new Color(0.3f, 0.02f, 0.0f);
    [ColorUsage(false, true)] public Color paintedTailOn     = new Color(1.4f, 0.08f, 0.02f);
    [ColorUsage(false, true)] public Color paintedBrake      = new Color(6f, 0.25f, 0.05f);

    [Header("Glow flares (camera-facing quads)")]
    public Renderer[] headGlows;
    public Renderer[] tailGlows;
    [Tooltip("Fake volumetric beams + road light pool, shown when headlights are on")]
    public Renderer[] headBeams;
    [Tooltip("RCC-style lens flares on the headlights (shown when headlights are on)")]
    public LensFlareComponentSRP[] headFlares;
    public float headGlowIntensity = 1.4f;
    public float tailGlowOff = 0.12f, tailGlowOn = 0.45f, tailGlowBrake = 1.6f;

    [Tooltip("AI cars keep headlights on")]
    public bool headLightsAlwaysOn = false;
    public bool headLightsOnAtStart = false;

    [ColorUsage(false, true)] public Color headLightColor = new Color(6f, 5.8f, 5.2f);
    [ColorUsage(false, true)] public Color tailOffColor   = new Color(0.25f, 0.0f, 0.0f);
    [ColorUsage(false, true)] public Color tailOnColor    = new Color(1.6f, 0.03f, 0.02f);
    [ColorUsage(false, true)] public Color brakeColor     = new Color(8f, 0.2f, 0.06f);

    public static CarLightFX Local { get; private set; }
    public static event Action<bool> LocalHeadlightsChanged;

    static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
    static readonly int IntensityId = Shader.PropertyToID("_Intensity");
    static readonly int HeadLampId = Shader.PropertyToID("_HeadLampColor");
    static readonly int TailLampId = Shader.PropertyToID("_TailLampColor");
    MaterialPropertyBlock _mpb;
    NetworkIdentity _net;
    float _brake = -1f;
    int _headState = -1;

    public bool HeadlightsOn =>
        headLightsAlwaysOn || (car != null && (car.lowBeamHeadLightsOn || car.highBeamHeadLightsOn));

    Transform _follower;

    void Awake()
    {
        CarInputMode.Apply();   // keyboard in Editor/PC, buttons on mobile (also covers hot-update builds)
        DetachVisuals();
        _mpb = new MaterialPropertyBlock();
        if (car == null) car = GetComponentInParent<RCC_CarControllerV3>();
        _net = GetComponentInParent<NetworkIdentity>();
        if (car != null && car.externalController) headLightsAlwaysOn = true; // AI
        if (car != null && !car.externalController) car.lowBeamHeadLightsOn = true;   // player: headlights on at start (button turns off)
        ConfigureHeadlights();
        BuildRCCFlares();
        Apply(0f, HeadlightsOn);
    }

    bool IsLocalCar()
    {
        if (car == null || car.externalController) return false;
        if (_net != null && _net.netId != 0) return _net.isOwned;
        return car.canControl;
    }

    void Update()
    {
        if (car == null || target == null) return;

        if (IsLocalCar() && Local != this)
        {
            Local = this;
            LocalHeadlightsChanged?.Invoke(HeadlightsOn);
        }

        float wanted = (car.brakeInput >= 0.1f || car.handbrakeInput >= 0.5f) ? 1f : 0f;
        float b = Mathf.MoveTowards(Mathf.Max(_brake, 0f), wanted, Time.deltaTime * 14f);
        bool head = HeadlightsOn;
        if (!Mathf.Approximately(b, _brake) || (head ? 1 : 0) != _headState) Apply(b, head);
    }

    /// RCC-style headlight setup at runtime, so cars look right even if the editor tool was not re-run.
    void ConfigureHeadlights()
    {
        bool ai = headLightsAlwaysOn;
        var root = car != null ? car.transform : transform;
        foreach (var l in root.GetComponentsInChildren<RCC_Light>(true))
        {
            if (l.lightType != RCC_Light.LightType.HeadLight && l.lightType != RCC_Light.LightType.HighBeamHeadLight) continue;
            var lt = l.GetComponent<Light>();
            if (lt == null || lt.type != UnityEngine.LightType.Spot) continue;
            l.defaultIntensity = 6f;
            lt.color = new Color(1f, 0.97f, 0.92f);
            lt.innerSpotAngle = 45f;
            lt.shadows = LightShadows.None;
            if (!ai) lt.renderMode = LightRenderMode.ForcePixel;
        }
        if (headFlares == null) return;
        foreach (var f in headFlares)
        {
            if (f == null) continue;
            f.attenuationByLightShape = false;   // visible from front + sides
            f.useOcclusion = true;
            f.occlusionOffset = 0.35f;           // test outside the lamp glass, not inside the body mesh
            f.occlusionRadius = 0.06f;
            f.sampleCount = 8;
            f.enabled = false;                   // replaced by the RCC star flare below (works on every camera setup)
        }
        headFlares = null;
    }

    static Mesh _quad;
    static Material _flareMat;

    /// RCC look: a bright star flare on each headlight (camera-facing, visible from the front/sides, hidden by the
    /// body from behind). Built at runtime so every car gets it without re-running editor tools.
    void BuildRCCFlares()
    {
        var root = car != null ? car.transform : transform;
        var lamps = new System.Collections.Generic.List<Transform>();
        foreach (var l in root.GetComponentsInChildren<RCC_Light>(true))
            if (l.lightType == RCC_Light.LightType.HeadLight && l.GetComponentInParent<RCC_PoliceSiren>() == null) lamps.Add(l.transform);
        if (lamps.Count == 0) return;

        var mat = FlareMaterial();
        if (mat == null) return;
        if (_quad == null)
        {
            _quad = new Mesh { name = "RCCFlareQuad" };
            _quad.vertices = new[] { new Vector3(-0.5f, -0.5f), new Vector3(0.5f, -0.5f), new Vector3(-0.5f, 0.5f), new Vector3(0.5f, 0.5f) };
            _quad.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) };
            _quad.triangles = new[] { 0, 2, 1, 2, 3, 1 };
            _quad.bounds = new Bounds(Vector3.zero, Vector3.one * 4f);
        }
        var parent = _follower != null ? _follower : transform;
        var list = new System.Collections.Generic.List<Renderer>();
        if (headGlows != null) foreach (var g in headGlows) if (g != null) list.Add(g);
        foreach (var lamp in lamps)
        {
            var go = new GameObject("RCCFlare");
            go.transform.SetPositionAndRotation(lamp.position + lamp.forward * 0.08f, lamp.rotation);
            go.transform.SetParent(parent, true);
            go.AddComponent<MeshFilter>().sharedMesh = _quad;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.lightProbeUsage = LightProbeUsage.Off;
            r.reflectionProbeUsage = ReflectionProbeUsage.Off;
            list.Add(r);
        }
        headGlows = list.ToArray();
    }

    Material FlareMaterial()
    {
        if (_flareMat != null) return _flareMat;
        Shader sh = null;
        if (tailGlows != null)
            foreach (var g in tailGlows) if (g != null && g.sharedMaterial != null) { sh = g.sharedMaterial.shader; break; }
        if (sh == null) sh = Shader.Find("GamesPanda/Car Light Glow");
        if (sh == null) return null;
        _flareMat = new Material(sh) { name = "RCC_HeadFlare", enableInstancing = true };
        _flareMat.SetColor("_Color", new Color(1f, 0.96f, 0.88f));
        _flareMat.SetFloat("_Size", 1.6f);
        _flareMat.SetFloat("_Intensity", 1f);
        _flareMat.SetFloat("_FacingPower", 2.2f);
        _flareMat.SetFloat("_DepthOffset", 0.35f);
        _flareMat.SetFloat("_Star", 1f);
        _flareMat.SetFloat("_DistScale", 1f);
        return _flareMat;
    }

    public void SetHeadlights(bool on)
    {
        if (car == null) return;
        car.lowBeamHeadLightsOn = on;
        car.highBeamHeadLightsOn = false;
        Apply(Mathf.Max(_brake, 0f), HeadlightsOn);
        if (Local == this) LocalHeadlightsChanged?.Invoke(HeadlightsOn);
    }

    public void ToggleHeadlights() => SetHeadlights(!HeadlightsOn);

    /// For HUD button.
    public static void ToggleLocalHeadlights()
    {
        if (Local != null) Local.ToggleHeadlights();
    }

    void Apply(float brake, bool head)
    {
        _brake = brake;
        _headState = head ? 1 : 0;
        if (target == null) return;
        if (headLightSlots != null)
            foreach (int i in headLightSlots) Set(i, head ? headLightColor : Color.black);
        Color tail = Color.Lerp(head ? tailOnColor : tailOffColor, brakeColor, brake);
        if (tailLightSlots != null)
            foreach (int i in tailLightSlots) Set(i, tail);

        Color headC = head ? paintedHeadColor : Color.black;
        Color tailC = Color.Lerp(head ? paintedTailOn : paintedTailOff, paintedBrake, brake);
        SetLamps(lampMaskSlot, headC, tailC);
        if (extraLampMaskSlots != null)
            foreach (int i in extraLampMaskSlots) SetLamps(i, headC, tailC);

        if (headGlows != null)
            foreach (var g in headGlows)
                if (g != null) { g.enabled = head; Glow(g, headGlowIntensity); }
        if (headBeams != null)
            foreach (var g in headBeams)
                if (g != null) g.enabled = head;
        if (headFlares != null)
            foreach (var f in headFlares)
                if (f != null) f.enabled = head;
        if (tailGlows != null)
        {
            float t = Mathf.Lerp(head ? tailGlowOn : tailGlowOff, tailGlowBrake, brake);
            foreach (var g in tailGlows)
                if (g != null) { g.enabled = t > 0.01f; Glow(g, t); }
        }
    }

    void SetLamps(int slot, Color headC, Color tailC)
    {
        if (slot < 0 || slot >= target.sharedMaterials.Length) return;
        target.GetPropertyBlock(_mpb, slot);
        _mpb.SetColor(HeadLampId, headC);
        _mpb.SetColor(TailLampId, tailC);
        target.SetPropertyBlock(_mpb, slot);
    }

    void Glow(Renderer r, float intensity)
    {
        r.GetPropertyBlock(_mpb);
        _mpb.SetFloat(IntensityId, intensity);
        r.SetPropertyBlock(_mpb);
    }

    void Set(int slot, Color c)
    {
        if (slot < 0 || slot >= target.sharedMaterials.Length) return;
        target.GetPropertyBlock(_mpb, slot);
        _mpb.SetColor(EmissionId, c);
        target.SetPropertyBlock(_mpb, slot);
    }

    /// Beams/glows are moved out of the car hierarchy at runtime so the RCC camera's
    /// TPSAutoFocus (bounds of all car renderers) keeps the original camera distance/height.
    void DetachVisuals()
    {
        Transform beams = transform.Find("HeadlightBeams");
        Transform glows = transform.Find("LightGlows");
        if (beams == null && glows == null) return;

        var go = new GameObject(name + "_LightFX");
        _follower = go.transform;
        _follower.SetPositionAndRotation(transform.position, transform.rotation);
        _follower.localScale = transform.lossyScale;
        if (beams != null) beams.SetParent(_follower, false);
        if (glows != null) glows.SetParent(_follower, false);
        go.AddComponent<CarLightFollow>().target = transform;
    }

    void OnEnable()
    {
        if (_follower != null) _follower.gameObject.SetActive(true);
    }

    void OnDisable()
    {
        if (_follower != null) _follower.gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        if (Local == this) Local = null;
        if (_follower != null) Destroy(_follower.gameObject);
    }
}
