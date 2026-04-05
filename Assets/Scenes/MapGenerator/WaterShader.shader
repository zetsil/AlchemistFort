Shader "Custom/WaterSurface"
{
    Properties
    {
        // --- Culori & Aspect ---
        _ShallowColor       ("Culoare Apă Mică",         Color)  = (0.18, 0.72, 0.78, 0.75)
        _DeepColor          ("Culoare Apă Adâncă",       Color)  = (0.04, 0.22, 0.48, 0.97)
        _FoamColor          ("Culoare Spumă",             Color)  = (0.92, 0.97, 1.0,  1.0)
        _FresnelColor       ("Culoare Fresnel/Reflecție", Color)  = (0.80, 0.93, 1.0,  1.0)

        // --- Toon Shading ---
        _ToonSteps          ("Toon – Număr Trepte Lumină", Range(2, 8))   = 3
        _ToonSmoothness     ("Toon – Netezime Margini",    Range(0, 0.3)) = 0.04
        _ToonSpecSize       ("Toon – Mărime Specular",     Range(0, 1))   = 0.18
        _ToonSpecSmooth     ("Toon – Netezime Specular",   Range(0, 0.2)) = 0.03
        _ToonRimSize        ("Toon – Mărime Rim Light",    Range(0, 1))   = 0.55
        _ToonRimSmooth      ("Toon – Netezime Rim",        Range(0, 0.3)) = 0.06
        _RimColor           ("Toon – Culoare Rim",         Color)         = (0.75, 0.92, 1.0, 1.0)
        _DepthBands         ("Toon – Trepte Adâncime",     Range(2, 6))   = 3

        // --- Adâncime & Transparență ---
        _DepthFadeDistance  ("Distanță Fade Adâncime (m)", Float)     = 4.0
        _FoamDepthThreshold ("Prag Spumă (m)",              Float)     = 0.5
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

        // --- Opacitate ---
        _MinAlpha           ("Alpha Minim Suprafață",   Range(0,1))   = 0.70

        // --- Vizibilitate pe distanță ---
        _VisibilityStart    ("Fade Start (m)",                   Float) = 25.0
        _VisibilityEnd      ("Fade End – invizibil complet (m)", Float) = 40.0

        // --- Textura injectată de Renderer Feature (opțional) ---
        [HideInInspector] _InputTexture ("Input Depth Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue"          = "Transparent"
        }

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

            // ---------------------------------------------------------------
            // SRP Batcher
            // ---------------------------------------------------------------
            CBUFFER_START(UnityPerMaterial)
                float4 _ShallowColor;
                float4 _DeepColor;
                float4 _FoamColor;
                float4 _FresnelColor;
                float4 _RimColor;

                float  _ToonSteps;
                float  _ToonSmoothness;
                float  _ToonSpecSize;
                float  _ToonSpecSmooth;
                float  _ToonRimSize;
                float  _ToonRimSmooth;
                float  _DepthBands;

                float  _DepthFadeDistance;
                float  _FoamDepthThreshold;
                float  _RefractionStrength;

                float4 _NormalMap_ST;
                float4 _NormalMap2_ST;
                float  _NormalStrength;
                float  _WaveSpeed;
                float  _WaveTiling;

                float  _Smoothness;
                float  _FresnelPower;

                float  _MinAlpha;
                float  _VisibilityStart;
                float  _VisibilityEnd;
            CBUFFER_END

            TEXTURE2D(_NormalMap);            SAMPLER(sampler_NormalMap);
            TEXTURE2D(_NormalMap2);           SAMPLER(sampler_NormalMap2);
            TEXTURE2D_X_FLOAT(_InputTexture); SAMPLER(sampler_InputTexture);

            // ---------------------------------------------------------------
            // Structuri
            // ---------------------------------------------------------------
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                float4 screenPos   : TEXCOORD0;
                float3 positionWS  : TEXCOORD1;
                float3 normalWS    : TEXCOORD2;
                float3 tangentWS   : TEXCOORD3;
                float3 bitangentWS : TEXCOORD4;
                float2 uv          : TEXCOORD5;
            };

            // ---------------------------------------------------------------
            // Helpers
            // ---------------------------------------------------------------
            float SampleDepth(float2 uv)
            {
                float d = SAMPLE_TEXTURE2D_X_LOD(_InputTexture, sampler_InputTexture, uv, 0).r;
                if (d >= 0.999 || d <= 0.0)
                    d = SampleSceneDepth(uv);
                return d;
            }

            float GetSceneLinearDepth(float2 uv)
            {
                return LinearEyeDepth(SampleDepth(uv), _ZBufferParams);
            }

            float3 BlendNormals(float3 n1, float3 n2)
            {
                return normalize(float3(n1.xy + n2.xy, n1.z * n2.z));
            }

            // Toon ramp: cuantizează o valoare [0,1] în N trepte cu margini moi
            float ToonRamp(float value, float steps, float smoothWidth)
            {
                float s       = value * steps;
                float stepped = floor(s) / steps;
                float f       = frac(s);
                // Tranziție moale doar la granița fiecărei trepte
                float edge    = smoothstep(0.0, smoothWidth * steps, f)
                              + smoothstep(1.0, 1.0 - smoothWidth * steps, f) - 1.0;
                return saturate(stepped + edge / steps);
            }

            // ---------------------------------------------------------------
            // Vertex
            // ---------------------------------------------------------------
            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs posInputs  = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs   normInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                output.positionCS  = posInputs.positionCS;
                output.positionWS  = posInputs.positionWS;
                output.screenPos   = ComputeScreenPos(output.positionCS);
                output.normalWS    = normInputs.normalWS;
                output.tangentWS   = normInputs.tangentWS;
                output.bitangentWS = normInputs.bitangentWS;
                output.uv          = input.uv;
                return output;
            }

            // ---------------------------------------------------------------
            // Fragment
            // ---------------------------------------------------------------
            half4 frag(Varyings input) : SV_Target
            {
                float2 screenUV = input.screenPos.xy / input.screenPos.w;

                // ── Adâncime ──────────────────────────────────────────────
                float sceneDepthM = GetSceneLinearDepth(screenUV);
                float waterDepthM = LinearEyeDepth(
                    input.positionCS.z / input.positionCS.w, _ZBufferParams);
                float depthDiff   = sceneDepthM - waterDepthM;
                float depthFactor = saturate(depthDiff / max(0.001, _DepthFadeDistance));

                // ── Toon pe adâncime: N benzi de culoare vizibile ──────────
                // Dă acea stratificare caracteristică apei toon (dungi de albastru)
                float depthBanded = ToonRamp(depthFactor, _DepthBands, _ToonSmoothness);
                float3 waterColor = lerp(_ShallowColor.rgb, _DeepColor.rgb, depthBanded);

                // ── Normal Maps animate ───────────────────────────────────
                float  t   = _Time.y * _WaveSpeed;
                float2 uv1 = input.uv * _WaveTiling + float2( t * 0.07,  t * 0.05);
                float2 uv2 = input.uv * _WaveTiling * 0.7 + float2(-t * 0.04, t * 0.09);

                float3 n1 = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap,  sampler_NormalMap,  uv1));
                float3 n2 = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap2, sampler_NormalMap2, uv2));
                float3 blendedN = BlendNormals(n1, n2);
                blendedN.xy *= _NormalStrength;

                float3x3 TBN = float3x3(
                    normalize(input.tangentWS),
                    normalize(input.bitangentWS),
                    normalize(input.normalWS)
                );
                float3 normalWS = normalize(mul(blendedN, TBN));

                // ── Refracție ─────────────────────────────────────────────
                float2 refractedUV  = screenUV + normalWS.xz * _RefractionStrength * (1.0 - depthFactor);
                float  refractDepth = GetSceneLinearDepth(refractedUV);
                if (refractDepth < waterDepthM)
                    refractedUV = screenUV;
                float3 bgColor = SampleSceneColor(refractedUV).rgb;

                // ── Blend fundal + culoare apă ────────────────────────────
                float waterBlend  = max(_MinAlpha, depthBanded * _DeepColor.a);
                float3 finalColor = lerp(bgColor, waterColor, waterBlend);

                // ── Calcule de iluminare ───────────────────────────────────
                Light  mainLight = GetMainLight();
                float3 viewDir   = normalize(GetCameraPositionWS() - input.positionWS);
                float3 halfDir   = normalize(mainLight.direction + viewDir);

                // NdotL remap [−1,1]→[0,1] pentru a nu tăia complet umbra
                float NdotL = dot(normalWS, mainLight.direction) * 0.5 + 0.5;
                float NdotH = saturate(dot(normalWS, halfDir));
                float NdotV = saturate(dot(normalWS, viewDir));

                // ── TOON Difuz: lumina în trepte ──────────────────────────
                // 3 trepte = umbra (0.55) / semi-lumina (0.78) / lumina plina (1.0)
                float toonDiff = ToonRamp(NdotL, _ToonSteps, _ToonSmoothness);
                finalColor    *= lerp(0.50, 1.0, toonDiff) * mainLight.color;

                // ── TOON Specular: un singur highlight dur, alb ───────────
                // pow ridică NdotH la putere mare → un cerc mic intens
                // smoothstep îl transformă dintr-un gradient în hard-cut
                float specRaw  = pow(NdotH, exp2(_Smoothness * 10.0 + 1.0));
                float toonSpec = smoothstep(
                    _ToonSpecSize - _ToonSpecSmooth,
                    _ToonSpecSize + _ToonSpecSmooth,
                    specRaw);
                finalColor += mainLight.color * toonSpec * 0.85;

                // ── TOON Rim Light: contur luminos la marginea siluetei ───
                // NdotV mic = unghi rasant = margine de siluetă
                float rimRaw  = 1.0 - NdotV;
                float toonRim = smoothstep(
                    _ToonRimSize - _ToonRimSmooth,
                    _ToonRimSize + _ToonRimSmooth,
                    rimRaw);
                finalColor = lerp(finalColor, _RimColor.rgb, toonRim * 0.40);

                // ── Fresnel ───────────────────────────────────────────────
                float fresnel = pow(1.0 - NdotV, _FresnelPower);
                finalColor    = lerp(finalColor, _FresnelColor.rgb, fresnel * 0.25);

                // ── Spumă Toon ────────────────────────────────────────────
                // foamMask: hard-cut pe adâncime (nu gradient, ci linie clară)
                float foamMask   = 1.0 - saturate(depthDiff / max(0.001, _FoamDepthThreshold));
                foamMask         = smoothstep(0.35, 0.55, foamMask); // margine moale-dură
                // Noise animat, și el discretizat cu step()
                float foamNoise  = saturate(sin((blendedN.x + blendedN.y) * 7.0 + t * 2.5) * 0.5 + 0.5);
                foamNoise        = step(0.45, foamNoise);
                float foamFactor = foamMask * foamNoise;
                finalColor       = lerp(finalColor, _FoamColor.rgb, foamFactor * _FoamColor.a);

                // ── Alpha ─────────────────────────────────────────────────
                float alpha = lerp(_ShallowColor.a, _DeepColor.a, depthBanded);
                alpha = max(alpha, _MinAlpha);
                alpha = max(alpha, foamFactor);

                // ── Fade pe distanță față de cameră ──────────────────────
                float distFade = 1.0 - saturate(
                    (waterDepthM - _VisibilityStart) /
                    max(0.001, _VisibilityEnd - _VisibilityStart));
                alpha *= distFade;

                return half4(finalColor, alpha);
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}