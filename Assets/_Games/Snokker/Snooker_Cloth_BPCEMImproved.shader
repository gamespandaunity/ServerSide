Shader "Snooker/Cloth_BPCEMImproved" {
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
        _DetailsTex ("Detail", 2D) = "white" {}
        _DetailsLighten ("Details Lighten", Range(0, 1)) = 0.5
        _DetailsPow ("Details Contrast", Range(0, 8)) = 1
        _DetailsPow2 ("Details Contrast Light", Range(0, 8)) = 1
        _DiffuseAmount ("Diffuse Amount", Range(0, 1)) = 0.5
        _MainAmount ("Main Amount", Range(0, 1)) = 0.5
        _Amount ("Reflection Amount", Range(0, 3)) = 0.5
        _Cube ("Cubemap", Cube) = "" {}
        _ReflectionColor ("Reflection Color ", Vector) = (1,1,1,1)
        _BBoxMin ("Env Box Min", Vector) = (0,0,0,1)
        _BBoxMax ("Env Box Max", Vector) = (10,10,10,1)
        _EnviCubeMapPos ("Cube Map Pos", Vector) = (0,0,0,1)
        _Roughness ("Roughness", Range(0, 0.99)) = 0
        _FresnelAmount ("Fresnel Amount", Range(0, 5)) = 0.5
        _FresnelFocus ("Fresnel Focus", Range(0, 1)) = 0.5
        _FresnelReflection ("Fresnel Reflection Bottom", Range(0, 1)) = 0.5
        _FresnelReflectionIntensity ("Fresnel Reflection Intensity", Range(0, 8)) = 0.5
    }
    
    SubShader {
        Tags { "RenderType"="Opaque" }
        LOD 200
        
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0
        
        sampler2D _MainTex;
        sampler2D _DetailsTex;
        samplerCUBE _Cube;
        
        float _DetailsLighten;
        float _DetailsPow;
        float _DetailsPow2;
        float _DiffuseAmount;
        float _MainAmount;
        float _Amount;
        float4 _ReflectionColor;
        float4 _BBoxMin;
        float4 _BBoxMax;
        float4 _EnviCubeMapPos;
        float _Roughness;
        float _FresnelAmount;
        float _FresnelFocus;
        float _FresnelReflection;
        float _FresnelReflectionIntensity;
        
        struct Input {
            float2 uv_MainTex;
            float2 uv_DetailsTex;
            float3 worldPos;
            float3 worldNormal;
            float3 viewDir;
            float3 worldRefl;
            INTERNAL_DATA
        };
        
        // Box projection for environment mapping
        float3 BoxProjection(float3 direction, float3 position, float3 cubemapPosition, float3 boxMin, float3 boxMax) {
            if (length(boxMax.xyz - boxMin.xyz) > 0.01) {
                float3 nrdir = normalize(direction);
                float3 rbmax = (boxMax.xyz - position) / nrdir;
                float3 rbmin = (boxMin.xyz - position) / nrdir;
                float3 rbminmax = (nrdir > 0.0) ? rbmax : rbmin;
                float fa = min(min(rbminmax.x, rbminmax.y), rbminmax.z);
                float3 posonbox = position + nrdir * fa;
                return posonbox - cubemapPosition;
            }
            return direction;
        }
        
        // Advanced Fresnel with custom focus and bottom reflection
        float AdvancedFresnel(float cosTheta, float fresnelFocus, float bottomReflection) {
            float f0 = pow(1.0 - fresnelFocus, 2.0) / pow(1.0 + fresnelFocus, 2.0);
            float fresnel = f0 + (1.0 - f0) * pow(1.0 - cosTheta, 5.0);
            
            // Add bottom reflection component
            fresnel = lerp(bottomReflection, fresnel, cosTheta);
            
            return fresnel;
        }
        
        // Detail texture blending with contrast controls
        float3 BlendDetails(float3 baseColor, float3 detailColor, float lighten, float contrast, float contrastLight) {
            // Apply contrast to detail texture
            float3 contrastedDetail = pow(abs(detailColor), contrast);
            
            // Separate contrast for light areas
            float luminance = dot(detailColor, float3(0.299, 0.587, 0.114));
            float3 lightContrastedDetail = pow(abs(detailColor), lerp(contrast, contrastLight, luminance));
            
            // Blend contrasted details
            float3 finalDetail = lerp(contrastedDetail, lightContrastedDetail, luminance);
            
            // Apply lightening
            finalDetail = lerp(finalDetail, float3(1,1,1), lighten);
            
            // Multiply blend with base color
            return baseColor * finalDetail;
        }
        
        void surf (Input IN, inout SurfaceOutputStandard o) {
            // Sample main texture
            fixed4 mainTex = tex2D(_MainTex, IN.uv_MainTex);
            
            // Sample detail texture
            fixed4 detailTex = tex2D(_DetailsTex, IN.uv_DetailsTex);
            
            // Blend main texture with details
            float3 baseColor = BlendDetails(mainTex.rgb, detailTex.rgb, _DetailsLighten, _DetailsPow, _DetailsPow2);
            
            // Apply main amount (controls base color intensity)
            baseColor = lerp(float3(0.5, 0.5, 0.5), baseColor, _MainAmount);
            
            // Calculate view direction and normal
            float3 viewDir = normalize(IN.viewDir);
            float3 worldNormal = WorldNormalVector(IN, o.Normal);
            
            // Calculate advanced fresnel
            float ndotv = saturate(dot(worldNormal, viewDir));
            float fresnel = AdvancedFresnel(ndotv, _FresnelFocus, _FresnelReflection);
            fresnel = saturate(fresnel * _FresnelAmount);
            
            // Calculate reflection direction with box projection
            float3 worldRefl = reflect(-viewDir, worldNormal);
            float3 correctedRefl = BoxProjection(worldRefl, IN.worldPos, _EnviCubeMapPos.xyz, _BBoxMin.xyz, _BBoxMax.xyz);
            
            // Sample environment map with roughness-based mip level
            float mipLevel = _Roughness * 7.0;
            fixed4 envColor = texCUBElod(_Cube, float4(correctedRefl, mipLevel));
            
            // Apply reflection color tint
            float3 tintedReflection = envColor.rgb * _ReflectionColor.rgb;
            
            // Apply fresnel reflection intensity
            tintedReflection = tintedReflection * _FresnelReflectionIntensity;
            
            // Combine base color with reflections
            float3 diffuseColor = lerp(baseColor, baseColor * _DiffuseAmount, 1.0 - _DiffuseAmount);
            float3 finalColor = lerp(diffuseColor, tintedReflection, fresnel * _Amount);
            
            // Set surface properties
            o.Albedo = finalColor;
            o.Metallic = 0.0;
            o.Smoothness = 1.0 - _Roughness;
            o.Alpha = mainTex.a;
        }
        ENDCG
    }
    
    Fallback "Diffuse"
}