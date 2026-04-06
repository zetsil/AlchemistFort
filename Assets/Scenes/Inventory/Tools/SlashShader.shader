Shader "Custom/AnimeSlashTrail_URP"
{
    Properties
    {
        // --- Texturi și Culori ---
        _MainTex        ("Texture (RGBA)", 2D)      = "white" {}
        _NoiseTex       ("Noise Texture (Greyscale)", 2D) = "white" {} // Necesită o textură de noise

        [HDR] _Color            ("Base Color",       Color) = (1, 1, 1, 1)
        [HDR] _GlowColor        ("Glow Color",       Color) = (0.5, 0.8, 1.0, 1) // Culoare diferită pentru margini/intensitate

        // --- Parametri Efec ---
        _GlowIntensity          ("Glow Intensity",   Range(0, 5))   = 2.5
        _TipSharpness           ("Tip Sharpness",    Range(0.5, 8)) = 3.0 // Cât de ascuțit e vârful/coada
        
        // --- Parametri Margini (Edge Glow) ---
        _EdgeGlow               ("Edge Glow Width",  Range(0, 0.5)) = 0.15
        _EdgeGlowIntensity      ("Edge Glow Intensity", Range(0, 5)) = 2.0
        
        // --- Parametri Distorsiune/Noise ---
        _Distortion             ("Distortion Strength", Range(0, 0.05)) = 0.01
        _DistortionSpeed        ("Distortion Speed", Range(0, 10))  = 4.0
        
        // --- Parametri Anime Scanlines ---
        _ScanlineCount          ("Scanline Count",   Range(0, 30))  = 8.0
        _ScanlineStrength       ("Scanline Strength",Range(0, 1))   = 0.25
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline" // IMPORTANT: Acest shader e conceput pentru URP
            "RenderType"     = "Transparent"
            "Queue"          = "Transparent"
            "IgnoreProjector"= "True"
        }

        LOD 100
        Cull Off
        ZWrite Off
        Blend SrcAlpha One      // Additive — luminos, anime-style

        Pass
        {
            Name "AnimeTrailPass"

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            // URP includes
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // Textura si sampler
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);

            // Constant buffer — obligatoriu in URP
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _NoiseTex_ST;
                half4  _Color;
                half4  _GlowColor;
                half   _GlowIntensity;
                half   _TipSharpness;
                half   _EdgeGlow;
                half   _EdgeGlowIntensity;
                half   _Distortion;
                half   _DistortionSpeed;
                half   _ScanlineCount;
                half   _ScanlineStrength;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                half4  color      : COLOR;      // alpha per-vertex din Trail Renderer
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                half4  color      : COLOR;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                // TransformObjectToHClip = echivalentul URP al UnityObjectToClipPos
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv         = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color      = IN.color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;

                // --- 1. Distorsiune ondulata (Anime/Energy style) ---
                // Folosim sinus si timp pentru a ondula coordonatele UV.y pe orizontala.
                float2 distUV = uv;
                distUV.y += sin(uv.x * 15.0 + _Time.y * _DistortionSpeed) * _Distortion;

                // --- 2. Sample Textura (cu distorsiune) ---
                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, distUV);
                // Înmultim textura cu culoarea de bază HDR și alpha vertex-ului (Trail Fade)
                col *= _Color * IN.color;

                // --- 3. Forțăm Fade Coadă -> Cap (UV.x: 0=cap, 1=coadă) ---
                // Folosim pow pentru a prăbuși alpha la coadă mult mai repede.
                // Cu cât _TipSharpness e mai mare, cu atât vârful e mai ascuțit.
                half fade = pow(1.0 - uv.x, _TipSharpness);

                // --- 4. Efect Margini Stralucitoare (Edge Glow) ---
                // Calculăm distanța de la centrul vertical (uv.y - 0.5)
                half distFromCenter = abs(uv.y - 0.5) * 2.0; // 0 la centru, 1 la margini
                
                // Cream o mască pentru margini folosind smoothstep.
                half edgeMask       = smoothstep(1.0 - _EdgeGlow, 1.0, distFromCenter);
                half4 edgeGlowC     = _GlowColor * edgeMask * _EdgeGlowIntensity * fade;

                // --- 5. Efect Centru Luminos ---
                // Un glow general, concentrat pe centru.
                half centerMask  = 1.0 - distFromCenter;
                half centerGlow  = smoothstep(0.3, 1.0, centerMask);
                half4 centerGlowC = _GlowColor * centerGlow * _GlowIntensity * fade;

                // --- 6. Anime Scanlines (Linii digitale) ---
                // Folosim sin pe UV.y pentru a crea linii orizontale.
                half scanline     = sin(uv.y * _ScanlineCount * PI) * 0.5 + 0.5;
                // Mascăm liniile, făcându-le să dispară spre coadă.
                half scanlineMask = scanline * _ScanlineStrength * fade;

                // --- 7. Compozitie Finală ---
                // Adunăm efectele (Additive blending in shader si in cod)
                half4 final = col + edgeGlowC + centerGlowC;
                
                // Adăugăm scanlines doar ca intensitate de culoare
                final.rgb  += final.rgb * scanlineMask;
                
                // Setăm Alpha Final: Textura * Fade Coadă * Alpha Trail Renderer
                final.a     = col.a * fade * IN.color.a;

                return final;
            }
            ENDHLSL
        }
    }

    FallBack Off
}