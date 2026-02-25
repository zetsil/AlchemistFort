using UnityEngine;
using System.Collections;

public class TowerAlly : AllyEntity
{
    [Header("Tower Stats")]
    public float attackRange = 15f;
    public float damage = 25f;
    public float attackCooldown = 2.0f;
    public float chargeTime = 0.8f; // Cât timp îi ia să "încarce" înainte de glonț

    [Header("Projectile Settings")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float bulletSpeed = 10f;

    [Header("Detection")]
    public LayerMask enemyLayer;
    
    private NPCBase currentTarget;
    private bool isCharging = false;
    private float nextAttackTime;

    protected override void Update()
    {
        base.Update();

        if (isCharging) return; // Dacă încarcă, nu căutăm altă țintă

        if (Time.time >= nextAttackTime)
        {
            FindTarget();
            if (currentTarget != null)
            {
                StartCoroutine(ChargeAndShoot());
            }
        }
    }

    private void FindTarget()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, attackRange, enemyLayer);
        float minDistance = Mathf.Infinity;
        NPCBase closest = null;

        foreach (var hit in hitColliders)
        {
            NPCBase npc = hit.GetComponent<NPCBase>();
            if (npc != null && npc.CurrentHealth > 0)
            {
                float d = Vector3.Distance(transform.position, npc.transform.position);
                if (d < minDistance)
                {
                    minDistance = d;
                    closest = npc;
                }
            }
        }
        currentTarget = closest;
    }

    private IEnumerator ChargeAndShoot()
    {
        isCharging = true;

        // --- START CHARGE ---
        // Aici poți activa un efect vizual de încărcare (ex: lumina crește în intensitate)
        Debug.Log("Charging attack...");
        
        float timer = 0;
        while (timer < chargeTime)
        {
            if (currentTarget == null || currentTarget.CurrentHealth <= 0)
            {
                isCharging = false;
                yield break; // Inamicul a murit sau a dispărut în timpul încărcării
            }

            // Turnul urmărește inamicul în timp ce încarcă
            RotateTowardsTarget();
            timer += Time.deltaTime;
            yield return null;
        }

        // --- SHOOT ---
        ShootProjectile();

        isCharging = false;
        nextAttackTime = Time.time + attackCooldown;
    }

    private void RotateTowardsTarget()
    {
        Vector3 dir = currentTarget.transform.position - transform.position;
        dir.y = 0;
        if (dir != Vector3.zero)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 5f);
    }

    private void ShootProjectile()
    {
        if (currentTarget == null) return;

        GameObject bulletGO = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
        Projectile proj = bulletGO.GetComponent<Projectile>();
        
        if (proj != null)
        {
            proj.Setup(currentTarget, damage, bulletSpeed);
        }

        // Sunet de tragere
        if (entityData != null)
            GlobalEvents.TriggerPlaySoundAtPosition("Shoot_" + entityData.name, transform.position);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}