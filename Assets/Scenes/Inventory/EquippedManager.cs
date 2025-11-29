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

    // =================================================================
    // LOGICĂ ECHIPARE (APELATĂ PRIN EVENT)
    // =================================================================

    /// <summary>
    /// Gestionează cererea de echipare primită prin evenimentul GlobalEvents.OnSlotEquipRequested.
    /// </summary>
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

    /// <summary>
    /// Încearcă să returneze slotul echipat înapoi în inventarul stocabil.
    /// </summary>
    public void UnequipTool()
    {
        if (currentEquippedSlot == null) return;
        
        InventorySlot slotToUnequip = currentEquippedSlot;
        
        // 1. Încearcă să returnezi slotul înapoi în inventar
        bool success = InventoryManager.Instance.AddItem(slotToUnequip.itemData); // trebuie schimbat

        if (success)
        {
            // 2. Deconectare și Curățare
            ToolController equippedController = FindToolControllerInScene(); 
            if (equippedController != null)
            {
                // equippedController.equippedSlot = null;
            }

            slotToUnequip.ToolItemData?.Unequip(); 
            
            currentEquippedSlot = null;
            
            // Anunță dezechiparea
            OnSlotEquippedStateChanged?.Invoke(null);
            
            Debug.Log($"[EquippedManager] Slotul {slotToUnequip.itemData.itemName} a fost dezechipat și returnat.");
        }
        else
        {
            // Dezechipare eșuată (inventar plin) - Slotul rămâne ECHIPAT.
            Debug.LogError($"⚠️ Dezechiparea {slotToUnequip.itemData.itemName} a eșuat! Inventarul stocabil este plin.");
        }
    }

    private ToolController FindToolControllerInScene()
    {
        // Metoda ajutătoare
        return FindObjectOfType<ToolController>();
    }
}