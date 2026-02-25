using UnityEngine;
using System.Collections.Generic;

public class MultiSpawnPoint : MonoBehaviour
{
    [Tooltip("Configurația de valuri specifică acestui punct de spawn (atasat la acest GameObject).")]
    public List<DayWaveData> allDayWaves;

    // Helper pentru a lua datele zilei curente din acest punct
    public DayWaveData GetWaveDataForDay(int dayIndex)
    {
        // dayIndex-1 pentru că lista e 0-indexed, dar zilele încep de la 1
        if (dayIndex >= 0 && dayIndex <= allDayWaves.Count)
            return allDayWaves[dayIndex];
        
        return null;
    }
    
    // Opțional, ca să vezi punctele ușor în editor (o sferă roșie):
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1, 0, 0, 0.5f);
        Gizmos.DrawSphere(transform.position, 1f);
    }
}