using UnityEngine;
using System;

[CreateAssetMenu(fileName = "NewPerlinSettings", menuName = "Map/Perlin Settings")]
public class PerlinSettings : ScriptableObject
{
    public event Action OnSettingsUpdated;

    [Header("Setări Zgomot (Noise)")]
    public int seed = 1337;
    public float scale = 50f;
    [Range(1, 10)] public int octaves = 4;
    [Range(0, 1)] public float persistence = 0.5f;
    public float lacunarity = 2f;

    [Header("Reguli Formă Teren (Slidere)")]
    [Tooltip("Până la ce valoare sunt gropi (lacuri).")]
    [Range(0f, 1f)] 
    public float nivelCampie = 0.3f; 

    [Tooltip("De la ce valoare încep munții.")]
    [Range(0f, 1f)] 
    public float nivelMunte = 0.5f;

    [Header("Lac Central & Câmpie")]
    public bool createCentralLake = true;
    [Tooltip("Raza apei din centrul hărții.")]
    public float lakeRadius = 25f;
    [Tooltip("Raza zonei plate din jurul lacului.")]
    public float plainRadius = 50f;
    [Tooltip("Adâncimea lacului (0 = nivelul apei, sub 1 = adânc).")]
    [Range(0f, 1f)]
    public float lakeDepthMultiplier = 0.5f;

    [Header("Dimensiuni Teren")]
    public float terrainHeightMultiplier = 50f;
    public int width = 257; 
    public int height = 257;
    bool tesssssst = false;

    public float[,] GenerateMap()
    {
        // Corecție automată slidere
        if (nivelMunte < nivelCampie) nivelMunte = nivelCampie;
        if (plainRadius < lakeRadius) plainRadius = lakeRadius;

        return PerlinNoiseGenerator.GenerateNoiseMap(
            width, height, seed, scale, octaves, persistence, lacunarity,
            nivelCampie, nivelMunte
        );
    }

    private void OnValidate()
    {
        if (nivelMunte < nivelCampie) nivelMunte = nivelCampie;
        if (plainRadius < lakeRadius) plainRadius = lakeRadius;

        #if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        #endif
        
        OnSettingsUpdated?.Invoke();
    }
}//d