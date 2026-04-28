using UnityEngine;
using UnityEngine.AI; // <--- IMPORTANT: Ai nevoie de asta pentru NavMeshAgent

public class Enemy : Entity
{
    private Animator animator;
    private NavMeshAgent agent; // <--- Definim agentul aici

    [Header("Behavior Settings")]
    [SerializeField] private string hitTriggerName = "Hit";

    protected override void Start()
    {
        // Execută logica de bază din Entity (viață, materiale, etc.)
        base.Start();

        animator = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>(); // <--- Inițializăm agentul
        
        // Opțional: Sincronizare viteză animație cu agentul
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
        // Dezactivăm agentul la moarte ca să nu se mai miște cadavrul
        if (agent != null) agent.enabled = false;
        
        base.Die();
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