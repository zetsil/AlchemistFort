using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    // Aici tragi și plasezi asset-ul tău Apple, Stick, etc.
    public Item itemData; // <-- Folosește clasa de bază Item!
    
    private Rigidbody rb;
    private AbstractActionLogicSO pickUpLogic; 
    private GameObject actionButtonPrefab;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        if (itemData == null) return;

        // 1. Încărcăm automat resursele necesare
        // Presupunem că le ai în Assets/Resources/Actions/...
        pickUpLogic = Resources.Load<AbstractActionLogicSO>("Actions/PickUpAction");
        actionButtonPrefab = Resources.Load<GameObject>("Actions/iconPrefab");

        if (pickUpLogic == null || actionButtonPrefab == null)
        {
            Debug.LogError($"[ItemPickup] Lipsesc resursele în folderul Resources! Logic: {pickUpLogic}, Prefab: {actionButtonPrefab}");
            return;
        }

        // 2. Creăm Rețeta dinamică
        ActionRecipeSO dynamicRecipe = ScriptableObject.CreateInstance<ActionRecipeSO>();
        dynamicRecipe.actionName = "Pick Up " + itemData.itemName;
        dynamicRecipe.actionIcon = itemData.icon;
        dynamicRecipe.actionLogic = pickUpLogic;

        // 3. Setup UI
        SetupDynamicUI(dynamicRecipe);

        VisibilityRangeController visibility = GetComponent<VisibilityRangeController>();
        if (visibility == null)
        {
            visibility = gameObject.AddComponent<VisibilityRangeController>();

            // Configurăm setările cerute de tine
            visibility.activationDistance = 4f; // Distanța la care apare UI-ul
            visibility.hideOnlyInteractionButtons = true; // Ascunde doar butoanele, nu mărul
            visibility.shouldReinitialize = true;
        }
        
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

        if (itemData is ToolItem toolItem)
        {
            if (EquippedManager.Instance.IsEquippedSlotEmpty())
            {

                InventorySlot newSlot = new InventorySlot(toolItem, -1);

                GlobalEvents.RequestSlotEquip(newSlot);

                // --- LOGICA DE SALVARE (BLACKING LIST) ---
                // Încercăm să luăm componenta WorldItem pentru a accesa ID-ul unic
                WorldEntityState worldItem = GetComponent<WorldEntityState>();
                if (worldItem != null && !worldItem.isSpawnedAtRuntime)
                {
                    // Dacă are un ID valid și este un obiect original din scenă, îl trecem pe lista neagră
                    if (!string.IsNullOrEmpty(worldItem.uniqueID))
                    {
                        SaveManager.Instance.RegisterDestroyedWorldItem(worldItem.uniqueID);
                    }
                }
                // ------------------------------------------

                Debug.Log($"✅ Unealta {toolItem.itemName} a fost echipată direct din Lume.");

                Destroy(gameObject);

                // Opțional: Trimitem un semnal de sunet specific pentru echipare
                // GlobalEvents.TriggerPlaySound("EquipToolSound"); 
                return;
            }
        }

        bool added = InventoryManager.Instance.AddItem(itemData);

        if (added)
        {
            // Obiectul a fost adăugat cu succes în inventar.
            Debug.Log($"✅ Colectat: {itemData.itemName} x{itemData.amount}.");

            // Notificare (pentru sunet/UI, indiferent de succesul adăugării)
            // Păstrăm logica ta de semnal combinat pentru notificare/sunet.
            string combinedSignal = "Collect_" + itemData.itemName;
            GlobalEvents.TriggerPlaySound(combinedSignal); // Managerul de sunet primește "Collect_Wood"


            // --- LOGICA DE SALVARE (BLACKING LIST) ---
            // Încercăm să luăm componenta WorldItem pentru a accesa ID-ul unic
            WorldEntityState worldItem = GetComponent<WorldEntityState>();
            if (worldItem != null && !worldItem.isSpawnedAtRuntime)
            {
                // Dacă are un ID valid și este un obiect original din scenă, îl trecem pe lista neagră
                if (!string.IsNullOrEmpty(worldItem.uniqueID))
                {
                    SaveManager.Instance.RegisterDestroyedWorldItem(worldItem.uniqueID);
                }
            }
            // ------------------------------------------

            // Distrugem obiectul fizic, deoarece adăugarea a fost confirmată.
            Destroy(gameObject);
        }
        else
        {
            Debug.LogWarning($"❌ Inventarul este plin! Nu s-a putut adăuga {itemData.itemName}.");

        }

    }
}