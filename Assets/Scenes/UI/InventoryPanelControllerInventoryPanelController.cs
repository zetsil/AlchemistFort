using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System; // Adăugat pentru Action, necesar pentru evenimente (deși nu e folosit direct aici, e bună practică)


public class InventoryPanelController : MonoBehaviour
{
    // === Configurare ===
    [Tooltip("Un VisualElement gol care va servi ca șablon pentru un slot de inventar.")]
    public VisualTreeAsset slotTemplate; // Șablon UXML simplu pentru un singur slot\


    private Vector2 pointerStartPos;
    private const float dragThreshold = 5f; // Pixeli de mișcare înainte să considerăm că e Drag

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
    private Button btnDropAll;

    private VisualElement tooltipPanel;
    private Label tooltipNameLabel;
    private Label tooltipDescriptionLabel;

    private VisualElement ghostIcon; 
    private InventorySlot draggedSlot;
    private VisualElement originalSlotElement;

    [Header("Tooltip Assets")]
    public VisualTreeAsset tooltipUxml;
    private List<VisualElement> uiSlotElements = new List<VisualElement>();

    public static InventoryPanelController Instance { get; private set; }

    private VisualElement inventoryPanel;       // Containerul care se ascunde
    private VisualElement hotbarPanel;          // Containerul care rămâne vizibil
    private VisualElement hotbarSlotContainer;  // Unde vom genera slo



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

    private void Start()
    {
        var uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null)
        {
            Debug.LogError("InventoryPanelController necesită un UIDocument.");
            return;
        }

        // 1. Încărcarea și Clonarea UXML
        rootElement = uiDocument.rootVisualElement;

        // 2. Obținerea Referințelor
        FindUIElements();

        // 3. Atașarea Evenimentelor
        RegisterCallbacks();

        // 4. NOU: Abonarea la Evenimentele de Echipare
        if (EquippedManager.Instance != null)
        {
            EquippedManager.Instance.OnSlotEquippedStateChanged += RefreshEquippedToolUI;
        }

        if (QuickSlotManager.Instance != null)
        {
            QuickSlotManager.Instance.OnQuickSlotUIUpdate +=  RefreshHotbarVisuals;
        }

        // 5. NOU: Atașează callback-ul de click pe slotul echipat
        if (equippedToolSlot != null)
        {
            equippedToolSlot.RegisterCallback<MouseDownEvent>(OnEquippedSlotMouseDown);
            equippedToolSlot.RegisterCallback<PointerEnterEvent>(OnEquippedSlotPointerEnter);
            equippedToolSlot.RegisterCallback<PointerLeaveEvent>(OnEquippedSlotPointerLeave);
            // equippedToolSlot.RegisterCallback<PointerDownEvent>(evt => OnSlotPointerDown(evt, slotIndex, equippedToolSlot));
            // equippedToolSlot.RegisterCallback<PointerUpEvent>(evt => OnSlotPointerUp(evt, slotIndex));
        }

        // La început, ascundem panoul.
        SetPanelVisibility(false);
        CreateGhostIcon();
        InitializeContextMenu();
        InitializeTooltip();
        InitializeHotbar();

