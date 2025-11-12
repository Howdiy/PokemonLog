using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.U2D;

/// <summary>
/// @ 전투 화면에서 포켓몬 1개에 대응되는 UI/스프라이트 제어
/// </summary>
public class PokemonInfo : MonoBehaviour
{
    public Image image;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI hpText;

    public PokemonBattleManager BattleGameManager;

    public Sprite choiceSprite;
    public Sprite battleIdleSprite;
    public Sprite skillSprite;
    public Sprite attackSprite;

    public Pokemon targetPokemon;

    private SpriteAtlas _atlas;
    private string _currentAtlasName;
    private bool _isMoving;

    void Awake()
    {
        if (BattleGameManager == null)
        {
            BattleGameManager = FindObjectOfType<PokemonBattleManager>();
        }
    }

    private void Start()
    {
        ApplyBattleIdlePose();
    }

    // ========= 이동(선형 보간) 유틸 =========
    private IEnumerator MoveImageOnce(Vector3 from, Vector3 to, float dur)
    {
        float t = 0f;
        while (t < 1f)
        {
            t = t + (Time.deltaTime / (dur <= 0f ? 0.0001f : dur));
            if (image != null)
            {
                image.transform.position = Vector3.Lerp(from, to, t);
            }
            yield return null;
        }
    }

    // ========= 전투 연출: 일반공격 =========
    /// <summary>
    /// @ 일반공격 시퀀스
    /// @ 1) 공격자 스프라이트가 수비자까지의 2/3 지점까지 이동
    /// @ 2) 수비자 위치에 NormalAttackFxPerfsb 프리팹 1회 재생 후 삭제
    /// @ 3) 공격자 원위치 복귀
    /// </summary>
    public IEnumerator NormalAttackSequence(PokemonInfo defenderInfo, GameObject normalFx)
    {
        if (_isMoving) { yield break; }
        if (image == null) { yield break; }

        _isMoving = true;

        Vector3 startPos = image.transform.position;
        Vector3 defPos = startPos;
        if (defenderInfo != null)
        {
            if (defenderInfo.image != null)
            {
                defPos = defenderInfo.image.transform.position;
            }
        }

        Vector3 twoThird = startPos + ((defPos - startPos) * 0.6666667f);

        // 1) 2/3 지점까지
        yield return StartCoroutine(MoveImageOnce(startPos, twoThird, 0.2f));

        // 2) 수비자 위치에 FX 1회
        if (normalFx != null)
        {
            Vector3 spawnPos = defPos;
            GameObject fx = GameObject.Instantiate(normalFx, spawnPos, Quaternion.identity);
            fx.transform.SetParent(transform.parent, true);
            GameObject.Destroy(fx, 1.25f);
            yield return new WaitForSeconds(0.25f);
        }

        // 3) 원위치 복귀
        yield return StartCoroutine(MoveImageOnce(twoThird, startPos, 0.2f));

        _isMoving = false;
    }

    // ========= 전투 연출: 근접 스킬 =========
    /// <summary>
    /// @ 근접공격 타입 스킬
    /// @ 1) 공격자 2/3 지점까지 이동
    /// @ 2) 공격자 위치에서 FX 생성 -> 수비자 위치까지 FX 이동 -> 도착 시 1회 재생 후 삭제
    /// @ 3) 공격자 원위치 복귀
    /// </summary>
    public IEnumerator MeleeSkillSequence(PokemonInfo defenderInfo, GameObject effectFx)
    {
        if (_isMoving) { yield break; }
        if (image == null) { yield break; }

        _isMoving = true;

        Vector3 startPos = image.transform.position;
        Vector3 defPos = startPos;
        if (defenderInfo != null)
        {
            if (defenderInfo.image != null)
            {
                defPos = defenderInfo.image.transform.position;
            }
        }

        Vector3 twoThird = startPos + ((defPos - startPos) * 0.6666667f);

        // 1) 2/3 지점까지
        yield return StartCoroutine(MoveImageOnce(startPos, twoThird, 0.2f));

        // 2) FX 이동
        if (effectFx != null)
        {
            GameObject fx = GameObject.Instantiate(effectFx, startPos, Quaternion.identity);
            fx.transform.SetParent(transform.parent, true);

            float t = 0f;
            float dur = 0.25f;
            while (t < 1f)
            {
                t = t + (Time.deltaTime / (dur <= 0f ? 0.0001f : dur));
                fx.transform.position = Vector3.Lerp(startPos, defPos, t);
                yield return null;
            }

            // 도착 후 약간 유지 후 삭제
            yield return new WaitForSeconds(0.15f);
            GameObject.Destroy(fx);
        }

        // 3) 원위치 복귀
        yield return StartCoroutine(MoveImageOnce(twoThird, startPos, 0.2f));

        _isMoving = false;
    }

