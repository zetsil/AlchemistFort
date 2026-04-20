using UnityEngine;
using System.Collections.Generic;
using System.Text;
using System;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }
    public event Action OnInventoryDataChanged;

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
            InitializeEmptySlots();
            // Sincronizăm și listele de debug/UI
            UpdateDebugList();
        }
        else
        {
            Destroy(gameObject);
        }
    }


    private void InitializeEmptySlots()
{
    allSlots.Clear();
    for (int i = 0; i < max_slots; i++)
    {
        InventorySlot newSlot = new InventorySlot(SlotType.General, null, i);
        
        // ABONARE: Când acest slot execută OnSlotChanged, 
        // managerul va executa RebuildInventoryDictionary și va anunța UI-ul
        newSlot.OnSlotChanged += (slot) => {
            // RebuildInventoryDictionary();
            OnInventoryDataChanged?.Invoke();
        };

        allSlots.Add(newSlot);
    }
}
    

    public bool AddItemFromEquipped(InventorySlot equippedSlot)
{
    Debug.Log($"<color=orange>[Transfer-Start]</color> Încercăm transferul pentru: {(equippedSlot?.itemData != null ? equippedSlot.itemData.itemName : "NULL")}");

    if (equippedSlot == null || equippedSlot.itemData == null) 
    {
        Debug.LogError("<color=red>[Transfer-Error]</color> Slotul echipat este gol sau null!");
        return false;
    }

    // 1. Căutăm un slot gol în lista de 6 sloturi fixe
    for (int i = 0; i < allSlots.Count; i++)
    {
        InventorySlot slot = allSlots[i];

        if (slot.itemData == null) 
        {
            Debug.Log($"<color=cyan>[Transfer-Found]</color> Slot gol găsit la indexul #{i} (ID intern: {slot.slotIndex})");

            // 2. Transferăm datele
            slot.CopyFrom(equippedSlot);
            
            // LOG DE VERIFICARE DUPĂ COPIERE
            Debug.Log($"<color=green>[Transfer-Success]</color> Date copiate în Slot #{i}: " +
                      $"Item: {slot.itemData.itemName}, " +
                      $"Count: {slot.count}, " +
                      $"HasIcon: {slot.icon != null}, " +
                      $"HasState: {slot.state != null}");

            // 3. IMPORTANT: Forțăm refresh-ul dicționarului
            RebuildInventoryDictionary();
            
            // 4. Verificăm dacă dicționarul a "văzut" noul item
            int total = GetTotalItemCount(slot.itemData.itemName);
            Debug.Log($"<color=yellow>[Inventory-Map]</color> Rebuild terminat. Total {slot.itemData.itemName} în inventar: {total}");
                            
            return true;
        }
    }

    Debug.LogWarning("<color=red>[Inventory-Full]</color> Nu s-a găsit niciun slot gol în allSlots (Capacitate: " + allSlots.Count + ")");
    return false;
}

    // =============================== ADD ITEM ===============================

    public bool AddItem(Item itemData)
    {
        if (itemData == null)
        {
            Debug.LogError("<color=red>[Inventory] Eroare: Încerci să adaugi un item NULL!</color>");
            return false;
        }

        string key = itemData.itemName;
        int remain = itemData.amount;

        Debug.Log($"<color=cyan>[Inventory] Început adăugare: {key} x{remain}</color>");

        // 1️⃣ Căutăm sloturi care au deja acest item (Stacking)
        Debug.Log($"<color=cyan>[Inventory Scan] Verificăm cele {allSlots.Count} sloturi pentru stacking cu {key}...</color>");

        foreach (InventorySlot slot in allSlots)
        {
            // LOG PENTRU FIECARE SLOT
            string contentInfo = slot.itemData != null
                ? $"CONȚINE: {slot.itemData.itemName} (x{slot.count}/{slot.itemData.stackSize})"
                : "ESTE: GOL";

            Debug.Log($"<color=gray>[Slot #{slot.slotIndex}] {contentInfo}</color>");

            // LOGICA DE STACKING
            if (slot.itemData != null && slot.itemData.itemName == key)
            {
                // Verificăm dacă slotul este deja plin (pentru a nu încerca stacking inutil)
                if (slot.count >= slot.itemData.stackSize)
                {
                    Debug.Log($"<color=yellow>   -> Slot #{slot.slotIndex} este deja PLIN. Trecem mai departe.</color>");
                    continue;
                }

                int beforeCount = slot.count;
                remain = slot.IncreaseCount(remain);

                Debug.Log($"<color=orange>   -> MATCH! Stacking în Slot #{slot.slotIndex}: {beforeCount} -> {slot.count}. Rămas: {remain}</color>");

                if (remain <= 0) break;
            }
        }

        // 2️⃣ Dacă mai rămân, căutăm sloturi complet GOALE
        if (remain > 0)
        {
            foreach (InventorySlot slot in allSlots)
            {
                if (slot.itemData == null)
                {
                    slot.SetItem(itemData);
                    // Important: Setăm count-ul la 0 înainte de IncreaseCount dacă SetItem nu o face deja
                    slot.count = 0;

                    int toAddInEmpty = remain;
                    remain = slot.IncreaseCount(toAddInEmpty);
                    current_slots++;

                    if (!inventory.ContainsKey(key)) inventory[key] = new List<InventorySlot>();
                    inventory[key].Add(slot);

                    Debug.Log($"<color=green>[New Slot] Ocupat Slot #{slot.slotIndex} cu {key} x{toAddInEmpty - remain}. Rămas total: {remain}</color>");

                    if (remain <= 0) break;
                }
            }
        }

        if (remain > 0)
        {
            Debug.LogWarning($"<color=red>[Inventory Full] Nu s-au putut adăuga {remain} bucăți de {key}!</color>");
        }
        else
        {
            Debug.Log($"<color=white>[Inventory Success] {itemData.itemName} a fost adăugat complet.</color>");
        }
        RebuildInventoryDictionary();
        UpdateDebugList();
        return remain <= 0;
    }
    // =============================== REMOVE ITEM ===============================

    public bool DecreaseItem(string itemName, int amount)
    {
        // 1. Verificăm dacă avem destul (Atomic check)
        if (GetTotalItemCount(itemName) < amount) return false;

        if (!inventory.TryGetValue(itemName, out List<InventorySlot> slots)) return false;

        int remain = amount;

        // 2. Consumăm valorile din sloturi fără să ștergem nimic din liste încă
        for (int i = slots.Count - 1; i >= 0 && remain > 0; i--)
        {
            InventorySlot slot = slots[i];
            if (slot == null) continue;

            // Scădem cantitatea. Dacă slotul ajunge la 0, se va auto-curăța intern (itemData = null)
            remain = slot.DecreaseCount(remain);
        }

        // 3. CURĂȚENIA: Reconstruim totul o singură dată la final
        // Asta evită decalarea indecșilor în timpul buclei
        RebuildInventoryDictionary();
        UpdateDebugList();

        return remain <= 0;
    }



    public bool DecreaseItemAtSlot(InventorySlot slot, int amount)
    {
        if (slot == null || slot.itemData == null) return false;

        string itemName = slot.itemData.itemName;
        int remain = slot.DecreaseCount(amount);

        // Dacă au mai rămas de scăzut (de exemplu, ai dat Drop la 10 dintr-un slot de 5)
        if (remain > 0)
        {
            // Continuăm scăderea din restul inventarului pentru acel item
            return DecreaseItem(itemName, remain);
        }

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
        }

        slot.Clear(); // Slotul rămâne în allSlots, dar e marcat ca gol
        current_slots--;

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
        bool removed = DecreaseItemAtSlot(slot, amount);
        
        if (removed)
            Debug.Log($"<color=green>[DropItem] Succes: {amount}x {nameToDecrease} eliminate și create fizic.</color>");

        return true;
    }

    public bool AddExistingSlot(InventorySlot incomingSlot)
    {
        if (incomingSlot == null || incomingSlot.itemData == null) return false;

        // 1. Căutăm un slot REALE și GOL în lista noastră fixă de 6
        InventorySlot persistentSlot = allSlots.Find(s => s.itemData == null);

        if (persistentSlot != null)
        {
            // 2. TRANSFERĂM DATELE în slotul care există deja în listă
            persistentSlot.CopyFrom(incomingSlot);
            
            // Siguranță pentru count
            if (persistentSlot.count <= 0) persistentSlot.count = 1;

            // 3. Actualizăm dicționarul intern (pentru căutări rapide)
            string key = persistentSlot.itemData.itemName;
            if (!inventory.ContainsKey(key)) inventory[key] = new List<InventorySlot>();
            inventory[key].Add(persistentSlot);
            
            current_slots++; // Opțional, dacă mai folosești acest contor

            UpdateDebugList();
            Debug.Log($"✅ {persistentSlot.itemData.itemName} a ocupat slotul persistent {persistentSlot.slotIndex}.");
            return true;
        }

        Debug.LogWarning("Inventar plin! Nu am găsit niciun slot persistent gol.");
        return false;
    }


    public void SwapSlots(InventorySlot source, InventorySlot target)
    {
        if (source == null || target == null || source == target) return;


        bool sourceCanAcceptTarget = source.CanAcceptItem(target.itemData);
        bool targetCanAcceptSource = target.CanAcceptItem(source.itemData);

        if (!sourceCanAcceptTarget || !targetCanAcceptSource)
        {
            Debug.LogWarning($"[InventoryManager] Swap respins! Incompatibilitate între {source.slotType} și {target.slotType}");
            return; 
        }

        // 1. SALVĂM valorile individuale (nu obiectul cu totul)
        Item tItem = source.itemData;
        Sprite tIcon = source.icon;
        int tCount = source.count;
        int tMax = source.max_count;
        ItemState tState = source.state;

        // 2. Sursa ia datele de la Target (Sursa se schimbă acum)
        source.CopyFrom(target);

        // 3. Target ia valorile salvate anterior în variabilele temporare
        target.CopyFromValues(tItem, tIcon, tCount, tMax, tState);

        RebuildInventoryDictionary();
        UpdateDebugList();
    }


    // O metodă ajutătoare pentru a re-sincroniza dicționarul rapid după un Swap
    public  void RebuildInventoryDictionary()
    {
        inventory.Clear();
        current_slots = 0;

        foreach (var slot in allSlots)
        {
            // 1. Verificare de siguranță: Dacă un slot are date dar count e 0, îl golim forțat
            if (slot.itemData != null && slot.count <= 0)
            {
                slot.Clear();
            }

            // 2. Dacă slotul este valid (are item), îl adăugăm în dicționar
            if (slot.itemData != null)
            {
                string key = slot.itemData.itemName;
                
                if (!inventory.ContainsKey(key))
                {
                    inventory[key] = new List<InventorySlot>();
                }
                
                inventory[key].Add(slot);
                current_slots++;
            }
        }
        
        Debug.Log($"<color=yellow>[Inventory] Dicționar actualizat. Sloturi active: {current_slots}</color>");
    }

    // O variantă mai precisă pentru UI Toolkit unde știm indexul destinație:
    public void MoveSlotToIndex(InventorySlot source, int newIndex)
    {
        if (source == null) return;

        // Verificăm dacă există deja cineva la indexul nou
        InventorySlot occupant = allSlots.Find(s => s.slotIndex == newIndex);

        if (occupant != null)
        {
            // Swap de indecși
            occupant.slotIndex = source.slotIndex;
            source.slotIndex = newIndex;
        }
        else
        {
            // Mutare simplă pe loc gol
            source.slotIndex = newIndex;
        }

        UpdateDebugList();
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
        current_slots = 0;
        foreach(var slot in allSlots)
        {
            slot.Clear(); // Golește datele, nu distruge containerul!
        }
        UpdateDebugList();
    }
}

