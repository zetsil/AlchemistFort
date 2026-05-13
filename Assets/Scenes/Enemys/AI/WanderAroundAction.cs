using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using UnityEngine.AI;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Set Random Wander Target", 
                 story: "Set [targetPos] around [centerObject] within [radius]", 
                 category: "Action/Movement", 
                 id: "065f5c6663e78ceabfe984f8d34c7a1e")]
public partial class SetRandomWanderTargetAction : Action
{
    [SerializeReference] public BlackboardVariable<Vector3> TargetPos;
    [SerializeReference] public BlackboardVariable<GameObject> CenterObject;
    [SerializeReference] public BlackboardVariable<float> Radius;

    protected override Status OnStart()
    {
        if (TargetPos == null || CenterObject.Value == null)
        {
            return Status.Failure;
        }

        // Extragem poziția din GameObject-ul dat ca referință
        Vector3 origin = CenterObject.Value.transform.position;

        // Generăm un punct aleatoriu în cerc
        Vector3 randomSpherePoint = UnityEngine.Random.insideUnitSphere * Radius.Value;
        
        // Menținem punctul la aceeași înălțime cu centrul înainte de SamplePosition (opțional)
        randomSpherePoint.y = 0; 
        
        Vector3 searchPoint = origin + randomSpherePoint;

        // Validăm punctul pe NavMesh
        NavMeshHit hit;
        if (NavMesh.SamplePosition(searchPoint, out hit, Radius.Value, NavMesh.AllAreas))
        {
            TargetPos.Value = hit.position;
            return Status.Success;
        }

        return Status.Failure;
    }

    protected override Status OnUpdate()
    {
        return Status.Success;
    }
}