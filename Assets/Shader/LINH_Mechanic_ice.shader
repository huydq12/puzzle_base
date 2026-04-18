Shader "LINH/Mechanic_ice"
{
    Properties
    {
        [NoScaleOffset] _IceTex ("Ice Texture", 2D) = "white" {}
        [NoScaleOffset] _MatcapTex ("Matcap Texture", 2D) = "white" {}
        _HSVController ("HSV (Hue, Saturation, Value)", Vector) = (0,1,1,0)
        _Alpha ("Alpha", Range(0, 1)) = 1
        _FresnelPower ("Fresnel Power", Range(0.1, 8)) = 2
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
        }
        LOD 200

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            sampler2D _IceTex;
            sampler2D _MatcapTex;
            float4 _IceTex_ST;
            float4 _HSVController;
            float _Alpha;
            float _FresnelPower;
            float3 _WorldSpaceCameraPos;

            float4x4 unity_ObjectToWorld;
            float4x4 unity_MatrixVP;
            float4x4 unity_MatrixV;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 matcapUV : TEXCOORD1;
                float fresnel : TEXCOORD2;
            };

            float3 RGBToHSV(float3 c)
            {
                float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
                float4 p = (c.g < c.b) ? float4(c.bg, K.wz) : float4(c.gb, K.xy);
                float4 q = (c.r < p.x) ? float4(p.xyw, c.r) : float4(c.r, p.yzx);
                float d = q.x - min(q.w, q.y);
                float e = 1e-10;
                return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
            }

            float3 HSVToRGB(float3 c)
            {
                float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
                float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
                return c.z * lerp(K.xxx, saturate(p - K.xxx), c.y);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;

                float4 worldPos = mul(unity_ObjectToWorld, input.positionOS);
                float3 worldNormal = normalize(mul((float3x3)unity_ObjectToWorld, input.normalOS));
                float3 viewDir = normalize(_WorldSpaceCameraPos - worldPos.xyz);

                float3 viewNormal = normalize(mul((float3x3)unity_MatrixV, worldNormal));
                output.matcapUV = viewNormal.xy * 0.5 + 0.5;

                float fresnelTerm = 1.0 - saturate(dot(worldNormal, viewDir));
                output.fresnel = pow(fresnelTerm, max(_FresnelPower, 0.001));

                output.uv = input.uv * _IceTex_ST.xy + _IceTex_ST.zw;
                output.positionCS = mul(unity_MatrixVP, worldPos);
                return output;
            }

            float4 frag(Varyings input) : SV_TARGET
            {
                float4 iceSample = tex2D(_IceTex, input.uv);
                float3 matcapSample = tex2D(_MatcapTex, input.matcapUV).rgb;

                float3 col = iceSample.rgb;
                col *= lerp(0.65, 1.2, matcapSample);
                col += matcapSample * (0.15 + input.fresnel * 0.35);

                float3 hsv = RGBToHSV(max(col, 0.0));
                hsv.x = frac(hsv.x + _HSVController.x);
                hsv.y = saturate(hsv.y * max(_HSVController.y, 0.0));
                hsv.z *= max(_HSVController.z, 0.0);
                col = HSVToRGB(hsv);

                float alpha = saturate(_Alpha * lerp(0.35, 1.0, iceSample.a) + input.fresnel * 0.15);
                return float4(col, alpha);
            }
            ENDHLSL
        }
    }
}
