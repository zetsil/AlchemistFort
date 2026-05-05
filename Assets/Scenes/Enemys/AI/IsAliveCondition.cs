using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "IsAlive", story: "Enemy [Agent] is [Alive] alive", category: "Conditions", id: "246a4e7705636ef3c707a9fff0274f55")]
public partial class IsAliveCondition : Condition
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;
    
    [SerializeReference] public BlackboardVariable<bool> Alive;

    public override bool IsTrue()
    {
        if (Agent.Value == null) return false;

        // Încercăm să luăm componenta Entity (părintele lui Enemy)
        Entity entity = Agent.Value.GetComponent<Entity>();

        if (entity != null)
        {
            // Verificăm dacă starea isDead a entității corespunde cu ce am cerut în graf
            // Dacă Alive este True, returnăm true dacă !entity.isDead
            // Dacă Alive este False (adică verificăm dacă e mort), returnăm true dacă entity.isDead
            return !entity.isDead == Alive.Value;
        }

        return false;
    }
}