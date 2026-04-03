using UnityEngine;
using System;
using System.Linq; 

public class EquippedManager : MonoBehaviour
{
    // Singleton Setup
    public static EquippedManager Instance { get; private set; }
    
    // Semnalul Public de Stare (pe baza slotului) asd
    public event Action<InventorySlot> OnSlotEquippedStateChanged;

    // Slotul de Echipare (Instanța cu durabilitate)
    private InventorySlot currentEquippedSlot = null;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            currentEquippedSlot = new InventorySlot(null, 999);
            currentEquippedSlot.OnSlotChanged += HandleSlotContentChanged;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        // Abonare la evenimentul bazat pe Slot
        GlobalEvents.OnSlotEquipRequested += HandleEquipSlotRequest; 
    }

    private void OnDisable()
    {
        GlobalEvents.OnSlotEquipRequested -= HandleEquipSlotRequest; 
    }

    // =================================================================
    // ACCES PUBLIC
    // =================================================================

    public InventorySlot GetEquippedSlot()
    {
        return currentEquippedSlot;
    }
    
    public bool IsEquippedSlotEmpty()
    {
        // Verificăm conținutul, nu containerul
        return currentEquippedSlot == null || currentEquippedSlot.itemData == null;
    }
    

    private void HandleSlotContentChanged(InventorySlot slot)
    {
        if (slot.itemData == null)
        {
            // 1. Vizual: Deconectăm modelul 3D dacă slotul s-a golit
            // ToolController tc = FindToolControllerInScene();
            // tc?.ClearVisual();

            Debug.Log("[EquippedManager] Slotul s-a golit. Curățăm vizualul.");
        }

        // 2. Notificăm UI-ul (InventoryPanelController va primi slotul gol și va ascunde iconița)
        OnSlotEquippedStateChanged?.Invoke(slot.itemData == null ? null : slot);
    }
    
    

    private void HandleEquipSlotRequest(InventorySlot slotFromInventory)
    {
        // 1. Verificări de bază (null sau gol)
        if (slotFromInventory == null || slotFromInventory.itemData == null) return;

        // 2. LOGICA DE TOGGLE: Dacă slotul primit este deja cel din mână, îl dechipăm
        if (slotFromInventory == currentEquippedSlot)
        {
            UnequipTool();
            return;
        }

        // 3. SALVĂM DATELE DIN MÂNĂ (pentru Swap)
        Item tempItem = currentEquippedSlot.itemData;
        int tempCount = currentEquippedSlot.count;
        ItemState tempState = currentEquippedSlot.state;

        // 4. TRANSFERUL DE DATE (Deep Copy)
        // Punem în mână ce a venit din inventar
        currentEquippedSlot.CopyFrom(slotFromInventory);

        // Punem în inventar ce aveam înainte în mână 
        // (Dacă mâna era goală, slotul din inventar va deveni acum gol prin CopyFromValues)
        slotFromInventory.CopyFromValues(tempItem, tempItem?.icon, tempCount, tempItem?.stackSize ?? 1, tempState);

        // 5. ASIGURARE DATE (Minim 1 item dacă slotul nu e null)
        if (currentEquippedSlot.itemData != null && currentEquippedSlot.count <= 0)
        {
            currentEquippedSlot.count = 1;
        }

        // 6. SINCRONIZARE (Refacem dicționarul pentru a evita "fantomele")
        InventoryManager.Instance.RebuildInventoryDictionary();

        // 7. NOTIFICARE UI
        OnSlotEquippedStateChanged?.Invoke(currentEquippedSlot);
    }
    
    public void ClearEquippedSlot()
    {
        if (currentEquippedSlot != null)
        {
            currentEquippedSlot.ToolItemData?.Unequip();
            currentEquippedSlot.Clear(); // Folosește metoda Clear din InventorySlot
        }

        OnSlotEquippedStateChanged?.Invoke(null);
    }

        /// <summary>
        /// Încearcă să returneze slotul echipat înapoi în inventarul stocabil.
        /// </summary>
    public void UnequipTool()
    {
        if (currentEquippedSlot == null) return;

        bool success = InventoryManager.Instance.AddItemFromEquipped(currentEquippedSlot);

        if (success)
        {
            currentEquippedSlot.ToolItemData?.Unequip();
            
            // REPARARE: Nu facem currentEquippedSlot = null;
            // Golește datele, dar păstrează obiectul InventorySlot viu
            currentEquippedSlot.Clear(); 

            OnSlotEquippedStateChanged?.Invoke(null);
            Debug.Log("<color=green>[EquippedManager] Unealta a fost pusă înapoi în rucsac.</color>");
        }
        else
        {
            Debug.LogError("⚠️ Inventar plin!");
        }
    }

    public bool DropEquippedTool(int amount = 1)
    {
        // Verificăm dacă avem item în slot, nu dacă slotul e null
        if (currentEquippedSlot == null || currentEquippedSlot.itemData == null)
        {
            Debug.LogWarning("[EquippedManager] Nu este nicio unealtă echipată de aruncat.");
            return false;
        }

        InventorySlot slotToDrop = currentEquippedSlot; // Referință temporară

        // ... (restul logicii tale de Controller) ...

        // REPARARE: În loc de null, golim slotul persistent
        bool dropSuccess = InventoryManager.Instance.DropItem(slotToDrop, amount);
        
        if (dropSuccess)
        {
            // Golește slotul, dar păstrează instanța pentru viitoarele echipări
            currentEquippedSlot.Clear(); 
            OnSlotEquippedStateChanged?.Invoke(null);
            return true;
        }
        return false;
    }

    public void DestroyEquippedToolBySlot(InventorySlot slotToDestroy)
    {
        // Verificare sănătoasă
        if (currentEquippedSlot == null || currentEquippedSlot.itemData == null) return;

        // ... (logică sunet/controller) ...

        // REPARARE: Golește, nu șterge instanța
        currentEquippedSlot.Clear();
        OnSlotEquippedStateChanged?.Invoke(null);

        Debug.Log($"[EquippedManager] Unealta a fost DISTRUSĂ.");
    }

    private ToolController FindToolControllerInScene()
    {
        // Metoda ajutătoare
        return FindObjectOfType<ToolController>();
    }
}