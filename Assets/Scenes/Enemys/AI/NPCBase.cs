// NPCBase.cs
using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

// Acum moștenește clasa de bază 'Entity' (care gestionează Viața, Damage-ul și Loot-ul)
// și implementează interfața Entity (dacă există)
public abstract class NPCBase : Entity // <-- MODIFICARE CHEIE AICI
{


    [SerializeField] private float defaultSpeed = 3.5f;
    public float Speed { get; private set; }
    public float AttackSpeed { get; set; } = 1.0f;
    private bool isChangingState = false;

    protected int currentStateLvl = 0;
    public enum NPCStateLvl { Safe, Atack, Run };

    public bool IsStateLocked { get; protected set; } = false;


    // Lista de stări pe nivele (INPCState trebuie să fie interfața ta)
    public List<List<INPCState>> StateLevels = new List<List<INPCState>>();

    
    // 🏃‍♂️ Componenta NavMeshAgent - ESENȚIALĂ
    [HideInInspector]
    public NavMeshAgent Agent;

    [Header("Combat Settings")]
    [Tooltip("Raza la care NPC-ul se oprește din mișcare pentru a iniția atacul.")]
    public float attackStopRange = 2.0f;

    [Tooltip("Ținta curentă a NPC-ului (ex: Player, structură de atac)")]
    public GameObject Target { get; set; }

    // 🎭 State Machine
    public enum NPCStateID { Idle, Wander, Attack, Run, Hide, MoveToBase, ChooseTarget }

    [HideInInspector]
    public IdleState idleState = new IdleState();

    [HideInInspector]
    public WanderState wanderState = new WanderState();

    [HideInInspector]
    public AttackState attackState = new AttackState();

    [HideInInspector]
    public RunState runState = new RunState();

    [HideInInspector]
    public HideState hideState = new HideState();
    
    [HideInInspector]
    public MoveToState moveToState = new MoveToState();


    public void LockState()
    {
        IsStateLocked = true;
    }

    public void UnlockState()
    {
        IsStateLocked = false;
    }



    public INPCState currentState;
    public NPCStateID CurrentStateID { get; private set; }
    private NPCStateID previousStateID;
    public Animator animator;

    // Metodă pentru a obține poziția
    public Vector3 Position => transform.position;

    // Înlocuim Awake cu Start, deoarece Start() este deja folosită în clasa de bază Entity
    // Dar, pentru a ne asigura că Agent-ul este inițializat la timp, păstrăm Awake/Setările FSM aici.
    public new void Awake() // <- Folosim 'new' pentru a masca Awake din MonoBehaviour, deși nu e ideal
    {
        // 1. Preia componenta NavMeshAgent
        Agent = GetComponent<NavMeshAgent>();
        if (Agent == null)
        {
            Debug.LogError("NavMeshAgent component missing on " + gameObject.name);
        }

        // Asigurăm inițializarea de bază (inclusiv Health/Loot)
        base.Start(); // Apelează Start() din clasa de bază Entity

        // 2. Setează viteza inițială
        SetSpeed(defaultSpeed);

        // 3. Inițializarea stărilor pe nivele
        SetupStateLevels();
        // 4. Setează starea inițială (o trimitem pe prima stare a nivelului 0)
        ChangeState(StateLevels[0][0]);
    }

    // Metodă pentru actualizarea vitezei și sincronizarea cu NavMeshAgent
    public void SetSpeed(float newSpeed)
    {
        Speed = newSpeed;
        if (Agent != null && Agent.isOnNavMesh)
        {
            Agent.speed = newSpeed;
        }
    }

    // Înlocuiește implementarea ta actuală incorectă cu asta:

