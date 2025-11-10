using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.U2D;
using UnityEngine.UI;
using TMPro;

public class PokemonBattleManager : MonoBehaviour
{
    // @ 싱글톤
    public static PokemonBattleManager instance;
    public static PokemonBattleManager Instance { get; private set; }

    // @ 설정
    public Setting settingsRef;

    // @ 전투 UI
    public PokemonInfo myInfo;
    public PokemonInfo otherInfo;

    public Pokemon myPokemonB;
    public Pokemon otherPokemonB;

    public TextMeshProUGUI textLog;
    public TextMeshProUGUI roundText;

    // Command : 0-Attack, 1-BAG, 2-BattleSkill, 3-PokemonList
    public Button[] commandBts;
    public Button[] skill1_4;

    // 상점
    public GameObject ShopPanel;
    public Button[] shopItemButtons;

    // 교체
    public GameObject switchPanel;

    // 내부 상태
    private int roundIndex = 1;
    private bool isPlayerTurn = true;

    // =========================================================
    // 라이프사이클
    // =========================================================
    private void Awake()
    {
        if (Instance != null)
        {
            if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }
        }
        Instance = this;
        instance = this;
        myPokemonB = PokemonGamemanager.SelectAvailablePokemon(true, true);
        otherPokemonB = PokemonGamemanager.SelectAvailablePokemon(false, true);
        if (myPokemonB == null || otherPokemonB == null)
        {
            if (textLog != null)
            {
                textLog.text = "  ϸ մϴ.";
                textLog.gameObject.SetActive(true);
            }
            return;
        }

        // @ PokemonChoices → Battle 전환 시 전달 포인터 보강
        if (myPokemonB == null)
        {
            if (PokemonGamemanager.myPokemonG != null)
            {
                myPokemonB = PokemonGamemanager.myPokemonG;
            }
        }
        if (otherPokemonB == null)
        {
            if (PokemonGamemanager.otherPokemonG != null)
            {
                otherPokemonB = PokemonGamemanager.otherPokemonG;
            }
        }


        if (myInfo != null) { myInfo.BattleGameManager = this; }
        if (otherInfo != null) { otherInfo.BattleGameManager = this; }
        if (roundText != null)
        {
            roundText.text = "Round " + roundIndex.ToString();
        }

        if (myInfo != null)
        {
            myInfo.targetPokemon = myPokemonB;
            if (myPokemonB != null) { myPokemonB.info = myInfo; }
            myInfo.ApplyBattleIdlePose();
        }
        if (otherInfo != null)
        {
            otherInfo.targetPokemon = otherPokemonB;
            if (otherPokemonB != null) { otherPokemonB.info = otherInfo; }
            otherInfo.ApplyBattleIdlePose();
        }

