using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    // Aici tragi și plasezi asset-ul tău Apple, Stick, etc.
    public Item itemData; // <-- Folosește clasa de bază Item!
    
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        // ... (restul logicii Start)
    }

    public void Collect()
    {
        if (itemData == null)
        {
            Debug.LogError("Item Data nu este setat pentru: " + gameObject.name);
            return;
        }

        // 1. ADĂUGAREA ÎN INVENTAR
        // Aici se folosește asset-ul ScriptableObject.
        Debug.Log("Am ridicat: " + itemData.itemName);
        
        // Dacă e un obiect consumabil, poți face un test (deși de obicei nu se face aici)
        // if (itemData is Apple) 
        // {
        //     Apple apple = itemData as Apple;
        //     Debug.Log("Mărul restaurează " + apple.healthRestored + " viață.");
        // }
        
        // 2. ȘTERGEREA OBIECTULUI FIZIC DIN SCENĂ
        Destroy(gameObject); 
    }
}