// GamesPanda - Car Light Glow (URP)
// Camera-facing additive flare for head/brake lights. Visible mainly from the side the lamp faces
// (like a real lamp), pulled slightly toward the camera so the car body does not hide it.
// Very cheap: one quad, no texture, no realtime light.
Shader "GamesPanda/Car Light Glow"
{
    Properties
    {
        [HDR] _Color("Glow Color", Color) = (1, 0.95, 0.85, 1)
        _Size("Size (m)", Float) = 1.0
        _Intensity("Intensity", Range(0, 4)) = 1
        _FacingPower("Facing Falloff", Range(0, 8)) = 2
        _DepthOffset("Pull To Camera (m)", Float) = 0.3
        _Star("RCC Star Flare (rays)", Range(0, 2)) = 0
        _DistScale("Keep Screen Size With Distance", Range(0, 1)) = 0
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent+50" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Pass
        {
            Name "Glow"
            Tags { "LightMode" = "UniversalForward" }
            Blend One One
            ZWrite Off
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _Size;
                half _Intensity;
                half _FacingPower;
                float _DepthOffset;
                half _Star;
                float _DistScale;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; half fade : TEXCOORD1; half fog : TEXCOORD2; };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                float3 centerWS = TransformObjectToWorld(float3(0, 0, 0));
                float3 fwd = normalize(TransformObjectToWorldDir(float3(0, 0, 1)));
                float3 toCam = normalize(_WorldSpaceCameraPos - centerWS);
                OUT.fade = pow(saturate(dot(fwd, toCam) * 0.85 + 0.15), _FacingPower);

                float3 viewPos = TransformWorldToView(centerWS);
                float dist = -viewPos.z;
                float sizeMul = lerp(1.0, clamp(dist * 0.06, 0.7, 4.0), _DistScale);   // RCC flares stay readable far away
                viewPos.xy += IN.positionOS.xy * _Size * sizeMul;
                viewPos.z += _DepthOffset;                      // toward camera (view space looks down -Z)
                OUT.positionCS = TransformWViewToHClip(viewPos);
                OUT.uv = IN.uv;
                OUT.fog = ComputeFogFactor(OUT.positionCS.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float d = length(IN.uv - 0.5) * 2.0;
                half core = saturate(1.0 - d);
                half g = core * core * (0.6 + 0.4 * core * core);   // soft halo + bright core
                if (_Star > 0.001)
                {
                    // RCC-style star: hot centre + long horizontal streak + short vertical streak
                    float2 p = (IN.uv - 0.5) * 2.0;
                    half hot = pow(saturate(1.0 - d * 2.2), 3.0) * 1.5;
                    half hRay = pow(saturate(1.0 - abs(p.x)), 3.0) * exp(-abs(p.y) * 55.0);
                    half vRay = pow(saturate(1.0 - abs(p.y) * 1.6), 3.0) * exp(-abs(p.x) * 70.0) * 0.45;
                    g = g * 0.55 + (hot + hRay + vRay) * _Star;
                }
                half3 col = _Color.rgb * g * _Intensity * IN.fade;
                col *= ComputeFogIntensity(IN.fog);                  // fade into fog (additive)
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}
