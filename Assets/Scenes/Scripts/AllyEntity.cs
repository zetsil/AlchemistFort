using UnityEngine;

public class AllyEntity : Entity
{
    [Header("Building Settings")]
    [Tooltip("Prefabul care reprezintă modelul distrus (același mesh cu shader special)")]
    public GameObject brokenBuildingPrefab;

    // 1. Override TakeDamage (opțional dacă nu adaugi logică extra)
    public override void TakeDamage(float baseDamage, ToolType attackingToolType)
    {
        base.TakeDamage(baseDamage, attackingToolType);
    }

    // 2. Override DropLoot pentru a schimba comportamentul de distrugere
    protected override void DropLoot()
    {
        WorldEntityState state = GetComponent<WorldEntityState>();
        
        if (state != null && SaveManager.Instance != null)
        {
            // 1. Extragem ID-ul original (scoatem "Built_")
            string originalID = state.uniqueID.Replace("Built_", "");

            // 2. Scoatem ID-ul original din lista de distruse 
            // Astfel, dacă dai Load acum, Ghost-ul reapare (sau ruina îl înlocuiește)
            SaveManager.Instance.UnregisterDestroyedWorldItem(originalID);

            // 3. Oprim salvarea instanței curente de runtime
            state.isSpawnedAtRuntime = false;
        }

        if (brokenBuildingPrefab != null)
        {
            // Spawnăm ruina
            GameObject broken = Instantiate(brokenBuildingPrefab, transform.position, transform.rotation);
            
            // OPȚIONAL: Dacă vrei ca și RUINA să fie salvată, 
            // îi dai tot ID-ul "Built_..." și isSpawnedAtRuntime = true
        }
    }
}