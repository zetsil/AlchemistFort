using UnityEngine;


public class AddWoodExecutor
 {
//     [Header("Setări Bonfire")]
//     [Tooltip("Cantitatea de timp adăugată de o bucată de lemn în secunde.")]
//     public float timeIncreaseAmount = 8f; 

//     private BonfireTimerManager bonfireManager;

//     void Start()
//     {
//         // Ne asigurăm că executorul este pe același GameObject cu BonfireTimerManager.
//         bonfireManager = GetComponent<BonfireTimerManager>();
//         if (bonfireManager == null)
//         {
//             Debug.LogError($"[{actionRecipe?.actionName ?? "AddWoodExecutor"}] BonfireTimerManager nu a fost găsit pe acest GameObject!");
//             enabled = false;
//         }
//     }


//     public override bool CanExecuteAction()
//     {
//         // 1. Verifică resursele folosind logica din clasa de bază (AbstractActionExecutor).
//         if (!CanExecuteResourceCheck())
//         {
//             return false;
//         }

//         // 2. Verifică pre-condiția specifică: Focul trebuie să fie deja aprins (Nivelul 1).
//         if (bonfireManager != null && bonfireManager.actionUIGenerator.currentActionLevel != 1)
//         {
//             Debug.Log($"❌ Nu se poate executa acțiunea '{actionRecipe?.actionName}'. Focul nu este aprins.");
//             return false;
//         }

//         return true;
//     }


//     public override void ExecuteAction()
//     {
//         if (!CanExecuteAction()) 
//         {
//             Debug.LogWarning($"Nu s-a putut executa '{actionRecipe?.actionName}' (validare eșuată).");
//             return;
//         }

//         // 1. Consumă resursele necesare (lemnul).
//         ConsumeRequiredResources();
        
//         // 2. Adaugă timpul la timer.
//         if (bonfireManager != null)
//         {
//             // Apelăm funcția publică de adăugare timp din manager.
//             bonfireManager.AddTimeToTimer(timeIncreaseAmount);
//         }
        
//         Debug.Log($"✅ Acțiunea '{actionRecipe?.actionName}' finalizată.");
//     }
}