using UnityEngine;
using System.Collections;

public class CameraShakeListener : MonoBehaviour
{
    private Vector3 originalPos;
    private Coroutine shakeCoroutine;

    private void OnEnable()
    {
        // Subscribe to your new Global Event
        GlobalEvents.OnScreenShakeRequested += StartShake;
    }

    private void OnDisable()
    {
        // Unsubscribe to prevent memory leaks and errors when switching scenes
        GlobalEvents.OnScreenShakeRequested -= StartShake;
    }

    private void StartShake(float intensity, float duration)
    {
        // If a shake is already happening, stop it to start the new one
        if (shakeCoroutine != null) StopCoroutine(shakeCoroutine);
        
        shakeCoroutine = StartCoroutine(ShakeProcess(intensity, duration));
    }

    private IEnumerator ShakeProcess(float intensity, float duration)
    {
        // Save the camera's position before the shaking starts
        originalPos = transform.localPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            // Calculate a random offset based on intensity
            float x = Random.Range(-1f, 1f) * intensity;
            float y = Random.Range(-1f, 1f) * intensity;

            // Apply the offset to the local position
            transform.localPosition = new Vector3(originalPos.x + x, originalPos.y + y, originalPos.z);

            elapsed += Time.deltaTime;
            yield return null; // Wait for the next frame
        }

        // Reset the camera to its exact original position
        transform.localPosition = originalPos;
        shakeCoroutine = null;
    }
}