    public void ChangeLevel(int newLevel)
    {
        // 1. Validări
        if (newLevel < 0 || newLevel >= StateLevels.Count)
        {
            Debug.LogError($"{gameObject.name}: Nivelul specificat ({newLevel}) este în afara limitelor.");
            return;
        }

        // Ieși dacă ești deja pe nivel
        if (currentStateLvl == newLevel) return;

        // 2. Setează flag-ul (Acesta este semnalul că nivelul s-a schimbat!)
        currentStateLvl = newLevel;

        // 3. Forțează trecerea direct la prima stare din noul nivel
        List<INPCState> newLevelStates = StateLevels[currentStateLvl];

        if (newLevelStates.Count > 0)
        {
            INPCState firstStateOfNewLevel = newLevelStates[0];

            // ChangeState se folosește de noul currentStateLvl pentru a valida tranziția
            ChangeState(firstStateOfNewLevel);

        }
        else
        {
            Debug.LogError($"{gameObject.name}: Nivelul {newLevel} este gol.");
        }
    }


    public void RevertToLevelStart()
    {
        // 1. Validare nivel
        if (currentStateLvl < 0 || currentStateLvl >= StateLevels.Count)
        {
            Debug.LogError($"[NPCBase] Nivel curent invalid: {currentStateLvl}. Resetare la 0.");
            currentStateLvl = 0;
        }

        List<INPCState> currentLevelStates = StateLevels[currentStateLvl];

        // 2. Verificare dacă există stări
        if (currentLevelStates != null && currentLevelStates.Count > 0)
        {
            INPCState firstState = currentLevelStates[0];
            Debug.Log($"↩️ [Revert] {gameObject.name} revine la startul Nivelului {currentStateLvl} -> {firstState.StateID}");
            
            // 3. Tranziție
            ChangeState(firstState);
        }
        else
        {
            Debug.LogError($"[NPCBase] Nivelul {currentStateLvl} este gol! Fallback la Idle.");
            ToIdle();
        }
    }

    protected virtual void SetupStateLevels()
    {
        // Aceasta este implementarea de BAZĂ. Clasa copil o poate ignora sau modifica.

        // Nivelul 0 (Safe): Idle, Wander
        StateLevels.Add(new List<INPCState> { idleState, wanderState });

        // Nivelul 1 (Attack): Attack, Run
        StateLevels.Add(new List<INPCState> { attackState, runState });

        Debug.Log($"[NPCBase] Stările inițiale au fost setate. Nivele: {StateLevels.Count}");
    }

    // Schimbarea Stării
    public void ChangeState(INPCState newState)
    {
        // 1. BLOCARE RECURSIVĂ: Oprește bucla imediată
        if (isChangingState)
        {
            Debug.LogWarning($"Blocat ChangeState recursiv spre {newState.StateID} de la {CurrentStateID}");
            return;
        }

        if (Agent == null || !Agent.enabled || !Agent.isOnNavMesh)
        {
            // Debug.LogWarning($"Agentul {gameObject.name} nu este încă gata pentru NavMesh.");
            return; 
        }

        if (IsStateLocked && currentState != newState)
        return;

        // if (!this.Agent.enabled || !this.Agent.isOnNavMesh) return;

        // Ieși dacă starea nu se schimbă
        if (currentState == newState) return;
        isChangingState = true;

        // Asigură-te că nivelul curent este valid, altfel setează la 0
        if (currentStateLvl < 0 || currentStateLvl >= StateLevels.Count)
        {
            currentStateLvl = 0;
        }

        // Obține lista de stări pentru nivelul curent
        List<INPCState> currentLevelStates = StateLevels[currentStateLvl];

        // Verifică dacă noua stare (`newState`) este în nivelul corect
        if (currentLevelStates.Contains(newState))
        {
            // Stare validă pe nivelul curent: Mergi înainte
            currentState?.ExitState(this);
            newState.EnterState(this);
            currentState = newState;
            CurrentStateID = newState.StateID;

        }
        else
        {
            // Stare invalidă pentru nivelul curent: Treci la prima stare din nivel
            if (currentLevelStates.Count > 0)
            {
                INPCState firstState = currentLevelStates[0];
                currentState?.ExitState(this);
                firstState.EnterState(this);
                currentState = firstState;
                CurrentStateID = firstState.StateID;

            }
            else
            {
                Debug.LogError($"Eroare: Nivelul curent {currentStateLvl} este gol.");
            }
        }
        isChangingState = false;
    }

    protected override void Update() // <- Folosim override pentru a extinde funcționalitatea din Entity
    {
        if (Agent == null || !Agent.enabled || !Agent.isOnNavMesh) return;
        
        base.Update(); // Executăm logica de bază (dacă există)
        TickStateMachine();
    }

