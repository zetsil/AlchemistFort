using UnityEngine;
using System.Collections.Generic;

public class ZombieNPC : NPCBase, IHasBasePoint
{
    [Header("Zombie Settings")]
    [Tooltip("Cripta/Mormântul unde se ascunde ZIUA.")]
    [SerializeField] private Transform hidePoint;

    [Tooltip("Șansa (0.0 - 1.0) ca zombiul să atace playerul când ajunge la bază.")]
    [Range(0f, 1f)]
    public float aggroPlayerChance = 0.3f;
    [HideInInspector] public GameObject baseAccessPoint;
    [HideInInspector] public bool hasReachedAccessPoint = false;
    [HideInInspector] public bool zombieChoseBase = false;


    // Proprietăți pentru interfață și logică
    public Transform BasePoint => hidePoint;
    public Transform CrystalTarget { get; private set; }

    // --- STĂRI (Instanțe) ---
    public readonly RunToHideState runToHideState = new RunToHideState();
    public readonly MoveToBaseState moveToBaseState = new MoveToBaseState();
    public readonly ChooseTargetState chooseTargetState = new ChooseTargetState();
    public readonly ZombieMoveToState zombieMoveToState = new ZombieMoveToState();

    // 🚨 ATENȚIE: AttackBaseState a fost eliminată. Folosim attackState din NPCBase.

    public new void Awake()
    {
        // Setări specifice Zombie
        SetSpeed(1.5f);
        AttackSpeed = 2f;

        // 1. Găsirea punctului de ascundere (Cripta) - ZIUA
        if (hidePoint == null)
        {
            GameObject bp = new GameObject(gameObject.name + "_CryptPoint");
            bp.transform.position = transform.position;
            hidePoint = bp.transform;
        }

        // 2. Găsirea Cristalului (Tag "Base") - NOAPTEA
        GameObject crystalObj = GameObject.FindGameObjectWithTag("Base");
        if (crystalObj != null)
        {
            CrystalTarget = crystalObj.transform;
        }
        else
        {
            Debug.Log("❌ Zombie nu a găsit niciun obiect cu tag-ul 'Base' (Cristalul)!");
        }

        base.Awake();
    }
    // Resetăm bool-ul când se face noapte sau când zombiul este refolosit
    public void ResetBaseAccess()
    {
        hasReachedAccessPoint = false;
        // Optional: sterge si punctul vechi daca vrei unul nou de fiecare data
    }


    private void OnDestroy()
    {
        if (baseAccessPoint != null) Destroy(baseAccessPoint);
    }


    public override void TakeDamage(float baseDamage, ToolType attackingToolType = ToolType.None)
    {
        // 1. Aplică logica de bază de damage (scădere viață, Die())
        base.TakeDamage(baseDamage, attackingToolType);

        // O condiție simplă pentru a evita Aggro-ul dacă NPC-ul moare sau e imun
        if (currentHealth <= 0) return;
        if (currentHealth == MaxHealth && baseDamage > 0) return;

        // 2. Logica de AGGRO: Găsim Player-ul global (identificat prin Tag)
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            // Verificăm dacă Playerul nu este deja ținta curentă
            if (Target != player)
            {
                Debug.Log($"💥 Zombie {gameObject.name} a fost lovit ({attackingToolType}) și a făcut AGGRO către Player (Căutare Tag)!");

                // Setează noul Target
                Target = player;

                // Intră în starea de mișcare (care va urmări noul Target)
                ToMoveTo(player);
            }
        }
    }

    protected override void SetupStateLevels()
    {
        StateLevels.Clear();

        // ☀️ NIVELUL 0: ZIUA (Fuge la criptă)
        StateLevels.Add(new List<INPCState> { runToHideState, hideState });

        // 🌙 NIVELUL 1: NOAPTEA (Atacă Baza/Cristalul)
        // Dacă e noapte, pornește MoveToBaseState. Orice atac trece la attackState (generic).
        StateLevels.Add(new List<INPCState> { chooseTargetState, zombieMoveToState, attackState, wanderState });
    }


    public override void ToMoveTo(GameObject target)
    {
        // Această metodă se asigură că orice cerere de mișcare (inclusiv din ChooseTargetState) 
        // folosește logica complexă din ZombieMoveToState.
        Target = target;
        animator.SetTrigger("DoMove");
        ChangeState(zombieMoveToState);
    }

    protected override void Update()
    {
        base.Update();
        CheckDayNightCycle();
    }

    private void CheckDayNightCycle()
    {
        if (GameStateManager.Instance == null) return;

        bool isNight = GameStateManager.Instance.IsNight;

        if (isNight)
        {
            if (currentStateLvl != 1)
            {
                Debug.Log("🌙 Noapte: Zombie începe asediul asupra bazei.");
                ChangeLevel(1);
            }
        }
        else
        {
            if (currentStateLvl != 0)
            {
                Debug.Log("☀️ Zi: Zombie se retrage în criptă.");
                ChangeLevel(0);
            }
        }
    }
    
    protected override void Die()
    {
        // Notificăm sistemul de wave că un inamic a murit înainte de a fi distrus obiectul
        GlobalEvents.NotifyEnemyDeath(this);
        
        // Apelăm logica de bază (animații, distrugere loot, etc.)
        base.Die();
    }

    // Metodă helper pentru a trece în starea de atac normală (spre player)
    // public override void ToAttack()
    // {
    //     base.ToAttack();
    //     SetSpeed(Speed * 1.5f);
    // }

}



