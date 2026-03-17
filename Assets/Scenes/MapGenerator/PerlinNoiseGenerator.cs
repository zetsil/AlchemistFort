using UnityEngine;

public static class PerlinNoiseGenerator
{
    public static float[,] GenerateNoiseMap(int width, int height, int seed, float scale, int octaves, float persistence, float lacunarity, float nivelCampie, float nivelMunte)
    {
        float[,] noiseMap = new float[width, height];

        System.Random prng = new System.Random(seed);
        Vector2[] octaveOffsets = new Vector2[octaves];
        for (int i = 0; i < octaves; i++)
        {
            float offsetX = prng.Next(-100000, 100000);
            float offsetY = prng.Next(-100000, 100000);
            octaveOffsets[i] = new Vector2(offsetX, offsetY);
        }

        if (scale <= 0) scale = 0.0001f;

        float maxNoiseHeight = float.MinValue;
        float minNoiseHeight = float.MaxValue;

        // PASUL 1: Generare Zgomot Brut
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float amplitude = 1;
                float frequency = 1;
                float noiseHeight = 0;

                for (int i = 0; i < octaves; i++)
                {
                    float sampleX = (x + octaveOffsets[i].x) / scale * frequency;
                    float sampleY = (y + octaveOffsets[i].y) / scale * frequency;

                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
                    noiseHeight += perlinValue * amplitude;

                    amplitude *= persistence;
                    frequency *= lacunarity;
                }

                if (noiseHeight > maxNoiseHeight) maxNoiseHeight = noiseHeight;
                if (noiseHeight < minNoiseHeight) minNoiseHeight = noiseHeight;
                
                noiseMap[x, y] = noiseHeight;
            }
        }
        
        float intensitateVariatieCampie = 0.02f; 

        // PASUL 2: Remapare cu Slidere (Logic pentru munți înalți)
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float v = Mathf.InverseLerp(minNoiseHeight, maxNoiseHeight, noiseMap[x, y]);

                if (v < nivelCampie)
                {
                    // GROPI: Păstrăm progresia normală sub nivelul câmpiei
                    noiseMap[x, y] = v;
                }
                else if (v >= nivelCampie && v <= nivelMunte)
                {
                    // CÂMPIE: Calculăm o variație lină între nivelCampie și nivelCampie + intensitate
                    float t = (v - nivelCampie) / (nivelMunte - nivelCampie);
                    noiseMap[x, y] = nivelCampie + (t * intensitateVariatieCampie);
                }
                else
                {
                    // MUNȚI: Eliminăm "zidul" prin pornirea pantei exact de la nivelul final al câmpiei
                    float t = (v - nivelMunte) / (1f - nivelMunte);

                    // Exponent pentru vârfuri ascuțite
                    t = Mathf.Pow(t, 1.3f);

                    // Punctul de start al muntelui este nivelul maxim la care a ajuns câmpia
                    float startMunte = nivelCampie + intensitateVariatieCampie;
                    noiseMap[x, y] = startMunte + (t * (1f - startMunte));
                }
            }
        }

        return noiseMap;
    }
}