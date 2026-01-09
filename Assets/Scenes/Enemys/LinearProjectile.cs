using UnityEngine;

public class LinearProjectile : MonoBehaviour
{
    [Header("Referință Manuală")]
    public NPCBase ownerNPC; // O poți trage în Inspector sau o setezi la Spawn

    [Header("Setări Mișcare")]
    public float speed = 25f;
    public float maxLifetime = 5f;
    
    [Header("Impact")]
    public float impactDamage = 5f;

    private Vector3 direction;
    private bool hasHit = false;
    private bool initialized = false;

    void Start()
    {
        // Verificăm dacă am primit owner-ul
        if (ownerNPC != null)
        {
            if (ownerNPC.Target != null)
            {
                // Calculăm direcția
                Vector3 targetPos = ownerNPC.Target.transform.position;
                Vector3 targetCenter = targetPos + Vector3.up * 0.8f; 
                
                direction = (targetCenter - transform.position).normalized;
                transform.forward = direction;
                
                initialized = true;
                Debug.Log($"[Projectile] Lansat cu succes către {ownerNPC.Target.name}");
            }
            else
            {
                Debug.LogWarning($"[Projectile] Owner-ul {ownerNPC.name} nu are Target!");
                Destroy(gameObject);
            }
        }
        else
        {
            // Dacă ai uitat să îl tragi în Inspector sau să îl setezi din cod
            Debug.LogError("[Projectile] Lipsește referința către NPCBase!");
            Destroy(gameObject);
        }

        Destroy(gameObject, maxLifetime);
    }

    void Update()
    {
        if (!initialized || hasHit) return;
        transform.position += direction * speed * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        HandleCollision(other);
    }

    // Adăugăm și Stay pentru situațiile în care Enter este ratat sau obiectele se suprapun
    private void OnTriggerStay(Collider other)
    {
        HandleCollision(other);
    }

    private void HandleCollision(Collider other)
    {
        // Dacă am lovit deja ceva valid, nu mai procesăm nimic
        if (hasHit) return;

        // 1. Ignorăm shooter-ul
        if (ownerNPC != null && other.transform.root == ownerNPC.transform.root) return;
        
        // 2. Ignorăm alte proiectile și triggere (care nu sunt AllyEntity)
        if (other.isTrigger || other.GetComponent<LinearProjectile>() != null) return;

        // 3. Căutăm componenta AllyEntity
        AllyEntity allyVictim = other.GetComponentInParent<AllyEntity>();

        // 4. Dacă am găsit-o, aplicăm damage și distrugem proiectilul
        if (allyVictim != null)
        {
            hasHit = true;
            allyVictim.TakeDamage(impactDamage, ToolType.Claw);
            Debug.Log($"🎯 Lovitură confirmată (via Trigger) pe: {allyVictim.gameObject.name}");
            
            Destroy(gameObject);
        }
    }
}