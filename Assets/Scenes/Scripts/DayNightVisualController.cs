using UnityEngine;

public class DayNightVisualController : MonoBehaviour
{
    [Header("Referințe")]
    public Light sunLight;
    // Păstrăm referința doar dacă ai nevoie de ea pentru alte logici, 
    // dar am scos orice modificare de Clipping asupra ei.
    public Camera mainCamera;

    [Header("Setări Zi")]
    public float dayIntensity = 1.2f;
    public Color dayAmbientColor = new Color(0.5f, 0.5f, 0.5f);
    public float dayFogDistance = 150f;
    public Color dayFogColor = new Color(0.7f, 0.8f, 0.9f);

    [Header("Setări Noapte")]
    public float nightIntensity = 0.05f;
    public Color nightAmbientColor = new Color(0.02f, 0.02f, 0.05f);
    public float nightFogDistance = 40f; 
    public Color nightFogColor = Color.black;

    [Header("Control Tranziție")]
    [Range(1f, 10f)]
    public float lightFadePower = 2.5f; 
    public float lerpSpeed = 2f; 

    private void Awake()
    {
        if (sunLight == null) sunLight = FindObjectOfType<Light>();
        if (mainCamera == null) mainCamera = Camera.main;

        // Configurare inițială Fog
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        
        // --- MODIFICARE: NU MAI SETĂM FAR CLIP PLANE AICI ---
    }

    private void Update()
    {
        if (GameStateManager.Instance == null) return;
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        float timeRemaining = GameStateManager.Instance.timeRemaining;
        float totalDuration = GameStateManager.Instance.IsNight 
                            ? GameStateManager.Instance.nightDuration 
                            : GameStateManager.Instance.dayDuration;
        
        float rawProgress = 1f - (timeRemaining / totalDuration); 
        float curvedProgress = Mathf.Pow(rawProgress, lightFadePower); 

        float targetIntensity;
        Color targetAmbient;
        float targetFogDist;
        Color targetFogColor;

        if (!GameStateManager.Instance.IsNight)
        {
            targetIntensity = Mathf.Lerp(dayIntensity, nightIntensity, curvedProgress);
            targetAmbient = Color.Lerp(dayAmbientColor, nightAmbientColor, curvedProgress);
            targetFogDist = Mathf.Lerp(dayFogDistance, nightFogDistance, curvedProgress);
            targetFogColor = Color.Lerp(dayFogColor, nightFogColor, curvedProgress);
        }
        else
        {
            targetIntensity = Mathf.Lerp(nightIntensity, dayIntensity, curvedProgress);
            targetAmbient = Color.Lerp(nightAmbientColor, dayAmbientColor, curvedProgress);
            targetFogDist = Mathf.Lerp(nightFogDistance, dayFogDistance, curvedProgress);
            targetFogColor = Color.Lerp(nightFogColor, dayFogColor, curvedProgress);
        }

        float t = Time.deltaTime * lerpSpeed;

        if (sunLight != null)
            sunLight.intensity = Mathf.Lerp(sunLight.intensity, targetIntensity, t);

        RenderSettings.ambientLight = Color.Lerp(RenderSettings.ambientLight, targetAmbient, t);
        RenderSettings.fogEndDistance = Mathf.Lerp(RenderSettings.fogEndDistance, targetFogDist, t);
        RenderSettings.fogColor = Color.Lerp(RenderSettings.fogColor, targetFogColor, t);

        if (RenderSettings.skybox != null)
        {
            if (RenderSettings.skybox.HasProperty("_Exposure"))
                RenderSettings.skybox.SetFloat("_Exposure", sunLight.intensity);
            
            if (RenderSettings.skybox.HasProperty("_SkyTint"))
                RenderSettings.skybox.SetColor("_SkyTint", Color.Lerp(RenderSettings.skybox.GetColor("_SkyTint"), targetFogColor, t));
        }

        RotateSun(rawProgress);
        DynamicGI.UpdateEnvironment();
    }

    private void RotateSun(float progress)
    {
        if (sunLight == null) return;

        float angle;
        if (!GameStateManager.Instance.IsNight)
            angle = Mathf.Lerp(10f, 170f, progress);
        else
            angle = Mathf.Lerp(190f, 350f, progress);

        sunLight.transform.rotation = Quaternion.Slerp(
            sunLight.transform.rotation, 
            Quaternion.Euler(angle, -90f, 0f), 
            Time.deltaTime * lerpSpeed
        );
    }
}