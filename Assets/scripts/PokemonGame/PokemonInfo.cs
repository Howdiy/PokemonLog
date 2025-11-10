using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.U2D;

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

    // ========= 이동 연출 =========
    public void SpriteMove(PokemonInfo otherInfo)
    {
        if (_isMoving) { return; }
        StartCoroutine(ImageMove(otherInfo));
    }

    private IEnumerator ImageMove(PokemonInfo otherInfo)
    {
        if (_isMoving) { yield break; }
        _isMoving = true;

        Vector3 startPos = image.transform.position;
        Vector3 targetPos = startPos;

        if (otherInfo != null)
        {
            if (otherInfo.image != null)
            {
                targetPos = otherInfo.image.transform.position;
            }
        }

        Vector3 originalScale = image.transform.localScale;

        float t = 0f;
        while (t < 1f)
        {
            t = t + Time.deltaTime;
            image.transform.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }

        while (t > 0f)
        {
            t = t - Time.deltaTime;
            image.transform.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }

        image.transform.localScale = originalScale;
        _isMoving = false;
    }

    // ========= 포즈 적용 =========
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
        SetSpriteFromAtlas(targetPokemon.spriteKeyAtk);
    }

    public void ApplySkillPose()
    {
        if (targetPokemon == null) { return; }
        LoadAtlasIfNeeded(targetPokemon.atlasResourcePath);
        SetSpriteFromAtlas(targetPokemon.spriteKeyDef);
    }

    // ========= 아틀라스 =========
    private void LoadAtlasIfNeeded(string path)
    {
        if (_atlas != null) { return; }
        if (string.IsNullOrEmpty(path)) { return; }
        _atlas = Resources.Load<SpriteAtlas>(path);
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
