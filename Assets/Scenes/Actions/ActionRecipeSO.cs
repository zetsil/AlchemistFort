using UnityEngine;
using System.Collections.Generic;

// Structura ItemCost rămâne aceeași
[System.Serializable]
public struct ItemCost
{
    public Item requiredItem; 
    public int amount;
}

[CreateAssetMenu(fileName = "NewActionRecipe", menuName = "Building/Action Recipe")]
public class ActionRecipeSO : ScriptableObject
{
    [Header("Action Identity")]
    public string actionName = "Actiune Generica";
    public string description = "Descriere pentru UI";

    // NOUL CÂMP PENTRU REFERINȚĂ LA EXECUTOR
    [Tooltip("Referința la o instanță a Executorului (de pe Bonfire, de ex.) pentru a obține Tipul Clasei (Type).")]
    public AbstractActionLogicSO actionLogic;
    // Puteți trage aici o componentă LightBonfireExecutor, care moștenește AbstractActionExecutor.

    // NOU: Pictograma acțiunii
    [Tooltip("Pictograma afișată pe butonul de acțiune")]
    public Sprite actionIcon;

    [Header("Requirements (Cost)")]
    public List<ItemCost> requiredItems = new List<ItemCost>();

    [Header("Outcomes (Effect)")]
    public float genericValue = 0f;
    public Item resultItem;
    public string targetState = "Completed";
    
    public bool InitializeAction(GameObject initiator)
    {
        // 0. VERIFICARE PREALABILĂ
        if (actionLogic == null)
        {
            Debug.LogError($"Rețeta '{actionName}' nu are Logica de Acțiune (ActionLogic) atașată!");
            return false;
        }

        // 1. VERIFICARE: Poate inițiatorul rula acțiunea?
        if (!actionLogic.CheckCanExecute(this)) 
        {
            // Debug-ul este deja în Logica SO
            return false;
        }

        // 2. CONSUMUL: Consumă resursele
        if (!actionLogic.ConsumeRequiredResources(this))
        {
            Debug.LogError($"Eroare la consumul resurselor pentru '{actionName}'. Acțiunea nu a fost rulată.");
            return false;
        }
        
        // 3. EXECUȚIA: Rularea logicii de bază a acțiunii
        bool success = actionLogic.ExecuteAction(this, initiator);
        
        if (success)
        {
            Debug.Log($"Acțiunea '{actionName}' executată cu succes pe {initiator.name}.");
        }

        return success;
    }
}