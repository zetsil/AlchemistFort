using System.Collections.Generic;
using UnityEngine;

public class ToolController : MonoBehaviour, ToolHitboxHandler.IWeaponData
{
    [Header("Referințe Componente")]
    public ToolHitboxHandler hitboxHandler;
    public TrailRenderer _trail;

    // --- Hitbox state (neschimbat) ---
    private bool isAttacking = false;

    // --- Combo state ---
    private Queue<string> _attackQueue = new Queue<string>();
    private string[] _slashTriggers = { "Slash1", "Slash2", "Slash3" };
    private int _currentIndex = 0;

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
    public void OnAttackFinished()
    {
        _listenForCombo = false;

        if (_comboQueued)
            PlayNextAttack();
        else
            ResetCombo();
    }

    /// <summary>
    /// Apelată de Animation Event la începutul fazei de lovitură.
    /// </summary>
    public void StartAttackWindow()
    {
        if (hitboxHandler == null) return;

        isAttacking = true;
        hitboxHandler.gameObject.SetActive(true);
        hitboxHandler.ClearHitRegistry();

        PlaySlashSound();
    }

    /// <summary>
    /// Apelată de Animation Event la sfârșitul fazei de lovitură.
    /// </summary>
    public void EndAttackWindow()
    {
        if (hitboxHandler != null)
            hitboxHandler.gameObject.SetActive(false);

        isAttacking = false;
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
            GlobalEvents.TriggerPlaySound("slash_" + slot.itemData.itemName);
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