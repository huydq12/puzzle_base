Shader "IKAME/Particles/Add_CenterGlow" {
	Properties {
		_TintColor ("Tint Color", Vector) = (0.5,0.5,0.5,0.5)
		_MainTex ("Particle Texture", 2D) = "white" {}
		_InvFade ("Soft Particles Factor", Range(0.01, 3)) = 1
		_Noise ("Noise", 2D) = "white" {}
		_Flow ("Flow", 2D) = "white" {}
		_Mask ("Mask", 2D) = "white" {}
		_SpeedMainTexUVNoiseZW ("Speed MainTex U/V + Noise Z/W", Vector) = (0,0,0,0)
		_DistortionSpeedXYPowerZ ("Distortion Speed XY Power Z", Vector) = (0,0,0,0)
		_Emission ("Emission", Float) = 2
		_Color ("Color", Vector) = (0.5,0.5,0.5,1)
		[Toggle] _Usecenterglow ("Use center glow?", Float) = 0
		[Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("ZTest", Float) = 4
		[Toggle] _CustomUVOffset ("CustomUVOffset", Float) = 0
		[HideInInspector] _texcoord ("", 2D) = "white" {}
	}

	SubShader{
		Tags {
			"Queue" = "Transparent"
			"IgnoreProjector" = "True"
			"RenderType" = "Transparent"
		}
		LOD 100
		Blend SrcAlpha One
		Cull Off
		Lighting Off
		ZWrite Off
		ZTest [_ZTest]

		Pass
		{
			Tags { "LightMode" = "SRPDefaultUnlit" }

			CGPROGRAM
			#pragma target 2.0
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_particles

			#include "UnityCG.cginc"

			sampler2D _MainTex;
			float4 _MainTex_ST;
			fixed4 _TintColor;
			fixed4 _Color;
			float _Emission;
			float _Usecenterglow;

			struct appdata_t
			{
				float4 vertex : POSITION;
				fixed4 color : COLOR;
				float2 texcoord : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				fixed4 color : COLOR;
				float2 uv : TEXCOORD0;
			};

			v2f vert(appdata_t v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
				o.color = v.color;
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				fixed4 tex = tex2D(_MainTex, i.uv);
				float glow = 1.0;
				if (_Usecenterglow > 0.5)
				{
					float2 centeredUv = i.uv - float2(0.5, 0.5);
					glow = saturate(1.0 - dot(centeredUv, centeredUv) * 4.0);
				}

				fixed4 col = tex * i.color * _TintColor * _Color;
				col.rgb *= max(_Emission, 0.0) * glow;
				return col;
			}
			ENDCG
		}
	}
}
