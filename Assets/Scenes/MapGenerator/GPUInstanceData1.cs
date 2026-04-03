using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class GPUInstanceData
{
    public string name;
    public Mesh mesh;
    public Material material;

    [System.NonSerialized] 
    public List<Matrix4x4[]> batches = new List<Matrix4x4[]>();
    
    // NOU: Salvăm direct Bounding Box-ul real pentru fiecare batch
    [System.NonSerialized]
    public List<Bounds> batchBounds = new List<Bounds>();

    public void Initialize(List<Matrix4x4> transforms, float chunkSize = 32f)
    {
        Debug.Log($"[GPUInstanceData] Initialize apelat cu {transforms.Count} transforms");
        
        batches = new List<Matrix4x4[]>();
        batchBounds = new List<Bounds>();

        Dictionary<Vector2Int, List<Matrix4x4>> chunks = new Dictionary<Vector2Int, List<Matrix4x4>>();
        
        foreach (var matrix in transforms)
        {
            Vector3 pos = (Vector3)matrix.GetColumn(3);
            Vector2Int key = new Vector2Int(
                Mathf.FloorToInt(pos.x / chunkSize),
                Mathf.FloorToInt(pos.z / chunkSize)
            );
            if (!chunks.ContainsKey(key)) chunks[key] = new List<Matrix4x4>();
            chunks[key].Add(matrix);
        }

        Debug.Log($"[GPUInstanceData] Chunks create: {chunks.Count}");

        foreach (var chunk in chunks)
        {
            var list = chunk.Value;
            Vector3 chunkCenter = new Vector3(
                (chunk.Key.x + 0.5f) * chunkSize,
                0,
                (chunk.Key.y + 0.5f) * chunkSize
            );
            Bounds chunkBounds = new Bounds(chunkCenter, Vector3.one * chunkSize);

            for (int i = 0; i < list.Count; i += 1023)
            {
                int count = Mathf.Min(1023, list.Count - i);
                Matrix4x4[] batch = new Matrix4x4[count];
                list.CopyTo(i, batch, 0, count);
                batches.Add(batch);
                batchBounds.Add(chunkBounds);
            }
        }
        
        Debug.Log($"[GPUInstanceData] Batches finale: {batches.Count}");
    }
}