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
    private HashSet<GameObject> hitRegistry = new HashSet<GameObject>();

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
        // 1. Verificăm dacă a fost deja lovit
        if (hitRegistry.Contains(other.gameObject))
        {
            Debug.Log($"Am lovit {gameObject.name}");

            return;
        }

        // 2. Încercăm să obținem interfața IDamageable (Player, Bază, alt NPC)
        AllyEntity damageableTarget = other.GetComponent<AllyEntity>();
        
        // Dacă nu este o țintă validă, ignorăm coliziunea
        if (damageableTarget == null)
        {
            Debug.Log($"Nu am gasit pe obiect  damageableTarget{gameObject.name}");
            return;
        }
        
        Debug.Log($"Am lovit {gameObject.name}");


        // 3. Aplicăm Daunele

        // Obținem datele de atac direct de la NPC
        float damage = npcController.Damage;
        ToolType toolType = ToolType.Claw; // Presupunem un tip fix pentru inamic

        // Aplicăm damage-ul țintei
        damageableTarget.TakeDamage(damage, toolType);

        // 4. Înregistrăm ținta
        hitRegistry.Add(other.gameObject);
        
        // Debug.Log($"Lovitură NPC aplicată: {other.gameObject.name} (Damage: {damage})");
    }

    /// <summary>
    /// Golește registrul de lovituri. Trebuie apelată la START-ul fiecărei ferestre de atac (din Animation Event).
    /// </summary>
    public void ClearHitRegistry()
    {
        hitRegistry.Clear();
    }
}