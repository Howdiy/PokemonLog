using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// @ 포켓몬 기본 모델 @ MonoBehaviour 미상속
/// </summary>
public class Pokemon
{
    /// <summary> 타입 </summary>
    public enum Tpye
    {
        fire,
        water,
        grass,
        elec
    }

    /// <summary> 인덱스 </summary>
    public enum PokemonIndex
    {
        pikach,
        paily,
        goBook,
        eSang
    }

    /// <summary> 타입 상성 </summary>
    enum SkillType
    {
        atkSk,
        defSk,
        hpSk
    }
    public Tpye type;
    public PokemonIndex index;
    public PokemonInfo info;

    public string name;

    private int hp;
    public string spriteKeyAttack = "";
    public string spriteKeySkill = "";
    public int atk;
    public int def;
    public int speed;

    /// <summary> 타입 상성 배율 </summary>
    const float h = 0.5f;
    const float n = 1f;
    const float g = 2f;

    /// <summary> 아틀라스 키 </summary>
    public string atlasResourcePath = "";
    public string spriteKeyChoice = "";
    public string spriteKeyBattleIdle = "";
    public string spriteKeyAtk = "";
    public string spriteKeyDef = "";
    public string spriteKeyHp = "";

    /// <summary> 스킬 이름 </summary>
    public string[] skillNames = new string[4];

    /// <summary> 스킬 타입 동작 모델 </summary>
    public SkillTpye[] skillTypeBehaviours = new SkillTpye[4];

    public int Hp
    {
        get
        {
            return hp;
        }
        set
        {
            hp = value;
            if (hp < 0) { hp = 0; }
        }
    }

    /// <summary> 생성자 @ 기본 스킬 이름 세팅 </summary>
    public Pokemon()
    {
        skillNames[0] = Skill1();
        skillNames[1] = Skill2();
        skillNames[2] = Skill3();
        skillNames[3] = Skill4();

        // 포켓몬의 스킬 매핑
        skillTypeBehaviours[0] = new MeleeAttackType();
        skillTypeBehaviours[1] = new RangedAttackType();
        skillTypeBehaviours[2] = new DefenseType();
        skillTypeBehaviours[3] = new HealType();

        // 포켓몬 초기 개체값
        Hp = Random.Range(0, 32);
        atk = Random.Range(0, 32);
        def = Random.Range(0, 32);
        speed = Random.Range(0, 32);
    }

    /// <summary> 행=공격, 열=방어 </summary>
    public static float[,] battleType =
    {
        { h, h, g, n },
        { g, h, h, n },
        { h, g, h, n },
        { n, g, h, h },
    };

    /// <summary> 스킬 표시명 접근자 </summary>
    public virtual string Skill1() { return "공격"; }
    public virtual string Skill2() { return "공격"; }
    public virtual string Skill3() { return "공격"; }
    public virtual string Skill4() { return "공격"; }

    /// <summary>
    /// 전투 1행동 코루틴
    /// skillIndex < 0 이면 일반공격
    /// HealType, DefenseType 은 스탯 조정 및 로그 후 종료
    /// </summary>
    public IEnumerator Attack(Pokemon other, int skillIndex)
    {
        // 회복 및 방어 스킬 판정
        bool isHeal = false;
        bool isDefense = false;
        SkillTpye model = null;

        if (skillIndex >= 0)
        {
            if (skillIndex < skillTypeBehaviours.Length)
            {
                model = skillTypeBehaviours[skillIndex];
            }
        }

        if (model != null)
        {
            if (model is HealType) { isHeal = true; }
            else
            {
                if (model is DefenseType) { isDefense = true; }
            }
        }

        if (isDefense)
        {
            int maxDef = def * 2;
            if (maxDef < 10) { maxDef = 10; }
            int add = Random.Range(10, maxDef + 1);

            // 버프는 배틀매니저를 통해 적용 및 만료 관리
            if (PokemonBattleManager.instance != null)
            {
                PokemonBattleManager.instance.ApplyDefenseBuff(this, add);
                if (PokemonBattleManager.instance.textLog != null)
                {
                    PokemonBattleManager.instance.textLog.text = name + "의 방어가 " + add.ToString() + " 상승하였다.";
                }
            }
            yield return new WaitForSeconds(0.5f);
            yield break;
        }

        if (isHeal)
        {
            int maxHeal = Hp * 2;
            if (maxHeal < 10) { maxHeal = 10; }
            int heal = Random.Range(10, maxHeal + 1);
            Hp = Hp + heal;

            if (PokemonBattleManager.instance != null)
            {
                if (PokemonBattleManager.instance.textLog != null)
                {
                    PokemonBattleManager.instance.textLog.text = name + "의 체력이 " + heal.ToString() + " 회복되었다.";
                }
            }
            yield return new WaitForSeconds(0.5f);
            yield break;
        }

        // @ 공격 라벨 표시
        if (PokemonBattleManager.instance != null)
        {
            if (PokemonBattleManager.instance.textLog != null)
            {
                string dispName = "공격";
                if (skillNames != null)
                {
                    if (skillIndex >= 0)
                    {
                        if (skillIndex < 4)
                        {
                            dispName = skillNames[skillIndex];
                        }
                    }
                }
                PokemonBattleManager.instance.textLog.text = name + "의 " + dispName + " 공격";
            }
        }

        // 일반 공격 데미지 기본 공식
        float typeMul = battleType[(int)type, (int)other.type];
        float raw = (atk - (other.def * 0.5f));
        raw = raw < 1f ? 1f : raw;
        float dmg = raw * typeMul;

        if (typeMul > 1.5f)
        {
            if (PokemonBattleManager.instance != null)
            {
                if (PokemonBattleManager.instance.textLog != null)
                {
                    PokemonBattleManager.instance.textLog.text = "효과는 굉장했다!";
                }
            }
        }
        else
        {
            if (typeMul < 0.75f)
            {
                if (PokemonBattleManager.instance != null)
                {
                    if (PokemonBattleManager.instance.textLog != null)
                    {
        if (skillIndex < 0)
        {
            if (PokemonBattleManager.instance != null)
            {
                bool counter = PokemonBattleManager.instance.TryApplyCounterDamage(other, this);
                if (counter)
                {
                    yield return new WaitForSeconds(0.35f);
                }
            }
        }

                        PokemonBattleManager.instance.textLog.text = "효과는 미미했다";
                    }
                }
            }
        }

        int adjustedDamage = (int)((dmg <= 0f) ? 1f : dmg);

        if (skillIndex >= 0)
        {
            if (skillIndex < skillTypeBehaviours.Length)
            {
                SkillTpye m = skillTypeBehaviours[skillIndex];
                if (m != null)
                {
                    adjustedDamage = m.ComputeDamageOverride(this, other, adjustedDamage);
                }
            }
        }
        // 데미지 누적하여 현재체력으로 변경
        other.Hp = other.Hp - adjustedDamage;
        // -Hp 표기 방지용
        other.Hp = (other.Hp <= 0) ? 0 : other.Hp;

        yield return new WaitForSeconds(1f);
    }
}
