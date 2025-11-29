using UnityEngine;

[CreateAssetMenu(fileName = "LogicSpawnPrefab", menuName = "Building/Action Logic/Spawn Prefab")]
public class LogicSpawnPrefabSO : AbstractActionLogicSO
{
    public override bool ExecuteAction(ActionRecipeSO recipe, GameObject initiator)
    {
        // 1. Verifică și extrage referința directă la Item
        // 🎯 SCHIMBARE AICI: Stocăm obiectul Item, nu numele său.
        Item itemKey = recipe.resultItem; 

        if (itemKey == null) // Verifică dacă referința este null
        {
            Debug.LogError($"[LogicSpawnPrefabSO] Rețeta '{recipe.actionName}' nu are un 'resultedItem' setat. Nu se poate determina Prefab-ul.");
            return false;
        }

        // ⚠️ ATENȚIE: Nu mai avem nevoie de verificarea string.IsNullOrEmpty,
        // deoarece nu mai folosim 'itemName' ca și cheie.
        
        // 2. Găsește componenta PrefabSpawner pe inițiator
        PrefabSpawner spawner = initiator.GetComponent<PrefabSpawner>();
        
        if (spawner == null)
        {
            Debug.LogError($"[LogicSpawnPrefabSO] Nu a fost găsit PrefabSpawner pe inițiator ({initiator.name}).");
            return false;
        }

        // 3. Execută generarea, pasând referința directă la Item
        // 🎯 SCHIMBARE AICI: Apelăm metoda care acceptă un obiect Item.
        GameObject spawnedObject = spawner.SpawnInFrontOfInitiator(itemKey); 
        
        return spawnedObject != null;
    }
}