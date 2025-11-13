using UnityEngine;

/// <summary>
/// 전투 중 보정/연출 확장 메서드 모음
/// </summary>
public static class BattleFxAndBuffExtensions
{
    /// <summary>
    /// 방어 증가 버프 적용 @ 즉시 def 가산 + 로그 출력
    /// 'durationTurns'는 이후 규칙 확정시 내부 스택/상태로 확장 가능
    /// </summary>
    public static void ApplyDefenseBuff(this PokemonBattleManager bm, Pokemon target, int amount, int durationTurns)
    {
        if (bm == null) { return; }
        if (target == null) { return; }

        // @ 즉시 수치 반영
        target.def = target.def + amount;

        // @ 로그 출력
        if (PokemonBattleManager.instance != null)
        {
            if (PokemonBattleManager.instance.textLog != null)
            { PokemonBattleManager.instance.textLog.text = target.name + "의 방어가 " + amount + " 상승했다."; }
        }

        // durationTurns 저장/감소 로직은 이후 규칙 확정시 bm 내부 상태로 확장
    }

    /// <summary>
    /// 하위 코드 호환용(지속 턴 기본 3)
    /// 디폴트 오버로드
    /// </summary>
    public static void ApplyDefenseBuff(this PokemonBattleManager bm, Pokemon target, int amount)
    {
        bm.ApplyDefenseBuff(target, amount, 3);
    }
}
