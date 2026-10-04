Shader "EivaaGames/Diffuse Cubemap with Rimlight" {
	Properties {
		_MainTex ("Texture", 2D) = "white" {}
		_Cube ("Cubemap", Cube) = "" {}
		_ReflColor ("Relection Color", Vector) = (0.5,0.5,0.5,1)
		_RimColor ("Rim Color", Vector) = (1,1,1,1)
		_RimPower ("Rim Power", Range(0.5, 8)) = 3
		_RimDirection ("Rim Direction", Vector) = (0,1,0,1)
	}
	//DummyShaderTextExporter
	SubShader{
		Tags { "RenderType"="Opaque" }
		LOD 200
		CGPROGRAM
#pragma surface surf Standard
#pragma target 3.0

		sampler2D _MainTex;
		struct Input
		{
			float2 uv_MainTex;
		};

		void surf(Input IN, inout SurfaceOutputStandard o)
		{
			fixed4 c = tex2D(_MainTex, IN.uv_MainTex);
			o.Albedo = c.rgb;
			o.Alpha = c.a;
		}
		ENDCG
	}
	Fallback "Diffuse"
}