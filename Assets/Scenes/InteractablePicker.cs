using UnityEngine;
using UnityEngine.UI; // Necesar pentru a folosi componentele de UI (Image/Text și Button)

public class InteractablePicker : MonoBehaviour
{
    // Trebuie setată în Inspector, de preferat la Camera jucătorului
    [Header("Setup")]
    public Camera playerCamera; 
    
    [Header("Settings")]
    public float interactionDistance = 3f;
    [Tooltip("Tasta folosită pentru ridicare SAU pentru a activa butonul vizat.")]
    public KeyCode pickUpKey = KeyCode.E;
    
    [Header("UI Interaction Prompt")]
    // Elementul de UI (un GameObject, Image, sau Text) care conține prompt-ul "Press E to Interact"
    public GameObject interactionPromptUI; 
    
    // Obiectul țintă pe care îl vizăm (pentru a-i putea arăta un efect de hover)
    private Transform currentTarget = null;
    
    // Variabila care ține minte dacă Raycast-ul a lovit un buton valid.
    private bool canInteract = false; 

    void Start()
    {
        // Ne asigurăm că prompt-ul UI este ascuns la început
        if (interactionPromptUI != null)
        {
            interactionPromptUI.SetActive(false);
        }
    }

    void Update()
    {
        if (playerCamera == null)
        {
            Debug.LogError("Player Camera nu este setată pe InteractablePicker!");
            return;
        }

        // Resetăm starea de interacțiune la fiecare frame
        currentTarget = null;
        canInteract = false;
        
        // Efectuăm Raycast-ul central
        RaycastHit hit;
        Vector3 rayOrigin = playerCamera.transform.position;
        Vector3 rayDirection = playerCamera.transform.forward;
        string resourceCostMessage = null;

        if (Physics.Raycast(rayOrigin, rayDirection, out hit, interactionDistance))
        {
            currentTarget = hit.transform;
            ActionButtonUI buttonUI = hit.transform.GetComponent<ActionButtonUI>();

            if (buttonUI != null && hit.transform.parent != null && hit.transform.parent.name == "UI_Action_Container")
            {
                canInteract = true;

                if (buttonUI.recipe != null)
                {
                    resourceCostMessage = FormatResourceCost(buttonUI.recipe);
                }

                if (Input.GetKeyDown(pickUpKey))
                {
                    ActivateActionButton(buttonUI);
                }
            }
            // ----------------------------------------------------
            // Prioritate 2: Obiect Ridicabil (Bazat pe Componenta PickableObject)
            // ----------------------------------------------------
            else
            {
                // Caută componenta PickableObject pe obiectul lovit.
                // Acesta este cel mai bun mod de a identifica un obiect ridicabil.
                ItemPickup pickable = hit.transform.GetComponent<ItemPickup>();

                if (pickable != null)
                {
                    canInteract = true;

                    if (Input.GetKeyDown(pickUpKey))
                    {
                        // Transmite GameObject-ul care deține componenta PickableObject
                        PickUpObject(pickable.gameObject);
                    }
                }
                else
                {
                    // 🆕 Tranziție de scenă (ușă / portal)
                    SceneTransitionDoor door = hit.transform.GetComponent<SceneTransitionDoor>();

                    if (door != null)
                    {
                        canInteract = true;

                        if (Input.GetKeyDown(pickUpKey))
                        {
                            door.TriggerTransition();
                        }
                    }
                }
            }


            
        }
        
        // Actualizăm UI-ul de prompt (la final, după ce canInteract a fost setat)
        UpdateInteractionPrompt(canInteract, resourceCostMessage);
    }
    

    private string FormatResourceCost(ActionRecipeSO recipe)
    {
        if (recipe == null) return null;

        var sb = new System.Text.StringBuilder();

        // 1. Titlul (Numele Acțiunii)
        sb.AppendLine(recipe.actionName); 

        // 2. Costurile Detașate (cu verificare de inventar)
        if (recipe.requiredItems != null && recipe.requiredItems.Count > 0)
        {
            sb.Append("Cost: ");
            
            for (int i = 0; i < recipe.requiredItems.Count; i++)
            {
                ItemCost cost = recipe.requiredItems[i];
                
                if (cost.requiredItem != null)
                {
                    int playerHave = InventoryManager.Instance.GetTotalItemCount(cost.requiredItem.itemName);
                    
                    // Formatul dorit: "ItemName: Needed X / Have Y"
                    // Exemplu: "Wood: 7 / 4"
                    
                    sb.Append($"{cost.requiredItem.itemName}: {playerHave} / {cost.amount}");
                    
                    // Adăugăm separator dacă nu este ultimul element
                    if (i < recipe.requiredItems.Count - 1)
                    {
                        sb.Append(" | "); // Folosesc '|' pentru o separare vizuală mai clară
                    }
                }
            }
        }
        else
        {
            sb.Append("Cost: Free");
        }

        // Mesajul va avea formatul (Exemplu): 
        // "Build Wall\nCost: Wood: 7 / 4 | Stone: 5 / 8"
        return sb.ToString();
    }
    
    private void UpdateInteractionPrompt(bool show, string costMessage)
    {
        if (interactionPromptUI != null)
        {
            interactionPromptUI.SetActive(show);
        }

        if (show && !string.IsNullOrEmpty(costMessage))
        {
            GlobalEvents.RequestNotification(costMessage, MessageType.ResourceNeeded);
        }
        else if (!show)
        {
            // Dacă nu vizăm nimic interacționabil, ascundem fereastra de notificare
            // Presupunem că GlobalEvents are o metodă pentru a ascunde notificarea curentă
            // sau că UIInfoWindow ascunde singur după un timp.
            // Pentru siguranță, putem trimite un mesaj gol sau un eveniment de curățare.
            // Presupunem că UIInfoWindow se șterge singur după DISPLAY_TIME.
        }
    }

    private void ActivateActionButton(ActionButtonUI buttonUI)
    {
        // Simulăm click-ul pe componenta Unity Button
        Button unityButton = buttonUI.GetComponent<Button>();
        if (unityButton != null)
        {
             unityButton.onClick.Invoke();
             Debug.Log($"[InteractablePicker] Buton de acțiune activat: {buttonUI.name}");
        }
    }

    private void PickUpObject(GameObject pickableObject)
    {
        // 1. Obține componenta ItemPickup
        ItemPickup pickup = pickableObject.GetComponent<ItemPickup>();

        // 2. Verifică validitatea (dacă pickup și itemData sunt setate)
        if (pickup != null && pickup.itemData != null)
        {
            // 3. 🚀 Deleagă TOATĂ LOGICA DE COLECTARE componentei ItemPickup
            // ItemPickup.Collect() decide dacă itemul e echipat, adăugat sau dacă inventarul e plin.
            // Și tot ItemPickup.Collect() gestionează distrugerea obiectului (Destroy(pickableObject)).
            
            pickup.Collect();
        }
        else
        {
            // Eroare dacă obiectul interacționat nu are componenta necesară
            Debug.LogError($"Obiectul {pickableObject.name} nu are un ItemPickup valid (sau itemData este null)! Verifică Asset-ul SO.");
        }
    }
}