using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Linq;

/// <summary>
/// Gestionează logica de timp pentru Nivelul 1 (Bonfire Active).
/// Oprește efectele de foc/lumină și resetează nivelul când timpul expiră.
/// </summary>
public class BonfireTimerManager : MonoBehaviour
{
    [Header("Referințe Script Principal")]
    [Tooltip("Referința la componenta NewActionUIGenerator.")]
    public NewActionUIGenerator NewActionUIGenerator;

    [Header("Setări Timer")]
    [Tooltip("Durata inițială a nivelului 1, folosită la aprinderea focului.")]
    public float levelOneDuration = 10f;
    [Tooltip("Durata maximă pe care o poate atinge timer-ul (ex: 60 secunde).")]
    public float maxTimeDuration = 60f; // NOU: Limită maximă
    
    [Header("Referințe UI World Space")]
    [Tooltip("Containerul UI (Canvas/GameObject) care deține bara de progres.")]
    public GameObject uiContainerParent;
    [Tooltip("Bara de progres UI (Image Fill) care va fi afișată (componenta Image).")]
    public Image timerFillImage; 

    [Header("Efecte de Oprit (Bonfire)")]
    [Tooltip("Obiectul (GameObject) care reprezintă Particulele de Foc.")]
    public GameObject bonfireParticles; 
    
    [Tooltip("Obiectul (GameObject) care reprezintă Sursa de Lumină a Focului.")]
    public GameObject bonfireLight; 

    private bool isTimerRunning = false;
    private Coroutine timerCoroutine;
    private float currentTimerTime = 0f; // NOU: Timpul real rămas


    [Tooltip("Obiectul care conține Shader-ul de Shield (Energy Field).")]
    public GameObject energyField;

    void Start()
    {
        if (NewActionUIGenerator == null)
        {
            NewActionUIGenerator = FindObjectOfType<NewActionUIGenerator>();
            if (NewActionUIGenerator == null)
            {
                Debug.LogError("[BonfireTimerManager] NewActionUIGenerator nu a fost găsit în scenă.");
                enabled = false;
                return;
            }
        }

        if (uiContainerParent != null)
        {
            uiContainerParent.SetActive(false);
        }
        
        if (energyField != null) energyField.SetActive(false);
    }

    void Update()
    {
        CheckLevelState();
    }

    private void CheckLevelState()
    {
        // 1. Intră în Nivelul 1: Pornire Timer
        if (NewActionUIGenerator.currentActionLevel == 1 && !isTimerRunning)
        {
            StartLevelOneTimer();
        }
        // 2. Ieși din Nivelul 1: Oprire Timer (manual)
        else if (NewActionUIGenerator.currentActionLevel != 1 && isTimerRunning)
        {
            StopLevelOneTimer(false); 
        }
    }

    // =========================================================================
    // METODĂ PUBLICĂ PENTRU ADĂUGAREA TIMPULUI
    // =========================================================================
    /// <summary>
    /// Adaugă o durată specifică la timpul rămas, dar funcționează DOAR dacă timer-ul rulează.
    /// </summary>
    /// <param name="timeToAdd">Durata în secunde de adăugat.</param>
    public void AddTimeToTimer(float timeToAdd)
    {
        // Regula: Se adaugă timp doar dacă focul este aprins (timer-ul rulează).
        if (!isTimerRunning)
        {
            Debug.LogWarning("[BonfireTimerManager] Nu s-a putut adăuga timp. Focul nu este aprins.");
            return;
        }

        currentTimerTime += timeToAdd;
        
        // Aplică limita maximă
        if (currentTimerTime > maxTimeDuration)
        {
            currentTimerTime = maxTimeDuration;
        }
        
        Debug.Log($"[BonfireTimerManager] Timp adăugat: {timeToAdd}s. Total: {currentTimerTime:F2}s");

        // Actualizează imediat bara de UI
        if (timerFillImage != null)
        {
            timerFillImage.fillAmount = currentTimerTime / maxTimeDuration;
        }
    }
    // =========================================================================

    private void StartLevelOneTimer()
    {
        if (isTimerRunning) return;

        isTimerRunning = true;
        Debug.Log("[BonfireTimerManager] Nivelul 1 (Bonfire) a început. Pornire Timer...");

        // Setează timpul inițial (dacă nu e deja setat prin AddTimeToTimer într-un scenariu special)
        if (currentTimerTime <= 0)
        {
            currentTimerTime = levelOneDuration;
        }

        // Afișează containerul UI al barei de progres
        if (uiContainerParent != null)
        {
            uiContainerParent.SetActive(true);
        }

        // Activează efectele
        SetBonfireEffectsActive(true);

        // Pornește Corutina
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
        }
        timerCoroutine = StartCoroutine(CountdownRoutine());
        
        
    }

    private void StopLevelOneTimer(bool expired)
    {
        if (!isTimerRunning) return;

        isTimerRunning = false;
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
            timerCoroutine = null;
        }

        if (uiContainerParent != null)
        {
            uiContainerParent.SetActive(false);
        }

        if (expired)
        {
            Debug.Log("[BonfireTimerManager] Timer expirat! Bonfire stins și nivel resetat.");
            SetBonfireEffectsActive(false);
            NewActionUIGenerator.SetActionLevel(0);
            currentTimerTime = 0f; // Asigură că timpul este zero
        } 
        else
        {
            Debug.Log("[BonfireTimerManager] Timer oprit manual (Nivelul a fost schimbat).");
        }
    }

    private IEnumerator CountdownRoutine()
    {
        // Loop-ul merge cât timp currentTimerTime > 0
        while (currentTimerTime > 0)
        {
            yield return null; 

            currentTimerTime -= Time.deltaTime;
            
            // Actualizează vizual bara de UI (raportat la timpul maxim)
            if (timerFillImage != null)
            {
                timerFillImage.fillAmount = currentTimerTime / maxTimeDuration;
            }
        }
        
        currentTimerTime = 0f; 
        StopLevelOneTimer(true);
    }

    private void SetBonfireEffectsActive(bool active)
    {
        if (bonfireParticles != null)
        {
            bonfireParticles.SetActive(active);
        }

        if (bonfireLight != null)
        {
            bonfireLight.SetActive(active);
        }
        
        if (energyField != null)
        {
            energyField.SetActive(active);
            
            // Opțional: Dacă vrei un efect de particule când apare scutul
            if (active) GlobalEvents.RequestParticle("Shield_Start_Effect", energyField.transform.position);
        }
    }
}