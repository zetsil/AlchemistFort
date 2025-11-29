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
        // Am eliminat: if (target == gameObject) 
        
        bool parameterExists = false;

        // 2. Iterează prin toți parametrii Animator-ului
        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            // Verifică numele și tipul parametrului
            if (parameter.name == triggerName && parameter.type == AnimatorControllerParameterType.Trigger)
            {
                parameterExists = true;
                break;
            }
        }

        if (parameterExists)
        {
            // 3. Dacă există, declanșează Trigger-ul
            animator.SetTrigger(triggerName);
            Debug.Log($"[Responder] Animație declanșată pe {gameObject.name} de un eveniment global: {triggerName}");
        }
        else
        {
            Debug.LogWarning($"Animator-ul pe {gameObject.name} nu are un Trigger cu numele '{triggerName}'.");
        }
        
    }
}