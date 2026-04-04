using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public struct TerrainTextureSettings
{
    public string name;
    public TerrainLayer layer;
    [Range(0f, 1f)] public float startHeight;
    [Range(0f, 1f)] public float blendStrength;
}


[System.Serializable]
public class VegetationSettings
{
    public string name;
    public Mesh mesh;
    public Material material;
    [Range(0f, 1f)] public float density = 0.3f;        // Cât de des apare
    [Range(0f, 1f)] public float minHeight = 0.35f;     // Înălțime minimă teren
    [Range(0f, 1f)] public float maxHeight = 0.45f;     // Înălțime maximă teren
    public float maxSlope = 20f;                         // Pantă maximă
    public Vector2 scaleRange = new Vector2(0.6f, 1.0f);
    public float noiseScale = 40f;                       // Scala noise-ului de distribuție
    public float lakeOffset = 5f;                        // Distanță față de lac
}


[System.Serializable]
public class FlowerSettings
{
    public string name;
    public Mesh mesh;
    public Material material;
    [Range(0, 1)] public float spawnChance = 0.05f; // Cât de des apare față de iarbă
    public Vector2 scaleRange = new Vector2(0.5f, 1.2f);
}


[System.Serializable]
public class UnderTreeSettings
{
    public List<GameObject> prefabs; // Lista cu mere, crengi, etc.
    [Range(0, 5)] public int maxItems = 3;
    public float spawnRadius = 1.5f; // Cât de departe de trunchi să apară
}

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

    [Header("Texturi Teren")]
    [Tooltip("Adaugă texturi în ordine crescătoare a înălțimii (ex: Iarbă, Piatră, Zăpadă)")]
    public List<TerrainTextureSettings> textureSettings;

    [Header("Setări Vegetație")]
    public GameObject treePrefab;
    [Range(0, 100)] public int treeDensity = 50;
    [Range(0, 1)] public float treeMinHeight = 0.35f;
    [Range(0, 1)] public float treeMaxHeight = 0.6f;

    [Header("Setări Sub Copac")]
    public UnderTreeSettings underTreeSettings;

    [Header("GPU Multi Renderer")]
    private GPUMultiRenderer multiRenderer;

    [Header("Setări Iarbă GPU")]

    public Mesh grassMesh;
    public Material grassMaterial;
    private GPUInstanceData currentGrassRenderer;
    [Range(0.1f, 0.9f)] public float grassDensity = 0.4f;

    [Header("Setări Flori GPU")]
    public List<FlowerSettings> flowerSettingsList;
    public List<VegetationSettings> vegetationList;



    [Header("Setări Mining Rocks")]
    public GameObject miningRockPrefab;
    [Range(0, 100)] public int miningRockDensity = 30;
    public float miningMinHeight = 0.42f;
    public float miningMaxHeight = 0.65f;





    // ─────────────────────────────────────────────
    //  LIFECYCLE
    // ─────────────────────────────────────────────
    private void Awake()
    {
        EnsureRendererExists();
    }

    private void OnEnable()
    {
        if (settings != null)
        {
            settings.OnSettingsUpdated -= HandleSettingsUpdated;
            settings.OnSettingsUpdated += HandleSettingsUpdated;
        }
        GenerateMap();
    }

    private void OnDisable()
    {
        if (settings != null)
            settings.OnSettingsUpdated -= HandleSettingsUpdated;
    }

    private void HandleSettingsUpdated()
    {
        if (autoUpdate) GenerateMap();
    }

    private void EnsureRendererExists()
    {
        if (multiRenderer == null)
        {
            // Căutăm în copii dacă există deja (în caz de ierarhie persistentă)
            multiRenderer = GetComponentInChildren<GPUMultiRenderer>();

            if (multiRenderer == null)
            {
                GameObject go = new GameObject("GPU_Vegetation_Manager");
                go.transform.parent = this.transform;
                multiRenderer = go.AddComponent<GPUMultiRenderer>();
            }
        }
    }

    // ─────────────────────────────────────────────
    //  ENTRY POINT
    // ─────────────────────────────────────────────

    [ContextMenu("Generate Map")]
    public void GenerateMap()
    {
        if (settings == null || terrain == null)
        {
            Debug.LogWarning("Asignează setările și obiectul Terrain în Inspector!");
            return;
        }

        float[,] noiseMap = settings.GenerateMap();

        // Ordinea este importantă:
        SetupTerrainLayers();           // 1. Pregătim layerele (înainte de orice altceva)
        ApplyToTerrain(noiseMap);       // 2. Aplicăm heightmap-ul
        ApplyTexturesToTerrain(noiseMap); // 3. Texturăm după înălțime

        if (treePrefab != null)
            PlaceTrees();               // 4. Punem copacii

        GenerateVegetation();
        SpawnMiningRock();
        UpdateWaterLevel();            // 6. Poziționăm apa (nu afectează TerrainData)
    }

    // ─────────────────────────────────────────────
    //  TERRAIN SHAPE
    // ─────────────────────────────────────────────

    private void ApplyToTerrain(float[,] map)
    {
        TerrainData terrainData = terrain.terrainData;
        int width = map.GetLength(0);
        int height = map.GetLength(1);

        terrainData.heightmapResolution = width;
        terrainData.size = new Vector3(width, settings.terrainHeightMultiplier, height);

        float[,] finalHeights = new float[height, width];
        Vector2 center = new Vector2(width / 2f, height / 2f);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float noiseValue = map[x, y];

                if (settings.createCentralLake)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);

                    if (dist < settings.lakeRadius)
                    {
                        // PÂLNIE: Calculăm cât de aproape suntem de centru (0 = centru, 1 = marginea lacului)
                        float t = dist / settings.lakeRadius;

                        // Nivelul fundului lacului (cel mai adânc punct)
                        float hFund = noiseValue * (settings.nivelCampie * settings.lakeDepthMultiplier);

                        // Nivelul marginii lacului (unde începe câmpia)
                        float hMargine = settings.nivelCampie + (noiseValue * 0.02f);

                        // Interpolăm între fund și margine pentru a crea panta de pâlnie
                        // Folosim SmoothStep pentru o curbură mai naturală (nu doar o linie dreaptă)
                        float smoothT = Mathf.SmoothStep(0, 1, t);
                        noiseValue = Mathf.Lerp(hFund, hMargine, smoothT);
                    }
                    else if (dist < settings.plainRadius)
                    {
                        // Câmpie plată
                        noiseValue = settings.nivelCampie + (noiseValue * 0.02f);
                    }
                    else
                    {
                        // Tranziție munte
                        float transitionWidth = 25f;
                        float t = Mathf.Clamp01((dist - settings.plainRadius) / transitionWidth);
                        float hCampie = settings.nivelCampie + (noiseValue * 0.02f);
                        noiseValue = Mathf.Lerp(hCampie, noiseValue, t);
                    }
                }

                finalHeights[y, x] = noiseValue;
            }
        }

        terrainData.SetHeights(0, 0, finalHeights);
    }

    // ─────────────────────────────────────────────
    //  TEXTURI
    // ─────────────────────────────────────────────

    private void SetupTerrainLayers()
    {
        if (textureSettings == null || textureSettings.Count == 0) return;

        TerrainLayer[] layers = new TerrainLayer[textureSettings.Count];
        for (int i = 0; i < textureSettings.Count; i++)
        {
            if (textureSettings[i].layer == null)
            {
                Debug.LogError($"Lipsește TerrainLayer la indexul {i}!");
                return;
            }
            layers[i] = textureSettings[i].layer;
        }

        terrain.terrainData.terrainLayers = layers;

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(terrain.terrainData);
#endif
    }

    private void ApplyTexturesToTerrain(float[,] map)
    {
        TerrainData terrainData = terrain.terrainData;
        int alphaW = terrainData.alphamapWidth;
        int alphaH = terrainData.alphamapHeight;
        int numLayers = textureSettings.Count;

        float[,,] splatmap = new float[alphaH, alphaW, numLayers];

        for (int y = 0; y < alphaH; y++)
        {
            for (int x = 0; x < alphaW; x++)
            {
                // Coordonate normalizate centrate pe pixel
                float normX = (x + 0.5f) / alphaW;
                float normY = (y + 0.5f) / alphaH;

                float currentHeight = terrainData.GetInterpolatedHeight(normX, normY) / terrainData.size.y;

                float[] weights = new float[numLayers];

                for (int i = 0; i < numLayers; i++)
                {
                    if (currentHeight >= textureSettings[i].startHeight)
                    {
                        // Resetăm straturile inferioare
                        for (int j = 0; j < i; j++) weights[j] = 0f;
                        weights[i] = 1f;

                        // Blending lin cu stratul anterior
                        if (i > 0 && textureSettings[i].blendStrength > 0f)
                        {
                            float blendStart = textureSettings[i].startHeight - textureSettings[i].blendStrength;
                            float blendFactor = Mathf.InverseLerp(blendStart, textureSettings[i].startHeight, currentHeight);
                            weights[i] = blendFactor;
                            weights[i - 1] = 1f - blendFactor;
                        }
                    }
                }

                for (int i = 0; i < numLayers; i++)
                    splatmap[y, x, i] = weights[i];
            }
        }

        terrainData.SetAlphamaps(0, 0, splatmap);
    }

    // ─────────────────────────────────────────────
    //  Vegetation
    // ─────────────────────────────────────────────

    public void GenerateVegetation()
    {
        if (multiRenderer == null) EnsureRendererExists();
        
        // Ștergem datele vechi
        multiRenderer.ClearAll();

        TerrainData terrainData = terrain.terrainData;
        int sampleRes = 256;
        Vector2 center = new Vector2(sampleRes / 2f, sampleRes / 2f);
        float gridLakeRadius = (settings.lakeRadius / terrainData.heightmapResolution) * sampleRes;
        float spacing = terrainData.size.x / sampleRes;

        // O listă per tip de vegetație
        Dictionary<int, List<Matrix4x4>> transforms = new Dictionary<int, List<Matrix4x4>>();
        for (int i = 0; i < vegetationList.Count; i++)
            transforms[i] = new List<Matrix4x4>();

        // Seed diferit per tip ca să nu se suprapună pattern-urile
        System.Random[] prngs = new System.Random[vegetationList.Count];
        for (int i = 0; i < vegetationList.Count; i++)
            prngs[i] = new System.Random(settings.seed + i * 137);

        for (int y = 0; y < sampleRes; y++)
        {
            for (int x = 0; x < sampleRes; x++)
            {
                float normX = (float)x / (sampleRes - 1);
                float normY = (float)y / (sampleRes - 1);

                float height = terrainData.GetInterpolatedHeight(normX, normY);
                float normHeight = height / terrainData.size.y;
                float slope = terrainData.GetSteepness(normX, normY);
                float distToCenter = Vector2.Distance(new Vector2(x, y), center);

                for (int i = 0; i < vegetationList.Count; i++)
                {
                    var veg = vegetationList[i];

                    // Verificări de bază
                    if (distToCenter < gridLakeRadius + veg.lakeOffset) continue;
                    if (normHeight < veg.minHeight || normHeight > veg.maxHeight) continue;
                    if (slope > veg.maxSlope) continue;

                    // Noise unic per tip — seed diferit ca să nu se suprapună
                    float noise = Mathf.PerlinNoise(
                        normX * veg.noiseScale + settings.seed + i * 13.7f,
                        normY * veg.noiseScale + settings.seed + i * 13.7f
                    );

                    // density controlează direct pragul noise-ului
                    if (noise < (1f - veg.density)) continue;

                    // Offset aleator ca să nu fie pe grid
                    float offsetX = ((float)prngs[i].NextDouble() - 0.5f) * spacing * 1.5f;
                    float offsetZ = ((float)prngs[i].NextDouble() - 0.5f) * spacing * 1.5f;

                    Vector3 pos = new Vector3(
                        normX * terrainData.size.x + offsetX,
                        0,
                        normY * terrainData.size.z + offsetZ
                    ) + terrain.transform.position;

                    float finalNormX = Mathf.Clamp01((pos.x - terrain.transform.position.x) / terrainData.size.x);
                    float finalNormZ = Mathf.Clamp01((pos.z - terrain.transform.position.z) / terrainData.size.z);
                    pos.y = terrainData.GetInterpolatedHeight(finalNormX, finalNormZ) + terrain.transform.position.y;

                    Quaternion rot = Quaternion.Euler(0, (float)prngs[i].NextDouble() * 360f, 0);
                    float s = Mathf.Lerp(veg.scaleRange.x, veg.scaleRange.y, (float)prngs[i].NextDouble());

                    transforms[i].Add(Matrix4x4.TRS(pos, rot, Vector3.one * s));
                }
            }
        }

        // Trimitem la renderer
        for (int i = 0; i < vegetationList.Count; i++)
        {
            if (transforms[i].Count == 0) continue;

            GPUInstanceData data = new GPUInstanceData
            {
                name = vegetationList[i].name,
                mesh = vegetationList[i].mesh,
                material = vegetationList[i].material
            };
            data.Initialize(transforms[i]);
            multiRenderer.AddRenderData(data);

            Debug.Log($"[Vegetation] {vegetationList[i].name}: {transforms[i].Count} instanțe");
        }
    }


    
    // ─────────────────────────────────────────────
    //  COPACI
    // ─────────────────────────────────────────────

    public void PlaceTrees()
    {

        string containerName = "Generated_Trees";
        Transform oldContainer = transform.Find(containerName);
        if (oldContainer != null) DestroyImmediate(oldContainer.gameObject);

        GameObject treeContainer = new GameObject(containerName);
        treeContainer.transform.parent = this.transform;

        System.Random prng = new System.Random(settings.seed);
        TerrainData terrainData = terrain.terrainData;

        float mapWidth = terrainData.size.x;
        float mapZ = terrainData.size.z;
        int res = terrainData.heightmapResolution;

        int step = Mathf.Max(2, 110 - treeDensity);

        // Centrul hărții pentru calculul distanței față de lac
        Vector2 center = new Vector2(res / 2f, res / 2f);

        for (int y = 0; y < res; y += step)
        {
            for (int x = 0; x < res; x += step)
            {
                float xPos = x + (float)prng.NextDouble() * step;
                float yPos = y + (float)prng.NextDouble() * step;

                float normX = xPos / res;
                float normY = yPos / res;

                float currentHeight = terrainData.GetInterpolatedHeight(normX, normY);
                float currentHeightNormalized = currentHeight / terrainData.size.y;
                float slope = terrainData.GetSteepness(normX, normY);

                // CALCUL DISTANȚĂ FAȚĂ DE LAC
                float distToCenter = Vector2.Distance(new Vector2(xPos, yPos), center);

                // CONDIȚIE NOUĂ: distToCenter > settings.lakeRadius + offset
                // Am adăugat + 9f ca să nu stea copacii fix în buza apei
                if (distToCenter > (settings.lakeRadius + 9f) &&
                    currentHeightNormalized >= treeMinHeight &&
                    currentHeightNormalized <= treeMaxHeight &&
                    slope < 30f)
                {
                    Vector3 worldPos = new Vector3(
                        normX * mapWidth,
                        currentHeight,
                        normY * mapZ
                    ) + terrain.transform.position;

                    float randomRotation = (float)prng.NextDouble() * 360f;
                    GameObject tree = Instantiate(treePrefab, worldPos, Quaternion.Euler(0, randomRotation, 0));
                    tree.transform.parent = treeContainer.transform;

                    float scale = 0.8f + (float)prng.NextDouble() * 0.4f;
                    tree.transform.localScale = Vector3.one * scale;

                    if (underTreeSettings.prefabs.Count > 0)
                    {
                        GenerateUnderTreeItems(worldPos, treeContainer.transform);
                    }
                }
            }
        }
    }

    private void GenerateUnderTreeItems(Vector3 treePos, Transform parent)
    {
        if (underTreeSettings.prefabs == null || underTreeSettings.prefabs.Count == 0) return;

        // Creăm un SEED LOCAL bazat pe poziția X și Z a copacului.
        // Înmulțim cu numere mari ca să evităm seed-uri similare pentru copaci apropiați.
        int localSeed = (int)(treePos.x * 1000 + treePos.z * 1000);
        System.Random itemPrng = new System.Random(localSeed);

        // Acum folosim itemPrng (nu prng-ul principal de la PlaceTrees)
        int itemCount = itemPrng.Next(0, underTreeSettings.maxItems + 1);

        for (int i = 0; i < itemCount; i++)
        {
            float angle = (float)itemPrng.NextDouble() * Mathf.PI * 2f;
            float distance = (float)itemPrng.NextDouble() * underTreeSettings.spawnRadius;

            Vector3 offset = new Vector3(Mathf.Cos(angle) * distance, 0, Mathf.Sin(angle) * distance);
            Vector3 spawnPos = treePos + offset;

            // Punem obiectul pe sol (Terrain)
            spawnPos.y = terrain.SampleHeight(spawnPos) + terrain.transform.position.y;

            int prefabIndex = itemPrng.Next(0, underTreeSettings.prefabs.Count);
            GameObject prefab = underTreeSettings.prefabs[prefabIndex];

            GameObject item = Instantiate(prefab, spawnPos, Quaternion.Euler(0, (float)itemPrng.NextDouble() * 360f, 0));
            item.transform.parent = parent;

            float scale = 0.8f + (float)itemPrng.NextDouble() * 0.4f;
            item.transform.localScale = Vector3.one * scale;
        }
    }
    // ─────────────────────────────────────────────
    //  APĂ
    // ─────────────────────────────────────────────

    private void UpdateWaterLevel()
    {
        if (waterPlane == null) return;

        float waterY = (settings.nivelCampie * settings.terrainHeightMultiplier) - waterOffset;

        Vector3 pos = waterPlane.position;
        pos.y = waterY;
        pos.x = settings.width / 2f;
        pos.z = settings.height / 2f;
        waterPlane.position = pos;

        float scaleX = settings.width / 10f;
        float scaleZ = settings.height / 10f;
        waterPlane.localScale = new Vector3(scaleX, 1f, scaleZ);
    }
    
    // ─────────────────────────────────────────────
    //  MINERIT
    // ─────────────────────────────────────────────

    public void SpawnMiningRock()
    {
        string containerName = "Generated_MiningRocks";
        Transform oldContainer = transform.Find(containerName);
        if (oldContainer != null) DestroyImmediate(oldContainer.gameObject);

        GameObject rockContainer = new GameObject(containerName);
        rockContainer.transform.parent = this.transform;

        System.Random prng = new System.Random(settings.seed + 888);
        TerrainData terrainData = terrain.terrainData;
        Vector3 terrainPos = terrain.transform.position;

        // Setăm o rezoluție fixă pentru eșantionare (nu neapărat rezoluția hărții)
        int sampleRes = 128; 
        float stepX = terrainData.size.x / sampleRes;
        float stepZ = terrainData.size.z / sampleRes;
        
        Vector2 center = new Vector2(0.5f, 0.5f); // Centrul în coordonate UV (0-1)
        float lakeRadiusUV = settings.lakeRadius / terrainData.size.x;

        for (int y = 0; y < sampleRes; y++)
        {
            for (int x = 0; x < sampleRes; x++)
            {
                // Densitatea: șansă bazată pe density (ex: 30% șansă să testăm acest punct)
                if (prng.Next(0, 100) > miningRockDensity) continue;

                float normX = (float)x / sampleRes;
                float normY = (float)y / sampleRes;

                // Evităm lacul
                float distToCenter = Vector2.Distance(new Vector2(normX, normY), center);
                if (distToCenter < lakeRadiusUV + 0.05f) continue;

                float height = terrainData.GetInterpolatedHeight(normX, normY);
                float normHeight = height / terrainData.size.y;
                float slope = terrainData.GetSteepness(normX, normY);

                // LOGICA RELAXATĂ
                // slope > 10f (să nu fie pe iarbă plată)
                // slope < 85f (aproape orice pantă de munte)
                if (normHeight >= miningMinHeight && normHeight <= miningMaxHeight && slope > 10f)
                {
                    Vector3 worldPos = new Vector3(
                        normX * terrainData.size.x,
                        height,
                        normY * terrainData.size.z
                    ) + terrainPos;

                    // Rotire completă pentru aspect natural
                    Quaternion rot = Quaternion.Euler(
                        (float)prng.NextDouble() * 360f, 
                        (float)prng.NextDouble() * 360f, 
                        (float)prng.NextDouble() * 360f
                    );

                    GameObject rock = Instantiate(miningRockPrefab, worldPos, rot);
                    rock.transform.parent = rockContainer.transform;

                    float s = 0.5f + (float)prng.NextDouble() * 1.0f;
                    rock.transform.localScale = Vector3.one * s;
                }
            }
        }
    }
}