using UnityEngine;

/// <summary>
/// Rotește obiectul pe axa Y pentru a privi constant către jucător (Billboarding).
/// Se atașează la containerul 3D (World Space Canvas) al barei de progres.
/// </summary>
public class LookAtPlayer : MonoBehaviour
{
    private Transform playerTransform;

    void Start()
    {
        // Găsește Transform-ul Jucătorului la început (Presupune că are tag-ul "Player")
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            // Reține referința la Transform-ul jucătorului
            playerTransform = player.transform;
        }
        else
        {
            Debug.LogError("[LookAtPlayer] Nu a fost găsit niciun obiect cu tag-ul 'Player'. Rotația UI-ului va fi dezactivată.");
            // Dezactivăm scriptul dacă nu poate găsi jucătorul
            enabled = false; 
        }
    }

    // Folosim LateUpdate pentru a ne asigura că rotația camerei (jucătorului) a fost deja calculată în frame-ul curent.
    void LateUpdate()
    {
        if (playerTransform != null)
        {
            // Calculează direcția de la acest obiect (bara UI) către jucător.
            Vector3 directionToPlayer = playerTransform.position - transform.position;
            
            // Ignoră diferența de înălțime (axa Y) pentru a menține UI-ul perfect vertical.
            directionToPlayer.y = 0; 
            
            // Verifică dacă există o direcție validă
            if (directionToPlayer.sqrMagnitude > 0.001f)
            {
                // Creează rotația necesară. Folosim -directionToPlayer pentru ca UI-ul să nu fie întors cu spatele.
                Quaternion targetRotation = Quaternion.LookRotation(-directionToPlayer);
                
                // Aplică doar rotația pe axa Y (yaw), resetând X și Z (pitch și roll) la zero.
                // Aceasta simulează un "billboard" care stă în picioare.
                transform.rotation = Quaternion.Euler(0, targetRotation.eulerAngles.y, 0);
            }
        }
    }
}