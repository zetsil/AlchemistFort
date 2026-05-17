using System.Collections;
using UnityEngine;

/// <summary>
/// Ascultă GlobalEvents.OnHitstopRequested și gestionează Time.timeScale global.
/// Rezolvă suprapunerea hitstop-urilor când lovești mai mulți inamici simultan:
/// ia întotdeauna cel mai puternic hitstop activ, nu cumuleazã duratele.
/// 
/// Setup: adaugă scriptul pe un GameObject din scenă, sau lasă-l să se
/// creeze singur — Enemy.Start() îl instanțiază automat dacă lipsește.
/// </summary>
public class HitstopManager : MonoBehaviour
{
    public static HitstopManager Instance { get; private set; }

    // Numărul de cereri de hitstop active în momentul curent
    private int activeRequests;

    // Cel mai restrictiv timeScale cerut de cererile active
    private float strongestTimeScale = 1f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        GlobalEvents.OnHitstopRequested += HandleHitstopRequest;
    }

    private void OnDisable()
    {
        GlobalEvents.OnHitstopRequested -= HandleHitstopRequest;
    }

    private void HandleHitstopRequest(float duration, float timeScale)
    {
        activeRequests++;

        // Aplicăm timeScale-ul doar dacă e mai puternic decât cel curent activ
        if (timeScale < strongestTimeScale)
        {
            strongestTimeScale = timeScale;
            Time.timeScale = strongestTimeScale;
        }

        StartCoroutine(ExpireRequest(duration, timeScale));
    }

    private IEnumerator ExpireRequest(float duration, float timeScale)
    {
        yield return new WaitForSecondsRealtime(duration);

        activeRequests = Mathf.Max(0, activeRequests - 1);

        if (activeRequests == 0)
        {
            // Ultima cerere expirat — revenim la normal
            strongestTimeScale = 1f;
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;
        }
        // Dacă mai sunt cereri active, lăsăm timeScale-ul lor să expire natural
    }

    private void OnDestroy()
    {
        if (Instance != this) return;
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
    }
}