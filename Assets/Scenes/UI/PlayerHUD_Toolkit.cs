using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class PlayerHUD_Toolkit_Victory : MonoBehaviour
{
    private PlayerStats playerStats;
    private WaveManager waveManager;
    
    // Elemente UI
    private VisualElement healthFill;
    private VisualElement staminaFill;
    private VisualElement timeFill;
    private VisualElement nightWarning;
    private Label enemyCountLabel;
    private Label timeLabel;
    private Label waveTitleLabel;
    private VisualElement gameOverScreen;
    private VisualElement winScreen;

    [Header("Setări Alertă")]
    public float nightWarningThreshold = 0.2f;

    void OnEnable()
    {
        var uiDocument = GetComponent<UIDocument>();
        var root = uiDocument.rootVisualElement;

        // Referințe UI
        healthFill = root.Q<VisualElement>("HealthFill");
        staminaFill = root.Q<VisualElement>("StaminaFill");
        timeFill = root.Q<VisualElement>("TimeFill");
        nightWarning = root.Q<VisualElement>("NightWarning");
        enemyCountLabel = root.Q<Label>("EnemyCount");
        timeLabel = root.Q<Label>("TimeLabel");
        waveTitleLabel = root.Q<Label>("WaveTitle");

        gameOverScreen = root.Q<VisualElement>("GameOverScreen");
        winScreen = root.Q<VisualElement>("WinScreen");

        // ABONARE LA EVENIMENTE
        GlobalEvents.OnPlayerDeath += HandlePlayerDeath;
        GlobalEvents.OnGameWin += HandleGameWin;

        // Referințe Scripturi
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null) playerStats = player.GetComponent<PlayerStats>();
        
        waveManager = WaveManager.Instance;
    }

    void OnDisable()
    {
        GlobalEvents.OnPlayerDeath -= HandlePlayerDeath;
        GlobalEvents.OnGameWin -= HandleGameWin;
    }

    void Update()
    {
        UpdatePlayerStats();
        UpdateTimeUI();
        UpdateWaveInfo();
    }

    private void HandlePlayerDeath()
    {
        ShowEndScreen(gameOverScreen);
    }

    private void HandleGameWin()
    {
        ShowEndScreen(winScreen);
    }

    private void ShowEndScreen(VisualElement screen)
    {
        if (screen == null) return;

        screen.style.display = DisplayStyle.Flex;
        screen.style.opacity = 0;

        // Adăugăm o mică animație de fade-in folosind transition
        screen.RegisterCallback<GeometryChangedEvent>(evt => {
            screen.style.transitionProperty = new List<StylePropertyName> { "opacity" };
            screen.style.transitionDuration = new List<TimeValue> { new TimeValue(1.5f, TimeUnit.Second) };
            screen.style.opacity = 1;
        });
    }

    private void UpdatePlayerStats()
    {
        if (playerStats == null) return;
        healthFill.style.width = Length.Percent((float)playerStats.currentHealth / playerStats.MaxHealth * 100f);
        staminaFill.style.width = Length.Percent(playerStats.currentStamina / playerStats.maxStamina * 100f);
    }

    private void UpdateTimeUI()
    {
        if (GameStateManager.Instance == null) return;

        float timeRem = GameStateManager.Instance.timeRemaining;
        bool isNight = GameStateManager.Instance.IsNight;
        float totalDuration = isNight ? GameStateManager.Instance.nightDuration : GameStateManager.Instance.dayDuration;
        
        timeFill.style.width = Length.Percent((timeRem / totalDuration) * 100f);

        if (isNight)
        {
            timeFill.style.backgroundColor = new StyleColor(new Color(0.5f, 0f, 0.8f));
            timeLabel.text = "SURVIVE THE NIGHT";
            nightWarning.style.display = DisplayStyle.None;
        }
        else
        {
            timeFill.style.backgroundColor = new StyleColor(new Color(0f, 0.75f, 1f));
            timeLabel.text = "TIME UNTIL NIGHT";
            
            float rawPercent = timeRem / totalDuration;
            nightWarning.style.display = (rawPercent <= nightWarningThreshold) ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }

    private void UpdateWaveInfo()
    {
        if (waveManager == null) waveManager = WaveManager.Instance;
        if (waveManager == null) return;

        int currentDay = waveManager.GetCurrentDayIndex(); 
        waveTitleLabel.text = $"DAY {currentDay}";

        // 1. Inamicii care sunt deja spawnați și umblă prin scenă
        int currentlyActive = waveManager.GetActiveEnemiesCount();
        
        // 2. Inamicii care urmează să apară până la finalul zilei (din coada de spawn)
        int pendingSpawn = waveManager.GetRemainingEnemiesToSpawn();

        // 3. Totalul real pe care jucătorul trebuie să îl elimine pentru a termina ziua/jocul
        int totalToEliminate = currentlyActive + pendingSpawn;

        // Afișaj: 0 / 15 (unde 15 scade spre 0 pe măsură ce mor)
        enemyCountLabel.text = $"{currentlyActive}/ {totalToEliminate}";

        // Schimbăm culoarea în funcție de gravitate
        if (totalToEliminate > 0)
        {
            // Roșu dacă sunt mulți, alb dacă sunt puțini
            enemyCountLabel.style.color = totalToEliminate > 5 
                ? new StyleColor(Color.red) 
                : new StyleColor(new Color(1f, 0.7f, 0.7f)); // Un roșu pal/roz
        }
        else
        {
            enemyCountLabel.style.color = new StyleColor(Color.green);
            enemyCountLabel.text = "CLEARED";
        }
    }
}