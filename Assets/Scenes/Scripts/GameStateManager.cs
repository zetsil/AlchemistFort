using UnityEngine;
using System.Collections;

/// <summary>
/// Singleton global care gestionează starea curentă a jocului (Zi/Noapte) și ciclul de timp.
/// Declanșează evenimente globale (prin GlobalEvents) la schimbarea stării.
/// </summary>
public class GameStateManager : MonoBehaviour
{

    public static GameStateManager Instance { get; private set; }
    
    [Header("Setări Timp")]
    [Tooltip("Durata unei zile în secunde")]
    public float dayDuration = 300f; // 5 minute
    [Tooltip("Durata unei nopți în secunde")]
    public float nightDuration = 180f; // 3 minute
    
    public enum GameState { Day, Night }
    
    [Header("Stare Curentă")]
    [SerializeField] private GameState currentState = GameState.Day;
    [SerializeField] public float timeRemaining;

    public bool IsNight => currentState == GameState.Night;
    private Coroutine cycleCoroutine;

    [Header("Runtime Status")]
    public bool isRestoringFromSave = false; // Steag pentru a preveni logica de "New Day"


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        StartCycle(GameState.Day); 
    }
    
    public void SetStateManually(GameState newState, float remainingTime)
    {
        if (cycleCoroutine != null) StopCoroutine(cycleCoroutine);

        // Activăm steagul de restaurare
        isRestoringFromSave = true;

        currentState = newState;
        timeRemaining = remainingTime;

        // Notificăm sistemele
        if (currentState == GameState.Day) GlobalEvents.NotifyDayStart();
        else GlobalEvents.NotifyNightStart();

        // Repornim timer-ul
        cycleCoroutine = StartCoroutine(CycleTimerCoroutine());
        
        // Resetăm steagul după ce evenimentele au fost procesate
        // Folosim un mic delay sau pur și simplu la finalul frame-ului
        StartCoroutine(ResetRestoringFlag());

        Debug.Log($"<color=yellow>[GameStateManager] Restaurare: {newState}, Timp: {remainingTime}s. Wave progression blocked.</color>");
    }


    private IEnumerator ResetRestoringFlag()
    {
        yield return new WaitForEndOfFrame();
        isRestoringFromSave = false;
    }

    
    private void StartCycle(GameState initialState)
    {
        currentState = initialState;
        timeRemaining = (initialState == GameState.Day) ? dayDuration : nightDuration;

        // Asigură-te că evenimentul inițial este declanșat
        if (initialState == GameState.Day)
        {
            GlobalEvents.NotifyDayStart();
            Debug.Log("☀️ Ciclul de timp a început. Este Zi.");
        }
        else
        {
            GlobalEvents.NotifyNightStart();
            Debug.Log("🌙 Ciclul de timp a început. Este Noapte.");
        }

        StartCoroutine(CycleTimerCoroutine());
    }

    private IEnumerator CycleTimerCoroutine()
    {
        while (true)
        {
            yield return null; 

            // Scade timpul rămas
            timeRemaining -= Time.deltaTime;
            
            // Calculează procentul rămas din ciclul curent (pentru UI)
            float totalDuration = (currentState == GameState.Day) ? dayDuration : nightDuration;
            float percentRemaining = Mathf.Clamp01(timeRemaining / totalDuration);
            
            // Notifică UI-ul și alte sisteme care au nevoie de timer
            GlobalEvents.NotifyTimeUpdate(percentRemaining); 

            // Dacă timpul s-a terminat, schimbă starea
            if (timeRemaining <= 0)
            {
                ChangeState();
            }
        }
    }

    private void ChangeState()
    {
        if (currentState == GameState.Day)
        {
            // Trecerea de la Zi la Noapte
            currentState = GameState.Night;
            timeRemaining = nightDuration;

            GlobalEvents.NotifyNightStart();
            Debug.Log("☀️ A început Noaptea! (Invoking OnDayStart)");
        }
        else // currentState == GameState.Night
        {
            currentState = GameState.Day;
            timeRemaining = dayDuration;

            Debug.Log("☀️ A început Ziua! (Invoking OnDayStart)");
            GlobalEvents.NotifyDayStart();
        }
    }


    public void SkipTime()
    {
        timeRemaining = 0;
    }
}