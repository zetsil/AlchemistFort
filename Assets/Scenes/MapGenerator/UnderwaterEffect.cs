using UnityEngine;
using UnityEngine.Rendering; // Namespace principal pentru Post-Processing
using UnityEngine.Rendering.Universal; // Specifica pentru URP, schimba cu HDRP daca e cazul

public class UnderwaterPostProcess : MonoBehaviour
{
    public MapGenerator mapGen;
    public Transform playerCamera;

    [Header("Setări Post-Procesare")]
    public Volume globalVolume; // Trage aici Volume-ul Global din scenă
    public VolumeProfile normalProfile; // Profilul tău standard (cer, post-procesare normală)
    public VolumeProfile underwaterProfile; // Creează un profil nou cu efecte underwater (color grading, lens distortion, etc.)

    [Header("Setări Fog (Afară)")]
    public Color normalFogColor = new Color(0.5f, 0.6f, 0.7f); // O culoare neutră de cer
    [Range(0f, 1f)] public float normalFogDensity = 0.01f;
    public FogMode normalFogMode = FogMode.ExponentialSquared;

    [Header("Setări Fog (Sub Apă)")]
    public Color underwaterFogColor = new Color(0f, 0.2f, 0.4f); // Un albastru închis, adânc
    [Range(0f, 1f)] public float underwaterFogDensity = 0.1f; // Mai dens pentru a simula opacitatea apei
    public FogMode underwaterFogMode = FogMode.ExponentialSquared; // Modul Exponential de obicei arată mai bine sub apă

    private bool isUnderwater;
    
    // Pentru a salva setările inițiale de Fog la Start
    private Color defaultFogColor;
    private float defaultFogDensity;
    private bool defaultFogEnabled;
    private FogMode defaultFogMode;

    void Start()
    {
        // Salvăm setările inițiale de Fog ale scenei
        defaultFogColor = RenderSettings.fogColor;
        defaultFogDensity = RenderSettings.fogDensity;
        defaultFogEnabled = RenderSettings.fog;
        defaultFogMode = RenderSettings.fogMode;

        // Asigurăm că avem un profil normal setat la început
        if (globalVolume != null && normalProfile != null)
        {
            globalVolume.profile = normalProfile;
        }
    }

    void Update()
    {
        if (mapGen == null || mapGen.settings == null || globalVolume == null) return;

        // Calculăm nivelul apei
        float waterLevel = (mapGen.settings.nivelCampie * mapGen.settings.terrainHeightMultiplier) 
                        - mapGen.waterOffset + mapGen.terrain.transform.position.y;

        // Adăugăm un offset (de exemplu 0.1 metri) pentru a nu declanșa efectul 
        // dacă suntem doar cu "vârful capului" în apă
        float detectionOffset = -0.1f; 

        if (playerCamera.position.y < (waterLevel - detectionOffset))
        {
            if (!isUnderwater) ToggleUnderwater(true);
        }
        else
        {
            // Ieșim din apă imediat ce camera trece de nivelul apei minus offset
            if (isUnderwater) ToggleUnderwater(false);
        }
    }

    void ToggleUnderwater(bool state)
    {
        isUnderwater = state;
        
        // --- Notificare prin GlobalEvents ---
        if (state)
        {
            GlobalEvents.NotifyEnterWater();
            Debug.Log("🌊 Jucătorul a intrat sub apă.");
        }
        else
        {
            GlobalEvents.NotifyExitWater();
            Debug.Log("☀️ Jucătorul a ieșit din apă.");
        }

        // --- Modificarea Post-Processing ---
        if (globalVolume != null)
        {
            globalVolume.profile = state ? underwaterProfile : normalProfile;
        }

        // --- Modificarea Fog ---
        if (state)
        {
            RenderSettings.fog = true;
            RenderSettings.fogColor = underwaterFogColor;
            RenderSettings.fogDensity = underwaterFogDensity;
            RenderSettings.fogMode = underwaterFogMode;
        }
        else
        {
            RenderSettings.fog = defaultFogEnabled; 
            RenderSettings.fogColor = defaultFogColor;
            RenderSettings.fogDensity = defaultFogDensity;
            RenderSettings.fogMode = defaultFogMode;
        }
    }
}