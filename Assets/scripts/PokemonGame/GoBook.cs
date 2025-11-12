using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// @ ²¿ºÏÀÌ
/// </summary>
public class GoBook : Pokemon
{
    public GoBook()
    {
        name = "²¿ºÏÀÌ";
        Hp += 44;
        atk += 48;
        def += 65;
        speed += 43;
        type = Tpye.water;
        index = PokemonIndex.goBook;

        skillTypeBehaviours[0] = new RangedAttackType();
        skillTypeBehaviours[1] = new RangedAttackType();
        skillTypeBehaviours[2] = new MeleeAttackType();
        skillTypeBehaviours[3] = new HealType();

        atlasResourcePath = "GoBookSpriteAtlas";
        spriteKeyChoice = "GOBOOK";
        spriteKeyBattleIdle = "GOBOOK2";
        spriteKeyAttack = "GOBOOK4";
        spriteKeySkill = "GOBOOK3";
    }

    public override string Skill1() { return "¸öÅë¹ÚÄ¡±â"; }
    public override string Skill2() { return "¾ÆÄí¾Æ Á¦Æ®"; }
    public override string Skill3() { return "²®Áú ¼û±â"; }
    public override string Skill4() { return "ÆÄµµ Å¸±â"; }
}

