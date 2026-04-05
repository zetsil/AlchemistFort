Shader "Custom/CartoonToonClouds_NightShift"
{
    Properties
    {
        _MainTex ("Cloud Noise 1", 2D) = "white" {}
        _SecondaryTex ("Cloud Noise 2", 2D) = "white" {}
        _Color ("Cloud Day Color", Color) = (1,1,1,1)
        _ShadowColor ("Cloud Day Shadow", Color) = (0.8, 0.8, 1, 1)
        
        // --- CULORI DE NOAPTE ---
        _NightColor ("Cloud Night Color", Color) = (0.2, 0.1, 0.3, 1)    // Mov închis
        _NightShadow ("Cloud Night Shadow", Color) = (0.05, 0.02, 0.1, 1) // Negru-Mov intens

        _Speed1 ("Speed Noise 1", Vector) = (0.02, 0.01, 0, 0)
        _Speed2 ("Speed Noise 2", Vector) = (-0.01, 0.03, 0, 0)
        _Cutoff ("Cloud Edge (Threshold)", Range(0, 1)) = 0.5
        _ShadowThreshold ("Shadow Threshold", Range(0, 1)) = 0.6
        
        _FadeStart ("Fade Start Distance", Range(0, 1)) = 0.6 
        _FadeEnd ("Fade End Distance", Range(0, 1)) = 0.9 
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM // Folosim HLSL pentru compatibilitate mai bună cu URP
            #pragma vertex vert
            #pragma fragment frag
            
            // Includem bibliotecile URP pentru a lua datele despre soare
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 worldUV : TEXCOORD1;
            };

            sampler2D _MainTex;
            sampler2D _SecondaryTex;
            float4 _MainTex_ST;
            float4 _Color, _ShadowColor, _NightColor, _NightShadow;
            float2 _Speed1, _Speed2;
            float _Cutoff, _ShadowThreshold, _FadeStart, _FadeEnd;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = TransformObjectToHClip(v.vertex.xyz);
                o.uv = v.uv * _MainTex_ST.xy + _MainTex_ST.zw;
                o.worldUV = v.uv;
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                // 1. Detectăm intensitatea soarelui
                Light mainLight = GetMainLight();
                float sunIntensity = saturate((mainLight.color.r + mainLight.color.g + mainLight.color.b) / 3.0);
                
                // 2. Mișcarea norilor
                float2 uv1 = i.uv + _Time.y * _Speed1;
                float2 uv2 = i.uv + _Time.y * _Speed2;

                float n1 = tex2D(_MainTex, uv1).r;
                float n2 = tex2D(_SecondaryTex, uv2).r;
                float combinedNoise = n1 * n2;

                // 3. Calculăm culorile curente (Tranziție Zi -> Noapte)
                // lerp(Noapte, Zi, sunIntensity)
                float3 currentColor = lerp(_NightColor.rgb, _Color.rgb, sunIntensity);
                float3 currentShadow = lerp(_NightShadow.rgb, _ShadowColor.rgb, sunIntensity);

                // 4. Toon Shading
                float cloudAlpha = step(_Cutoff, combinedNoise);
                float isShadow = step(_ShadowThreshold, combinedNoise);
                float3 finalRGB = lerp(currentShadow, currentColor, isShadow);

                // 5. Fade pe margini (Circular)
                float2 centerUV = float2(0.5, 0.5);
                float distFromCenter = distance(i.worldUV, centerUV) * 2.0;
                float radialFadeMask = 1.0 - smoothstep(_FadeStart, _FadeEnd, distFromCenter);

                // 6. Rezultat Final
                // Opțional: Putem face norii mai transparenți noaptea scăzând sunIntensity din alpha
                float finalAlpha = cloudAlpha * radialFadeMask;

                return half4(finalRGB, finalAlpha);
            }
            ENDHLSL
        }
    }
}