using UnityEngine;


public class AllyEntity : Entity
{
    // 🚨 Override al metodei TakeDamage din Entity
    public override void TakeDamage(float baseDamage, ToolType attackingToolType)
    {
        base.TakeDamage(baseDamage, attackingToolType);
    }
    
}