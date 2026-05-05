using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using Unity.Behavior;

public class PlayerAmbientSpawner : MonoBehaviour
{
    public static PlayerAmbientSpawner Instance { get; private set; }

    [Header("Referințe")]
    public Transform playerTransform;
    
    [Header("Setări Spawn Dinamic")]
    public float minSpawnDistance = 15f;
    public float maxSpawnDistance = 30f;

    [Header("Configurare Valuri")]
    public List<DayWaveData> ambientWaves; 

    private HashSet<string> spawnedEvents = new HashSet<string>();
    private bool isReady = false;

    private void Awake()
    {
        Instance = this;
        if (playerTransform == null) playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
    }

    private void OnEnable()
    {
        GlobalEvents.OnTimeUpdate += HandleTimeUpdate;
        GlobalEvents.OnNightStart += ResetForNewNight;
    }

    private void OnDisable()
    {
        GlobalEvents.OnTimeUpdate -= HandleTimeUpdate;
        GlobalEvents.OnNightStart -= ResetForNewNight;
    }

    private void ResetForNewNight()
    {
        Debug.Log("<color=blue>[AmbientSpawner] Noaptea a început. Resetare evenimente.</color>");
        spawnedEvents.Clear();
        isReady = true;
    }

    private void HandleTimeUpdate(float percentRemaining, bool isNight)
    {
        if (!isNight) return;
        if (!isReady) { Debug.LogWarning("[AmbientSpawner] Managerul nu este 'Ready'!"); return; }
        if (ambientWaves == null || ambientWaves.Count == 0) { Debug.LogError("[AmbientSpawner] Lista ambientWaves este goală!"); return; }

        int currentDay = GameStateManager.Instance.currentDay;
        int dayIndex = currentDay - 1;

        if (dayIndex < 0 || dayIndex >= ambientWaves.Count)
        {
            Debug.Log($"[AmbientSpawner] Nu există date de spawn pentru ziua {currentDay}");
            return;
        }

        DayWaveData currentDayData = ambientWaves[dayIndex];
        float percentElapsed = 1f - percentRemaining;
        float timeElapsed = percentElapsed * currentDayData.dayDurationSeconds;

        for (int i = 0; i < currentDayData.spawnEntries.Count; i++)
        {
            string eventKey = "Ambient_Day" + currentDay + "_Entry" + i;

            if (!spawnedEvents.Contains(eventKey))
            {
                var entry = currentDayData.spawnEntries[i];
                if (timeElapsed >= entry.timeInSeconds)
                {
                    Debug.Log($"<color=green>[AmbientSpawner] Condiție îndeplinită pentru {eventKey} la {timeElapsed:F2}s</color>");
                    TriggerAmbientSpawn(entry);
                    spawnedEvents.Add(eventKey);
                }
            }
        }
    }

    private void TriggerAmbientSpawn(WaveSpawnEntry entry)
    {
        Debug.Log($"[AmbientSpawner] Încercare spawn: {entry.spawnCount}x {entry.enemyData.name}");
        
        for (int i = 0; i < entry.spawnCount; i++)
        {
            Vector3 spawnPos = GetRandomNavMeshPositionAroundPlayer();
            
            if (spawnPos != Vector3.zero)
            {
                GameObject enemyGo = EnemySpawner.Instance.SpawnEnemy(entry.enemyData, spawnPos);

                if (enemyGo != null)
                {
                    if (enemyGo.TryGetComponent<BehaviorGraphAgent>(out var agent))
                    {
                        agent.SetVariableValue("Target", playerTransform.gameObject);
                        Debug.Log($"[AmbientSpawner] Target setat pentru {enemyGo.name} via Behavior Graph.");
                    }
                    else
                    {
                        Debug.LogWarning($"[AmbientSpawner] Inamicul {enemyGo.name} NU are componenta BehaviorGraphAgent!");
                    }
                }
            }
            else
            {
                Debug.LogError("[AmbientSpawner] Nu s-a putut găsi o poziție validă pe NavMesh după 10 încercări!");
            }
        }
    }

    private Vector3 GetRandomNavMeshPositionAroundPlayer()
    {
        for (int i = 0; i < 10; i++)
        {
            float angle = Random.Range(0, Mathf.PI * 2);
            float distance = Random.Range(minSpawnDistance, maxSpawnDistance);
            
            Vector3 offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * distance;
            Vector3 randomPoint = playerTransform.position + offset;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomPoint, out hit, 5f, NavMesh.AllAreas))
            {
                return hit.position;
            }
        }
        return Vector3.zero;
    }
}