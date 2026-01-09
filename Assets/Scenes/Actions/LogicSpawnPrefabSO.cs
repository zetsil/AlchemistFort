using UnityEngine;

[CreateAssetMenu(fileName = "LogicSpawnPrefab", menuName = "Building/Action Logic/Spawn Prefab")]
public class LogicSpawnPrefabSO : AbstractActionLogicSO
{
    public override bool ExecuteAction(ActionRecipeSO recipe, GameObject initiator)
    {
        // 1. Preluăm Item-ul rezultat din rețetă
        Item itemKey = recipe.resultItem; 

        if (itemKey == null)
        {
            Debug.LogError($"[LogicSpawnPrefabSO] Rețeta '{recipe.actionName}' nu are un 'resultItem' setat.");
            return false;
        }

        // 2. Întrebăm ItemVisualManager care este Prefab-ul asociat acestui Item
        if (ItemVisualManager.Instance == null)
        {
            Debug.LogError("[LogicSpawnPrefabSO] ItemVisualManager.Instance lipsește din scenă!");
            return false;
        }

        GameObject prefabToSpawn = ItemVisualManager.Instance.GetItemVisualPrefab(itemKey);

        if (prefabToSpawn == null)
        {
            // Mesajul de eroare este deja dat de ItemVisualManager.GetPrefab
            return false;
        }

        // 3. Găsim componenta care se ocupă de poziționarea obiectului în lume
        // NOTĂ: PrefabSpawner acum trebuie doar să știe UNDE să spawneze, nu CE.
        PrefabSpawner spawner = initiator.GetComponent<PrefabSpawner>();
        
        if (spawner == null)
        {
            Debug.LogError($"[LogicSpawnPrefabSO] Nu a fost găsit PrefabSpawner pe {initiator.name}.");
            return false;
        }

        // 4. Executăm spawn-ul efectiv pasând prefab-ul găsit în dicționar
        // Va trebui să te asiguri că în PrefabSpawner ai o metodă care acceptă direct un GameObject
        GameObject spawnedObject = spawner.SpawnObject(prefabToSpawn); 
        
        return spawnedObject != null;
    }
}