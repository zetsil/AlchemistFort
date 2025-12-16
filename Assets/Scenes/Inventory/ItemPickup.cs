using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    // Aici tragi și plasezi asset-ul tău Apple, Stick, etc.
    public Item itemData; // <-- Folosește clasa de bază Item!
    
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

   public void Collect()
    {
        if (itemData == null)
        {
            Debug.LogError("Item Data nu este setat pentru: " + gameObject.name);
            return;
        }

        if (itemData is ToolItem toolItem)
        {
            if (EquippedManager.Instance.IsEquippedSlotEmpty())
            {

                InventorySlot newSlot = new InventorySlot(toolItem, -1); 

                GlobalEvents.RequestSlotEquip(newSlot);
                
                Debug.Log($"✅ Unealta {toolItem.itemName} a fost echipată direct din Lume.");
                
                Destroy(gameObject); 
                
                // Opțional: Trimitem un semnal de sunet specific pentru echipare
                // GlobalEvents.TriggerPlaySound("EquipToolSound"); 
                return; 
            }
        }
        
        bool added = InventoryManager.Instance.AddItem(itemData); 

        if (added)
        {
            // Obiectul a fost adăugat cu succes în inventar.
            Debug.Log($"✅ Colectat: {itemData.itemName} x{itemData.amount}.");
            
            // Notificare (pentru sunet/UI, indiferent de succesul adăugării)
            // Păstrăm logica ta de semnal combinat pentru notificare/sunet.
            string combinedSignal = "Collect_" + itemData.itemName;
            GlobalEvents.TriggerPlaySound(combinedSignal); // Managerul de sunet primește "Collect_Wood"

            // Distrugem obiectul fizic, deoarece adăugarea a fost confirmată.
            Destroy(gameObject); 
        }
        else
        {
            Debug.LogWarning($"❌ Inventarul este plin! Nu s-a putut adăuga {itemData.itemName}.");
            
        }
        
    }
}