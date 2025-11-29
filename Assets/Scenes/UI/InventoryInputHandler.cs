using UnityEngine;

public class InventoryInputHandler : MonoBehaviour
{
    void Update()
    {
        // Verifică dacă tasta 'I' (pentru Inventory) a fost apăsată
        if (Input.GetKeyDown(KeyCode.I))
        {
            if (InventoryPanelController.Instance != null)
            {
                // Doar comutăm panoul. Logica de blocare/deblocare este în interiorul TogglePanel().
                InventoryPanelController.Instance.TogglePanel();
            }
            else
            {
                Debug.LogError("InventoryPanelController nu a fost găsit.");
            }
            
            // Nu mai avem nevoie să ne facem griji pentru FirstPersonController aici!
        }
    }
}