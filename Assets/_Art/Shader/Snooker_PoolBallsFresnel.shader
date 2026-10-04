Shader "Snooker/PoolBallsFresnel" {
    Properties {
        _MainTex ("Light", 2D) = "white" {}
        _Cube ("Reflection Map", Cube) = "" {}
        _MainAmount ("Main Amount", Range(0, 1)) = 0.5
        _Amount ("Reflection Amount", Range(0, 2)) = 0.5
        _FresnelAmount ("Fresnel Amount", Range(0, 1)) = 0.5
        _FresnelFocus ("Fresnel Focus", Range(0, 1)) = 0.5
        _BBoxMin ("Env Box Min", Vector) = (0,0,0,1)
        _BBoxMax ("Env Box Max", Vector) = (10,10,10,1)
        _EnviCubeMapPos ("Cube Map Pos", Vector) = (0,0,0,1)
        _IntegrateBRDF ("Integrate BRDF", 2D) = "white" {}
        _Roughness ("Roughness", Range(0, 0.99)) = 0
        _UVOffset ("UV Offset", Vector) = (0,0,0,0)
        _UVScale ("UV Scale", Vector) = (1,1,0,0)
        _SampleRedSide ("Sample Red Side", Range(0, 1)) = 0
        _RedSideWidth ("Red Side Width", Range(0, 1)) = 0.2
    }
    
    SubShader {
        Tags { "RenderType"="Opaque" }
        LOD 200
        
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0
        
        sampler2D _MainTex;
        samplerCUBE _Cube;
        sampler2D _IntegrateBRDF;
        
        float _MainAmount;
        float _Amount;
        float _FresnelAmount;
        float _FresnelFocus;
        float4 _BBoxMin;
        float4 _BBoxMax;
        float4 _EnviCubeMapPos;
        float _Roughness;
        float4 _UVOffset;
        float4 _UVScale;
        float _SampleRedSide;
        float _RedSideWidth;
        
        struct Input {
            float2 uv_MainTex;
            float3 worldPos;
            float3 worldNormal;
            float3 viewDir;
            float3 worldRefl;
            INTERNAL_DATA
        };
        
        // Box projection for environment mapping
        float3 BoxProjection(float3 direction, float3 position, float3 cubemapPosition, float3 boxMin, float3 boxMax) {
            // Only perform box projection if we have a valid bounding box
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
        
        // Fresnel calculation using Schlick's approximation
        float FresnelSchlick(float cosTheta, float fresnelFocus) {
            float f0 = pow(1.0 - fresnelFocus, 2.0) / pow(1.0 + fresnelFocus, 2.0);
            return f0 + (1.0 - f0) * pow(1.0 - cosTheta, 5.0);
        }
        
        // Environment BRDF approximation
        float2 EnvBRDFApprox(float roughness, float ndotv) {
            float4 c0 = float4(-1, -0.0275, -0.572, 0.022);
            float4 c1 = float4(1, 0.0425, 1.04, -0.04);
            float4 r = roughness * c0 + c1;
            float a004 = min(r.x * r.x, exp2(-9.28 * ndotv)) * r.x + r.y;
            return float2(-1.04, 1.04) * a004 + r.zw;
        }
        
        void surf (Input IN, inout SurfaceOutputStandard o) {
            // Calculate UV coordinates
            float2 uv = IN.uv_MainTex;
            
            // Apply UV transformations
            uv = uv * _UVScale.xy + _UVOffset.xy;
            
            // Option to force sampling from the red (left) side
            if (_SampleRedSide > 0.5) {
                // Map UV to sample only from the left portion of the texture
                uv.x = uv.x * _RedSideWidth;
            }
            
            // Sample main texture with modified UVs
            fixed4 mainTex = tex2D(_MainTex, uv);
            
            // Calculate view direction and normal
            float3 viewDir = normalize(IN.viewDir);
            float3 worldNormal = WorldNormalVector(IN, o.Normal);
            
            // Calculate fresnel
            float ndotv = saturate(dot(worldNormal, viewDir));
            float fresnel = FresnelSchlick(ndotv, _FresnelFocus);
            fresnel = lerp(1.0, fresnel, _FresnelAmount);
            
            // Calculate reflection direction with box projection
            float3 worldRefl = reflect(-viewDir, worldNormal);
            float3 correctedRefl = BoxProjection(worldRefl, IN.worldPos, _EnviCubeMapPos.xyz, _BBoxMin.xyz, _BBoxMax.xyz);
            
            // Sample environment map with roughness-based mip level
            float mipLevel = _Roughness * 7.0; // Assuming 8 mip levels
            fixed4 envColor = texCUBElod(_Cube, float4(correctedRefl, mipLevel));
            
            // Environment BRDF lookup
            float2 envBRDF = EnvBRDFApprox(_Roughness, ndotv);
            
            // Sample BRDF integration texture if available
            float2 brdfUV = float2(ndotv, _Roughness);
            fixed4 brdfTex = tex2D(_IntegrateBRDF, brdfUV);
            
            // Combine BRDF sources (prefer texture if available, fallback to approximation)
            float2 finalBRDF = (brdfTex.r > 0.01) ? brdfTex.rg : envBRDF;
            
            // Apply BRDF to environment reflection
            float3 envReflection = envColor.rgb * (finalBRDF.x + finalBRDF.y);
            
            // Combine main texture and reflections
            float3 finalColor = lerp(mainTex.rgb, mainTex.rgb * _MainAmount, 1.0 - _MainAmount);
            finalColor = lerp(finalColor, envReflection, fresnel * _Amount);
            
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