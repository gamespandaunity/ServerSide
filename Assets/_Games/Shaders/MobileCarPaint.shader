// GamesPanda - Mobile Car Paint (URP)
// Lightweight car paint for mobile: PBR base + clear coat (fresnel reflection + sharp sun highlight)
// + optional procedural metallic flakes + optional custom cubemap + emission for head/tail lights.
Shader "GamesPanda/Mobile Car Paint"
{
    Properties
    {
        [MainColor] _BaseColor("Paint Color", Color) = (0.75, 0.05, 0.05, 1)
        [MainTexture] _BaseMap("Albedo (RGB)", 2D) = "white" {}
        [HideInInspector] _Color("Legacy Color (UI color picker)", Color) = (0.75, 0.05, 0.05, 1)
        [NoScaleOffset] _PaintMask("Paint Mask (R = paintable body)", 2D) = "white" {}
        _Metallic("Metallic", Range(0, 1)) = 0.7
        _Smoothness("Base Smoothness", Range(0, 1)) = 0.6

        [Header(Clear Coat)]
        _ClearCoat("Clear Coat Strength", Range(0, 1)) = 1
        _ClearCoatSmoothness("Clear Coat Smoothness", Range(0, 1)) = 0.95
        _FresnelPower("Fresnel Power", Range(1, 8)) = 4
        _ReflectionBoost("Reflection Boost", Range(0, 3)) = 1.2

        [Header(Metallic Flakes)]
        [Toggle(_FLAKES_ON)] _Flakes("Enable Flakes", Float) = 0
        _FlakeColor("Flake Color", Color) = (1, 1, 1, 1)
        _FlakeTiling("Flake Tiling", Float) = 80
        _FlakeStrength("Flake Strength", Range(0, 1)) = 0.35

        [Header(Custom Reflection)]
        [Toggle(_CUSTOM_CUBE_ON)] _UseCube("Use Custom Cubemap", Float) = 0
        [NoScaleOffset] _ReflectionCube("Reflection Cubemap", Cube) = "" {}

        [Header(Emission)]
        [Toggle(_EMISSION)] _UseEmission("Enable Emission", Float) = 0
        [NoScaleOffset] _EmissionMap("Emission Mask", 2D) = "white" {}
        [HDR] _EmissionColor("Emission Color", Color) = (0, 0, 0, 1)

        [Header(Painted Lamps (driven by CarLightFX))]
        [NoScaleOffset] _LampMask("Lamp Mask (R head, G tail)", 2D) = "black" {}
        [HDR] _HeadLampColor("Head Lamp Glow", Color) = (0, 0, 0, 1)
        [HDR] _TailLampColor("Tail Lamp Glow", Color) = (0, 0, 0, 1)
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }
        LOD 300

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half _Metallic;
            half _Smoothness;
            half _ClearCoat;
            half _ClearCoatSmoothness;
            half _FresnelPower;
            half _ReflectionBoost;
            half4 _FlakeColor;
            float _FlakeTiling;
            half _FlakeStrength;
            half4 _EmissionColor;
            half4 _HeadLampColor;
            half4 _TailLampColor;
        CBUFFER_END
        ENDHLSL

        // ---------------------------------------------------------------- Forward
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #pragma shader_feature_local _FLAKES_ON
            #pragma shader_feature_local_fragment _CUSTOM_CUBE_ON
            #pragma shader_feature_local_fragment _EMISSION

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);        SAMPLER(sampler_BaseMap);
            TEXTURE2D(_EmissionMap);    SAMPLER(sampler_EmissionMap);
            TEXTURECUBE(_ReflectionCube); SAMPLER(sampler_ReflectionCube);
            TEXTURE2D(_LampMask);       SAMPLER(sampler_LampMask);
            TEXTURE2D(_PaintMask);      SAMPLER(sampler_PaintMask);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS   : TEXCOORD2;
                half   fogFactor  : TEXCOORD3;
            #ifdef _ADDITIONAL_LIGHTS_VERTEX
                half3  vertexLight : TEXCOORD4;
            #endif
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                VertexPositionInputs pos = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionCS = pos.positionCS;
                OUT.positionWS = pos.positionWS;
                OUT.normalWS   = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv         = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.fogFactor  = ComputeFogFactor(pos.positionCS.z);
            #ifdef _ADDITIONAL_LIGHTS_VERTEX
                OUT.vertexLight = VertexLighting(pos.positionWS, OUT.normalWS);
            #endif
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

                // Paint colour only where the mask says "body" (stripes, numbers, lamps keep their livery colour)
                half paintMask = SAMPLE_TEXTURE2D(_PaintMask, sampler_PaintMask, IN.uv).r;
                half3 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv).rgb * lerp(half3(1, 1, 1), _BaseColor.rgb, paintMask);
                float3 N = normalize(IN.normalWS);
                float3 V = GetWorldSpaceNormalizeViewDir(IN.positionWS);

                InputData inputData = (InputData)0;
                inputData.positionWS = IN.positionWS;
                inputData.normalWS = N;
                inputData.viewDirectionWS = V;
                inputData.shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                inputData.fogCoord = IN.fogFactor;
                inputData.bakedGI = SampleSH(N);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(IN.positionCS);
                inputData.shadowMask = half4(1, 1, 1, 1);
            #ifdef _ADDITIONAL_LIGHTS_VERTEX
                inputData.vertexLighting = IN.vertexLight;
            #endif

                SurfaceData surf = (SurfaceData)0;
                surf.albedo = albedo;
                surf.metallic = _Metallic;
                surf.smoothness = _Smoothness;
                surf.occlusion = 1.0h;
                surf.alpha = 1.0h;
                surf.normalTS = half3(0, 0, 1);
            #ifdef _EMISSION
                surf.emission = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, IN.uv).rgb * _EmissionColor.rgb;
            #endif

                // Base paint layer (URP PBR: sun, shadows, ambient, probes, extra lights)
                half4 color = UniversalFragmentPBR(inputData, surf);

                // ---------------- Clear coat layer ----------------
                half NdotV = saturate(dot(N, V));
                half fresnel = pow(1.0h - NdotV, _FresnelPower);
                half coatF = lerp(0.04h, 1.0h, fresnel) * _ClearCoat;
                half coatRoughness = 1.0h - _ClearCoatSmoothness;
                float3 R = reflect(-V, N);

            #ifdef _CUSTOM_CUBE_ON
                half mip = PerceptualRoughnessToMipmapLevel(coatRoughness);
                half3 env = SAMPLE_TEXTURECUBE_LOD(_ReflectionCube, sampler_ReflectionCube, R, mip).rgb;
            #else
                half3 env = GlossyEnvironmentReflection(R, IN.positionWS, coatRoughness, 1.0h);
            #endif

                Light mainLight = GetMainLight(inputData.shadowCoord, IN.positionWS, inputData.shadowMask);
                half3 lightCol = mainLight.color * (mainLight.shadowAttenuation * mainLight.distanceAttenuation)
                                 * saturate(dot(N, mainLight.direction));
                float3 H = SafeNormalize(mainLight.direction + V);
                half NdotH = saturate(dot(N, H));
                half specPow = exp2(10.0h * _ClearCoatSmoothness + 1.0h);
                half coatSpec = pow(NdotH, specPow) * (specPow + 8.0h) * 0.0398h; // ~ (n+8)/(8*pi)
                half3 coat = env * _ReflectionBoost * coatF + min(coatSpec, 30.0h) * lightCol * _ClearCoat;

                color.rgb = color.rgb * (1.0h - coatF) + coat;

                // ---------------- Metallic flakes ----------------
            #ifdef _FLAKES_ON
                float2 cell = floor(IN.uv * _FlakeTiling);
                float h1 = Hash21(cell);
                float h2 = Hash21(cell + 17.31);
                float h3 = Hash21(cell + 41.77);
                float3 flakeN = normalize(N + (float3(h1, h2, h3) - 0.5) * 0.8);
                half sparkle = pow(saturate(dot(flakeN, H)), 48.0h) * step(0.55, h1);
                color.rgb += _FlakeColor.rgb * sparkle * _FlakeStrength * lightCol * 4.0h;
            #endif

                // Lamps painted in the livery texture glow when CarLightFX switches them on
                half2 lamp = SAMPLE_TEXTURE2D(_LampMask, sampler_LampMask, IN.uv).rg;
                color.rgb += lamp.r * _HeadLampColor.rgb + lamp.g * _TailLampColor.rgb;

                color.rgb = MixFog(color.rgb, inputData.fogCoord);
                color.a = 1.0h;
                return color;
            }
            ENDHLSL
        }

        // ---------------------------------------------------------------- Shadow caster
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings ShadowVert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(IN.normalOS);
            #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                float3 lightDirectionWS = normalize(_LightPosition - positionWS);
            #else
                float3 lightDirectionWS = _LightDirection;
            #endif
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
            #if UNITY_REVERSED_Z
                positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
            #else
                positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
            #endif
                OUT.positionCS = positionCS;
                return OUT;
            }

            half4 ShadowFrag(Varyings IN) : SV_Target { return 0; }
            ENDHLSL
        }

        // ---------------------------------------------------------------- Depth only
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R
            Cull Back

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings DepthVert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                return OUT;
            }

            half DepthFrag(Varyings IN) : SV_Target { return IN.positionCS.z; }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
