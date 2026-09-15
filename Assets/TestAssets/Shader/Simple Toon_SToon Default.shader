Shader "Simple Toon/SToon Default" {
	Properties {
		_MainTex ("Texture", 2D) = "white" {}
		[Header(Colorize)] [Space(5)] _Color ("Color", Vector) = (1,1,1,1)
		[HideInInspector] _ColIntense ("Intensity", Range(0, 3)) = 1
		[HideInInspector] _ColBright ("Brightness", Range(-1, 1)) = 0
		_AmbientCol ("Ambient", Range(0, 1)) = 0
		[Header(Detail)] [Space(5)] [Toggle] _Segmented ("Segmented", Float) = 1
		_Steps ("Steps", Range(1, 25)) = 3
		_StpSmooth ("Smoothness", Range(0, 1)) = 0
		_Offset ("Lit Offset", Range(-1, 1.1)) = 0
		[Header(Light)] [Space(5)] [Toggle] _Clipped ("Clipped", Float) = 0
		_MinLight ("Min Light", Range(0, 1)) = 0
		_MaxLight ("Max Light", Range(0, 1)) = 1
		_Lumin ("Luminocity", Range(0, 2)) = 0
		[Header(Shine)] [Space(5)] [HDR] _ShnColor ("Color", Vector) = (1,1,0,1)
		[Toggle] _ShnOverlap ("Overlap", Float) = 0
		_ShnIntense ("Intensity", Range(0, 1)) = 0
		_ShnRange ("Range", Range(0, 1)) = 0.15
		_ShnSmooth ("Smoothness", Range(0, 1)) = 0
	}
	//DummyShaderTextExporter
	SubShader{
		Tags { "RenderType"="Opaque" }
		LOD 200

		Pass
		{
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			float4x4 unity_ObjectToWorld;
			float4x4 unity_MatrixVP;
			float4 _MainTex_ST;

			struct Vertex_Stage_Input
			{
				float4 pos : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct Vertex_Stage_Output
			{
				float2 uv : TEXCOORD0;
				float4 pos : SV_POSITION;
			};

			Vertex_Stage_Output vert(Vertex_Stage_Input input)
			{
				Vertex_Stage_Output output;
				output.uv = (input.uv.xy * _MainTex_ST.xy) + _MainTex_ST.zw;
				output.pos = mul(unity_MatrixVP, mul(unity_ObjectToWorld, input.pos));
				return output;
			}

			Texture2D<float4> _MainTex;
			SamplerState sampler_MainTex;
			float4 _Color;

			struct Fragment_Stage_Input
			{
				float2 uv : TEXCOORD0;
			};

			float4 frag(Fragment_Stage_Input input) : SV_TARGET
			{
				return _MainTex.Sample(sampler_MainTex, input.uv.xy) * _Color;
			}

			ENDHLSL
		}
	}
}