public class ZombieMoveToState : INPCState
{
    public NPCBase.NPCStateID StateID => NPCBase.NPCStateID.MoveToBase; 
    
    // ❌ AM ȘTERS: private const float ATTACK_RANGE_THRESHOLD = 2.0f;
    
    // Păstrăm restul constantelor care nu țin de stats
    private const float PLAYER_AGGRO_RANGE = 10.0f;
    private const float PLAYER_FLEE_RANGE = 12.0f;
    private const int ALLY_LAYER_MASK = 1 << 8;

    private const float ZOMBIE_WIDTH = 0.5f;
    private const float DETECT_RANGE = 1.5f;
    private const float BASE_ACCESS_RADIUS = 8f;

    public void EnterState(NPCBase npc)
    {
        ZombieNPC zombie = npc as ZombieNPC;
        if (zombie == null || npc.Target == null) { npc.ToIdle(); return; }

        npc.Agent.isStopped = false;

        // Când intrăm în urmărire, setăm distanța de oprire a agentului
        // să fie egală cu raza de atac, ca să nu intre în player
        npc.Agent.stoppingDistance = npc.attackStopRange;

        if (npc.animator != null) npc.animator.SetInteger("State", (int)StateID);
    }

    public void DoState(NPCBase npc)
    {
        ZombieNPC zombie = npc as ZombieNPC;
        if (zombie == null) return;

        // if (npc is TacticalZombieNPC tactical &&
        //     tactical.wasAttackWindowOpen)
        // {
        //     return; // ❌ NU ataca în Charge
        // }

        ReevaluateTargetPriority(zombie);

        if (npc.Target == null) 
        {
            if (npc.Agent.isOnNavMesh) npc.Agent.isStopped = true;
            return; 
        }
        
        if (npc.Target != null && npc.Agent.isOnNavMesh)
        {
            npc.Agent.SetDestination(npc.Target.transform.position);
        }

        // 3. Verificăm tranziția către Atac
        float distToTarget = Vector3.Distance(npc.transform.position, npc.Target.transform.position);
        
        // ✅ MODIFICARE AICI: Folosim npc.attackStopRange în loc de constanta fixă
        // Aceasta este valoarea setată în Inspector pe ZombieNPC
        if (distToTarget <= npc.attackStopRange && npc.Target != zombie.baseAccessPoint)
        {
            npc.ToAttack(npc.Target); 
        }
    }

