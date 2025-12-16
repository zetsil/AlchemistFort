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
    [Header("Resurse")]
    [Tooltip("Lista de ScriptableObject-uri DayWaveData pentru fiecare zi.")]
    public List<DayWaveData> allDayWaves;

    [Tooltip("Punctul de referință (Transform) în jurul căruia se vor spawna inamicii.")]
    public Transform spawnCenter;

    // --- Stare Internă ---
    private int currentDayIndex = 0;
    private float currentDayProgress = 0f;
    private DayWaveData currentDayData;
    private HashSet<WaveSpawnEntry> spawnedEvents = new HashSet<WaveSpawnEntry>();


    private void OnEnable()
    {
        // Ne abonăm la evenimentele de timp
        GlobalEvents.OnTimeUpdate += HandleTimeUpdate;
        GlobalEvents.OnNightStart += StartNewDay;
    }

    private void OnDisable()
    {
        // Ne dezabonăm
        GlobalEvents.OnTimeUpdate -= HandleTimeUpdate;
        GlobalEvents.OnNightStart -= StartNewDay;
    }

    private void StartNewDay()
    {
        currentDayIndex++;

        // Resetăm progresul și lista de evenimente declanșate
        currentDayProgress = 0f;
        spawnedEvents.Clear();

        if (currentDayIndex >= 1 && currentDayIndex <= allDayWaves.Count)
        {
            currentDayData = allDayWaves[currentDayIndex - 1];
            Debug.Log($"WaveManager: A început Ziua {currentDayIndex} cu datele '{currentDayData.name}'.");
        }
        else
        {
            currentDayData = null;
            Debug.LogWarning($"WaveManager: Nu există date de wave pentru Ziua {currentDayIndex}.");
        }
    }

    private void HandleTimeUpdate(float percentRemaining)
    {
        if (currentDayData == null)
            return;

        // Calculăm timpul scurs în secunde
        float percentElapsed = 1f - percentRemaining;
        float timeElapsed = percentElapsed * currentDayData.dayDurationSeconds;


        // Verificăm fiecare eveniment de spawn
        foreach (var entry in currentDayData.spawnEntries)
        {
            // Verificăm dacă nu a fost deja spawnat ȘI dacă timpul a trecut de timestamp-ul său
            if (!spawnedEvents.Contains(entry) && timeElapsed >= entry.timeInSeconds)
            {
                TriggerWaveSpawn(entry);
                spawnedEvents.Add(entry);
            }
        }
    }

    private void TriggerWaveSpawn(WaveSpawnEntry entry)
    {
        if (entry.enemyData == null || spawnCenter == null)
        {
            Debug.LogError($"Spawn invalid la {entry.timeInSeconds}s: Datele Entității sau Centrul de Spawn lipsesc.");
            return;
        }

        Debug.Log($"--- Wave Triggered la {entry.timeInSeconds}s ---");
        Debug.Log($"Spawnând {entry.spawnCount} x {entry.enemyData.name}.");

        // NOTĂ: Aici ar trebui să faceți un apel către o metodă de spawn efectivă (ex: Factory/Pool).
        for (int i = 0; i < entry.spawnCount; i++)
        {
            // Calculează o poziție aleatorie în jurul centrului de spawn
            Vector3 randomOffset = UnityEngine.Random.insideUnitSphere * entry.spawnRadius;
            randomOffset.y = 0; // De obicei, inamicii se spawnează la nivelul solului
            Vector3 spawnPosition = spawnCenter.position + randomOffset;

            // Exemplu de apel (va trebui implementat un EnemySpawner real)
            GameObject newEnemy = EnemySpawner.Instance.SpawnEnemy(entry.enemyData, spawnPosition);

            // Log de test
            Debug.Log($"   -> Spawnat '{entry.enemyData.name}' la poziția: {spawnPosition}.");
        }
    }
}