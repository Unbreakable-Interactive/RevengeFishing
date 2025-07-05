Shader "PostProcessing/GaussianBlur"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {} // Input method that gets passed to the mat via blit method
        _Spread ("Standard Deviation (Spread)", Float) = 0
        _GridSize("Grid Size", Integer) = 1
    }
    SubShader
    {
        Tags 
        { 
            "RenderType"="Opaque"
            "RenderPipeline"="UniversalPipeline"
        }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #define E 2.71828f

        sampler2D _MainTex;

        CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_TexelSize;
            float _Spread;
            int _GridSize;
        CBUFFER_END

        float gaussian(int x)
        {
            float sigmaSqr = _Spread * _Spread;
            return (1 / sqrt(TWO_PI * sigmaSqr)) * pow(E, -(x * x) / (2 * sigmaSqr));
        }

        struct appdata
        {
            float4 positionOS : Position;
            float2 uv : TEXCOORD0;
        };

        struct v2f
        {
            float4 positionCS : SV_Position;
            float2 uv : TEXCOORD0;
        };

        v2f vert(appdata v)
        {
            v2f o;
            o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
            o.uv = v.uv;
            return o;
        }

        ENDHLSL

        Pass
        {
            Name "Horizontal"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag_horizontal

            float4 frag_horizontal (v2f i) : SV_Target
            {
                float3 col = float3(0.0f, 0.0f, 0.0f);
                float gridSum = 0.0f;

                int upper = ((_GridSize - 1) / 2);
                int lower = -upper;

                for (int x = lower; x <= upper; ++x)
                {
                    float weight = gaussian(x);
                    gridSum += weight;
                    float2 uv = i.uv + float2(_MainTex_TexelSize.x * x, 0.0f);
                    col += weight * tex2D(_MainTex, uv).xyz;
                }

                col /= gridSum;
                return float4(col, 1.0f);
            }

            ENDHLSL

        }

        Pass
        {
            Name "Vertical"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag_vertical

            float4 frag_vertical (v2f i) : SV_Target
            {
                float3 col = float3(0.0f, 0.0f, 0.0f);
                float gridSum = 0.0f;

                int upper = ((_GridSize - 1) / 2);
                int lower = -upper;

                for (int y = lower; y <= upper; ++y)
                {
                    float weight = gaussian(y);
                    gridSum += weight;
                    float2 uv = i.uv + float2(0.0f, _MainTex_TexelSize.y * y);
                    col += weight * tex2D(_MainTex, uv).xyz;
                }

                col /= gridSum;
                return float4(col, 1.0f);
            }

            ENDHLSL

        }
    }
}
