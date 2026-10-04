Shader "Snooker/Balls"
{
  
    Properties
    {
        _MainTex("Overlay (RGB)", 2D) = "white" {}
        _LightTex("Light Texture", 2D) = "white" {}
        _ReflectionTex("Reflection Map", 2D) = "black" {}
        _FresnelAmount("Fresnel Amount", Range(0, 1)) = 0.5
        _FresnelFocus("Fresnel Focus", Range(0.01, 5)) = 0.2
        _ReflectionAmount("Reflection Amount", Range(0, 3)) = 1.0
        _Roughness("Roughness", Range(0, 1)) = 0.1
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            sampler2D _MainTex, _LightTex, _ReflectionTex;
            float4 _MainTex_ST, _LightTex_ST, _ReflectionTex_ST;

            float _FresnelAmount, _FresnelFocus;
            float _ReflectionAmount;
            float _Roughness;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uvMain : TEXCOORD0;
                float2 uvLight : TEXCOORD1;
                float3 worldNormal : TEXCOORD2;
                float3 viewDir : TEXCOORD3;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uvMain = TRANSFORM_TEX(v.uv, _MainTex);
                o.uvLight = TRANSFORM_TEX(v.uv, _LightTex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.viewDir = normalize(_WorldSpaceCameraPos - mul(unity_ObjectToWorld, v.vertex).xyz);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 normal = normalize(i.worldNormal);
                float3 viewDir = normalize(i.viewDir);

                // Sample overlay and light textures
                float4 mainCol = tex2D(_MainTex, i.uvMain);
                float4 lightCol = tex2D(_LightTex, i.uvLight);

                // Fresnel calculation
                float fresnel = pow(1.0 - saturate(dot(viewDir, normal)), _FresnelFocus) * _FresnelAmount;

                // Reflection sample
                float3 reflDir = reflect(-viewDir, normal);
                float4 reflCol = tex2D(_ReflectionTex, reflDir.xy * 0.5 + 0.5); // simple env map sample

                // Combine
                float4 color = mainCol;
                color.rgb += lightCol.rgb;
                color.rgb = lerp(color.rgb, reflCol.rgb, _ReflectionAmount);
                color.rgb += fresnel;

                return color;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
