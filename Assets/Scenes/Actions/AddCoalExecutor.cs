using UnityEngine;

/// <summary>
/// Execută acțiunea de a adăuga cărbune, verifică resursele, le consumă
/// și adaugă o durată mare timer-ului Bonfire-ului.
/// </summary>
public class AddCoalExecutor 
{
    // [Header("Setări Bonfire")]
    // [Tooltip("Cantitatea de timp adăugată de o bucată de cărbune (durată mare).")]
    // public float timeIncreaseAmount = 15f; 

    // private BonfireTimerManager bonfireManager;

    // void Start()
    // {
    //     // Ne asigurăm că executorul este pe același GameObject cu BonfireTimerManager.
    //     bonfireManager = GetComponent<BonfireTimerManager>();
    //     if (bonfireManager == null)
    //     {
    //         Debug.LogError($"[{actionRecipe?.actionName ?? "AddCoalExecutor"}] BonfireTimerManager nu a fost găsit pe acest GameObject!");
    //         enabled = false;
    //     }
    // }

    // /// <summary>
    // /// Metoda de validare TOTALĂ: Verifică resursele ȘI starea focului.
    // /// </summary>
    // public override bool CanExecuteAction()
    // {
    //     // 1. Verifică resursele folosind logica din clasa de bază.
    //     if (!CanExecuteResourceCheck())
    //     {
    //         return false;
    //     }

    //     // 2. Verifică pre-condiția specifică: Focul trebuie să fie deja aprins (Nivelul 1).
    //     if (bonfireManager != null && bonfireManager.actionUIGenerator.currentActionLevel != 1)
    //     {
    //         Debug.Log($"❌ Nu se poate executa acțiunea '{actionRecipe?.actionName}'. Focul nu este aprins.");
    //         return false;
    //     }

    //     return true;
    // }

    // /// <summary>
    // /// Metoda de execuție: Consumă resurse și adaugă timpul.
    // /// </summary>
    // public override void ExecuteAction()
    // {
    //     if (!CanExecuteAction()) 
    //     {
    //         Debug.LogWarning($"Nu s-a putut executa '{actionRecipe?.actionName}' (validare eșuată).");
    //         return;
    //     }

    //     // 1. Consumă resursele necesare (cărbunele).
    //     ConsumeRequiredResources();
        
    //     // 2. Adaugă timpul la timer.
    //     if (bonfireManager != null)
    //     {
    //         // Apelăm funcția publică de adăugare timp din manager.
    //         bonfireManager.AddTimeToTimer(timeIncreaseAmount);
    //     }
        
    //     Debug.Log($"✅ Acțiunea '{actionRecipe?.actionName}' finalizată.");
    // }
}