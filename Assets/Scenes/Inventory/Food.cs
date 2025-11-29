using UnityEngine;

// Cale de creare: Inventory/Food/Apple
[CreateAssetMenu(fileName = "Food", menuName = "Inventory/Food")]
public class Food : Item // Moștenește clasa de bază Item
{
    [Header("Food Properties")]
    public float healthRestored = 10f;
    
    // Suprascriem metoda Use() pentru a adăuga logica de consum.
    public override void Use()
    {
        // 1. Logica de bază (ex: afișează în consolă)
        base.Use(); 
        Debug.Log("Am mâncat un " + itemName + "! Am restaurat " + healthRestored + " viață.");
        
    }
}