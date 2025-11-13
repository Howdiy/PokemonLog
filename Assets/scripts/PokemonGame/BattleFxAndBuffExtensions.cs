/// <summary>
/// PokemonBattleManager와 Pokemon 간의 전투 FX 및 버프 확장 메서드 모음.
/// </summary>
public static class BattleFxAndBuffExtensions
{
    /// <summary>
    /// 'durationTurns'가 명시된 방어력 상승 버프를 적용한다.
    /// durationTurns는 추후 규칙 확정 시 내부 스택/상태로 확장된다.
    /// </summary>
    public static void ApplyDefenseBuff(this PokemonBattleManager bm, Pokemon target, int amount, int durationTurns)
    {
        if (bm == null) { return; }
        if (target == null) { return; }

        // PokemonBattleManager 내부 런타임 상태에 버프를 등록하고 수치를 반영한다.
        bm.ApplyDefenseBuffRuntime(target, amount, durationTurns);

        // 로그 출력
        if (PokemonBattleManager.instance != null)
        {
            if (PokemonBattleManager.instance.textLog != null)
            {
                PokemonBattleManager.instance.textLog.text = target.name + "의 방어가 " + amount + " 상승했다.";
            }
        }
    }

    /// <summary>
    /// 하위 코드 호환용(지속 턴 기본 3) 기본 오버로드.
    /// </summary>
    public static void ApplyDefenseBuff(this PokemonBattleManager bm, Pokemon target, int amount)
    {
        bm.ApplyDefenseBuff(target, amount, 3);
    }
}
