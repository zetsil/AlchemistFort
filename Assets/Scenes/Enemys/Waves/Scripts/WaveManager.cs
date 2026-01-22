using UnityEngine;
using System;
using System.Collections.Generic;

[Serializable]
public class WaveSpawnEntry
{
    [Tooltip("Timpul (în secunde) de la începutul zilei când se activează acest spawn.")]
    public float timeInSeconds;

    [Tooltip("Referința la ScriptableObject-ul EntityData al inamicului de spawnat.")]
    public EntityData enemyData;

    [Tooltip("Numărul de inamici de spawnat la acest timestamp.")]
    [Range(1, 50)]
    public int spawnCount = 1;

    [Tooltip("Raza maximă față de punctul de spawn în care vor fi plasați inamicii.")]
    [Range(0f, 50f)]
    public float spawnRadius = 20f;
}

public class WaveManager : MonoBehaviour
{
    // 1. Singleton Instance
    public static WaveManager Instance { get; private set; }

    [Header("Resurse")]
    [Tooltip("Lista de ScriptableObject-uri DayWaveData pentru fiecare zi.")]
    public List<DayWaveData> allDayWaves;

    [Tooltip("Punctul de referință (Transform) în jurul căruia se vor spawna inamicii.")]
    public Transform spawnCenter;

    // --- Stare Internă ---
    private int currentDayIndex = 0;
    private DayWaveData currentDayData;
    private HashSet<WaveSpawnEntry> spawnedEvents = new HashSet<WaveSpawnEntry>();

