using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using System;
using System.Linq;

[System.Serializable]
public class ActionLevel
{
    [Tooltip("List of Recipe")]
    public List<ActionRecipeSO> recipes = new List<ActionRecipeSO>();
}

public class NewActionUIGenerator : MonoBehaviour
{
    [Header("Definiții Acțiuni (Pe Nivel)")]
    public List<ActionLevel> actionLevels = new List<ActionLevel>();

    [Header("Stare Curentă")]
    [Range(0, 10)] 
    public int currentActionLevel = 0;

    [Header("Referințe UI")]
    public GameObject actionButtonPrefab; 

    private Transform uiContainer;
    private Transform playerTransform;

    [Header("Setări Container UI")]
    [Tooltip("Poziția relativă a UI-ului față de obiectul părinte (în unități World Space).")]
    public Transform uiAnchor;
    

    void Awake()
    {
        uiContainer = GetOrCreateUIContainer();
    }


    void Start()
    {
        // Păstrăm logica de găsire a Player-ului și a Container-ului
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
        else
        {
            Debug.LogError("[ActionUIGenerator] Nu a fost găsit 'Player' pentru orientarea UI-ului!");
        }

        // uiContainer = GetOrCreateUIContainer(); 
        GenerateActionButtons();
    }

    // Păstrăm LateUpdate pentru orientarea UI-ului
    void LateUpdate()
    {
        if (uiContainer != null && playerTransform != null)
        {
            Vector3 directionToPlayer = playerTransform.position - uiContainer.position;
            directionToPlayer.y = 0; 

            if (directionToPlayer.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(-directionToPlayer);
                Vector3 eulerAngles = targetRotation.eulerAngles;
                uiContainer.rotation = Quaternion.Euler(0, eulerAngles.y, 0);
            }
        }
    }
    
    public void SetActionLevel(int newLevel)
    {
        if (newLevel >= 0 && newLevel < actionLevels.Count)
        {
            if (currentActionLevel != newLevel)
            {
                currentActionLevel = newLevel;
                GenerateActionButtons();
            }
        }
        else if (actionLevels.Count > 0)
        {
            currentActionLevel = 0;
            GenerateActionButtons();
        }
    }


    private Transform GetOrCreateUIContainer()
    {
        GameObject go = new GameObject("UI_Action_Container");
        Transform container; 

            
        go.transform.SetParent(transform, false);

        // --- FIX PENTRU POZIȚIE (DISTANȚĂ) ---
        // Vrem ca UI-ul să fie la 2 metri reali deasupra, indiferent de cât de mare e obiectul.
        // Împărțim distanța dorită (2f) la scara Y a părintelui.
        float desiredHeight = 2f;
        float compensatedY = desiredHeight / transform.lossyScale.y;
        go.transform.localPosition = new Vector3(0, compensatedY, 0);

        go.transform.localRotation = Quaternion.identity;

        // --- FIX PENTRU SCARĂ (DIMENSIUNE) ---
        // Vrem ca UI-ul să aibă scara globală 0.005f.
        // Împărțim scara dorită la scara globală a părintelui (lossyScale).
        float targetWorldScale = 0.005f;
        Vector3 parentScale = transform.lossyScale;
        
        // Verificăm să nu împărțim la 0
        if (parentScale.x == 0) parentScale.x = 1;
        if (parentScale.y == 0) parentScale.y = 1;
        if (parentScale.z == 0) parentScale.z = 1;

        go.transform.localScale = new Vector3(
            targetWorldScale / parentScale.x,
            targetWorldScale / parentScale.y,
            targetWorldScale / parentScale.z
        );

        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        go.AddComponent<CanvasScaler>();
        go.AddComponent<GraphicRaycaster>();

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(400, 100); 
        rt.pivot = new Vector2(0.5f, 0.5f); 

        HorizontalLayoutGroup hlg = go.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.spacing = 200f; 
        hlg.childForceExpandHeight = false; 
        hlg.childForceExpandWidth = false;
        hlg.childControlHeight = true;
        hlg.childControlWidth = true;
        hlg.padding = new RectOffset(10, 10, 10, 10);

        container = go.transform;
        


        if (uiAnchor != null) 
        {
            container.SetParent(uiAnchor, true);
            container.position = uiAnchor.position;
        }
        
        return container;
    }


    // Logica esențială de generare (Simplificată)
    public void GenerateActionButtons()
    {
        if (uiContainer == null) return;
        
        // Curăță butoanele existente
        foreach (Transform child in uiContainer)
        {
            Destroy(child.gameObject);
        }
        
        if (currentActionLevel < 0 || currentActionLevel >= actionLevels.Count)
        {
            Debug.LogWarning($"Nivelul curent {currentActionLevel} este invalid. Nu se generează butoane.");
            return;
        }

        List<ActionRecipeSO> currentRecipes = actionLevels[currentActionLevel].recipes;
        
        // Referința la acest GameObject (inițiatorul)
        GameObject initiator = this.gameObject;

        foreach (var recipe in currentRecipes)
        {
            // Verificăm doar dacă rețeta și logica sunt setate
            if (recipe == null || recipe.actionLogic == null) continue;

            // 1. Instanțiază butonul de UI
            GameObject buttonInstance = Instantiate(actionButtonPrefab, uiContainer);
            
            // 2. Adaugă Collider-ul (logica ta rămâne OK)
            RectTransform buttonRect = buttonInstance.GetComponent<RectTransform>();
            BoxCollider collider = buttonInstance.GetComponent<BoxCollider>();

            if (collider == null && buttonRect != null)
            {
                collider = buttonInstance.AddComponent<BoxCollider>();
                collider.size = new Vector3(buttonRect.sizeDelta.x, buttonRect.sizeDelta.y, 1f);
                collider.center = Vector3.zero; 
                collider.isTrigger = true;
            }

            // 3. Configurează Butonul de UI (Vizual și Funcționalitate)
            Button uiButton = buttonInstance.GetComponent<Button>();
            ActionButtonUI buttonUI = buttonInstance.GetComponent<ActionButtonUI>(); // Presupunem că ai o clasă helper
            buttonUI.SetupExecutor(recipe.actionLogic,recipe);
            
            if (uiButton != null)
            {
                uiButton.onClick.RemoveAllListeners();
                
                uiButton.onClick.AddListener(() =>
                {
                    bool executionSuccess = recipe.InitializeAction(initiator);
                    if (executionSuccess)
                    {
                        // Acțiunea a fost inițializată/executată cu succes (LogicBuildingProgressSO a avansat, etc.)
                        buttonUI.isActionComplete = true;
                        Debug.Log($"Acțiunea '{recipe.actionName}' a fost executată cu succes.");
                    }
                    else
                    {
                        // Acțiunea a eșuat (ex: lipsă resurse, condiție "isFinished" era deja True)
                        Debug.LogWarning($"Acțiunea '{recipe.actionName}' a eșuat. Verifică logica SO-ului.");
                    }
                });
            }
            if (buttonUI != null)
            {
                buttonUI.SetVisuals(recipe.actionIcon, recipe.actionName); 
            }
        }
    }
}