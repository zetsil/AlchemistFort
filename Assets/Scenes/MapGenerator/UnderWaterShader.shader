Shader "Custom/UnderwaterEffect"
{
    Properties
    {
        _MainTex ("Scene Texture", 2D) = "white" {}
        _ColorTint ("Water Tint", Color) = (0, 0.4, 0.6, 0.3)
        _DistortionSpeed ("Distortion Speed", Float) = 1.0
        _DistortionStrength ("Distortion Strength", Range(0, 0.1)) = 0.02
        _WaveFreq ("Wave Frequency", Float) = 10.0
    }

    SubShader
    {
        // Tag-uri necesare pentru UI Image sau Post-Process
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100

        Pass
        {
            ZWrite Off
            ZTest Always
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _ColorTint;
            float _DistortionSpeed;
            float _DistortionStrength;
            float _WaveFreq;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Calculăm distorsiunea folosind funcții de sin/cos bazate pe timp și coordonatele UV
                float2 uvDistortion;
                uvDistortion.x = sin(i.uv.y * _WaveFreq + _Time.y * _DistortionSpeed) * _DistortionStrength;
                uvDistortion.y = cos(i.uv.x * _WaveFreq + _Time.y * _DistortionSpeed) * _DistortionStrength;

                // Aplicăm distorsiunea peste textura principală (ce vede camera)
                fixed4 col = tex2D(_MainTex, i.uv + uvDistortion);

                // Amestecăm imaginea distorsionată cu culoarea apei
                fixed4 finalColor = lerp(col, _ColorTint, _ColorTint.a);
                
                return finalColor;
            }
            ENDCG
        }
    }
}