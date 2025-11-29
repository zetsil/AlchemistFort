using UnityEngine;
using System;

public class DayNightVisualController : MonoBehaviour
{
    [Header("Referințe")]
    public Light sunLight;
    [Tooltip("Camera principală. Dacă e null, o caută automat.")]
    public Camera mainCamera;

    [Header("Setări Zi")]
    public float dayIntensity = 1f;
    public Color dayAmbientColor = Color.grey;
    public float dayFogDistance = 150f;
    
    [Header("Setări Noapte")]
    public float nightIntensity = 0.1f;
    public Color nightAmbientColor = new Color(0.1f, 0.15f, 0.25f); 
    public float nightFogDistance = 100f;
    public Color nightFogColor = Color.black;

    [Header("Control Curba de Estompare")]
    public float lightFadePower = 3f;

    private void Awake()
    {
        if (sunLight == null) sunLight = FindObjectOfType<Light>();
        if (mainCamera == null) mainCamera = Camera.main;

        RenderSettings.ambientLight = dayAmbientColor;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;

        if (sunLight != null)
        {
            sunLight.intensity = dayIntensity;
        }
    }

    private void Update()
    {
        // Asumăm că GameStateManager există. Dacă nu, comentează linia.
        if (GameStateManager.Instance == null) return;
        UpdateVisualsByTime();
    }

    private void UpdateVisualsByTime()
    {
        float timeRemaining = GameStateManager.Instance.timeRemaining;
        float totalDuration = GameStateManager.Instance.IsNight 
                            ? GameStateManager.Instance.nightDuration 
                            : GameStateManager.Instance.dayDuration;
        
        float rawProgress = 1f - (timeRemaining / totalDuration); 
        float curvedProgress = Mathf.Pow(rawProgress, lightFadePower); 
        float lerpSpeed = 5f * Time.deltaTime;

        // --- CALCULARE ȚINTE ---
        Color targetAmbient;
        float targetIntensity;
        float targetFogDist;
        Color targetFogColor;

        if (!GameStateManager.Instance.IsNight) // ZI -> NOAPTE
        {
            targetAmbient = Color.Lerp(dayAmbientColor, nightAmbientColor, curvedProgress);
            targetIntensity = Mathf.Lerp(dayIntensity, nightIntensity, curvedProgress);
            targetFogDist = Mathf.Lerp(dayFogDistance, nightFogDistance, curvedProgress);
            targetFogColor = Color.Lerp(dayAmbientColor, nightFogColor, curvedProgress);
        }
        else // NOAPTE -> ZI
        {
            targetAmbient = Color.Lerp(nightAmbientColor, dayAmbientColor, curvedProgress);
            targetIntensity = Mathf.Lerp(nightIntensity, dayIntensity, curvedProgress);
            targetFogDist = Mathf.Lerp(nightFogDistance, dayFogDistance, curvedProgress);
            targetFogColor = Color.Lerp(nightFogColor, dayAmbientColor, curvedProgress);
        }

        // --- APLICARE MODIFICĂRI ---

        // 1. Lumina Soarelui
        if (sunLight != null) 
            sunLight.intensity = Mathf.Lerp(sunLight.intensity, targetIntensity, lerpSpeed);

        // 2. Setări Fog (În URP trebuie activat Fog în Lighting Settings sau URP Asset)
        RenderSettings.fogEndDistance = Mathf.Lerp(RenderSettings.fogEndDistance, targetFogDist, lerpSpeed);
        RenderSettings.fogColor = Color.Lerp(RenderSettings.fogColor, targetFogColor, lerpSpeed);
        
        // 3. Camera Far Clip
        if (mainCamera != null)
        {
            mainCamera.farClipPlane = RenderSettings.fogEndDistance + 10f;
        }

        // =========================================================
        // --- FIX PENTRU URP SKYBOX ---
        // =========================================================
        if (RenderSettings.skybox != null)
        {
            // Shader-ul Skybox/Procedural folosește "_SkyTint" nu "_Tint"
            Color targetSkyColor = targetFogColor; 
            
            // Verificăm dacă shader-ul are proprietatea înainte să o setăm (pentru siguranță)
            if (RenderSettings.skybox.HasProperty("_SkyTint"))
            {
                Color currentSkyColor = RenderSettings.skybox.GetColor("_SkyTint");
                RenderSettings.skybox.SetColor("_SkyTint", Color.Lerp(currentSkyColor, targetSkyColor, lerpSpeed));
            }

            // Opțional: Schimbăm și culoarea solului (Ground), altfel jos rămâne gri ziua când e noapte
            if (RenderSettings.skybox.HasProperty("_GroundColor"))
            {
                Color currentGround = RenderSettings.skybox.GetColor("_GroundColor");
                // Facem solul puțin mai întunecat decât cerul
                Color targetGround = targetSkyColor * 0.5f; 
                RenderSettings.skybox.SetColor("_GroundColor", Color.Lerp(currentGround, targetGround, lerpSpeed));
            }

            // Ajustăm Expunerea (Luminozitatea generală a skybox-ului)
            if (RenderSettings.skybox.HasProperty("_Exposure"))
            {
                float currentExposure = RenderSettings.skybox.GetFloat("_Exposure");
                // Target intensity este deja calculat mai sus (dayIntensity / nightIntensity)
                RenderSettings.skybox.SetFloat("_Exposure", Mathf.Lerp(currentExposure, targetIntensity, lerpSpeed));
            }
            
            // Opțional: Atmosphere Thickness (Grosimea atmosferei)
            // Ziua e 1.0, Noaptea poate fi mai mic pentru un cer mai clar, sau mai mare pentru apus roșiatic
             if (RenderSettings.skybox.HasProperty("_AtmosphereThickness"))
             {
                 float targetAtmosphere = GameStateManager.Instance.IsNight ? 0.5f : 1.0f;
                 float currentAtmosphere = RenderSettings.skybox.GetFloat("_AtmosphereThickness");
                 RenderSettings.skybox.SetFloat("_AtmosphereThickness", Mathf.Lerp(currentAtmosphere, targetAtmosphere, lerpSpeed));
             }
        }

        // IMPORTANT: Forțăm actualizarea luminii ambientale globale (GI)
        // Fără asta, obiectele rămân luminate ca ziua chiar dacă cerul e negru
        DynamicGI.UpdateEnvironment(); 

        // ---------------------------------------------------------


        // --- ROTAȚIA SOARELUI ---
        if (sunLight != null)
        {
            float angle;
            if (!GameStateManager.Instance.IsNight) 
                angle = Mathf.Lerp(0f, 180f, rawProgress);
            else 
                angle = Mathf.Lerp(180f, 360f, rawProgress);

            Quaternion targetRotation = Quaternion.Euler(angle, -90f, 0f);
            sunLight.transform.rotation = Quaternion.Slerp(sunLight.transform.rotation, targetRotation, lerpSpeed);
        }
    }
}