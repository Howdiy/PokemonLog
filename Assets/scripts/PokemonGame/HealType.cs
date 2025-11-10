using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// @ 회복 타입 @ 회복 로직은 전투 루틴에서 별도 처리
/// </summary>
public class HealType : SkillTpye
{
    public override int ComputeDamageOverride(Pokemon attacker, Pokemon defender, int currentDamage)
    {
        return currentDamage;
    }
}

