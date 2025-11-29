using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System; // Adăugat pentru Action, necesar pentru evenimente (deși nu e folosit direct aici, e bună practică)


public class InventoryPanelController : MonoBehaviour
{
    // === Configurare ===
    [Header("UI Toolkit Assets")]
    public VisualTreeAsset inventoryPanelUxml; // Panoul principal UXML
    [Tooltip("Un VisualElement gol care va servi ca șablon pentru un slot de inventar.")]
    public VisualTreeAsset slotTemplate; // Șablon UXML simplu pentru un singur slot

    // === Referințe Runtime ===
    private VisualElement rootElement;
    private Button exitButton;
    private VisualElement slotsGridContainer;

    // NOU: Referințe pentru Slotul de Unealtă Echipată
    private VisualElement equippedToolSlot;
    private VisualElement equippedToolIcon;
    private Label equippedDurabilityLabel;
    private Label equippedToolTypeLabel;


    private Dictionary<InventorySlot, VisualElement> slotToElementMap = new Dictionary<InventorySlot, VisualElement>();
    private bool isPanelOpen = false;
    
    [Header("Context Menu Assets")]
    public VisualTreeAsset contextMenuUxml;
    private VisualElement contextMenu;
    private InventorySlot selectedSlot;
    // NOU: Referințe pentru butoanele statice
    private Button btnUse;
    private Button btnEquip;
    private Button btnDrop;

    private VisualElement tooltipPanel;
    private Label tooltipNameLabel;
    private Label tooltipDescriptionLabel;

    [Header("Tooltip Assets")]
    public VisualTreeAsset tooltipUxml;

    public static InventoryPanelController Instance { get; private set; }

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

    public bool getIsPanelOpen()
    {
        return isPanelOpen;
    }

    private void OnEnable()
    {
        var uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null)
        {
            Debug.LogError("InventoryPanelController necesită un UIDocument.");
            return;
        }

        // 1. Încărcarea și Clonarea UXML
        rootElement = inventoryPanelUxml.CloneTree();
        uiDocument.rootVisualElement.Add(rootElement);

        // 2. Obținerea Referințelor
        FindUIElements();

        // 3. Atașarea Evenimentelor
        RegisterCallbacks();

        // 4. NOU: Abonarea la Evenimentele de Echipare
        if (EquippedManager.Instance != null)
        {
            EquippedManager.Instance.OnSlotEquippedStateChanged += RefreshEquippedToolUI;
        }

        // 5. NOU: Atașează callback-ul de click pe slotul echipat
        if (equippedToolSlot != null)
        {
            equippedToolSlot.RegisterCallback<MouseDownEvent>(OnEquippedSlotMouseDown);
            equippedToolSlot.RegisterCallback<PointerEnterEvent>(OnEquippedSlotPointerEnter);
            equippedToolSlot.RegisterCallback<PointerLeaveEvent>(OnEquippedSlotPointerLeave);
        }

        // La început, ascundem panoul.
        SetPanelVisibility(false);

        InitializeContextMenu();
        InitializeTooltip();

