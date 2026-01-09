using UnityEngine;

public class WorldEntityState : MonoBehaviour
{
    [Header("Identificare Salvare")]
    public string uniqueID; 
    public bool isSpawnedAtRuntime = false;

    [Header("Stare")]
    // Folosim o variabilă generică pentru "sănătate" sau "durabilitate"
    public float currentHealthOrDurability = -1f;
    [Header("Salvare Date")]
    [Tooltip("Numele exact al Item-ului sau EntityData din ItemVisualManager")]
    public string itemNameForSave;

    // Referințe opționale (le caută singur)
    private ItemPickup pickup;
    private Entity entity;

    private void Awake()
    {
        pickup = GetComponent<ItemPickup>();
        entity = GetComponent<Entity>();

        if (isSpawnedAtRuntime && string.IsNullOrEmpty(uniqueID))
        {
            uniqueID = System.Guid.NewGuid().ToString();
        }
    }

    private void Start()
    {
        // Dacă e un OBIECT (ItemPickup)
        if (pickup != null && currentHealthOrDurability == -1f)
        {
            currentHealthOrDurability = pickup.itemData.maxDurability;
        }
        // Dacă e o ENTITATE (Zombi/Copac)
        else if (entity != null && currentHealthOrDurability == -1f)
        {
            currentHealthOrDurability = entity.entityData.maxHealth;
        }
    }

    // Aceasta înlocuiește RegisterDestroyedWorldItem
    public void OnDeathOrPickup()
    {
        if (!string.IsNullOrEmpty(uniqueID))
        {
            SaveManager.Instance.RegisterDestroyedWorldItem(uniqueID);
        }
    }
}