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

    [Header("Configurare Spawn-uri Multiple")]
    [Tooltip("Lista punctelor de unde se vor spawna inamicii.")]
    public List<MultiSpawnPoint> spawnPoints;

    // --- Stare Internă ---
    private int currentDayIndex = 0;
    
    // Urmărim ce am spawnat folosind o pereche PunctSpawn + WaveSpawnEntry
    // Acum trebuie să știm și DIN CE punct a fost spawnat un entry pentru a nu se repeta.
    private HashSet<(MultiSpawnPoint, WaveSpawnEntry)> spawnedEvents = new HashSet<(MultiSpawnPoint, WaveSpawnEntry)>();

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

    private void OnEnable()
    {
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

        currentDayIndex++;
        spawnedEvents.Clear();
        allDayEventsTriggered = false;
        winSignalSent = false;

        Debug.Log($"WaveManager: A început Ziua {currentDayIndex} pentru toate punctele de spawn.");
    }

    private void HandleTimeUpdate(float percentRemaining)
    {
        float percentElapsed = 1f - percentRemaining;
        bool anyUnspawned = false;

        // Iterăm prin TOATE punctele de spawn
        foreach (var point in spawnPoints)
        {
            DayWaveData dayData = point.GetWaveDataForDay(currentDayIndex);
            
            // Dacă punctul curent nu are wave-uri pentru ziua de azi, trecem mai departe
            if (dayData == null) continue;

            float timeElapsed = percentElapsed * dayData.dayDurationSeconds;

            foreach (var entry in dayData.spawnEntries)
            {
                var eventKey = (point, entry);

                if (!spawnedEvents.Contains(eventKey))
                {
                    if (timeElapsed >= entry.timeInSeconds)
                    {
                        TriggerWaveSpawn(point, entry);
                        spawnedEvents.Add(eventKey);
                    }
                    else
                    {
                        anyUnspawned = true;
                    }
                }
            }
        }

        // Dacă nicio intrare de spawn din NICIUN punct nu a rămas nespawnată pentru azi
        if (!anyUnspawned)
        {
            allDayEventsTriggered = true;
        }
    }

    private void HandleEnemyDeath(Entity enemy)
    {
        enemiesActive--;
        Debug.Log("ba ai murit fmmm !!!!!!");
        
        if (enemiesActive < 0) enemiesActive = 0;
        
        Debug.Log("cati is activi");
        Debug.Log(enemiesActive);

        CheckWinConditions();
    }

    // În WaveManager.cs

    public int GetRemainingEnemiesToSpawn()
    {
        int remaining = 0;
        
        foreach (var point in spawnPoints)
        {
            DayWaveData dayData = point.GetWaveDataForDay(currentDayIndex);
            if (dayData == null) continue;

            foreach (var entry in dayData.spawnEntries)
            {
                // Dacă acest entry nu a fost încă spawnat, adunăm numărul de inamici
                if (!spawnedEvents.Contains((point, entry)))
                {
                    remaining += entry.spawnCount;
                }
            }
        }
        return remaining;
    }

    // Returnează numărul de inamici care sunt DEJA în scenă (vii)
    public int GetActiveEnemiesCount()
    {
        // Găsim toți zombii din scenă
        ZombieNPC[] allZombies = UnityEngine.Object.FindObjectsByType<ZombieNPC>(FindObjectsSortMode.None);
        
        int activeAndVisible = 0;

        foreach (var zombie in allZombies)
        {
            // Verificăm dacă zombiul este valid și NU se află în HideState
            // Presupunem că ai o proprietate 'CurrentState' în NPCBase sau ZombieNPC
            if (zombie != null && zombie.CurrentStateID != NPCBase.NPCStateID.Hide)
            {
                activeAndVisible++;
            }
        }

        // Actualizăm și variabila internă pentru a fi sincronizată cu realitatea din teren
        enemiesActive = activeAndVisible;
        
        return activeAndVisible;
    }

    public void RefreshActiveEnemies()
    {
        ZombieNPC[] enemies = UnityEngine.Object.FindObjectsByType<ZombieNPC>(FindObjectsSortMode.None);
        enemiesActive = enemies.Length;
        Debug.Log($"<color=orange>[WaveManager] Contor inamici actualizat post-load: {enemiesActive}</color>");
    }

    public int GetTotalEnemiesRemaining()
    {
        return enemiesActive;
    }

    private void CheckWinConditions()
    {
        // 1. Găsim câte zile are în total campania (cel mai lung set de wave-uri dintre toate punctele)
        int maxDaysInCampaign = 0;
        foreach (var point in spawnPoints)
        {
            if (point.allDayWaves.Count > maxDaysInCampaign)
            {
                maxDaysInCampaign = point.allDayWaves.Count;
            }
        }

        Debug.Log($"<color=cyan>[WaveManager Debug]</color> " +
                $"Ziua: {currentDayIndex}/{maxDaysInCampaign} | " +
                $"Inamici: {enemiesActive} | " +
                $"Spawn Terminat: {allDayEventsTriggered} | " +
                $"Win Deja Trimis: {winSignalSent}");

        // Verificăm condiția principală
        if (allDayEventsTriggered && enemiesActive <= 0 && !winSignalSent)
        {
            if (currentDayIndex >= maxDaysInCampaign)
            {
                winSignalSent = true;
                Debug.Log("<color=gold>🏆 WaveManager: CONDIȚII DE VICTORIE ÎNDEPLINITE! JOC CÂȘTIGAT!</color>");
                GlobalEvents.NotifyGameWin();
            }
            else
            {
                Debug.Log($"<color=green>WaveManager: Ziua {currentDayIndex} terminată.</color> Se așteaptă ziua următoare.");
            }
        }
        else
        {
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
        if (enemiesActive <= 0)
        {
            RefreshActiveEnemies();
        }

        bool hasUnspawnedEvents = false;
        int totalSpawnEventsForToday = 0;

        foreach (var point in spawnPoints)
        {
            DayWaveData dayData = point.GetWaveDataForDay(currentDayIndex);
            if (dayData == null) continue;

            totalSpawnEventsForToday += dayData.spawnEntries.Count;

            foreach (var entry in dayData.spawnEntries)
            {
                if (!spawnedEvents.Contains((point, entry)))
                {
                    hasUnspawnedEvents = true;
                    // Nu dăm break aici pentru a putea itera prin toate și a vedea log-ul corect la final
                }
            }
        }

        allDayEventsTriggered = !hasUnspawnedEvents;

        Debug.Log(
            $"[WaveManager] Recalc after load | Day: {currentDayIndex} | " +
            $"SpawnedEvents: {spawnedEvents.Count}/{totalSpawnEventsForToday} | " +
            $"AllTriggered: {allDayEventsTriggered} | EnemiesAlive: {enemiesActive}"
        );
    }

    // --- Metode Publice ---

    public int GetCurrentDayIndex() => currentDayIndex;

    public void SetCurrentDay(int dayIndex)
    {
        currentDayIndex = dayIndex;
        spawnedEvents.Clear();

        if (GameStateManager.Instance != null)
        {
            float totalDuration = GameStateManager.Instance.IsNight ? GameStateManager.Instance.nightDuration : GameStateManager.Instance.dayDuration;
            float percentElapsed = 1f - Mathf.Clamp01(GameStateManager.Instance.timeRemaining / totalDuration);

            // Verificăm timpul curent pentru fiecare punct de spawn și marcăm spawn-urile vechi ca fiind "făcute"
            foreach (var point in spawnPoints)
            {
                DayWaveData dayData = point.GetWaveDataForDay(currentDayIndex);
                if (dayData == null) continue;

                float timeElapsed = percentElapsed * dayData.dayDurationSeconds;

                foreach (var entry in dayData.spawnEntries)
                {
                    if (timeElapsed >= entry.timeInSeconds)
                    {
                        spawnedEvents.Add((point, entry)); // Skip la spawn natural, îi vom încărca din JSON
                    }
                }
            }
        }

        RecalculateDayStateAfterLoad();
    }

    private void TriggerWaveSpawn(MultiSpawnPoint point, WaveSpawnEntry entry)
    {
        if (entry.enemyData == null || point.transform == null) return;

        for (int i = 0; i < entry.spawnCount; i++)
        {
            Vector3 randomOffset = UnityEngine.Random.insideUnitSphere * entry.spawnRadius;
            randomOffset.y = 0;
            Vector3 spawnPosition = point.transform.position + randomOffset;

            if (EnemySpawner.Instance != null)
            {
                EnemySpawner.Instance.SpawnEnemy(entry.enemyData, spawnPosition);
                enemiesActive++; // Incrementăm numărul de inamici activi
            }
        }
    }

    public int GetTotalEnemiesForCurrentDay()
    {
        int totalEnemies = 0;

        foreach (var point in spawnPoints)
        {
            DayWaveData dayData = point.GetWaveDataForDay(currentDayIndex);
            if (dayData == null) continue;

            foreach (var entry in dayData.spawnEntries)
            {
                totalEnemies += entry.spawnCount;
            }
        }

        return totalEnemies;
    }
}