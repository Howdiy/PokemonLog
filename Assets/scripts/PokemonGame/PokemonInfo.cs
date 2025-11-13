using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 스프라이트/이름/HP UI 바인딩 + 공격/스킬 연출(이펙트 생성, 이동, 삭제)
/// 실제 대미지 계산은 Pokemon.Attack()에서 처리
/// </summary>
public class PokemonInfo : MonoBehaviour
{
    [Header("UI")]
    public Image image;
    public TextMeshProUGUI nameText;
    public Slider hpBar;

    [Header("Runtime")]
    public Pokemon targetPokemon;
    public PokemonBattleManager BattleGameManager;

    [Header("Side")]
    public bool isPlayerSide;
    private bool _isMoving = false;

    /// <summary>
    /// 포켓몬 데이터 연결 및 기본 UI 갱신
    /// </summary>
    public void Bind(Pokemon p)
    {
        targetPokemon = p;

        if (nameText != null)
        {
            if (p != null) { nameText.text = p.name; }
            else { nameText.text = ""; }
        }

        if (hpBar != null)
        {
            if (p != null) { hpBar.value = p.Hp; }
            else { hpBar.value = 0; }
        }

        ApplyBattleIdlePose();
        ApplySpriteFromAtlas();
    }

    /// <summary>
    /// 전투 대기 포즈
    /// </summary>
    public void ApplyBattleIdlePose()
    {
        if (image == null) { return; }
        image.transform.localScale = Vector3.one;
    }

    /// <summary>
    /// @ 공격 포즈(간단 스케일 업)
    /// </summary>
    public void ApplyAttackPose()
    {
        if (image == null) { return; }
        image.transform.localScale = new Vector3(1.08f, 1.08f, 1f);
    }

    /// <summary>
    /// @ 아틀라스에서 전투 대기 스프라이트 적용
    /// </summary>
    public void ApplySpriteFromAtlas()
    {
        if (image == null) { return; }
        if (targetPokemon == null) { return; }
        Sprite s = PokemonSpriteAtlasProvider.GetSprite(targetPokemon.atlasResourcePath, ResolveBattleIdleKey());
        if (s != null) { image.sprite = s; }
    }

    private string ResolvePrefix()
    {
        if (targetPokemon == null) { return ""; }
        if (targetPokemon.index == Pokemon.PokemonIndex.pikach) { return "PIKA"; }
        if (targetPokemon.index == Pokemon.PokemonIndex.paily) { return "PAILY"; }
        if (targetPokemon.index == Pokemon.PokemonIndex.goBook) { return "GOBOOK"; }
        if (targetPokemon.index == Pokemon.PokemonIndex.eSang) { return "ESANG"; }
        return "";
    }

    /// <summary>  대기/상점/회복/방어용 아틀라스 키: 적=...1, 플레이어=...2 </summary>
    private string ResolveBattleIdleKey()
    {
        string p = ResolvePrefix();
        if (p == "") { return (targetPokemon != null) ? targetPokemon.spriteKeyBattleIdle : ""; }
        return isPlayerSide ? (p + "2") : (p + "1");
    }

    /// <summary> 근접/원거리/공격용 아틀라스 키: 적=...3, 플레이어=...4 </summary>
    private string ResolveOffenseKey()
    {
        string p = ResolvePrefix();
        if (p == "") { return (targetPokemon != null) ? targetPokemon.spriteKeyAttack : ""; }
        return isPlayerSide ? (p + "4") : (p + "3");
    }

    private void ApplyOffenseSprite()
    {
        if (image == null) { return; }
        if (targetPokemon == null) { return; }
        string key = ResolveOffenseKey();
        if (key == "") { return; }
        Sprite s = PokemonSpriteAtlasProvider.GetSprite(targetPokemon.atlasResourcePath, key);
        if (s != null) { image.sprite = s; }
    }
    
