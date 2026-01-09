using UnityEngine;

// [CreateAssetMenu] permite crearea unui Asset din această clasă
[CreateAssetMenu(fileName = "LogicLightBonfire", menuName = "Building/Action Logic/Light Bonfire")]
public class LogicLightBonfireSO : AbstractActionLogicSO
{
    // Date necesare:
    [Header("Bonfire Logic Data")]
    [Tooltip("Nivelul de acțiune UI de setat după succes (probabil 1).")]
    public int successActionLevel = 1;
    
    // --- Implementări Specifice ---

    // Metodă virtuală: Verificări specifice Focului (încă neterminat)
    public override bool CheckCanExecute(ActionRecipeSO recipe)
    {
        // 1. Apelăm verificarea resurselor din clasa părinte (folosind Singleton-ul global)
        if (!base.CheckCanExecute(recipe))
        {
            return false;
        }
        
        return true; 
    }
    
    // (ConsumeRequiredResources nu trebuie suprascris, folosește implementarea părintelui)

    // Metoda abstractă: Logica de joc efectivă
    public override bool ExecuteAction(ActionRecipeSO recipe, GameObject initiator)
    {
        // Această clasă se ocupă de logica care manipulează componenta atașată la 'initiator'.

        // 1. Obține componenta care deține starea și vizualul (Componenta nouă, VIZUALUL)
        BonfireVisuals visuals = initiator.GetComponent<BonfireVisuals>();
        if (visuals == null)
        {
            Debug.LogError($"[LogicLightBonfireSO] Obiectul {initiator.name} nu are componenta BonfireVisuals!");
            return false;
        }
        
        // 2. Logica jocului (Aprinde Focul)
        visuals.isBonfireLit = true; 
        Debug.Log("--- Acțiunea Executată: APRINDE FOCUL! ---");

        // 3. ACTUALIZARE VIZUALĂ: Deleagă sarcina către componenta vizuală
        visuals.fireParticles.SetActive(true);
        visuals.pointLight.SetActive(true);
        Debug.Log("✅ Vizual și Lumină activate.");
        
        // 4. ACTUALIZARE UI (Presupunem că UI Generator este tot pe initiator)
        NewActionUIGenerator uiGenerator = initiator.GetComponent<NewActionUIGenerator>();
        if (uiGenerator != null)
        {
            uiGenerator.SetActionLevel(successActionLevel);
            Debug.Log($"✅ UI level changed to {successActionLevel}.");
        }
        
        return true;
    }
}