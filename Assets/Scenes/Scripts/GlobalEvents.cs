using System;
using UnityEngine;

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

    public static event Action<float> OnTimeUpdate;


    // ================================================================
    // EVENIMENTE NOI (PENTRU SISTEMUL DE ECHIPARE BAZAT PE SLOT)
    // ================================================================

    /// <summary>
    /// NOU: Eveniment pentru a cere echiparea unui slot instanță specific, 
    /// care conține datele dinamice (durabilitatea).
    /// </summary>
    public static event Action<InventorySlot> OnSlotEquipRequested;


    // ================================================================
    // METODE DE APELARE (INVOKE)
    // ================================================================

    // Am păstrat metodele vechi, dar ele vor fi ignorate de EquippedManager
    // dacă acesta se bazează pe noul event OnSlotEquipRequested.

    public static void RequestEquip(ToolItem tool)
    {
        // Prin default, este cerere de echipare standard (nu directă)
        OnEquipRequested?.Invoke(tool, false);
        // NOTĂ: Acest apel NU va mai fi folosit pentru a echipa uneltele din inventar!
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
        if (slot == null || slot.ToolItemData == null)
        {
            Debug.LogError("Cerere de echipare slot invalidă: Slotul este null sau nu este ToolItem.");
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

    public static void NotifyTimeUpdate(float percentRemaining)
    {
        OnTimeUpdate?.Invoke(percentRemaining);
    }
}