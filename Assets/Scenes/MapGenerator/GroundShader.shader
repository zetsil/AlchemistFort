Shader "Custom/URP_Terrain_Stochastic"
{
    Properties
    {
        _Control("Splatmap", 2D) = "red" {}
        
        _Splat0("Layer 0 (R)", 2D) = "white" {}
        _Splat1("Layer 1 (G)", 2D) = "white" {}
        _Splat2("Layer 2 (B)", 2D) = "white" {}
        _Splat3("Layer 3 (A)", 2D) = "white" {}
        
        _Color0("Color 0", Color) = (1,1,1,1)
        _Color1("Color 1", Color) = (1,1,1,1)
        _Color2("Color 2", Color) = (1,1,1,1)
        _Color3("Color 3", Color) = (1,1,1,1)
        
        _NoiseScale("Noise Scale", Float) = 5.0
        _NoiseStrength("Noise Strength", Range(0,1)) = 0.3
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry-100" }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 worldPos   : TEXCOORD1;
                float3 normalWS   : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Control_ST;
                float4 _Splat0_ST;
                float4 _Splat1_ST;
                float4 _Splat2_ST;
                float4 _Splat3_ST;
                half4  _Color0;
                half4  _Color1;
                half4  _Color2;
                half4  _Color3;
                float  _NoiseScale;
                float  _NoiseStrength;
            CBUFFER_END

            TEXTURE2D(_Control); SAMPLER(sampler_Control);
            TEXTURE2D(_Splat0);  SAMPLER(sampler_Splat0);
            TEXTURE2D(_Splat1);  SAMPLER(sampler_Splat1);
            TEXTURE2D(_Splat2);  SAMPLER(sampler_Splat2);
            TEXTURE2D(_Splat3);  SAMPLER(sampler_Splat3);

            float2 StochasticUV(float2 uv, float2 worldXZ, float seed)
            {
                float2 noiseInput = worldXZ * 0.05 + seed;
                
                float n1 = frac(sin(dot(noiseInput, float2(127.1, 311.7))) * 43758.5);
                float n2 = frac(sin(dot(noiseInput + 0.5, float2(269.5, 183.3))) * 46839.3);
                
                float angle = n1 * 6.2831;
                float scale = 0.8 + n2 * 0.4;
                
                float2x2 rot = float2x2(cos(angle), -sin(angle), sin(angle), cos(angle));
                return mul(rot, uv) * scale + float2(n1, n2) * _NoiseStrength;
            }

            half4 SampleStochastic(TEXTURE2D_PARAM(tex, samp), float2 uv, float2 worldXZ, float seed)
            {
                float2 uv1 = StochasticUV(uv, worldXZ,        seed);
                float2 uv2 = StochasticUV(uv, worldXZ + 17.3, seed + 5.3);
                float2 uv3 = StochasticUV(uv, worldXZ + 31.7, seed + 11.7);
                
                half4 c1 = SAMPLE_TEXTURE2D(tex, samp, uv1);
                half4 c2 = SAMPLE_TEXTURE2D(tex, samp, uv2);
                half4 c3 = SAMPLE_TEXTURE2D(tex, samp, uv3);
                
                float blend = frac(sin(dot(worldXZ * 0.1, float2(127.1, 311.7))) * 43758.5);
                return lerp(lerp(c1, c2, blend), c3, frac(blend * 2.7));
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.worldPos   = TransformObjectToWorld(input.positionOS.xyz);
                output.normalWS   = TransformObjectToWorldNormal(input.normalOS);
                output.uv         = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 control = SAMPLE_TEXTURE2D(_Control, sampler_Control, input.uv);
                float2 worldXZ = input.worldPos.xz;

                half4 col0 = SampleStochastic(TEXTURE2D_ARGS(_Splat0, sampler_Splat0), input.uv * _Splat0_ST.xy, worldXZ, 0.0)  * _Color0;
                half4 col1 = SampleStochastic(TEXTURE2D_ARGS(_Splat1, sampler_Splat1), input.uv * _Splat1_ST.xy, worldXZ, 5.3)  * _Color1;
                half4 col2 = SampleStochastic(TEXTURE2D_ARGS(_Splat2, sampler_Splat2), input.uv * _Splat2_ST.xy, worldXZ, 11.7) * _Color2;
                half4 col3 = SampleStochastic(TEXTURE2D_ARGS(_Splat3, sampler_Splat3), input.uv * _Splat3_ST.xy, worldXZ, 17.1) * _Color3;

                half4 finalColor = col0 * control.r
                                 + col1 * control.g
                                 + col2 * control.b
                                 + col3 * control.a;

                Light mainLight = GetMainLight();
                float NdotL = saturate(dot(input.normalWS, mainLight.direction));
                float lighting = NdotL * 0.8 + 0.2;

                return half4(finalColor.rgb * mainLight.color * lighting, 1.0);
            }
            ENDHLSL
        }
    }
}