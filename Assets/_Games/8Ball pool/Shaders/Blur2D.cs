using UnityEngine;

/// <summary>
/// Separable blur for the 8-ball 2D (orthographic) camera, built-in pipeline.
///
/// WHY THIS EXISTS instead of reusing Snooker's BlurOptimized:
/// 1. BlurOptimized depends on the built-in "Hidden/FastBlur", which Unity strips from player
///    builds unless it is in Graphics > Always Included Shaders — hence its Shader.Find recovery
///    hack. GamesPanda/Blur2D is a normal project asset, so it always ships.
/// 2. It is written to survive a camera that has no depth buffer (ZTest Always, ZWrite Off), which
///    is the usual reason an image effect turns an orthographic UI/2D camera black.
///
/// ⚠️ THE BLACK SCREEN IS USUALLY THE CAMERA, NOT THE SHADER. Attaching any OnRenderImage effect
/// forces Unity to render that camera into a RenderTexture and then blit it to the screen. On an
/// OVERLAY camera (Depth > 0) that blit REPLACES the framebuffer, so everything the lower-depth
/// camera drew is gone — you are left with only this camera's own layers over its Clear Flags
/// colour. With Clear Flags = Solid Color that reads as a flat/black screen.
///
/// So put this on the camera that actually renders the content you want blurred:
///   • blurring the whole table view  -> put it on the BASE (lowest depth) camera
///   • blurring only the 2D overlay   -> keep it here, but set that camera's Clear Flags to
///     Depth Only so it composites over the camera beneath instead of erasing it
/// </summary>
[RequireComponent(typeof(Camera))]
[AddComponentMenu("GamesPanda/Blur 2D")]
[ExecuteInEditMode]
public class Blur2D : MonoBehaviour
{
    [Tooltip("Assign GamesPanda/Blur2D. Left empty it is looked up by name once.")]
    public Shader blurShader;

    [Range(0f, 10f)] public float blurSize = 3f;
    [Range(1, 4)]    public int  iterations = 2;
    [Range(1, 4)]    public int  downsample = 1;

    Material _mat;

    Material Mat
    {
        get
        {
            if (_mat == null)
            {
                if (blurShader == null) blurShader = Shader.Find("GamesPanda/Blur2D");
                if (blurShader == null || !blurShader.isSupported) return null;
                _mat = new Material(blurShader) { hideFlags = HideFlags.HideAndDontSave };
            }
            return _mat;
        }
    }

    // Scene-instance handle so the network layer can drop the blur the moment the reconnect/state
    // sync completes (replaces the old black ReconnectStateLoadingOverlay). The blur is authored
    // ENABLED in the scene — code never turns it ON; sync completion only ever turns it OFF.
    static Blur2D sceneInstance;

    void Awake()
    {
        if (Application.isPlaying)
            sceneInstance = this;
    }

    void OnDestroy()
    {
        if (sceneInstance == this)
            sceneInstance = null;
    }

    public static void DisableSyncBlur()
    {
        if (sceneInstance != null)
            sceneInstance.enabled = false;
    }

    void OnDisable()
    {
        if (_mat != null) { DestroyImmediate(_mat); _mat = null; }
    }

    void OnRenderImage(RenderTexture src, RenderTexture dst)
    {
        Material m = Mat;
        // Never leave the screen black: if anything is wrong, pass the frame through untouched.
        if (m == null || blurSize <= 0f || iterations <= 0)
        {
            Graphics.Blit(src, dst);
            return;
        }

        int w = Mathf.Max(1, src.width  / downsample);
        int h = Mathf.Max(1, src.height / downsample);

        // Keep src.format so an HDR-off, no-depth 2D camera round-trips exactly. Depth buffer 0 —
        // a fullscreen blit needs none, and requesting one on a camera that has none is a common
        // cause of a black result.
        RenderTexture a = RenderTexture.GetTemporary(w, h, 0, src.format);
        RenderTexture b = RenderTexture.GetTemporary(w, h, 0, src.format);
        a.filterMode = b.filterMode = FilterMode.Bilinear;

        Graphics.Blit(src, a);
        for (int i = 0; i < iterations; i++)
        {
            // Widen slightly each pass so a few taps still read as a smooth blur.
            m.SetFloat("_BlurSize", blurSize * (1f + i));
            Graphics.Blit(a, b, m, 0);   // horizontal
            Graphics.Blit(b, a, m, 1);   // vertical
        }
        Graphics.Blit(a, dst);

        RenderTexture.ReleaseTemporary(a);
        RenderTexture.ReleaseTemporary(b);
    }
}
