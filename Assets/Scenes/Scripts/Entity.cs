using UnityEngine;


public abstract class Entity : MonoBehaviour
{
    // === NOU: Referința la ScriptableObject ===
    [Header("Datele Entității (Scriptable Object)")]
    [Tooltip("Sursa de date a entității (Viață, Loot, Vulnerabilități).")]
    public EntityData entityData;

    // ===========================================
    // Câmpurile STATICE sunt mutate în EntityData
    // ===========================================

    // --- Viața Curentă (Rămâne pe GameObject, deoarece este dinamică) ---
    [Header("Statistici Dinamice")]
    [Tooltip("Viața curentă a entității.")]
    [SerializeField]
    protected int currentHealth;
    public int CurrentHealth => currentHealth;
    public int MaxHealth= 0;

    public float Damage
    {
        get
        {
            if (entityData != null)
            {
                return entityData.baseAttackDamage;
            }
            return 1f; 
        }
    }

    // --- Funcții de Bază ---

    protected virtual void Start()
    {
        // Setăm viața maximă din ScriptableObject
        if (entityData != null)
        {
            currentHealth = entityData.maxHealth;
            MaxHealth = currentHealth;
        }
        else
        {
            Debug.LogError($"EntityData lipsește pe {gameObject.name}! Setez viața implicit la 1.");
            currentHealth = 1;
        }
    }

    /// <summary>
    /// Aplică damage entității, luând în considerare tipul de unealtă folosită.
    /// </summary>
    public virtual void TakeDamage( float baseDamage,
                                    ToolType attackingToolType = ToolType.None)
    {
        if (currentHealth <= 0 || entityData == null) return;

        float multiplier = 0f;

        foreach (var effectiveness in entityData.toolEffectivenesses)
        {
            if (effectiveness.toolType == attackingToolType)
            {
                multiplier = effectiveness.damageMultiplier;
                break;
            }
        }

        int finalDamage = Mathf.RoundToInt(baseDamage * multiplier);

        if (finalDamage <= 0)
        {
            string nopeSignal = $"Hit_{attackingToolType}_{entityData.name}";
            GlobalEvents.TriggerPlaySound(nopeSignal);

            Debug.Log($"{gameObject.name} este imun/rezistent la damage-ul de tip {attackingToolType}. Base: {baseDamage} x Multiplier: {multiplier}.");
            return;
        }

        currentHealth -= finalDamage;
        currentHealth = Mathf.Max(currentHealth, 0);

        // 📡 EMITEREA SEMNALULUI COMBINAT (Cazul HIT)
        // Format: "Hit|ToolType.Nume|NumeGameObject"
        string hitSignal = $"Hit_{attackingToolType}_{entityData.name}";
        GlobalEvents.TriggerPlaySound(hitSignal);

        Debug.Log($"Damage aplicat de {attackingToolType}: {finalDamage}. Viață rămasă: {currentHealth}.");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    protected virtual void Die()
    {
        Debug.Log($"{gameObject.name} a murit/distrus.");
        DropLoot();
        Destroy(gameObject);
    }

    protected void DropLoot()
    {
        if (ItemVisualManager.Instance == null || entityData == null) return;

        foreach (ItemDrop drop in entityData.possibleDrops)
        {
            if (Random.value <= drop.dropChance)
            {
                int quantity = Random.Range(drop.minQuantity, drop.maxQuantity + 1);
                if (quantity <= 0) continue;

                GameObject visualPrefab = ItemVisualManager.Instance.GetItemVisualPrefab(drop.item);

                if (visualPrefab != null)
                {
                    for (int i = 0; i < quantity; i++)
                    {
                        float spread = 0.5f;
                        Vector3 randomOffset = new Vector3(Random.Range(-spread, spread),
                                                        Random.Range(0f, 0.5f),
                                                        Random.Range(-spread, spread));

                        Vector3 dropPosition = transform.position + randomOffset;
                        GameObject droppedItem = Instantiate(visualPrefab, dropPosition, Quaternion.identity);
                    }
                    Debug.Log($"Dropped: {quantity} x {drop.item.itemName}");
                }
                else
                {
                    Debug.LogWarning($"Itemul {drop.item.itemName} a fost selectat, dar nu are prefab vizual setat.");
                }
            }
        }
    }
    
    protected virtual void Update()
    {

    }
}