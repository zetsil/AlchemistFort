using UnityEngine;
using System; // Asigură-te că folosești System

[RequireComponent(typeof(Animator))]
public class AnimationResponder : MonoBehaviour
{
    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        // 💡 MODIFICARE: Abonare la evenimentul cu un singur parametru (string)
        // Presupunând că GlobalEvents.OnAnimationTriggerRequested a fost schimbat
        GlobalEvents.OnAnimationTriggerRequested += HandleAnimationRequest;
    }

    private void OnDisable()
    {
        GlobalEvents.OnAnimationTriggerRequested -= HandleAnimationRequest;
    }

    // 💡 MODIFICARE: Metoda primește acum doar triggerName (string)
    private void HandleAnimationRequest(string triggerName)
    {
        // 1. Luăm starea curentă
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

        // 2. Dacă suntem în "atack" și animația e sub 90% din durată, BLOCĂM tot
        // Asta simulează "Exit Time" - nu lasă alt click să strice atacul curent
        if (stateInfo.IsName("atack") && stateInfo.normalizedTime < 0.9f)
        {
            return; 
        }

        // 3. Verificăm dacă suntem deja în tranziție către atac
        if (animator.IsInTransition(0))
        {
            return;
        }

        // 4. Executăm atacul instant (fără lag, pentru că Has Exit Time e OFF în Animator)
        animator.SetTrigger(triggerName);
    }
}