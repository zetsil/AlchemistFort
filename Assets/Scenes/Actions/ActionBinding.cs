using UnityEngine;
using System;

// Poate fi o structură sau o clasă simplă (fără MonoBehaviour/ScriptableObject)
[Serializable]
public class ActionBinding
{
    // 1. Reteta: Datele de care are nevoie acțiunea (cost, iconiță, nume etc.)
    public ActionRecipeSO Recipe;

    // 2. Executorul: Logica de care are nevoie acțiunea (executarea)
    // Acesta ar trebui să fie instanțiat și configurat în ActionBinder
    public AbstractActionExecutor Executor; 

    public ActionBinding(ActionRecipeSO recipe, AbstractActionExecutor executor)
    {
        Recipe = recipe;
        Executor = executor;
    }
}