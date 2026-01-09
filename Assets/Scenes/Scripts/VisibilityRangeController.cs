using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class VisibilityRangeController : MonoBehaviour
{
    [Header("Setări Distanță")]
    public float activationDistance = 4f;
    
    [Header("Mod Vizibilitate")]
    public bool hideOnlyInteractionButtons = false;

    [Header("Timer Re-evaluare")]
    public float recheckInterval = 1.0f; 

    private Transform playerTransform;
    
    // Liste pentru referințe
    private MeshRenderer[] allMeshRenderers;
    private CanvasRenderer[] allCanvasRenderers;
    private Collider[] allColliders;
    
    // Listă pentru butoane specifice
    private ActionButtonUI[] interactionButtons;
    
    private bool isVisible = false;
    public bool shouldReinitialize = false; 

    void Awake()
    {
        // InitializeReferences();
        // SetVisibility(false);
        // StartCoroutine(ReinitializeTimerRoutine());
    }

    void Start()
    {
        InitializeReferences();
        SetVisibility(false);
        StartCoroutine(ReinitializeTimerRoutine());
    }

    public void RequestReinitialization()
    {
        shouldReinitialize = true;
    }
    
    private IEnumerator ReinitializeTimerRoutine()
    {
        while (true) 
        {
            yield return new WaitForSeconds(recheckInterval); 
            if (shouldReinitialize)
            {
                shouldReinitialize = false; 
                InitializeReferences();
                SetVisibility(isVisible); 
            }
        }
    }

    public void InitializeReferences()
    {
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTransform = player.transform;
        }

        // Colectăm tot pentru modul "Hide All"
        allMeshRenderers = GetComponentsInChildren<MeshRenderer>(true);
        allCanvasRenderers = GetComponentsInChildren<CanvasRenderer>(true);
        allColliders = GetComponentsInChildren<Collider>(true);

        // Colectăm butoanele pentru modul "Only Icons"
        interactionButtons = GetComponentsInChildren<ActionButtonUI>(true);
    }

    void Update()
    {
        if (playerTransform == null) return;

        float sqrDistance = (playerTransform.position - transform.position).sqrMagnitude;
        float sqrActivationDistance = activationDistance * activationDistance;

        if (sqrDistance < sqrActivationDistance)
        {
            if (!isVisible) SetVisibility(true);
        }
        else
        {
            if (isVisible) SetVisibility(false);
        }
    }

    private void SetVisibility(bool visible)
    {
        isVisible = visible;
        float alpha = visible ? 1f : 0f;

        if (hideOnlyInteractionButtons)
        {
            // --- MOD: DOAR BUTOANELE ---
            if (interactionButtons != null)
            {
                foreach (var btn in interactionButtons)
                {
                    if (btn == null) continue;

                    // Ascundem elementele grafice ale butonului (inclusiv copii)
                    CanvasRenderer[] btnRenderers = btn.GetComponentsInChildren<CanvasRenderer>(true);
                    foreach (var cr in btnRenderers)
                    {
                        cr.SetAlpha(alpha);
                    }

                    // Dezactivăm coliziunea butonului ca să nu poată fi apăsat când e invizibil
                    Collider btnCol = btn.GetComponent<Collider>();
                    if (btnCol != null) btnCol.enabled = visible;
                }
            }
        }
        else
        {
            // --- MOD: TOT OBIECTUL (Default) ---
            if (allMeshRenderers != null)
                foreach (var mr in allMeshRenderers) mr.enabled = visible;
            
            if (allCanvasRenderers != null)
                foreach (var cr in allCanvasRenderers) cr.SetAlpha(alpha);
            
            if (allColliders != null)
                foreach (var col in allColliders) col.enabled = visible;
        }
    }
}