using UnityEngine;

/// <summary>
/// @ 근접 공격 타입 스킬
/// </summary>
public class MeleeAttackType : SkillTpye
{
    public override int ComputeDamageOverride(Pokemon self, Pokemon other, int baseDamage)
    {
        // @ (atk - def) * 배율 + 명중 판정
        float typeMul = Pokemon.battleType[(int)self.type, (int)other.type];

        float raw = (float)self.atk - (float)other.def;
        raw = (raw < 1f) ? 1f : raw;

        // @ speed 퍼센트 기반 명중률 가정 @ 100 - speed @ 5..95 범위
        int sp = other.speed;
        int spPercent = (sp < 0) ? 0 : sp;
        spPercent = (spPercent > 100) ? 100 : spPercent;
        int hitChance = 100 - spPercent;
        hitChance = (hitChance < 5) ? 5 : hitChance;
        hitChance = (hitChance > 95) ? 95 : hitChance;

        int roll = Random.Range(0, 100);
        bool hit = (roll < hitChance) ? true : false;

        if (!hit)
        {
            if (PokemonBattleManager.instance != null)
            {
                if (PokemonBattleManager.instance.textLog != null)
                {
                    PokemonBattleManager.instance.textLog.text = "공격이 빗나갔다";
                }
            }
            return 0;
        }

        float dmgF = raw * typeMul;
        dmgF = (dmgF <= 0f) ? 1f : dmgF;
        int dmg = (int)dmgF;
        return dmg;
    }
}
