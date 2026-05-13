using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using UnityEngine.AI;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "RotateToTarget", 
                 story: "[Agent] Rotate towards [Target]", 
                 category: "Action", 
                 id: "b402d103feb6e18daa9f8516a1a06f89")]
public partial class RotateToTargetAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;
    [SerializeReference] public BlackboardVariable<GameObject> Target;
    
    [Tooltip("Viteza de rotație (Slerp)")]
    [SerializeReference] public BlackboardVariable<float> RotationSpeed = new BlackboardVariable<float>(10f);

    private NavMeshAgent _navAgent;

    protected override Status OnStart()
    {
        if (Agent.Value == null || Target.Value == null) return Status.Failure;

        _navAgent = Agent.Value.GetComponent<NavMeshAgent>();

        // Dezactivăm rotația automată a agentului pentru a prelua controlul manual
        if (_navAgent != null)
        {
            _navAgent.updateRotation = false;
        }

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (Agent.Value == null || Target.Value == null) return Status.Failure;

        Vector3 agentPos = Agent.Value.transform.position;
        Vector3 targetPos = Target.Value.transform.position;

        // Calculăm direcția pe plan orizontal
        Vector3 direction = (targetPos - agentPos).normalized;
        direction.y = 0; 

        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(direction);
            Agent.Value.transform.rotation = Quaternion.Slerp(
                Agent.Value.transform.rotation, 
                targetRot, 
                Time.deltaTime * RotationSpeed.Value
            );
        }

        // Returnăm mereu Running. 
        // Nodul se va opri doar când ramura paralelă (cu Wait) dă Success.
        return Status.Running;
    }

    protected override void OnEnd()
    {
        // Foarte important: Reactivăm rotația agentului pentru restul AI-ului
        if (_navAgent != null)
        {
            _navAgent.updateRotation = true;
        }
    }
}