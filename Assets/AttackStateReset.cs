using UnityEngine;

public class AttackStateReset : StateMachineBehaviour
{
    // Se execută când părăsești starea, indiferent de motiv
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // Căutăm ToolController pe acest obiect sau pe copiii lui
        ToolController tool = animator.GetComponentInChildren<ToolController>();
        if (tool != null)
        {
            tool.ForceResetAttack();
        }
    }
}