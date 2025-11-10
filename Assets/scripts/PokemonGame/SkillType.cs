using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// @ 스킬 타입 기본 베이스
/// </summary>
public class SkillTpye
{
    /// <summary> @ 스킬명 4칸 </summary>
    public string[] skillNames = new string[4];

    /// <summary> @ 스킬 타입 4칸 (공격, 방어, 회복 등) @ 기존 유지 </summary>
    public Pokemon.SkillType[] skillTypes = new Pokemon.SkillType[4];

    /// <summary> @ 기본 생성시 초기화 </summary>
    public SkillTpye()
    {
        for (int i = 0; i < 4; i++)
        {
            skillNames[i] = "";
            skillTypes[i] = Pokemon.SkillType.atkSk;   // 기존 값 유지
        }
    }

    /// <summary> @ 스킬명 접근 </summary>
    public virtual string Skill1() { return skillNames[0]; }
    public virtual string Skill2() { return skillNames[1]; }
    public virtual string Skill3() { return skillNames[2]; }
    public virtual string Skill4() { return skillNames[3]; }

    /// <summary>
    /// @ 데미지 오버라이드(전략 핫스팟)
    /// @ attacker, defender, 현재 계산된 데미지(정수)를 받아 최종 데미지로 환산하여 반환
    /// @ 기본은 변경 없음
    /// </summary>
    public virtual int ComputeDamageOverride(Pokemon attacker, Pokemon defender, int currentDamage)
    {
        return currentDamage;
    }
}

