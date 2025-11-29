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

        if (Physics.Raycast(rayOrigin, rayDirection, out hit, interactionDistance))
        {
            // Setăm ținta curentă indiferent de tip (util pentru highlight)
            currentTarget = hit.transform; 
            
            // ----------------------------------------------------
            // Prioritate 1: Buton de Acțiune UI (Ex: pe un panou)
            // ----------------------------------------------------
            ActionButtonUI buttonUI = hit.transform.GetComponent<ActionButtonUI>();
            
            // Verificăm dacă este buton ȘI este în containerul corect (pentru siguranță)
            if (buttonUI != null && hit.transform.parent != null && hit.transform.parent.name == "UI_Action_Container")
            {
                canInteract = true;
                
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
            }
        }
        
        // Actualizăm UI-ul de prompt (la final, după ce canInteract a fost setat)
        UpdateInteractionPrompt(canInteract);
    }
    
    private void UpdateInteractionPrompt(bool show)
    {
        if (interactionPromptUI != null)
        {
            interactionPromptUI.SetActive(show);
            
            // Opțional: Dacă dorești să afișezi un text diferit pentru butoane vs. obiecte,
            // poți face o verificare suplimentară pe currentTarget aici.
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

    // Metodă refăcută pentru a folosi InventoryManager.Instance.AddItem()
    private void PickUpObject(GameObject pickableObject)
    {
        ItemPickup pickup = pickableObject.GetComponent<ItemPickup>();

        if (pickup != null && pickup.itemData != null)
        {
            // 1. Verificare pentru Echipare Directă
            // Verificăm dacă obiectul este o Unealtă (ToolItem) ȘI dacă slotul de echipare este gol.
            if (pickup.itemData is ToolItem toolItem && EquippedManager.Instance.GetEquippedSlot() == null)
            {
                // Pas CRITIC: Creăm o instanță de slot pentru a urmări durabilitatea.
                // Folosim un index temporar de -1, deoarece EquippedManager va prelua proprietatea slotului.
                InventorySlot newSlot = new InventorySlot(toolItem, -1); 

                // Apelăm noul semnal bazat pe slot. EquippedManager se va abona și va echipa slotul.
                GlobalEvents.RequestSlotEquip(newSlot);
                
                Debug.Log($"✅ Unealta {toolItem.itemName} a fost echipată direct din Lume.");
                Destroy(pickableObject); 
                return; 
            }
            
            // 2. Logica Standard de Adăugare în Inventar (dacă nu e unealtă sau slotul e ocupat)
            bool added = InventoryManager.Instance.AddItem(pickup.itemData); 

            if (added)
            {
                Debug.Log($"✅ Colectat: {pickup.itemData.itemName} x{pickup.itemData.amount}.");
                Destroy(pickableObject); 
            }
            else
            {
                Debug.LogWarning($"❌ Inventarul este plin! Nu s-a putut adăuga {pickup.itemData.itemName}.");
            }
        }
        else
        {
            Debug.LogError($"Obiectul {pickableObject.name} nu are un ItemPickup valid (sau itemData este null)!");
        }
    }
}