    protected void TickStateMachine()
    {
        // Ex: actualizează starea curentă
        currentState?.DoState(this);

        // Logică pentru Animator
        if (animator != null && previousStateID != CurrentStateID)
        {
            animator.SetInteger("State", (int)CurrentStateID);
            previousStateID = CurrentStateID;
        }
    }

    // Metodă auxiliară comună pentru a găsi un punct valid pe NavMesh
    public Vector3 GetRandomNavMeshPoint(Vector3 origin, float radius)
    {
        Vector3 randomDirection = Random.insideUnitSphere * radius;
        randomDirection += origin;
        NavMeshHit hit;

        if (NavMesh.SamplePosition(randomDirection, out hit, radius, NavMesh.AllAreas))
        {
            return hit.position;
        }
        return Vector3.zero;
    }


    // Metode de Tranziție Simple

    public virtual void ToIdle()
    {
        ChangeState(idleState);
        SetSpeed(defaultSpeed);
        if (Agent != null && Agent.isOnNavMesh)
        {
            Agent.isStopped = true;
            Agent.ResetPath();
        }
    }

    public virtual void ToWander()
    {
        ChangeState(wanderState);
        if (Agent != null)
        {
            Agent.isStopped = false;
        }
    }

    public virtual void ToAttack(GameObject newTarget = null)
    {
        // 1. Setează noua țintă, dacă a fost furnizată
        if (newTarget != null)
        {
            Target = newTarget;
        }

        ChangeState(attackState);


        float attackSoundChance = 0.4f; 
    
        if (UnityEngine.Random.value <= attackSoundChance && entityData != null)
        {
            // Construim numele sunetului: Attack_Zombie (sau orice nume are entityData)
            string attackSoundName = "Attack_" + entityData.name;
            
            // Folosim noul eveniment pozițional pe care l-am creat anterior
            GlobalEvents.TriggerPlaySoundAtPosition(attackSoundName, transform.position);
        }

        // 2. Logică NavMeshAgent
        // Agentul trebuie să fie activ dacă avem o țintă
        if (Target != null && Agent != null)
        {
            Agent.isStopped = false; // Lăsăm agentul liber să urmărească în DoState-ul stării de atac
        }
        else
        {
            // Dacă nu avem o țintă (Target e null), ne oprim și resetăm calea
            if (Agent != null && Agent.isOnNavMesh)
            {
                Agent.isStopped = true;
                Agent.ResetPath();
            }
        }
    }

    public virtual void ToRun()
    {
        ChangeState(runState);
        if (Agent != null)
        {
            Agent.isStopped = false;
        }
    }


    public virtual void ToHide()
    {
        ChangeState(hideState);
        if (Agent != null)
        {
            Agent.isStopped = false;
        }
    }
    
    public virtual void ToMoveTo(GameObject target)
    {
        Target = target;
        ChangeState(moveToState); // Implicit generic. ZombieNPC va face override aici.
        if (Agent != null) Agent.isStopped = false;
    }
    

    protected bool ManeuverToTarget()
    {
        // 1. Validare Target
        if (Target == null)
        {
            ToIdle();
            return true; // Considerat finalizat (eșuat)
        }

        // 2. Urmărirea activă: Setează destinația la fiecare cadru
        if (Agent != null && Agent.isOnNavMesh)
        {
            Agent.SetDestination(Target.transform.position);
        }
        else
        {
            // Dacă nu mai este pe NavMesh, poate intra în Idle sau Run
            return false;
        }

        // 3. Verificarea distanței
        float distToTarget = Vector3.Distance(Position, Target.transform.position);

        // 4. Decizia (Raza de Atac atinsă)
        if (distToTarget <= attackStopRange)
        {
            // NPC-ul a ajuns suficient de aproape. Se oprește.
            if (Agent != null)
            {
                Agent.isStopped = true;
                Agent.ResetPath();
            }
            return true; // Gata de decizie (Atacă/Interacționează)
        }

        return false; // Nu am ajuns încă
    }
    

}