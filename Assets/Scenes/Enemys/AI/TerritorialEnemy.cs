using UnityEngine;
using System.Collections.Generic;

public class TerritorialEnemy : NPCBase, IHasBasePoint
{
    [Header("Territorial Settings")]
    [Tooltip("Punctul central pe care îl păzește.")]
    [SerializeField] private Transform basePoint;
    
    [Tooltip("Distanța la care te vede și începe să te urmărească.")]
    public float detectionRange = 10f;
    
    [Tooltip("Distanța maximă față de inamic la care renunță la urmărire.")]
    public float chaseLimitRange = 15f;

    [Tooltip("Raza în care patrulează în jurul punctului de bază.")]
    public float patrolRadius = 8f;

    public float attackRange = 1.2f;

    // Proprietate din interfață
    public Transform BasePoint => basePoint;

    // Referință la Player
    private Transform playerTransform;

    // --- STĂRILE INAMICULUI ---
    // 1. Starea de patrulare (copiată logic de la Iepure, dar adaptată)
    private readonly PatrolAroundPointState patrolState = new PatrolAroundPointState();
    // 2. Starea de urmărire activă
    private readonly ChasePlayerState chaseState = new ChasePlayerState();


    public new void Awake()
    {
        // Setări implicite
        SetSpeed(2.0f);   // Viteza de alergare
        AttackSpeed = 1.5f; // Cât de des atacă

        // Configurare BasePoint
        if (basePoint == null)
        {
            GameObject bp = new GameObject(gameObject.name + "_GuardPost");
            bp.transform.position = transform.position;
            basePoint = bp.transform;
        }

        base.Awake();
    }

    private void Start()
    {
        // Găsim player-ul
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) playerTransform = p.transform;
    }

    protected override void SetupStateLevels()
    {
        StateLevels.Clear();

        // 🟢 NIVELUL 0: CALM (Patrulează zona și stă Idle)
        StateLevels.Add(new List<INPCState> { idleState, patrolState });

        // 🔴 NIVELUL 1: AGRESIV (Urmărește și Atacă)
        // Când trece pe nivelul 1, va intra în Chase, iar Chase va declanșa Attack când e aproape.
        StateLevels.Add(new List<INPCState> { chaseState, attackState });
    }

    // Facem Override la ToWander pentru a folosi Patrol-ul nostru specific, nu cel generic
    public override void ToWander()
    {
        ChangeState(patrolState);
        Agent.isStopped = false;
    }

    protected override void Update()
    {
        base.Update();

        if (playerTransform == null) return;

        // Calculăm distanța până la jucător
        float distToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        // --- LOGICA DE SCHIMBARE A NIVELULUI ---

        // CAZ 1: Suntem CALMI (Lv 0) -> Vedem Playerul -> Devenim AGRESIVI (Lv 1)
        if (currentStateLvl == 0)
        {
            if (distToPlayer <= detectionRange)
            {
                Debug.Log($"👀 {gameObject.name} a detectat intrusul! Începe urmărirea.");
                Target = playerTransform.gameObject; // Setăm ținta pentru ChaseState
                ChangeLevel(1); 
            }
        }
        // CAZ 2: Suntem AGRESIVI (Lv 1) -> Playerul fuge departe -> Revenim la CALM (Lv 0)
        else if (currentStateLvl == 1)
        {
            if (distToPlayer > chaseLimitRange)
            {
                Debug.Log($"🏳️ {gameObject.name} a renunțat la urmărire. Se întoarce la post.");
                Target = null;
                ChangeLevel(0); // Revine automat la Idle/Patrol
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Verificăm dacă suntem în nivelul de luptă (Level 1)
        if (currentStateLvl == 1)
        {
            if (collision.gameObject.CompareTag("Player"))
            {
                Debug.Log("💥 Ciocnire directă cu Player-ul! Forțez atacul.");
                ToAttack(collision.gameObject);
            }
        }
    }

    // Desenăm razele în Editor pentru a vedea ușor zonele
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange); // Zona de alertă

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, chaseLimitRange); // Zona maximă de urmărire

        if (basePoint != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(basePoint.position, patrolRadius); // Zona de patrulare
        }
    }
}

// ======================================================================================
// STĂRILE SPECIFICE
// ======================================================================================

// 1. STAREA DE PATRULARE (Similară cu Iepurele, dar specifică acestui Inamic)
public class PatrolAroundPointState : INPCState
{
    public NPCBase.NPCStateID StateID => NPCBase.NPCStateID.Wander;
    private const float DestinationTolerance = 1.0f;

    public void EnterState(NPCBase npc)
    {
        // Putem reduce viteza când patrulează
        npc.SetSpeed(npc.Speed * 0.5f); 
    }

    public void DoState(NPCBase npc)
    {
         // 💡 Obține BasePoint-ul doar dacă NPC-ul implementează IHasBasePoint
            Vector3 origin = npc.Position; // Fallback: poziția curentă

            if (npc is IHasBasePoint basePointUser && basePointUser.BasePoint != null)
            {
                origin = basePointUser.BasePoint.position;
            }

            // 1. Verifică dacă trebuie să setăm o destinație nouă sau am ajuns
            if (npc.Agent.isOnNavMesh &&
                (!npc.Agent.hasPath ||
                 (!npc.Agent.pathPending && npc.Agent.remainingDistance <= DestinationTolerance)))
            {

                // Tranziție la Idle dacă a ajuns la destinație
                if (npc.Agent.remainingDistance <= DestinationTolerance && npc.Agent.hasPath)
                {
                    npc.ToIdle();
                    return;
                }

                // Calculează o destinație nouă, centrată pe 'origin'
                Vector3 newDestination = npc.GetRandomNavMeshPoint(origin, 10f);

                if (newDestination != Vector3.zero)
                {
                    npc.Agent.SetDestination(newDestination);
                }
                else
                {
                    npc.ToIdle();
                }
            }
    }

    public void ExitState(NPCBase npc) 
    {
        // Resetăm viteza la normal când iese din patrulare (pentru a ataca rapid)
        npc.SetSpeed(npc.Speed * 2.0f); 
    }
}

// 2. STAREA DE URMĂRIRE (Chase)
public class ChasePlayerState : INPCState
{
    public NPCBase.NPCStateID StateID => NPCBase.NPCStateID.Run;

    public void EnterState(NPCBase npc)
    {
        npc.Agent.isStopped = false;
        if(npc.animator != null) npc.animator.SetTrigger("DoMove");
    }

    public void DoState(NPCBase npc)
    {
        if (npc.Target == null) return;

        // Castăm npc la TerritorialEnemy pentru a-i accesa variabilele specifice
        TerritorialEnemy enemy = npc as TerritorialEnemy;
        if (enemy == null) return;

        npc.Agent.SetDestination(npc.Target.transform.position);

        float dist = Vector3.Distance(npc.transform.position, npc.Target.transform.position);
        
        // Folosim attackRange din clasa inamicului (cel setat în Inspector)
        if (dist <= enemy.attackRange)
        {
            npc.ToAttack(npc.Target);
            return;
        }

        // Folosim tot variabila din inspector și pentru verificarea de blocare
        if (npc.Agent.velocity.sqrMagnitude < 0.01f && dist < enemy.attackRange + 0.5f)
        {
            npc.ToAttack(npc.Target);
        }
    }

    public void ExitState(NPCBase npc) { }
}