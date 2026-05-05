using UnityEngine;
using UnityEngine.AI; // <--- IMPORTANT: Ai nevoie de asta pentru NavMeshAgent

public class Enemy : Entity
{
    private Animator animator;
    private NavMeshAgent agent; // <--- Definim agentul aici

    [Header("Behavior Settings")]
    [SerializeField] private string hitTriggerName = "Hit";

    [Header("Ragdoll Components")]
    private Rigidbody[] ragdollRigidbodies;
    private Collider[] ragdollColliders;

    protected override void Start()
    {
        base.Start();

        animator = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();

        // Cache pentru toate componentele de ragdoll (oasele copil)
        ragdollRigidbodies = GetComponentsInChildren<Rigidbody>();
        ragdollColliders = GetComponentsInChildren<Collider>();

        // Dezactivăm ragdoll-ul la început
        ToggleRagdoll(false);

        if (agent == null)
        {
            Debug.LogWarning($"NavMeshAgent lipsește de pe {gameObject.name}!");
        }
    }

    public override void TakeDamage(float baseDamage, ToolType attackingToolType = ToolType.None)
    {
        if (currentHealth <= 0) return;

        base.TakeDamage(baseDamage, attackingToolType);

        if (animator != null && currentHealth > 0)
        {
            // Resetăm trigger-ul înainte de setare pentru a evita dubla declanșare
            animator.ResetTrigger(hitTriggerName);
            animator.SetTrigger(hitTriggerName);
        }
    }

    protected override void Die() 
    {
        // 1. Logica specifică pentru inamic înainte de "moartea" oficială
        isDead = true ;
        if (agent != null)
        {
            agent.isStopped = true;
            agent.enabled = false;
        }

        if (animator != null) animator.enabled = false;

        // Dezactivăm coliziunea principală ca să nu încurce oasele
        Collider mainCol = GetComponent<Collider>();
        if (mainCol != null) mainCol.enabled = false;

        Rigidbody mainRb = GetComponent<Rigidbody>();
        if (mainRb != null) mainRb.isKinematic = true;

        // 2. Activăm Ragdoll-ul
        ToggleRagdoll(true);

        // 3. Apelăm base.Die și îi pasăm delay-ul de 5 secunde
        // Asta va rula Loot-ul, State-ul și va programa Destroy(gameObject, 5f)
        base.Die();
    }

    private void ToggleRagdoll(bool state)
    {
        foreach (var rb in ragdollRigidbodies)
        {
            // Când e mort (state=true), kinematic e FALSE (fizica preia controlul)
            rb.isKinematic = !state;
            rb.useGravity = state;
        }

        foreach (var col in ragdollColliders)
        {
            // Nu dezactivăm collider-ul principal al obiectului aici (e gestionat în Die)
            if (col.gameObject != this.gameObject)
            {
                col.enabled = state;
            }
        }
    }
    
    protected override void Update()
    {
        base.Update();

        // Verificăm dacă agentul e activ și pe NavMesh pentru a evita erorile
        if (agent != null && agent.isOnNavMesh && animator != null)
        {
            // Calculăm viteza relativă (0 la stop, 1 la viteză maximă)
            float speedPercent = agent.velocity.magnitude / agent.speed;

            // Trimitem valoarea către parametrul "Speed" din Blend Tree
            // 0.1f face ca tranziția între Idle și Walk să fie foarte lină
            animator.SetFloat("Speed", speedPercent, 0.1f, Time.deltaTime);
        }
    }
}