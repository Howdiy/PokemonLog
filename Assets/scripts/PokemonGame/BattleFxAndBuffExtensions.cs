using UnityEngine;

/// <summary>
/// 배틀 보조 효과 및 버프 관련 확장 메서드
/// </summary>
public static class BattleFxAndBuffExtensions
{
    private const int DefaultDefenseBuffDurationTurns = 3;

    /// <summary>
    ///     방어 상승 버프 적용 (지속 턴 지정)
    /// </summary>
    public static void ApplyDefenseBuff(this PokemonBattleManager bm, Pokemon target, int amount, int durationTurns)
    {
        if (bm == null) { return; }
        if (target == null) { return; }

        bm.ApplyDefenseBuffRuntime(target, amount, durationTurns);

        PokemonBattleManager manager = PokemonBattleManager.instance ?? bm;

        if (manager != null)
        {
            if (manager.textLog != null)
            {
                manager.textLog.text = target.name + "의 방어가 " + amount + " 상승했다.";
            }
        }

        // durationTurns 저장/감소 로직은 이후 규칙 확정시 bm 내부 상태로 확장
    }

    /// <summary>
    ///     기본 지속 턴(3턴)으로 방어 상승 버프 적용
    /// </summary>
    public static void ApplyDefenseBuff(this PokemonBattleManager bm, Pokemon target, int amount)
    {
        bm.ApplyDefenseBuff(target, amount, DefaultDefenseBuffDurationTurns);
    }
}
