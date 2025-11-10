using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// @ ų Ÿ ⺻ 
/// </summary>
public class SkillTpye
{
    /// <summary> @    </summary>
    public virtual int ComputeDamageOverride(Pokemon attacker, Pokemon defender, int currentDamage)
    {
        return currentDamage;
    }
}
