using UnityEngine;

public abstract class AbstractActionExecutor : MonoBehaviour
{
    // Reteta SO care defineste costul si efectul acestei acțiuni.
    public ActionRecipeSO actionRecipe; 

    // Metoda de bază pentru a verifica doar resursele (se bazează pe inventarul global)
    public virtual bool CanExecuteResourceCheck()
    {   
        // 1. Verificare pre-condiții
        if (actionRecipe == null || actionRecipe.requiredItems == null)
        {
            // O acțiune fără cost este întotdeauna executabilă (din punct de vedere al resurselor)
            return true;
        }

        if (InventoryManager.Instance == null)
        {
            Debug.LogError("InventoryManager nu este instanțiat. Nu se poate verifica costul acțiunii.");
            return false;
        }

        // 2. Iterare prin costurile acțiunii
        foreach (var recipe in actionRecipe.requiredItems)
        {
            // Verifică dacă itemul requiredItem este null înainte de a accesa proprietăți
            if (recipe.requiredItem == null || string.IsNullOrEmpty(recipe.requiredItem.itemName)) continue;

            // Obține stocul total al itemului necesar din inventar
            int totalCountInInventory = InventoryManager.Instance.GetTotalItemCount(recipe.requiredItem.itemName);

            // Compară stocul cu cantitatea necesară
            // CORECTAT: Folosește recipe.amount (cantitatea din ItemCost), nu recipe.requiredItem.amount
            if (totalCountInInventory < recipe.amount)
            {
                // Un item necesar lipsește sau nu este în cantitate suficientă
                Debug.Log($"❌ Nu se poate executa acțiunea '{actionRecipe.actionName}'. Lipsește {recipe.requiredItem.itemName} ({totalCountInInventory}/{recipe.amount}).");
                return false; 
            }
        }

        // 3. Toate resursele necesare au fost găsite
        // CORECTAT: Schimbat return false la return true.
        return true;
    }


    public void ConsumeRequiredResources()
    {
        // Verificări de siguranță înainte de consum
        if (actionRecipe != null && actionRecipe.requiredItems != null && InventoryManager.Instance != null)
        {
            // Iterăm prin lista de ItemCost definită în rețetă
            foreach (var itemCost in actionRecipe.requiredItems)
            {
                Item costItem = itemCost.requiredItem;
                int costAmount = itemCost.amount;

                if (costItem != null && !string.IsNullOrEmpty(costItem.itemName))
                {
                    // Apel la InventoryManager pentru a scădea cantitatea
                    InventoryManager.Instance.DecreaseItem(costItem.itemName, costAmount);
                    Debug.Log($"🔥 Consumat: {costItem.itemName} x{costAmount}");
                }
            }
        }
        else
        {
            Debug.LogWarning("Nu s-au putut consuma resursele: actionRecipe sau InventoryManager lipsesc.");
        }
    }

    // Metoda cheie: Logică de validare TOTALĂ (Acum fără parametru)
    public abstract bool CanExecuteAction();

    // Metoda cheie: Executarea acțiunii (Acum fără parametru)
    public abstract void ExecuteAction();
}