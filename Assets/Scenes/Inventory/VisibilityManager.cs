using UnityEngine;
using System.Collections.Generic;

public class VisibilityManager : MonoBehaviour
{
    public Transform playerTransform;
    public float detectionRadius = 6f; 
    public LayerMask itemLayer; // Setează asta pe Layer-ul obiectelor (ex: "Interactable")
    
    [Tooltip("La câte cadre facem scanarea? (1 = fiecare cadru, 5 = de 12 ori pe sec)")]
    public int scanInterval = 3; 

    // Buffer pentru rezultate - mărit la 200 pentru a acoperi aria de 40m
    private Collider[] hitColliders = new Collider[200];
    private HashSet<VisibilityRangeController> currentlyActive = new HashSet<VisibilityRangeController>();

    void Update()
    {
        // Optimizare: Nu scanăm în fiecare frame dacă nu e nevoie
        if (Time.frameCount % scanInterval != 0 || playerTransform == null) return;

        // 1. Găsim tot ce este în arie folosind fizica
        int count = Physics.OverlapSphereNonAlloc(playerTransform.position, detectionRadius, hitColliders, itemLayer);

        HashSet<VisibilityRangeController> foundThisFrame = new HashSet<VisibilityRangeController>();

        // 2. Activăm ce am găsit
        for (int i = 0; i < count; i++)
        {
            VisibilityRangeController item = hitColliders[i].GetComponent<VisibilityRangeController>();
            if (item != null)
            {
                item.ToggleVisibility(true);
                foundThisFrame.Add(item);
                currentlyActive.Add(item);
            }
        }

        // 3. Dezactivăm ce a ieșit din arie
        // Folosim un array temporar sau curățăm lista pentru a evita erorile de modificare în loop
        currentlyActive.RemoveWhere(item => {
            if (item == null) return true; // Curățăm obiectele distruse
            if (!foundThisFrame.Contains(item))
            {
                item.ToggleVisibility(false);
                return true; // Îl scoatem din lista de active
            }
            return false;
        });
    }

    // Vizualizare în Editor pentru a vedea raza radarului
    void OnDrawGizmosSelected()
    {
        if (playerTransform == null) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(playerTransform.position, detectionRadius);
    }
}