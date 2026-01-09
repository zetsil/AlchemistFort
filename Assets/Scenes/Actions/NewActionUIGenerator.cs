using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using System;

[System.Serializable]
public class ActionLevel
{
    [Tooltip("List of Recipe")]
    public List<ActionRecipeSO> recipes = new List<ActionRecipeSO>();
}

public class NewActionUIGenerator : MonoBehaviour
{
    [Header("Prioritate 1: Anchor Manual")]
    [Tooltip("Dacă e setat, UI-ul se va lipi exact de acest obiect.")]
    public Transform uiAnchor;

    [Header("Prioritate 2: Mod Item Pick Up")]
    [Tooltip("Dacă e bifat și nu există Anchor, pune UI-ul deasupra centrului la înălțimea de mai jos.")]
    public bool uiForItemPickUp = false;
    public float customUIHeight = 0.8f;

    [Header("Definiții Acțiuni (Pe Nivel)")]
    public List<ActionLevel> actionLevels = new List<ActionLevel>();

    [Header("Stare Curentă")]
    [Range(0, 10)] 
    public int currentActionLevel = 0;

    [Header("Referințe UI")]
    public GameObject actionButtonPrefab; 

    private Transform uiContainer;
    private Transform playerTransform;

    void Awake()
    {
        uiContainer = GetOrCreateUIContainer();
    }

    void Start()
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null) playerTransform = player.transform;
        else Debug.LogError("[ActionUIGenerator] Nu a fost găsit 'Player'!");

        GenerateActionButtons();
    }

    void LateUpdate()
    {
        if (playerTransform == null) return;

        Transform targetToRotate = null;

        // --- LOGICĂ POZIȚIONARE ---
        if (uiAnchor != null)
        {
            // Dacă avem ancoră, nu-i modificăm poziția prin cod (LateUpdate). 
            // Ea stă unde a fost pusă în Prefab.
            targetToRotate = uiAnchor;
        }
        else
        {
            // Dacă NU avem ancoră, containerul creat trebuie poziționat deasupra
            targetToRotate = uiContainer;
            float height = uiForItemPickUp ? customUIHeight : 2.0f;
            targetToRotate.position = transform.position + Vector3.up * height;
        }

        // --- LOGICĂ BILLBOARD (ROTAȚIE) ---
        if (targetToRotate != null)
        {
            Vector3 directionToPlayer = playerTransform.position - targetToRotate.position;
            directionToPlayer.y = 0; 

            if (directionToPlayer.sqrMagnitude > 0.001f)
            {
                targetToRotate.rotation = Quaternion.LookRotation(-directionToPlayer, Vector3.up);
            }
        }
    }

    private Transform GetOrCreateUIContainer()
    {
        GameObject go = new GameObject("UI_Action_Container");
        
        // --- PARENTING ȘI RESET ---
        if (uiAnchor != null)
        {
            // Dacă avem ancoră, punem UI-ul sub ea și îl resetăm la 0 local
            go.transform.SetParent(uiAnchor, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
        }
        else
        {
            // Dacă nu avem ancoră, îl punem sub obiectul principal
            go.transform.SetParent(transform, false);
        }

        // --- LOGICĂ DE SCARĂ (PĂSTRATĂ NEMODIFICATĂ) ---
        float targetWorldScale = 0.005f;
        Vector3 parentScale = transform.lossyScale;
        if (parentScale.x == 0) parentScale.x = 1;
        if (parentScale.y == 0) parentScale.y = 1;
        if (parentScale.z == 0) parentScale.z = 1;

        go.transform.localScale = new Vector3(
            targetWorldScale / parentScale.x,
            targetWorldScale / parentScale.y,
            targetWorldScale / parentScale.z
        );

        // --- CONFIGURARE CANVAS ---
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
        hlg.childControlHeight = true;
        hlg.childControlWidth = true;

        return go.transform;
    }

    // --- LOGICA TA ORIGINALĂ PENTRU BUTOANE (IDENTICĂ) ---
    public void GenerateActionButtons()
    {
        if (uiContainer == null) return;

        foreach (Transform child in uiContainer)
        {
            Destroy(child.gameObject);
        }

        if (currentActionLevel < 0 || currentActionLevel >= actionLevels.Count) return;

        List<ActionRecipeSO> currentRecipes = actionLevels[currentActionLevel].recipes;
        GameObject initiator = this.gameObject;

        foreach (var recipe in currentRecipes)
        {
            if (recipe == null || recipe.actionLogic == null) continue;

            GameObject buttonInstance = Instantiate(actionButtonPrefab, uiContainer);

            RectTransform buttonRect = buttonInstance.GetComponent<RectTransform>();
            BoxCollider collider = buttonInstance.GetComponent<BoxCollider>();

            if (collider == null && buttonRect != null)
            {
                collider = buttonInstance.AddComponent<BoxCollider>();
                collider.size = new Vector3(buttonRect.sizeDelta.x, buttonRect.sizeDelta.y, 1f);
                collider.center = Vector3.zero;
                collider.isTrigger = true;
            }

            Button uiButton = buttonInstance.GetComponent<Button>();
            ActionButtonUI buttonUI = buttonInstance.GetComponent<ActionButtonUI>(); 
            buttonUI.SetupExecutor(recipe.actionLogic, recipe);

            if (uiButton != null)
            {
                uiButton.onClick.RemoveAllListeners();
                uiButton.onClick.AddListener(() =>
                {
                    bool executionSuccess = recipe.InitializeAction(initiator);
                    if (executionSuccess)
                    {
                        buttonUI.isActionComplete = true;
                        Debug.Log($"Acțiunea '{recipe.actionName}' executată.");
                    }
                });
            }
            if (buttonUI != null)
            {
                buttonUI.SetVisuals(recipe.actionIcon, recipe.actionName);
            }
        }
    }

    public void SetActionLevel(int newLevel)
    {
        if (newLevel >= 0 && newLevel < actionLevels.Count)
        {
            currentActionLevel = newLevel;
            GenerateActionButtons();
        }
    }
}