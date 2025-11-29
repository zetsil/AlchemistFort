using UnityEngine;
using System.Collections.Generic;

// ==============================================================================
// STRUCTURI NECESARE (Definite Local în acest fișier)
// ==============================================================================

[System.Serializable]
public struct ItemDrop
{
    // Presupunem că Item este o clasă/ScriptableObject existentă
    public Item item; 
    
    // Șansa de drop (între 0 și 1)
    [Range(0f, 1f)] 
    public float dropChance; 
    
    // Câte iteme de acest tip pot fi dropate
    [Range(1, 99)]
    public int minQuantity;
    [Range(1, 99)]
    public int maxQuantity; 
}

[System.Serializable]
public struct ToolEffectiveness
{
    // Presupunem că ToolType este un enum existent (ex: Axe, Pickaxe, Shovel)
    public ToolType toolType; 

    [Tooltip("Multiplicatorul de damage. Ex: 2.0x (vulnerabilitate).")]
    [Range(0.01f, 5f)]
    public float damageMultiplier; 
}


// ==============================================================================
// CLASA PRINCIPALĂ: ENTITY DATA
// ==============================================================================

// Adăugăm meniul de creare pentru a putea crea SO-uri direct în Project
[CreateAssetMenu(fileName = "NewEntityData", menuName = "Entity/Entity Data")]
public class EntityData : ScriptableObject
{
    [Header("Statistici Generale")]
    [Tooltip("Viața maximă a acestei entități.")]
    public int maxHealth = 100;


    [Tooltip("Damage-ul de bază pe care această entitate îl aplică altor entități.")]
    [Range(0f, 100f)] // Poți ajusta range-ul în funcție de jocul tău
    public float baseAttackDamage = 1f;

    // --- Eficacitatea Uneltelor ---
    [Header("Tool Effectiveness")]
    [Tooltip("Setează multiplicatorul de damage primit în funcție de tipul de unealtă folosită.")]
    public List<ToolEffectiveness> toolEffectivenesses;

    // --- Loot Drop ---
    [Header("Loot Drop")]
    [Tooltip("Lista de iteme pe care le poate dropa entitatea și șansa fiecăruia.")]
    public List<ItemDrop> possibleDrops;
}