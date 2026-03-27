using UnityEngine;
using System.Collections;

public class ItemPickup : MonoBehaviour
{
    public Item itemData; 
    
    private Rigidbody rb;
    private AbstractActionLogicSO pickUpLogic; 
    private GameObject actionButtonPrefab;

    private Vector3 initialSpawnPosition;
    private float fallThreshold = -20f;


    void Awake()
    {
        // Salvăm poziția exactă de la creare (primul frame)
        initialSpawnPosition = transform.position;
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        if (rb != null)
        {
            StartCoroutine(StabilizePhysics());
        }

        if (itemData == null) return;

        pickUpLogic = Resources.Load<AbstractActionLogicSO>("Actions/PickUpAction");
        actionButtonPrefab = Resources.Load<GameObject>("Actions/iconPrefab");

        if (pickUpLogic == null || actionButtonPrefab == null)
        {
            Debug.LogError($"[ItemPickup] Lipsesc resursele în folderul Resources! Logic: {pickUpLogic}, Prefab: {actionButtonPrefab}");
            return;
        }

        ActionRecipeSO dynamicRecipe = ScriptableObject.CreateInstance<ActionRecipeSO>();
        dynamicRecipe.actionName = "Pick Up " + itemData.itemName;
        dynamicRecipe.actionIcon = itemData.icon;
        dynamicRecipe.actionLogic = pickUpLogic;
        // io

        SetupDynamicUI(dynamicRecipe);

        VisibilityRangeController visibility = GetComponent<VisibilityRangeController>();
        if (visibility == null)
        {
            visibility = gameObject.AddComponent<VisibilityRangeController>();
        }

        // IMPORTANT: Îi spunem să își ia referințele UI ACUM, 
        // pentru că tocmai am terminat SetupDynamicUI
        visibility.ManualInitialize();
        }
            

    void Update()
    {
        // Verificăm dacă a căzut prin hartă
        if (transform.position.y < fallThreshold)
        {
            RespawnAtTop();
        }
    }
    

    private void RespawnAtTop()
    {
        Debug.LogWarning($"[SafetyNet] Obiectul {itemData?.itemName} a căzut prin hartă! Teleportare la spawn.");

        // Îl punem la poziția inițială + 1 metru mai sus ca să fim siguri
        transform.position = initialSpawnPosition + Vector3.up * 1.0f;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero; // Îi oprim căderea
            rb.angularVelocity = Vector3.zero; // Îi oprim rotirea
            StartCoroutine(StabilizePhysics()); // Îi dăm încă un mic moment de freeze
        }
    }

    private IEnumerator StabilizePhysics()
    {
        rb.isKinematic = true;

        yield return new WaitForSeconds(0.2f);

        rb.isKinematic = false;

        // Opțional: Forțăm detectarea continuă pentru a preveni bug-urile viitoare
        // rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }

    private void SetupDynamicUI(ActionRecipeSO recipe)
    {
        NewActionUIGenerator uiGenerator = GetComponent<NewActionUIGenerator>();
        if (uiGenerator == null)
        {
            uiGenerator = gameObject.AddComponent<NewActionUIGenerator>();
            uiGenerator.uiForItemPickUp = true;
            uiGenerator.customUIHeight = 0.6f;
            uiGenerator.actionButtonPrefab = this.actionButtonPrefab;
        }

        ActionLevel pickUpLevel = new ActionLevel();
        pickUpLevel.recipes.Add(recipe);

        uiGenerator.actionLevels.Clear();
        uiGenerator.actionLevels.Add(pickUpLevel);
    }

    public void Collect()
    {
        if (itemData == null)
        {
            Debug.LogError("Item Data nu este setat pentru: " + gameObject.name);
            return;
        }

        // 1. Luăm referința la WorldEntityState o singură dată
        WorldEntityState worldItem = GetComponent<WorldEntityState>();

        // 2. VERIFICĂM DACĂ ESTE TOOL PENTRU A-I PĂSTRA DURABILITATEA
        if (itemData is ToolItem toolItem)
        {
            // Cazul A: Echipăm direct în mână
            if (EquippedManager.Instance.IsEquippedSlotEmpty())
            {
                InventorySlot newSlot = new InventorySlot(toolItem, -1);

                // --- INJECTĂM DURABILITATEA ---
                if (newSlot.state != null && worldItem != null)
                {
                    newSlot.state.currentDurability = worldItem.currentHealthOrDurability;
                    Debug.Log($"[Collect] Tool echipat direct. Durabilitate recuperată: {newSlot.state.currentDurability}");
                }

                GlobalEvents.RequestSlotEquip(newSlot);
                
                RegisterDestroyedOriginal(worldItem);
                Destroy(gameObject);
                return;
            }
            // Cazul B: Mâna e plină, îl punem în inventar
            else
            {
                InventorySlot inventorySlot = new InventorySlot(toolItem, -1);
                
                // --- INJECTĂM DURABILITATEA ---
                if (inventorySlot.state != null && worldItem != null)
                {
                    inventorySlot.state.currentDurability = worldItem.currentHealthOrDurability;
                    Debug.Log($"[Collect] Tool pus în inventar. Durabilitate recuperată: {inventorySlot.state.currentDurability}");
                }

                // Folosim AddExistingSlot pentru a trimite slotul personalizat
                bool toolAdded = InventoryManager.Instance.AddExistingSlot(inventorySlot);
                
                if (toolAdded) CompleteCollection(worldItem);
                else Debug.LogWarning($"❌ Inventarul este plin! Nu s-a putut adăuga {itemData.itemName}.");
                
                return; // Oprim execuția aici pentru ToolItem
            }
        }

        // 3. PENTRU OBIECTE NORMALE (Lemn, Măr - Stivuibile, fără durabilitate dinamică)
        bool added = InventoryManager.Instance.AddItem(itemData);

        if (added)
        {
            CompleteCollection(worldItem);
        }
        else
        {
            Debug.LogWarning($"❌ Inventarul este plin! Nu s-a putut adăuga {itemData.itemName}.");
        }
    }

    // --- METODE AJUTĂTOARE PENTRU A PĂSTRA CODUL CURAT ---

    private void CompleteCollection(WorldEntityState worldItem)
    {
        Debug.Log($"✅ Colectat: {itemData.itemName}.");
        string combinedSignal = "Collect_" + itemData.itemName;
        GlobalEvents.TriggerPlaySound(combinedSignal); 

        RegisterDestroyedOriginal(worldItem);
        Destroy(gameObject);
    }

    private void RegisterDestroyedOriginal(WorldEntityState worldItem)
    {
        if (worldItem != null && !worldItem.isSpawnedAtRuntime)
        {
            if (!string.IsNullOrEmpty(worldItem.uniqueID))
            {
                SaveManager.Instance.RegisterDestroyedWorldItem(worldItem.uniqueID);
            }
        }
    }
}