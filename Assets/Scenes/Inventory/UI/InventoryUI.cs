using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class InventoryUI : MonoBehaviour
{
    [Header("References")]
    public GameObject slotPrefab; // Prefabul de UI slot
    public Transform slotsParent; // Un obiect cu GridLayoutGroup

    private Dictionary<int, UIInventorySlot> uiSlots = new Dictionary<int, UIInventorySlot>();
    
    // Referință la CanvasGroup pentru a ascunde UI-ul fără a dezactiva GameObject-ul
    private CanvasGroup canvasGroup; 

    // NOU: Variabilă pentru a urmări numărul de sloturi de la ultima verificare
    private int lastSlotCount = 0; 
    // NOU: Flag pentru a forța un refresh al conținutului (ex: la deschidere)
    private bool needsContentRefresh = false;

    void Awake() 
    {
        // Preluăm componenta CanvasGroup
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            // Adaugă automat CanvasGroup dacă lipsește (opțional, dar util)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        // Inventarul este ascuns la început
        SetVisibility(false);
    }

    // Metodă publică pentru a schimba vizibilitatea (folosind CanvasGroup)
    public void SetVisibility(bool isVisible)
    {
        // 1. Schimbă vizualizarea (Alpha)
        canvasGroup.alpha = isVisible ? 1 : 0; 
        
        // 2. Blochează interacțiunile mouse-ului când e invizibil
        canvasGroup.interactable = isVisible; 
        canvasGroup.blocksRaycasts = isVisible; 
        
        // 3. Reîmprospătează UI-ul doar când se deschide
        if (isVisible)
        {
            RefreshUI();
            needsContentRefresh = true;
        }
    }

    public void RefreshUI()
    {
        if (InventoryManager.Instance == null)
        {
            Debug.LogWarning("InventoryManager.Instance e null!");
            return;
        }

        // Șterge sloturile vechi
        foreach (Transform child in slotsParent)
        {
            Destroy(child.gameObject);
        }
        uiSlots.Clear();

        // Creează sloturi noi
        foreach (InventorySlot slot in InventoryManager.Instance.allSlots)
        {
            // Asigură-te că folosești slotsParent (care este deja Transform)
            GameObject obj = Instantiate(slotPrefab, slotsParent); 
            UIInventorySlot uiSlot = obj.GetComponent<UIInventorySlot>();
            uiSlot.Setup(slot);
            uiSlots[slot.slotIndex] = uiSlot;
        }
        lastSlotCount = uiSlots.Count;
        Debug.Log($"🧱 UI actualizat: {uiSlots.Count} sloturi create.");
    }

    void Update()
    {
        if (InventoryManager.Instance == null) return;
        
        // 1. Logica de Afișare/Ascundere (Input)
        if (Input.GetKeyDown(KeyCode.I))
        {
            bool isCurrentlyVisible = canvasGroup.alpha == 1;
            SetVisibility(!isCurrentlyVisible); 
        }

        // 2. Actualizarea elementelor UI (opțional, dar condiționat)
        if (canvasGroup.alpha > 0) // Rulează doar dacă inventarul este vizibil
        {
            int currentDataSlotCount = InventoryManager.Instance.allSlots.Count;

            // A. Verifică dacă numărul total de sloturi s-a schimbat.
            if (currentDataSlotCount != lastSlotCount)
            {
                // Dacă numărul de sloturi s-a schimbat (adică s-au adăugat/scos rânduri),
                // trebuie să reconstruiești UI-ul complet (apelăm RefreshUI).
                Debug.Log($"⚠️ Schimbare majoră de structură detectată: {lastSlotCount} -> {currentDataSlotCount}. Reconstruiesc UI-ul.");
                RefreshUI(); 
                // După RefreshUI, lastSlotCount este actualizat.
            }
            // B. Dacă structura e aceeași, dar datele din sloturi s-au schimbat (item nou/număr crescut),
            // actualizează vizualul fiecărui slot (Partea 2 din codul tău original).
            else if (needsContentRefresh || currentDataSlotCount == lastSlotCount)
            {
                // Rulează actualizarea vizuală fină doar dacă numărul de sloturi e constant
                // sau dacă ai forțat-o (needsContentRefresh)
                foreach (var pair in uiSlots)
                {
                    pair.Value.Refresh();
                }
                
                // Dacă RefreshUI() a fost apelat la deschidere, setăm flag-ul pe false după prima rulare.
                needsContentRefresh = false; 
            }
        }
    }
}