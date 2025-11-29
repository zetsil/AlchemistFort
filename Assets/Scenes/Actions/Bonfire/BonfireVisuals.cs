using UnityEngine;

public class BonfireVisuals : MonoBehaviour
{
    [HideInInspector] public bool isBonfireLit = false;
    
    [Header("Bonfire Visuals")]
    public GameObject fireParticles;
    public GameObject pointLight;
    
    // NOTĂ: Logica din Start() din vechea ta clasă va fi mutată aici!
    void Start()
    {
        // Logica de găsire a copiilor și dezactivare la Start se mută aici.
        // ...
        if (fireParticles != null) fireParticles.SetActive(false);
        if (pointLight != null) pointLight.SetActive(false);
    }
}
