using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// @ ÀÌ»óÇØ¾¾
/// </summary>
public class Esang : Pokemon
{
    public Esang()
    {
        name = "ÀÌ»óÇØ¾¾";
        Hp += 45;
        atk += 49;
        def += 49;
        speed += 45;
        type = Tpye.grass;
        index = PokemonIndex.eSang;

        skillTypeBehaviours[0] = new RangedAttackType();
        skillTypeBehaviours[1] = new RangedAttackType();
        skillTypeBehaviours[2] = new MeleeAttackType();
        skillTypeBehaviours[3] = new HealType();

        atlasResourcePath = "EsangSpriteAtlas";
        spriteKeyChoice = "ESANG";
        spriteKeyBattleIdle = "ESANG2";
        spriteKeyAttack = "ESANG4";
        spriteKeySkill = "ESANG3";
    }

    public override string Skill1() { return "ÀÙ³¯°¡¸£±â"; }
    public override string Skill2() { return "³Õ±¼Ã¤Âï"; }
    public override string Skill3() { return "¼Ö¶ó ºö"; }
    public override string Skill4() { return "±¤ÇÕ¼º"; }
}

