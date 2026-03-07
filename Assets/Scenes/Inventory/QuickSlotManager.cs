using UnityEngine;
using System.Collections.Generic;

public class QuickSlotManager : MonoBehaviour
{
    public static QuickSlotManager Instance { get; private set; }

    [Header("Settings")]
    public int numberOfSlots = 4;
    public List<QuickSlot> quickSlots = new List<QuickSlot>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeSlots();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        for (int i = 0; i < numberOfSlots; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                HandleQuickSlotUsage(i);
            }
        }
    }

    private void InitializeSlots()
    {
        quickSlots.Clear();
        for (int i = 0; i < numberOfSlots; i++)
        {
            quickSlots.Add(new QuickSlot(i));
        }
    }

    public void HandleQuickSlotUsage(int index)
    {
        if (index < 0 || index >= quickSlots.Count) return;

        QuickSlot qs = quickSlots[index];
        if (!qs.IsAssigned) return;

        // Verificăm întâi dacă item-ul EXISTĂ undeva (folosind proprietatea ta TotalCount care vede tot)
        if (qs.TotalCount > 0)
        {
            // Căutăm slotul fizic (Prioritate: Mână -> Inventar)
            InventorySlot slotToUse = GetFirstAvailableInventorySlot(qs.targetItemName);

            if (slotToUse != null)
            {
                Debug.Log($"<color=cyan>[Hotbar]</color> Folosim {qs.targetItemName} de pe slotul {index + 1}");
                slotToUse.HandleUse();
                
                // Opțional: Refresh UI în caz că s-a schimbat durabilitatea sau stack-ul
                // InventoryPanelController.Instance.RefreshHotbarVisuals();
            }
        }
        else
        {
            Debug.LogWarning($"<color=red>[Hotbar]</color> {qs.targetItemName} nu mai este disponibil. Curățăm.");
            qs.Unassign();
        }
    }

    private InventorySlot GetFirstAvailableInventorySlot(string itemName)
    {
        // 1. Verificăm în ECHIPAMENT (EquippedManager)
        InventorySlot equipped = EquippedManager.Instance.GetEquippedSlot();
        if (equipped != null && equipped.itemData != null && equipped.itemData.itemName == itemName)
        {
            return equipped;
        }

        // 2. Verificăm în INVENTAR (InventoryManager)
        // Folosim lista 'allSlots' pentru a returna referința fizică la slot
        foreach (var slot in InventoryManager.Instance.allSlots)
        {
            if (slot.itemData != null && slot.itemData.itemName == itemName)
            {
                return slot;
            }
        }

        return null;
    }

    public Sprite GetQuickSlotIcon(int index)
    {
        if (index < 0 || index >= quickSlots.Count) return null;
        string itemName = quickSlots[index].targetItemName;
        if (string.IsNullOrEmpty(itemName)) return null;

        // Folosim aceeași logică de căutare ca să returnăm iconița corectă (mână sau inventar)
        InventorySlot slot = GetFirstAvailableInventorySlot(itemName);
        return slot != null ? slot.icon : null;
    }

    public void AssignToHotbar(string itemName, int hotbarIndex)
    {
        if (hotbarIndex >= 0 && hotbarIndex < quickSlots.Count)
        {
            quickSlots[hotbarIndex].Assign(itemName);
            Debug.Log($"<color=green>[Hotbar]</color> Asignat: {itemName} pe index {hotbarIndex}");
        }
    }
}