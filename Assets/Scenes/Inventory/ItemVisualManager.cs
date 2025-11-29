// Fișier: ItemVisualManager.cs

using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public struct ItemVisualMapping
{
    public Item itemData;
    public GameObject visualPrefab;
}

public class ItemVisualManager : MonoBehaviour
{
    public static ItemVisualManager Instance { get; private set; }

    [Header("Mapări Item -> Prefab")]
    [Tooltip("Asociază fiecare Item ScriptableObject cu modelul său 3D corespunzător.")]
    [SerializeField]
    private List<ItemVisualMapping> itemVisuals = new List<ItemVisualMapping>();
    
    private Dictionary<Item, GameObject> prefabMap = new Dictionary<Item, GameObject>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;

            PopulateDictionary();
        }
    }

    private void PopulateDictionary()
    {
        prefabMap.Clear();
        foreach (var map in itemVisuals)
        {
            if (map.itemData != null && map.visualPrefab != null)
            {
                // Cheia este ScriptableObject-ul Item Data
                prefabMap.Add(map.itemData, map.visualPrefab);
            }
        }
    }


    public GameObject GetVisualPrefab(Item item)
    {
        if (item == null)
        {
            return null;
        }

        if (prefabMap.TryGetValue(item, out GameObject prefab))
        {
            return prefab;
        }

        Debug.LogWarning($"Prefab-ul vizual pentru item-ul '{item.itemName}' nu a fost găsit în {nameof(ItemVisualManager)}.");
        return null;
    }
}