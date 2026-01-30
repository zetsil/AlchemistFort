using UnityEngine;
using System.Collections.Generic;

public class TacticalZombieNPC : ZombieNPC
{
    [Header("Tactical Settings")]
    [Tooltip("Punctul spre care fuge după atac. Dacă e null, va căuta automat tag-ul 'Charge'.")]
    public Transform specificChargePoint;
    // hello
    // Variabile mutate aici pentru a nu polua ZombieNPC
    [HideInInspector] public Transform activeChargePoint;
    [HideInInspector] public bool hasFinishedAttackTrigger = true;
    public readonly ChargeState chargeState = new ChargeState();

    private EnemyAttackController attackController;
    public bool wasAttackWindowOpen = false;


    public new void Awake()
    {
        base.Awake();
        // Căutăm componenta în copii
        attackController = GetComponentInChildren<EnemyAttackController>();
    }



    protected override void SetupStateLevels()
    {
        // Rulăm setup-ul de bază
        base.SetupStateLevels();

        // Adăugăm starea de Charge în nivelul de noapte (Lvl 1)
        if (StateLevels.Count > 1)
        {
            if (!StateLevels[1].Contains(chargeState))
            {
                StateLevels[1].Add(chargeState);
            }
        }
    }

    protected override void Update()
    {


        if (attackController == null) return;

        // Detectăm momentul când fereastra de atac se închide
        if (wasAttackWindowOpen && !attackController.IsAttackWindowOpen)
        {
            // ❌ AM SCOS condiția: if (currentState == attackState)
            // Motiv: Uneori AttackState iese automat în Idle înainte să apucăm noi să verificăm.

            // Verificăm doar să nu fim deja în Charge sau morți
            if (currentHealth > 0)
            {
                Debug.Log($"[Tactical] Hitbox închis. FORȚEZ fuga la încărcare!");
                ChangeState(chargeState);
            }
        }

        // Salvăm starea curentă pentru cadrul următor
        wasAttackWindowOpen = attackController.IsAttackWindowOpen;
        
        base.Update(); // Apelează logica din ZombieNPC/NPCBase
    }

    public Transform FindBestChargePoint()
    {
        if (specificChargePoint != null) return specificChargePoint;

        GameObject[] points = GameObject.FindGameObjectsWithTag("Charge");
        if (points.Length == 0) return null;

        Transform closest = null;
        float minDist = float.MaxValue;
        foreach (var p in points)
        {
            float d = Vector3.Distance(transform.position, p.transform.position);
            if (d < minDist)
            {
                minDist = d;
                closest = p.transform;
            }
        }
        return closest;
    }
}

// --- CHARGE STATE ---
public class ChargeState : INPCState
{
    // Folosim un ID existent pentru animație (Wander sau MoveToBase)
    public NPCBase.NPCStateID StateID => NPCBase.NPCStateID.Wander; 

    public void EnterState(NPCBase npc)
    {
        // Facem cast la TacticalZombieNPC
        TacticalZombieNPC tactical = npc as TacticalZombieNPC;
        
        if (tactical == null)
        {
            Debug.LogError($"{npc.name} nu este un TacticalZombieNPC!");
            npc.ToIdle();
            return;
        }

        tactical.activeChargePoint = tactical.FindBestChargePoint();

        if (tactical.activeChargePoint == null)
        {
            Debug.LogWarning($"{npc.name} nu are unde să se încarce!");
            tactical.ChangeState(tactical.chooseTargetState);
            return;
        }

        npc.Agent.isStopped = false;
        npc.Agent.stoppingDistance = 0.5f;
        npc.Agent.SetDestination(tactical.activeChargePoint.position);
        
        if (npc.animator != null) 
            npc.animator.SetInteger("State", (int)NPCBase.NPCStateID.Wander); 
    }

    public void DoState(NPCBase npc)
    {
        TacticalZombieNPC tactical = npc as TacticalZombieNPC;
        if (tactical == null || tactical.activeChargePoint == null) return;

        // Verificăm dacă a ajuns la punctul de charge
        if (!npc.Agent.pathPending && npc.Agent.remainingDistance <= npc.Agent.stoppingDistance)
        {
            tactical.zombieChoseBase = false;
            // Revine la alegerea unei noi ținte
            tactical.ChangeState(tactical.chooseTargetState);
        }
    }

    public void ExitState(NPCBase npc)
    {
        
     }
}