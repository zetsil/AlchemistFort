using UnityEngine;
using UnityEngine.UIElements;

public class PlayerHUD_Toolkit : MonoBehaviour
{
    private PlayerStats playerStats;
    
    private VisualElement healthFill;
    private VisualElement staminaFill;

    void OnEnable()
    {
        // 1. Luăm referința către Documentul UI
        var uiDocument = GetComponent<UIDocument>();
        var root = uiDocument.rootVisualElement;

        // 2. Găsim elementele de "Fill" după nume
        healthFill = root.Q<VisualElement>("HealthFill");
        staminaFill = root.Q<VisualElement>("StaminaFill");

        // 3. Găsim jucătorul (dacă nu e setat)
        if (playerStats == null)
        {
            playerStats = GameObject.FindWithTag("Player").GetComponent<PlayerStats>();
        }
    }

    void Update()
    {
        if (playerStats == null) return;

        // --- Actualizare VIAȚĂ ---
        // Calculăm procentul (0-100)
        float healthPercent = (float)playerStats.CurrentHealth / playerStats.MaxHealth * 100f;
        healthFill.style.width = Length.Percent(healthPercent);

        // --- Actualizare STAMINA ---
        float staminaPercent = playerStats.currentStamina / playerStats.maxStamina * 100f;
        staminaFill.style.width = Length.Percent(staminaPercent);
    }
}