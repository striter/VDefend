﻿Shader "Game/Particle/Distort"
{
	Properties
	{
		_DistortTex("DistortTex",2D) = "white"{}
		_DistortStrength("Distort Strength",Range(0,0.1))=.005
	}
	SubShader
	{
		Tags{ "RenderType" = "Transparent" "IgnoreProjector" = "True" "Queue" = "Transparent" "PreviewType"="Plane"}
		Cull Off Lighting Off ZWrite Off Fog { Color(0,0,0,0) }

		Pass
		{		
			name "Main"
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv:TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 uv:TEXCOORD1;
				float4 screenPos:TEXCOORD2;
			};
			sampler2D _CameraOpaqueTexture;
			sampler2D _DistortTex;
			float _DistortStrength;
			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				o.screenPos = ComputeScreenPos(o.vertex);
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				fixed4 col = tex2D(_CameraOpaqueTexture,i.screenPos.xy / i.screenPos.w + tex2D(_DistortTex,i.uv) *_DistortStrength);
				return col;
			}
			ENDCG
		}
	}
}
