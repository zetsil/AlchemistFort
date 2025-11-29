using UnityEngine;

// Clasa Resource moștenește din clasa ta de bază Entity
public class Resource : Entity
{
    [Header("Resource Specifics")]
    [Tooltip("Tipul de unealtă necesară pentru recoltarea optimă (ex: Axe pentru Copac).")]
    public ToolType requiredToolType = ToolType.None; 
    
    [Tooltip("Mesaj afișat dacă jucătorul folosește unealta greșită.")]
    [SerializeField]
    private string wrongToolMessage = "Ai nevoie de unealta corectă pentru a recolta acest lucru!";

    
    protected override void Start()
    {
        // Inițializează viața curentă din ScriptableObject-ul EntityData
        base.Start(); 
        
        // Dacă folosești ScriptableObject-ul EntityData (recomandat):
        // currentHealth = entityData.maxHealth;
        
        // Dacă vrei să poți suprascrie viața în inspector direct pe componentă:
        // currentHealth = maxHealth;
    }

    /// <summary>
    /// Metodă publică apelată de sistemul de interacțiune al jucătorului.
    /// </summary>
    /// <param name="damageAmount">Damage-ul de bază aplicat (din unealtă).</param>
    /// <param name="toolUsed">Tipul uneltei folosite de jucător.</param>
    public void Harvest(float damageAmount, ToolType toolUsed)
    {
        // 1. Verificare opțională: Asigură-te că jucătorul folosește cel puțin unealta *corectă* //    (chiar dacă logica de damage este deja în TakeDamage)
        if (requiredToolType != ToolType.None && requiredToolType != toolUsed)
        {
            // O poți folosi pentru a afișa un mesaj de notificare pe ecran
            Debug.LogWarning(wrongToolMessage); 
        }

        // 2. Aplică damage-ul, lăsând clasa de bază Entity să gestioneze viața, 
        //    multiplicatorul de damage (eficacitatea uneltei) și logica de Die/Drop.
        TakeDamage(damageAmount, toolUsed);
    }
    
    // Suprascriem metoda Die pentru a adăuga logica specifică resurselor, 
    // cum ar fi schimbarea modelului vizual (de la copac întreg la ciot)
    protected override void Die()
    {
        Debug.Log($"Resursa {gameObject.name} a fost epuizată.");
        
        // Aici poți adăuga logică specifică resursei, cum ar fi:
        // * Schimbarea vizualului (ex: dezactivează modelul copacului, activează modelul ciotului).
        // * Dacă vrei ca resursa să reapară (respawn)
        
        // Apelăm DropLoot din clasa de bază Entity
        DropLoot();
        
        // O resursă ar putea să nu se distrugă imediat, ci să dispară doar vizual.
        // Pentru simplitate, menținem distrugerea obiectului, ca în Entity:
        Destroy(gameObject);
    }
}