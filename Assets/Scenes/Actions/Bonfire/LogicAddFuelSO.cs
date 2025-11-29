using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Necesită Linq pentru căutare (opțional, dar mai curat)
[System.Serializable]
public struct FuelDurationEntry
{
    [Tooltip("Item-ul care acționează ca și combustibil (ex: Coal, Wood).")]
    public Item fuelItem;

    [Tooltip("Timpul în secunde adăugat per unitate de combustibil.")]
    public float durationPerUnit;
}


[CreateAssetMenu(fileName = "LogicAddFuel", menuName = "Building/Action Logic/Add Fuel")]
public class LogicAddFuelSO : AbstractActionLogicSO
{
    [Header("Mapează Tipul de Combustibil la Durată")]
    [Tooltip("Definițiile duratei pentru fiecare tip de combustibil (Lemn, Cărbune, etc.).")]
    public List<FuelDurationEntry> fuelDurationSettings = new List<FuelDurationEntry>();

    public override bool ExecuteAction(ActionRecipeSO recipe, GameObject initiator)
    {
        BonfireTimerManager timerManager = initiator.GetComponent<BonfireTimerManager>();
        
        if (timerManager == null)
        {
            Debug.LogError($"[LogicAddFuelSO] Lipsește BonfireTimerManager pe {initiator.name}.");
            return false;
        }

        float totalTimeToAdd = CalculateTotalFuelDuration(recipe);

        if (totalTimeToAdd <= 0)
        {
            // Acțiunea este executată (resursele au fost consumate), dar nu adaugă timp.
            Debug.LogWarning("[LogicAddFuelSO] Rețeta nu conține Item-uri definite ca și combustibil. Timp adăugat: 0.");
            return true;
        }
        
        // 1. Adaugă timpul calculat la Timer Manager
        timerManager.AddTimeToTimer(totalTimeToAdd);
        
        Debug.Log($"[LogicAddFuelSO] Combustibil adăugat în total: {totalTimeToAdd:F2}s.");
        
        return true;
    }

    private float CalculateTotalFuelDuration(ActionRecipeSO recipe)
    {
        float totalTime = 0f;

        foreach (var itemCost in recipe.requiredItems)
        {
            // Verifică dacă Item-ul din rețetă este definit ca și combustibil în lista noastră de setări
            FuelDurationEntry? fuelEntry = fuelDurationSettings
                .FirstOrDefault(e => e.fuelItem == itemCost.requiredItem);
            
            // Dacă elementul este combustibil (adică fuelItem nu este null)
            if (fuelEntry.HasValue && fuelEntry.Value.fuelItem != null) 
            {
                // Timp adăugat = Cantitatea necesară * Durata per unitate
                float duration = itemCost.amount * fuelEntry.Value.durationPerUnit;
                totalTime += duration;

                Debug.Log($"[{recipe.actionName}] - {itemCost.requiredItem.itemName} ({itemCost.amount}x) contribuie cu {duration:F2}s.");
            }
        }

        return totalTime;
    }
}