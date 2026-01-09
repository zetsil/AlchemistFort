using UnityEngine;

public class SpawnPrefabAhead : MonoBehaviour
{
    [Header("Referințe")]
    [Tooltip("Trage aici NPC-ul care deține acest script (Root-ul).")]
    public NPCBase ownerNPC;

    [Header("Settings")]
    public GameObject prefabToSpawn;
    public float distanceAhead = 4.0f;
    public float heightOffset = 2f;
    public bool makeChildOfThis = false;

    private void OnEnable()
    {
        SpawnObject();
    }

    public void SpawnObject()
    {
        if (prefabToSpawn == null)
        {
            Debug.LogWarning($"Nu ai setat niciun prefab pe {gameObject.name}!");
            return;
        }

        // 1. Calculăm poziția de spawn
        Vector3 spawnPosition = transform.position + (transform.forward * distanceAhead) + (Vector3.up * heightOffset);

        // 2. Instanțiem obiectul
        GameObject spawnedObj = Instantiate(prefabToSpawn, spawnPosition, transform.rotation);

        // 3. LOGICA NOUĂ: Setăm owner-ul pe proiectil dacă acesta are scriptul LinearProjectile
        if (ownerNPC != null)
        {
            LinearProjectile projectileScript = spawnedObj.GetComponent<LinearProjectile>();
            if (projectileScript != null)
            {
                projectileScript.ownerNPC = ownerNPC;
                Debug.Log($"[Spawn] Am setat owner-ul {ownerNPC.name} pe proiectilul {spawnedObj.name}");
            }
        }
        else
        {
            Debug.LogWarning($"[Spawn] Obiectul a fost spawnat, dar ownerNPC este NULL pe {gameObject.name}!");
        }

        // 4. Dacă vrei ca obiectul să rămână lipit de inamic
        if (makeChildOfThis)
        {
            spawnedObj.transform.SetParent(this.transform);
        }
    }
}