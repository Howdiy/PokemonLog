using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// @ 원거리 공격 타입 @ 방어 일부 무시 느낌의 관통 보정
/// </summary>
public class RangedAttackType : SkillTpye
{
    /// <summary> @ 원거리 보정 @ 최소 1 보장 </summary>
    public override int ComputeDamageOverride(Pokemon attacker, Pokemon defender, int currentDamage)
    {
        int ignoreDef = defender.def / 8;
        int finalDmg = currentDamage + ignoreDef;
        finalDmg = finalDmg <= 1 ? 1 : finalDmg;
        return finalDmg;
    }
}
