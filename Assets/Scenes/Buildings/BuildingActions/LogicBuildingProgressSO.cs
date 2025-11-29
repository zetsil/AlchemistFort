using UnityEngine;

// [CreateAssetMenu] este esențial pentru a crea instanțe din acest SO în Editor.
[CreateAssetMenu(fileName = "Logic_BuildingProgress", menuName = "Building/Action Logic/Building Progress Logic")]
public class LogicBuildingProgressSO : AbstractActionLogicSO
{
    public override bool CheckCanExecute(ActionRecipeSO actionRecipe)
    {

        if (!base.CheckCanExecute(actionRecipe))
        {
            // Dacă verificarea de bază eșuează (lipsă iteme necesare), întoarcem false imediat.
            return false;
        }


        return !isFinished;
        
    }


    // --- METODA ABSTRACTĂ (Logica Unică de Execuție) ---
    public override bool ExecuteAction(ActionRecipeSO recipe, GameObject initiator)
    {
        // Resetăm starea internă la fiecare execuție (dacă SO-ul este reutilizat)
        // sau lăsăm-o pe ultima stare (dacă SO-ul reprezintă o singură acțiune).
        // Alegem să o resetăm sau să o ignorăm momentan, deoarece logica reală e pe componentă.

        if (initiator == null)
        {
            Debug.LogError("Initiator-ul este NULL. Nu se poate progresa construcția.");
            // În caz de eroare, nu putem spune că s-a finalizat cu succes.
            return false;
        }

        // 1. Căutăm componenta care gestionează starea clădirii pe obiectul inițiator.
        BuildingProgressComponent progressComponent = initiator.GetComponent<BuildingProgressComponent>();
        
        if (progressComponent == null)
        {
             Debug.LogError($"Obiectul '{initiator.name}' nu are BuildingProgressComponent.");
             return false;
        }

        // 3. Aplicăm logica unică: avansăm progresul clădirii.
        // Presupunem că rețeta curentă (ActionRecipeSO) definește o etapă de construcție.
        progressComponent.AdvanceProgress(recipe);
        

        // Logica de succes va fi gestionată de BuildingProgressComponent la finalizare.
        return true;
    }
}