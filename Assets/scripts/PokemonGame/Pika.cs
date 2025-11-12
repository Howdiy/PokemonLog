using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// @ 피카츄
/// </summary>
public class Pika : Pokemon
{
    public Pika()
    {
        name = "피카츄";
        Hp += 35;
        atk += 55;
        def += 40;
        speed += 90;
        type = Tpye.elec;
        index = PokemonIndex.pikach;

        skillTypeBehaviours[0] = new RangedAttackType();
        skillTypeBehaviours[1] = new RangedAttackType();
        skillTypeBehaviours[2] = new MeleeAttackType();
        skillTypeBehaviours[3] = new HealType();

        atlasResourcePath = "PikaSpriteAtlas";
        spriteKeyChoice = "PIKACH";
        spriteKeyBattleIdle = "PIKA2";
        spriteKeyAttack = "PIKA4";
        spriteKeySkill = "PIKA3";
    }

    public override string Skill1() { return "전기쇼크"; }
    public override string Skill2() { return "백만볼트"; }
    public override string Skill3() { return "전광석화"; }
    public override string Skill4() { return "일렉트릭 네트"; }
}
