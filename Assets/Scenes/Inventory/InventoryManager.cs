using UnityEngine;
using System.Collections.Generic;
using System.Text;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    // Map: key = numele itemului, value = lista sloturilor de acel tip
    private Dictionary<string, List<InventorySlot>> inventory = new Dictionary<string, List<InventorySlot>>();

    // Listă globală cu toate sloturile existente (pentru acces rapid sau UI)
    public List<InventorySlot> allSlots = new List<InventorySlot>();

    // Limite
    public int max_slots = 6;
    public float dropDistance = 1.5f;
    private int current_slots = 0;
    private static int nextSlotIndex = 0; // ID unic global

    [Header("Settings")]
    public KeyCode debugKey = KeyCode.I;
    public bool autoRefresh = false;

    [Header("Inventory Debug")]
    public List<InventorySlot> currentItemsDebug = new List<InventorySlot>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // =============================== ADD ITEM ===============================

    public bool AddItem(Item itemData)
    {
        string key = itemData.itemName;
        int remain = itemData.amount;

        if (!inventory.ContainsKey(key))
            inventory[key] = new List<InventorySlot>();

        // 1️⃣ Încearcă să umpli sloturile existente
        foreach (InventorySlot slot in inventory[key])
        {
            remain = slot.IncreaseCount(remain);
            if (remain <= 0)
            {
                UpdateDebugList();
                return true;
            }
        }

        // 2️⃣ Creează sloturi noi dacă mai rămân iteme
        while (remain > 0 && current_slots < max_slots)
        {
            InventorySlot newSlot = new InventorySlot(itemData, nextSlotIndex++);
            remain = newSlot.IncreaseCount(remain);
            inventory[key].Add(newSlot);
            allSlots.Add(newSlot);
            current_slots++;
        }

        if (remain > 0)
        {

            GlobalEvents.TriggerPlaySound("Inventory_Full");
            Debug.LogWarning($"⚠️ Inventarul este plin! Nu s-au putut adăuga {remain} bucăți din {key}.");
            return false;
        }

        UpdateDebugList();
        return true;
    }

    // =============================== REMOVE ITEM ===============================

    public bool DecreaseItem(string itemName, int amount)
    {
        if (!inventory.ContainsKey(itemName))
        {
            Debug.LogWarning($"Itemul {itemName} nu există în inventar!");

            var allKeys = string.Join(", ", inventory.Keys);
            Debug.Log($"Chei disponibile în inventar: [{allKeys}]");
            return false;
        }

        int remain = amount;
        List<InventorySlot> slots = inventory[itemName];

        // Iterăm prin sloturi și scădem din fiecare
        for (int i = 0; i < slots.Count && remain > 0; i++)
        {
            InventorySlot slot = slots[i];
            remain = slot.DecreaseCount(remain);
        }

        // Dacă lista acelui item e goală, o scoatem din map
        if (slots.Count == 0)
            inventory.Remove(itemName);

        if (remain > 0)
            Debug.LogWarning($"⚠️ Nu s-au putut elimina toate obiectele ({remain} rămase).");

        UpdateDebugList();
        return true;
    }

    // =============================== DEBUG ===============================

    private void UpdateDebugList()
    {
        currentItemsDebug.Clear();
        currentItemsDebug.AddRange(allSlots);
    }

    public void PrintInventory()
    {
        var builder = new StringBuilder();

        builder.AppendLine("===== 📦 INVENTAR CURENT =====");
        builder.AppendLine($"Sloturi folosite: {current_slots} / {max_slots}");
        builder.AppendLine("------------------------------");

        if (allSlots.Count == 0)
        {
            builder.AppendLine("(Inventar gol)");
        }
        else
        {
            foreach (var slot in allSlots)
            {
                string name = slot.itemData != null ? slot.itemData.itemName : "(null)";
                builder.AppendLine($"[#{slot.slotIndex}] {name} x{slot.count}/{slot.itemData.stackSize}");
            }
        }

        builder.AppendLine("==============================");
        Debug.Log(builder.ToString());
    }


    public int GetTotalItemCount(string itemName)
    {
        if (!inventory.ContainsKey(itemName))
        {
            return 0;
        }

        int total = 0;
        // Adună count-ul din fiecare slot al itemului
        foreach (InventorySlot slot in inventory[itemName])
        {
            total += slot.count;
        }

        return total;
    }

    public int GetCurrentSlots()
    {
        return current_slots;
    }

    public void RemoveSlot(InventorySlot slot)
    {
        if (slot == null || slot.itemData == null) return;

        string key = slot.itemData.itemName;

        if (inventory.ContainsKey(key))
        {
            inventory[key].Remove(slot);
            if (inventory[key].Count == 0)
                inventory.Remove(key);
        }

        allSlots.Remove(slot);
        current_slots--;

        Debug.Log($"✅ Slot {slot.slotIndex} eliminat complet din inventar.");
        UpdateDebugList();
    }


    private void Update()
    {
        if (Input.GetKeyDown(debugKey))
            PrintInventory();

        if (autoRefresh)
        {
            PrintInventory();
            autoRefresh = false;
        }
    }

    public bool DropItem(InventorySlot slot, int amount)
    {
        Debug.Log($"<color=cyan>[DropItem] Tentativă drop: {(slot != null && slot.itemData != null ? slot.itemData.itemName : "NULL/GOL")}, Cantitate: {amount}</color>");

        // 1. Verificări de siguranță
        if (slot == null || slot.itemData == null) return false;

        if (amount <= 0 || amount > slot.count) 
        {
            amount = slot.count;
        }

        if (ItemVisualManager.Instance == null) return false;

        GameObject itemPrefab = ItemVisualManager.Instance.GetItemVisualPrefab(slot.itemData);
        if (itemPrefab == null) return false;

        // 2. Calculare poziție de bază
        Transform playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        Camera mainCamera = Camera.main;
        Vector3 baseDropPosition = (mainCamera != null) 
            ? mainCamera.transform.position + mainCamera.transform.forward * dropDistance 
            : (playerTransform != null ? playerTransform.position + playerTransform.forward * dropDistance : Vector3.zero);

        // ========================================================
        // 3. LOGICA DE FOR LOOP PENTRU TURN (STACKING)
        // ========================================================
        for (int i = 0; i < amount; i++)
        {
            // Adăugăm un mic offset pe verticală pentru fiecare obiect din stivă
            // 0.2f este o valoare estimativă, ajusteaz-o în funcție de mărimea modelului tău 3D
            Vector3 spawnPosition = baseDropPosition + new Vector3(0, i * 0.25f, 0);

            GameObject droppedObject = Instantiate(itemPrefab, spawnPosition, Quaternion.identity);

            // Configurăm starea pentru fiecare obiect individual
            WorldEntityState state = droppedObject.GetComponent<WorldEntityState>();
            if (state != null)
            {
                state.isSpawnedAtRuntime = true;
                state.uniqueID = System.Guid.NewGuid().ToString();

                // Dacă e o unealtă (amount va fi 1 oricum), îi dăm durabilitatea
                if (slot.state != null)
                {
                    state.currentHealthOrDurability = slot.state.currentDurability;
                }
            }
        }

        // 4. ELIMINAREA DIN INVENTAR (după ce am creat toate obiectele)
        string nameToDecrease = slot.itemData.itemName;
        bool removed = DecreaseItem(nameToDecrease, amount);
        
        if (removed)
            Debug.Log($"<color=green>[DropItem] Succes: {amount}x {nameToDecrease} eliminate și create fizic.</color>");

        return true;
    }

    public bool AddExistingSlot(InventorySlot slot)
    {
        if (slot == null || slot.itemData == null) return false;
        
        // VERIFICARE CRITICĂ: Forțăm count-ul la 1 dacă e o unealtă care se întoarce
        if (slot.count <= 0) 
        {
            Debug.Log($"[Inventory] Fix: Am setat count la 1 pentru {slot.itemData.itemName} care era 0.");
            slot.count = 1;
        }

        if (current_slots >= max_slots) 
        {
            Debug.LogWarning("Inventar plin!");
            return false;
        }

        // Găsim un index vizual valid (0-5)
        slot.slotIndex = GetFirstAvailableIndex(); 

        if (!allSlots.Contains(slot))
        {
            allSlots.Add(slot);
            string key = slot.itemData.itemName;
            if (!inventory.ContainsKey(key)) inventory[key] = new List<InventorySlot>();
            inventory[key].Add(slot);
            current_slots++;
        }

        UpdateDebugList();
        Debug.Log($"✅ {slot.itemData.itemName} a revenit în inventar la slot {slot.slotIndex} cu count {slot.count}.");
        return true;
    }

    // Metodă nouă pentru a găsi primul slot liber între 0 și max_slots
    private int GetFirstAvailableIndex()
    {
        // Creăm un set cu indecșii deja ocupați
        HashSet<int> occupiedIndices = new HashSet<int>();
        foreach (var s in allSlots)
        {
            occupiedIndices.Add(s.slotIndex);
        }

        // Căutăm prima cifră liberă de la 0 la 5
        for (int i = 0; i < max_slots; i++)
        {
            if (!occupiedIndices.Contains(i))
            {
                return i;
            }
        }

        return 0; // Fallback
    }
    
    public void ClearInventory()
    {
        inventory.Clear();
        allSlots.Clear();
        current_slots = 0;
        // nextSlotIndex = 0; // Opțional: dacă vrei să resetezi și ID-urile sloturilor
        UpdateDebugList();
    }
}

