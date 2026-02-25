using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections;

public class ToxicityListener : MonoBehaviour
{
    [Header("Setări Damage")]
    public float damageAmount = 10f;
    public float damageInterval = 2f;
    
    [Header("Visual Effects - Particles")]
    public ParticleSystem gasParticles;

    [Header("Visual Effects - Camera Tint")]
    public Volume globalVolume;
    public Color toxicColor = new Color(1f, 0.9f, 0f, 1f); 
    public float colorTransitionSpeed = 2f;

    private PlayerStats cachedPlayer;
    private Coroutine toxicityCoroutine;
    private Coroutine colorFadeCoroutine;
    private ColorAdjustments colorAdjustments;
    private Color defaultColor = Color.white;

    private void Awake()
    {
        if (globalVolume != null && globalVolume.profile.TryGet(out colorAdjustments))
        {
            // Forțăm activarea override-ului pentru a fi siguri că bifa e pusă
            colorAdjustments.colorFilter.overrideState = true;
            colorAdjustments.colorFilter.value = defaultColor;
        }
    }

    private void Start()
    {
        // Verificăm starea inițială la spawn
        if (GameStateManager.Instance != null)
        {
            if (GameStateManager.Instance.IsNight) StartGas();
            else ForceStopEverything();
        }
    }

    private void OnEnable()
    {
        GlobalEvents.OnNightStart += StartGas;
        GlobalEvents.OnDayStart += StopGas;
    }

    private void OnDisable()
    {
        GlobalEvents.OnNightStart -= StartGas;
        GlobalEvents.OnDayStart -= StopGas;
        StopGas();
    }

    private void ForceStopEverything()
    {
        if (gasParticles != null)
            gasParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        if (colorAdjustments != null)
            colorAdjustments.colorFilter.value = defaultColor;
        
        if (toxicityCoroutine != null)
        {
            StopCoroutine(toxicityCoroutine);
            toxicityCoroutine = null;
        }
    }

    private void StartGas()
    {
        // CHECK: Executăm DOAR dacă GameStateManager confirmă că este noapte
        if (GameStateManager.Instance == null || !GameStateManager.Instance.IsNight) return;

        if (cachedPlayer == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) cachedPlayer = playerObj.GetComponent<PlayerStats>();
        }

        if (toxicityCoroutine == null)
        {
            GlobalEvents.NotifyToxicGasStart();
            
            if (gasParticles != null) gasParticles.Play();

            if (colorFadeCoroutine != null) StopCoroutine(colorFadeCoroutine);
            colorFadeCoroutine = StartCoroutine(FadeCameraColor(toxicColor));

            toxicityCoroutine = StartCoroutine(ApplyDamageRoutine());
            
            Debug.Log("<color=yellow>[Toxicity] Night confirmed. Starting effects.</color>");
        }
    }

    private void StopGas()
    {
        // CHECK: Executăm oprirea DOAR dacă GameStateManager confirmă că este zi
        // (sau dacă forțăm oprirea la OnDisable)
        if (GameStateManager.Instance != null && GameStateManager.Instance.IsNight) return;

        if (toxicityCoroutine != null)
        {
            GlobalEvents.NotifyToxicGasStop();

            if (gasParticles != null) gasParticles.Stop();

            if (colorFadeCoroutine != null) StopCoroutine(colorFadeCoroutine);
            colorFadeCoroutine = StartCoroutine(FadeCameraColor(defaultColor));

            StopCoroutine(toxicityCoroutine);
            toxicityCoroutine = null;

            Debug.Log("<color=green>[Toxicity] Day confirmed. Stopping effects.</color>");
        }
    }

    private IEnumerator ApplyDamageRoutine()
    {
        yield return new WaitForSeconds(1f);
        while (true)
        {
            // Verificare suplimentară în buclă pentru siguranță maximă
            if (GameStateManager.Instance != null && !GameStateManager.Instance.IsNight)
            {
                StopGas();
                yield break;
            }

            if (cachedPlayer != null && !cachedPlayer.isDead)
            {
                cachedPlayer.TakeToxicDamage(damageAmount);
            }
            yield return new WaitForSeconds(damageInterval);
        }
    }

    private IEnumerator FadeCameraColor(Color targetColor)
    {
        if (colorAdjustments == null) yield break;

        Color startColor = colorAdjustments.colorFilter.value;
        float time = 0;

        while (time < 1f)
        {
            time += Time.deltaTime * colorTransitionSpeed;
            colorAdjustments.colorFilter.value = Color.Lerp(startColor, targetColor, time);
            yield return null;
        }
        colorAdjustments.colorFilter.value = targetColor;
    }
}