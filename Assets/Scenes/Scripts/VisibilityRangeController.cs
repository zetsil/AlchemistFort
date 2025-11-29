using UnityEngine;
using System.Collections.Generic;
using System.Collections; // Necesită acest namespace pentru Corutine

public class VisibilityRangeController : MonoBehaviour
{
    // --- Configurarea ---
    [Header("Setări Distanță")]
    public float activationDistance = 4f;
    
    [Header("Timer Re-evaluare")]
    [Tooltip("Intervalul de timp (în secunde) după care se verifică necesitatea re-inițializării referințelor.")]
    public float recheckInterval = 1.0f; 

    private Transform playerTransform;
    
    private MeshRenderer[] allMeshRenderers;
    private CanvasRenderer[] allCanvasRenderers;
    private Collider[] allColliders;
    
    private bool isVisible = false;
    
    // NOU: Flag-ul de control al timer-ului
    public bool shouldReinitialize = false; 

    void Awake()
    {
        // Apelăm metoda de inițializare o dată, devreme
        InitializeReferences();
        
        SetVisibility(false);
        
        // NOU: Pornim corutina de timer
        StartCoroutine(ReinitializeTimerRoutine());
    }

    // NOU: Metodă publică pentru a semnala necesitatea re-inițializării (apelată, de exemplu, de NewActionUIGenerator)
    public void RequestReinitialization()
    {
        shouldReinitialize = true;
    }
    
    // NOU: Corutina care verifică și re-inițializează la intervalul specificat
    private IEnumerator ReinitializeTimerRoutine()
    {
        while (true) // Buclă infinită, rulează pe toată durata de viață a obiectului
        {
            // Așteptăm intervalul specificat (ex: 1.0 secundă)
            yield return new WaitForSeconds(recheckInterval); 

            // Verificăm flag-ul
            if (shouldReinitialize)
            {
                // 1. Resetăm flag-ul
                shouldReinitialize = false; 

                // 2. Re-colectăm referințele (butoanele UI/3D)
                InitializeReferences();
                
                // 3. Re-aplicăm starea curentă de vizibilitate
                SetVisibility(isVisible); 
            }
        }
    }

    // NOU: Metodă publică care înlocuiește logica din Awake()
    public void InitializeReferences()
    {
        // 1. Găsim Player-ul (Doar dacă nu l-am găsit deja)
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
            else
            {
                Debug.LogError("Player-ul nu a fost găsit! Asigură-te că obiectul Player are Tag-ul 'Player'.");
            }
        }

        // 2. Colectăm referințele la Mesh/Canvas/Collider-e
        allMeshRenderers = GetComponentsInChildren<MeshRenderer>(true);
        allCanvasRenderers = GetComponentsInChildren<CanvasRenderer>(true);
        allColliders = GetComponentsInChildren<Collider>(true);
    }

    
    void Update()
    {
        if (playerTransform == null) return;

        float sqrDistance = (playerTransform.position - transform.position).sqrMagnitude;
        float sqrActivationDistance = activationDistance * activationDistance;

        if (sqrDistance < sqrActivationDistance)
        {
            if (!isVisible)
            {
                SetVisibility(true);
            }
        }
        else
        {
            if (isVisible)
            {
                SetVisibility(false);
            }
        }
    }
    
    // ... (Metoda SetVisibility rămâne neschimbată) ...

    private void SetVisibility(bool visible)
    {
        isVisible = visible;
        
        // 1. ITERĂM PRIN MeshRenderer-ii (pentru obiecte 3D)
        if (allMeshRenderers != null)
        {
            foreach (var mr in allMeshRenderers)
            {
                mr.enabled = visible;
            }
        }
        
        // 2. ITERĂM PRIN CanvasRenderer-ii (pentru elementele UI)
        if (allCanvasRenderers != null)
        {
            foreach (var cr in allCanvasRenderers)
            {
                cr.SetAlpha(visible ? 1f : 0f);
            }
        }
        
        // 3. ITERĂM PRIN Collider-ii (pentru interacțiuni 3D și UI)
        if (allColliders != null)
        {
            foreach (var col in allColliders)
            {
                col.enabled = visible;
            }
        }
    }
}