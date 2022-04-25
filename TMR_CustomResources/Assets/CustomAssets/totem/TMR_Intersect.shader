// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'
// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "TeammateRevive/TMR_Intersect"
{
	Properties
	{
		_Cloud1 ("Cloud 1", 2D) = "white" {}
		_IntersectPower1 ("Intersect Power", Range(0,5)) = 1
		_IntersectAlpha1 ("Intersect Alpha", Range(0,1)) = 1
		
		_Cloud2 ("Cloud 2", 2D) = "white" {}
		_IntersectPower2 ("Intersect Power 2", Range(0,5)) = 1.5
		_IntersectAlpha2 ("Intersect Alpha 2", Range(0,1)) = 0.5

		_RemapTex ("Remap Texture", 2D) = "white" {}
		_IntersectFadePower ("Intersect Fade Power", Range(0,1)) = 0.1

		_RimPower ("Rim Power", Range(0,5)) = 1
		_RimAlpha ("Rim Alpha", Range(0,1)) = 0.5

		_GlobalPower ("Global Power", Range(0,3)) = 1
		
		_CenterThreshold ("Center Threshold", Range(0,1)) = 0.01
		_CenterAlpha ("Center Alpha", Range(0,1)) = 0

		_HueShift ("Hue Shift", Range(0,1)) = 0

	}

	SubShader
	{
		Blend One One
		ZWrite Off
		Cull Off

		Tags
		{
			"RenderType"="Transparent"
			"Queue"="Transparent"
		}

		Pass
		{
			CGPROGRAM
			#pragma target 3.0
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			sampler2D _CameraDepthTexture;

			sampler2D _Cloud1;
			sampler2D _Cloud2;
			sampler2D _RemapTex;
			fixed4 _Cloud1_ST;

			float _RimAlpha;
			float _IntersectPower1;
			float _IntersectPower2;
			float _IntersectAlpha1;
			float _IntersectAlpha2;
			float _IntersectFadePower;
			float _RimPower;
			float _GlobalPower;
			float _CenterThreshold;
			float _CenterAlpha;
			float _HueShift;


			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float3 normal : NORMAL;
			};

			struct v2f
            {
                float2 uv : TEXCOORD0;
				float3 viewDir : TEXCOORD1;
				float3 normal : NORMAL;
            };

			v2f vert(appdata v, out float4 vertex : SV_POSITION)
            {
                v2f o;
                vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _Cloud1);
				o.normal = UnityObjectToWorldNormal(v.normal);
				o.viewDir = normalize(UnityWorldSpaceViewDir(mul(unity_ObjectToWorld, v.vertex)));

                return o;
            }

			fixed4 texColor1(float2 uv)
			{
				float4 color = tex2D(_Cloud1, uv);
				return color;
			}

			fixed4 texColor2(float2 uv)
			{
				float4 color = tex2D(_Cloud2, uv);
				return color;
			}

			fixed4 remap (float4 color)
			{
				fixed4 remapColor = tex2D(_RemapTex, float2(length(color), 0.5));
				if(length(color) <= _CenterThreshold)
				{
					remapColor *= _CenterAlpha;
				}

				return remapColor;
			}

			fixed3 shift_col(fixed3 RGB, fixed3 shift)
            {
				fixed3 RESULT = fixed3(RGB);
				float VSU = shift.z*shift.y*cos(shift.x*3.14159265/180);
				float VSW = shift.z*shift.y*sin(shift.x*3.14159265/180);
			
				RESULT.r = (.299*shift.z+.701*VSU+.168*VSW)*RGB.r
						+ (.587*shift.z-.587*VSU+.330*VSW)*RGB.g
						+ (.114*shift.z-.114*VSU-.497*VSW)*RGB.b;
			
				RESULT.g = (.299*shift.z-.299*VSU-.328*VSW)*RGB.r
						+ (.587*shift.z+.413*VSU+.035*VSW)*RGB.g
						+ (.114*shift.z-.114*VSU+.292*VSW)*RGB.b;
			
				RESULT.b = (.299*shift.z-.3*VSU+1.25*VSW)*RGB.r
						+ (.587*shift.z-.588*VSU-1.05*VSW)*RGB.g
						+ (.114*shift.z+.886*VSU-.203*VSW)*RGB.b;
				
				return (RESULT);
            }

			fixed4 frag (v2f i, UNITY_VPOS_TYPE vpos : VPOS) : SV_Target
            {
                float2 screenuv = vpos.xy / _ScreenParams.xy;
                float screenDepth = Linear01Depth(tex2D(_CameraDepthTexture, screenuv));
                float diff = screenDepth - Linear01Depth(vpos.z);
                float intersect1 = 0;
				float intersect2 = 0;
				float intersectFade = 0;

                if(diff > 0)
				{
                    intersect1 = (1 - smoothstep(0, _ProjectionParams.w * _IntersectPower1 * _GlobalPower, diff)) * _IntersectAlpha1;
					intersect2 = (1 - smoothstep(0, _ProjectionParams.w * _IntersectPower2 * _GlobalPower, diff)) * _IntersectAlpha2;
					intersectFade = smoothstep(0, _ProjectionParams.w * _IntersectFadePower * _GlobalPower, diff);
				}

				float rim = (1 - abs(dot(i.normal, normalize(i.viewDir))));
				rim = pow(rim, _RimPower) * _RimAlpha;
                
                fixed4 intersectColor1 = texColor1(i.uv + _Time.x) * intersect1;
				fixed4 intersectColor2 = texColor2(i.uv - _Time.x) * intersect2;

				fixed4 rimColor = (texColor1(i.uv * 2 - _Time.x) + texColor2(i.uv * 2 - _Time.x)) * rim;

				fixed4 col = intersectColor1 + intersectColor2 + rimColor;
				col = remap(col);
				//hue shift
				col = fixed4(shift_col(col.rgb, float3(_HueShift*360 + 1, 1, 1)).rgb, length(col.rgb));
				col = col * intersectFade;
				
				return col;
            }
			ENDCG
		}
	}
}
