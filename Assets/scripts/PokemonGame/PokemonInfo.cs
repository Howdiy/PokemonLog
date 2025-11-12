using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.U2D;

/// <summary>
/// @ 배틀 씬에서 포켓몬 표시와 연출을 담당
/// </summary>
public class PokemonInfo : MonoBehaviour
{
    public Image image;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI hpText;

    public PokemonBattleManager BattleGameManager;
    // 아틀라스에 저장된 이미지를 상황별로 분리
    public Sprite choiceSprite;
    public Sprite battleIdleSprite;
    public Sprite attackPoseSprite;
    public Sprite skillPoseSprite;

    public Pokemon targetPokemon;
    // 스프라이트 아틀라스 불러오기
    private SpriteAtlas _atlas;
    private bool _isMoving = false;

    private void Awake()
    {
        LoadAtlasIfNeeded();
        ApplyBattleIdlePose();
        ApplyNameAndHp();
    }

    private void Update()
    {
        ApplyNameAndHp();
    }

    /// <summary> 아틀라스 로드 </summary>
    private void LoadAtlasIfNeeded()
    {
        if (_atlas != null) { return; }
        if (targetPokemon == null) { return; }
        if (string.IsNullOrEmpty(targetPokemon.atlasResourcePath)) { return; }
        _atlas = Resources.Load<SpriteAtlas>(targetPokemon.atlasResourcePath);
    }

    /// <summary> '이름, HP'텍스트 반영 </summary>
    private void ApplyNameAndHp()
    {
        if (targetPokemon == null) { return; }
        if (nameText != null) { nameText.text = targetPokemon.name; }
        if (hpText != null) { hpText.text = "HP " + targetPokemon.Hp.ToString(); }
    }

    /// <summary> 배틀 대기 포즈 </summary>
    public void ApplyBattleIdlePose()
    {
        LoadAtlasIfNeeded();
        SetSpriteByKey(targetPokemon != null ? targetPokemon.spriteKeyBattleIdle : "", battleIdleSprite);
    }

    /// <summary> 공격 포즈 </summary>
    public void ApplyAttackPose()
    {
        LoadAtlasIfNeeded();
        SetSpriteByKey(targetPokemon != null ? targetPokemon.spriteKeyAttack : "", attackPoseSprite);
    }

    /// <summary> 스킬 포즈 </summary>
    public void ApplySkillPose()
    {
        LoadAtlasIfNeeded();
        SetSpriteByKey(targetPokemon != null ? targetPokemon.spriteKeySkill : "", skillPoseSprite);
    }

    /// <summary> 키에 맞는 스프라이트 세팅 </summary>
    private void SetSpriteByKey(string spriteKey, Sprite fallback)
    {
        if (_atlas != null)
        {
            if (!string.IsNullOrEmpty(spriteKey))
            {
                Sprite sp = _atlas.GetSprite(spriteKey);
                if (sp != null)
                {
                    if (image != null) { image.sprite = sp; }
                    return;
                }
            }
        }
        if (fallback != null)
        {
            if (image != null) { image.sprite = fallback; }
        }
    }

    /// <summary> 자신의 스프라이트를 타겟의 2/3 지점까지 왕복 이동 </summary>
    private IEnumerator DashToTwoThirdAndBack(PokemonInfo target)
    {
        if (_isMoving) { yield break; }
        _isMoving = true;

        Transform myTr = (image != null) ? image.transform : this.transform;
        Transform tgTr = (target != null && target.image != null) ? target.image.transform : null;

        Vector3 startPos = myTr.position;
        Vector3 targetPos = startPos;

        if (tgTr != null)
        {
            Vector3 dir = tgTr.position - startPos;
            targetPos = startPos + dir * 0.66f;
        }

        float t = 0f;
        while (t < 1f)
        {
            t = t + Time.deltaTime * 2f;
            myTr.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }

        while (t > 0f)
        {
            t = t - Time.deltaTime * 2f;
            myTr.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }

        _isMoving = false;
    }

    /// <summary> 프리팹을 특정 위치에 1회 재생 후 파괴 </summary>
    private IEnumerator SpawnFxOnce(GameObject prefab, Vector3 pos, Transform parent)
    {
        if (prefab == null) { yield break; }
        GameObject fx = GameObject.Instantiate(prefab, pos, Quaternion.identity, parent);
        yield return new WaitForSeconds(0.6f);
        if (fx != null)
        {
            GameObject.Destroy(fx);
        }
    }

    /// 연출 시퀀스

    // 일반 공격: 2/3 지점 왕복 + 수비쪽 위치에서 FX 한번만
    public IEnumerator NormalAttackSequence(PokemonInfo target, GameObject normalFx)
    {
        yield return StartCoroutine(DashToTwoThirdAndBack(target));

        if (target != null)
        {
            Transform tgTr = (target.image != null) ? target.image.transform : target.transform;
            Vector3 pos = tgTr.position;
            Transform parent = tgTr;
            yield return StartCoroutine(SpawnFxOnce(normalFx, pos, parent));
        }
    }

    // 근접 스킬: 2/3 지점 왕복 + 타겟 위치 FX 1회(추후 확장)
    public IEnumerator MeleeSkillSequence(PokemonInfo target, GameObject fx)
    {
        yield return StartCoroutine(DashToTwoThirdAndBack(target));

        if (target != null)
        {
            Transform tgTr = (target.image != null) ? target.image.transform : target.transform;
            Vector3 pos = tgTr.position;
            Transform parent = tgTr;
            yield return StartCoroutine(SpawnFxOnce(fx, pos, parent));
        }
    }

    // 원거리 스킬: 현재는 타겟 위치 FX 1회(추후 이동 연출 확장)
    public IEnumerator RangedSkillSequence(PokemonInfo target, GameObject fx)
    {
        if (target != null)
        {
            Transform tgTr = (target.image != null) ? target.image.transform : target.transform;
            Vector3 pos = tgTr.position;
            Transform parent = tgTr;
            yield return StartCoroutine(SpawnFxOnce(fx, pos, parent));
        }
    }

    // 회복 스킬: 자신의 위치에서 FX 한번만
    public IEnumerator HealSkillSequence(GameObject fx)
    {
        Transform myTr = (image != null) ? image.transform : this.transform;
        Vector3 pos = myTr.position;
        Transform parent = myTr;
        yield return StartCoroutine(SpawnFxOnce(fx, pos, parent));
    }

    // 방어 스킬: 자신의 위치에서 FX 한번만
    public IEnumerator DefenseSkillSequence(GameObject fx)
    {
        Transform myTr = (image != null) ? image.transform : this.transform;
        Vector3 pos = myTr.position;
        Transform parent = myTr;
        yield return StartCoroutine(SpawnFxOnce(fx, pos, parent));
    }
}
