// NPCStates.cs
using UnityEngine;
using UnityEngine.AI;

// ----------------------------------------------------------------------
// 1. Idle State
// ----------------------------------------------------------------------

public class IdleState : INPCState
{
    public NPCBase.NPCStateID StateID => NPCBase.NPCStateID.Idle;

    // 1. Intervalul (Poate fi configurat in functie de NPC)
    private readonly float minDuration = 3f;
    private readonly float maxDuration = 7f;

    private float idleTimer = 0f;
    private float currentIdleDuration = 0f;

    public void EnterState(NPCBase npc)
    {
        // 💡 Calculează durata aleatorie când se intră în Idle
        currentIdleDuration = Random.Range(minDuration, maxDuration);
        idleTimer = 0f; // Resetăm timer-ul


        // Asigură-te că NPC-ul stă pe loc
        if (npc.Agent != null)
        {
            npc.Agent.isStopped = true;
            npc.Agent.ResetPath();
        }
    }

    public void DoState(NPCBase npc)
    {
        // Timer pentru idle
        idleTimer += Time.deltaTime;

        // După ce a stat o perioadă, trece în Wander
        if (idleTimer >= currentIdleDuration)
        {
            idleTimer = 0f;
            npc.ToWander();
        }
    }
    
    public void ExitState(NPCBase npc) 
    { 
        // Nu este necesară nicio logică specifică de ieșire aici
    }
}

// ----------------------------------------------------------------------
// 2. Wander State (NavMesh Adapted)
// ----------------------------------------------------------------------

public class WanderState : INPCState
{
    public NPCBase.NPCStateID StateID => NPCBase.NPCStateID.Wander;

    private readonly float wanderRadius = 10f;
    private const float DestinationCheckDistance = 1.0f;

    public void EnterState(NPCBase npc)
    {

    }

    public void DoState(NPCBase npc)
    {
        // 1. Verifică dacă trebuie să setăm o destinație nouă (ajuns sau nu are cale)
        if (npc.Agent.isOnNavMesh &&
            (!npc.Agent.hasPath || (!npc.Agent.pathPending && npc.Agent.remainingDistance <= DestinationCheckDistance)))
        {
            // Caută o nouă destinație validă pe NavMesh folosind metoda din NPCBase
            Vector3 newDestination = npc.GetRandomNavMeshPoint(npc.Position, wanderRadius);

            if (newDestination != Vector3.zero)
            {
                npc.Agent.SetDestination(newDestination);
            }
            else
            {
                // Nu a găsit destinație validă, trece în Idle
                npc.ToIdle();
            }
        }

        // 2. Tranziția la Idle după ce ajunge
        if (npc.Agent.isOnNavMesh && !npc.Agent.pathPending && npc.Agent.remainingDistance <= DestinationCheckDistance)
        {
            npc.ToIdle();
        }

    }
    
    public void ExitState(NPCBase npc) 
    { 
        // Nu este necesară nicio logică specifică de ieșire aici
    }
}

// ----------------------------------------------------------------------
// 3. Attack State
// ----------------------------------------------------------------------


public class AttackState : INPCState
{
    public NPCBase.NPCStateID StateID => NPCBase.NPCStateID.Attack;

    private float attackTimer;
    private const string ATTACK_TRIGGER_NAME = "MeleeAttack"; 
    private const float MAX_TARGET_ANGLE = 60f; 
    
    // O marjă de eroare: Dacă ținta se îndepărtează puțin (ex: 1 metru) peste raza de atac,
    // nu ieșim imediat din stare. Doar dacă fuge clar.
    private const float ATTACK_EXIT_BUFFER = 1.2f; 

    public void EnterState(NPCBase npc)
    {
        // Dacă nu avem țintă la intrare, ieșim imediat
        if (npc.Target == null)
        {
            npc.RevertToLevelStart();
            return;
        }

        // Oprim mișcarea pentru a ataca
        if (npc.Agent != null && npc.Agent.isOnNavMesh)
        {
            npc.Agent.isStopped = true;
            npc.Agent.ResetPath();
        }

        if(npc.animator != null)
        {
            npc.animator.SetInteger("State", (int)StateID);
        }
        attackTimer = 0f; 
    }

