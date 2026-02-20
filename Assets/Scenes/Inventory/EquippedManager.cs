using UnityEngine;
using System;
using System.Linq; 

public class EquippedManager : MonoBehaviour
{
    // Singleton Setup
    public static EquippedManager Instance { get; private set; }
    
    // Semnalul Public de Stare (pe baza slotului)
    public event Action<InventorySlot> OnSlotEquippedStateChanged;

    // Slotul de Echipare (Instanța cu durabilitate)
    private InventorySlot currentEquippedSlot = null;

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
        return currentEquippedSlot == null;
    }

    private void HandleEquipSlotRequest(InventorySlot slotToEquip)
    {
        // 1. Verificări preliminare
        if (slotToEquip == null || slotToEquip.ToolItemData == null)
        {
            Debug.LogError("Tentativă de echipare slot eșuată: Nu este ToolItem.");
            return;
        }

        // 2. Dezechiparea obiectului curent (dacă există)
        if (currentEquippedSlot != null)
        {
            UnequipTool();

            // Verificăm dacă dezechiparea a reușit (dacă inventarul nu e plin)
            if (currentEquippedSlot != null)
            {
                Debug.LogWarning($"Echipare nouă eșuată: Slotul curent ({currentEquippedSlot.itemData.itemName}) nu a putut fi dezechipat (Inventarul este plin).");
                return;
            }
        }

        // 3. Finalizarea Echipării Noului Slot
        currentEquippedSlot = slotToEquip;

        // CONECTARE CRITICĂ: Trimitem Slotul instanță către ToolController.
        ToolController equippedController = FindToolControllerInScene();
        if (equippedController != null)
        {
            Debug.Log($"[EquippedManager] Conectat Slotul {currentEquippedSlot.itemData.itemName} la ToolController. Durabilitate: {currentEquippedSlot.state.currentDurability:F0}");
        }

        // Emiterea Semnalului
        OnSlotEquippedStateChanged?.Invoke(currentEquippedSlot);

        Debug.Log($"[EquippedManager] Unealta {currentEquippedSlot.itemData.itemName} a fost echipată.");
    }
    
    // Adaugă asta în EquippedManager.cs
    public void ClearEquippedSlot()
    {
        if (currentEquippedSlot != null)
        {
            // Notificăm unealta că a fost dezechipată (pentru a opri animații/sunete)
            currentEquippedSlot.ToolItemData?.Unequip();
        }

        currentEquippedSlot = null;
        
        // Anunțăm UI-ul să golească slotul vizual de echipament
        OnSlotEquippedStateChanged?.Invoke(null);
        
        Debug.Log("<color=cyan>[EquippedManager] Slotul de echipament a fost resetat.</color>");
    }

    /// <summary>
    /// Încearcă să returneze slotul echipat înapoi în inventarul stocabil.
    /// </summary>
    public void UnequipTool()
    {
        if (currentEquippedSlot == null) return;

        // Păstrăm referința locală solidă
        InventorySlot slotToReturn = currentEquippedSlot;

        // IMPORTANT: Nu setăm currentEquippedSlot pe null încă! 
        // Îl ținem până când știm sigur că a ajuns în siguranță în inventar.

        // 1. Încercăm mutarea
        bool success = InventoryManager.Instance.AddExistingSlot(slotToReturn);

        if (success)
        {
            // 2. Curățăm controlerul vizual
            ToolController equippedController = FindToolControllerInScene();
            if (equippedController != null)
            {
                // Opțional: Spune-i controlerului să se oprească din randare
                // equippedController.ClearVisuals(); 
            }

            // 3. Notificăm ScriptableObject-ul (doar date)
            slotToReturn.ToolItemData?.Unequip();

            // 4. ABIA ACUM eliberăm referința din EquippedManager
            currentEquippedSlot = null;

            // 5. Forțăm reîmprospătarea UI-ului pentru a lega noile butoane de noua referință din rucsac
            OnSlotEquippedStateChanged?.Invoke(null);
            
            Debug.Log($"<color=green>[EquippedManager] {slotToReturn.itemData.itemName} a fost transferat cu succes în InventoryManager.</color>");
        }
        else
        {
            Debug.LogError("⚠️ Inventar plin! Unealta rămâne în mână.");
        }
    }


    public bool DropEquippedTool(int amount = 1)
    {
        if (currentEquippedSlot == null)
        {
            Debug.LogWarning("[EquippedManager] Nu este nicio unealtă echipată de aruncat.");
            return false;
        }

        // Preia referința la slot înainte de curățare
        InventorySlot slotToDrop = currentEquippedSlot;

        // 1. Curățarea slotului echipat
        ToolController equippedController = FindToolControllerInScene();
        if (equippedController != null)
        {
            // Deconectează ToolController
        }

        // Curățarea EquippedManager (Dezechiparea)
        currentEquippedSlot = null;
        OnSlotEquippedStateChanged?.Invoke(null);
        
        Debug.Log($"[EquippedManager] Se pregătește aruncarea uneltei '{slotToDrop.itemData.itemName}'.");
        
        // 2. Apelul la logica de aruncare din InventoryManager
        // DropItem va genera obiectul vizual în lume și va scădea count-ul din slotToDrop
        // (Deși count-ul este 1 și va ajunge la 0).
        
        bool dropSuccess = InventoryManager.Instance.DropItem(slotToDrop, amount);
        
        if (dropSuccess)
        {
            Debug.Log($"Unealta Echipată '{slotToDrop.itemData.itemName}' a fost aruncată.");
            return true;
        }
        else
        {
            Debug.LogError("Aruncarea uneltei echipate a eșuat la nivel vizual/locație.");
            return false;
        }
    }


    public void DestroyEquippedToolBySlot(InventorySlot slotToDestroy)
    {
        if (currentEquippedSlot == null || currentEquippedSlot != slotToDestroy)
        {
            Debug.LogWarning($"[EquippedManager] Tentativă de distrugere slot ignorată. Slotul primit ({slotToDestroy.itemData.itemName}) nu este slotul echipat curent.");
            return;
        }

        ToolController equippedController = FindToolControllerInScene();
        if (equippedController != null)
        {
            // Deconectează ToolController de slot
            // Adaugă aici codul specific de deconectare a ToolController-ului
        }

        // 3. Curățarea EquippedManager
        slotToDestroy.ToolItemData?.Unequip(); 
        currentEquippedSlot = null;
        GlobalEvents.TriggerPlaySound("broke");
        
        // 4. Anunță Dezechiparea (pentru UI, etc.)
        OnSlotEquippedStateChanged?.Invoke(null);

        Debug.Log($"[EquippedManager] Unealta echipată '{slotToDestroy.itemData.itemName}' a fost DISTRUSĂ (durabilitate 0).");
        

    }

    private ToolController FindToolControllerInScene()
    {
        // Metoda ajutătoare
        return FindObjectOfType<ToolController>();
    }
}