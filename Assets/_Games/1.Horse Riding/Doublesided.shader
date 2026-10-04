// Unlit alpha-cutout shader.
// - no lighting
// - no lightmap support
// - no per-material color

Shader "Custom/Unlit/Transparent Cutout2" {
Properties {
    _MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
    _Cutoff ("Alpha cutoff", Range(0,1)) = 0.5

    _wind_dir ("Wind Direction", Vector) = (0.5,0.05,0.5,0)
        _wind_size ("Wind Wave Size", range(5,50)) = 15

        _tree_sway_stutter_influence("Tree Sway Stutter Influence", range(0,1)) = 0.2
        _tree_sway_stutter ("Tree Sway Stutter", range(0,10)) = 1.5
        _tree_sway_speed ("Tree Sway Speed", range(0,10)) = 1
        _tree_sway_disp ("Tree Sway Displacement", range(0,1)) = 0.3

        _branches_disp ("Branches Displacement", range(0,0.5)) = 0.3

        _leaves_wiggle_disp ("Leaves Wiggle Displacement", float) = 0.07
        _leaves_wiggle_speed ("Leaves Wiggle Speed", float) = 0.01

        _r_influence ("Red Vertex Influence", range(0,1)) = 1
        _b_influence ("Blue Vertex Influence", range(0,1)) = 1
}
SubShader {
    Tags {"Queue"="AlphaTest" "IgnoreProjector"="True" "RenderType"="TransparentCutout"}
    LOD 100

    Lighting Off
	Cull Off
    Pass {  
        CGPROGRAM
            #pragma vertex vert
           // #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_fog
                        #pragma surface surf Lambert vertex:vert addshadow

            #include "UnityCG.cginc"

            struct appdata_t {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float2 texcoord : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed _Cutoff;

            v2f vert (appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.texcoord);
                clip(col.a - _Cutoff);
                UNITY_APPLY_FOG(i.fogCoord, col);

                float edgeHeight = 0.015;
                return lerp(
                    col,
                    fixed4(0, 4, 2, 1),
                    step(col.a, _Cutoff + edgeHeight)
                );
            }


            //Declared Variables
            float4 _wind_dir;
            float _wind_size;
            float _tree_sway_speed;
            float _tree_sway_disp;
            float _leaves_wiggle_disp;
            float _leaves_wiggle_speed;
            float _branches_disp;
            float _tree_sway_stutter;
            float _tree_sway_stutter_influence;
            float _r_influence;
            float _b_influence;

            sampler2D _MainTex;
            fixed4 _Tint;

                //Structs
                struct Input {
                    float2 uv_MainTex;
                };

                // Vertex Manipulation Function
                void vert (inout appdata_full i) {

                     //Gets the vertex's World Position 
                    float3 worldPos = mul (unity_ObjectToWorld, i.vertex).xyz;

                    //Tree Movement and Wiggle
                    i.vertex.x += (cos(_Time.z * _tree_sway_speed + (worldPos.x/_wind_size) + (sin(_Time.z * _tree_sway_stutter * _tree_sway_speed + (worldPos.x/_wind_size)) * _tree_sway_stutter_influence) ) + 1)/2 * _tree_sway_disp * _wind_dir.x * (i.vertex.y / 10) + 
                    cos(_Time.w * i.vertex.x * _leaves_wiggle_speed + (worldPos.x/_wind_size)) * _leaves_wiggle_disp * _wind_dir.x * i.color.b * _b_influence;

                    i.vertex.z += (cos(_Time.z * _tree_sway_speed + (worldPos.z/_wind_size) + (sin(_Time.z * _tree_sway_stutter * _tree_sway_speed + (worldPos.z/_wind_size)) * _tree_sway_stutter_influence) ) + 1)/2 * _tree_sway_disp * _wind_dir.z * (i.vertex.y / 10) + 
                    cos(_Time.w * i.vertex.z * _leaves_wiggle_speed + (worldPos.x/_wind_size)) * _leaves_wiggle_disp * _wind_dir.z * i.color.b * _b_influence;

                    i.vertex.y += cos(_Time.z * _tree_sway_speed + (worldPos.z/_wind_size)) * _tree_sway_disp * _wind_dir.y * (i.vertex.y / 10);

                    //Branches Movement
                    i.vertex.y += sin(_Time.w * _tree_sway_speed + _wind_dir.x + (worldPos.z/_wind_size)) * _branches_disp  * i.color.r * _r_influence;

                }

                // Surface Shader
                void surf (Input IN, inout SurfaceOutput o) {
                    fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Tint;
                    o.Albedo = c.rgb;
                    o.Alpha = c.a;
                }

        ENDCG
    }
}


}