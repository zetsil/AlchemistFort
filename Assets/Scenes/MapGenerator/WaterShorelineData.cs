using UnityEngine;

/// <summary>
/// Atașează pe același GameObject ca MeshRenderer-ul apei.
/// Preia heightmap-ul din Terrain sau din PerlinSettings și îl pasează
/// la materialul de apă ca textură — precizie constantă la orice distanță.
/// </summary>
[RequireComponent(typeof(MeshRenderer))]
public class WaterShorelineData : MonoBehaviour
{
    [Header("Referințe")]
    [Tooltip("Terrain-ul Unity din scenă (dacă există).")]
    public Terrain terrain;

    [Tooltip("Dacă nu ai Terrain, referențiază scriptul care generează mesh-ul.")]
    public MapGenerator mapGenerator;

    [Header("Setări Textură")]
    [Tooltip("Rezoluția texturii de înălțime. 512 e suficient pentru cele mai multe hărți.")]
    public int heightmapResolution = 512;

    [Tooltip("Y-ul suprafeței apei în world space (același cu plane-ul de apă).")]
    public float waterLevel = 0f;

    // Intern
    private Texture2D _heightTex;
    private Material  _waterMat;
    private static readonly int HeightmapID    = Shader.PropertyToID("_TerrainHeightmap");
    private static readonly int HeightmapScaleID = Shader.PropertyToID("_TerrainHeightmapScale");
    private static readonly int TerrainOriginID  = Shader.PropertyToID("_TerrainOrigin");
    private static readonly int WaterLevelID     = Shader.PropertyToID("_WaterLevel");

    void Start()
    {
        _waterMat = GetComponent<MeshRenderer>().material;
        BakeAndUpload();
    }

    /// <summary>Apelează din editor sau după regenerarea terenului.</summary>
    public void BakeAndUpload()
    {
        if (_waterMat == null)
            _waterMat = GetComponent<MeshRenderer>().material;

        if (terrain != null)
            BakeFromTerrain();
        else if (mapGenerator != null)
            BakeFromMapGenerator();
        else
            Debug.LogWarning("WaterShorelineData: nu ai setat nici Terrain nici MapGenerator.");
    }

    // ── Caz 1: Terrain Unity standard ────────────────────────────────────
    void BakeFromTerrain()
    {
        TerrainData td  = terrain.terrainData;
        int res         = heightmapResolution;

        _heightTex = new Texture2D(res, res, TextureFormat.RFloat, false);
        _heightTex.wrapMode = TextureWrapMode.Clamp;
        _heightTex.filterMode = FilterMode.Bilinear;

        Color[] pixels = new Color[res * res];
        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                float h = td.GetInterpolatedHeight(
                    (float)x / (res - 1),
                    (float)y / (res - 1));
                // Normalizăm la [0,1] față de înălțimea maximă a terenului
                pixels[y * res + x] = new Color(h / td.size.y, 0, 0, 1);
            }
        }

        _heightTex.SetPixels(pixels);
        _heightTex.Apply();

        // Pasăm la shader: originea și dimensiunea terenului în world space
        Vector3 origin = terrain.transform.position;
        _waterMat.SetTexture(HeightmapID, _heightTex);
        // xy = dimensiunea în world space (X și Z), z = înălțimea maximă
        _waterMat.SetVector(HeightmapScaleID,
            new Vector4(td.size.x, td.size.z, td.size.y, 0f));
        _waterMat.SetVector(TerrainOriginID,
            new Vector4(origin.x, origin.y, origin.z, 0f));
        _waterMat.SetFloat(WaterLevelID, waterLevel);
    }

    // ── Caz 2: Mesh procedural din MapGenerator ───────────────────────────
    void BakeFromMapGenerator()
    {
        // if (mapGenerator == null) return;

        // float[,] heightData = mapGenerator.GetHeightmap();
        // if (heightData == null)
        // {
        //     Debug.LogWarning("WaterShorelineData: GetHeightmap() a returnat null.");
        //     return;
        // }

        // int dataW = heightData.GetLength(0);
        // int dataH = heightData.GetLength(1);
        // int res   = heightmapResolution;

        // _heightTex = new Texture2D(res, res, TextureFormat.RFloat, false);
        // _heightTex.wrapMode  = TextureWrapMode.Clamp;
        // _heightTex.filterMode = FilterMode.Bilinear;

        // Color[] pixels = new Color[res * res];
        // for (int y = 0; y < res; y++)
        // {
        //     for (int x = 0; x < res; x++)
        //     {
        //         int sx = Mathf.Clamp(Mathf.RoundToInt((float)x / (res-1) * (dataW-1)), 0, dataW-1);
        //         int sy = Mathf.Clamp(Mathf.RoundToInt((float)y / (res-1) * (dataH-1)), 0, dataH-1);
        //         pixels[y * res + x] = new Color(heightData[sx, sy], 0, 0, 1);
        //     }
        // }

        // _heightTex.SetPixels(pixels);
        // _heightTex.Apply();

        // // Presupunem că mesh-ul e centrat în origine — ajustează după nevoie
        // Bounds bounds = GetComponent<MeshFilter>() != null
        //     ? GetComponent<MeshFilter>().sharedMesh.bounds
        //     : new Bounds(Vector3.zero, new Vector3(256, 0, 256));

        // Vector3 worldSize = new Vector3(
        //     bounds.size.x * transform.localScale.x,
        //     mapGenerator.terrainHeightMultiplier,
        //     bounds.size.z * transform.localScale.z);

        // Vector3 origin = transform.position - new Vector3(worldSize.x * 0.5f, 0, worldSize.z * 0.5f);

        // _waterMat.SetTexture(HeightmapID, _heightTex);
        // _waterMat.SetVector(HeightmapScaleID,
        //     new Vector4(worldSize.x, worldSize.z, worldSize.y, 0f));
        // _waterMat.SetVector(TerrainOriginID,
        //     new Vector4(origin.x, origin.y, origin.z, 0f));
        // _waterMat.SetFloat(WaterLevelID, waterLevel);
    }

    void OnDestroy()
    {
        if (_heightTex != null)
            Destroy(_heightTex);
    }

#if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(WaterShorelineData))]
    public class Editor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            if (GUILayout.Button("🔄 Re-Bake Heightmap"))
                ((WaterShorelineData)target).BakeAndUpload();
        }
    }
#endif
}