    // ========= 전투 연출: 원거리 스킬 =========
    /// <summary>
    /// @ 원거리공격 타입 스킬
    /// @ 1) 공격자 위치에서 FX 생성
    /// @ 2) 수비자 위치까지 FX 이동
    /// @ 3) 도착 시 FX 삭제
    /// </summary>
    public IEnumerator RangedSkillSequence(PokemonInfo defenderInfo, GameObject effectFx)
    {
        if (image == null) { yield break; }

        Vector3 startPos = image.transform.position;
        Vector3 defPos = startPos;
        if (defenderInfo != null)
        {
            if (defenderInfo.image != null)
            {
                defPos = defenderInfo.image.transform.position;
            }
        }

        if (effectFx != null)
        {
            GameObject fx = GameObject.Instantiate(effectFx, startPos, Quaternion.identity);
            fx.transform.SetParent(transform.parent, true);

            float t = 0f;
            float dur = 0.25f;
            while (t < 1f)
            {
                t = t + (Time.deltaTime / (dur <= 0f ? 0.0001f : dur));
                fx.transform.position = Vector3.Lerp(startPos, defPos, t);
                yield return null;
            }
            GameObject.Destroy(fx);
        }
        else
        {
            // FX가 없으면 최소 연출 텀만 제공
            yield return new WaitForSeconds(0.25f);
        }
    }

    // ========= 포즈 =========
    public void ApplyBattleIdlePose()
    {
        if (targetPokemon == null) { return; }
        LoadAtlasIfNeeded(targetPokemon.atlasResourcePath);
        SetSpriteFromAtlas(targetPokemon.spriteKeyBattleIdle);

        if (nameText != null) { nameText.text = targetPokemon.name; }
        if (hpText != null) { hpText.text = targetPokemon.Hp.ToString(); }
    }

    public void ApplyAttackPose()
    {
        if (targetPokemon == null) { return; }
        LoadAtlasIfNeeded(targetPokemon.atlasResourcePath);
        SetSpriteFromAtlas(targetPokemon.spriteKeyAttack);
    }

    public void ApplySkillPose()
    {
        if (targetPokemon == null) { return; }
        LoadAtlasIfNeeded(targetPokemon.atlasResourcePath);
        SetSpriteFromAtlas(targetPokemon.spriteKeySkill);
    }

    // ========= 유틸 =========
    private void LoadAtlasIfNeeded(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            _atlas = null;
            _currentAtlasName = string.Empty;
            return;
        }

        if (_atlas != null)
        {
            if (_currentAtlasName == path) { return; }
        }

        _atlas = PokemonSpriteAtlasProvider.GetAtlas(path);
        _currentAtlasName = path;
    }

    private void SetSpriteFromAtlas(string spriteName)
    {
        if (_atlas != null)
        {
            if (!string.IsNullOrEmpty(spriteName))
            {
                Sprite sp = _atlas.GetSprite(spriteName);
                if (sp != null)
                {
                    if (image != null) { image.sprite = sp; }
                    return;
                }
            }
        }
        if (battleIdleSprite != null)
        {
            if (image != null) { image.sprite = battleIdleSprite; }
        }
    }
}
