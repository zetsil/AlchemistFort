using System;
using UnityEngine;


public enum MessageType
{
    Info,
    Alert,
    ResourceNeeded,
    Health,
    Tutorial
}

public static class GlobalEvents
{
    // ================================================================
    // EVENIMENTE VECHI (PĂSTRATE PENTRU COMPATIBILITATE SAU ALT SISTEM)
    // ================================================================

    // Eveniment bazat pe Scriptable Object (SO) ToolItem
    public static event Action<ToolItem, bool> OnEquipRequested;
    public static event Action<string> OnAnimationTriggerRequested;
    public static event Action OnDayStart;

    public static event Action OnNightStart;

    public static event Action<float, bool> OnTimeUpdate;

    public static event Action<InventorySlot> OnSlotEquipRequested;

    public static event Action<string, MessageType> OnNotificationRequested;

    public static event Action<string> OnPlaySound;

    public static event Action<string, Vector3> OnParticleEffectRequested;
    public static event Action OnPlayerDeath;
    public static event Action OnGameWin;
    public static event Action<Entity> OnEnemyDeath;
    public static event Action<float, float> OnScreenShakeRequested;
    public static event Action<string, Vector3> OnPlaySoundAtPosition;
    public static System.Action OnToxicGasStart;
    public static System.Action OnToxicGasStop;
    public static event Action<string> OnAttackImpactPerformed;

    public static void NotifyToxicGasStart() => OnToxicGasStart?.Invoke();
    public static void NotifyToxicGasStop() => OnToxicGasStop?.Invoke();

    public static void TriggerPlaySoundAtPosition(string soundName, Vector3 position)
    {
        OnPlaySoundAtPosition?.Invoke(soundName, position);
    }

    public static void NotifyEnemyDeath(Entity enemy)
    {
        OnEnemyDeath?.Invoke(enemy);
    }

    public static void NotifyAttackImpact(string attackType)
    {
        OnAttackImpactPerformed?.Invoke(attackType);
    }

    public static void RequestEquip(ToolItem tool)
    {
        // Prin default, este cerere de echipare standard (nu directă)
        OnEquipRequested?.Invoke(tool, false);
        // NOTĂ: Acest apel NU va mai fi folosit pentru a echipa uneltele din inventar!
    }

    /// <summary>
    /// Se apelează când sănătatea jucătorului ajunge la 0.
    /// Poate opri gameplay-ul sau afișa ecranul de Game Over.
    /// </summary>
    public static void NotifyPlayerDeath()
    {
        Debug.Log("💀 GlobalEvents: Player has died.");
        OnPlayerDeath?.Invoke();
    }

    public static void RequestScreenShake(float intensity, float duration)
    {
        OnScreenShakeRequested?.Invoke(intensity, duration);
    }

    /// <summary>
    /// Se apelează când toate valurile au fost terminate sau obiectivul a fost atins.
    /// </summary>
    public static void NotifyGameWin()
    {
        Debug.Log("🏆 GlobalEvents: Victory achieved!");
        OnGameWin?.Invoke();
    }

    public static void RequestDirectEquipFromWorld(ToolItem tool)
    {
        OnEquipRequested?.Invoke(tool, true);
        // NOTĂ: Acest apel NU va mai fi folosit pentru a echipa uneltele din inventar!
    }


    public static void RequestAnimationTrigger(string triggerName)
    {
        if (!string.IsNullOrEmpty(triggerName))
        {
            OnAnimationTriggerRequested?.Invoke(triggerName);
        }
        else
        {
            Debug.LogError("Cerere de Trigger de Animație invalidă: Numele Trigger-ului lipsește.");
        }
    }

    /// <summary>
    /// NOU: Metodă apelată de InventorySlot.HandleUse() pentru a începe echiparea.
    /// </summary>
    public static void RequestSlotEquip(InventorySlot slot)
    {
        if (slot == null)
        {
            Debug.LogError("RequestSlotEquip: Slotul primit este NULL (referință inexistentă).");
            return;
        }

        if (slot.itemData == null)
        {
            // Dacă ajungi aici, înseamnă că InventoryManager a golit slotul 
            // ÎNAINTE să trimită cererea de echipare.
            Debug.LogError($"RequestSlotEquip: Slotul {slot.slotIndex} este GOL. Nu am ce echipa!");
            return;
        }

        // Dacă vrei să echipezi și mere, verifică doar itemData.
        // Dacă vrei DOAR unelte, lasă verificarea de ToolItemData.
        if (slot.ToolItemData == null)
        {
            Debug.LogWarning($"RequestSlotEquip: Itemul {slot.itemData.itemName} nu este o unealtă.");
            return;
        }

        OnSlotEquipRequested?.Invoke(slot);
    }

    public static void NotifyDayStart()
    {
        OnDayStart?.Invoke();
    }

    public static void NotifyNightStart()
    {
        OnNightStart?.Invoke();
    }

    public static void NotifyTimeUpdate(float percent, bool isNight)
    {
        OnTimeUpdate?.Invoke(percent, isNight);
    }


    public static void RequestNotification(string message, MessageType type)
    {
        if (string.IsNullOrEmpty(message))
        {
            Debug.LogError($"Cerere de notificare invalidă: Mesajul lipsește. Tip: {type}");
            return;
        }

        OnNotificationRequested?.Invoke(message, type);
    }


    public static void TriggerPlaySound(string soundName)
    {
        // Verifică dacă există abonați înainte de a declanșa evenimentul
        if (OnPlaySound != null)
        {
            OnPlaySound.Invoke(soundName);
        }
    }

    public static void RequestParticle(string effectName, Vector3 position)
    {
        OnParticleEffectRequested?.Invoke(effectName, position);
    }

}