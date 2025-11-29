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
        // Ieșire rapidă dacă nu avem sursa de date sau dacă obiectul nu este activ.
        if (weaponDataSource == null || !gameObject.activeInHierarchy) return;

        // 1. Prevenirea Multi-Hit: Verificăm registrul
        if (hitTargets.Contains(other))
        {
            return; 
        }

        // 2. Identificare Țintă: Căutăm Entități sau Resurse pe obiectul lovit.
        // *NOTĂ: Asigură-te că Resource.cs și Entity.cs există în proiectul tău.*
        Resource resource = other.GetComponent<Resource>();
        Entity entity = other.GetComponent<Entity>();
        
        // Verificăm dacă am lovit o țintă validă
        if (resource != null || entity != null)
        {
            // Preluăm datele atacului de la sursa de date (ToolController)
            float damage = weaponDataSource.GetAttackDamage();
            ToolType toolType = weaponDataSource.GetToolType();

            // Aplicăm damage-ul/recoltarea
            if (resource != null)
            {
                resource.Harvest(damage, toolType);
            }
            else if (entity != null) 
            {
                entity.TakeDamage(damage, toolType);
            }

            // Aplicăm Uzura: Cerem sursei de date (ToolController) să aplice uzura pe slot.
            // Aceasta va scădea durabilitatea pe InventorySlot.
            weaponDataSource.ApplyToolDurabilityLoss();
            
            // 3. Adăugăm ținta în registru pentru a preveni loviturile multiple în același swing.
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