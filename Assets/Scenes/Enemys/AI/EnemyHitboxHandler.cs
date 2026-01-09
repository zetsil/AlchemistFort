using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Gestionează coliziunile Hitbox-ului unui inamic și aplică daune
/// entităților IDamageable, folosind statistica de Damage a NPC-ului.
/// </summary>
public class EnemyHitboxHandler : MonoBehaviour
{
    // Referința la NPC-ul părinte pentru a obține Damage-ul și ToolType-ul de atac.
    private NPCBase npcController; 
    
    // Registru pentru a stoca obiectele deja lovite într-o fereastră de atac.
    HashSet<AllyEntity> hitRegistry = new HashSet<AllyEntity>();


    private void Awake()
    {
        // Găsim referința la NPC/Zombi în ierarhia părinte
        npcController = GetComponentInParent<NPCBase>();

        if (npcController == null)
        {
            Debug.LogError($"EnemyHitboxHandler pe {gameObject.name} nu a găsit un NPCBase părinte!");
        }
    }

    /// <summary>
    /// Se declanșează când collider-ul Hitbox-ului intră în contact cu o altă entitate.
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        AllyEntity damageableTarget = other.GetComponentInParent<AllyEntity>();

        if (damageableTarget == null)
        {
            Debug.Log($"Nu am gasit AllyEntity pe {other.name}");
            return;
        }

        if (hitRegistry.Contains(damageableTarget))
            return;

        float damage = npcController.Damage;
        ToolType toolType = ToolType.Claw;

        damageableTarget.TakeDamage(damage, toolType);

        hitRegistry.Add(damageableTarget);

        Debug.Log($"Lovit {damageableTarget.name} cu {damage} damage");
    }

    /// <summary>
    /// Golește registrul de lovituri. Trebuie apelată la START-ul fiecărei ferestre de atac (din Animation Event).
    /// </summary>
    public void ClearHitRegistry()
    {
        hitRegistry.Clear();
    }
}