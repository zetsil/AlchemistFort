using UnityEngine;

public class PlayerStats : AllyEntity
{
    [Header("Stamina Settings")]
    public float maxStamina = 100f;
    public float currentStamina;
    public float staminaRegenRate = 20f;
    public float sprintCost = 30f;

    private FirstPersonController controller;

    protected override void Start()
    {
        base.Start(); // Apelează Start-ul din Entity (setează viața din SO)
        currentStamina = maxStamina;
        controller = GetComponent<FirstPersonController>();
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
        // Overridem Die ca să nu distrugem obiectul Player instant (Destroy(gameObject))
        // Mai bine activăm un ecran de Game Over sau respawn
        Debug.Log("Jucătorul a murit! Încărcare ecran Game Over...");
        controller.playerCanMove = false;
        controller.cameraCanMove = false;
        // base.Die(); // NU apelăm base.Die() dacă nu vrem să dispară Player-ul de pe ecran
    }
}
