using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "PlayerInRange", story: "If [agent] is close to [player] in [range]", category: "Conditions", id: "7de7347f7c02db12606275ff66f394b4")]
public partial class PlayerInRangeCondition : Condition
{
    // Adăugăm Agentul (Inamicul) ca referință
    [SerializeReference] public BlackboardVariable<GameObject> Agent;
    [SerializeReference] public BlackboardVariable<GameObject> Player;
    [SerializeReference] public BlackboardVariable<float> Range;

    public override bool IsTrue()
    {
        // Verificăm dacă ambele obiecte există
        if (Agent == null || Agent.Value == null || Player == null || Player.Value == null)
        {
            return false;
        }

        // Luăm pozițiile de la ambele GameObject-uri
        Vector3 agentPos = Agent.Value.transform.position;
        Vector3 playerPos = Player.Value.transform.position;

        // Calculăm distanța
        float distance = Vector3.Distance(agentPos, playerPos);

        return distance <= Range.Value;
    }

    public override void OnStart() {}
    public override void OnEnd() {}
}