using UnityEngine;

// ATENȚIE: Presupune că ai un ItemVisualManager.Instance disponibil în scenă.
public class EquippedToolVisualizer : MonoBehaviour
{

    public static EquippedToolVisualizer Instance { get; private set; }
    
    // Obiectul părinte unde va fi instanțiat modelul uneltei (e.g., sub camera FPC)
    [Tooltip("Transform-ul care servește ca punct de prindere pentru uneltele echipate.")]
    public Transform toolHoldPoint; 

    private GameObject currentToolInstance;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
    }

    private void Start()
    {

        if (EquippedManager.Instance != null)
        {
            EquippedManager.Instance.OnSlotEquippedStateChanged += UpdateToolVisual;
            
            // BONUS: Facem o primă actualizare a vizualului (pentru a afișa starea inițială)
            UpdateToolVisual(EquippedManager.Instance.GetEquippedSlot());
        }
        else
        {
            Debug.LogError("EquippedManager.Instance nu a fost găsit în Start()! Verificați ordinea de execuție.");
        }
    }

    private void OnDisable()
    {
        // 2. Dezabonare
        if (EquippedManager.Instance != null)
        {
            EquippedManager.Instance.OnSlotEquippedStateChanged -= UpdateToolVisual;
        }
    }

    public void UpdateToolVisual(InventorySlot equippedSlot)
    {
        // Extragem datele statice (ToolItem ScriptableObject) din slot.
        ToolItem newToolData = equippedSlot?.ToolItemData;

        // A. Curăță instanța veche, dacă există.
        if (currentToolInstance != null)
        {
            Destroy(currentToolInstance);
            currentToolInstance = null;
        }

        // B. Dacă se echipează o unealtă nouă:
        if (newToolData != null && ItemVisualManager.Instance != null)
        {
            // Cerem Prefab-ul 3D de la manager, folosind ScriptableObject-ul (din slot) ca cheie.
            GameObject prefabToInstantiate = ItemVisualManager.Instance.GetVisualPrefab(newToolData);
            
            if (prefabToInstantiate != null)
            {
                // Instanțiem Prefab-ul ca și copil al punctului de prindere (toolHoldPoint).
                currentToolInstance = Instantiate(prefabToInstantiate, toolHoldPoint);

                // Asigurăm că poziția și rotația sunt resetate (dacă nu sunt setate în Prefab)
                currentToolInstance.transform.localPosition = Vector3.zero;
                // currentToolInstance.transform.localRotation = Quaternion.identity;

                Debug.Log($"[Visualizer] Modelul 3D pentru {newToolData.itemName} a fost afișat.");
            }
        }
        else if (newToolData == null)
        {
            Debug.Log("[Visualizer] Unealta a fost dezechipată, modelul 3D a fost curățat.");
        }
    }
}