// Self-contained separable Gaussian blur for the BUILT-IN pipeline.
//
// Written for the 8-ball 2D (orthographic overlay) camera. Deliberately does NOT rely on
// Unity's "Hidden/FastBlur": that is a built-in shader which Unity STRIPS from player builds
// unless it is listed in Project Settings > Graphics > Always Included Shaders, which is why the
// existing BlurOptimized has to Shader.Find it back at runtime. This one is a normal asset in the
// project, so it is always included and can be assigned straight into the material slot.
//
// Two passes (horizontal, then vertical) = O(2n) taps instead of O(n^2). Alpha is preserved, so it
// is safe over transparent UI. No depth, no HDR requirement, no _CameraDepthTexture — nothing that
// an orthographic UI/2D camera lacks.
Shader "GamesPanda/Blur2D"
{
    Properties
    {
        _MainTex ("Base (RGB)", 2D) = "white" {}
        _BlurSize ("Blur Size", Float) = 1.0
    }

    CGINCLUDE
    #include "UnityCG.cginc"

    sampler2D _MainTex;
    // .xy = 1/width, 1/height — supplied automatically by Unity for any sampler2D named _MainTex.
    float4    _MainTex_TexelSize;
    float     _BlurSize;

    struct v2f
    {
        float4 pos : SV_POSITION;
        float2 uv  : TEXCOORD0;
    };

    v2f vert (appdata_img v)
    {
        v2f o;
        o.pos = UnityObjectToClipPos(v.vertex);
        o.uv  = v.texcoord;
        return o;
    }

    // 9-tap Gaussian along an arbitrary axis. weights sum to 1.
    fixed4 blur (float2 uv, float2 dir)
    {
        float2 o = dir * _BlurSize;
        fixed4 c = tex2D(_MainTex, uv) * 0.2270270270;
        c += tex2D(_MainTex, uv + o * 1.3846153846) * 0.3162162162;
        c += tex2D(_MainTex, uv - o * 1.3846153846) * 0.3162162162;
        c += tex2D(_MainTex, uv + o * 3.2307692308) * 0.0702702703;
        c += tex2D(_MainTex, uv - o * 3.2307692308) * 0.0702702703;
        return c;
    }

    fixed4 fragH (v2f i) : SV_Target { return blur(i.uv, float2(_MainTex_TexelSize.x, 0)); }
    fixed4 fragV (v2f i) : SV_Target { return blur(i.uv, float2(0, _MainTex_TexelSize.y)); }
    ENDCG

    SubShader
    {
        // Fullscreen blit: no culling, no depth read/write. An overlay 2D camera has no usable
        // depth buffer, so touching ZTest/ZWrite here is what turns these effects black.
        Cull Off  ZWrite Off  ZTest Always

        // Pass 0 — horizontal
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragH
            ENDCG
        }

        // Pass 1 — vertical
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragV
            ENDCG
        }
    }

    Fallback Off
}
