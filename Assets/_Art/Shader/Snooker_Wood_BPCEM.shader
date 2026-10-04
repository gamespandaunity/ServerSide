Shader "Snooker/Wood_BPCEM" {
	Properties {
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_Cube ("Reflection Map", Cube) = "" {}
		_MainAmount ("Main Amount", Range(0, 1)) = 0.5
		_Amount ("Reflection Amount", Float) = 0.5
		_BBoxMin ("Env Box Min", Vector) = (0,0,0,1)
		_BBoxMax ("Env Box Max", Vector) = (10,10,10,1)
		_EnviCubeMapPos ("Cube Map Pos", Vector) = (0,0,0,1)
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
}