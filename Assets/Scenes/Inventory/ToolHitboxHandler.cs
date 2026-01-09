using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Script atașat Collider-ului Hitbox al uneltei.
/// Responsabil pentru detectarea loviturilor și aplicarea damage-ului și uzurii.
/// </summary>
public class ToolHitboxHandler : MonoBehaviour
{
    // Registrul: Stochează Collider-ele care au fost deja lovite în acest swing,
    // prevenind aplicarea damage-ului de mai multe ori pe aceeași țintă.
    private HashSet<Collider> hitTargets = new HashSet<Collider>();

    [Tooltip("Referință la componenta ToolController (implementează IWeaponData).")]
    // Folosim interfața pentru a menține scriptul generic și decuplat de ToolController.
    public IWeaponData weaponDataSource; 

    // =================================================================
    // INTERFAȚA DE DATE ȘI ACȚIUNI
    // =================================================================

    /// <summary>
    /// Interfața pe care trebuie să o implementeze sursa datelor (ToolController).
    /// </summary>
    public interface IWeaponData
    {
        float GetAttackDamage();
        ToolType GetToolType();
        void ApplyToolDurabilityLoss();
        void NotifyHitboxCleared(); 
    }

    // =================================================================
    // LOGICA DE DETECȚIE A COLIZIUNILOR
    // =================================================================

    private void OnTriggerEnter(Collider other)
    {
        if (weaponDataSource == null || !gameObject.activeInHierarchy) return;
        if (hitTargets.Contains(other)) return;
        if (other.CompareTag("Player")) return;

        // Deoarece Resource moștenește din Entity, acest apel le prinde pe ambele!
        Entity targetEntity = other.GetComponent<Entity>();
        
        if (targetEntity != null && !targetEntity.isDead && targetEntity.entityData != null)
        {
            float damage = weaponDataSource.GetAttackDamage();
            ToolType toolType = weaponDataSource.GetToolType();

            // 1. Preluăm numele curat din Scriptable Object-ul comun
            string targetName = targetEntity.entityData.name; 

            // 2. Construim cheia: ex. "Hit_Pickaxe_Rock" sau "Hit_Sword_Goblin"
            string particleKey = $"Hit_{toolType}_{targetName}";

            // 3. Punctul de impact
            Vector3 impactPoint = other.ClosestPoint(transform.position);

            // 4. Trimitem semnalul către ParticleMap
            GlobalEvents.RequestParticle(particleKey, impactPoint);

            // 5. Aplicăm logica de damage/recoltare
            // Dacă ținta este Resource, folosim Harvest, altfel TakeDamage
            if (targetEntity is Resource resource)
            {
                resource.Harvest(damage, toolType);
                weaponDataSource.ApplyToolDurabilityLoss();

            }
            else
            {
                targetEntity.TakeDamage(damage, toolType);
                weaponDataSource.ApplyToolDurabilityLoss();
            }

            hitTargets.Add(other);
        }
    }

    /// <summary>
    /// Metodă apelată de ToolController.StartAttackWindow() pentru a reseta
    /// lista de ținte lovite pentru un nou ciclu de atac.
    /// </summary>
    public void ClearHitRegistry()
    {
        hitTargets.Clear();
        weaponDataSource.NotifyHitboxCleared();
    }
}