// ============================================================================

[System.Serializable]
public class ItemState
{
    public float currentDurability;
}


public enum SlotType 
{ 
    General,    // Acceptă orice (Rucsac)
    Tool,       // Doar unelte/arme
    Head,       // Doar căști
    Chest,      // Doar armură corp
    // QuickSlot   // Sloturi de acces rapid
}

[System.Serializable]
public class InventorySlot
{
    public int slotIndex;
    public event Action<InventorySlot> OnSlotChanged;
    public Item itemData;
    public Sprite icon;
    public int count = 0;
    public int max_count;
    public SlotType slotType;
    private InventoryManager manager => InventoryManager.Instance;
    // folosita doar pentru tool
    public ItemState state;
    // Proprietate ajutătoare pentru acces facil la ToolItem (dacă este cazul)
    public ToolItem ToolItemData => itemData as ToolItem;
    public string instanceID;

    public void GenerateID()
    {
        if (string.IsNullOrEmpty(instanceID))
            instanceID = System.Guid.NewGuid().ToString();
    }

    public InventorySlot(SlotType type, Item data, int index)
    {
        this.slotType = type; // Stabilim tipul slotului imediat
        this.slotIndex = index;
        GenerateID(); // Generăm ID-ul la creare

        // DACĂ SLOTUL ESTE INIȚIALIZAT GOL
        if (data == null)
        {
            itemData = null;
            icon = null;
            count = 0;
            max_count = 0;
            state = null;
            return; 
        }

        // DACĂ ARE DATE (pentru adăugare normală sau încărcare)
        itemData = data;
        icon = data.icon;
        max_count = data.stackSize;
        
        // Dacă e un ToolItem, îi inițializăm starea (durabilitatea)
        InitializeState(data);
    }