// ============================================================================

[System.Serializable]
public class ItemState
{
    public float currentDurability; 
}

[System.Serializable]
public class InventorySlot
{
    public int slotIndex;
    public Item itemData;
    public Sprite icon;
    public int count = 0;
    private int max_count;
    private InventoryManager manager;
    // folosita doar pentru tool
    public ItemState state;
    // Proprietate ajutătoare pentru acces facil la ToolItem (dacă este cazul)
    public ToolItem ToolItemData => itemData as ToolItem;

    public InventorySlot(Item data, int index)
    {
        slotIndex = index;
        itemData = data;
        icon = data.icon;
        max_count = data.stackSize;
        manager = InventoryManager.Instance;
        // Inițializarea stării dinamice la creare
        InitializeState(data);

        

    }


    public void DropOne()
    {
        // ADAUGARE VERIFICARE SLOT GOL
        if (count <= 0)
        {
            Debug.LogWarning($"Nu se poate arunca item din slotul #{slotIndex} deoarece este gol (Count = 0).");
            return;
        }

        // De asemenea, asigură-te că managerul există
        if (manager == null)
        {
            Debug.LogError("InventoryManager nu este inițializat. Nu se poate arunca itemul.");
            return;
        }

        // Aruncă o bucată, dacă este cazul
        manager.DropItem(this, 1);
    }

