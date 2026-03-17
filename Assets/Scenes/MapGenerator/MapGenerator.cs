using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    public PerlinSettings settings;
    
    [Header("Referință Teren")]
    public Terrain terrain;

    [Header("Referință Apă (Plane)")]
    public Transform waterPlane;
    [Tooltip("Dacă apa ar trebui să fie exact la nivelul câmpiei sau puțin sub.")]
    public float waterOffset = 0.1f;
    
    public bool autoUpdate = true;

    private void OnEnable()
    {
        if (settings != null) settings.OnSettingsUpdated += HandleSettingsUpdated;
    }

    private void OnDisable()
    {
        if (settings != null) settings.OnSettingsUpdated -= HandleSettingsUpdated;
    }

    private void HandleSettingsUpdated()
    {
        if (autoUpdate) GenerateMap();
    }

    [ContextMenu("Generate Map")]
    public void GenerateMap()
    {
        if (settings == null || terrain == null) 
        {
            Debug.LogWarning("Asignează setările și obiectul Terrain în Inspector!");
            return;
        }

        float[,] noiseMap = settings.GenerateMap();
        ApplyToTerrain(noiseMap);
        UpdateWaterLevel();
    }

    private void ApplyToTerrain(float[,] map)
    {
        TerrainData terrainData = terrain.terrainData;
        int width = map.GetLength(0);
        int height = map.GetLength(1);

        terrainData.heightmapResolution = width;
        terrainData.size = new Vector3(width, settings.terrainHeightMultiplier, height);

        float[,] finalHeights = new float[width, height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                finalHeights[y, x] = map[x, y];
            }
        }

        terrainData.SetHeights(0, 0, finalHeights);
    }

    private void UpdateWaterLevel()
    {
        if (waterPlane == null) return;

        // Calculăm înălțimea apei: (procent nivelCampie * înălțime maximă) - un mic offset
        // Astfel apa va acoperi gropile dar nu va trece de zona plată a câmpiei
        float waterY = (settings.nivelCampie * settings.terrainHeightMultiplier) - waterOffset;

        // Actualizăm poziția planului
        Vector3 pos = waterPlane.position;
        pos.y = waterY;
        
        // Centrăm apa pe mijlocul terenului
        pos.x = settings.width / 2f;
        pos.z = settings.height / 2f;
        
        waterPlane.position = pos;

        // Scalăm planul să acopere tot terenul (Plane-ul default are 10x10 unități)
        float scaleX = settings.width / 10f;
        float scaleZ = settings.height / 10f;
        waterPlane.localScale = new Vector3(scaleX, 1f, scaleZ);
    }
}