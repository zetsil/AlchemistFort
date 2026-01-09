using System.Collections.Generic;
using UnityEngine;
using System.Linq; // Necesar pentru funcții LINQ (All)

// Componenta atașată la obiectul care este în construcție (LumberPilePrefab)
public class BuildingProgressComponent : MonoBehaviour
{
    // --- Configurare ---
    [Header("Configurare Construcție")]
    [Tooltip("Lista ordonată a tuturor rețetelor (etapelor) necesare pentru finalizare.")]
    public List<ActionRecipeSO> requiredSteps = new List<ActionRecipeSO>();

    [Tooltip("Prefab-ul final care înlocuiește acest obiect la finalizare. (Clădirea finală cu acțiuni complete)")]
    public GameObject finalBuildingPrefab;
    
    // --- Starea Curentă (Runtime) ---
    // Dicționar pentru a urmări care rețete (etape) au fost deja completate.
    private Dictionary<ActionRecipeSO, bool> completionStatus = new Dictionary<ActionRecipeSO, bool>();

    private void Awake()
    {
        // Inițializăm starea: toate etapele sunt FALSE la început.
        InitializeProgressStatus();
    }
    
    // Metodă de inițializare a dicționarului
    private void InitializeProgressStatus()
    {
        completionStatus.Clear();
        if (requiredSteps == null || requiredSteps.Count == 0)
        {
            Debug.LogError($"Componenta BuildingProgressComponent de pe '{gameObject.name}' nu are etape necesare configurate!");
            return;
        }

        foreach (var step in requiredSteps)
        {
            // Adaugă doar rețetele care nu sunt deja în dicționar
            if (!completionStatus.ContainsKey(step))
            {
                completionStatus.Add(step, false);
            }
        }
    }
    

    public bool IsRecipeCompleted(ActionRecipeSO recipe)
    {
        // Verificăm dacă rețeta este în dicționar și dacă valoarea sa este True.
        if (completionStatus.TryGetValue(recipe, out bool isCompleted))
        {
            return isCompleted;
        }
        // Dacă rețeta nu face parte din pașii necesari, o considerăm nefinalizată.
        return false;
    }

    // Această metodă este apelată de LogicBuildingProgressSO după consumarea resurselor.
    public void AdvanceProgress(ActionRecipeSO completedRecipe)
    {
        // 1. Verificare: Asigură-te că rețeta este una validă și necesară
        if (completedRecipe == null || !requiredSteps.Contains(completedRecipe))
        {
            Debug.LogWarning($"Rețeta '{completedRecipe?.actionName}' nu face parte din pașii necesari pentru construcția curentă de pe '{gameObject.name}'.");
            return;
        }

        // 2. Marcare: Marcam rețeta ca fiind completată, DOAR dacă nu era deja True.
        if (completionStatus.ContainsKey(completedRecipe) && completionStatus[completedRecipe] == false)
        {
            completionStatus[completedRecipe] = true;

            // Debugging
            int completedCount = completionStatus.Count(pair => pair.Value);
            Debug.Log($"✅ Etapă Construcție Finalizată: {completedRecipe.actionName}. Progres Total: {completedCount} / {requiredSteps.Count}");

            CheckIfBuildingIsComplete();
        }
    }
    
    // Metodă de finalizare
    private void CheckIfBuildingIsComplete()
    {
        // Verificăm dacă TOATE valorile din dicționar sunt true.
        // Folosim LINQ: Returnează TRUE dacă toate perechile au Value = true.
        bool allCompleted = completionStatus.All(pair => pair.Value);
        
        if (allCompleted)
        {
            CompleteBuilding();
        }
    }

    private void CompleteBuilding()
    {
        Debug.Log($"🎉 Construcție finalizată pentru {gameObject.name}! Se înlocuiește Prefab-ul.");




        if (finalBuildingPrefab != null)
        {
            // 1. Instanțiază clădirea finală la poziția și rotația obiectului temporar.
            GameObject newBuilding = Instantiate(finalBuildingPrefab, transform.position, transform.rotation);

            // 2. IMPORTANT: Marchează obiectul ca fiind spawnat la runtime pentru SaveManager

            // 3. Înregistrează clădirea GHOST (cea curentă) ca fiind distrusă în SaveManager
            // Presupunând că obiectul ghost are un uniqueID de la editor
            WorldEntityState ghostState = GetComponent<WorldEntityState>();
            if (ghostState != null && SaveManager.Instance != null)
            {
                SaveManager.Instance.RegisterDestroyedWorldItem(ghostState.uniqueID);
            }

            WorldEntityState state = newBuilding.GetComponent<WorldEntityState>();
            if (state != null)
            {
                state.isSpawnedAtRuntime = true;
                // Opțional: Dacă vrei să generezi un ID unic imediat
                state.uniqueID = "Built_" + ghostState.uniqueID;
            }
        
        }
        else
        {
            Debug.LogError($"Prefab-ul final nu este setat pentru obiectul '{gameObject.name}'!");
        }
        
        // --- 2. Distruge obiectul temporar de construcție ---
        Destroy(gameObject);
    }
}