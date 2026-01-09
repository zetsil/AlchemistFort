using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public abstract class Entity : MonoBehaviour
{
    // === DATE SCRIPTABLE OBJECT ===
    [Header("Datele Entității (Scriptable Object)")]
    [Tooltip("Sursa de date a entității (Viață, Loot, Vulnerabilități).")]
    public EntityData entityData;

    // === STATISTICI DINAMICE ===
    [Header("Statistici Dinamice")]
    [SerializeField] protected int currentHealth;
    public int CurrentHealth => currentHealth;
    public bool isDead = false;
    public int MaxHealth { get; private set; }

    // === OPȚIUNI FLASH (VIZUAL) ===
    [System.Serializable]
    public class FlashOptions
    {
        [Tooltip("Dacă pui un material Unlit aici, flash-ul va fi vizibil chiar și prin animații complexe.")]
        public Material flashMaterial; 

        [ColorUsage(true, true)]
        public Color flashColor = Color.white * 4f; // HDR pentru intensitate
        
        public float duration = 0.05f;

        [Tooltip("Dacă lista e goală, scriptul va găsi automat toate MeshRenderers și SkinnedMeshRenderers la Start.")]
        public List<Renderer> targetRenderers = new List<Renderer>();
    }

    [Header("Vizual - Damage Flash")]
    public FlashOptions flashSettings;

    // === CACHE INTERN PENTRU FLASH ===
    private Dictionary<Renderer, Material[]> originalMaterialsMap = new Dictionary<Renderer, Material[]>();
    private Dictionary<Material, Color> originalColorsMap = new Dictionary<Material, Color>();
    private List<Material> instanceMaterials = new List<Material>();
    private Coroutine flashCoroutine;

    // --- PROPRIETĂȚI ---
    public float Damage => entityData != null ? entityData.baseAttackDamage : 1f;

    // --- LOGICĂ START ---
    protected virtual void Start()
    {
        // 1. Inițializare Viață
        if (entityData != null)
        {
            currentHealth = entityData.maxHealth;
            MaxHealth = currentHealth;
        }
        else
        {
            Debug.LogError($"EntityData lipsește pe {gameObject.name}!");
            currentHealth = 1;
        }

        // 2. Setup Renderers & Materials
        SetupVisualCache();
    }

    private void SetupVisualCache()
    {
        // Găsim toate rendererele dacă lista este goală
        if (flashSettings.targetRenderers.Count == 0)
        {
            flashSettings.targetRenderers.AddRange(GetComponentsInChildren<Renderer>());
        }

        foreach (var r in flashSettings.targetRenderers)
        {
            if (r == null) continue;

            // Salvăm materialele originale (pentru Material Swap)
            originalMaterialsMap[r] = r.sharedMaterials;

            // Accesăm r.materials (Unity creează instanțe) pentru a salva culorile originale
            foreach (var mat in r.materials)
            {
                instanceMaterials.Add(mat);
                
                if (!originalColorsMap.ContainsKey(mat))
                {
                    if (mat.HasProperty("_Color")) originalColorsMap[mat] = mat.color;
                    else if (mat.HasProperty("_BaseColor")) originalColorsMap[mat] = mat.GetColor("_BaseColor");
                }
            }
        }
    }

    // --- LOGICĂ DAMAGE ---
    public virtual void TakeDamage(float baseDamage, ToolType attackingToolType = ToolType.None)
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

        // Numele semnalului (folosit și pentru HIT și pentru imunitate)
        string signal = $"Hit_{attackingToolType}_{entityData.name}";

        if (finalDamage <= 0)
        {
            GlobalEvents.TriggerPlaySound(signal);
            Debug.Log($"{gameObject.name} este imun la {attackingToolType}.");
            return;
        }

        // Aplicare Damage
        currentHealth = Mathf.Max(currentHealth - finalDamage, 0);
        
        // Feedback Vizual și Audio
        GlobalEvents.TriggerPlaySound(signal);
        TriggerFlash();

        if (this is ZombieNPC && HitStopManager.Instance != null)
        {
            // 0.07f este o valoare standard pentru un impact satisfăcător
            HitStopManager.Instance.RequestHitStop(0.05f);
            ApplyKnockbackFromCenter(8f);
        }



        Debug.Log($"{gameObject.name} a luat {finalDamage} damage. Viață: {currentHealth}");

        if (currentHealth <= 0) Die();
    }

    // --- LOGICĂ FLASH ---
    private void TriggerFlash()
    {
        if (flashSettings.targetRenderers.Count == 0) return;
        
        if (flashCoroutine != null) StopCoroutine(flashCoroutine);
        flashCoroutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        // 1. APLICARE FLASH
        if (flashSettings.flashMaterial != null)
        {
            // Metoda A: Înlocuire completă de Material (Cea mai sigură pentru animații)
            foreach (var r in flashSettings.targetRenderers)
            {
                if (r == null) continue;
                Material[] flashArray = new Material[r.sharedMaterials.Length];
                for (int i = 0; i < flashArray.Length; i++) flashArray[i] = flashSettings.flashMaterial;
                r.materials = flashArray;
            }
        }
        else
        {
            // Metoda B: Modificare proprietăți Material (Tinting)
            foreach (var mat in instanceMaterials)
            {
                if (mat == null) continue;
                if (mat.HasProperty("_Color")) mat.color = flashSettings.flashColor;
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", flashSettings.flashColor);
                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", flashSettings.flashColor);
                }
            }
        }

        yield return new WaitForSeconds(flashSettings.duration);

        // 2. REVENIRE LA NORMAL
        if (flashSettings.flashMaterial != null)
        {
            foreach (var r in flashSettings.targetRenderers)
            {
                if (r != null && originalMaterialsMap.ContainsKey(r))
                    r.materials = originalMaterialsMap[r];
            }
        }
        else
        {
            foreach (var mat in instanceMaterials)
            {
                if (mat == null) continue;
                Color originalCol = originalColorsMap.ContainsKey(mat) ? originalColorsMap[mat] : Color.white;

                if (mat.HasProperty("_Color")) mat.color = originalCol;
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", originalCol);
                if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", Color.black);
            }
        }
    }

    // --- LOGICĂ MOARTE & LOOT ---
    protected virtual void Die()
    {
        Debug.Log($"{gameObject.name} distrus.");
        isDead = true;

        // Save state !!!
        WorldEntityState state = GetComponent<WorldEntityState>();
        if (state != null)
        {
            state.OnDeathOrPickup();
        }

        DropLoot();
        Destroy(gameObject);
    }

    public void ApplyKnockbackFromCenter(float force)
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        UnityEngine.AI.NavMeshAgent agent = GetComponent<UnityEngine.AI.NavMeshAgent>();

        if (rb != null)
        {
            // 1. Spunem agentului să NU mai miște obiectul, dar îl lăsăm ACTIV
            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.updatePosition = false;
                agent.updateRotation = false;
            }

            // 2. Activăm fizica
            rb.isKinematic = false;
            Vector3 pushDirection = Camera.main.transform.forward;
            pushDirection.y = 0.2f; 
            
            rb.linearVelocity = Vector3.zero; 
            rb.AddForce(pushDirection.normalized * force, ForceMode.Impulse);

            StartCoroutine(ResetAfterKnockback(rb, agent));
        }
    }

    private IEnumerator ResetAfterKnockback(Rigidbody rb, UnityEngine.AI.NavMeshAgent agent)
    {
        yield return new WaitForSeconds(0.25f);

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        if (agent != null && !isDead)
        {
            // Sincronizăm poziția agentului cu locul unde a aterizat corpul
            agent.nextPosition = transform.position;
            
            // Rendăm controlul înapoi agentului
            agent.updatePosition = true;
            agent.updateRotation = true;
            
            if (agent.isOnNavMesh)
                agent.isStopped = false;
        }
    }
    

    public virtual void RestoreHealth(float amount)
    {
        // Convertim float-ul în int folosind rotunjire
        int healAmount = Mathf.RoundToInt(amount);
        currentHealth += healAmount;
        
        // Verificăm viața maximă
        if (entityData != null && currentHealth > entityData.maxHealth)
        {
            currentHealth = Mathf.RoundToInt(entityData.maxHealth);
        }
        
        Debug.Log($"{gameObject.name} s-a vindecat cu {healAmount}. HP actual (int): {currentHealth}");
    }

    protected virtual void DropLoot()
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
                        Vector3 randomOffset = new Vector3(
                            Random.Range(-spread, spread),
                            Random.Range(0.2f, 0.7f),
                            Random.Range(-spread, spread)
                        );

                        // 1. Instanțiem obiectul și păstrăm referința către el
                        GameObject droppedObj = Instantiate(visualPrefab, transform.position + randomOffset, Quaternion.identity);

                        // 2. Accesăm componenta WorldEntityState (fostul WorldItem)
                        WorldEntityState state = droppedObj.GetComponent<WorldEntityState>();

                        if (state != null)
                        {
                            // 3. Îl marcăm ca fiind spawnat în timpul jocului (Whitelist)
                            state.isSpawnedAtRuntime = true;

                            // 4. Generăm un ID unic nou imediat
                            // (Deși Awake-ul din WorldEntityState face asta, e bine să fim expliciți)
                            state.uniqueID = System.Guid.NewGuid().ToString();

                            Debug.Log($"<color=yellow>Loot generat:</color> {drop.item.itemName} cu ID unic pentru salvare.");
                        }
                    }
                }
            }
        }
    }

    protected virtual void Update() { }
}