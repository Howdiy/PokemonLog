using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// @ 근접 공격 타입 @ 공격력 일부 보너스 + 상대 방어 일부 반영
/// </summary>
public class MeleeAttackType : SkillTpye
{
    /// <summary> @ 근접 보정 @ 최소 1 보장 </summary>
    public override int ComputeDamageOverride(Pokemon attacker, Pokemon defender, int currentDamage)
    {
        int bonusAtk = (int)(attacker.atk * 0.20f);
        int extraDef = defender.def / 10;
        int finalDmg = currentDamage + bonusAtk - extraDef;
        finalDmg = finalDmg <= 1 ? 1 : finalDmg;
        return finalDmg;
    }
}
