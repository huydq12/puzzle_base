Shader "KIM/2D/FakeShadow" {
	Properties {
		[NoScaleOffset] _MainTex ("Texture", 2D) = "gray" {}
		_ColorSha ("Shadow Color", Vector) = (0,0,0,1)
	}

	SubShader{
		Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
		LOD 100

		Pass
		{
			Tags { "LightMode" = "SRPDefaultUnlit" }
			Blend SrcAlpha OneMinusSrcAlpha
			ZWrite Off
			Cull Off

			CGPROGRAM
			#pragma target 2.0
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			sampler2D _MainTex;
			float4 _MainTex_ST;
			fixed4 _ColorSha;

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;
			};

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				fixed texAlpha = tex2D(_MainTex, i.uv).a;
				return fixed4(_ColorSha.rgb, texAlpha * _ColorSha.a);
			}
			ENDCG
		}
	}
	Fallback Off
}