    /// <summary>
    /// 공격 이펙트 시퀀스(이펙트는 SkillType 레지스트리에서 가져옴)
    /// </summary>
    // 일반공격: 2/3 지점까지 이동 -> 피격 위치에서 공용 이펙트 1회 -> 원위치 복귀
    public IEnumerator NormalAttackSequence(PokemonInfo otherInfo)
    {
        if (image == null) { yield break; }
        if (otherInfo == null) { yield break; }
        if (otherInfo.image == null) { yield break; }

        Vector3 startPos = image.transform.position;
        Vector3 targetPos = otherInfo.image.transform.position;
        Vector3 twoThird = Vector3.Lerp(startPos, targetPos, 0.66f);

        yield return StartCoroutine(MoveImageTo(twoThird));
        ApplyOffenseSprite();

        GameObject prefab = null;
        if (SkillType.instance != null) { prefab = SkillType.instance.GetNormalAttackFx(); }
        SpawnOneShotFxAt(prefab, otherInfo.image.transform.position);

        yield return StartCoroutine(MoveImageTo(startPos));
        ApplyBattleIdlePose();
        ApplySpriteFromAtlas();
    }
    // 근접 스킬: 2/3 이동 -> 내 위치 생성 이펙트가 상대 위치로 이동/삭제 -> 복귀
    public IEnumerator MeleeSkillSequence(PokemonInfo otherInfo, int skillIndex)
    {
        if (image == null) { yield break; }
        if (otherInfo == null) { yield break; }
        if (otherInfo.image == null) { yield break; }
        if (targetPokemon == null) { yield break; }

        Vector3 startPos = image.transform.position;
        Vector3 targetPos = otherInfo.image.transform.position;
        Vector3 twoThird = Vector3.Lerp(startPos, targetPos, 0.66f);

        yield return StartCoroutine(MoveImageTo(twoThird));
        ApplyOffenseSprite();

        GameObject prefab = null;
        // behaviours 전달(에러 CS7036 방지용)
        if (SkillType.instance != null) { prefab = SkillType.instance.GetSkillFx(targetPokemon.index, skillIndex, targetPokemon.skillTypeBehaviours); }
        yield return StartCoroutine(SpawnMoveAndDestroyFx(prefab, startPos, targetPos, 0.25f));

        yield return StartCoroutine(MoveImageTo(startPos));
        ApplyBattleIdlePose();
        ApplySpriteFromAtlas();
    }
    // 원거리 스킬: 내 위치에서 생성 -> 상대 위치까지 이동 -> 삭제
    public IEnumerator RangedSkillSequence(PokemonInfo otherInfo, int skillIndex)
    {
        if (image == null) { yield break; }
        if (otherInfo == null) { yield break; }
        if (otherInfo.image == null) { yield break; }
        if (targetPokemon == null) { yield break; }

        Vector3 startPos = image.transform.position;
        Vector3 targetPos = otherInfo.image.transform.position;
        ApplyOffenseSprite();

        GameObject prefab = null;
        if (SkillType.instance != null) { prefab = SkillType.instance.GetSkillFx(targetPokemon.index, skillIndex, targetPokemon.skillTypeBehaviours); }
        yield return StartCoroutine(SpawnMoveAndDestroyFx(prefab, startPos, targetPos, 0.25f));

        ApplyBattleIdlePose();
        ApplySpriteFromAtlas();
    }
    // 회복 스킬: 내 위치에서 1회 생성 -> 잠시 후 삭제
    public IEnumerator HealSkillSequence(int skillIndex)
    {
        if (image == null) { yield break; }
        if (targetPokemon == null) { yield break; }

        GameObject prefab = null;
        if (SkillType.instance != null) { prefab = SkillType.instance.GetSkillFx(targetPokemon.index, skillIndex, targetPokemon.skillTypeBehaviours); }
        SpawnOneShotFxAt(prefab, image.transform.position);
        yield return new WaitForSeconds(0.35f);
        ApplySpriteFromAtlas();
        ApplyBattleIdlePose();
    }
    // 방어 스킬: 내 위치에서 1회 생성 -> 잠시 후 삭제
    public IEnumerator DefenseSkillSequence(int skillIndex)
    {
        if (image == null) { yield break; }
        if (targetPokemon == null) { yield break; }

        GameObject prefab = null;
        if (SkillType.instance != null) { prefab = SkillType.instance.GetSkillFx(targetPokemon.index, skillIndex, targetPokemon.skillTypeBehaviours); }
        SpawnOneShotFxAt(prefab, image.transform.position);
        yield return new WaitForSeconds(0.35f);
        ApplyBattleIdlePose();
        ApplySpriteFromAtlas();
    }

    /// <summary>
    /// 이동/이펙트 유틸
    /// </summary>
    // '이미지'오브젝트 이동
    private IEnumerator MoveImageTo(Vector3 targetPos)
    {
        if (_isMoving) { yield break; }
        _isMoving = true;

        Vector3 startPos = image.transform.position;
        float t = 0f;
        while (t < 1f)
        {
            t = t + Time.deltaTime * 3f;
            image.transform.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }

        _isMoving = false;
    }
    // '공격'타입 스킬 이펙트 생성
    private void SpawnOneShotFxAt(GameObject fxPrefab, Vector3 pos)
    {
        if (fxPrefab == null) { return; }
        GameObject go = GameObject.Instantiate(fxPrefab, pos, Quaternion.identity);
        GameObject.Destroy(go, 1.2f);
    }
    // '공격'타입 스킬 이펙트 이동 및 삭제
    private IEnumerator SpawnMoveAndDestroyFx(GameObject fxPrefab, Vector3 startPos, Vector3 endPos, float lifeAfterArrive)
    {
        if (fxPrefab == null) { yield break; }

        GameObject go = GameObject.Instantiate(fxPrefab, startPos, Quaternion.identity);

        float t = 0f;
        while (t < 1f)
        {
            t = t + Time.deltaTime * 2f;
            go.transform.position = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }

        yield return new WaitForSeconds(lifeAfterArrive);
        GameObject.Destroy(go);
    }
}
