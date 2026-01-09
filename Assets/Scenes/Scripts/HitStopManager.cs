using UnityEngine;
using System.Collections;

public class HitStopManager : MonoBehaviour
{
    public static HitStopManager Instance;

    void Awake() => Instance = this;

    public void RequestHitStop(float duration)
    {
        StopAllCoroutines();
        StartCoroutine(Wait(duration));
    }

    private IEnumerator Wait(float duration)
    {
        Time.timeScale = 0f;
        // Folosim SecondsRealtime pentru că timeScale e 0
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1f;
    }
}