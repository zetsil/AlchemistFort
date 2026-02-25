using UnityEngine;
using System.Collections;

public class PlayerStats : AllyEntity
{
    [Header("Stamina Settings")]
    public float maxStamina = 100f;
    public float currentStamina;
    public float staminaRegenRate = 20f;
    public float sprintCost = 30f;
    public static event System.Action<PlayerStats> OnPlayerStatsReady;

    private bool isInToxicGas = false;


    private FirstPersonController controller;


    private void OnEnable()
    {
        // Ne abonăm la semnalele specifice emise de ToxicityListener
        GlobalEvents.OnToxicGasStart += StartToxicityEffect;
        GlobalEvents.OnToxicGasStop += StopToxicityEffect;
    }

    private void OnDisable()
    {
        // Dezabonare pentru a evita memory leaks
        GlobalEvents.OnToxicGasStart -= StartToxicityEffect;
        GlobalEvents.OnToxicGasStop -= StopToxicityEffect;
    }


    private void StartToxicityEffect()
    {
        isInToxicGas = true;
        Debug.Log("<color=green>PlayerStats: Recieved Toxic Gas Start Signal!</color>");
        // Aici poți adăuga un efect vizual pe cameră (ex: overlay verde)
    }

    private void StopToxicityEffect()
    {
        isInToxicGas = false;
        Debug.Log("<color=red>PlayerStats: Recieved Toxic Gas Stop Signal!</color>");
    }

    public void TakeToxicDamage(float amount)
    {
        if (isDead) return;

        base.TakeDamage(amount, ToolType.Claw);
        
        Debug.Log($"<color=purple>☣️ Player taking {amount} toxic damage! Health remaining: {currentHealth}</color>");
        
        // OPȚIONAL: Poți adăuga un "screen shake" mic sau un sunet de tuse aici
    }

    protected override void Start()
    {
        base.Start(); // Apelează Start-ul din Entity (setează viața din SO)
        currentStamina = maxStamina;
        controller = GetComponent<FirstPersonController>();

        OnPlayerStatsReady?.Invoke(this);
    }

    protected override void Update()
    {
        base.Update();
        HandleStamina();
    }

    private void HandleStamina()
    {
        // Verificăm dacă jucătorul sprintează activ folosind input-ul din controller
        bool isMoving = Input.GetAxis("Horizontal") != 0 || Input.GetAxis("Vertical") != 0;
        bool isSprinting = Input.GetKey(controller.sprintKey) && isMoving && controller.playerCanMove;

        if (isSprinting && currentStamina > 0)
        {
            currentStamina -= sprintCost * Time.deltaTime;
            if (currentStamina <= 0)
            {
                currentStamina = 0;
                controller.enableSprint = false; // Îi tăiem "picioarele" din controller
            }
        }
        else
        {
            // Regenerare
            if (currentStamina < maxStamina)
                currentStamina += staminaRegenRate * Time.deltaTime;

            if (currentStamina >= 10f) // Prag minim de recuperare
                controller.enableSprint = true;
        }
    }

    protected override void Die()
    {
        if (isDead) return;
        
        isDead = true;
        Debug.Log("💀 PlayerStats: Jucătorul a murit!");

        // 1. Dezactivăm controalele
        controller.playerCanMove = false;
        controller.cameraCanMove = false;
        controller.enableSprint = false;

        // 2. Declanșăm efectul de "cădere" a camerei
        StartCoroutine(FallToGroundRoutine());

        // 3. Trimitem semnalul global
        GlobalEvents.NotifyPlayerDeath();
    }

    private IEnumerator FallToGroundRoutine()
    {
        Transform camTransform = Camera.main.transform;
        Vector3 startPosition = camTransform.localPosition;
        Quaternion startRotation = camTransform.localRotation;

        // Definirea poziției de "mort la pământ"
        Vector3 targetPosition = new Vector3(startPosition.x, -0.8f, startPosition.z); // Coboară camera spre picioare
        Quaternion targetRotation = Quaternion.Euler(startRotation.eulerAngles.x, startRotation.eulerAngles.y, 60f); // Înclinație laterală

        float elapsed = 0f;
        float duration = 1.2f; // Cât de repede cade la pământ

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Folosim un SmoothStep pentru o cădere mai naturală (accelerează la început)
            float curve = t * t; 

            camTransform.localPosition = Vector3.Lerp(startPosition, targetPosition, curve);
            camTransform.localRotation = Quaternion.Slerp(startRotation, targetRotation, curve);

            yield return null;
        }
        
        // Asigurăm poziția finală
        camTransform.localPosition = targetPosition;
        camTransform.localRotation = targetRotation;
    }
}
