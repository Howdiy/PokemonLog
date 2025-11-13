using UnityEngine;

/// <summary>
/// @ SkillType 컴포넌트
/// @ 포켓몬별 스킬 타입(근접, 원거리, 회복, 방어) 이펙트 프리팹을 인스펙터에서 지정
/// </summary>
public class SkillType : MonoBehaviour
{
    public static SkillType instance;

    [System.Serializable]
    public class PokemonSkillTypeFxSet
    {
        public GameObject meleeFx;    // @ 근접공격 타입 이펙트
        public GameObject rangedFx;   // @ 원거리공격 타입 이펙트
        public GameObject healFx;     // @ 회복 타입 이펙트
        public GameObject defenseFx;  // @ 방어 타입 이펙트
    }

    [Header("Common FX")]
    public GameObject normalAttackFxPrefab; // @ 일반공격 공통 이펙트

    [Header("Pika FX")]
    public PokemonSkillTypeFxSet pikaFxSet;

    [Header("Paily FX")]
    public PokemonSkillTypeFxSet pailyFxSet;

    [Header("GoBook FX")]
    public PokemonSkillTypeFxSet goBookFxSet;

    [Header("Esang FX")]
    public PokemonSkillTypeFxSet esangFxSet;

    private void Awake()
    {
        if (instance != null)
        {
            if (instance != this)
            {
                Destroy(gameObject);
                return;
            }
        }
        instance = this;
    }

    /// <summary>
    /// @ 일반공격 이펙트 반환
    /// </summary>
    public GameObject GetNormalAttackFx()
    {
        return normalAttackFxPrefab;
    }

    /// <summary>
    /// @ 포켓몬 인덱스 + 스킬 타입 모델로 이펙트 반환
    /// </summary>
    public GameObject GetSkillFxByType(Pokemon.PokemonIndex index, SkillTypeBase behaviour)
    {
        PokemonSkillTypeFxSet set = GetSetByIndex(index);
        if (set == null)
        {
            return null;
        }

        if (behaviour == null)
        {
            return null;
        }

        // @ 타입 매칭
        if (behaviour is MeleeAttackType)
        {
            return set.meleeFx;
        }

        if (behaviour is RangedAttackType)
        {
            return set.rangedFx;
        }

        if (behaviour is HealType)
        {
            return set.healFx;
        }

        if (behaviour is DefenseType)
        {
            return set.defenseFx;
        }

        return null;
    }

    /// <summary>
    /// @ 포켓몬 인덱스 + (슬롯 배열, skillIndex) 기반으로 이펙트 반환
    /// </summary>
    public GameObject GetSkillFx(Pokemon.PokemonIndex index, int skillIndex, SkillTypeBase[] behaviours)
    {
        if (behaviours == null)
        {
            return null;
        }

        if (skillIndex < 0)
        {
            return null;
        }

        if (skillIndex >= behaviours.Length)
        {
            return null;
        }

        SkillTypeBase b = behaviours[skillIndex];
        return GetSkillFxByType(index, b);
    }

    /// <summary>
    /// @ 포켓몬 인덱스에 해당하는 FX 세트 선택
    /// </summary>
    private PokemonSkillTypeFxSet GetSetByIndex(Pokemon.PokemonIndex index)
    {
        if (index == Pokemon.PokemonIndex.pikach)
        {
            return pikaFxSet;
        }

        if (index == Pokemon.PokemonIndex.paily)
        {
            return pailyFxSet;
        }

        if (index == Pokemon.PokemonIndex.goBook)
        {
            return goBookFxSet;
        }

        return esangFxSet;
    }
}

/// <summary>
/// @ 스킬 타입 베이스(정상 철자)
/// </summary>
public abstract class SkillTypeBase
{
    /// <summary>
    /// @ 스킬 타입별 데미지 재계산 훅
    /// </summary>
    public abstract int ComputeDamageOverride(Pokemon self, Pokemon other, int baseDamage);
}

/// <summary>
/// @ 하위 호환용 오탈자 클래스
/// @ 기존 코드의 ': SkillTpye' 상속을 그대로 통과시키기 위한 추상 클래스
/// </summary>
public abstract class SkillTpye : SkillTypeBase
{
}

/// <summary>
/// @ SkillType 확장 메서드(하위 코드 시그니처 호환용)
/// </summary>
public static class SkillTypeExtensions
{
    public static GameObject GetSkillFx(this SkillType reg, Pokemon.PokemonIndex index, SkillTypeBase behaviour)
    {
        if (reg == null)    { return null; }
        return reg.GetSkillFxByType(index, behaviour);
    }

    public static GameObject GetSkillFx(this SkillType reg, Pokemon.PokemonIndex index, int skillIndex, SkillTypeBase[] behaviours)
    {
        if (reg == null)    { return null; }
        return reg.GetSkillFx(index, skillIndex, behaviours);
    }

    public static GameObject GetNormalAttackFx(this SkillType reg)
    {
        if (reg == null)    { return null; }
        return reg.GetNormalAttackFx();
    }
}
