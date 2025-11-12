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
        elec,
        nomel    // @ 요청 사항 @ 타입 확장 자리만 추가 @ 현재 상성 행렬에는 미사용
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

    public string spriteKeyAttack = "";
    public string spriteKeySkill = "";
    public int atk;
    public int def;
    public int speed;

    /// <summary> 타입 상성 배율 </summary>
    const float h = 0.5f; // @ 하프
    const float n = 1f;   // @ 노말
    const float g = 2f;   // @ 굿

    /// <summary> 포즈 및 아틀라스 로딩 보조 필드 </summary>
    public string atlasResourcePath = "";
    public string spriteKeyChoice = "";
    public string spriteKeyBattleIdle = "";

    // @ 신규 명칭 매핑
    public string spriteKeyAtk = "";
    public string spriteKeyDef = "";
    public string spriteKeyHp = "";

    /// <summary> 스킬 이름 4슬롯 </summary>
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

        // @ 기본 매핑 @ 자식에서 덮어씀 가능
        skillTypeBehaviours[0] = new MeleeAttackType();
        skillTypeBehaviours[1] = new RangedAttackType();
        skillTypeBehaviours[2] = new DefenseType();
        skillTypeBehaviours[3] = new HealType();

        // @ 기본 개체값
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
    /// @ 전투 1행동 코루틴
    /// @ skillIndex < 0 = 일반공격, skillIndex >= 0 = 스킬
    /// </summary>
    public IEnumerator Attack(Pokemon other, int skillIndex)
    {
        // @ 스킬 모델 로딩
        SkillTpye model = null;
        if (skillTypeBehaviours != null)
        {
            if (skillIndex >= 0)
            {
                if (skillIndex < skillTypeBehaviours.Length)
                {
                    model = skillTypeBehaviours[skillIndex];
                }
            }
        }

        // @ 로그용 이름
        if (PokemonBattleManager.instance != null)
        {
            if (PokemonBattleManager.instance.textLog != null)
            {
                string dispName = (skillIndex < 0) ? "공격" : "스킬";
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

        // @ 상성 배율
        float typeMul = battleType[(int)type, (int)other.type];

        // @ 효과 로그
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

        // @ 데미지 계산
        int finalDamage = 0;
        bool isNormal = (skillIndex < 0);

        if (isNormal)
        {
            // 1) 일반공격: (atk - def) * 배율
            float baseRaw = (float)atk - (float)other.def;
            baseRaw = (baseRaw < 1f) ? 1f : baseRaw;
            float dmgF = baseRaw * typeMul;
            dmgF = (dmgF <= 0f) ? 1f : dmgF;
            finalDamage = (int)dmgF;
        }
        else
        {
            // 2,3) 스킬은 모델에서 처리
            if (model != null)
            {
                finalDamage = model.ComputeDamageOverride(this, other, 0);
            }
            else
            {
                // @ 모델이 없으면 일반공격 공식으로 폴백
                float baseRaw = (float)atk - (float)other.def;
                baseRaw = (baseRaw < 1f) ? 1f : baseRaw;
                float dmgF = baseRaw * typeMul;
                dmgF = (dmgF <= 0f) ? 1f : dmgF;
                finalDamage = (int)dmgF;
            }
        }

        // @ 피해 적용
        other.Hp = other.Hp - finalDamage;
        other.Hp = (other.Hp <= 0) ? 0 : other.Hp;

        // @ 4) 일반공격 반격: 20% 확률로 입힌 피해의 10% 즉시 반사
        if (isNormal)
        {
            float r = Random.value;
            if (r <= 0.2f)
            {
                int counterDmg = (int)((float)finalDamage * 0.1f);
                counterDmg = (counterDmg < 1) ? 1 : counterDmg;
                Hp = Hp - counterDmg;
                Hp = (Hp <= 0) ? 0 : Hp;

                if (PokemonBattleManager.instance != null)
                {
                    if (PokemonBattleManager.instance.textLog != null)
                    {
                        PokemonBattleManager.instance.textLog.text = "반격 발생 @ " + name + " 이 " + counterDmg.ToString() + " 피해를 받았다";
                    }
                }
            }
        }

        yield return new WaitForSeconds(1f);
    }
}