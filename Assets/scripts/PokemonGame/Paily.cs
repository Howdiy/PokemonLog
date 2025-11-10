using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// @ ÆÄÀÌ¸®
/// </summary>
public class Paily : Pokemon
{
    public Paily()
    {
        name = "ÆÄÀÌ¸®";
        Hp += 39;
        atk += 52;
        def += 43;
        speed += 65;
        type = Tpye.fire;
        index = PokemonIndex.paily;

        skillTypeBehaviours[0] = new RangedAttackType();
        skillTypeBehaviours[1] = new RangedAttackType();
        skillTypeBehaviours[2] = new MeleeAttackType();
        skillTypeBehaviours[3] = new HealType();

        atlasResourcePath = "PailySpriteAtlas";
        spriteKeyChoice = "PAILY";
        spriteKeyBattleIdle = "paily2";
        spriteKeyAttack = "paily4";
        spriteKeySkill = "paily3";
    }

    public override string Skill1() { return "ÇÒÄû±â"; }
    public override string Skill2() { return "ºÒ²É¼¼·Ê"; }
    public override string Skill3() { return "ºÒ²É¾ö´Ï"; }
    public override string Skill4() { return "È¸¿À¸® ºÒ²É"; }
}


