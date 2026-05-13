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
    [Range(0f, 1f)] public float density = 0.3f;
    [Range(0f, 1f)] public float minHeight = 0.35f;
    [Range(0f, 1f)] public float maxHeight = 0.45f;
    public float maxSlope = 20f;
    public Vector2 scaleRange = new Vector2(0.6f, 1.0f);
    public float noiseScale = 40f;
    public float lakeOffset = 5f;
    public int seedOffset = 0;
}


[System.Serializable]
public class UnderTreeSettings
{
    public List<GameObject> prefabs;
    [Range(0, 5)] public int maxItems = 3;
    public float spawnRadius = 1.5f;
}


// ─────────────────────────────────────────────────────────────────────────────
//  GENERIC SPAWN GROUP — adaugă câte vrei din Inspector
// ─────────────────────────────────────────────────────────────────────────────

[System.Serializable]
public class GenericSpawnGroup
{
    [Header("Identificare")]
    public string name = "SpawnGroup";

    [Header("Prefab")]
    public GameObject prefab;

    [Header("Înălțime (normalizat 0-1)")]
    [Range(0f, 1f)] public float minHeight = 0.35f;
    [Range(0f, 1f)] public float maxHeight = 0.60f;

    [Header("Pantă maximă (grade)")]
    public float maxSlope = 25f;

    [Header("Distanță față de lac (UV 0-1)")]
    [Range(0f, 0.5f)] public float lakeOffset = 0.05f;

    [Header("Densitate & Noise")]
    [Range(0f, 1f)] public float density = 0.4f;
    public float noiseScale = 30f;

    [Header("Verificare suprapunere cu obiecte existente")]
    public float overlapRadius = 1.2f;
    public LayerMask overlapMask = ~0;

    [Header("Scale")]
    public Vector2 scaleRange = new Vector2(0.8f, 1.2f);

    [Header("Rotație")]
    public bool randomYaw = true;
    [Range(0f, 30f)] public float maxTiltX = 0f;
    [Range(0f, 30f)] public float maxTiltZ = 0f;

    [Header("Offset Y față de teren")]
    public float yOffset = 0f;

    [Header("Seed offset")]
    public int seedOffset = 0;

    [Header("Rezoluție sampling")]
    [Range(32, 512)] public int sampleResolution = 128;
}


public class MapGenerator : MonoBehaviour
{
    public PerlinSettings settings;

    [Header("Referință Teren")]
    public Terrain terrain;

    [Header("Referință Apă")]
    public Transform waterPlane;
    public float waterOffset = 0.1f;
    public float waterDepth = 10f;

    public bool autoUpdate = true;

    [Header("Texturi Teren")]
    public List<TerrainTextureSettings> textureSettings;

    [Header("Textură Lac (aplicată în gropi)")]
    public TerrainLayer lakeLayer;
    [Range(0f, 1f)] public float lakeBlendStrength = 0.02f;

    [Header("Setări Vegetație")]
    public GameObject treePrefab;
    [Range(0, 100)] public int treeDensity = 50;
    [Range(0, 1)] public float treeMinHeight = 0.35f;
    [Range(0, 1)] public float treeMaxHeight = 0.6f;

    [Header("Setări Sub Copac")]
    public UnderTreeSettings underTreeSettings;

    [Header("GPU Multi Renderer")]
    private GPUMultiRenderer multiRenderer;

    [Header("Setări Vegetație GPU")]
    public List<VegetationSettings> vegetationList;

    [Header("Setări Mining Rocks")]
    public GameObject miningRockPrefab;
    [Range(0, 100)] public int miningRockDensity = 30;
    public float miningMinHeight = 0.42f;
    public float miningMaxHeight = 0.65f;

    [Header("Pietre Mici (Colectabile)")]
    public GameObject smallRockPrefab;
    [Range(0, 100)] public int smallRockDensity = 40;

    [Header("Spawn Grupuri Generice")]
    public List<GenericSpawnGroup> spawnGroups = new List<GenericSpawnGroup>();

