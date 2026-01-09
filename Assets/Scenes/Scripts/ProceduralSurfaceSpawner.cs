#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class ProceduralSurfaceSpawner : MonoBehaviour
{
    [Header("Setări Spawn")]
    public GameObject[] prefabsToSpawn;
    public int spawnCount = 10;
    public float spawnRadius = 20f;
    public LayerMask groundLayer;

    [Header("Setări Aliniere")]
    public bool alignToSurfaceNormal = true;
    public float minScale = 0.8f;
    public float maxScale = 1.2f;

    // Am eliminat Start() pentru a preveni rularea accidentală în timpul jocului

    [ContextMenu("Spawn Objects")]
    public void SpawnObjects()
    {
        if (prefabsToSpawn == null || prefabsToSpawn.Length == 0)
        {
            Debug.LogWarning("[Spawner] Te rog să adaugi cel puțin un Prefab în listă!");
            return;
        }

        int succesfulSpawns = 0;

        for (int i = 0; i < spawnCount; i++)
        {
            Vector2 randomPoint = Random.insideUnitCircle * spawnRadius;
            Vector3 spawnOrigin = transform.position + new Vector3(randomPoint.x, 50f, randomPoint.y);

            if (Physics.Raycast(spawnOrigin, Vector3.down, out RaycastHit hit, 100f, groundLayer))
            {
                GameObject prefab = prefabsToSpawn[Random.Range(0, prefabsToSpawn.Length)];
                if (prefab == null) continue;

                GameObject instance;

                // Dacă suntem în Editor, folosim PrefabUtility pentru a păstra legătura cu sursa
                if (Application.isPlaying)
                {
                    instance = Instantiate(prefab, hit.point, Quaternion.identity);
                }
                else
                {
                    instance = (GameObject)
                    PrefabUtility.InstantiatePrefab(prefab);
                    instance.transform.position = hit.point;
                    instance.transform.rotation = Quaternion.identity;
                    
                    // Permite Ctrl+Z pentru fiecare obiect creat
                    Undo.RegisterCreatedObjectUndo(instance, "Spawn Procedural Object");
                }

                if (alignToSurfaceNormal)
                {
                    instance.transform.up = hit.normal;
                }

                instance.transform.Rotate(Vector3.up, Random.Range(0f, 360f), Space.Self);
                float scale = Random.Range(minScale, maxScale);
                instance.transform.localScale = Vector3.one * scale;
                
                instance.transform.SetParent(this.transform);
                succesfulSpawns++;
            }
        }
        
        Debug.Log($"[Spawner] Generare finalizată. Obiecte create: {succesfulSpawns}");
    }

    [ContextMenu("Clear Objects")]
    public void ClearExistingObjects()
    {
        // În Edit Mode, distrugerea trebuie să fie imediată și să suporte Undo
        while (transform.childCount > 0)
        {
            Undo.DestroyObjectImmediate(transform.GetChild(0).gameObject);
        }
        Debug.Log("[Spawner] Toate obiectele au fost șterse.");
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0, 1, 1, 0.3f);
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
        
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position + Vector3.up * 50f, transform.position + Vector3.down * 10f);
    }
}

#endif