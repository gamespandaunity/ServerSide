Shader "Custom/WaterLance"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
        _NoiseTex ("Noise Texture", 2D) = "white" {}
        _Color ("Water Color", Color) = (0.2, 0.6, 1.0, 1.0)
        _CoreColor ("Core Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _Intensity ("Intensity", Range(1, 10)) = 3.0
        _FlowSpeed ("Flow Speed", Range(0, 5)) = 2.0
        _NoiseScale ("Noise Scale", Range(0.1, 2)) = 1.0
        _CoreWidth ("Core Width", Range(0.1, 0.8)) = 0.3
        _EdgeSoftness ("Edge Softness", Range(0.01, 0.5)) = 0.1
        _Distortion ("Distortion Strength", Range(0, 0.2)) = 0.05
        _PulseFreq ("Pulse Frequency", Range(0, 10)) = 2.0
        _PulseAmp ("Pulse Amplitude", Range(0, 0.5)) = 0.1
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType"="Transparent" 
            "Queue"="Transparent"
            "IgnoreProjector"="True"
        }
        
        LOD 200
        
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };
            
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float3 worldPos : TEXCOORD1;
                UNITY_FOG_COORDS(2)
            };
            
            sampler2D _MainTex;
            sampler2D _NoiseTex;
            float4 _MainTex_ST;
            float4 _NoiseTex_ST;
            
            fixed4 _Color;
            fixed4 _CoreColor;
            float _Intensity;
            float _FlowSpeed;
            float _NoiseScale;
            float _CoreWidth;
            float _EdgeSoftness;
            float _Distortion;
            float _PulseFreq;
            float _PulseAmp;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                UNITY_TRANSFER_FOG(o, o.vertex);
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float time = _Time.y;
                
                // Create flowing UV coordinates
                float2 flowUV = uv;
                flowUV.x += time * _FlowSpeed;
                
                // Sample noise for distortion and variation
                float2 noiseUV = uv * _NoiseScale;
                noiseUV.x += time * _FlowSpeed * 0.5;
                noiseUV.y += time * 0.3;
                
                float4 noise = tex2D(_NoiseTex, noiseUV);
                
                // Apply distortion to create turbulent water effect
                flowUV.y += (noise.r - 0.5) * _Distortion;
                flowUV.x += (noise.g - 0.5) * _Distortion * 0.5;
                
                // Sample main texture with flowing UVs
                float4 mainTex = tex2D(_MainTex, flowUV);
                
                // Create radial gradient from center (water lance beam shape)
                float distFromCenter = abs(uv.y - 0.5) * 2.0;
                
                // Add pulse effect
                float pulse = sin(time * _PulseFreq + uv.x * 10.0) * _PulseAmp + 1.0;
                distFromCenter /= pulse;
                
                // Create core and edge regions
                float coreAlpha = 1.0 - smoothstep(_CoreWidth - _EdgeSoftness, _CoreWidth, distFromCenter);
                float edgeAlpha = 1.0 - smoothstep(1.0 - _EdgeSoftness, 1.0, distFromCenter);
                
                // Combine core and edge colors
                fixed4 coreCol = _CoreColor * coreAlpha;
                fixed4 edgeCol = _Color * (edgeAlpha - coreAlpha);
                fixed4 finalColor = coreCol + edgeCol;
                
                // Apply texture and noise variations
                finalColor.rgb *= mainTex.rgb * (0.8 + noise.r * 0.4);
                finalColor.a *= edgeAlpha * i.color.a;
                
                // Apply intensity
                finalColor.rgb *= _Intensity;
                
                // Add some animated sparkle effects
                float sparkle = noise.b * noise.a;
                sparkle = pow(sparkle, 4.0) * 2.0;
                finalColor.rgb += sparkle * _CoreColor.rgb * coreAlpha;
                
                // Apply fog
                UNITY_APPLY_FOG(i.fogCoord, finalColor);
                
                return finalColor;
            }
            ENDCG
        }
    }
    
    Fallback "Sprites/Default"
}