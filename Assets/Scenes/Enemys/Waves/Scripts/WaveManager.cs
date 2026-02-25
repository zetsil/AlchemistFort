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

    private enum ManagerState { Locked, Ready }
    private ManagerState currentState = ManagerState.Locked;

    // --- Stare Internă ---
    
    // Urmărim ce am spawnat folosind o pereche PunctSpawn + WaveSpawnEntry
    // Acum trebuie să știm și DIN CE punct a fost spawnat un entry pentru a nu se repeta.
    private HashSet<string> spawnedEvents = new HashSet<string>();
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
        // Verificăm dacă suntem în timpul unui Load
        if (GameStateManager.Instance != null && GameStateManager.Instance.isRestoringFromSave)
        {
            return;
        }

        spawnedEvents.Clear();
        allDayEventsTriggered = false;
        winSignalSent = false;
        currentState = ManagerState.Ready;

        int day = GameStateManager.Instance.currentDay;
    }

    private void HandleTimeUpdate(float percentRemaining, bool isNight)
    {
        if (currentState != ManagerState.Ready || !isNight)
        {
            return;
        }

        int currentDay = GameStateManager.Instance.currentDay;
        float percentElapsed = 1f - percentRemaining;
        bool anyUnspawned = false;

        foreach (var point in spawnPoints)
        {
            DayWaveData dayData = point.GetWaveDataForDay(currentDay);
            if (dayData == null) continue;
            string existingKeys = string.Join(", ", spawnedEvents);

            float timeElapsed = percentElapsed * dayData.dayDurationSeconds;

            for (int i = 0; i < dayData.spawnEntries.Count; i++)
            {
                var entry = dayData.spawnEntries[i];
                // MODIFICARE: Generare cheie
                string eventKey = point.name + i.ToString();

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

        if (!anyUnspawned && !allDayEventsTriggered)
        {
            allDayEventsTriggered = true;
        }
    }

    private void HandleEnemyDeath(Entity enemy)
    {
        enemiesActive--;
        
        if (enemiesActive < 0) enemiesActive = 0;
        
        Debug.Log(enemiesActive);

        CheckWinConditions();
    }

    // În WaveManager.cs

    public int GetRemainingEnemiesToSpawn()
    {
        if (spawnPoints == null || spawnPoints.Count == 0 || GameStateManager.Instance == null) return 0;

        int remaining = 0;
        int day = GameStateManager.Instance.currentDay;
        
        foreach (var point in spawnPoints)
        {
            if (point == null) continue;
            DayWaveData dayData = point.GetWaveDataForDay(day);
            if (dayData == null) continue;

            for (int i = 0; i < dayData.spawnEntries.Count; i++)
            {
                var entry = dayData.spawnEntries[i];
                // MODIFICARE: Verificare prin string
                string eventKey = point.name + i.ToString();

                if (!spawnedEvents.Contains(eventKey))
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
        enemiesActive = 0; 

        ZombieNPC[] enemies = UnityEngine.Object.FindObjectsByType<ZombieNPC>(FindObjectsSortMode.None);
        
        int count = 0;
        foreach (var zombie in enemies)
        {
            if (zombie != null && zombie.gameObject.activeInHierarchy)
            {
                count++;
            }
        }

        enemiesActive = count;
    }

    public int GetTotalEnemiesRemaining()
    {
        return enemiesActive;
    }

    private void CheckWinConditions()
    {
        // 1. Găsim câte zile are în total campania (cel mai lung set de wave-uri dintre toate punctele)
        int maxDaysInCampaign = 0;
        int currentDay = GameStateManager.Instance.currentDay;
        foreach (var point in spawnPoints)
        {
            if (point.allDayWaves.Count > maxDaysInCampaign)
            {
                maxDaysInCampaign = point.allDayWaves.Count;
            }
        }

        Debug.Log($"<color=cyan>[WaveManager Debug]</color> " +
                $"Ziua: {currentDay}/{maxDaysInCampaign} | " +
                $"Inamici: {enemiesActive} | " +
                $"Spawn Terminat: {allDayEventsTriggered} | " +
                $"Win Deja Trimis: {winSignalSent}");

        // Verificăm condiția principală
        if (allDayEventsTriggered && enemiesActive <= 0 && !winSignalSent)
        {
            if (currentDay >= maxDaysInCampaign)
            {
                winSignalSent = true;
                Debug.Log("<color=gold>🏆 WaveManager: CONDIȚII DE VICTORIE ÎNDEPLINITE! JOC CÂȘTIGAT!</color>");
                GlobalEvents.NotifyGameWin();
            }
            else
            {
                Debug.Log($"<color=green>WaveManager: Ziua {currentDay} terminată.</color> Se așteaptă ziua următoare.");
            }
        }
        else
        {
            if (!winSignalSent)
            {
                string failReason = "";
                if (!allDayEventsTriggered) failReason += "[Mai sunt inamici de spawnat conform timpului] ";
                if (enemiesActive > 0) failReason += $"[Mai sunt {enemiesActive} inamici în viață] ";
                if (currentDay == 0) failReason += "[Ziua curentă este 0 - jocul nu a început corect] ";

                if (!string.IsNullOrEmpty(failReason))
                {
                    Debug.Log($"<color=orange>WaveManager: Win neactivat deoarece: {failReason}</color>");
                }
            }
        }
    }

    public void RecalculateDayStateAfterLoad()
    {
        RefreshActiveEnemies();

        bool hasUnspawnedEvents = false;
        int currentDay = GameStateManager.Instance.currentDay;

        foreach (var point in spawnPoints)
        {
            DayWaveData dayData = point.GetWaveDataForDay(currentDay);
            if (dayData == null) continue;

            for (int i = 0; i < dayData.spawnEntries.Count; i++)
            {
                // MODIFICARE: Verificare prin string
                string eventKey = point.name + i.ToString();
                if (!spawnedEvents.Contains(eventKey))
                {
                    hasUnspawnedEvents = true;
                }
            }
        }
        allDayEventsTriggered = !hasUnspawnedEvents;
    }

    public void LockManager() 
    {
        currentState = ManagerState.Locked;
        Debug.Log("<color=red>[WaveManager] 🔒 Manager BLOCAT manual pentru procedură externă.</color>");
    }

    public void UnlockManager() 
    {
        currentState = ManagerState.Ready;
        Debug.Log("<color=red>[WaveManager] 🔒 Manager BLOCAT manual pentru procedură externă.</color>");
    }

    // --- Metode Publice ---

    public int GetCurrentDayIndex() => GameStateManager.Instance != null ? GameStateManager.Instance.currentDay : 0;

    public void SetCurrentDay(int dayIndex)
    {
        currentState = ManagerState.Locked;

        GameStateManager.Instance.currentDay = dayIndex;
        spawnedEvents.Clear();

        if (GameStateManager.Instance != null)
        {
            float totalDuration = GameStateManager.Instance.IsNight ? GameStateManager.Instance.nightDuration : GameStateManager.Instance.dayDuration;
            float timeRemaining = GameStateManager.Instance.timeRemaining;
            float percentElapsed = 1f - Mathf.Clamp01(timeRemaining / totalDuration);

            Debug.Log($"[WaveManager] Timp rămas: {timeRemaining}s | Total: {totalDuration}s | Procent scurs: {percentElapsed * 100}%");

            foreach (var point in spawnPoints)
            {
                DayWaveData dayData = point.GetWaveDataForDay(dayIndex);
                if (dayData == null) continue;

                float timeElapsed = percentElapsed * dayData.dayDurationSeconds;
                int blockedCount = 0;

                for (int i = 0; i < dayData.spawnEntries.Count; i++)
                {
                    var entry = dayData.spawnEntries[i];

                    if (timeElapsed >= entry.timeInSeconds)
                    {
                        // Generăm cheia unică: NumePunctSpawn + Index
                        string spawnKey = point.name + i.ToString();
                        spawnedEvents.Add(spawnKey);
                        blockedCount++;
                    }
                }

                if (blockedCount > 0)
                {
                    Debug.Log($"<color=cyan>[WaveManager] Punct '{point.name}': Am blocat {blockedCount} spawn-uri vechi prin cheie unică.</color>");
                }
            }
        }
        else
        {
            Debug.LogError("[WaveManager] 🔴 GameStateManager.Instance este NULL în SetCurrentDay!");
        }
        RecalculateDayStateAfterLoad();
        currentState = ManagerState.Ready;
        Debug.Log($"<color=lime>[WaveManager] ✅ Managerul este acum READY. HashSet-ul are {spawnedEvents.Count} chei.</color>");
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
                RefreshActiveEnemies();
            }
        }
    }

    public int GetTotalEnemiesForCurrentDay()
    {
        int totalEnemies = 0;

        foreach (var point in spawnPoints)
        {
            DayWaveData dayData = point.GetWaveDataForDay(GameStateManager.Instance.currentDay);
            if (dayData == null) continue;

            foreach (var entry in dayData.spawnEntries)
            {
                totalEnemies += entry.spawnCount;
            }
        }

        return totalEnemies;
    }
}