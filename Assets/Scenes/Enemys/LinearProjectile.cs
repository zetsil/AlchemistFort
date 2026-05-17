using UnityEngine;

public class LinearProjectile : MonoBehaviour
{
    [Header("Referință Manuală")]
    public Entity entity; // S-a redenumit din ownerNPC în entity

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
        // Căutăm direct Player-ul (AllyEntity) în scenă pentru a stabili ținta
        AllyEntity player = GameObject.FindAnyObjectByType<AllyEntity>();

        if (player != null)
        {
            // Calculăm direcția către Player
            Vector3 targetPos = player.transform.position;
            // Adăugăm înălțimea de 0.8f ca proiectilul să meargă spre corp, nu la picioare
            Vector3 targetCenter = targetPos + Vector3.up * 0.8f; 
            
            direction = (targetCenter - transform.position).normalized;
            transform.forward = direction;
            
            initialized = true;
            Debug.Log($"[Projectile] Lansat cu succes către Player: {player.name}");
        }
        else
        {
            Debug.LogWarning("[Projectile] Nu am găsit niciun AllyEntity (Player) în scenă!");
            Destroy(gameObject);
            return; // Oprim execuția funcției Start aici
        }

        // Autodistrugere după timpul maxim de viață
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

    private void OnTriggerStay(Collider other)
    {
        HandleCollision(other);
    }

    private void HandleCollision(Collider other)
    {
        if (hasHit) return;

        // 1. Ignorăm shooter-ul (folosind noua variabilă 'entity')
        if (entity != null && other.transform.root == entity.transform.root) return;
        
        // 2. Ignorăm alte proiectile și triggere
        if (other.isTrigger || other.GetComponent<LinearProjectile>() != null) return;

        // 3. Căutăm componenta AllyEntity (Player)
        AllyEntity allyVictim = other.GetComponentInParent<AllyEntity>();

        // 4. Dacă am lovit player-ul, aplicăm damage
        if (allyVictim != null)
        {
            hasHit = true;
            allyVictim.TakeDamage(impactDamage, ToolType.Claw);
            Debug.Log($"🎯 Lovitură confirmată pe: {allyVictim.gameObject.name}");
            
            Destroy(gameObject);
        }
    }
}