    // Contorizare pentru condiția de victorie
    private int enemiesActive = 0;
    private bool allDayEventsTriggered = false;
    private bool winSignalSent = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
    }

    private void OnEnable()
    {
        // Ne abonăm la evenimentele de timp și moarte inamic
        GlobalEvents.OnTimeUpdate += HandleTimeUpdate;
        GlobalEvents.OnNightStart += StartNewDay;
        GlobalEvents.OnEnemyDeath += HandleEnemyDeath;
    }

    private void OnDisable()
    {
        GlobalEvents.OnTimeUpdate -= HandleTimeUpdate;
        GlobalEvents.OnNightStart -= StartNewDay;
        GlobalEvents.OnEnemyDeath -= HandleEnemyDeath;
    }

    private void StartNewDay()
    {
        
        if (GameStateManager.Instance != null && GameStateManager.Instance.isRestoringFromSave)
        {
            Debug.Log("[WaveManager] Load detectat. Se păstrează ziua curentă: " + currentDayIndex);
            return;
        }

        // RefreshActiveEnemies();
        
        currentDayIndex++;
        spawnedEvents.Clear();
        allDayEventsTriggered = false;
        winSignalSent = false;

        if (currentDayIndex >= 1 && currentDayIndex <= allDayWaves.Count)
        {
            currentDayData = allDayWaves[currentDayIndex - 1];
            Debug.Log($"WaveManager: A început Ziua {currentDayIndex}.");
        }
        else
        {
            currentDayData = null;
        }
    }

    private void HandleTimeUpdate(float percentRemaining)
    {
        if (currentDayData == null) return;

        float percentElapsed = 1f - percentRemaining;
        float timeElapsed = percentElapsed * currentDayData.dayDurationSeconds;

        bool anyUnspawned = false;

        foreach (var entry in currentDayData.spawnEntries)
        {
            if (!spawnedEvents.Contains(entry))
            {
                if (timeElapsed >= entry.timeInSeconds)
                {
                    TriggerWaveSpawn(entry);
                    spawnedEvents.Add(entry);
                }
                else
                {
                    anyUnspawned = true;
                }
            }
        }

        // Dacă toate entry-urile de spawn pentru azi au fost declanșate
        if (!anyUnspawned)
        {
            allDayEventsTriggered = true;
        }
    }

    private void HandleEnemyDeath(Entity enemy)
    {
        enemiesActive--;
        Debug.Log("ba ai murit fmmm !!!!!!");
        // Safety check: dacă contorul a luat-o razna (e negativ), îl resetăm
        if (enemiesActive < 0) enemiesActive = 0;
        
        Debug.Log("cati is activi");
        Debug.Log(enemiesActive);

        CheckWinConditions();
    }

    public void RefreshActiveEnemies()
    {
        // Căutăm toate obiectele de tip ZombieNPC (sau clasa ta de bază pentru inamici)
        ZombieNPC[] enemies = UnityEngine.Object.FindObjectsByType<ZombieNPC>(FindObjectsSortMode.None);
        
        // Setăm contorul exact la numărul de inamici găsiți în scenă
        enemiesActive = enemies.Length;
        
        Debug.Log($"<color=orange>[WaveManager] Contor inamici actualizat post-load: {enemiesActive}</color>");
        
    }

    private void CheckWinConditions()
    {
        // 1. Log de diagnostic general (se execută la fiecare apel)
        Debug.Log($"<color=cyan>[WaveManager Debug]</color> " +
                $"Ziua: {currentDayIndex}/{allDayWaves.Count} | " +
                $"Inamici: {enemiesActive} | " +
                $"Spawn Terminat: {allDayEventsTriggered} | " +
                $"Win Deja Trimis: {winSignalSent}");

        // Verificăm condiția principală
        if (allDayEventsTriggered && enemiesActive <= 0 && !winSignalSent)
        {
            // Verificăm dacă a fost ultima zi din lista totală
            if (currentDayIndex >= allDayWaves.Count)
            {
                winSignalSent = true;
                Debug.Log("<color=gold>🏆 WaveManager: CONDIȚII DE VICTORIE ÎNDEPLINITE! JOC CÂȘTIGAT!</color>");
                GlobalEvents.NotifyGameWin();
            }
            else
            {
                Debug.Log($"<color=green>WaveManager: Ziua {currentDayIndex} terminată.</color> Se așteaptă ziua următoare (Total zile: {allDayWaves.Count}).");
            }
        }
        else
        {
            // 2. Log de eroare logică (ne spune DE CE nu dă win)
            if (!winSignalSent)
            {
                string failReason = "";
                if (!allDayEventsTriggered) failReason += "[Mai sunt inamici de spawnat conform timpului] ";
                if (enemiesActive > 0) failReason += $"[Mai sunt {enemiesActive} inamici în viață] ";
                if (currentDayIndex == 0) failReason += "[Ziua curentă este 0 - jocul nu a început corect] ";

                if (!string.IsNullOrEmpty(failReason))
                {
                    Debug.Log($"<color=orange>WaveManager: Win neactivat deoarece: {failReason}</color>");
                }
            }
        }
    }
    private void RecalculateDayStateAfterLoad()
    {
        if (currentDayData == null)
            return;

        if (enemiesActive <= 0)
        {
            RefreshActiveEnemies();
        }

        // 1️⃣ Verificăm dacă mai există spawn-uri care NU ar fi trebuit declanșate
        bool hasUnspawnedEvents = false;

        foreach (var entry in currentDayData.spawnEntries)
        {
            if (!spawnedEvents.Contains(entry))
            {
                hasUnspawnedEvents = true;
                break;
            }
        }

        // 2️⃣ Dacă NU mai sunt spawn-uri → ziua e complet declanșată
        allDayEventsTriggered = !hasUnspawnedEvents;

        Debug.Log(
            $"[WaveManager] Recalc after load | Day: {currentDayIndex} | " +
            $"SpawnedEvents: {spawnedEvents.Count}/{currentDayData.spawnEntries.Count} | " +
            $"AllTriggered: {allDayEventsTriggered} | EnemiesAlive: {enemiesActive}"
        );
    }


    // --- Metode Publice ---

    public int GetCurrentDayIndex() => currentDayIndex;

    public void SetCurrentDay(int dayIndex)
    {
        currentDayIndex = dayIndex;
        currentDayData = (currentDayIndex >= 1 && currentDayIndex <= allDayWaves.Count) ? allDayWaves[currentDayIndex - 1] : null;

        spawnedEvents.Clear();

        // ADAUGĂ ACEASTA: Verifică timpul curent și marchează spawn-urile vechi ca fiind "făcute"
        if (currentDayData != null && GameStateManager.Instance != null)
        {
            float totalDuration = GameStateManager.Instance.IsNight ? GameStateManager.Instance.nightDuration : GameStateManager.Instance.dayDuration;
            float percentElapsed = 1f - Mathf.Clamp01(GameStateManager.Instance.timeRemaining / totalDuration);
            float timeElapsed = percentElapsed * currentDayData.dayDurationSeconds;

            foreach (var entry in currentDayData.spawnEntries)
            {
                if (timeElapsed >= entry.timeInSeconds)
                {
                    spawnedEvents.Add(entry); // Skip la spawn natural, îi vom încărca din JSON
                }
            }
        }

        RecalculateDayStateAfterLoad();
        

    }

    private void TriggerWaveSpawn(WaveSpawnEntry entry)
    {
        if (entry.enemyData == null || spawnCenter == null) return;

        for (int i = 0; i < entry.spawnCount; i++)
        {
            Vector3 randomOffset = UnityEngine.Random.insideUnitSphere * entry.spawnRadius;
            randomOffset.y = 0;
            Vector3 spawnPosition = spawnCenter.position + randomOffset;

            if (EnemySpawner.Instance != null)
            {
                EnemySpawner.Instance.SpawnEnemy(entry.enemyData, spawnPosition);
                enemiesActive++; // Incrementăm numărul de inamici activi
            }
        }
    }
    
    public int GetTotalEnemiesForCurrentDay()
    {
        if (currentDayData == null) return 0;

        int totalEnemies = 0;
        foreach (var entry in currentDayData.spawnEntries)
        {
            totalEnemies += entry.spawnCount;
        }
        return totalEnemies;
    }
}