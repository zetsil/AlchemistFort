using UnityEngine;
using System.Collections.Generic;

public class GPUMultiRenderer : MonoBehaviour
{
    [Header("Culling")]
    public Transform player;
    public float renderDistance = 80f;

    private List<GPUInstanceData> allRenderData = new List<GPUInstanceData>();

    public void ClearAll() => allRenderData.Clear();

    public void AddRenderData(GPUInstanceData data)
    {
        if (data.batches.Count > 0)
            allRenderData.Add(data);
    }

    void Update()
    {
        if (player == null)
        {
            GameObject foundPlayer = GameObject.FindGameObjectWithTag("Player");
            if (foundPlayer != null)
            {
                player = foundPlayer.transform;
            }
            else
            {
                // Dacă tot nu îl găsim, oprim Update-ul ca să nu dea erori
                if (Time.frameCount % 120 == 0) 
                    Debug.LogWarning($"[GPUMultiRenderer] Jucătorul lipsește pe {gameObject.name} și nu a fost găsit niciun obiect cu tag-ul 'Player'!");
                return;
            }
        }

        if (player == null || allRenderData.Count == 0)
        {
            Debug.Log($"allRenderData.Count: {allRenderData.Count} | player: {player}");
            return;
        }
       
        Vector3 playerPos = player.position;
        float distSq = renderDistance * renderDistance;
        int rendered = 0;
        
        for (int i = 0; i < allRenderData.Count; i++)
        {
            var data = allRenderData[i];

            for (int j = 0; j < data.batches.Count; j++)
            {
                Bounds currentBounds = data.batchBounds[j];
                float sqrMag = currentBounds.SqrDistance(playerPos);

                if (sqrMag > distSq) continue;

                Graphics.DrawMeshInstanced(data.mesh, 0, data.material,
                    data.batches[j], data.batches[j].Length);
                rendered++;
            }
        }
        
    }
}