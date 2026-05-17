using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class Enemy : Entity
{
    private Animator animator;
    private NavMeshAgent agent;

    [Header("Behavior Settings")]
    [SerializeField] private string hitTriggerName = "Hit";

    [Header("Hit Hitstop")]
    [Tooltip("0 = freeze total, 1 = nicio schimbare")]
    [SerializeField] [Range(0f, 1f)] private float hitTimeScale = 0.05f;
    [SerializeField] private float hitDuration = 0.06f;

    [Header("Death Hitstop")]
    [SerializeField] [Range(0f, 1f)] private float deathTimeScale = 0f;
    [SerializeField] private float deathHitstopDuration = 0.12f;

    [Header("Death Mid-Air Pause")]
    [Tooltip("Cat timp zboara inainte sa se opreasca in aer (secunde reale)")]
    [SerializeField] private float flyDuration = 0.18f;

    [Tooltip("Cat timp sta suspendat in aer inainte sa-si continue traiectoria (secunde reale)")]
    [SerializeField] private float midAirPauseDuration = 0.15f;

    [Header("Ragdoll")]
    private Rigidbody[] ragdollRigidbodies;
    private Collider[] ragdollColliders;
    [SerializeField] private Rigidbody ragdollTargetBone;

    [Header("Death Knockback")]
    [SerializeField] private float backwardForce = 10f;
    [SerializeField] private float upwardForce = 5f;

    [Header("Weapon Drop")]
    [Tooltip("Trage aici arma din ierarhia inamicului care are scriptul DecoupledLoot pe ea")]
    [SerializeField] private DecoupledLoot weaponLoot;

    // Vitezele salvate la momentul pauzei, pentru a le restaura dupa
    private Vector3[] savedVelocities;
    private Vector3[] savedAngularVelocities;

    protected override void Start()
    {
        base.Start();

        animator = GetComponent<Animator>();
        agent    = GetComponent<NavMeshAgent>();

        ragdollRigidbodies = GetComponentsInChildren<Rigidbody>();
        ragdollColliders   = GetComponentsInChildren<Collider>();

        ToggleRagdoll(false);

        if (ragdollTargetBone == null)
        {
            foreach (var rb in ragdollRigidbodies)
            {
                if (rb.gameObject != gameObject)
                {
                    ragdollTargetBone = rb;
                    break;
                }
            }
        }

        if (weaponLoot == null)
            weaponLoot = GetComponentInChildren<DecoupledLoot>();

        if (HitstopManager.Instance == null)
        {
            var go = new GameObject("HitstopManager");
            go.AddComponent<HitstopManager>();
        }
    }

    // -------------------------------------------------------------------------
    // Combat
    // -------------------------------------------------------------------------

    public override void TakeDamage(float baseDamage, ToolType attackingToolType = ToolType.None)
    {
        if (currentHealth <= 0) return;

        base.TakeDamage(baseDamage, attackingToolType);

        if (currentHealth > 0)
        {
            TriggerHitAnimation();
            GlobalEvents.RequestHitstop(hitDuration, hitTimeScale);
        }
    }

    private void TriggerHitAnimation()
    {
        if (animator == null) return;
        animator.ResetTrigger(hitTriggerName);
        animator.SetTrigger(hitTriggerName);
    }

    // -------------------------------------------------------------------------
    // Moarte
    // -------------------------------------------------------------------------

    protected override void Die()
    {
        if (isDead) return;
        isDead = true;

        DisableMovement();
        DisableMainColliderAndRigidbody();

        if (weaponLoot != null)
            weaponLoot.DecoupleAndDrop();

        GlobalEvents.RequestHitstop(deathHitstopDuration, deathTimeScale);
        ToggleRagdoll(true);
        StartCoroutine(DeathSequence());

        base.Die();
    }

    private IEnumerator DeathSequence()
    {
        // Faza 1: hitstop
        yield return new WaitForSecondsRealtime(deathHitstopDuration);

        // Faza 2: knockback — inamicul incepe sa zboare
        ApplyDeathKnockback();

        // Faza 3: zboara liber
        yield return new WaitForSecondsRealtime(flyDuration);

        // Faza 4: salvam vitezele si inghetam in aer
        SaveAndFreezeRagdoll();

        // Faza 5: pauza in aer
        yield return new WaitForSecondsRealtime(midAirPauseDuration);

        // Faza 6: restauram vitezele — continua traiectoria
        RestoreAndUnfreezeRagdoll();
    }

    // -------------------------------------------------------------------------
    // Mid-Air Pause
    // -------------------------------------------------------------------------

    private void SaveAndFreezeRagdoll()
    {
        savedVelocities        = new Vector3[ragdollRigidbodies.Length];
        savedAngularVelocities = new Vector3[ragdollRigidbodies.Length];

        for (int i = 0; i < ragdollRigidbodies.Length; i++)
        {
            var rb = ragdollRigidbodies[i];
            if (rb.gameObject == gameObject) continue;

            savedVelocities[i]        = rb.linearVelocity;
            savedAngularVelocities[i] = rb.angularVelocity;

            rb.linearVelocity  = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.useGravity      = false;
        }
    }

    private void RestoreAndUnfreezeRagdoll()
    {
        for (int i = 0; i < ragdollRigidbodies.Length; i++)
        {
            var rb = ragdollRigidbodies[i];
            if (rb.gameObject == gameObject) continue;

            rb.useGravity      = true;
            rb.linearVelocity  = savedVelocities[i];
            rb.angularVelocity = savedAngularVelocities[i];
        }
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private void ApplyDeathKnockback()
    {
        if (ragdollTargetBone == null) return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        Vector3 dir = transform.position - player.transform.position;
        dir.y = 0f;
        dir.Normalize();

        ragdollTargetBone.AddForce(
            dir * backwardForce + Vector3.up * upwardForce,
            ForceMode.VelocityChange
        );
    }

    private void DisableMovement()
    {
        if (agent != null)
        {
            agent.isStopped = true;
            agent.enabled   = false;
        }
        if (animator != null) animator.enabled = false;
    }

    private void DisableMainColliderAndRigidbody()
    {
        Collider mainCol = GetComponent<Collider>();
        if (mainCol != null) mainCol.enabled = false;

        Rigidbody mainRb = GetComponent<Rigidbody>();
        if (mainRb != null) mainRb.isKinematic = true;
    }

    private void ToggleRagdoll(bool state)
    {
        foreach (var rb in ragdollRigidbodies)
        {
            if (rb.gameObject == gameObject) continue;
            rb.isKinematic = !state;
            rb.useGravity  = state;
        }
        foreach (var col in ragdollColliders)
        {
            if (col.gameObject == gameObject) continue;
            col.enabled = state;
        }
    }

    // -------------------------------------------------------------------------
    // Update
    // -------------------------------------------------------------------------

    protected override void Update()
    {
        if (isDead) return;

        base.Update();

        if (agent != null && agent.isOnNavMesh && animator != null)
        {
            float speedPercent = agent.velocity.magnitude / agent.speed;
            animator.SetFloat("Speed", speedPercent, 0.1f, Time.deltaTime);
        }
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }
}