        // Facem o primă actualizare a slotului echipat (pentru a afișa starea inițială)
        RefreshEquippedToolUI(EquippedManager.Instance.GetEquippedSlot()); 
    }

    public void SetPanelVisibility(bool isVisible)
    {
        if (inventoryPanel != null)
        {
            // DOAR panoul de inventar se ascunde/arată
            inventoryPanel.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;
            
            isPanelOpen = isVisible;
            
            // Blocăm input-ul jucătorului doar când inventarul e deschis
            ToggleGameInput(isVisible);

            if (isVisible)
            {
                RefreshUI();
            }
        }
    }

    private void OnDisable()
    {
        // Dezabonare pentru a preveni erorile la distrugerea obiectului
        if (EquippedManager.Instance != null)
        {
            EquippedManager.Instance.OnSlotEquippedStateChanged -= RefreshEquippedToolUI;

        }

        if (QuickSlotManager.Instance != null)
        {
            QuickSlotManager.Instance.OnQuickSlotUIUpdate -=  RefreshHotbarVisuals;
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
        btnDropAll.clicked -= OnContextDropAllClicked;
    }
    

    private void CreateHotbarSlotUI(int index, VisualElement container)
    {
        var slotElement = slotTemplate.CloneTree();
        var slotVisual = slotElement.Q<VisualElement>("slot-container");
        
        // Adăugăm o clasă CSS specială dacă vrei să arate diferit
        slotVisual.AddToClassList("quick-shortcut-slot");
        
        container.Add(slotElement);
        
        // Actualizăm vizualul pe baza numelui itemului din QuickSlot
        UpdateHotbarVisuals();

        // IMPORTANT: Callbacks speciale pentru Hotbar (ex: Drop un item în hotbar pentru asignare)
        slotVisual.RegisterCallback<PointerUpEvent>(evt => OnPointerUpOnHotbar(evt, index));
    }
    

    private void UpdateHotbarVisuals()
    {
        if (hotbarSlotContainer == null || QuickSlotManager.Instance == null) return;

        // Iterăm prin toate elementele UI din containerul de hotbar
        for (int i = 0; i < hotbarSlotContainer.childCount; i++)
        {
            // Obținem elementul vizual al slotului curent
            VisualElement slotUI = hotbarSlotContainer[i];
            
            // Luăm datele corespunzătoare din QuickSlotManager
            if (i >= QuickSlotManager.Instance.quickSlots.Count) break;
            QuickSlot qs = QuickSlotManager.Instance.quickSlots[i];

            // Căutăm elementele interne (icon și label) prin clasele CSS
            VisualElement icon = slotUI.Q<VisualElement>(className: "item-icon");
            Label countLabel = slotUI.Q<Label>(className: "stack-count-label");

            if (qs.IsAssigned && qs.TotalCount > 0)
            {
                // Dacă avem un item asignat, îi luăm iconița din InventoryManager
                Sprite itemSprite = QuickSlotManager.Instance.GetQuickSlotIcon(i);
                
                icon.style.backgroundImage = new StyleBackground(itemSprite);
                icon.style.display = DisplayStyle.Flex;
                slotUI.style.opacity = 1f;

                // Afișăm numărul total (suma tuturor sloturilor cu același nume)
                if (qs.TotalCount > 1)
                {
                    countLabel.text = qs.TotalCount.ToString();
                    countLabel.style.display = DisplayStyle.Flex;
                }
                else
                {
                    countLabel.style.display = DisplayStyle.None;
                }
            }
            else
            {
                // Dacă slotul e gol sau itemul a fost epuizat
                icon.style.backgroundImage = null;
                icon.style.display = DisplayStyle.None;
                countLabel.style.display = DisplayStyle.None;
                
                // Opțional: putem lăsa slotul puțin transparent dacă e gol
                slotUI.style.opacity = 0.5f; 
            }
        }
    }


    private void OnPointerUpOnHotbar(PointerUpEvent evt, int hotbarIndex)
    {
        if (draggedSlot != null && draggedSlot.itemData != null)
        {
            // Asignăm numele item-ului tras către slotul de Hotbar
            QuickSlotManager.Instance.AssignToHotbar(draggedSlot.itemData.itemName, hotbarIndex);

            Debug.Log($"[UI] Item {draggedSlot.itemData.itemName} asignat la Hotbar {hotbarIndex + 1}");

            StopDrag();
            RefreshHotbarVisuals();
        }
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

    private void CreateGhostIcon()
    {
        ghostIcon = new VisualElement();
        ghostIcon.style.width = 60; // Ajustează conform design-ului tău
        ghostIcon.style.height = 60;
        ghostIcon.style.position = Position.Absolute;
        // ghostIcon.style.usageHints = UsageHints.DynamicTransform;
        ghostIcon.style.display = DisplayStyle.None;
        ghostIcon.pickingMode = PickingMode.Ignore; // Foarte important: să nu blocheze mouse-ul pentru slotul de destinație
        rootElement.Add(ghostIcon);
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
        btnDropAll = contextMenu.Q<Button>("ContextMenuDropAll");

        // Atașează callback-urile (o singură dată)
        btnUse.clicked += OnContextActionClicked; // Folosim aceeași metodă generală
        btnEquip.clicked += OnContextActionClicked; // Folosim aceeași metodă generală
        btnDrop.clicked += OnContextDropClicked;
        btnDropAll.clicked += OnContextDropAllClicked;

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
        // 1. Căutăm containerele principale din UIScreeRoot (Universul mare)
        inventoryPanel = rootElement.Q<VisualElement>("inventory-panel");
        hotbarPanel = rootElement.Q<VisualElement>("hotbar-panel");
        hotbarSlotContainer = rootElement.Q<VisualElement>("hotbar-slot-container");

        // 2. Elementele de Inventar (Căutăm ÎNĂUNTRUL inventoryPanel pentru siguranță)
        if (inventoryPanel != null)
        {
            exitButton = inventoryPanel.Q<Button>("Exit");
            slotsGridContainer = inventoryPanel.Q<VisualElement>("slots-grid-container");

            // Căutăm slotul echipat în ierarhia inventarului
            equippedToolSlot = inventoryPanel.Q<VisualElement>("equipped-tool-slot");

            if (equippedToolSlot != null)
            {
                equippedToolIcon = equippedToolSlot.Q<VisualElement>("tool-icon");
                equippedDurabilityLabel = equippedToolSlot.Q<Label>("durability-label");
                SetupEquippedSlotUI();
            }
            else
            {
                Debug.LogError("Nu am găsit 'equipped-tool-slot' în interiorul 'inventory-panel'!");
            }

            equippedToolTypeLabel = inventoryPanel.Q<Label>("tool-type-label");
        }
        else
        {
            Debug.LogError("FATAL: 'inventory-panel' nu a fost găsit în UIScreeRoot!");
        }
    }



    private void SetupEquippedSlotUI()
    {
        if (equippedToolSlot != null)
        {
            InventorySlot logicSlot = EquippedManager.Instance.GetEquippedSlot();

            // 1. START DRAG: Când tragi DIN slotul de echipare
            equippedToolSlot.RegisterCallback<PointerMoveEvent>(evt =>
            {
                if (evt.pressedButtons == 1 && draggedSlot == null)
                {
                    float dist = Vector2.Distance(pointerStartPos, evt.position);
                    if (dist > dragThreshold)
                    {
                        if (logicSlot != null && logicSlot.itemData != null)
                        {
                            HideContextMenu();
                            StartDrag(logicSlot, equippedToolSlot, evt.position);
                        }
                    }
                }
            });

            // 2. MEMORARE POZIȚIE: Necesar pentru a calcula distanța de drag
            equippedToolSlot.RegisterCallback<PointerDownEvent>(evt => 
            {
                pointerStartPos = evt.position; 
            });

            // 3. FINISH DROP: Când eliberezi mouse-ul PESTE acest slot
            equippedToolSlot.RegisterCallback<PointerUpEvent>(evt =>
            {
                // Dacă avem un item "în zbor" (draggedSlot), încercăm să-l punem aici
                if (draggedSlot != null)
                {
                    // Chemăm FinishDrop care va face verificarea de tip (Tool vs Item)
                    FinishDrop(logicSlot);
                }
                else
                {
                    // Click scurt (opțional: de exemplu pentru a vedea detalii)
                    // OnEquippedSlotClicked(logicSlot);
                }
            });

            // 4. FEEDBACK VIZUAL (Opțional dar recomandat)
            equippedToolSlot.RegisterCallback<PointerEnterEvent>(evt => 
            {
                if (logicSlot.itemData != null) 
                    ShowTooltip(logicSlot, evt.position);
            });
            
            equippedToolSlot.RegisterCallback<PointerLeaveEvent>(evt => HideTooltip());
        }
    }

    private void RegisterCallbacks()
    {
        if (exitButton != null)
        {
            exitButton.clicked += OnExitButtonClicked;
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
        // if (hotbarSlotContainer == null) return;

        // 1. Inițializăm containerele UI DOAR dacă nu au fost create deja
        if (uiSlotElements.Count == 0)
        {
            InitializeFixedGrid();
        }


        // 2. Actualizăm datele pentru fiecare slot existent
        for (int i = 0; i < InventoryManager.Instance.allSlots.Count; i++)
        {
            InventorySlot dataSlot = InventoryManager.Instance.allSlots[i];
            VisualElement uiSlot = uiSlotElements[i];

            UpdateSlotVisual(uiSlot, dataSlot);
        }

        // 3. Sincronizăm și slotul echipat
        RefreshEquippedToolUI(EquippedManager.Instance.GetEquippedSlot());
            // Actualizăm vizualele pentru Hotbar
        RefreshHotbarVisuals();
    }



    private void InitializeHotbar()
    {
        if (hotbarSlotContainer == null || QuickSlotManager.Instance == null) return;
        
        hotbarSlotContainer.Clear();
        Debug.Log("<color=yellow>[UI-Hotbar]</color> Generăm structura sloturilor...");

        for (int i = 0; i < QuickSlotManager.Instance.numberOfSlots; i++)
        {
            int index = i;
            QuickSlot qs = QuickSlotManager.Instance.quickSlots[index];

            // 1. Slotul părinte (Căsuța gri)
            VisualElement slotElement = new VisualElement();
            slotElement.AddToClassList("inventory-slot"); 
            slotElement.AddToClassList("quick-shortcut-slot"); 
            
            // ASIGURĂ-TE că are dimensiuni dacă USS-ul nu le aplică
            slotElement.style.width = 60; 
            slotElement.style.height = 60;
            slotElement.pickingMode = PickingMode.Position;

            // 2. Iconița (Elementul care ține imaginea)
            VisualElement iconElement = new VisualElement();
            iconElement.AddToClassList("item-icon");
            
            // CRITIC: Iconița trebuie să se întindă pe tot slotul!
            iconElement.style.flexGrow = 1; 
            iconElement.style.width = new Length(100, LengthUnit.Percent);
            iconElement.style.height = new Length(100, LengthUnit.Percent);
            
            iconElement.pickingMode = PickingMode.Ignore;
            slotElement.Add(iconElement);

            // 3. Label-ul pentru număr (Stack Count)
            Label countLabel = new Label();
            countLabel.AddToClassList("stack-count-label");

            countLabel.style.position = Position.Absolute;
            countLabel.style.right = 2;
            countLabel.style.bottom = 2;
            countLabel.style.fontSize = 12;
            countLabel.style.color = Color.white;
            countLabel.pickingMode = PickingMode.Ignore;
            slotElement.Add(countLabel);

            // --- CALLBACKS ---
            slotElement.RegisterCallback<PointerUpEvent>(evt =>
            {
                if (draggedSlot != null) OnPointerUpOnHotbar(evt, index);
            });

            slotElement.RegisterCallback<PointerEnterEvent>(evt =>
            {
                if (qs.IsAssigned)
                {
                    InventorySlot dummy = InventoryManager.Instance.allSlots.Find(s => s.itemData?.itemName == qs.targetItemName);
                    if (dummy != null) ShowTooltip(dummy, evt.position);
                }
            });

            slotElement.RegisterCallback<PointerLeaveEvent>(evt => HideTooltip());

            hotbarSlotContainer.Add(slotElement);
        }

        // După ce am creat "scheletul", punem pozele în el
        RefreshHotbarVisuals();
    }
    public void RefreshHotbarVisuals()
    {
        for (int i = 0; i < hotbarSlotContainer.childCount; i++)
        {
            VisualElement slotUI = hotbarSlotContainer[i];
            QuickSlot qs = QuickSlotManager.Instance.quickSlots[i];
            
            VisualElement icon = slotUI.Q<VisualElement>(className: "item-icon");
            Label countLabel = slotUI.Q<Label>(className: "stack-count-label");

            if (qs.IsAssigned && qs.TotalCount > 0)
            {
                icon.style.backgroundImage = new StyleBackground(QuickSlotManager.Instance.GetQuickSlotIcon(i));
                icon.style.display = DisplayStyle.Flex;
                countLabel.text = (qs.TotalCount > 1) ? qs.TotalCount.ToString() : "";
                countLabel.style.display = (qs.TotalCount > 1) ? DisplayStyle.Flex : DisplayStyle.None;
            }
            else
            {
                icon.style.backgroundImage = null;
                icon.style.display = DisplayStyle.None;
                countLabel.style.display = DisplayStyle.None;
            }
        }
    }


    private void UpdateSlotVisual(VisualElement uiElement, InventorySlot slot)
    {
        VisualElement icon = uiElement.Q<VisualElement>(className: "item-icon");
        Label countLabel = uiElement.Q<Label>(className: "stack-count-label");

        if (slot != null && slot.itemData != null)
        {
            icon.style.backgroundImage = new StyleBackground(slot.icon);
            icon.style.display = DisplayStyle.Flex;
            uiElement.style.opacity = 1f;

            if (slot.count > 1)
            {
                countLabel.text = slot.count.ToString();
                countLabel.style.display = DisplayStyle.Flex;
            }
            else countLabel.style.display = DisplayStyle.None;
        }
        else
        {
            // --- SLOT GOL ---
            icon.style.backgroundImage = null;
            icon.style.display = DisplayStyle.None;
            countLabel.style.display = DisplayStyle.None;
            uiElement.style.opacity = 1f; // Resetează opacitatea dacă a fost golit
        }
    }


    private void OnSlotMouseDown(MouseDownEvent evt, int slotIndex)
    {
        InventorySlot slot = InventoryManager.Instance.allSlots[slotIndex];

        if (slot.itemData == null) {
            HideContextMenu();
            return;
        }

        if (evt.button == (int)MouseButton.RightMouse || evt.button == (int)MouseButton.LeftMouse)
        {
            selectedSlot = slot;
            ShowContextMenu(slot, Input.mousePosition, isEquippedSlot: false);
        }
        evt.StopPropagation(); // Previne click-ul de a declanșa alte evenimente (ex: ascunderea meniului)
    }

    private void OnSlotPointerDown(PointerDownEvent evt, int slotIndex, VisualElement uiElement)
    {
        if (evt.button == (int)MouseButton.LeftMouse)
        {
            pointerStartPos = evt.position;
            // Nu începem StartDrag aici, așteptăm să vedem dacă se mișcă mouse-ul
        }

        // InventorySlot slot = InventoryManager.Instance.allSlots[slotIndex];
        // if (evt.button == (int)MouseButton.LeftMouse && slot.itemData != null)
        // {
        //     StartDrag(slot, uiElement, evt.position);
        // }
    }

    private void OnSlotPointerUp(PointerUpEvent evt, InventorySlot targetSlot)
    {
        if (draggedSlot != null)
        {
            // Acum targetSlot poate fi oricare: cel din inventar SAU cel din EquippedManager
            FinishDrop(targetSlot);
        }
    }


    private void InitializeFixedGrid()
    {
        slotsGridContainer.Clear();
        uiSlotElements.Clear();

        for (int i = 0; i < InventoryManager.Instance.max_slots; i++)
        {
            // 1. Creăm elementele vizuale
            VisualElement slotElement = new VisualElement();
            slotElement.AddToClassList("inventory-slot");

            VisualElement icon = new VisualElement();
            icon.AddToClassList("item-icon");
            slotElement.Add(icon);

            Label countLabel = new Label();
            countLabel.AddToClassList("stack-count-label");
            slotElement.Add(countLabel);

            // 2. Salvează indexul pentru a ști la ce slot din manager face referință
            int slotIndex = i;
            InventorySlot slot = InventoryManager.Instance.allSlots[slotIndex];

            slotElement.RegisterCallback<PointerMoveEvent>(evt =>
            {
                // Dacă am mouse-ul apăsat și nu facem deja drag
                if (evt.pressedButtons == 1 && draggedSlot == null)
                {
                    float dist = Vector2.Distance(pointerStartPos, evt.position);
                    if (dist > dragThreshold)
                    {
                        
                        if (slot.itemData != null)
                        {
                            HideContextMenu(); // ÎNCHIDEM meniul dacă cumva era deschis
                            StartDrag(slot, slotElement, evt.position);
                        }
                    }
                }
            });


            // Modificăm MouseDown în PointerUp pentru meniul contextual
            slotElement.RegisterCallback<PointerUpEvent>(evt =>
            {
                // Dacă NU facem drag, înseamnă că a fost un click scurt
                if (draggedSlot == null)
                {
                    OnSlotClickedShort(evt, slotIndex);
                }
                else
                {
                    OnSlotPointerUp(evt, slot); // Logica de Drop existentă
                }
            });

            // 3. Înregistrează EVENIMENTELE O SINGURĂ DATĂ
            slotElement.RegisterCallback<PointerDownEvent>(evt => OnSlotPointerDown(evt, slotIndex, slotElement));
            // slotElement.RegisterCallback<PointerUpEvent>(evt => OnSlotPointerUp(evt, slot));
            slotElement.RegisterCallback<PointerEnterEvent>(evt => ShowTooltip(InventoryManager.Instance.allSlots[slotIndex], evt.position));
            slotElement.RegisterCallback<PointerLeaveEvent>(evt => HideTooltip());
            // slotElement.RegisterCallback<MouseDownEvent>(evt => OnSlotMouseDown(evt, slotIndex));



            slotsGridContainer.Add(slotElement);
            uiSlotElements.Add(slotElement);
        }
    }


    private void OnSlotClickedShort(PointerUpEvent evt, int slotIndex)
    {
        InventorySlot slot = InventoryManager.Instance.allSlots[slotIndex];

        if (slot.itemData == null) {
            HideContextMenu();
            return;
        }

        // Verificăm distanța din nou ca siguranță
        float dist = Vector2.Distance(pointerStartPos, evt.position);
        if (dist < dragThreshold)
        {
            selectedSlot = slot;
            ShowContextMenu(slot, evt.position, isEquippedSlot: false);
        }
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

            // DRAG START
            slotElement.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button == (int)MouseButton.LeftMouse)
                {
                    StartDrag(inventorySlot, slotElement, evt.position);
                }
            });

            // DROP / END DRAG
            slotElement.RegisterCallback<PointerUpEvent>(evt =>
            {
                if (draggedSlot != null)
                {
                    FinishDrop(inventorySlot); // 'inventorySlot' aici este destinația
                }
            });

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
            // Permitem Drop și pe sloturi goale!FinishDrop
            slotElement.RegisterCallback<PointerUpEvent>(evt =>
            {
                if (draggedSlot != null)
                {
                    FinishDrop(null); // Drop pe un slot gol
                }
            });
        }

        // 5. Adaugă slotul la containerul Grid
        slotsGridContainer.Add(slotElement);
    }

    private void CreateEmptySlotVisual()
    {
        // Metodă ajutătoare pentru sloturile goale
        CreateSlotVisual(null);
    }
    

    private void StartDrag(InventorySlot slot, VisualElement element, Vector2 startPos)
    {
        draggedSlot = slot;
        originalSlotElement = element;

        // Setează iconița fantomă
        ghostIcon.style.backgroundImage = new StyleBackground(slot.icon);
        ghostIcon.style.display = DisplayStyle.Flex;
        ghostIcon.style.left = startPos.x - 30; // Centrează pe mouse
        ghostIcon.style.top = startPos.y - 30;

        // Opțional: Opacitate redusă pe slotul original
        element.style.opacity = 0.5f;

        // Înregistrăm mișcarea pe ROOT pentru a fi fluidă
        rootElement.RegisterCallback<PointerMoveEvent>(OnPointerMove);
        rootElement.RegisterCallback<PointerUpEvent>(OnGlobalPointerUp);
        
        HideTooltip(); // Nu vrem tooltip în timpul drag-ului
    }

    private void OnPointerMove(PointerMoveEvent evt)
    {
        if (draggedSlot == null) return;
        
        ghostIcon.style.left = evt.position.x - 30;
        ghostIcon.style.top = evt.position.y - 30;
    }

    private void FinishDrop(InventorySlot targetSlot)
    {
        if (draggedSlot == null) return;

        // --- LOGICA DE FILTRARE ---
        // Verificăm dacă încercăm să punem ceva în slotul de Equipped
        if (targetSlot == EquippedManager.Instance.GetEquippedSlot())
        {
            // Presupunând că ai o proprietate 'isTool' sau 'itemType' în ItemData
            bool isItemTool = draggedSlot.itemData is ToolItem; // Sau: draggedSlot.itemData.itemType == ItemType.Tool

            if (!isItemTool)
            {
                Debug.LogWarning("⚠️ Nu poți echipa acest obiect! Nu este o unealtă.");
                StopDrag(); // Anulăm drag-ul fără swap
                RefreshUI();
                return;
            }
        }

        // Dacă trece de verificare (sau dacă ținta e un slot normal de inventar), facem swap-ul
        InventoryManager.Instance.SwapSlots(draggedSlot, targetSlot);

        StopDrag();
        RefreshUI();
    }

    private void OnGlobalPointerUp(PointerUpEvent evt)
    {
        // Aceasta curăță drag-ul dacă dai drumul la mouse în afara unui slot valid
        if (draggedSlot != null)
        {
            StopDrag();
        }
    }

    private void StopDrag()
    {
        if (originalSlotElement != null) originalSlotElement.style.opacity = 1f;
        
        draggedSlot = null;
        originalSlotElement = null;
        ghostIcon.style.display = DisplayStyle.None;
        
        rootElement.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
        rootElement.UnregisterCallback<PointerUpEvent>(OnGlobalPointerUp);
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
            if (!(slot.itemData is ToolItem) && slot.count > 1)
            {
                btnDropAll.style.display = DisplayStyle.Flex;
            }
        }
        
        // 4. Poziționează meniul (Logica corectă pentru UI Toolkit)
        // Transformăm poziția mouse-ului în coordonate locale față de root
        Vector2 localPos = rootElement.WorldToLocal(screenPosition);

        contextMenu.style.position = Position.Absolute;
        contextMenu.style.left = localPos.x + 10;
        contextMenu.style.top = localPos.y + 10; // FĂRĂ uiHeight - y !

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
    

    private void OnContextDropAllClicked()
    {
        if (selectedSlot == null || selectedSlot.itemData == null)
        {
            HideContextMenu();
            return;
        }

        // Executăm DropAll din InventorySlot (metoda pe care am creat-o anterior)
        selectedSlot.DropAll();

        // Debug.Log($"[UI] Drop All executat pentru: {selectedSlot.itemData.itemName}");

        HideContextMenu();
        RefreshUI();
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
        if (selectedSlot == null || selectedSlot.itemData == null)
        {
            HideContextMenu();
            return;
        }

        if (selectedSlot == EquippedManager.Instance.GetEquippedSlot())
        {
            Debug.Log($"Aruncă Unealta Echipată: {selectedSlot.itemData.itemName}");
            

            EquippedManager.Instance.DropEquippedTool(1);
        }
        else
        {
            Debug.Log($"Aruncă din Inventar: {selectedSlot.itemData.itemName}");
            selectedSlot.DropOne();
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

    
    private void ShowTooltip(InventorySlot slot, Vector2 mousePosition)
    {
        // Dacă slotul e gol, ne asigurăm că ascundem orice tooltip vechi și ieșim
        if (slot == null || slot.itemData == null) 
        {
            HideTooltip();
            return;
        }

        if (tooltipPanel == null) return;

        // 1. Populare date
        tooltipNameLabel.text = slot.itemData.itemName;
        tooltipDescriptionLabel.text = slot.itemData.description;

        // 2. Poziționare inteligentă
        // Root-ul vizual al UI-ului (InventoryPanel)
        VisualElement root = tooltipPanel.parent; 
        
        // Convertim poziția mouse-ului din spațiul "ecran" în spațiul "UI local"
        Vector2 localPos = root.WorldToLocal(mousePosition);

        tooltipPanel.style.left = localPos.x + 20f; // Offset să nu fie sub deget/mouse
        tooltipPanel.style.top = localPos.y + 20f;

        // 3. Afișare
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