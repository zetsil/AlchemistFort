Shader "Custom/URP_Instanced_Wind"
{
    Properties
    {
        _BaseMap("Texture", 2D) = "white" {}
        _AmbientColor("Night Tint", Color) = (0.2, 0.2, 0.2, 1)
        
        // Proprietăți pentru Vânt
        _WindSpeed("Viteza Vantului", Float) = 1.5
        _WindStrength("Intensitate Vant", Float) = 0.3
        _WindVerticalScale("Scara Verticala (Inaltime)", Float) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _AmbientColor;
                float _WindSpeed;
                float _WindStrength;
                float _WindVerticalScale;
            CBUFFER_END

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            Varyings vert(Attributes input) {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                // --- LOGICA DE VANT (Vertex Animation) ---
                // Folosim matricea de instanțiere pentru a extrage poziția obiectului în lume
                float3 worldOrigin = float3(UNITY_MATRIX_M[0][3], UNITY_MATRIX_M[1][3], UNITY_MATRIX_M[2][3]);
                float phase = worldOrigin.x + worldOrigin.z;
                
                // Mască bazată pe înălțimea locală (baza rămâne fixă, vârful se mișcă)
                float windMask = saturate(input.positionOS.y * _WindVerticalScale);
                float sway = sin(_Time.y * _WindSpeed + phase) * _WindStrength * windMask;
                
                float3 vPos = input.positionOS.xyz;
                vPos.x += sway;
                vPos.z += sway * 0.5;

                output.positionCS = TransformObjectToHClip(vPos);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 frag(Varyings input) : SV_Target {
                UNITY_SETUP_INSTANCE_ID(input);

                // 1. Detectăm lumina soarelui pentru desaturare
                Light mainLight = GetMainLight();
                float sunIntensity = saturate((mainLight.color.r + mainLight.color.g + mainLight.color.b) / 3.0);

                // 2. Sample Textură
                half4 texColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                
                // 3. Logica de Noapte (Gri)
                float gray = dot(texColor.rgb, float3(0.2126, 0.7152, 0.0722));
                float3 grayColor = float3(gray, gray, gray);

                // Tranziția spre gri bazată pe cât de mult "soare" avem
                float desaturateAmount = saturate(1.0 - (sunIntensity * 2.0)); 
                float3 finalBaseColor = lerp(texColor.rgb, grayColor, desaturateAmount);

                // 4. Aplicăm culoarea luminii (plat, fără umbre de unghi)
                float3 lighting = lerp(_AmbientColor.rgb, mainLight.color, sunIntensity);
                float3 finalRGB = finalBaseColor * lighting;

                return half4(finalRGB, texColor.a);
            }
            ENDHLSL
        }
    }
}