        // Facem o primă actualizare a slotului echipat (pentru a afișa starea inițială)
        RefreshEquippedToolUI(EquippedManager.Instance.GetEquippedSlot()); 
    }

    private void OnDisable()
    {
        // Dezabonare pentru a preveni erorile la distrugerea obiectului
        if (EquippedManager.Instance != null)
        {
            EquippedManager.Instance.OnSlotEquippedStateChanged -= RefreshEquippedToolUI;
            
        }

        if (equippedToolSlot != null)
        {
            equippedToolSlot.UnregisterCallback<MouseDownEvent>(OnEquippedSlotMouseDown);
            equippedToolSlot.UnregisterCallback<PointerEnterEvent>(OnEquippedSlotPointerEnter);
            equippedToolSlot.UnregisterCallback<PointerLeaveEvent>(OnEquippedSlotPointerLeave);
        }

        // Dezabonare callback-uri pentru butoanele meniului
        btnUse.clicked -= OnContextActionClicked;
        btnEquip.clicked -= OnContextActionClicked;
        btnDrop.clicked -= OnContextDropClicked;
    }



    private void OnEquippedSlotPointerEnter(PointerEnterEvent evt)
    {
        InventorySlot equippedSlot = EquippedManager.Instance.GetEquippedSlot();
        
        if (equippedSlot != null)
        {
            ShowTooltip(equippedSlot, evt.position); 
        }
        evt.StopPropagation();
    }

    // NOU: Gestează ieșirea mouse-ului de pe slotul echipat pentru tooltip
    private void OnEquippedSlotPointerLeave(PointerLeaveEvent evt)
    {
        HideTooltip();
        evt.StopPropagation();
    }


    private void InitializeTooltip()
    {
        if (tooltipUxml == null || rootElement == null) return;

        // Clonează meniul (o singură dată)
        tooltipPanel = tooltipUxml.CloneTree();

        // Obține referințele la elementele interne
        tooltipNameLabel = tooltipPanel.Q<Label>("ItemNameLabel");
        tooltipDescriptionLabel = tooltipPanel.Q<Label>("ItemDescriptionLabel");

        // Adaugă-l la rădăcină (permanent)
        rootElement.Add(tooltipPanel);

        // Ascunde-l inițial
        tooltipPanel.style.display = DisplayStyle.None;
        tooltipPanel.style.position = Position.Absolute; // Asigură-te că folosește poziționare absolută
    }

    private void InitializeContextMenu()
    {
        if (contextMenuUxml == null) return;

        // Clonează meniul (o singură dată)
        contextMenu = contextMenuUxml.CloneTree();
        rootElement.Add(contextMenu); // Adaugă-l la rădăcină permanent

        // Obține referințele la butoane
        btnUse = contextMenu.Q<Button>("ContextMenuUse");
        btnEquip = contextMenu.Q<Button>("ContextMenuEquip");
        btnDrop = contextMenu.Q<Button>("ContextMenuDrop");

        // Atașează callback-urile (o singură dată)
        btnUse.clicked += OnContextActionClicked; // Folosim aceeași metodă generală
        btnEquip.clicked += OnContextActionClicked; // Folosim aceeași metodă generală
        btnDrop.clicked += OnContextDropClicked;

        rootElement.RegisterCallback<MouseDownEvent>(evt =>
        {
            // Dacă meniul e vizibil și click-ul e în afara lui, ascunde-l
            if (contextMenu.style.display == DisplayStyle.Flex && !contextMenu.ContainsPoint(evt.localMousePosition))
            {
                HideContextMenu();
            }
        });


        // Ascunde întregul meniu inițial
        contextMenu.style.display = DisplayStyle.None;
    }

    private void FindUIElements()
    {
        exitButton = rootElement.Q<Button>("Exit");
        slotsGridContainer = rootElement.Q<VisualElement>("slots-grid-container");

        // NOU: Obține referințele la slotul de Unealtă Echipată
        equippedToolSlot = rootElement.Q<VisualElement>("equipped-tool-slot");
        equippedToolIcon = equippedToolSlot.Q<Image>("tool-icon"); // Folosim Q<Image> dacă UXML-ul îl are ca <ui:Image>
        equippedDurabilityLabel = equippedToolSlot.Q<Label>("durability-label");
        equippedToolTypeLabel = rootElement.Q<Label>("tool-type-label");
    }

    private void RegisterCallbacks()
    {
        if (exitButton != null)
        {
            exitButton.clicked += OnExitButtonClicked;
        }
    }

    public void SetPanelVisibility(bool isVisible)
    {
        if (rootElement != null)
        {
            rootElement.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;
            isPanelOpen = isVisible;
            ToggleGameInput(isVisible);

            if (isVisible)
            {
                RefreshUI();
            }
        }
    }

    // NOU: Metodă publică pentru a comuta starea panoului
    public void TogglePanel()
    {
        SetPanelVisibility(!isPanelOpen);
    }

    private void ToggleGameInput(bool inventoryIsOpen)
    {
        if (FirstPersonController.Instance == null)
        {
            return;
        }

        bool playerCanMoveAndLook = !inventoryIsOpen;

        // 1. Blochează/Permite mișcarea camerei (look)
        FirstPersonController.Instance.cameraCanMove = playerCanMoveAndLook;

        // 2. Blochează/Permite mișcarea corpului (walk/sprint)
        FirstPersonController.Instance.playerCanMove = playerCanMoveAndLook;

        // 3. Comută starea cursorului
        if (inventoryIsOpen)
        {
            // Inventar Deschis: Eliberăm mouse-ul și îl facem vizibil
            UnityEngine.Cursor.lockState = CursorLockMode.None;
            UnityEngine.Cursor.visible = true;
        }
        else
        {
            // Inventar Închis: Blocăm mouse-ul în centru
            UnityEngine.Cursor.lockState = CursorLockMode.Locked;
            UnityEngine.Cursor.visible = false;
        }

        Debug.Log($"Stare joc comutată. Mișcare permisă: {playerCanMoveAndLook}");
    }


    private void OnExitButtonClicked()
    {
        SetPanelVisibility(false);
    }


    // ===============================================
    // LOGICA INVENTARULUI (Popularea Grid-ului)
    // ===============================================

    public void RefreshUI()
    {
        if (slotsGridContainer == null || InventoryManager.Instance == null) return;

        // 1. Curățăm Grid-ul vechi
        slotsGridContainer.Clear();
        slotToElementMap.Clear();

        // 2. Iterăm prin toate sloturile pline din InventoryManager
        foreach (var inventorySlot in InventoryManager.Instance.allSlots)
        {
            // Generează și populează un slot vizual doar pentru sloturile care au item-uri.
            CreateSlotVisual(inventorySlot);
        }

        int slotsUsed = InventoryManager.Instance.GetCurrentSlots();
        int maxSlots = InventoryManager.Instance.max_slots;

        for (int i = slotsUsed; i < maxSlots; i++)
        {
            CreateEmptySlotVisual();
        }
        
        // Asigură-te că și slotul echipat este actualizat, deși evenimentul ar trebui să se ocupe de asta.
        RefreshEquippedToolUI(EquippedManager.Instance.GetEquippedSlot());
    }

    private void CreateSlotVisual(InventorySlot inventorySlot = null)
    {
        // 1. Crează elementul vizual de bază (folosește un slotTemplate UXML simplu dacă ai)
        VisualElement slotElement = new VisualElement();
        slotElement.AddToClassList("inventory-slot"); // Adaugă clasa USS

        // 2. Adaugă elementul Icon (care va ține imaginea item-ului)
        VisualElement iconElement = new VisualElement();
        iconElement.AddToClassList("item-icon");
        slotElement.Add(iconElement);

        if (inventorySlot != null)
        {
            // 3. Popularea cu Item-uri PLINE

            // Setăm Iconița
            iconElement.style.backgroundImage = new StyleBackground(inventorySlot.icon);

            // Setăm numărul de Item-uri (Stack Count)
            if (inventorySlot.count > 1)
            {
                Label countLabel = new Label(inventorySlot.count.ToString());
                countLabel.AddToClassList("stack-count-label");
                slotElement.Add(countLabel);
            }

            slotElement.RegisterCallback<PointerEnterEvent>(evt =>
            {
                ShowTooltip(inventorySlot, evt.position);
                evt.StopPropagation();
            });

            slotElement.RegisterCallback<PointerLeaveEvent>(evt =>
            {
                HideTooltip();
                evt.StopPropagation();
            });

            // 4. Adăugăm interacțiunea (Click pe Item)
            slotElement.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.button == (int)MouseButton.RightMouse || evt.button == (int)MouseButton.LeftMouse)
                {
                    OnSlotClicked(inventorySlot);
                }
                evt.StopPropagation(); 
            });

            slotToElementMap.Add(inventorySlot, slotElement);
        }
        else
        {
            // 3. Popularea cu Sloturi GOALE (dacă inventorySlot este null)
            // Slotul rămâne gol vizual, dar își păstrează fundalul din USS.
        }

        // 5. Adaugă slotul la containerul Grid
        slotsGridContainer.Add(slotElement);
    }

    private void CreateEmptySlotVisual()
    {
        // Metodă ajutătoare pentru sloturile goale
        CreateSlotVisual(null);
    }


    private void OnSlotClicked(InventorySlot slot)
    {
        // Dacă slotul e gol, închidem meniul contextual și ieșim.
        if (slot.itemData == null)
        {
            HideContextMenu();
            return;
        }
        selectedSlot = slot;

        // Slotul din inventar nu este slotul echipat.
        ShowContextMenu(slot, Input.mousePosition, isEquippedSlot: false); 
    }

    // NOU: Callback pentru click pe slotul echipat
    private void OnEquippedSlotMouseDown(MouseDownEvent evt)
    {
        // CORECTARE 1: Schimbăm tipul variabilei locale la InventorySlot.
        InventorySlot equippedSlot = EquippedManager.Instance.GetEquippedSlot();

        // Verificăm dacă slotul este ocupat și dacă este click Dreapta/Stânga
        if (equippedSlot != null && (evt.button == (int)MouseButton.RightMouse || evt.button == (int)MouseButton.LeftMouse))
        {
            // CORECTARE 2: Folosim slotul real, nu creăm un slot temporar inutil.
            selectedSlot = equippedSlot; 

            // Arătăm meniul contextual specific pentru slotul ECHIPAT
            ShowContextMenu(selectedSlot, Input.mousePosition, isEquippedSlot: true); 
        }
        else
        {
            HideContextMenu();
        }
        evt.StopPropagation();
    }


    private void ShowContextMenu(InventorySlot slot, Vector3 screenPosition, bool isEquippedSlot)
    {
        if (contextMenu == null || slot.itemData == null)
        {
            HideContextMenu();
            return;
        }
        HideTooltip();

        // 2. Ascunde TOATE butoanele înainte de a afișa
        btnUse.style.display = DisplayStyle.None;
        btnEquip.style.display = DisplayStyle.None;
        btnDrop.style.display = DisplayStyle.None;

        // 3. LOGICA DINAMICĂ
        if (isEquippedSlot)
        {
            // Slotul ECHIPAT: oferă opțiunea Unequip
            btnEquip.text = "Unequip";
            btnEquip.style.display = DisplayStyle.Flex; 
        }
        else
        {
            // Slot din INVENTARUL STOCABIL
            
            // a) Consumabile
            if (slot.itemData is Food)
            {
                btnUse.text = "Use / Eat";
                btnUse.style.display = DisplayStyle.Flex;
            }
            // b) Item-uri care se Echipează
            else if (slot.itemData is ToolItem)
            {
                btnEquip.text = "Equip";
                btnEquip.style.display = DisplayStyle.Flex; 
            }
        }
        
        // Acțiunea comună: Drop (Valabilă pentru toți item-ii non-goi)
        btnDrop.style.display = DisplayStyle.Flex;


        // 4. Poziționează meniul (Logica corectă)
        float uiHeight = rootElement.resolvedStyle.height;

        contextMenu.style.position = Position.Absolute;
        contextMenu.style.left = screenPosition.x + 10;
        contextMenu.style.top = uiHeight - screenPosition.y;

        // 5. Afișează containerul principal al meniului
        contextMenu.style.display = DisplayStyle.Flex;
    }

    private void HideContextMenu()
    {
        if (contextMenu != null)
        {
            contextMenu.style.display = DisplayStyle.None;
            selectedSlot = null;
        }
    }


    private void OnContextActionClicked()
    {
        if (selectedSlot == null || selectedSlot.itemData == null)
        {
            HideContextMenu();
            return;
        }
        
        // 1. Verifică acțiunile de Echipare/Dezechipare
        if (btnEquip.style.display == DisplayStyle.Flex)
        {
            if (btnEquip.text == "Unequip")
            {
                // Dezechipare (se apelează pe EquippedManager pentru a gestiona itemul curent)
                EquippedManager.Instance.UnequipTool();
            }
            else // "Equip" (pentru slotul din inventar)
            {
                // NOU: Apelăm HandleUse() pe SLOT.
                // Logica din HandleUse va decide că e o unealtă și va apela EquipSlot(this).
                selectedSlot.HandleUse(); 
            }
        }
        
        // 2. Verifică acțiunile de Consum/Use
        if (btnUse.style.display == DisplayStyle.Flex)
        {
            // NOU: Apelăm HandleUse() pe SLOT.
            // Logica din HandleUse va decide dacă este un consumabil (caz în care va apela Item.Use()).
            selectedSlot.HandleUse(); 
        }


        HideContextMenu();
        // Trebuie să forțezi o actualizare a inventarului (deoarece un item a fost scos/consumat)
        RefreshUI(); 
    }

    private void OnContextDropClicked()
    {
        // Logica de Aruncare
        if (selectedSlot != null && selectedSlot.itemData != null)
        {
            Debug.Log($"Aruncă: {selectedSlot.itemData.itemName}");
            // EX: InventoryManager.Instance.DropItem(selectedSlot);
        }
        
        HideContextMenu();
        RefreshUI();
    }
    
    // ===============================================
    // LOGICA SLOTULUI ECHIPAT
    // ===============================================

   public void RefreshEquippedToolUI(InventorySlot slot)
    {
        // Ne asigurăm că toate elementele UI necesare sunt prezente
        if (equippedToolIcon == null || equippedDurabilityLabel == null || equippedToolTypeLabel == null) return;

        // ItemData este S.O.-ul, folosit pentru iconiță și tip.
        // ItemState este starea dinamică, folosită pentru durabilitate.
        ToolItem toolData = slot?.ToolItemData;
        ItemState toolState = slot?.state;

        if (slot != null && toolData != null && toolState != null)
        {
            // 1. Echipat: Afișează Iconița și Info
            equippedToolIcon.style.backgroundImage = new StyleBackground(toolData.icon);
            equippedToolIcon.style.display = DisplayStyle.Flex; 
            
            // Afișează Durabilitatea (folosim datele dinamice din slot.state)
            // NOTĂ: Dacă dorești % din total, folosește (current / max) * 100
            float durabilityPercentage = (toolState.currentDurability / toolData.maxDurability) * 100f;
            
            equippedDurabilityLabel.text = $"{durabilityPercentage:F0}% ({toolState.currentDurability:F0})";
            equippedDurabilityLabel.style.display = DisplayStyle.Flex;
            
            // Afișează Tipul
            equippedToolTypeLabel.text = $"Type: {toolData.toolCategory}";
        }
        else
        {
            // 2. Dezechipat: Ascunde Iconița și Info
            equippedToolIcon.style.backgroundImage = null;
            equippedToolIcon.style.display = DisplayStyle.None;
            
            equippedDurabilityLabel.text = string.Empty;
            equippedDurabilityLabel.style.display = DisplayStyle.None;
            
            equippedToolTypeLabel.text = "Type: None";
        }
    }


    // ===============================================
    // LOGICA TOOLTIP-ULUI
    // ===============================================

    private void ShowTooltip(InventorySlot slot, Vector2 screenPosition)
    {
        HideTooltip();

        if (tooltipPanel == null || slot.itemData == null) return;
        
        // 1. Popularea textului 
        tooltipNameLabel.text = slot.itemData.itemName;
        tooltipDescriptionLabel.text = slot.itemData.description; 
        
        // 2. Poziționarea
        float offsetX = 15f; 
        tooltipPanel.style.left = screenPosition.x + offsetX;
        tooltipPanel.style.top = screenPosition.y; 
        
        // 3. Afișarea
        tooltipPanel.style.display = DisplayStyle.Flex;
    }

    private void HideTooltip()
    {
        if (tooltipPanel != null)
        {
            tooltipPanel.style.display = DisplayStyle.None;
        }
    }

}