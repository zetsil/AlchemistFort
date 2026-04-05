Shader "Custom/WaterSurface_CleanContour"
{
    Properties
    {
        // --- Culori & Aspect ---
        _ShallowColor       ("Culoare Apă Mică",             Color) = (0.18, 0.72, 0.78, 0.75)
        _DeepColor          ("Culoare Apă Adâncă",           Color) = (0.04, 0.22, 0.48, 0.97)
        _FoamColor          ("Culoare Contur Mal",           Color) = (1.0, 1.0, 1.0, 1.0)
        _FresnelColor       ("Culoare Fresnel/Reflecție",     Color) = (0.80, 0.93, 1.0,  1.0)

        // --- Toon Shading ---
        _ToonSteps          ("Toon – Număr Trepte Lumină", Range(2, 8))   = 3
        _ToonSmoothness     ("Toon – Netezime Margini",    Range(0, 0.3)) = 0.04
        _ToonSpecSize       ("Toon – Mărime Specular",     Range(0, 1))   = 0.08
        _ToonSpecSmooth     ("Toon – Netezime Specular",   Range(0, 0.2)) = 0.02
        _SpecularIntensity  ("Toon – Intensitate Specular",Range(0, 1))   = 0.55
        _ToonRimSize        ("Toon – Mărime Rim Light",    Range(0, 1))   = 0.55
        _ToonRimSmooth      ("Toon – Netezime Rim",        Range(0, 0.3)) = 0.06
        _RimColor           ("Toon – Culoare Rim",         Color)         = (0.75, 0.92, 1.0, 1.0)

        // --- Adâncime & Transparență ---
        _ShoreLineThickness ("Grosime Linie Contur (m)",    Float)     = 0.15
        _RefractionStrength ("Intensitate Refracție",       Float)     = 0.04

        // --- Valuri (Normal Maps) ---
        _NormalMap          ("Normal Map Valuri",       2D)           = "bump" {}
        _NormalMap2         ("Normal Map Valuri 2",     2D)           = "bump" {}
        _NormalStrength     ("Intensitate Normal",      Float)        = 0.55
        _WaveSpeed          ("Viteza Valurilor",        Float)        = 0.45
        _WaveTiling         ("Scalare Valuri",          Float)        = 1.2

        // --- Specular / Fresnel ---
        _Smoothness         ("Netezime Suprafață",      Range(0,1))   = 0.88
        _FresnelPower       ("Putere Fresnel",          Float)        = 3.5

        // --- Noapte ---
        _NightDarkness      ("Lumină Ambientală Minimă Noapte", Range(0, 0.3)) = 0.06

        // --- Opacitate ---
        _MinAlpha           ("Alpha Minim Suprafață",   Range(0,1))   = 0.70

        // --- Vizibilitate pe distanță ---
        _VisibilityEnd      ("Vizibilitate Maximă (m)",  Float) = 10.0
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" }

        Pass
        {
            Name "WaterToonPass"
            ZWrite Off
            ZTest LEqual
            Cull Back
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _ShallowColor, _DeepColor, _FoamColor, _FresnelColor, _RimColor;
                float  _ToonSteps, _ToonSmoothness, _ToonSpecSize, _ToonSpecSmooth, _SpecularIntensity, _ToonRimSize, _ToonRimSmooth;
                float  _ShoreLineThickness, _RefractionStrength, _NormalStrength, _WaveSpeed, _WaveTiling, _Smoothness, _FresnelPower;
                float  _MinAlpha, _NightDarkness, _VisibilityEnd;
            CBUFFER_END

            TEXTURE2D(_NormalMap);  SAMPLER(sampler_NormalMap);
            TEXTURE2D(_NormalMap2); SAMPLER(sampler_NormalMap2);

            struct Attributes {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings {
                float4 positionCS  : SV_POSITION;
                float4 screenPos   : TEXCOORD0;
                float3 positionWS  : TEXCOORD1;
                float3 normalWS    : TEXCOORD2;
                float3 tangentWS   : TEXCOORD3;
                float3 bitangentWS : TEXCOORD4;
                float2 uv          : TEXCOORD5;
            };

            float GetSceneLinearDepth(float2 uv) {
                return LinearEyeDepth(SampleSceneDepth(uv), _ZBufferParams);
            }

            float3 BlendNormals(float3 n1, float3 n2) {
                return normalize(float3(n1.xy + n2.xy, n1.z * n2.z));
            }

            float ToonRamp(float value, float steps, float smoothWidth) {
                float s = value * steps;
                float edge = smoothstep(0.0, smoothWidth * steps, frac(s)) + smoothstep(1.0, 1.0 - smoothWidth * steps, frac(s)) - 1.0;
                return saturate(floor(s) / steps + edge / steps);
            }

            Varyings vert(Attributes input) {
                Varyings output;
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.screenPos = ComputeScreenPos(output.positionCS);
                output.normalWS = normInputs.normalWS;
                output.tangentWS = normInputs.tangentWS;
                output.bitangentWS = normInputs.bitangentWS;
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target {
                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                float3 cameraPos = _WorldSpaceCameraPos.xyz;

                // --- Adâncime (Fără distorsiune la rotire) ---
                float sceneDepthM = GetSceneLinearDepth(screenUV);
                float waterDepthM = input.screenPos.w; 
                float depthDiff   = max(0.0, sceneDepthM - waterDepthM);
                float depthT      = saturate(depthDiff / max(0.001, _VisibilityEnd));

                // --- Culori ---
                float3 waterColor = lerp(_ShallowColor.rgb, _DeepColor.rgb, depthT);
                float currentAlpha = lerp(_ShallowColor.a, _DeepColor.a, depthT);

                // --- Normale Animate ---
                float waveT = _Time.y * _WaveSpeed;
                float2 uv1 = input.uv * _WaveTiling + float2(waveT * 0.07, waveT * 0.05);
                float2 uv2 = input.uv * _WaveTiling * 0.7 + float2(-waveT * 0.04, waveT * 0.09);
                float3 n1 = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uv1));
                float3 n2 = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap2, sampler_NormalMap2, uv2));
                float3 blendedN = BlendNormals(n1, n2);
                blendedN.xy *= _NormalStrength;
                float3x3 TBN = float3x3(normalize(input.tangentWS), normalize(input.bitangentWS), normalize(input.normalWS));
                float3 normalWS = normalize(mul(blendedN, TBN));

                // --- Refracție ---
                float2 refractedUV = screenUV + normalWS.xz * _RefractionStrength * (1.0 - depthT);
                if (GetSceneLinearDepth(refractedUV) < waterDepthM) refractedUV = screenUV;
                float3 bgColor = SampleSceneColor(refractedUV).rgb;
                float3 finalColor = lerp(lerp(bgColor, waterColor, depthT), waterColor, max(_MinAlpha, currentAlpha));

                // --- Iluminare Toon ---
                Light mainLight = GetMainLight();
                float3 viewDir = normalize(cameraPos - input.positionWS);
                float3 halfDir = normalize(mainLight.direction + viewDir);

                float toonDiff = ToonRamp(dot(normalWS, mainLight.direction) * 0.5 + 0.5, _ToonSteps, _ToonSmoothness);
                float diffMin = max(_NightDarkness, 0.5 * saturate(mainLight.color.r + mainLight.color.g + mainLight.color.b));
                finalColor *= lerp(diffMin, 1.0, toonDiff) * mainLight.color;

                float specRaw = pow(saturate(dot(normalWS, halfDir)), exp2(_Smoothness * 10.0 + 1.0));
                finalColor += mainLight.color * smoothstep(_ToonSpecSize - _ToonSpecSmooth, _ToonSpecSize + _ToonSpecSmooth, specRaw) * _SpecularIntensity;

                float rimRaw = 1.0 - saturate(dot(normalWS, viewDir));
                finalColor = lerp(finalColor, _RimColor.rgb, smoothstep(_ToonRimSize - _ToonRimSmooth, _ToonRimSize + _ToonRimSmooth, rimRaw) * 0.25);
                finalColor = lerp(finalColor, _FresnelColor.rgb, pow(1.0 - saturate(dot(normalWS, viewDir)), _FresnelPower) * 0.15);

                // --- Linie Contur Mal (Shoreline) ---
                float outline = 1.0 - saturate(depthDiff / max(0.001, _ShoreLineThickness));
                outline = smoothstep(0.0, 0.1, outline);
                finalColor = lerp(finalColor, _FoamColor.rgb, outline * _FoamColor.a);

                return half4(finalColor, max(max(currentAlpha, _MinAlpha), outline * _FoamColor.a));
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}