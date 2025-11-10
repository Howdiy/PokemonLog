using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// @ 포켓몬 기본 모델
/// @ MonoBehaviour 미상속 @ 순수 데이터 및 전투 로직 컨테이너
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

    /// <summary> 스킬 분류 </summary>
    public enum SkillType
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
    // @   Ʈ Ű
    public string spriteKeyAttack = "";       // @   
    public string spriteKeySkill = "";        // @ ų ⿡ 
    public int atk;
    public int def;
    public int speed;

    /// <summary> 타입 상성 배율 </summary>
    const float h = 0.5f; // @ 하프 = *절반
    const float n = 1f;   // @ 노말 = *1배
    const float g = 2f;   // @ 굿   = *2배

    /// <summary> 포즈 및 아틀라스 로딩 보조 필드 </summary>
    public string atlasResourcePath = "";     // @ Resources 하위 SpriteAtlas 경로
    public string spriteKeyChoice = "";       // @ 선택 화면용
    public string spriteKeyBattleIdle = "";   // @ 배틀 대기 포즈

    // @ 새로 참조되는 필드 추가(에러 CS1061 해결용)
    public string spriteKeyAtk = "";          // @ 공격 포즈(신규 명칭)
    public string spriteKeyDef = "";          // @ 방어/피격 포즈(신규 명칭)
    public string spriteKeyHp = "";          // @ HP 관련 표시용 키(신규 명칭)

    /// <summary> 스킬 이름 4슬롯 </summary>
    public string[] skillNames = new string[4];

    /// <summary> 스킬 타입 동작 모델(전략 패턴) </summary>
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
            if (info != null)
            {
                if (info.hpText != null)
                {
                    info.hpText.text = hp.ToString();
                }
            }
        }
    }

    /// <summary> 생성자 @ 기본 스킬 이름 세팅 </summary>
    public Pokemon()
    {
        skillNames[0] = Skill1();
        skillNames[1] = Skill2();
        skillNames[2] = Skill3();
        skillNames[3] = Skill4();

        // @ 기본 매핑(자식 클래스에서 자유롭게 덮어씀)
        skillTypeBehaviours[0] = new MeleeAttackType();
        skillTypeBehaviours[1] = new RangedAttackType();
        skillTypeBehaviours[2] = new DefenseType();
        skillTypeBehaviours[3] = new HealType();

        // @ 포켓몬마다 초기에 랜덤한 개체값 부여
        Hp = Random.Range(0, 32);
        atk = Random.Range(0, 32);
        def = Random.Range(0, 32);
        speed = Random.Range(0, 32);
    }

    /// <summary> 행=공격, 열=방어 </summary>
    /// <summary> 타입 상성별 데미지배율 배열 @ static </summary>
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
    /// @ 전투 1행동 코루틴
    /// @ skillIndex < 0 또는 atkSk = 일반공격, defSk = 방어+5, hpSk = 회복+15
    /// </summary>
    public IEnumerator Attack(Pokemon other, int skillIndex)
    {
        if (skillIndex >= 0)
        {
            SkillType st = SkillType.atkSk;
            if (skillIndex == 1)
            {
                st = SkillType.defSk;
            }
            else
            {
                if (skillIndex == 2)
                {
                    st = SkillType.hpSk;
                }
            }

            if (st == SkillType.defSk)
            {
                def = def + 5;
                if (PokemonBattleManager.instance != null)
                {
                    if (PokemonBattleManager.instance.textLog != null)
                    {
                        PokemonBattleManager.instance.textLog.text = name + "의 방어가 5 상승했다.";
                    }
                }
                yield return new WaitForSeconds(0.5f);
                yield break;
            }

            if (st == SkillType.hpSk)
            {
                Hp = Hp + 15;
                if (PokemonBattleManager.instance != null)
                {
                    if (PokemonBattleManager.instance.textLog != null)
                    {
                        PokemonBattleManager.instance.textLog.text = name + "의 체력이 15 회복됐다.";
                    }
                }
                yield return new WaitForSeconds(0.5f);
                yield break;
            }
        }

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
                        PokemonBattleManager.instance.textLog.text = "효과는 미미했다";
                    }
                }
            }
        }

        dmg = (dmg <= 0f) ? 1f : dmg;

        int adjustedDamage = (int)dmg;
        if (skillTypeBehaviours != null)
        {
            if (skillIndex >= 0)
            {
                if (skillIndex < skillTypeBehaviours.Length)
                {
                    SkillTpye model = skillTypeBehaviours[skillIndex];
                    if (model != null)
                    {
                        adjustedDamage = model.ComputeDamageOverride(this, other, adjustedDamage);
                    }
                }
            }
        }

        other.Hp = other.Hp - adjustedDamage;
        other.Hp = (other.Hp <= 0) ? 0 : other.Hp;

        yield return new WaitForSeconds(1f);
    }
}