    public void DropAll()
    {
        if (count <= 0)
        {
            Debug.LogWarning($"Nu se poate face Drop All din slotul #{slotIndex} deoarece este gol.");
            return;
        }

        if (manager == null)
        {
            Debug.LogError("InventoryManager nu este inițializat.");
            return;
        }

        // Trimitem tot ce avem în acest slot către metoda DropItem
        // manager.DropItem se ocupă deja de instanțierea vizuală și de eliminarea din liste
        manager.DropItem(this, count);
        
        Debug.Log($"[InventorySlot] Drop All executat pentru {itemData.itemName}. Toate cele {count} bucăți au fost eliminate.");
    }

    public void ApplyDurabilityLoss()
    {
        ToolItem tool = itemData as ToolItem;

        if (state == null || tool == null || tool.durabilityLossPerUse <= 0)
        {
            // Nu este o unealtă urmăribilă sau nu pierde durabilitate.
            return;
        }

        // 1. Scăderea durabilității
        state.currentDurability -= tool.durabilityLossPerUse;

        Debug.Log($"🛠️ Durabilitate {tool.itemName} scazută la {state.currentDurability:F1}");


        // 2. Verificare Distrugere
        if (state.currentDurability <= 0)
        {
            state.currentDurability = 0;

            Debug.Log($"💔 Unealta {tool.itemName} s-a rupt și va fi eliminată.");


            if (EquippedManager.Instance != null && EquippedManager.Instance.GetEquippedSlot() == this)
            {
                EquippedManager.Instance.DestroyEquippedToolBySlot(this);
            }
            
            if (manager != null)
            {
                // Deși slotul ar trebui să fie deținut de EquippedManager în acest caz,
                // apelăm un eveniment sau o metodă care să gestioneze distrugerea.

                // NOTĂ: Dacă slotul este echipat, el NU este în inventar, 
                // ci este în EquippedManager. Trebuie să notificăm EquippedManager.
                // GlobalEvents.RequestUnequipToolBroken(this); // Presupunem un nou eveniment

                // Dacă cumva ar fi rămas în inventar, l-am scoate:
                // manager.RemoveSlot(this);
            }
        }

        // NOTĂ: Aici ar trebui emis un eveniment UI pentru a actualiza bara de durabilitate
        // GlobalEvents.OnDurabilityChanged?.Invoke(this);
    }

    public int IncreaseCount(int amount)
    {
        int availableSpace = max_count - count;
        int toAdd = Mathf.Min(amount, availableSpace);
        count += toAdd;
        return amount - toAdd;
    }

    private void InitializeState(Item data)
    {
        // ItemState este necesar DOAR dacă itemul nu se stivuiește (StackSize = 1) 
        // ȘI are durabilitate de urmărit (> 0)
        if (data.stackSize == 1 && data.maxDurability > 0)
        {
            state = new ItemState
            {
                currentDurability = data.maxDurability
            };
        }
        else
        {
            state = null;
        }
    }


    public void HandleUse()
    {
        // 1. Verificare: Itemul este o Unealtă (echipabilă, cu stare dinamică)?
        if (itemData is ToolItem tool && state != null)
        {
            // sterge(muta din inventar)
            manager.RemoveSlot(this);
            GlobalEvents.RequestSlotEquip(this);
            return;
        }

        // 2. Altfel (este un item consumabil, material, sau nu are EquippedManager), apelăm logica SO de bază.
        itemData.Use();
    }

    public int DecreaseCount(int amount)
    {
        int toRemove = Mathf.Min(amount, count);
        count -= toRemove;

        // dacă slotul a rămas gol, se elimină automat
        if (count <= 0)
        {
            Debug.Log($"🗑️ Slot {slotIndex} ({itemData.itemName}) a ajuns la 0 și va fi eliminat.");
            manager.RemoveSlot(this);
        }

        return amount - toRemove;
    }
    



    
}

