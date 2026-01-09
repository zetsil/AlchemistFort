using UnityEngine;
using System;

// Definim Enumerarea (Enum) pentru a clasifica tipurile de unelte
public enum ToolType
{
    None,
    Axe,        // Topor (pentru tăiat lemne)
    Pickaxe,    // Târnăcop (pentru spart piatră/minereu)
    Shovel,     // Lopată (pentru săpat pământ)
    Sword,      // Sabie (pentru luptă)
    Claw,
    Shield,
    // Adaugă orice alte tipuri necesare
}

// Permite crearea de obiecte Tool Item direct din meniul de Assets/Create
[CreateAssetMenu(fileName = "New Tool", menuName = "Inventory/Tool Item")]
public class ToolItem : Item // Moștenește din clasa ta Item ScriptableObject
{
    [Header("Tool Classification")]
    [Tooltip("Specifică tipul uneltei pentru logica de crafting/colectare.")]
    public ToolType toolCategory = ToolType.None; // Tipul uneltei

    [Header("Tool Specific Data")]
    public float attackDamage = 10f;


    public override void Use()
    {
        return; 
        
        if (this is ToolItem tool)
        {

            GlobalEvents.RequestEquip(tool);

            Debug.Log($"[ToolItem] Cerere de echipare trimisă pentru {itemName} ({toolCategory}).");
        }
        else
        {
            // Dacă nu e un ToolItem, poate executa logica părinte (consum etc.)
            base.Use();
        }
        
        // **ATENȚIE:** Uneltele echipabile nu sunt de obicei consumate (nu apelăm base.Use() aici).
    }

    // O metodă ajutătoare care poate fi apelată de EquippedManager la dezechipare
    public void Unequip()
    {
        Debug.Log($"[Echipare] Unealta '{itemName}' a fost dezechipată. Durabilitate rămasă: {maxDurability}.");
        // Aici poți adăuga orice logică de curățare specifică uneltei.
    }
}