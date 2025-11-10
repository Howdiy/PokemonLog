using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// @ 방어 타입 @ 추후 피해감소 등 확장 지점
/// </summary>
public class DefenseType : SkillTpye
{
    public override int ComputeDamageOverride(Pokemon attacker, Pokemon defender, int currentDamage)
    {
        return currentDamage;
    }
}
