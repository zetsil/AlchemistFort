using UnityEngine;
using System.Collections.Generic;

// ==============================================================================
// STRUCTURI DE MAPARE
// ==============================================================================

// Mapare Item de Inventar/Lume -> Prefab (folosește clasa Item existentă)
[System.Serializable]
public struct ItemVisualMapping
{
    public Item itemData; 
    public GameObject visualPrefab;
}

// Mapare Vizualizare Persoana Intai -> Prefab
// **ATENTIE:** Presupunem ca exista clasa 'FirstPersonVisualSO'
[System.Serializable]
public struct FirstPersonVisualMapping
{
    public Item visualData; 
    public GameObject visualPrefab;
}

// Mapare Entitate/Inamic -> Prefab (folosește clasa EntityData furnizată)
[System.Serializable]
public struct EntityVisualMapping
{
    public EntityData entityData; 
    public GameObject visualPrefab;
}


// ==============================================================================
// CLASA PRINCIPALĂ: ITEM VISUAL MANAGER
// ==============================================================================

public class ItemVisualManager : MonoBehaviour
{
    public static ItemVisualManager Instance { get; private set; }

    [Header("1. Mapări Item de Inventar/Lume")]
    [Tooltip("Asociază fiecare Item ScriptableObject cu modelul său 3D corespunzător.")]
    [SerializeField]
    private List<ItemVisualMapping> itemVisuals = new List<ItemVisualMapping>();

    [Header("2. Mapări Vizual Persoana Întâi")]
    [Tooltip("Asociază ScriptableObject-ul de vizualizare FP cu Prefab-ul său.")]
    [SerializeField]
    private List<FirstPersonVisualMapping> firstPersonVisuals = new List<FirstPersonVisualMapping>();

    [Header("3. Mapări Entități/Inamici")]
    [Tooltip("Asociază fiecare EntityData ScriptableObject cu Prefab-ul său de inamic/NPC.")]
    [SerializeField]
    private List<EntityVisualMapping> entityVisuals = new List<EntityVisualMapping>();


    // --- Dicționarele de Mapare ---
    private Dictionary<Item, GameObject> prefabMap_Items = new Dictionary<Item, GameObject>();
    private Dictionary<Item, GameObject> prefabMap_FirstPersonVisuals = new Dictionary<Item, GameObject>();
    private Dictionary<EntityData, GameObject> prefabMap_Entities = new Dictionary<EntityData, GameObject>();


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
            PopulateDictionaries();
        }
    }

    private void PopulateDictionaries()
    {
        // 1. Mapare Item de Inventar/Lume
        prefabMap_Items.Clear();
        foreach (var map in itemVisuals)
        {
            if (map.itemData != null && map.visualPrefab != null)
            {
                prefabMap_Items.TryAdd(map.itemData, map.visualPrefab);
            }
        }

        // 2. Mapare Vizual Persoana Întâi
        prefabMap_FirstPersonVisuals.Clear();
        foreach (var map in firstPersonVisuals)
        {
            if (map.visualData != null && map.visualPrefab != null)
            {
                prefabMap_FirstPersonVisuals.TryAdd(map.visualData, map.visualPrefab);
            }
        }

        // 3. Mapare Entități/Inamici
        prefabMap_Entities.Clear();
        foreach (var map in entityVisuals)
        {
            if (map.entityData != null && map.visualPrefab != null)
            {
                prefabMap_Entities.TryAdd(map.entityData, map.visualPrefab);
            }
        }
    }


    // ==============================================================================
    // METODE PUBLICE DE ACCES
    // ==============================================================================

    /// <summary>
    /// Returnează Prefab-ul vizual pentru un Item (Lume/Inventar).
    /// </summary>
    public GameObject GetItemVisualPrefab(Item item)
    {
        return GetPrefab(item, prefabMap_Items, nameof(Item));
    }

    /// <summary>
    /// Returnează Prefab-ul vizual pentru un obiect ținut în mână la Persoana Întâi.
    /// </summary>
    public GameObject GetFirstPersonVisualPrefab(Item visualData)
    {
        return GetPrefab(visualData, prefabMap_FirstPersonVisuals, nameof(Item));
    }

    /// <summary>
    /// Returnează Prefab-ul vizual pentru o Entitate (Inamic/NPC).
    /// </summary>
    public GameObject GetEntityVisualPrefab(EntityData entityData)
    {
        return GetPrefab(entityData, prefabMap_Entities, nameof(EntityData));
    }

    // Metodă generică privată pentru a gestiona logica de căutare și avertizare
    private GameObject GetPrefab<T>(T key, Dictionary<T, GameObject> map, string keyTypeName) where T : UnityEngine.Object
    {
        if (key == null)
        {
            return null;
        }

        if (map.TryGetValue(key, out GameObject prefab))
        {
            return prefab;
        }

        Debug.LogWarning($"Prefab-ul vizual pentru {keyTypeName} '{key.name}' nu a fost găsit în {nameof(ItemVisualManager)}.");
        return null;
    }


    public Item GetItemDataByName(string itemName)
    {
        foreach (var item in prefabMap_Items.Keys)
        {
            if (item.itemName == itemName) // Sau item.name
            {
                return item;
            }
        }

        return null;
    }

    public EntityData GetEntityDataByName(string entityName)
    {
        foreach (var entity in prefabMap_Entities.Keys)
        {
            if (entity.name == entityName) 
            {
                return entity;
            }
        }
        return null;
    }

    
}