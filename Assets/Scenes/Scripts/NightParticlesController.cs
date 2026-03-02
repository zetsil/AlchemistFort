using UnityEngine;
using System.Collections.Generic;

public class NightParticlesController : MonoBehaviour
{
    [Header("Referințe Particule")]
    [Tooltip("Lista cu sistemele de particule sau obiectele care apar doar noaptea.")]
    public List<GameObject> nightObjects;

    private void OnEnable()
    {
        // Ne abonăm la evenimentele globale
        GlobalEvents.OnDayStart += HandleDayStart;
        GlobalEvents.OnNightStart += HandleNightStart;
    }

    private void OnDisable()
    {
        // Dezabonare pentru a evita erorile de memorie
        GlobalEvents.OnDayStart -= HandleDayStart;
        GlobalEvents.OnNightStart -= HandleNightStart;
    }

    private void Start()
    {
        // Verificăm starea inițială la pornirea jocului
        if (GameStateManager.Instance != null)
        {
            ToggleNightEffects(GameStateManager.Instance.IsNight);
        }
    }

    private void HandleDayStart()
    {
        Debug.Log("[Environment] Oprire particule de noapte.");
        ToggleNightEffects(false);
    }

    private void HandleNightStart()
    {
        Debug.Log("[Environment] Activare particule de noapte (Gazul Galben).");
        ToggleNightEffects(true);
    }

    private void ToggleNightEffects(bool isNight)
    {
        foreach (GameObject obj in nightObjects)
        {
            if (obj != null)
            {
                // Varianta 1: Enable/Disable la tot obiectul
                obj.SetActive(isNight);

                // Varianta 2 (Opțional): Dacă vrei ca particulele să se oprească "lin" (fade out)
                // în loc să dispară brusc, ai putea folosi obj.GetComponent<ParticleSystem>().Stop();
            }
        }
    }
}