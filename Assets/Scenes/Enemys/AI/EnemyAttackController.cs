using UnityEngine;

/// <summary>
/// Componentă atașată Hitbox-ului unui inamic. Furnizează datele de atac
/// (Damage și ToolType) către HitboxHandler, preluate din ZombieNPC.
/// </summary>
public class EnemyAttackController : MonoBehaviour, ToolHitboxHandler.IWeaponData
{
    // Referința la scriptul care gestionează coliziunile (Hitbox-ul propriu-zis)
    public EnemyHitboxHandler hitboxHandler;
    
    // Referința la componenta NPC (pentru a obține Damage-ul)
    private NPCBase npcController; 
    
    // Variabilă de stare pentru fereastra de atac
    private bool isAttackWindowOpen = false; 

    private void Awake()
    {
        npcController = GetComponentInParent<NPCBase>();

        if (npcController == null)
        {
            Debug.LogError($"EnemyAttackController pe {gameObject.name} nu a găsit un ZombieNPC părinte!");
            return;
        }

        // if (hitboxHandler != null)
        // {
        //     // Setăm această componentă ca sursă de date pentru Hitbox
        //     hitboxHandler.weaponDataSource = this;
        // }
        // else
        // {
        //     Debug.LogError($"EnemyAttackController pe {gameObject.name} nu are referință la ToolHitboxHandler.");
        // }
    }
    
    // =================================================================
    // IMPLEMENTAREA IWeaponData
    // =================================================================

    public float GetAttackDamage()
    {
        // Returnează damage-ul setat în statistica NPC-ului
        return npcController != null ? npcController.Damage : 0f;
    }

    public ToolType GetToolType()
    {
        // Returnează tipul de atac al inamicului (ex: Claw, pentru a afecta rezistența)
        return ToolType.Claw; 
    }

    public void ApplyToolDurabilityLoss()
    {
        // Inamicii nu pierd durabilitate.
    }

    public void NotifyHitboxCleared()
    {
        // Funcție necesară pentru interfață.
    }

    // =================================================================
    // METODE DE CONTROL (Apelate de ZombieNPC prin Animation Events)
    // =================================================================

    /// <summary> Activează Hitbox-ul și resetează registrul de lovituri. </summary>
    public void StartAttackWindow()
    {
        if (hitboxHandler != null)
        {
            isAttackWindowOpen = true;
            hitboxHandler.gameObject.SetActive(true);
            hitboxHandler.ClearHitRegistry(); 
        }
    }

    /// <summary> Dezactivează Hitbox-ul. </summary>
    public void EndAttackWindow()
    {
        if (hitboxHandler != null)
        {
            hitboxHandler.gameObject.SetActive(false);
        }
        
        isAttackWindowOpen = false; 
    }
}