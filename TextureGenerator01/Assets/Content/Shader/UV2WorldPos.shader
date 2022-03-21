/*====================================================
*
* TOTAL BAKER
*
* Francesco Cucchiara - 3POINT SOFT
* http://threepointsoft.altervista.org
*
=====================================================*/

Shader "TB/UV2WorldPos" {
	SubShader{
		Pass{

			Lighting Off
			Cull Off

			CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#include "UnityCG.cginc"

				struct v2f {
					float4 pos : SV_POSITION;
					float4 color : COLOR;
				};

				// vertex input: position, UV
				struct appdata {
					float4 vertex : POSITION;
					float2 uv : TEXCOORD0;
				};

				v2f vert(appdata v){
					v2f o;
					float3 worldPos =  mul(unity_ObjectToWorld, v.vertex);
					o.color = float4(worldPos.x, worldPos.y, worldPos.z, 1);
					o.pos = float4(v.uv.x * 2.0 - 1.0, v.uv.y * 2.0 - 1.0, 1.0, 1.0);
					return o;
				}

				float4 frag(v2f i) : SV_Target{
					return i.color;
				}
			ENDCG
		}
	}
}
