﻿Shader "Hidden/PostEffect/PE_DepthSSAO"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
		SubShader
		{
			Cull Off ZWrite Off ZTest Always

			Pass
			{
				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag

				#include "UnityCG.cginc"

			sampler2D _MainTex;
			sampler2D _CameraDepthTexture;
			float4 _SampleSphere[32];
			int _SampleCount;
			float _Strength;
			float _FallOff;
			float _FallOffLimit;
			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			v2f vert (appdata_img v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.texcoord;
				return o;
			}

			float3 normal_from_depth(float2 texcoords) {

				const float2 offset1 = float2(0.0, 0.001);
				const float2 offset2 = float2(0.001, 0.0);

				float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, texcoords);
				float depth1 = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, texcoords + offset1);
				float depth2 = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, texcoords + offset2);
				
				float3 p1 = float3(offset1, depth1 - depth);
				float3 p2 = float3(offset2, depth2 - depth);
				return -normalize(cross(p1, p2));
			}

			fixed4 frag (v2f i) : SV_Target
			{
				float4 col = tex2D(_MainTex, i.uv);
				float3 normal = normal_from_depth( i.uv);
				float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv);
				float3 position = float3(i.uv, depth);
				float occlusion = 0;
				for (int i = 0; i < _SampleCount; i++) {
					float3 ray = _SampleSphere[i];
					float3 hemi_ray = position + sign(dot(ray, normal)) * ray;
					float occ_depth =  SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, saturate(hemi_ray.xy));
					float difference = occ_depth-depth; 

					occlusion += step(difference,_FallOffLimit)* lerp(-1, 1, smoothstep(-_FallOff, _FallOff, difference));
				}
				occlusion =  saturate( occlusion/_SampleCount);
				float ao = pow(occlusion,5)*_Strength;
				return lerp(col,float4(0,0,0,1),ao);
			}
			ENDCG
		}
	}
}
