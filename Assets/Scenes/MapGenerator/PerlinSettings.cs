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
    [Tooltip("Până la ce valoare sunt gropi. Aici începe zona plată.")]
    [Range(0f, 1f)] 
    public float nivelCampie = 0.3f; 

    [Tooltip("De la ce valoare se termină zona plată și încep munții.")]
    [Range(0f, 1f)] 
    public float nivelMunte = 0.5f;

    [Header("Dimensiuni Teren")]
    public float terrainHeightMultiplier = 50f;
    public int width = 257; // 257 este optim pentru Unity Terrain
    public int height = 257;

    public float[,] GenerateMap()
    {
        // Ne asigurăm că sliderul de munte nu e tras accidental sub cel de câmpie
        if (nivelMunte < nivelCampie) nivelMunte = nivelCampie;

        // Trimitem valorile sliderelor către generator
        return PerlinNoiseGenerator.GenerateNoiseMap(width, height, seed, scale, octaves, persistence, lacunarity, nivelCampie, nivelMunte);
    }

    private void OnValidate()
    {
        // Validare în timp real în Inspector
        if (nivelMunte < nivelCampie) nivelMunte = nivelCampie;

        #if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        #endif
        
        OnSettingsUpdated?.Invoke();
    }
}