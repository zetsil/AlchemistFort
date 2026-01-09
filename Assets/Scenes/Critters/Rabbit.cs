using UnityEngine;
using System.Collections.Generic;
using System.Collections;


// 💡 Interfață pentru accesul la BasePoint (Opțiunea 3 din discuția anterioară)
// Aceasta permite stării să acceseze BasePoint doar pe NPC-urile care îl au.
public interface IHasBasePoint
{
    Transform BasePoint { get; }
}

public class Rabbit : NPCBase, IHasBasePoint // Implementăm noua interfață
{
    // 🐇 1. Adăugăm referința la BasePoint (pentru a trage obiectul din Inspector)
    [Header("Rabbit Settings")]
    [Tooltip("Punctul în jurul căruia se va plimba Iepurele.")]
    [SerializeField]
    private Transform basePoint;

    public Transform BasePoint => basePoint; // Implementarea IHasBasePoint
    public float detectionRange = 10f;
    private bool playerInRange = false;
    private Transform player;
    private Coroutine hideExitTimerCoroutine;
    public float hideExitDelay = 5f;
    public float alertRadius = 5f;

    // 🔹 Instanțierea stării specifice Rabbit-ului
    private readonly WanderAroundPointState wanderAroundPointState = new WanderAroundPointState();
    public readonly RunToHideState runToHideState = new RunToHideState();


    // 3. Suprascriem Awake (Corecție: folosim base.Awake() pentru a inițializa starea)
    public new void Awake()
    {
        // Setăm proprietățile înainte de base.Awake()
        SetSpeed(0.5f);
        AttackSpeed = 0;

        // Dacă BasePoint nu este setat în Inspector, îl setăm la poziția curentă a NPC-ului
        if (basePoint == null)
        {
            GameObject bp = new GameObject("Rabbit_BasePoint");
            bp.transform.position = transform.position;
            basePoint = bp.transform;
        }

        base.Awake();
    }
    

    void Start()
    {
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
    }

    // 4. Suprascriem SetupStateLevels
    protected override void SetupStateLevels()
    {
        // Golim lista moștenită și o redefinim
        StateLevels.Clear();

        // 🟢 Nivelul 0 (Safe): Idle, WanderAroundPoint
        StateLevels.Add(new List<INPCState> { idleState, wanderAroundPointState });

        // 🟡 Nivelul 1 (Run/Flee/Hide): 
        StateLevels.Add(new List<INPCState> { runToHideState, hideState });
    }

    // 5. Suprascriem ToWander pentru a folosi starea specifică
    public override void ToWander()
    {
        // Folosește starea specifică WanderAroundPointState
        ChangeState(wanderAroundPointState);

        if (Agent != null)
        {
            Agent.isStopped = false;
        }
    }


    protected override void Update()
    {
        // 1️ Apelează Update-ul de bază
        base.Update();

        // 2️ Caută player-ul o singură dată
        if (player == null)
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
        }

        if (player == null) return;

        // 3️ Verifică distanța față de player
        float distance = Vector3.Distance(transform.position, player.position);

        if (!playerInRange && distance <= detectionRange)
        {
            // Player detectat -> fugi
            playerInRange = true;

            AlertNearbyRabbits();
            ChangeLevel(1); // ex: RunToHideState
        }
        else if (currentState is HideState && distance > detectionRange * 3 && hideExitTimerCoroutine == null)
        {
            // Player a ieșit din rază
            playerInRange = false;

            Debug.Log("🕒 Player-ul a plecat. Resetez timerul de ieșire din ascunzătoare...");

            // Dacă timerul rulează deja, oprește-l
            if (hideExitTimerCoroutine != null)
            {
                StopCoroutine(hideExitTimerCoroutine);
                hideExitTimerCoroutine = null;
            }

            // Pornește din nou timerul
            hideExitTimerCoroutine = StartCoroutine(HideExitTimer());
        }
    }


    private void AlertNearbyRabbits()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, alertRadius);
        foreach (Collider c in colliders)
        {
            Rabbit rabbit = c.GetComponent<Rabbit>();
            if (rabbit != null && rabbit != this)
            {
                rabbit.OnAlerted();
            }
        }
    }

    public void OnAlerted()
    {
        // NPC-ul reacționează la alertă

        ChangeLevel(1); // fugi către ascunzătoare
        
    }


    private IEnumerator HideExitTimer()
    {
        yield return new WaitForSeconds(hideExitDelay);

        Debug.Log("✅ Timer terminat. NPC-ul iese din ascunzătoare.");
        ChangeLevel(0); // revine la Idle
        hideExitTimerCoroutine = null;
        playerInRange = false;
    }

    // ----------------------------------------------------
    // 🐇  CLASE DE STARE SPECIFICE
    // ----------------------------------------------------

    public class WanderAroundPointState : INPCState
    {
        private const float DestinationCheckDistance = 0.5f;
        private const float wanderRadius = 10f;

        // Trebuie să te asiguri că ai un enum corespunzător în NPCBase (ex: NPCStateID.Wander)
        public NPCBase.NPCStateID StateID => NPCBase.NPCStateID.Wander;

        public void EnterState(NPCBase npc)
        {

        }
        public void ExitState(NPCBase npc) { }

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
                 (!npc.Agent.pathPending && npc.Agent.remainingDistance <= DestinationCheckDistance)))
            {

                // Tranziție la Idle dacă a ajuns la destinație
                if (npc.Agent.remainingDistance <= DestinationCheckDistance && npc.Agent.hasPath)
                {
                    npc.ToIdle();
                    return;
                }

                // Calculează o destinație nouă, centrată pe 'origin'
                Vector3 newDestination = npc.GetRandomNavMeshPoint(origin, wanderRadius);

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
    }
}


public class RunToHideState : INPCState
{
    public NPCBase.NPCStateID StateID => NPCBase.NPCStateID.Run;

    public void EnterState(NPCBase npc)
    {
        if (npc is IHasBasePoint basePointUser && basePointUser.BasePoint != null)
        {
            npc.Agent.isStopped = false;
            npc.Agent.stoppingDistance = 0.5f; // se oprește înainte de punct
            npc.Agent.SetDestination(basePointUser.BasePoint.position);
            npc.SetSpeed(npc.Speed * 2.5f); // Crește viteza pentru a simula fuga

        }
        else
        {
            Debug.LogError($"{npc.GetType().Name} nu are BasePoint definit. Trecere la Idle.");
            npc.ToIdle();
        }
    }

    public void DoState(NPCBase npc)
    {
        if (!npc.Agent.isOnNavMesh) return;

        // Dacă e aproape de destinație
        if (!npc.Agent.pathPending && npc.Agent.remainingDistance <= npc.Agent.stoppingDistance + 0.5f)
        {
            // Și viteza efectivă e mică (s-a oprit)
            if (npc.Agent.velocity.sqrMagnitude < 0.1f)
            {
                npc.Agent.isStopped = true;
                npc.Agent.ResetPath();

                Debug.Log("✅ A ajuns în punctul de ascundere!");
                npc.ToHide();
            }
        }
    }

    public void ExitState(NPCBase npc)
    {
        npc.SetSpeed(npc.Speed / 2.5f);
    }
}