    public static event System.Action OnMapGenerated;

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

        SetupTerrainLayers();
        ApplyToTerrain(noiseMap);
        ApplyTexturesToTerrain(noiseMap);

        if (treePrefab != null)
            PlaceTrees();

        GenerateVegetation();
        SpawnMiningRock();
        SpawnSmallRocks();
        SpawnGenericGroups();
        UpdateWaterLevel();

        OnMapGenerated?.Invoke();
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
                        float t = dist / settings.lakeRadius;
                        float hFund = noiseValue * (settings.nivelCampie * settings.lakeDepthMultiplier);
                        float hMargine = settings.nivelCampie + (noiseValue * 0.02f);
                        float smoothT = Mathf.SmoothStep(0, 1, t);
                        noiseValue = Mathf.Lerp(hFund, hMargine, smoothT);
                    }
                    else if (dist < settings.plainRadius)
                    {
                        noiseValue = settings.nivelCampie + (noiseValue * 0.02f);
                    }
                    else
                    {
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
        terrain.Flush();
    }

    // ─────────────────────────────────────────────
    //  TEXTURI
    // ─────────────────────────────────────────────

    private void SetupTerrainLayers()
    {
        if (textureSettings == null || textureSettings.Count == 0) return;

        List<TerrainLayer> layers = new List<TerrainLayer>();
        for (int i = 0; i < textureSettings.Count; i++)
        {
            if (textureSettings[i].layer == null)
            {
                Debug.LogError($"Lipsește TerrainLayer la indexul {i}!");
                return;
            }
            layers.Add(textureSettings[i].layer);
        }

        if (lakeLayer != null)
            layers.Add(lakeLayer);

        terrain.terrainData.terrainLayers = layers.ToArray();

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

        bool hasLakeLayer = lakeLayer != null;
        int totalLayers = hasLakeLayer ? numLayers + 1 : numLayers;
        int lakeLayerIndex = numLayers;

        float[,,] splatmap = new float[alphaH, alphaW, totalLayers];
        float campieHeight = settings.nivelCampie;

        for (int y = 0; y < alphaH; y++)
        {
            for (int x = 0; x < alphaW; x++)
            {
                float normX = (x + 0.5f) / alphaW;
                float normY = (y + 0.5f) / alphaH;

                float currentHeight = terrainData.GetInterpolatedHeight(normX, normY) / terrainData.size.y;
                float[] weights = new float[totalLayers];

                for (int i = 0; i < numLayers; i++)
                {
                    if (currentHeight >= textureSettings[i].startHeight)
                    {
                        for (int j = 0; j < i; j++) weights[j] = 0f;
                        weights[i] = 1f;

                        if (i > 0 && textureSettings[i].blendStrength > 0f)
                        {
                            float blendStart = textureSettings[i].startHeight - textureSettings[i].blendStrength;
                            float blendFactor = Mathf.InverseLerp(blendStart, textureSettings[i].startHeight, currentHeight);
                            weights[i] = blendFactor;
                            weights[i - 1] = 1f - blendFactor;
                        }
                    }
                }

                if (hasLakeLayer && currentHeight < campieHeight - lakeBlendStrength)
                {
                    float depth = Mathf.InverseLerp(
                        campieHeight - lakeBlendStrength,
                        campieHeight - lakeBlendStrength * 5f,
                        currentHeight
                    );
                    float lakeWeight = Mathf.Clamp01(depth);

                    for (int i = 0; i < numLayers; i++)
                        weights[i] *= (1f - lakeWeight);

                    weights[lakeLayerIndex] = lakeWeight;
                }

                for (int i = 0; i < totalLayers; i++)
                    splatmap[y, x, i] = weights[i];
            }
        }

        terrainData.SetAlphamaps(0, 0, splatmap);
    }

    // ─────────────────────────────────────────────
    //  VEGETAȚIE GPU
    // ─────────────────────────────────────────────

    public void GenerateVegetation()
    {
        if (multiRenderer == null) EnsureRendererExists();
        multiRenderer.ClearAll();

        TerrainData terrainData = terrain.terrainData;
        int sampleRes = 256;
        Vector2 center = new Vector2(sampleRes / 2f, sampleRes / 2f);
        float gridLakeRadius = (settings.lakeRadius / terrainData.heightmapResolution) * sampleRes;
        float spacing = terrainData.size.x / sampleRes;

        Dictionary<int, List<Matrix4x4>> transforms = new Dictionary<int, List<Matrix4x4>>();
        for (int i = 0; i < vegetationList.Count; i++)
            transforms[i] = new List<Matrix4x4>();

        System.Random[] prngs = new System.Random[vegetationList.Count];
        for (int i = 0; i < vegetationList.Count; i++)
            prngs[i] = new System.Random(settings.seed + vegetationList[i].seedOffset);

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

                    if (distToCenter < gridLakeRadius + veg.lakeOffset) continue;
                    if (normHeight < veg.minHeight || normHeight > veg.maxHeight) continue;
                    if (slope > veg.maxSlope) continue;

                    float noise = Mathf.PerlinNoise(
                        normX * veg.noiseScale + settings.seed + veg.seedOffset,
                        normY * veg.noiseScale + settings.seed + veg.seedOffset
                    );

                    if (noise < (1f - veg.density)) continue;

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
        int treeIndex = 0;
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
                float distToCenter = Vector2.Distance(new Vector2(xPos, yPos), center);

                if (distToCenter > (settings.lakeRadius + 9f) &&
                    currentHeightNormalized >= treeMinHeight &&
                    currentHeightNormalized <= treeMaxHeight &&
                    slope < 30f)
                {
                    Vector3 worldPos = new Vector3(normX * mapWidth, currentHeight, normY * mapZ)
                        + terrain.transform.position;

                    float randomRotation = (float)prng.NextDouble() * 360f;
                    GameObject tree = Instantiate(treePrefab, worldPos, Quaternion.Euler(0, randomRotation, 0));
                    tree.transform.parent = treeContainer.transform;

                    float scale = 1.2f + (float)prng.NextDouble() * 0.4f;
                    tree.transform.localScale = Vector3.one * scale;

                    WorldEntityState wState = tree.GetComponent<WorldEntityState>();
                    if (wState != null)
                    {
                        wState.isSpawnedAtRuntime = false;
                        wState.GenerateSeedBasedID(settings.seed, "Tree", treeIndex);
                        treeIndex++;
                    }

                    if (underTreeSettings.prefabs.Count > 0)
                        GenerateUnderTreeItems(worldPos, treeContainer.transform);
                }
            }
        }
    }

    private void GenerateUnderTreeItems(Vector3 treePos, Transform parent)
    {
        if (underTreeSettings.prefabs == null || underTreeSettings.prefabs.Count == 0) return;

        int localSeed = (int)(treePos.x * 1000 + treePos.z * 1000);
        System.Random itemPrng = new System.Random(localSeed);
        int itemCount = itemPrng.Next(0, underTreeSettings.maxItems + 1);

        TerrainData terrainData = terrain.terrainData;
        Vector3 terrainPos = terrain.transform.position;

        for (int i = 0; i < itemCount; i++)
        {
            float angle = (float)itemPrng.NextDouble() * Mathf.PI * 2f;
            float distance = (float)itemPrng.NextDouble() * underTreeSettings.spawnRadius;

            Vector3 offset = new Vector3(Mathf.Cos(angle) * distance, 0, Mathf.Sin(angle) * distance);
            Vector3 spawnPos = treePos + offset;

            float normX = Mathf.Clamp01((spawnPos.x - terrainPos.x) / terrainData.size.x);
            float normZ = Mathf.Clamp01((spawnPos.z - terrainPos.z) / terrainData.size.z);

            Vector3 terrainNormal = terrainData.GetInterpolatedNormal(normX, normZ);
            float flatness = Vector3.Dot(terrainNormal, Vector3.up);
            if (flatness < 0.92f) continue;

            spawnPos.y = terrainData.GetInterpolatedHeight(normX, normZ) + terrainPos.y + 0.05f;

            float randomYaw = (float)itemPrng.NextDouble() * 360f;
            Quaternion finalRotation = Quaternion.Euler(0, randomYaw, 0);

            int prefabIndex = itemPrng.Next(0, underTreeSettings.prefabs.Count);
            GameObject item = Instantiate(underTreeSettings.prefabs[prefabIndex], spawnPos, finalRotation);
            item.transform.parent = parent;
            item.transform.localScale = Vector3.one * (0.8f + (float)itemPrng.NextDouble() * 0.4f);

            WorldEntityState wState = item.GetComponent<WorldEntityState>();
            if (wState == null) wState = item.AddComponent<WorldEntityState>();

            wState.isSpawnedAtRuntime = false;

            string cleanName = item.name.Replace("(Clone)", "").Trim();
            wState.uniqueID = $"{settings.seed}_{cleanName}_{spawnPos.x:F1}_{spawnPos.z:F1}";
        }
    }

    // ─────────────────────────────────────────────
    //  APĂ
    // ─────────────────────────────────────────────

    private void UpdateWaterLevel()
    {
        if (waterPlane == null) return;

        float waterSurfaceY = (settings.nivelCampie * settings.terrainHeightMultiplier) - waterOffset;

        Vector3 pos = waterPlane.position;
        pos.y = waterSurfaceY + terrain.transform.position.y;
        pos.x = terrain.transform.position.x + (settings.width / 2f);
        pos.z = terrain.transform.position.z + (settings.height / 2f);
        waterPlane.position = pos;

        float scaleX = settings.width / 10f;
        float scaleZ = settings.height / 10f;
        waterPlane.localScale = new Vector3(scaleX, 1f, scaleZ);
    }

    // ─────────────────────────────────────────────
    //  MINING ROCKS
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

        int sampleRes = 128;
        Vector2 center = new Vector2(0.5f, 0.5f);
        float lakeRadiusUV = settings.lakeRadius / terrainData.size.x;
        int rockIndex = 0;

        for (int y = 0; y < sampleRes; y++)
        {
            for (int x = 0; x < sampleRes; x++)
            {
                if (prng.Next(0, 100) > miningRockDensity) continue;

                float normX = (float)x / sampleRes;
                float normY = (float)y / sampleRes;

                float distToCenter = Vector2.Distance(new Vector2(normX, normY), center);
                if (distToCenter < lakeRadiusUV + 0.05f) continue;

                float height = terrainData.GetInterpolatedHeight(normX, normY);
                float normHeight = height / terrainData.size.y;
                float slope = terrainData.GetSteepness(normX, normY);

                if (normHeight >= miningMinHeight && normHeight <= miningMaxHeight && slope > 10f)
                {
                    Vector3 worldPos = new Vector3(
                        normX * terrainData.size.x, height, normY * terrainData.size.z
                    ) + terrainPos;

                    Quaternion rot = Quaternion.Euler(
                        (float)prng.NextDouble() * 360f,
                        (float)prng.NextDouble() * 360f,
                        (float)prng.NextDouble() * 360f
                    );

                    GameObject rock = Instantiate(miningRockPrefab, worldPos, rot);
                    rock.transform.parent = rockContainer.transform;
                    rock.transform.localScale = Vector3.one * (0.5f + (float)prng.NextDouble() * 1.0f);

                    WorldEntityState wState = rock.GetComponent<WorldEntityState>();
                    if (wState != null)
                    {
                        wState.isSpawnedAtRuntime = false;
                        wState.GenerateSeedBasedID(settings.seed, "MiningRock", rockIndex);
                        rockIndex++;
                    }
                }
            }
        }
    }

    // ─────────────────────────────────────────────
    //  PIETRE MICI
    // ─────────────────────────────────────────────

    public void SpawnSmallRocks()
    {
        if (smallRockPrefab == null) return;

        string containerName = "Generated_SmallRocks";
        Transform oldContainer = transform.Find(containerName);
        if (oldContainer != null) DestroyImmediate(oldContainer.gameObject);

        GameObject rockContainer = new GameObject(containerName);
        rockContainer.transform.parent = this.transform;

        System.Random prng = new System.Random(settings.seed + 555);
        TerrainData terrainData = terrain.terrainData;
        Vector3 terrainPos = terrain.transform.position;

        int sampleRes = 128;
        Vector2 center = new Vector2(0.5f, 0.5f);
        float lakeRadiusUV = settings.lakeRadius / terrainData.size.x;

        float minHeight = settings.nivelCampie - 0.02f;
        float maxHeight = settings.nivelCampie + 0.08f;
        int rockIndex = 0;

        for (int y = 0; y < sampleRes; y++)
        {
            for (int x = 0; x < sampleRes; x++)
            {
                if (prng.Next(0, 100) > smallRockDensity) continue;

                float normX = (float)x / sampleRes;
                float normY = (float)y / sampleRes;

                float distToCenter = Vector2.Distance(new Vector2(normX, normY), center);
                if (distToCenter < lakeRadiusUV + 0.06f) continue;

                float height = terrainData.GetInterpolatedHeight(normX, normY);
                float normHeight = height / terrainData.size.y;
                float slope = terrainData.GetSteepness(normX, normY);

                if (normHeight >= minHeight && normHeight <= maxHeight && slope < 25f)
                {
                    float offsetX = ((float)prng.NextDouble() - 0.5f) * (terrainData.size.x / sampleRes);
                    float offsetZ = ((float)prng.NextDouble() - 0.5f) * (terrainData.size.z / sampleRes);

                    Vector3 worldPos = new Vector3(
                        normX * terrainData.size.x + offsetX,
                        height,
                        normY * terrainData.size.z + offsetZ
                    ) + terrainPos;

                    worldPos.y = terrain.SampleHeight(worldPos) + terrainPos.y;

                    Quaternion rot = Quaternion.Euler(
                        (float)prng.NextDouble() * 15f,
                        (float)prng.NextDouble() * 360f,
                        (float)prng.NextDouble() * 15f
                    );

                    GameObject rock = Instantiate(smallRockPrefab, worldPos, rot);
                    rock.transform.parent = rockContainer.transform;

                    WorldEntityState wState = rock.GetComponent<WorldEntityState>();
                    if (wState != null)
                    {
                        wState.isSpawnedAtRuntime = false;
                        wState.GenerateSeedBasedID(settings.seed, "Rock", rockIndex);
                        rockIndex++;
                    }
                }
            }
        }

        Debug.Log($"[SmallRocks] Pietre mici generate în scenă.");
    }

    // ─────────────────────────────────────────────
    //  SPAWN GRUPURI GENERICE
    // ─────────────────────────────────────────────

    public void SpawnGenericGroups()
    {
        foreach (var group in spawnGroups)
        {
            if (group.prefab == null)
            {
                Debug.LogWarning($"[SpawnGroups] Grupul '{group.name}' nu are prefab setat.");
                continue;
            }
            SpawnGroup(group);
        }
    }

    private void SpawnGroup(GenericSpawnGroup group)
    {
        string containerName = $"Generated_{group.name}";
        Transform old = transform.Find(containerName);
        if (old != null) DestroyImmediate(old.gameObject);

        GameObject container = new GameObject(containerName);
        container.transform.parent = this.transform;

        TerrainData td = terrain.terrainData;
        Vector3 terrainPos = terrain.transform.position;
        int res = group.sampleResolution;
        int seed = settings.seed + group.seedOffset;
        System.Random prng = new System.Random(seed);

        float lakeRadiusUV = settings.lakeRadius / td.heightmapResolution;
        Vector2 centerUV = new Vector2(0.5f, 0.5f);

        int failLake = 0, failHeight = 0, failSlope = 0, failNoise = 0, failOverlap = 0, spawnIndex = 0;

        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                float normX = (x + 0.5f) / res;
                float normY = (y + 0.5f) / res;

                // 1. Filtru lac
                float distToCenter = Vector2.Distance(new Vector2(normX, normY), centerUV);
                if (distToCenter < lakeRadiusUV + group.lakeOffset)
                {
                    failLake++;
                    continue;
                }

                // 2. Filtru pantă (pe celula originală, înainte de offset)
                float slope = td.GetSteepness(normX, normY);
                if (slope > group.maxSlope)
                {
                    failSlope++;
                    continue;
                }

                // 3. Filtru densitate (Perlin Noise)
                float noise = Mathf.PerlinNoise(
                    normX * group.noiseScale + seed * 0.01f,
                    normY * group.noiseScale + seed * 0.01f
                );
                if (noise < (1f - group.density))
                {
                    failNoise++;
                    continue;
                }

                // Calculează poziția finală cu offset
                float cellSizeX = td.size.x / res;
                float cellSizeZ = td.size.z / res;
                float offsetX = ((float)prng.NextDouble() - 0.5f) * cellSizeX;
                float offsetZ = ((float)prng.NextDouble() - 0.5f) * cellSizeZ;

                Vector3 worldPos = new Vector3(
                    terrainPos.x + normX * td.size.x + offsetX,
                    0f,
                    terrainPos.z + normY * td.size.z + offsetZ
                );

                // Clamp strict în bounds teren
                worldPos.x = Mathf.Clamp(worldPos.x, terrainPos.x + 0.5f, terrainPos.x + td.size.x - 0.5f);
                worldPos.z = Mathf.Clamp(worldPos.z, terrainPos.z + 0.5f, terrainPos.z + td.size.z - 0.5f);

                // Re-sample height și pantă la poziția FINALĂ cu offset
                float fnX = Mathf.Clamp01((worldPos.x - terrainPos.x) / td.size.x);
                float fnZ = Mathf.Clamp01((worldPos.z - terrainPos.z) / td.size.z);
                float sampledHeight = td.GetInterpolatedHeight(fnX, fnZ);
                float normHeight = sampledHeight / td.size.y;

                // ✅ Filtru înălțime pe poziția FINALĂ, nu pe celula originală
                if (normHeight < group.minHeight || normHeight > group.maxHeight)
                {
                    failHeight++;
                    continue;
                }

                worldPos.y = sampledHeight + terrainPos.y + group.yOffset;

                // 4. Filtru overlap
                if (group.overlapRadius > 0f)
                {
                    if (Physics.CheckSphere(worldPos, group.overlapRadius, group.overlapMask))
                    {
                        failOverlap++;
                        continue;
                    }
                }

                // Spawn
                float yaw   = group.randomYaw ? (float)prng.NextDouble() * 360f : 0f;
                float tiltX = group.maxTiltX > 0 ? ((float)prng.NextDouble() - 0.5f) * 2f * group.maxTiltX : 0f;
                float tiltZ = group.maxTiltZ > 0 ? ((float)prng.NextDouble() - 0.5f) * 2f * group.maxTiltZ : 0f;
                Quaternion rot = Quaternion.Euler(tiltX, yaw, tiltZ);

                float scale = Mathf.Lerp(group.scaleRange.x, group.scaleRange.y, (float)prng.NextDouble());

                GameObject spawned = Instantiate(group.prefab, worldPos, rot, container.transform);
                spawned.transform.localScale = Vector3.one * scale;

                WorldEntityState wState = spawned.GetComponent<WorldEntityState>();
                if (wState == null) wState = spawned.AddComponent<WorldEntityState>();
                wState.isSpawnedAtRuntime = false;
                wState.GenerateSeedBasedID(seed, group.name, spawnIndex);
                spawnIndex++;
            }
        }

        Debug.Log($"[{group.name}] ✅ {spawnIndex} spawned | ❌ lac:{failLake} h:{failHeight} pantă:{failSlope} noise:{failNoise} overlap:{failOverlap}");
    }

   

    public int GetCurrentSeed() => settings != null ? settings.seed : 0;
}