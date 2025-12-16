using UnityEngine;

public abstract class AbstractActionLogicSO : ScriptableObject
{
    // --- METODA ABSTRACTĂ (Logica Unică) ---
    // Această metodă are nevoie de 'initiator' pentru a aplica efectul pe obiect.
    public abstract bool ExecuteAction(ActionRecipeSO recipe, GameObject initiator);
    [System.NonSerialized] 
    protected bool isFinished = false;
    public bool IsProgressAction = false;
    public bool IsFinished()
    {
        return isFinished;
    }
    
    // --- METODE VIRTUALE (Logică de Bază Comună - Verificare) ---

    // NU are nevoie de 'initiator', folosește doar Singleton-ul InventoryManager.Instance
    public virtual bool CheckCanExecute(ActionRecipeSO actionRecipe)
    {
        if (actionRecipe == null || actionRecipe.requiredItems == null)
        {
            return true;
        }

        if (InventoryManager.Instance == null)
        {
            Debug.LogError("InventoryManager nu este instanțiat. Nu se poate verifica costul acțiunii.");
            return false;
        }

        foreach (var itemCost in actionRecipe.requiredItems)
        {
            if (itemCost.requiredItem == null) continue;

            string itemName = itemCost.requiredItem.itemName;
            int requiredAmount = itemCost.amount;

            if (string.IsNullOrEmpty(itemName)) continue;

            int totalCountInInventory = InventoryManager.Instance.GetTotalItemCount(itemName);
            if (totalCountInInventory < requiredAmount)
            {
                return false;
            }
        }

        return true;
    }
    
    // --- METODE VIRTUALE (Logică de Bază Comună - Consum) ---

    // NU are nevoie de 'initiator', folosește doar Singleton-ul InventoryManager.Instance
    public virtual bool ConsumeRequiredResources(ActionRecipeSO actionRecipe)
    {
        if (actionRecipe == null || actionRecipe.requiredItems == null || InventoryManager.Instance == null)
        {
            Debug.LogWarning("Nu s-au putut consuma resursele: actionRecipe sau InventoryManager lipsesc.");
            return false;
        }
        
        // Nu consuma dacă nu trece de verificare (gard de siguranță)
        if (!CheckCanExecute(actionRecipe))
        {
             return false;
        }


        bool success = true;
        
        foreach (var itemCost in actionRecipe.requiredItems)
        {
            Item costItem = itemCost.requiredItem;
            int costAmount = itemCost.amount;

            if (costItem != null && !string.IsNullOrEmpty(costItem.itemName))
            {
                bool itemConsumed = InventoryManager.Instance.DecreaseItem(costItem.itemName, costAmount);
                
                if (itemConsumed)
                {
                    Debug.Log($"🔥 Consumat: {costItem.itemName} x{costAmount}");
                }
                else
                {
                    Debug.LogError($"Eroare critică: Nu s-a putut consuma {costItem.itemName}.");
                    success = false;
                }
            }
        }
        return success;
    }
}