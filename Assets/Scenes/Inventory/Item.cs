using UnityEngine;

// Permite crearea de obiecte Item direct din meniul de Assets/Create
[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class Item : ScriptableObject
{
    // Proprietăți generale și obligatorii
    [Header("Item Data")]
    public string itemName = "New Item";
    public Sprite icon;             // Imaginea 2D a obiectului pentru UI
    public string description = "O descriere scurtă a obiectului.";
    public int stackSize = 1;       // Câte obiecte se pot stoca într-un slot (1 = nu se stochează)
    public int amount = 1;
    public float maxDurability = 1f;
    public float durabilityLossPerUse = 0f;

    // O metodă virtuală pe care clasele copil o pot suprascrie (override)
    // Utila pentru logica de utilizare (ex: "Use" pe o poțiune sau "Equip" pe o armă)
    public virtual void Use()
    {
        Debug.Log("Folosind: " + itemName + ".");

        if (InventoryManager.Instance == null)
        {
            Debug.LogError("Managerul de Inventar nu este instanțiat. Nu se poate elimina obiectul.");
            return;
        }

        // --- Logica de Consum Globală ---
        int amountToConsume = 1;

        // Folosim metoda 'DecreaseItem' pentru a consuma 1 unitate.
        bool consumed = InventoryManager.Instance.DecreaseItem(itemName, amountToConsume);

        if (consumed)
        {
            Debug.Log($"[Inventar] {itemName} x{amountToConsume} a fost consumat.");
            // **Aici se adaugă logica unică a itemului (ex: vindecare, buff).**
        }
        else
        {
            // Managerul afișează deja warning-ul specific.
            Debug.LogWarning($"[Inventar] Nu s-a putut folosi/consuma {itemName}. Acțiunea eșuează.");
        }
    }
}