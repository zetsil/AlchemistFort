using UnityEngine;
using System.Collections.Generic;



[CreateAssetMenu(fileName = "DayWaveData_D1", menuName = "Wave System/Day Wave Data")]
public class DayWaveData : ScriptableObject
{
    [Header("Configurație Zi")]
    [Tooltip("Indexul zilei (ex: 1 pentru Ziua 1, 2 pentru Ziua 2).")]
    public int dayIndex = 1;

    [Tooltip("Durata totală a zilei (în secunde) pe care se bazează timestamp-urile.")]
    public float dayDurationSeconds = 600f; // Ex: 10 minute

    [Header("Evenimente de Spawn (Wave-uri)")]
    [Tooltip("Lista de evenimente de spawn programate pentru această zi.")]
    public List<WaveSpawnEntry> spawnEntries = new List<WaveSpawnEntry>();

    // Vă puteți adăuga un event/trigger special pentru sfârșitul zilei aici.
    // public EntityData bossOfThisDay; 
}