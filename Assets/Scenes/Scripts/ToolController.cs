using UnityEngine;

/// <summary>
/// Componentă atașată obiectului uneltei echipate (Topor/Sabie).
/// Gestionează sincronizarea atacului (Animation Events), citirea datelor
/// de la InventorySlot și comunicarea cu Hitbox-ul.
/// </summary>
public class ToolController : MonoBehaviour, ToolHitboxHandler.IWeaponData
{
    // Variabila veche publică este eliminată (sau transformată în privată fără [Tooltip])
    // public InventorySlot equippedSlot; 

    [Header("Referințe Componente")]
    [Tooltip("Referința la scriptul atașat GameObject-ului Hitbox (ToolHitboxHandler).")]
    public ToolHitboxHandler hitboxHandler;

    // Variabilă de stare pentru a preveni input-ul în timpul animației sau double-hit.
    private bool isAttacking = false;
    public TrailRenderer _trail;

    // NOU: Proprietate ajutătoare care accesează direct slotul de la manager.
    private InventorySlot CurrentEquippedSlot => EquippedManager.Instance.GetEquippedSlot();


    private void Awake()
    {
        if (hitboxHandler != null)
        {
            hitboxHandler.weaponDataSource = this;
        }
        else
        {
            Debug.LogError($"ToolController pe {gameObject.name} nu are referință la ToolHitboxHandler.");
        }
    }

    // =================================================================
    // IMPLEMENTAREA IWeaponData (Sursa de date pentru Hitbox)
    // =================================================================

    public float GetAttackDamage()
    {
        InventorySlot slot = CurrentEquippedSlot; // Folosim proprietatea de acces

        // 1. Verificăm dacă există un item echipat.
        if (slot == null || slot.itemData == null)
        {
            return 0f;
        }

        // 2. Încercăm conversia sigură (as) la ToolItem pentru a accesa attackDamage.
        ToolItem tool = slot.itemData as ToolItem;

        // 3. Dacă itemul este o unealtă, returnăm damage-ul, altfel 0.
        if (tool != null)
        {
            return tool.attackDamage;
        }

        return 0f;
    }

    public ToolType GetToolType()
    {
        // Folosim proprietatea ajutătoare ToolItemData din slot, care face deja conversia 'as ToolItem'.
        // Folosim proprietatea de acces
        return CurrentEquippedSlot?.ToolItemData != null ? CurrentEquippedSlot.ToolItemData.toolCategory : ToolType.None;
    }

    public void ApplyToolDurabilityLoss()
    {
        // Aplicăm uzura direct pe Slotul din Inventar (obținut prin proprietatea de acces).
        if (CurrentEquippedSlot != null)
        {
            // Apelăm metoda care gestionează scăderea durabilității și eliminarea din slot.
            CurrentEquippedSlot.ApplyDurabilityLoss();
        }
    }

    public void NotifyHitboxCleared()
    {
        // Funcție necesară pentru interfață, poate fi goală.
    }

    // =================================================================
    // ANIMATION EVENTS (APELATE DE TIMELINE)
    // =================================================================

    /// <summary>
    /// Apelată de Animation Event la începutul fazei de lovitură.
    /// Activează Hitbox-ul și resetează registrul de lovituri (HashSet).
    /// </summary>
    public void StartAttackWindow()
    {
        // Verificăm dacă avem hitbox și dacă nu cumva e deja activ
        if (hitboxHandler == null) return;

        isAttacking = true;
        hitboxHandler.gameObject.SetActive(true);
        hitboxHandler.ClearHitRegistry();

        PlaySlashSound();

    }


    private void PlaySlashSound()
    {
        // Preluăm slotul echipat prin proprietatea existentă
        InventorySlot slot = CurrentEquippedSlot;

        if (slot != null && slot.itemData != null)
        {
            // Construim numele sunetului: "slash" + numele itemului (ex: "slashTopor")
            string soundName = "slash_" + slot.itemData.itemName;
            
            // Trimitem cererea către sistemul de sunet prin GlobalEvents
            GlobalEvents.TriggerPlaySound(soundName);
            
            // Opțional: Debug.Log pentru a verifica în consolă dacă numele e corect
            // Debug.Log($"Playing sound: {soundName}");
        }
    }

    /// <summary>
    /// Apelată de Animation Event la sfârșitul fazei de lovitură.
    /// Dezactivează Hitbox-ul.
    /// </summary>
    public void EndAttackWindow()
    {
        if (hitboxHandler != null)
        {
            // Dezactivăm ÎNTREGUL GameObject Hitbox
            hitboxHandler.gameObject.SetActive(false);
        }

        isAttacking = false; // Resetăm starea
    }

    public void ForceResetAttack()
    {
        if (hitboxHandler != null)
        {
            hitboxHandler.gameObject.SetActive(false);
        }
        isAttacking = false;
    }

    // Opțional: Dezactivăm automat hitbox-ul dacă obiectul uneltei este dezactivat
    private void OnDisable()
    {
        ForceResetAttack();
    }
    
    public void ActivaTrail()
    {
        if (_trail != null)
        {
            _trail.emitting = true;
        }
    }

    /// <summary>
    //  Dezactivează emisia trail-ului. Chemati-o la finalul atacului.
    /// </summary>
    public void DezactiveazaTrail()
    {
        if (_trail != null)
        {
            _trail.emitting = false;
        }
    }
}