    public bool CanAcceptItem(Item item)
    {
        if (item == null) return true; // Putem goli orice slot oricând

        if (slotType == SlotType.Tool)
        {
            // Verificăm dacă obiectul este de tip ToolItem (sau o clasă care îl moștenește)
            return item is ToolItem;
        }

        // Slotul General acceptă orice (Item simplu, ToolItem, etc.)
        return true;
    }

    public void SetItem(Item data)
    {
        if (data == null) return;

        this.itemData = data;
        this.icon = data.icon;
        this.max_count = data.stackSize;

        // Inițializăm durabilitatea dacă e unealtă
        if (data.stackSize == 1 && data.maxDurability > 0)
        {
            state = new ItemState { currentDurability = data.maxDurability };
        }

        OnSlotChanged?.Invoke(this);
    }

    public void Clear()
    {
        this.itemData = null;
        this.icon = null;
        this.count = 0;
        this.state = null;

        OnSlotChanged?.Invoke(this);
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

    public void CopyFrom(InventorySlot other)
    {
        this.itemData = other.itemData;
        this.icon = other.icon;
        this.count = other.count;
        this.max_count = other.max_count;
        this.state = other.state;

        OnSlotChanged?.Invoke(this);
    }


    public void CopyFromValues(Item data, Sprite icon, int count, int maxCount, ItemState state)
    {
        this.itemData = data;
        this.icon = icon;
        this.count = count;
        this.max_count = maxCount;
        this.state = state;

        OnSlotChanged?.Invoke(this);
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

        int amountToDrop = count;

        // Trimitem tot ce avem în acest slot către metoda DropItem
        // manager.DropItem se ocupă deja de instanțierea vizuală și de eliminarea din liste
        manager.DropItem(this, amountToDrop);
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
        OnSlotChanged?.Invoke(this);
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
        if (itemData == null) return;

        // 1. Verificăm dacă este o unealtă (ToolItem)
        if (itemData is ToolItem)
        {
            // Trimitem cererea de echipare
            GlobalEvents.RequestSlotEquip(this);
            OnSlotChanged?.Invoke(this);

            // Verificăm dacă transferul a reușit (dacă EquippedManager are acum datele noastre)
            if (EquippedManager.Instance.GetEquippedSlot().itemData == this.itemData)
            {
                this.Clear();
            }
        }
        // 2. Altfel, tratăm item-ul ca pe un consumabil normal
        else
        {
            // Apelăm metoda Use() definită în ScriptableObject (Item)
            itemData.Use();
            OnSlotChanged?.Invoke(this);
        }
    }

    public int DecreaseCount(int amount)
    {
        int toRemove = Mathf.Min(amount, count);
        count -= toRemove;
        OnSlotChanged?.Invoke(this);

        if (count <= 0)
        {
            // Foarte important: Doar ștergem datele, nu apelăm RemoveSlot din manager!
            Clear();
        }

        return amount - toRemove;
    }


}


[System.Serializable]
public class QuickSlot
{
    public int hotbarIndex; 
    public string targetItemName = ""; 

    public bool IsAssigned => !string.IsNullOrEmpty(targetItemName);

    // Această proprietate returnează suma totală a itemelor din inventar cu acest nume
    public int TotalCount 
    {
        get 
        {
            if (!IsAssigned) return 0;

            // 1. Căutăm în inventar (folosind metoda ta de Dicționar/Total)
            int count = InventoryManager.Instance.GetTotalItemCount(targetItemName);
            
            // 2. Adăugăm și dacă este în mână
            var equipped = EquippedManager.Instance.GetEquippedSlot();
            if (equipped != null && equipped.itemData != null && equipped.itemData.itemName == targetItemName)
            {
                count += equipped.count;
            }

            return count;
        }
    }

    public QuickSlot(int index) => hotbarIndex = index;

    public void Assign(string itemName) => targetItemName = itemName;
    public void Unassign() => targetItemName = "";
}

