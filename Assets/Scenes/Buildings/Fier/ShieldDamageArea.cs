using UnityEngine;
using System.Collections.Generic;

public class ShieldDamageArea : MonoBehaviour
{
    [Header("Setări Damage")]
    public float damageAmount = 1f;
    public float damageInterval = 2f;
    
    // Listă internă pentru a stoca doar zombii detectați
    private List<ZombieNPC> zombiesInShield = new List<ZombieNPC>();
    private float timer = 0f;

    void Update()
    {
        // Dacă nu sunt zombi în interior, nu procesăm timpul
        if (zombiesInShield.Count == 0)
        {
            timer = 0f;
            return;
        }

        timer += Time.deltaTime;

        // Verificăm dacă a trecut intervalul de 2 secunde
        if (timer >= damageInterval)
        {
            ApplyShieldDamage();
            timer = 0f;
        }
    }

    private void ApplyShieldDamage()
    {
        // Parcurgem lista invers pentru a evita erorile la eliminarea obiectelor distruse
        for (int i = zombiesInShield.Count - 1; i >= 0; i--)
        {
            if (zombiesInShield[i] != null)
            {
                // Aplicăm damage prin componenta Entity moștenită sau direct
                // Folosim ToolType.None deoarece este damage de mediu/magie
                zombiesInShield[i].TakeDamage(damageAmount, ToolType.Shield);
            }
            else
            {
                // Eliminăm referințele null (zombii care au murit între timp)
                zombiesInShield.RemoveAt(i);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Verificăm dacă obiectul are atașat scriptul specific ZombieNPC
        ZombieNPC zombie = other.GetComponent<ZombieNPC>();
        
        if (zombie != null && !zombiesInShield.Contains(zombie))
        {
            zombiesInShield.Add(zombie);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        ZombieNPC zombie = other.GetComponent<ZombieNPC>();
        
        if (zombie != null && zombiesInShield.Contains(zombie))
        {
            zombiesInShield.Remove(zombie);
        }
    }
}