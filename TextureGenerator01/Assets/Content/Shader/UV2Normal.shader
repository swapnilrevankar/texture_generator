/*====================================================
*
* TOTAL BAKER
*
* Francesco Cucchiara - 3POINT SOFT
* http://threepointsoft.altervista.org
*
=====================================================*/

Shader "TB/UV2Normal" {
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

	// vertex input: normal, UV
	struct appdata {
		float4 normal : NORMAL;
		float2 uv : TEXCOORD0;
	};



	v2f vert(appdata v) {
		v2f o;
		float3 worldNormal = UnityObjectToWorldNormal(v.normal);
		o.color.xyz = (worldNormal*0.5)+0.5;
		o.color.w = 1.0;
		o.pos = float4(v.uv.x * 2.0 - 1.0, v.uv.y * 2.0 - 1.0, 1.0, 1.0); //reverse y coordinate because Texture2D coordinates are reversed on y and the pixel writing will be more efficient
		return o;
	}

	float4 frag(v2f i) : SV_Target{
		return i.color;
	}
		ENDCG
	}
	}
}
