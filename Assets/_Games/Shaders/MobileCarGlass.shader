// GamesPanda - Mobile Car Glass (URP)
// Cheap transparent tinted glass: fresnel reflection (probes) + sharp sun highlight. No depth write.
Shader "GamesPanda/Mobile Car Glass"
{
    Properties
    {
        [MainColor] _BaseColor("Tint (Alpha = opacity)", Color) = (0.04, 0.06, 0.08, 0.55)
        _Smoothness("Smoothness", Range(0, 1)) = 0.95
        _FresnelPower("Fresnel Power", Range(1, 8)) = 3
        _ReflectionBoost("Reflection Boost", Range(0, 3)) = 1.3
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" "IgnoreProjector" = "True" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Blend One OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half _Smoothness;
                half _FresnelPower;
                half _ReflectionBoost;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                half   fogFactor  : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                VertexPositionInputs pos = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionCS = pos.positionCS;
                OUT.positionWS = pos.positionWS;
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.fogFactor = ComputeFogFactor(pos.positionCS.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

                float3 N = normalize(IN.normalWS);
                float3 V = GetWorldSpaceNormalizeViewDir(IN.positionWS);
                half NdotV = saturate(dot(N, V));
                half fres = pow(1.0h - NdotV, _FresnelPower);

                float3 R = reflect(-V, N);
                half3 env = GlossyEnvironmentReflection(R, IN.positionWS, 1.0h - _Smoothness, 1.0h);

                Light mainLight = GetMainLight(TransformWorldToShadowCoord(IN.positionWS));
                float3 H = SafeNormalize(mainLight.direction + V);
                half specPow = exp2(10.0h * _Smoothness + 1.0h);
                half spec = pow(saturate(dot(N, H)), specPow) * (specPow + 8.0h) * 0.0398h;
                half3 sun = min(spec, 30.0h) * mainLight.color * mainLight.shadowAttenuation * saturate(dot(N, mainLight.direction));

                half alpha = saturate(_BaseColor.a + fres * (1.0h - _BaseColor.a));
                half3 col = SampleSH(N) * _BaseColor.rgb * _BaseColor.a
                          + env * _ReflectionBoost * lerp(0.06h, 1.0h, fres)
                          + sun;
                col = MixFog(col, IN.fogFactor);
                return half4(col, alpha);
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
