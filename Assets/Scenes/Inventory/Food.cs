using UnityEngine;

// Cale de creare: Inventory/Food/Apple
[CreateAssetMenu(fileName = "Food", menuName = "Inventory/Food")]
public class Food : Item // Moștenește clasa de bază Item
{
    [Header("Food Properties")]
    public float healthRestored = 10f;
    public float staminaRestored = 5f;
    
    // Suprascriem metoda Use() pentru a adăuga logica de consum.
    public override void Use()
    {
        // 1. Căutăm jucătorul în scenă
        // Folosim tag-ul "Player" pentru a găsi obiectul care are PlayerStats
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");

        if (playerObj != null)
        {
            // 2. Încercăm să luăm componenta PlayerStats (care moștenește din AllyEntity/Entity)
            if (playerObj.TryGetComponent<PlayerStats>(out var stats))
            {
                // Restaurăm viața (folosind metoda RestoreHealth pe care ar trebui să o aibă Entity)
                // Dacă nu ai o metodă RestoreHealth, putem modifica direct variabila
                stats.RestoreHealth(healthRestored);

                // Restaurăm și stamina (am adăugat-o mai devreme în PlayerStats)
                stats.currentStamina = Mathf.Min(stats.currentStamina + staminaRestored, stats.maxStamina);

                Debug.Log($"🍎 Consumat: {itemName}. HP +{healthRestored}, Stamina +{staminaRestored}");

                if (InventoryManager.Instance == null)
                {
                    Debug.LogError("Managerul de Inventar nu este instanțiat. Nu se poate elimina obiectul.");
                    return;
                }

                // --- Logica de Consum Globală ---
                int amountToConsume = 1;

                // Folosim metoda 'DecreaseItem' pentru a consuma 1 unitate.
                bool consumed = InventoryManager.Instance.DecreaseItem(itemName, amountToConsume);

                if (consumed)
                {
                    Debug.Log($"[Inventar] {itemName} x{amountToConsume} a fost consumat.");
                    // **Aici se adaugă logica unică a itemului (ex: vindecare, buff).**
                }
                else
                {
                    // Managerul afișează deja warning-ul specific.
                    Debug.LogWarning($"[Inventar] Nu s-a putut folosi/consuma {itemName}. Acțiunea eșuează.");
                }

                // 3. Logica de bază (afișare consolă)
                base.Use();

                // 4. Aici ar trebui să apelezi și o metodă de eliminare din inventar
                // Inventory.Instance.RemoveItem(this);
            }
        }
        else
        {
            Debug.LogWarning("Nu am găsit jucătorul pentru a consuma obiectul!");
        }
    }
}
