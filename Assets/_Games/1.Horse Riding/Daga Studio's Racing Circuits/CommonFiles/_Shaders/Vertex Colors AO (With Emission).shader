// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Arkhams/Standard/Vertex Colors AO (With Emission)"
{
	Properties
	{
		[HideInInspector] __dirty( "", Int ) = 1
		_NormalIntensity("Normal Intensity", Range( 0 , 1)) = 0
		_RoughnessIntensity("Roughness Intensity", Range( 0 , 1)) = 0
		_EmissionIntensity("Emission Intensity", Range( 0 , 10)) = 0
		_VertexAOIntensity("Vertex AO Intensity", Range( 0 , 1.1)) = 0
		_Albedo("Albedo", 2D) = "white" {}
		_Normal("Normal", 2D) = "bump" {}
		_EmissionA("Emission (A)", 2D) = "white" {}
		_MetallicRRoughnessA("Metallic (R) Roughness (A)", 2D) = "white" {}
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
	}

	SubShader
	{
		Tags{ "RenderType" = "Opaque"  "Queue" = "Geometry+0" "IsEmissive" = "true"  }
		Cull Back
		CGPROGRAM
		#include "UnityStandardUtils.cginc"
		#pragma target 3.0
		#pragma surface surf Standard keepalpha addshadow fullforwardshadows 
		struct Input
		{
			float2 uv_texcoord;
			float4 vertexColor : COLOR;
		};

		uniform float _NormalIntensity;
		uniform sampler2D _Normal;
		uniform float4 _Normal_ST;
		uniform sampler2D _Albedo;
		uniform float4 _Albedo_ST;
		uniform float _VertexAOIntensity;
		uniform sampler2D _EmissionA;
		uniform float4 _EmissionA_ST;
		uniform float _EmissionIntensity;
		uniform sampler2D _MetallicRRoughnessA;
		uniform float4 _MetallicRRoughnessA_ST;
		uniform float _RoughnessIntensity;

		void surf( Input i , inout SurfaceOutputStandard o )
		{
			float2 uv_Normal = i.uv_texcoord * _Normal_ST.xy + _Normal_ST.zw;
			o.Normal = UnpackScaleNormal( tex2D( _Normal, uv_Normal ) ,_NormalIntensity );
			float2 uv_Albedo = i.uv_texcoord * _Albedo_ST.xy + _Albedo_ST.zw;
			float4 tex2DNode3 = tex2D( _Albedo, uv_Albedo );
			o.Albedo = lerp( tex2DNode3 , ( tex2DNode3 * i.vertexColor ) , _VertexAOIntensity ).rgb;
			float2 uv_EmissionA = i.uv_texcoord * _EmissionA_ST.xy + _EmissionA_ST.zw;
			o.Emission = ( tex2D( _EmissionA, uv_EmissionA ) * _EmissionIntensity ).rgb;
			float2 uv_MetallicRRoughnessA = i.uv_texcoord * _MetallicRRoughnessA_ST.xy + _MetallicRRoughnessA_ST.zw;
			float4 tex2DNode16 = tex2D( _MetallicRRoughnessA, uv_MetallicRRoughnessA );
			o.Metallic = tex2DNode16.r;
			o.Smoothness = ( tex2DNode16.b * _RoughnessIntensity );
			o.Alpha = 1;
		}

		ENDCG
	}
	Fallback "Diffuse"
	
}
