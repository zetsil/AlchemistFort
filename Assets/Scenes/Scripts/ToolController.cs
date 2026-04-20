using System.Collections.Generic;
using UnityEngine;

public class ToolController : MonoBehaviour, ToolHitboxHandler.IWeaponData
{
    [Header("Referințe Componente")]
    public ToolHitboxHandler hitboxHandler;
    public TrailRenderer _trail;

    [Header("VFX & Ground Scanning")]
    [SerializeField] private WeaponGroundScanner groundScanner;

    // --- Hitbox state (neschimbat) ---
    private bool isAttacking = false;

    // --- Combo state ---
    private Queue<string> _attackQueue = new Queue<string>();
    private string[] _slashTriggers = { "Slash1", "Slash2", "Slash3" };
    private int _currentIndex = 0;

    private string _activeAttackName;

    private bool _isInComboSequence = false; // Suntem în mijlocul unui combo?
    private bool _listenForCombo = false;    // Fereastra în care acceptăm next click
    private bool _comboQueued = false;       // A venit un click în fereastră?

    private Animator _animator;
    private InventorySlot CurrentEquippedSlot => EquippedManager.Instance.GetEquippedSlot();

    private void Awake()
    {
        _animator = GetComponent<Animator>();

        if (hitboxHandler != null)
            hitboxHandler.weaponDataSource = this;
        else
            Debug.LogError($"ToolController pe {gameObject.name} nu are referință la ToolHitboxHandler.");

        if (groundScanner == null)
            Debug.LogWarning($"GroundScanner nu este asignat pe {gameObject.name}. Praful de pe pământ nu va apărea.");    
        }

    private void OnEnable()  => GlobalEvents.OnAnimationTriggerRequested += HandleAnimationRequest;
    private void OnDisable()
    {
        GlobalEvents.OnAnimationTriggerRequested -= HandleAnimationRequest;
        ForceResetAttack();
    }

    // =================================================================
    // COMBO LOGIC
    // =================================================================

    private void HandleAnimationRequest(string triggerName)
    {
        if (!_isInComboSequence)
        {
            // Prima lovitură — pornim direct
            PlayNextAttack();
        }
        else if (_listenForCombo)
        {
            // Suntem în fereastră, reținem intenția
            _comboQueued = true;
        }
        // Dacă _listenForCombo e false, clickul e ignorat (prea devreme sau prea târziu)
    }


    /// <summary>
    /// Apelată de Animation Event pentru a activa scanarea de praf pe pământ.
    /// </summary>
    public void StartGroundPrafScan()
    {
        if (groundScanner != null)
        {
            groundScanner.StartScanning();
        }
    }

    /// <summary>
    /// Apelată de Animation Event pentru a opri scanarea.
    /// </summary>
    public void StopGroundPrafScan()
    {
        if (groundScanner != null)
        {
            groundScanner.StopScanning();
        }
    }

    private void PlayNextAttack()
    {
        _isInComboSequence = true;
        _listenForCombo = false;
        _comboQueued = false;

        string trigger = _slashTriggers[_currentIndex];
        _currentIndex = (_currentIndex + 1) % _slashTriggers.Length;

        _animator.SetTrigger(trigger);
    }

    private void ResetCombo()
    {
        _isInComboSequence = false;
        _listenForCombo = false;
        _comboQueued = false;
        _currentIndex = 0;
    }

    // =================================================================
    // ANIMATION EVENTS
    // =================================================================

    /// <summary>
    /// Pus în clipul de animație când vrei să înceapă fereastra de combo.
    /// </summary>
    public void OpenComboWindow()
    {
        _listenForCombo = true;
    }

    /// <summary>
    /// Pus la sfârșitul fiecărui clip Slash1, Slash2, Slash3.
    /// </summary>
    public void OnAttackFinished(string type)
    {
        // Dacă nu e fereastra de combo activă, ignorăm
        if (!_isInComboSequence) return;

        // Dacă am apăsat click pentru următorul atac (_comboQueued), 
        // lăsăm orice eveniment (chiar și cel de la mijloc) să declanșeze atacul următor.
        // Asta face jocul să se simtă "fast-paced".
        if (_comboQueued)
        {
            PlayNextAttack();
            return; 
        }

        // DACĂ NU avem click în coadă, resetăm DOAR la evenimentul marcat ca "Final"
        if (type == "Final")
        {
            ResetCombo();
        }
    }

    /// <summary>
    /// Apelată de Animation Event la începutul fazei de lovitură.
    /// </summary>
    public void StartAttackWindow(string attackName)
    {
        if (hitboxHandler == null) return;

        isAttacking = true;
        _activeAttackName = attackName;

        hitboxHandler.gameObject.SetActive(true);

        var col = hitboxHandler.GetComponent<Collider>();
        if (col != null) col.enabled = true;

        hitboxHandler.ClearHitRegistry();
        PlaySlashSound();
        // StartGroundPrafScan();
    }
    

    public string GetCurrentAttackName() 
    {
        return _activeAttackName;
    }

    /// <summary>
    /// Apelată de Animation Event la sfârșitul fazei de lovitură.
    /// </summary>
    public void EndAttackWindow()
    {
        if (hitboxHandler != null)
        {
            var col = hitboxHandler.GetComponent<Collider>();
            if (col != null) col.enabled = false;
        }

        isAttacking = false;
        StopGroundPrafScan();
    }

    public void ForceResetAttack()
    {
        if (hitboxHandler != null)
            hitboxHandler.gameObject.SetActive(false);

        isAttacking = false;
        ResetCombo();
    }

    // =================================================================
    // IWEAPONDATA
    // =================================================================

    public float GetAttackDamage()
    {
        InventorySlot slot = CurrentEquippedSlot;
        if (slot == null || slot.itemData == null) return 0f;
        ToolItem tool = slot.itemData as ToolItem;
        return tool != null ? tool.attackDamage : 0f;
    }

    public ToolType GetToolType()
    {
        return CurrentEquippedSlot?.ToolItemData != null
            ? CurrentEquippedSlot.ToolItemData.toolCategory
            : ToolType.None;
    }

    public void ApplyToolDurabilityLoss()
    {
        CurrentEquippedSlot?.ApplyDurabilityLoss();
    }

    public void NotifyHitboxCleared() { }

    // =================================================================
    // TRAIL + SOUND
    // =================================================================

    private void PlaySlashSound()
    {
        InventorySlot slot = CurrentEquippedSlot;
        if (slot != null && slot.itemData != null)
            GlobalEvents.TriggerPlaySound("Slash_" + slot.itemData.itemName);
    }

    public void ActivaTrail()
    {
        if (_trail != null) _trail.emitting = true;
    }

    public void DezactiveazaTrail()
    {
        if (_trail != null) _trail.emitting = false;
    }
}