    private void ReevaluateTargetPriority(ZombieNPC zombie)
    {
        Vector3 zombiePos = zombie.transform.position;

        // if (zombie is TacticalZombieNPC tactical &&
        // tactical.currentState == tactical.chargeState)
        // {
        //     return;
        // }

        // --- PASUL A: Detecție Aliați (SphereCast) ---
        // Prioritatea 1: Dacă are ceva imediat în față, se oprește să îl bată
        RaycastHit hit;
        if (Physics.SphereCast(zombiePos, ZOMBIE_WIDTH, zombie.transform.forward, out hit, DETECT_RANGE, ALLY_LAYER_MASK))
        {
            if (hit.collider.TryGetComponent<AllyEntity>(out var ally))
            {
                zombie.Target = ally.gameObject;
                return; // Ieșim, aliatul este prioritatea maximă
            }
        }

        // --- PASUL B: Logică Jucător (Aggro / Flee) ---
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        float distToPlayer = player != null ? Vector3.Distance(zombiePos, player.transform.position) : float.MaxValue;

        // 1. Verificăm dacă player-ul este în raza de detecție
        if (player != null && distToPlayer < PLAYER_AGGRO_RANGE)
        {
            // Dacă zombiul deja urmărește player-ul, nu facem nimic, mergem la return mai jos
            if (zombie.Target == player) 
            {
                // Totuși, verificăm Flee
                if (distToPlayer > PLAYER_FLEE_RANGE) 
                {
                    Debug.Log("🏃 Player-ul a fugit.");
                    zombie.Target = (zombie.hasReachedAccessPoint && zombie.CrystalTarget != null) 
                                    ? zombie.CrystalTarget.gameObject 
                                    : GetOrCreateBaseAccessPoint(zombie);
                    zombie.zombieChoseBase = false; // Resetăm decizia pentru a putea fi atras iar mai târziu
                }
                return; 
            }

            // Verificăm dacă zombiul a ajuns aproape de punctul de acces (ex: sub 15 metri de el)
            float distToAccessPoint = (zombie.baseAccessPoint != null) 
                ? Vector3.Distance(zombiePos, zombie.baseAccessPoint.transform.position) 
                : float.MaxValue;

            bool isNearObjective = distToAccessPoint < 15f || zombie.hasReachedAccessPoint;

            if (isNearObjective)
            {
                // --- ZOMBIUL ESTE APROAPE DE BAZĂ: Ia o decizie calculată ---
                if (!zombie.zombieChoseBase)
                {
                    if (Random.value < zombie.aggroPlayerChance)
                    {
                        Debug.Log("🧠 Aproape de bază, dar zombiul a ales totuși PLAYERUL.");
                        zombie.Target = player;
                        return;
                    }
                    else
                    {
                        Debug.Log("🏰 Aproape de bază, zombiul a ales BAZA și te ignoră!");
                        zombie.zombieChoseBase = true;
                        zombie.hasReachedAccessPoint = true;

                        if (zombie.CrystalTarget != null)
                        {
                            zombie.Target = zombie.CrystalTarget.gameObject;
                        }
                        // Nu dăm return, mergem spre Pasul C
                    }
                }
            }
            else
            {
                // --- ZOMBIUL ESTE ÎN TRANZIT (Departe de bază): Aggro automat ---
                Debug.Log("🥩 Zombiul te-a văzut în drum spre bază. Aggro instinctiv!");
                zombie.Target = player;
                zombie.zombieChoseBase = false; // Nu blocăm decizia încă
                return;
            }
        }
        else
        {
            // Jucătorul nu e în rază, resetăm starea
            if (zombie.Target == player)
            {
                ResetToPrioritizedTarget(zombie);
            }
            zombie.zombieChoseBase = false;
        }

        // 2. Verificăm progresul către punctul de acces
        if (zombie.Target == zombie.baseAccessPoint)
        {
            float distToPoint = Vector3.Distance(zombiePos, zombie.baseAccessPoint.transform.position);
            
            // Dacă am ajuns la punctul de acces (raza de 1.5m)
            if (distToPoint < 15f)
            {
                Debug.Log("🎯 Punct de acces atins! Urmează asaltul final asupra Cristalului.");
                zombie.hasReachedAccessPoint = true; // Blocăm revenirea la acest pas
                zombie.Target = zombie.CrystalTarget.gameObject; // Setăm ținta finală
            }
        }
    }
    

    private void ResetToPrioritizedTarget(ZombieNPC zombie)
    {
        // 1. Verificare de siguranță: Dacă baza a fost distrusă, zombiul nu mai are obiectiv principal
        if (zombie.CrystalTarget == null)
        {
            Debug.Log($"🏠 Baza a fost distrusă. Zombie #{zombie.GetInstanceID()} intră în Idle.");
            zombie.Target = null;
            zombie.ToIdle();
            return;
        }

        // 2. Logica de redirecționare bazată pe progresul asediului
        // Verificăm dacă zombiul a ajuns deja la perimetru înainte de a fi distras
        if (zombie.hasReachedAccessPoint)
        {
            // Dacă a fost deja la punctul de acces, îl trimitem direct la cristal
            zombie.Target = zombie.CrystalTarget.gameObject;
            Debug.Log($"🎯 Redirecționare: Revenire directă la Cristal pentru Zombie #{zombie.GetInstanceID()}.");
        }
        else
        {
            // Dacă nu ajunsese la perimetru, îi dăm (sau îi recalculăm) punctul de acces
            zombie.Target = GetOrCreateBaseAccessPoint(zombie);
            Debug.Log($"🚩 Redirecționare: Revenire la Punctul de Acces pentru Zombie #{zombie.GetInstanceID()}.");
        }
    }

