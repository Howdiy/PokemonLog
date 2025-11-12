using UnityEngine;

/// <summary>
/// @ 원거리 공격 타입 스킬
/// </summary>
public class RangedAttackType : SkillTpye
{
    public override int ComputeDamageOverride(Pokemon self, Pokemon other, int baseDamage)
    {
        // @ (atk - (def + speed)) * 배율
        float typeMul = Pokemon.battleType[(int)self.type, (int)other.type];

        float raw = (float)self.atk - ((float)other.def + (float)other.speed);
        raw = (raw < 1f) ? 1f : raw;

        float dmgF = raw * typeMul;
        dmgF = (dmgF <= 0f) ? 1f : dmgF;
        int dmg = (int)dmgF;
        return dmg;
    }
}   