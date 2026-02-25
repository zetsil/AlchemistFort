using UnityEngine;

public class Projectile : MonoBehaviour
{
    private NPCBase target;
    private float damage;
    private float speed;

    public void Setup(NPCBase targetNPC, float dmg, float moveSpeed)
    {
        target = targetNPC;
        damage = dmg;
        speed = moveSpeed;
        
        // Distruge glonțul după 5 secunde dacă nu lovește nimic (safety)
        Destroy(gameObject, 5f);
    }

    void Update()
    {
        if (target == null || target.CurrentHealth <= 0)
        {
            Destroy(gameObject);
            return;
        }

        // Deplasare spre inamic
        transform.position = Vector3.MoveTowards(transform.position, target.transform.position + Vector3.up, speed * Time.deltaTime);
        transform.LookAt(target.transform.position + Vector3.up);

        // Verificăm distanța mică pentru "impact"
        if (Vector3.Distance(transform.position, target.transform.position + Vector3.up) < 0.2f)
        {
            HitTarget();
        }
    }

    void HitTarget()
    {
        if (target != null)
        {
            target.TakeDamage(damage, ToolType.Axe);
        }
        
        // Aici poți spawna un efect de explozie/particule
        Destroy(gameObject);
    }
}