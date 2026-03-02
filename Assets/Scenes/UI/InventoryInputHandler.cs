using UnityEngine;


public class InventoryInputHandler : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            // --- MODIFICARE AICI: Adaugă .Instance înainte de funcție ---
            if (PauseMenuManager.Instance != null && PauseMenuManager.Instance.IsGamePaused())
            {
                return; 
            }

            if (InventoryPanelController.Instance != null)
            {
                InventoryPanelController.Instance.TogglePanel();
            }
            else
            {
                Debug.LogError("InventoryPanelController nu a fost găsit.");
            }
        }
    }
}