    public void DoState(NPCBase npc)
    {
        // ------------------------------------------------------------------
        // 1. VERIFICĂRI DE IEȘIRE (Generic)
        // ------------------------------------------------------------------
        
        // Calculăm distanța maximă permisă înainte de a renunța la atac
        // (Raza de oprire + un buffer mic pentru a preveni oscilația)
        float maxDistance = npc.attackStopRange + ATTACK_EXIT_BUFFER;

        // Dacă ținta e null SAU e prea departe
        if (npc.Target == null || 
            Vector3.Distance(npc.Position, npc.Target.transform.position) > maxDistance)
        {
            // 🚨 LOGICA CERUTĂ: Intră în prima stare din nivel (ex: ChooseTarget sau Wander)
            npc.RevertToLevelStart();
            return;
        }

        // ------------------------------------------------------------------
        // 2. LOGICA DE ATAC
        // ------------------------------------------------------------------

        RotateTowardsTarget(npc);

        attackTimer -= Time.deltaTime;
        
        if (attackTimer <= 0f)
        {
            if (CheckFacingTarget(npc))
            {
                attackTimer = npc.AttackSpeed; 
                
                if (npc.animator != null)
                {
                    npc.animator.SetTrigger(ATTACK_TRIGGER_NAME);
                }
            }
            else
            {
                attackTimer = 0.5f; // Așteaptă să se rotească
            }
        }
    }

    public void ExitState(NPCBase npc) 
    { 
        // Reactivează agentul la ieșire
        if (npc.Agent != null && npc.Agent.isOnNavMesh)
        {
            npc.Agent.isStopped = false;
        }
    }
    
    // =========================================================
    //  METODE AJUTĂTOARE GENERICE
    // =========================================================

    private void RotateTowardsTarget(NPCBase npc)
    {
        if (npc.Target != null)
        {
            Vector3 direction = npc.Target.transform.position - npc.transform.position;
            direction.y = 0; // Păstrăm rotația doar pe orizontală
            
            if (direction != Vector3.zero)
            {
                Quaternion lookRotation = Quaternion.LookRotation(direction);
                npc.transform.rotation = Quaternion.Slerp(npc.transform.rotation, lookRotation, Time.deltaTime * 10f);
            }
        }
    }

    private bool CheckFacingTarget(NPCBase npc)
    {
         if (npc.Target != null)
         {
             Vector3 directionToTarget = (npc.Target.transform.position - npc.transform.position).normalized;
             float angle = Vector3.Angle(npc.transform.forward, directionToTarget);
             
             return angle < MAX_TARGET_ANGLE; 
         }
         return false; 
    }
}

// ----------------------------------------------------------------------
// 4. Run State (NavMesh Adapted)
// ----------------------------------------------------------------------

public class RunState : INPCState
{
    public NPCBase.NPCStateID StateID => NPCBase.NPCStateID.Run;

    private Vector3 safePosition = Vector3.zero;
    private const float EscapeRadius = 15f;
    private const float RunSpeedMultiplier = 2.0f;

    public void EnterState(NPCBase npc)
    {

    }

    public void DoState(NPCBase npc)
    {
        // 1. Setăm viteza de fugă
        npc.SetSpeed(npc.Speed * RunSpeedMultiplier);

        // 2. Dacă nu avem o poziție sigură, alegem una nouă
        if (safePosition == Vector3.zero ||
            (npc.Agent.isOnNavMesh && !npc.Agent.pathPending && npc.Agent.remainingDistance <= npc.Agent.stoppingDistance))
        {
            // Găsim un punct nou
            safePosition = npc.GetRandomNavMeshPoint(npc.Position, EscapeRadius);

            if (safePosition != Vector3.zero)
            {
                npc.Agent.SetDestination(safePosition);
            }
            else
            {
                // Nu a găsit unde să fugă
                npc.ToIdle();
                return;
            }
        }

        // 3. Când ajunge într-un loc sigur, se relaxează
        if (npc.Agent.isOnNavMesh && !npc.Agent.pathPending && npc.Agent.remainingDistance <= npc.Agent.stoppingDistance)
        {
            safePosition = Vector3.zero;
            npc.ToIdle(); // Viteza este resetată la normal în ToIdle()
        }
    }


