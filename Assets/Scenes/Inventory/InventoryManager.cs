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
        if (slot == null || slot.itemData == null) return false;
        if (amount <= 0 || amount > slot.count) amount = slot.count;

        if (ItemVisualManager.Instance == null)
        {
            Debug.LogError("ItemVisualManager lipsește!");
            return false;
        }

        GameObject itemPrefab = ItemVisualManager.Instance.GetItemVisualPrefab(slot.itemData);
        if (itemPrefab == null) return false;

        Transform playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (playerTransform == null) return false;

        Camera mainCamera = Camera.main;
        Vector3 dropPosition = (mainCamera != null) 
            ? mainCamera.transform.position + mainCamera.transform.forward * dropDistance 
            : playerTransform.position + playerTransform.forward * dropDistance;

        // --- LOGICA DE INSTANȚIERE ȘI SALVARE ---
        GameObject droppedObject = Instantiate(itemPrefab, dropPosition, Quaternion.identity);

        // 1. Accesăm componenta de stare pentru salvare
        WorldEntityState state = droppedObject.GetComponent<WorldEntityState>();
        if (state != null)
        {
            // 2. Marcăm obiectul ca fiind spawnat la runtime (pentru a fi inclus în Whitelist)
            state.isSpawnedAtRuntime = true;

            // 3. Generăm ID-ul unic pentru acest obiect nou
            state.uniqueID = System.Guid.NewGuid().ToString();

            // 4. Păstrăm starea obiectului (ex: durabilitatea dacă este o unealtă)
            if (slot.state != null)
            {
                state.currentHealthOrDurability = slot.state.currentDurability;
            }

            Debug.Log($"✅ [SAVE SYSTEM] Referință salvată pentru {slot.itemData.itemName} (ID: {state.uniqueID})");
        }
        // ----------------------------------------

        // Eliminarea din inventar
        DecreaseItem(slot.itemData.itemName, amount);

        return true;
    }

    public bool AddExistingSlot(InventorySlot slot)
    {
        // Verificăm dacă inventarul este plin, ignorând faptul că slotul există deja
        if (current_slots >= max_slots)
        {
            Debug.LogError($"⚠️ Inventarul este plin! Nu se poate returna slotul {slot.itemData.itemName}.");
            return false;
        }

        string key = slot.itemData.itemName;

        if (!inventory.ContainsKey(key))
        {
            inventory[key] = new List<InventorySlot>();
        }

        // 1. Adăugăm slotul în map-ul de inventar și în lista globală
        inventory[key].Add(slot);
        allSlots.Add(slot);

        // 2. Incrementăm numărul de sloturi folosite
        current_slots++;

        // 3. Ne asigurăm că slotIndex-ul nu se suprapune cu următoarele sloturi noi
        // Deși nu ar trebui să se întâmple dacă EquippedManager gestionează corect,
        // actualizăm indexul global pentru siguranță, deși acest slot are deja un index.
        if (slot.slotIndex >= nextSlotIndex)
        {
            nextSlotIndex = slot.slotIndex + 1;
        }

        Debug.Log($"✅ Slotul {slot.itemData.itemName} a fost reintegrat în inventar.");
        UpdateDebugList();
        return true;
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

