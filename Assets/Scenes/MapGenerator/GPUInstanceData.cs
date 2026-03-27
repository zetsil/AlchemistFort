using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Rendering;

public class GPUMultiRenderer : MonoBehaviour
{
    
    private List<GPUInstanceData> allRenderData = new List<GPUInstanceData>();

    public void ClearAll() => allRenderData.Clear();

    public void AddRenderData(GPUInstanceData data)
    {
        if (data.batches.Count > 0)
            allRenderData.Add(data);
    }

    void Update()
    {
        for (int i = 0; i < allRenderData.Count; i++)
        {
            var data = allRenderData[i];
            for (int j = 0; j < data.batches.Count; j++)
            {
                Graphics.DrawMeshInstanced(data.mesh, 0, data.material, data.batches[j], data.batches[j].Length);
            }
        }
    }
}