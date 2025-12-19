using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Gestionează instanțierea unui Prefab ales dintr-o mapă centralizată (ItemVisualManager),
/// plasându-l în fața inițiatorului.
/// </summary>
public class PrefabSpawner : MonoBehaviour
{
    // NU mai avem nevoie de lista sau dictionary-ul local, deoarece folosim ItemVisualManager.

    [Header("Setări Generare")]
    [Tooltip("Distanța față de inițiator (acest obiect) la care va fi plasat noul obiect.")]
    public float spawnDistance = 2f;


    public GameObject SpawnInFrontOfInitiator(Item itemKey)
    {
        // 1. Verificări inițiale
        if (itemKey == null)
        {
            Debug.LogError($"[PrefabSpawner pe {gameObject.name}] Cheia Item-ului este NULL. Nu se poate genera Prefab.");
            return null;
        }

        if (ItemVisualManager.Instance == null)
        {
            Debug.LogError($"[PrefabSpawner] Lipsește ItemVisualManager din scenă!");
            return null;
        }

        // 2. Obține Prefab-ul din Managerul Centralizat
        // (Folosim funcția GetVisualPrefab din ItemVisualManager.cs)
        GameObject prefabToSpawn = ItemVisualManager.Instance.GetItemVisualPrefab(itemKey);

        if (prefabToSpawn == null)
        {
            // Mesajul de eroare specific este dat deja de ItemVisualManager.
            Debug.LogError($"[PrefabSpawner pe {gameObject.name}] Prefab-ul vizual nu a putut fi obținut pentru item-ul: '{itemKey.itemName}'.");
            return null;
        }

        // 3. Calculează poziția de generare
        // Vectorul forward este direcția în care privește obiectul (inițiatorul)
        Vector3 spawnPosition = transform.position + transform.forward * spawnDistance;
        spawnPosition.y = transform.position.y; // Păstrează aceeași înălțime ca inițiatorul


        // 4. Instanțiază Prefab-ul
        // Folosim rotația inițiatorului.
        GameObject newObject = Instantiate(prefabToSpawn, spawnPosition, transform.rotation);

        Debug.Log($"[PrefabSpawner] Obiectul '{newObject.name}' generat după Item-ul '{itemKey.itemName}'!");

        return newObject;
    }
    

    public GameObject SpawnObject(GameObject prefab)
    {
        if (prefab == null) return null;

        // Exemplu de calcul poziție în fața inițiatorului
        Vector3 spawnPos = transform.position + transform.forward * spawnDistance;
        
        GameObject spawned = Instantiate(prefab, spawnPos, Quaternion.identity);
        return spawned;
    }
}