        DecideFirstTurn();
        ShowFirstTurnLog();
        BindCommandButtons();
        BindSkillButtons();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
            instance = null;
        }
    }

    // =========================================================
    // 선제공격
    // =========================================================
    private void DecideFirstTurn()
    {
        if (myPokemonB == null) { isPlayerTurn = true; return; }
        if (otherPokemonB == null) { isPlayerTurn = true; return; }

        if (myPokemonB.speed > otherPokemonB.speed) { isPlayerTurn = true; }
        else
        {
            if (myPokemonB.speed < otherPokemonB.speed) { isPlayerTurn = false; }
            else
            {
                isPlayerTurn = ((roundIndex % 2) == 1) ? true : false;
            }
        }
    }

    private void ShowFirstTurnLog()
    {
        if (textLog == null)
        {
            return;
        }
        string msg = isPlayerTurn ? "선제공격 판정 성공!" : "선제공격 판정 실패";
        textLog.text = msg;
        textLog.gameObject.SetActive(true);
        StartCoroutine(HideLogAfter(0.75f));
    }

    private IEnumerator HideLogAfter(float sec)
    {
        yield return new WaitForSeconds(sec);
        if (textLog != null)
        {
            textLog.gameObject.SetActive(false);
        }
    }

    // =========================================================
    // 버튼 바인딩
    // =========================================================
    private void BindCommandButtons()
    {
        if (commandBts == null)
        {
            return;
        }

        if (commandBts.Length > 0)
        {
            if (commandBts[0] != null)
            {
                commandBts[0].onClick.RemoveAllListeners();
                commandBts[0].onClick.AddListener(OnClickAttackCommand);
            }
        }
        if (commandBts.Length > 1)
        {
            if (commandBts[1] != null)
            {
                commandBts[1].onClick.RemoveAllListeners();
                commandBts[1].onClick.AddListener(OpenShop);
            }
        }
        if (commandBts.Length > 2)
        {
            if (commandBts[2] != null)
            {
                commandBts[2].onClick.RemoveAllListeners();
                commandBts[2].onClick.AddListener(OpenSkillPanel);
            }
        }
        if (commandBts.Length > 3)
        {
            if (commandBts[3] != null)
            {
                commandBts[3].onClick.RemoveAllListeners();
                commandBts[3].onClick.AddListener(OpenSwitch);
            }
        }
    }

    private void BindSkillButtons()
    {
        if (skill1_4 == null)
        {
            return;
        }

        if (skill1_4.Length > 0)
        {
            if (skill1_4[0] != null)
            {
                skill1_4[0].onClick.RemoveAllListeners();
                skill1_4[0].onClick.AddListener(OnClickSkill0);
            }
        }
        if (skill1_4.Length > 1)
        {
            if (skill1_4[1] != null)
            {
                skill1_4[1].onClick.RemoveAllListeners();
                skill1_4[1].onClick.AddListener(OnClickSkill1);
            }
        }
        if (skill1_4.Length > 2)
        {
            if (skill1_4[2] != null)
            {
                skill1_4[2].onClick.RemoveAllListeners();
                skill1_4[2].onClick.AddListener(OnClickSkill2);
            }
        }
        if (skill1_4.Length > 3)
        {
            if (skill1_4[3] != null)
            {
                skill1_4[3].onClick.RemoveAllListeners();
                skill1_4[3].onClick.AddListener(OnClickSkill3);
            }
        }
    }

    // =========================================================
    // 커맨드 동작
    // =========================================================
    public void OnClickAttackCommand()
    {
        if (!isPlayerTurn)
        {
            return;
        }

        if (textLog != null)
        {
            if (myPokemonB != null)
            {
                textLog.text = myPokemonB.name + "이 공격하였다.";
                textLog.gameObject.SetActive(true);
            }
        }

        StartCoroutine(PerformAttack(myPokemonB, otherPokemonB, -1)); // -1: 일반공격
    }

    public void OpenSkillPanel()
    {
        SetSkillButtonsActive(true);
    }

    public void OpenSwitch()
    {
        if (switchPanel != null)
        {
            switchPanel.SetActive(true);
        }
    }

    // 스킬 버튼 핸들러 0~3
    public void OnClickSkill0() { OnSkillClick(0); }
    public void OnClickSkill1() { OnSkillClick(1); }
    public void OnClickSkill2() { OnSkillClick(2); }
    public void OnClickSkill3() { OnSkillClick(3); }

    private void OnSkillClick(int skillIdx)
    {
        if (!isPlayerTurn)
        {
            return;
        }

        if (textLog != null)
        {
            if (myPokemonB != null)
            {
                string sName = "스킬";
                if (myPokemonB.skillNames != null)
                {
                    if (skillIdx >= 0)
                    {
                        if (skillIdx < myPokemonB.skillNames.Length)
                        {
                            sName = myPokemonB.skillNames[skillIdx];
                        }
                    }
                }
                textLog.text = myPokemonB.name + "이 " + sName + "을 사용하였다.";
                textLog.gameObject.SetActive(true);
            }
        }

        SetSkillButtonsActive(false);
        StartCoroutine(PerformAttack(myPokemonB, otherPokemonB, skillIdx));
    }

    private void SetSkillButtonsActive(bool b)
    {
        if (skill1_4 == null)
        {
            return;
        }
        for (int i = 0; i < skill1_4.Length; i++)
        {
            if (skill1_4[i] != null)
            {
                skill1_4[i].gameObject.SetActive(b);
            }
        }
        if (!b)
        {
            if (textLog != null)
            {
                textLog.gameObject.SetActive(true);
            }
        }
    }

    // =========================================================
    // 전투 실행
    // =========================================================
    private IEnumerator PerformAttack(Pokemon attacker, Pokemon defender, int skillIndex)
    {
        if (attacker == null) { yield break; }
        if (defender == null) { yield break; }

        // @ 이동 및 포즈 연출을 매니저에서 확실히 트리거
        if (attacker.info != null)
        {
            attacker.info.ApplyAttackPose();
            attacker.info.SpriteMove(defender != null ? defender.info : null);
        }

        // @ 실제 타격 로직은 Pokemon.Attack 에서 처리
        yield return StartCoroutine(attacker.Attack(defender, skillIndex));

        // @ 상대 리액션 포즈 가볍게 사용 후 원위치
        if (defender != null)
        {
            if (defender.info != null)
            {
                defender.info.ApplySkillPose();
            }
        }
        yield return new WaitForSeconds(0.25f);

        if (attacker.info != null)
        {
            attacker.info.ApplyBattleIdlePose();
        }
        if (defender != null)
        {
            if (defender.info != null)
            {
                defender.info.ApplyBattleIdlePose();
            }
        }

        // @ 로그 잠시 보여준 뒤 숨김
        yield return new WaitForSeconds(0.35f);
        if (textLog != null)
        {
            textLog.gameObject.SetActive(false);
        }

        // @ 기절 체크 및 교체
        yield return StartCoroutine(CheckAndResolveFaintStates());

        // @ 턴 전환
        if (isPlayerTurn)
        {
            isPlayerTurn = false;
        }
        else
        {
            isPlayerTurn = true;
        }

        // @ 적 행동
        if (!isPlayerTurn)
        {
            yield return StartCoroutine(EnemyActOnceThenPass());
        }
    }

    private IEnumerator CheckAndResolveFaintStates()
    {
        if (myPokemonB != null)
        {
            if (myPokemonB.Hp <= 0)
            {
                yield return StartCoroutine(DoSwitchPlayer());
            }
        }
        if (otherPokemonB != null)
        {
            if (otherPokemonB.Hp <= 0)
            {
                yield return StartCoroutine(DoSwitchEnemy());
            }
        }
    }

    // =========================================================
    // 적 턴 처리
    // =========================================================
    private IEnumerator EnemyActOnceThenPass()
    {
        if (otherPokemonB == null || myPokemonB == null)
        {
            yield break;
        }

        yield return new WaitForSeconds(0.5f);

        int r = Random.Range(0, 10);
        if (r < 7)
        {
            int s = Random.Range(0, 4);

            if (textLog != null)
            {
                if (otherPokemonB != null)
                {
                    string sName = "스킬";
                    if (otherPokemonB.skillNames != null)
                    {
                        if (s >= 0)
                        {
                            if (s < otherPokemonB.skillNames.Length)
                            {
                                sName = otherPokemonB.skillNames[s];
                            }
                        }
                    }
                    textLog.text = "적의 " + otherPokemonB.name + "이 " + sName + "을 사용하였다.";
                    textLog.gameObject.SetActive(true);
                }
            }
            StartCoroutine(PerformAttack(otherPokemonB, myPokemonB, s));
        }
        else
        {
            if (textLog != null)
            {
                if (otherPokemonB != null)
                {
                    textLog.text = "적의 " + otherPokemonB.name + "이 공격하였다.";
                    textLog.gameObject.SetActive(true);
                }
            }
            StartCoroutine(PerformAttack(otherPokemonB, myPokemonB, -1));
        }
        yield return null;
    }

    // =========================================================
    // 교체(샘플)
    // =========================================================
    private IEnumerator DoSwitchPlayer()
    {
        Pokemon newP = PokemonGamemanager.SelectAvailablePokemon(true, false);
        myPokemonB = newP;

        if (myInfo != null)
        {
            myInfo.targetPokemon = myPokemonB;
            if (myPokemonB != null)
            {
                myPokemonB.info = myInfo;
                myInfo.ApplyBattleIdlePose();
            }
        }

        if (newP == null)
        {
            if (textLog != null)
            {
                textLog.text = " ̻   ϸ .";
                textLog.gameObject.SetActive(true);
            }
        }
        yield return null;
    }

    private IEnumerator DoSwitchEnemy()
    {
        Pokemon newE = PokemonGamemanager.SelectAvailablePokemon(false, false);
        otherPokemonB = newE;

        if (otherInfo != null)
        {
            otherInfo.targetPokemon = otherPokemonB;
            if (otherPokemonB != null)
            {
                otherPokemonB.info = otherInfo;
                otherInfo.ApplyBattleIdlePose();
            }
        }

        if (newE == null)
        {
            if (textLog != null)
            {
                textLog.text = "밡  ̻  ϸ .";
                textLog.gameObject.SetActive(true);
            }
        }
        yield return null;
    }

    // =========================================================
    // 상점
    // =========================================================
    private enum ShopKind { HealHP, BuffATK, BuffDEF, BuffSPD }
    private struct ShopItem
    {
        public ShopKind kind;
        public int value;
        public string label;
    }
    private List<ShopItem> _currentShopItems = new List<ShopItem>();

    private void OpenShop()
    {
        if (!isPlayerTurn)
        {
            StartCoroutine(EnemyActOnceThenPass());
            return;
        }

        if (ShopPanel == null)
        {
            StartCoroutine(EnemyActOnceThenPass());
            return;
        }
        if (shopItemButtons == null)
        {
            StartCoroutine(EnemyActOnceThenPass());
            return;
        }

        _currentShopItems = BuildRandom5Items();
        ShopPanel.SetActive(true);

        for (int i = 0; i < shopItemButtons.Length; i++)
        {
            if (shopItemButtons[i] == null)
            {
                continue;
            }
            TextMeshProUGUI t = shopItemButtons[i].GetComponentInChildren<TextMeshProUGUI>();
            if (t != null)
            {
                if (i < _currentShopItems.Count)
                {
                    t.text = _currentShopItems[i].label;
                }
            }
            shopItemButtons[i].onClick.RemoveAllListeners();
        }
        if (shopItemButtons.Length > 0)
        {
            if (shopItemButtons[0] != null) { shopItemButtons[0].onClick.AddListener(OnClickShop0); }
        }
        if (shopItemButtons.Length > 1)
        {
            if (shopItemButtons[1] != null) { shopItemButtons[1].onClick.AddListener(OnClickShop1); }
        }
        if (shopItemButtons.Length > 2)
        {
            if (shopItemButtons[2] != null) { shopItemButtons[2].onClick.AddListener(OnClickShop2); }
        }
        if (shopItemButtons.Length > 3)
        {
            if (shopItemButtons[3] != null) { shopItemButtons[3].onClick.AddListener(OnClickShop3); }
        }
        if (shopItemButtons.Length > 4)
        {
            if (shopItemButtons[4] != null) { shopItemButtons[4].onClick.AddListener(OnClickShop4); }
        }
    }

    public void OnClickShop0() { UseShopItemIndex(0); }
    public void OnClickShop1() { UseShopItemIndex(1); }
    public void OnClickShop2() { UseShopItemIndex(2); }
    public void OnClickShop3() { UseShopItemIndex(3); }
    public void OnClickShop4() { UseShopItemIndex(4); }

    private void UseShopItemIndex(int idx)
    {
        if (idx < 0) { return; }
        if (idx >= _currentShopItems.Count) { return; }

        ApplyItem(myPokemonB, _currentShopItems[idx]);

        if (ShopPanel != null) { ShopPanel.SetActive(false); }
        StartCoroutine(EnemyActOnceThenPass());
    }

    private List<ShopItem> BuildRandom5Items()
    {
        List<ShopItem> list = new List<ShopItem>();
        for (int i = 0; i < 5; i++)
        {
            ShopItem it = RandomShopItem();
            list.Add(it);
        }
        return list;
    }

    private ShopItem RandomShopItem()
    {
        int r = Random.Range(0, 4);
        ShopItem it = new ShopItem();

        if (r == 0) { it.kind = ShopKind.HealHP; it.value = Random.Range(10, 31); it.label = "HP 회복 +" + it.value; }
        else
        {
            if (r == 1) { it.kind = ShopKind.BuffATK; it.value = Random.Range(2, 7); it.label = "공격 +" + it.value; }
            else
            {
                if (r == 2) { it.kind = ShopKind.BuffDEF; it.value = Random.Range(2, 7); it.label = "방어 +" + it.value; }
                else { it.kind = ShopKind.BuffSPD; it.value = Random.Range(2, 7); it.label = "속도 +" + it.value; }
            }
        }
        return it;
    }

    private void ApplyItem(Pokemon p, ShopItem it)
    {
        if (p == null) { return; }

        if (it.kind == ShopKind.HealHP) { p.Hp = p.Hp + it.value; }
        else
        {
            if (it.kind == ShopKind.BuffATK) { p.atk = p.atk + it.value; }
            else
            {
                if (it.kind == ShopKind.BuffDEF) { p.def = p.def + it.value; }
                else { p.speed = p.speed + it.value; }
            }
        }

        if (textLog != null)
        {
            bool isEnemy = false;
            if (p == otherPokemonB) { isEnemy = true; }
            string prefix = isEnemy ? "적의 " : "";
            textLog.text = prefix + p.name + "이 " + it.label + "을 사용하였다.";
            textLog.gameObject.SetActive(true);
        }
    }

    // =========================================================
    // 유틸
    // =========================================================
    public static Pokemon CreateByIndexShared(Pokemon.PokemonIndex idx, bool isPlayerSide)
    {
        Pokemon p = null;
        if (idx == Pokemon.PokemonIndex.pikach) { p = new Pika(); }
        else
        {
            if (idx == Pokemon.PokemonIndex.paily) { p = new Paily(); }
            else
            {
                if (idx == Pokemon.PokemonIndex.goBook) { p = new GoBook(); }
                else { p = new Esang(); }
            }
        }
        if (p == null) { return null; }
        p.index = idx;
        ConfigureAtlasForSide(p, isPlayerSide);
        return p;
    }

    public static void ConfigureAtlasForSide(Pokemon p, bool isPlayerSide)
    {
        if (p == null) { return; }

        if (p.index == Pokemon.PokemonIndex.pikach)
        {
            p.atlasResourcePath = "PikaSpriteAtlas";
            p.spriteKeyChoice = "PIKACH";
            p.spriteKeyBattleIdle = "PIKA2";
            p.spriteKeyAtk = "PIKA4";
            p.spriteKeyDef = "PIKA3";
            p.spriteKeyHp = "PIKA1";
        }
        else
        {
            if (p.index == Pokemon.PokemonIndex.paily)
            {
                p.atlasResourcePath = "PailySpriteAtlas";
                p.spriteKeyChoice = "PAILY";
                p.spriteKeyBattleIdle = "PAILY2";
                p.spriteKeyAtk = "PAILY4";
                p.spriteKeyDef = "PAILY3";
                p.spriteKeyHp = "PAILY1";
            }
            else
            {
                if (p.index == Pokemon.PokemonIndex.goBook)
                {
                    p.atlasResourcePath = "GoBookSpriteAtlas";
                    p.spriteKeyChoice = "GOBOOK1";
                    p.spriteKeyBattleIdle = "GOBOOK2";
                    p.spriteKeyAtk = "GOBOOK4";
                    p.spriteKeyDef = "GOBOOK3";
                    p.spriteKeyHp = "GOBOOK1";
                }
                else
                {
                    p.atlasResourcePath = "EsangSpriteAtlas";
                    p.spriteKeyChoice = "ESANGSEE";
                    p.spriteKeyBattleIdle = "ESANG2";
                    p.spriteKeyAtk = "ESANG4";
                    p.spriteKeyDef = "ESANG3";
                    p.spriteKeyHp = "ESANG1";
                }
            }
        }
    }

    private IEnumerator StartNextRoundFlow()
    {
        roundIndex = roundIndex + 1;
        if (roundText != null) { roundText.text = "Round " + roundIndex.ToString(); }
        DecideFirstTurn();
        ShowFirstTurnLog();
        yield return null;
    }

    public static int GetRoundSnapshot()
    {
        if (Instance == null) { return 1; }
        return Instance.roundIndex;
    }

    public static void SetRoundFromSave(int r)
    {
        if (Instance == null) { return; }
        int nr = (r < 1) ? 1 : r;
        Instance.roundIndex = nr;

        if (Instance.roundText != null)
        {
            Instance.roundText.text = "Round " + Instance.roundIndex.ToString();
        }
    }
}