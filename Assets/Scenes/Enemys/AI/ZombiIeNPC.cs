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
            Debug.LogError("❌ Zombie nu a găsit niciun obiect cu tag-ul 'Base' (Cristalul)!");
        }

        base.Awake();
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
    
    private const float ATTACK_RANGE_THRESHOLD = 2.0f;
    private const float PLAYER_AGGRO_RANGE = 10.0f;
    private const float PLAYER_FLEE_RANGE = PLAYER_AGGRO_RANGE + 2f;
    private const float RAYCAST_RANGE = 2.0f;
    private const int ALLY_LAYER_MASK = 1 << 8;
    
    public void EnterState(NPCBase npc)
    {
        ZombieNPC zombie = npc as ZombieNPC;

        if (zombie == null || npc.Target == null)
        {
            npc.ChangeState(npc.idleState);
            return;
        }

        npc.Agent.isStopped = false;

        if (npc.Agent.isOnNavMesh)
        {
            npc.Agent.SetDestination(npc.Target.transform.position);
        }

        if (npc.animator != null) npc.animator.SetInteger("State", (int)StateID);
    }

    private void ReevaluateTargetPriority(ZombieNPC zombie)
    {
        if (zombie.CrystalTarget == null) return; 
        
        // Calculează punctul de plecare (în fața zombie-ului) și direcția
        // Presupunem că 'zombie.Position' este Vector3 (zombie.transform.position) 
        // și că 'zombie.Forward' este Vector3 (zombie.transform.forward)
        Vector3 zombiePosition = zombie.transform.position;
        Vector3 zombieForward = zombie.transform.forward;
        
        Vector3 startPos = zombiePosition + zombieForward * 0.5f;
        
        // Execută Raycast-ul
        if (Physics.Raycast(startPos, zombieForward, out RaycastHit hit, RAYCAST_RANGE, ALLY_LAYER_MASK))
        {
            // Verifică dacă obiectul lovit are componenta AllyEntity
            AllyEntity ally = hit.collider.GetComponent<AllyEntity>();
            
            if (ally != null && ally.gameObject != zombie.Target)
            {
                Debug.Log($"🚨 Zombie #{zombie.GetInstanceID()} a detectat Ally-ul '{ally.gameObject.name}' prin Raycast! Schimbă ținta.");
                zombie.Target = ally.gameObject;
                return; 
            }
        }
        
        
        GameObject currentTarget = zombie.Target; 
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        
        if (player == null) return;
        
        float distToPlayer = Vector3.Distance(zombie.Position, player.transform.position);

        // Case 1: Base/Crystal -> Player (Aggro check)
        if (currentTarget == zombie.CrystalTarget.gameObject)
        {
            if (distToPlayer < PLAYER_AGGRO_RANGE && Random.value < zombie.aggroPlayerChance)
            {
                zombie.Target = player;
            }
        }
        
        // Case 2: Player -> Base/Crystal (Flee check)
        else if (currentTarget == player)
        {
             if (distToPlayer > PLAYER_FLEE_RANGE) 
             {
                 zombie.Target = zombie.CrystalTarget.gameObject;
             }
        }
    }

    public void DoState(NPCBase npc)
    {
        ZombieNPC zombie = npc as ZombieNPC;
        if (zombie == null || npc.Target == null)
        {
            npc.ToIdle();
            return;
        }

        // 1. Re-evaluate target priority
        ReevaluateTargetPriority(zombie);
        
        // 2. Execute movement
        if (npc.Agent.isOnNavMesh)
        {
             npc.Agent.SetDestination(npc.Target.transform.position);
        }
        
        // 3. Check transitions
        
        // Path Invalid check (e.g., path blocked by new walls)
        if (npc.Agent.pathStatus == UnityEngine.AI.NavMeshPathStatus.PathInvalid)
        {
            zombie.ChangeState(zombie.chooseTargetState);
            return;
        }
        
        // Arrival check (Attack range)
        float distToTarget = Vector3.Distance(npc.Position, npc.Target.transform.position);
        
        if (distToTarget <= ATTACK_RANGE_THRESHOLD)
        {
            npc.ToAttack(npc.Target); 
            return; 
        }
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