    private GameObject GetOrCreateBaseAccessPoint(ZombieNPC zombie)
    {
        // Verificăm dacă mai avem bază la care să calculăm punctul
        if (zombie.CrystalTarget == null) return null;

        if (zombie.baseAccessPoint == null)
        {
            zombie.baseAccessPoint = new GameObject($"Access_{zombie.name}_{zombie.GetInstanceID()}");
        }

        float distToCrystal = Vector3.Distance(zombie.baseAccessPoint.transform.position, zombie.CrystalTarget.position);

        // Verificarea distanței față de bază
        if (distToCrystal > BASE_ACCESS_RADIUS + 1f || distToCrystal < BASE_ACCESS_RADIUS - 1f)
        {
            Vector2 randomCircle = Random.insideUnitCircle.normalized * BASE_ACCESS_RADIUS;
            Vector3 targetPos = zombie.CrystalTarget.position + new Vector3(randomCircle.x, 0, randomCircle.y);

            if (UnityEngine.AI.NavMesh.SamplePosition(targetPos, out UnityEngine.AI.NavMeshHit navHit, 5f, UnityEngine.AI.NavMesh.AllAreas))
            {
                zombie.baseAccessPoint.transform.position = navHit.position;
            }
            else
            {
                zombie.baseAccessPoint.transform.position = targetPos;
            }
        }

        return zombie.baseAccessPoint;
    }

    public void ExitState(NPCBase npc) { }
}


public class MoveToBaseState : INPCState
{
    public NPCBase.NPCStateID StateID => NPCBase.NPCStateID.MoveToBase;
    private const float BASE_ATTACK_RANGE = 3.0f;
    private const float PLAYER_AGGRO_RANGE = 10.0f;

    public void EnterState(NPCBase npc)
    {
        if (npc is ZombieNPC zombie && zombie.CrystalTarget != null)
        {
            npc.Agent.isStopped = false;
            npc.SetSpeed(zombie.Speed);
            npc.Agent.SetDestination(zombie.CrystalTarget.position);
            // Re-setăm animația la mers
            if (npc.animator != null) npc.animator.SetInteger("State", (int)StateID);
            Debug.Log($"Zombie {npc.name} merge spre Cristal.");
        }
        else
        {
            // Fallback
            npc.ChangeState(npc.idleState);
        }
    }

    public void DoState(NPCBase npc)
    {
        ZombieNPC zombie = npc as ZombieNPC;
        if (zombie == null || zombie.CrystalTarget == null) return;

        float distToCrystal = Vector3.Distance(npc.transform.position, zombie.CrystalTarget.position);

        // Dacă zombiul este în raza de atac a bazei
        if (distToCrystal <= BASE_ATTACK_RANGE)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            float distToPlayer = float.MaxValue;

            if (player != null)
            {
                distToPlayer = Vector3.Distance(npc.transform.position, player.transform.position);
            }

            // Condiție: Jucătorul este aproape (10m) ȘI șansa aleatorie este îndeplinită
            if (distToPlayer < PLAYER_AGGRO_RANGE && Random.value < zombie.aggroPlayerChance)
            {
                Debug.Log("⚠️ Zombie a fost distras de Player lângă bază! Atac Player.");
                zombie.ToAttack(player); // Tranzitie la attackState (care va targeta Playerul)
            }
            else
            {
                Debug.Log("⚔️ Zombie a ajuns la Cristal! Atac Bază.");
                // Trecem la starea generică de atac. Zombiul se va opri și va lovi.
                zombie.ChangeState(zombie.attackState);
            }
        }
    }

    public void ExitState(NPCBase npc) { }
}


public class ChooseTargetState : INPCState
{
    // Ne asigurăm că acest ID a fost adăugat în NPCBase.NPCStateID
    public NPCBase.NPCStateID StateID => NPCBase.NPCStateID.ChooseTarget; 
    
    // Un timp scurt pentru a preveni apelarea la fiecare cadru
    private const float EVALUATION_INTERVAL = 0.1f; 
    private float timer = 0f;

    public void EnterState(NPCBase npc)
    {
        // Oprim mișcarea pentru a lua o decizie
        npc.Agent.isStopped = true;
        timer = 0f;
    }

    public void DoState(NPCBase npc)
    {
        timer += Time.deltaTime;
        if (timer < EVALUATION_INTERVAL) return;

        timer = 0f; 

        ZombieNPC zombie = npc as ZombieNPC;
        
        // 1. Validare țintă și context
        if (zombie == null || zombie.CrystalTarget == null)
        {
            npc.ToIdle(); // Nu există bază de atacat
            return;
        }

        // 2. Alege ținta inițială (Baza/Cristalul)
        GameObject initialTarget = zombie.CrystalTarget.gameObject;
        
        // 3. Execută acțiunea: Setează ținta și trece la starea de mișcare (specializată)
        // Metoda ToMoveTo() va apela ChangeState(zombieMoveToState)
        zombie.ToMoveTo(initialTarget);
    }
    
    
    public void ExitState(NPCBase npc) { }
}