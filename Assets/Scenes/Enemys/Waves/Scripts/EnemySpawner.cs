using UnityEngine;
using System.Collections.Generic;


public class EnemySpawner : MonoBehaviour
{

    public static EnemySpawner Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
        }
    }


    public GameObject SpawnEnemy(EntityData entityData, Vector3 position)
    {
        if (entityData == null)
        {
            Debug.LogError("SpawnEnemy eșuat: EntityData este null.");
            return null;
        }

        // 1. Obținem Prefab-ul din dicționarul ItemVisualManager
        GameObject enemyPrefab = ItemVisualManager.Instance.GetEntityVisualPrefab(entityData);

        if (enemyPrefab == null)
        {
            // ItemVisualManager a logat deja un warning, dar dăm și un log final aici.
            Debug.LogError($"SpawnEnemy eșuat: Nu s-a putut găsi Prefab-ul vizual pentru Entity '{entityData.name}'. Ați adăugat maparea în ItemVisualManager?");
            return null;
        }

        // 2. Instanțiem Prefab-ul
        GameObject spawnedEnemy = Instantiate(enemyPrefab, position, Quaternion.identity);

        // 3. (OPȚIONAL) Configurăm componenta de sănătate/date a inamicului după spawn
        // Dacă aveți o componentă 'EnemyComponent' care are nevoie de datele SO:
        /*
        EnemyComponent enemyComp = spawnedEnemy.GetComponent<EnemyComponent>();
        if (enemyComp != null)
        {
            enemyComp.Initialize(entityData);
        }
        */

        Debug.Log($"Inamic spawnat: '{entityData.name}' la poziția {position}.");
        return spawnedEnemy;
    }
}