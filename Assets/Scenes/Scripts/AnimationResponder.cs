using UnityEngine;

[RequireComponent(typeof(Animator))]
public class AnimationResponder : MonoBehaviour
{
    private Animator animator;
    
    // Parametrii din Animator
    private const string ATTACK_TRIGGER = "AttackTrigger"; // Folosit pentru primul atac
    private const string WANTS_TO_ATTACK = "WantsToAttack"; // Folosit pentru combo (Bool)

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void OnEnable() => GlobalEvents.OnAnimationTriggerRequested += HandleAnimationRequest;
    private void OnDisable() => GlobalEvents.OnAnimationTriggerRequested -= HandleAnimationRequest;

    private void HandleAnimationRequest(string triggerName)
    {
        // // 1. Nu acceptăm nimic dacă suntem deja în tranziție
        // // Debug.Log("Click primit la ora: " + Time.time); // Dacă asta nu apare în consolă când apeși, e mouse-ul!
        
        // if (animator.IsInTransition(0)) return;

        // AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        // bool isAttacking = stateInfo.IsTag("Attack");

        // if (!isAttacking)
        // {
        //     // CAZ 1: Pornim primul atac dintr-o stare neutră (ex: Idle)
        //     // Folosim TRIGGER pentru a garanta o singură execuție
        //     animator.SetTrigger(ATTACK_TRIGGER);
        // }
        // else
        // {
        //     // CAZ 2: Suntem deja într-un atac, verificăm fereastra de combo
        //     // Folosim BOOL pentru a "reține" intenția de atac următor
        //     if (stateInfo.normalizedTime >= 0.01f && stateInfo.normalizedTime <= 0.99f)
        //     {
        //         animator.SetBool(WANTS_TO_ATTACK, true);
        //     }
        // }
    }

    // private void Update()
    // {
    //     AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
    //     bool isAttacking = stateInfo.IsTag("Attack");

    //     if (isAttacking)
    //     {
    //         // Resetăm normal dacă suntem în tranziție (combo reușit)
    //         if (animator.IsInTransition(0) && animator.GetBool(WANTS_TO_ATTACK))
    //         {
    //             animator.SetBool(WANTS_TO_ATTACK, false);
    //         }
            
    //         // Resetăm la finalul animației
    //         if (stateInfo.normalizedTime > 0.99f)
    //         {
    //             animator.SetBool(WANTS_TO_ATTACK, false);
    //         }
    //     }
    //     else if (!animator.IsInTransition(0)) // Suntem blocați în Idle/Move
    //     {
    //         if (animator.GetBool(WANTS_TO_ATTACK))
    //         {
    //             // Dacă am ajuns aici, e clar că suntem blocați.
    //             // Soluția: Consumăm bool-ul și activăm Trigger-ul manual.
    //             animator.SetBool(WANTS_TO_ATTACK, false);
    //             animator.SetTrigger(ATTACK_TRIGGER);
                
    //             Debug.Log("Blocaj detectat în Idle! Forțez AttackTrigger.");
    //         }
    //     }
    // }
}