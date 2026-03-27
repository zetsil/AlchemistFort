using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class GPUInstanceData
{
    public string name;
    public Mesh mesh;
    public Material material;

    // Marcăm ca NonSerialized pentru a evita eroarea NativeClass la salvarea scenei
    [System.NonSerialized] 
    public List<Matrix4x4[]> batches = new List<Matrix4x4[]>();

    public GPUInstanceData() { }

    public void Initialize(List<Matrix4x4> transforms)
    {
        if (batches == null) batches = new List<Matrix4x4[]>();
        batches.Clear();

        // Limitarea de 1023 este impusă de GPU Instancing în Unity
        for (int i = 0; i < transforms.Count; i += 1023)
        {
            int count = Mathf.Min(1023, transforms.Count - i);
            Matrix4x4[] batch = new Matrix4x4[count];
            transforms.CopyTo(i, batch, 0, count);
            batches.Add(batch);
        }
    }
}