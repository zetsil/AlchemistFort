using UnityEngine;
using System.Collections;

public class CameraAnimations : MonoBehaviour
{
    [Header("Referințe")]
    [SerializeField] private Camera mainCamera;
    private Coroutine punchCoroutine;

    [Header("Setări Impact Axe")]
    [SerializeField] private float shakeAngle = 0.6f;
    [SerializeField] private float shakeDuration = 0f;
    [SerializeField] private float punchAmount = -0.1f;
    [SerializeField] private float punchDuration = 0.2f;
    [SerializeField] private float punchReturnDuration = 0.1f;

    [Header("Setări Impact Sword")]
    [SerializeField] private float swordSlideAmount = 0.05f;   // Cât se mișcă stânga/dreapta/jos
    [SerializeField] private float swordSlideDuration = 0.08f;       // Rapid la impact
    [SerializeField] private float swordReturnDuration = 0.14f;      // Revenire mai lentă

    [SerializeField] private float pickaxeShakeIntensity = 0.8f;
    [SerializeField] private float pickaxeFinalShakeIntensity = 1.5f;
    [SerializeField] private float pickaxeShakeDuration = 0.15f;

    private Vector3 originalLocalPos;
    private Quaternion originalLocalRot;
    private Coroutine impactCoroutine;

    private void Awake()
    {
        if (mainCamera == null) mainCamera = GetComponent<Camera>();

        if (mainCamera != null)
        {
            originalLocalPos = mainCamera.transform.localPosition;
            originalLocalRot = mainCamera.transform.localRotation;
        }
    }

    private void OnEnable() => GlobalEvents.OnAttackImpactPerformed += HandleImpact;
    private void OnDisable() => GlobalEvents.OnAttackImpactPerformed -= HandleImpact;

    private void HandleImpact(string attackType)
    {
        if (impactCoroutine != null) StopCoroutine(impactCoroutine);
        if (punchCoroutine != null) StopCoroutine(punchCoroutine); // oprește punch-ul vechi

        // Refreshează origin la momentul impactului (fix pentru child mobil)
        // originalLocalPos = mainCamera.transform.localPosition;
        // originalLocalRot = mainCamera.transform.localRotation;

        switch (attackType)
        {
            case "Axe":
                impactCoroutine = StartCoroutine(ImpactRoutine(shakeAngle, shakeDuration));
                break;
            case "Pickaxe":
                impactCoroutine = StartCoroutine(ImpactRoutine(pickaxeShakeIntensity, pickaxeShakeDuration));
                break;
            case "PickaxeFinal":
                impactCoroutine = StartCoroutine(ImpactRoutine(pickaxeFinalShakeIntensity, pickaxeShakeDuration * 1.5f));
                break;
            case "SwordSlashLeft":
                impactCoroutine = StartCoroutine(SwordSlideRoutine(new Vector3(-1f, 0f, 0f)));
                break;
            case "SwordSlashRight":
                impactCoroutine = StartCoroutine(SwordSlideRoutine(new Vector3(1f, 0f, 0f)));
                break;
            case "SwordFinal":
                impactCoroutine = StartCoroutine(SwordSlideRoutine(new Vector3(0f, -1f, 0f)));
                break;
        }
    }

    // ── AXE ────────────────────────────────────────────────────────────────────

    private IEnumerator ImpactRoutine(float intensity, float duration)
    {
        punchCoroutine = StartCoroutine(PunchRoutine());

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            float dampen = 1f - Mathf.Pow(progress, 2f);

            float angleZ = Random.Range(-intensity, intensity) * dampen;
            float angleX = Random.Range(-intensity * 0.5f, intensity * 0.5f) * dampen;

            // Citește rotația CURENTĂ în acest frame (după head bob, după look)
            // și adaugă shake peste ea
            Quaternion currentRot = mainCamera.transform.localRotation;
            mainCamera.transform.localRotation = currentRot * Quaternion.Euler(angleX, 0f, angleZ);

            yield return null;

            // Resetează shake-ul adăugat, astfel încât în frame-ul următor
            // citim din nou rotația curată
            mainCamera.transform.localRotation = currentRot;
        }
    }

    private IEnumerator PunchRoutine()
    {
        float elapsed = 0f;
        while (elapsed < punchDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / punchDuration);
            float offset = Mathf.Lerp(0f, punchAmount, t);
            // Adaugă offset la poziția curentă, nu la un snapshot
            mainCamera.transform.localPosition += Vector3.forward * offset;
            yield return null;
            mainCamera.transform.localPosition -= Vector3.forward * offset; // curăță
        }

        elapsed = 0f;
        while (elapsed < punchReturnDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / punchReturnDuration);
            float offset = Mathf.Lerp(punchAmount, 0f, t);
            mainCamera.transform.localPosition += Vector3.forward * offset;
            yield return null;
            mainCamera.transform.localPosition -= Vector3.forward * offset; // curăță
        }
    }

    // ── SWORD ───────────────────────────────────────────────────────────────────

    private IEnumerator SwordSlideRoutine(Vector3 direction)
    {
        Transform camTransform = mainCamera.transform;
        
        // Folosim right-ul LOCAL al camerei, nu world space
        Vector3 localDirection = camTransform.right * direction.x 
                            + camTransform.up * direction.y;
        
        Vector3 slideTarget = originalLocalPos + localDirection * swordSlideAmount;

        // Slide spre direcție
        float elapsed = 0f;
        while (elapsed < swordSlideDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / swordSlideDuration);
            camTransform.localPosition = Vector3.Lerp(originalLocalPos, slideTarget, t);
            yield return null;
        }

        // Revenire
        elapsed = 0f;
        while (elapsed < swordReturnDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / swordReturnDuration);
            camTransform.localPosition = Vector3.Lerp(slideTarget, originalLocalPos, t);
            yield return null;
        }

        camTransform.localPosition = originalLocalPos;
    }
}