    public void ExitState(NPCBase npc)
    {
        // Nu este necesară nicio logică specifică de ieșire aici
    }

}


public class HideState : INPCState
{
    public NPCBase.NPCStateID StateID => NPCBase.NPCStateID.Hide;

    public void EnterState(NPCBase npc)
    {
        npc.Agent.isStopped = true;
        npc.Agent.ResetPath();

        // 🔹 Dezactivează toate rendererele (invizibil)
        foreach (Renderer r in npc.GetComponentsInChildren<Renderer>())
        {
            r.enabled = false;
        }

        // 🔹 Dezactivează toate coliderele (nu poate fi lovit sau interacționat)
        foreach (Collider c in npc.GetComponentsInChildren<Collider>())
        {
            c.enabled = false;
        }

    }

    public void DoState(NPCBase npc)
    {
        // Aici poți pune logică de "așteptare" sau "ascultare"
    }

    public void ExitState(NPCBase npc)
    {
        // 🔹 Reafişează toate componentele vizuale
        foreach (Renderer r in npc.GetComponentsInChildren<Renderer>())
        {
            r.enabled = true;
        }

        // 🔹 Reactivează toate coliderele
        foreach (Collider c in npc.GetComponentsInChildren<Collider>())
        {
            c.enabled = true;
        }


    }
}


// ----------------------------------------------------------------------
// 6. MoveTo State (Urmărește un GameObject)
// ----------------------------------------------------------------------

public class MoveToState : INPCState
{
    public NPCBase.NPCStateID StateID => NPCBase.NPCStateID.MoveToBase; 
    
    // Distanța rămasă sub care considerăm că destinația a fost atinsă
    private const float DestinationTolerance = 1.5f; // Puțin mai mare decât stoppingDistance a agentului

    public void EnterState(NPCBase npc)
    {
        // 1. Verifică ținta
        if (npc.Target == null)
        {
            npc.ToIdle();
            return;
        }

        // 2. Setări Agent
        if (npc.Agent != null && npc.Agent.isOnNavMesh)
        {
            npc.Agent.isStopped = false;
            npc.SetSpeed(npc.Speed); 
            // Setează destinația inițială
            npc.Agent.SetDestination(npc.Target.transform.position);
            
        }
        else
        {
            npc.ToIdle();
        }
    }

    public void DoState(NPCBase npc)
    {
        // 1. Verifică existența țintei
        if (npc.Target == null)
        {
            npc.ToIdle();
            return;
        }

        // 2. Urmărirea activă (actualizează destinația)
        if (npc.Agent.isOnNavMesh)
        {
             // Actualizează destinația la poziția curentă a țintei
             npc.Agent.SetDestination(npc.Target.transform.position);
        }

        // 3. Verifică dacă a ajuns (folosim distanța Vector3 pentru a fi mai robust)
        float distance = Vector3.Distance(npc.Position, npc.Target.transform.position);
        
        if (distance <= DestinationTolerance)
        {
            // A ajuns la destinație, trece în Idle (sau în starea următoare, ex: Attack)
            npc.ToIdle(); 
            return;
        }

        // 4. Verifică calea invalidă
        if (npc.Agent.isOnNavMesh && npc.Agent.pathStatus == NavMeshPathStatus.PathInvalid)
        {
            npc.ToIdle();
        }
    }
    
    public void ExitState(NPCBase npc) 
    { 
        // Nu este necesară nicio logică specifică
    }
}



