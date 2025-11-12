using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PokemonBattleManager : MonoBehaviour
{
    // 싱글톤 --------------------------------------------------------------
    public static PokemonBattleManager instance;
    public static PokemonBattleManager Instance { get; private set; }
    private static int _pendingRound = 1;

    // Setting 연동 --------------------------------------------------------
    public GameObject settingsRef;
    public int settingsSortOrder = 5000;
    public Transform uiRoot;

    // 전투 UI -------------------------------------------------------------
    public PokemonInfo myInfo;
    public PokemonInfo otherInfo;

    public Pokemon myPokemonB;
    public Pokemon otherPokemonB;

    public TextMeshProUGUI textLog;
    public TextMeshProUGUI roundText;

    // Command/Skill
    public Button[] commandBts;
    public Button[] skill1_4;

    // 상점/교체
    public GameObject ShopPanel;
    public Button[] shopItemButtons;
    public GameObject switchPanel;

    // 내부 상태 -----------------------------------------------------------
    private int roundIndex = 1;
    private bool isPlayerTurn = true;
    private bool _isInitialized;

    // 라이프사이클 --------------------------------------------------------
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        instance = this;

        _isInitialized = false;
        roundIndex = Mathf.Max(1, _pendingRound);

        InitializeBattleState();
    }

    private void OnDestroy()
    {
        if (Instance == this) { Instance = null; }
        if (instance == this) { instance = null; }
    }

    // 초기화 --------------------------------------------------------------
    private void InitializeBattleState()
    {
        if (_isInitialized) { return; }

        // @ 트레이너 참조 제거: 정적 팀에서 “살아있는” 개체를 고른다
        myPokemonB = PokemonGamemanager.myPokemonG;
        if (myPokemonB == null)
        {
            myPokemonB = FirstAlive(PokemonGamemanager.playerTeam3);
            if (myPokemonB != null)
            {
                PokemonGamemanager.playerActiveIndex = IndexOf(PokemonGamemanager.playerTeam3, myPokemonB);
            }
        }

        otherPokemonB = PokemonGamemanager.otherPokemonG;
        if (otherPokemonB == null)
        {
            otherPokemonB = FirstAlive(PokemonGamemanager.enemyTeam3);
            if (otherPokemonB != null)
            {
                PokemonGamemanager.enemyActiveIndex = IndexOf(PokemonGamemanager.enemyTeam3, otherPokemonB);
            }
        }

        if (myPokemonB == null || otherPokemonB == null)
        {
            if (textLog != null)
            {
                textLog.text = "전투를 시작할 포켓몬이 없습니다.";
                textLog.gameObject.SetActive(true);
            }
            return;
        }

        ConfigureAtlasForSide(myPokemonB, true);
        ConfigureAtlasForSide(otherPokemonB, false);

        if (myInfo != null)
        {
            myInfo.BattleGameManager = this;
            myInfo.targetPokemon = myPokemonB;
            myPokemonB.info = myInfo;
            myInfo.ApplyBattleIdlePose();
        }

        if (otherInfo != null)
        {
            otherInfo.BattleGameManager = this;
            otherInfo.targetPokemon = otherPokemonB;
            otherPokemonB.info = otherInfo;
            otherInfo.ApplyBattleIdlePose();
        }

        if (roundText != null)
        {
            roundText.text = "Round " + roundIndex.ToString();
        }

        DecideFirstTurn();
        ShowFirstTurnLog();
        BindCommandButtons();
        BindSkillButtons();

        _pendingRound = roundIndex;
        _isInitialized = true;
    }

    // 턴 결정 ------------------------------------------------------------
    private void DecideFirstTurn()
    {
        if (myPokemonB == null || otherPokemonB == null)
        {
            isPlayerTurn = true;
            return;
        }

        if (myPokemonB.speed > otherPokemonB.speed)
        {
            isPlayerTurn = true;
            return;
        }
        if (myPokemonB.speed < otherPokemonB.speed)
        {
            isPlayerTurn = false;
            return;
        }
        isPlayerTurn = (roundIndex % 2) == 1 ? true : false;
    }

    private void ShowFirstTurnLog()
    {
        if (textLog == null) { return; }
        string msg = isPlayerTurn ? "선제공격 판정 성공!" : "선제공격 판정 실패";
        textLog.text = msg;
        textLog.gameObject.SetActive(true);
        StartCoroutine(HideLogAfter(0.75f));
    }

    private IEnumerator HideLogAfter(float sec)
    {
        yield return new WaitForSeconds(sec);
        if (textLog != null) { textLog.gameObject.SetActive(false); }
    }

    // 버튼 바인딩 --------------------------------------------------------
    private void BindCommandButtons()
    {
        if (commandBts == null) { return; }

        if (commandBts.Length > 0 && commandBts[0] != null)
        {
            commandBts[0].onClick.RemoveAllListeners();
            commandBts[0].onClick.AddListener(OnClickAttackCommand);
        }
        if (commandBts.Length > 1 && commandBts[1] != null)
        {
            commandBts[1].onClick.RemoveAllListeners();
            commandBts[1].onClick.AddListener(OpenShop);
        }
        if (commandBts.Length > 2 && commandBts[2] != null)
        {
            commandBts[2].onClick.RemoveAllListeners();
            commandBts[2].onClick.AddListener(OpenSkillPanel);
        }
        if (commandBts.Length > 3 && commandBts[3] != null)
        {
            commandBts[3].onClick.RemoveAllListeners();
            commandBts[3].onClick.AddListener(OpenSwitch);
        }
    }

    private void BindSkillButtons()
    {
        if (skill1_4 == null) { return; }

        if (skill1_4.Length > 0 && skill1_4[0] != null)
        {
            skill1_4[0].onClick.RemoveAllListeners();
            skill1_4[0].onClick.AddListener(OnClickSkill0);
        }
        if (skill1_4.Length > 1 && skill1_4[1] != null)
        {
            skill1_4[1].onClick.RemoveAllListeners();
            skill1_4[1].onClick.AddListener(OnClickSkill1);
        }
        if (skill1_4.Length > 2 && skill1_4[2] != null)
        {
            skill1_4[2].onClick.RemoveAllListeners();
            skill1_4[2].onClick.AddListener(OnClickSkill2);
        }
        if (skill1_4.Length > 3 && skill1_4[3] != null)
        {
            skill1_4[3].onClick.RemoveAllListeners();
            skill1_4[3].onClick.AddListener(OnClickSkill3);
        }

        SetSkillButtonsActive(false);
    }

    // 커맨드 -------------------------------------------------------------
    public void OnClickAttackCommand()
    {
        if (!isPlayerTurn) { return; }

        if (textLog != null && myPokemonB != null)
        {
            textLog.text = myPokemonB.name + "이 공격하였다.";
            textLog.gameObject.SetActive(true);
        }

        StartCoroutine(PerformAttack(myPokemonB, otherPokemonB, -1));
    }

    public void OpenSkillPanel()
    {
        if (!isPlayerTurn) { return; }
        SetSkillButtonsActive(true);
    }

    public void OpenSwitch()
    {
        if (switchPanel != null) { switchPanel.SetActive(true); }
    }

    public void OnClickSkill0() { OnSkillClick(0); }
    public void OnClickSkill1() { OnSkillClick(1); }
    public void OnClickSkill2() { OnSkillClick(2); }
    public void OnClickSkill3() { OnSkillClick(3); }

    private void OnSkillClick(int skillIdx)
    {
        if (!isPlayerTurn) { return; }

        if (textLog != null && myPokemonB != null)
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

        SetSkillButtonsActive(false);
        StartCoroutine(PerformAttack(myPokemonB, otherPokemonB, skillIdx));
    }

    private void SetSkillButtonsActive(bool active)
    {
        if (skill1_4 == null) { return; }
        for (int i = 0; i < skill1_4.Length; i++)
        {
            if (skill1_4[i] != null)
            {
                skill1_4[i].gameObject.SetActive(active);
            }
        }
        if (!active && textLog != null) { textLog.gameObject.SetActive(true); }
    }

    // 전투 실행 ----------------------------------------------------------
    private IEnumerator PerformAttack(Pokemon attacker, Pokemon defender, int skillIndex)
    {
        if (attacker == null) { yield break; }
        if (defender == null) { yield break; }

        if (attacker.info != null)
        {
            attacker.info.ApplyAttackPose();
            attacker.info.SpriteMove(defender.info);
        }

        yield return StartCoroutine(attacker.Attack(defender, skillIndex));

        if (defender.info != null) { defender.info.ApplySkillPose(); }
        yield return new WaitForSeconds(0.25f);
        if (attacker.info != null) { attacker.info.ApplyBattleIdlePose(); }
        if (defender.info != null) { defender.info.ApplyBattleIdlePose(); }

        yield return new WaitForSeconds(0.35f);
        if (textLog != null) { textLog.gameObject.SetActive(false); }

        yield return StartCoroutine(CheckAndResolveFaintStates());

        isPlayerTurn = !isPlayerTurn;
        if (!isPlayerTurn)
        {
            yield return StartCoroutine(EnemyActOnceThenPass());
        }
        else
        {
            yield return StartCoroutine(StartNextRoundFlow());
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

    private IEnumerator EnemyActOnceThenPass()
    {
        if (otherPokemonB == null)
        {
            isPlayerTurn = true;
            yield break;
        }

        if (otherPokemonB.Hp <= 0)
        {
            yield return StartCoroutine(DoSwitchEnemy());
            if (otherPokemonB == null)
            {
                isPlayerTurn = true;
                yield break;
            }
        }

        yield return new WaitForSeconds(0.25f);

        bool useSkill = false;
        if (otherPokemonB.skillNames != null)
        {
            if (otherPokemonB.skillNames.Length > 0)
            {
                useSkill = Random.Range(0, 2) == 0 ? true : false;
            }
        }

        if (useSkill)
        {
            int sMax = 4;
            if (otherPokemonB.skillNames != null)
            {
                if (otherPokemonB.skillNames.Length < sMax)
                {
                    sMax = otherPokemonB.skillNames.Length;
                }
            }
            int s = Random.Range(0, sMax);
            if (textLog != null)
            {
                string sName = otherPokemonB.skillNames[s];
                textLog.text = "적의 " + otherPokemonB.name + "이 " + sName + "을 사용하였다.";
                textLog.gameObject.SetActive(true);
            }
            yield return StartCoroutine(PerformAttack(otherPokemonB, myPokemonB, s));
        }
        else
        {
            if (textLog != null)
            {
                textLog.text = "적의 " + otherPokemonB.name + "이 공격하였다.";
                textLog.gameObject.SetActive(true);
            }
            yield return StartCoroutine(PerformAttack(otherPokemonB, myPokemonB, -1));
        }

        isPlayerTurn = true;
    }

    // 교체 ---------------------------------------------------------------
    private IEnumerator DoSwitchPlayer()
    {
        Pokemon replacement = NextAliveAfterIndex(PokemonGamemanager.playerTeam3, PokemonGamemanager.playerActiveIndex);
        myPokemonB = replacement;
        if (myPokemonB != null)
        {
            PokemonGamemanager.playerActiveIndex = IndexOf(PokemonGamemanager.playerTeam3, myPokemonB);
            ConfigureAtlasForSide(myPokemonB, true);
        }

        if (myInfo != null)
        {
            myInfo.targetPokemon = myPokemonB;
            if (myPokemonB != null)
            {
                myPokemonB.info = myInfo;
                myInfo.ApplyBattleIdlePose();
            }
        }

        if (myPokemonB == null && textLog != null)
        {
            textLog.text = "더 이상 사용할 포켓몬이 없습니다.";
            textLog.gameObject.SetActive(true);
        }

        yield return null;
    }

    private IEnumerator DoSwitchEnemy()
    {
        Pokemon replacement = NextAliveAfterIndex(PokemonGamemanager.enemyTeam3, PokemonGamemanager.enemyActiveIndex);
        otherPokemonB = replacement;
        if (otherPokemonB != null)
        {
            PokemonGamemanager.enemyActiveIndex = IndexOf(PokemonGamemanager.enemyTeam3, otherPokemonB);
            ConfigureAtlasForSide(otherPokemonB, false);
        }

        if (otherInfo != null)
        {
            otherInfo.targetPokemon = otherPokemonB;
            if (otherPokemonB != null)
            {
                otherPokemonB.info = otherInfo;
                otherInfo.ApplyBattleIdlePose();
            }
        }

        if (otherPokemonB == null && textLog != null)
        {
            textLog.text = "상대는 더 이상 사용할 포켓몬이 없습니다.";
            textLog.gameObject.SetActive(true);
        }

        yield return null;
    }

    // 상점(간단 유지) ----------------------------------------------------
    private enum ShopKind { HealHP, BuffATK, BuffDEF, BuffSPD }
    private struct ShopItem { public ShopKind kind; public int value; public string label; }
    private readonly List<ShopItem> _currentShopItems = new List<ShopItem>();

    private void OpenShop()
    {
        if (!isPlayerTurn)
        {
            StartCoroutine(EnemyActOnceThenPass());
            return;
        }

        if (ShopPanel == null) { StartCoroutine(EnemyActOnceThenPass()); return; }
        if (shopItemButtons == null) { StartCoroutine(EnemyActOnceThenPass()); return; }

        _currentShopItems.Clear();
        _currentShopItems.AddRange(BuildRandom5Items());
        ShopPanel.SetActive(true);

        for (int i = 0; i < shopItemButtons.Length; i++)
        {
            if (shopItemButtons[i] == null) { continue; }
            TextMeshProUGUI label = shopItemButtons[i].GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                if (i < _currentShopItems.Count) { label.text = _currentShopItems[i].label; }
                else { label.text = "-"; }
            }
            shopItemButtons[i].onClick.RemoveAllListeners();
        }

        if (shopItemButtons.Length > 0 && shopItemButtons[0] != null) { shopItemButtons[0].onClick.AddListener(OnClickShop0); }
        if (shopItemButtons.Length > 1 && shopItemButtons[1] != null) { shopItemButtons[1].onClick.AddListener(OnClickShop1); }
        if (shopItemButtons.Length > 2 && shopItemButtons[2] != null) { shopItemButtons[2].onClick.AddListener(OnClickShop2); }
        if (shopItemButtons.Length > 3 && shopItemButtons[3] != null) { shopItemButtons[3].onClick.AddListener(OnClickShop3); }
        if (shopItemButtons.Length > 4 && shopItemButtons[4] != null) { shopItemButtons[4].onClick.AddListener(OnClickShop4); }
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

        isPlayerTurn = false;
        StartCoroutine(EnemyActOnceThenPass());
    }

    private List<ShopItem> BuildRandom5Items()
    {
        List<ShopItem> list = new List<ShopItem>();
        for (int i = 0; i < 5; i++) { list.Add(RandomShopItem()); }
        return list;
    }

    private ShopItem RandomShopItem()
    {
        int r = Random.Range(0, 4);
        ShopItem item = new ShopItem();
        if (r == 0)
        {
            item.kind = ShopKind.HealHP;
            item.value = Random.Range(10, 31);
            item.label = "HP 회복 +" + item.value.ToString();
            return item;
        }
        if (r == 1)
        {
            item.kind = ShopKind.BuffATK;
            item.value = Random.Range(2, 7);
            item.label = "공격 +" + item.value.ToString();
            return item;
        }
        if (r == 2)
        {
            item.kind = ShopKind.BuffDEF;
            item.value = Random.Range(2, 7);
            item.label = "방어 +" + item.value.ToString();
            return item;
        }
        item.kind = ShopKind.BuffSPD;
        item.value = Random.Range(2, 7);
        item.label = "속도 +" + item.value.ToString();
        return item;
    }

    private void ApplyItem(Pokemon target, ShopItem item)
    {
        if (target == null) { return; }

        if (item.kind == ShopKind.HealHP) { target.Hp = target.Hp + item.value; }
        else if (item.kind == ShopKind.BuffATK) { target.atk = target.atk + item.value; }
        else if (item.kind == ShopKind.BuffDEF) { target.def = target.def + item.value; }
        else { target.speed = target.speed + item.value; }

        if (textLog != null)
        {
            bool isEnemy = target == otherPokemonB ? true : false;
            string prefix = isEnemy ? "적의 " : "";
            textLog.text = prefix + target.name + "이 " + item.label + "을 사용하였다.";
            textLog.gameObject.SetActive(true);
        }
    }

    // 유틸 ---------------------------------------------------------------
    private static Pokemon FirstAlive(Pokemon[] team)
    {
        if (team == null) { return null; }
        for (int i = 0; i < team.Length; i++)
        {
            Pokemon p = team[i];
            if (p != null)
            {
                if (p.Hp > 0) { return p; }
            }
        }
        return null;
    }

    private static int IndexOf(Pokemon[] team, Pokemon p)
    {
        if (team == null) { return -1; }
        for (int i = 0; i < team.Length; i++)
        {
            if (team[i] == p) { return i; }
        }
        return -1;
    }

    private static Pokemon NextAliveAfterIndex(Pokemon[] team, int startExclusive)
    {
        if (team == null) { return null; }
        int idx = startExclusive + 1;
        while (idx < team.Length)
        {
            Pokemon cand = team[idx];
            if (cand != null)
            {
                if (cand.Hp > 0) { return cand; }
            }
            idx = idx + 1;
        }
        return null;
    }

    public static Pokemon CreateByIndexShared(Pokemon.PokemonIndex idx, bool isPlayerSide)
    {
        Pokemon pokemon;
        if (idx == Pokemon.PokemonIndex.pikach) { pokemon = new Pika(); }
        else if (idx == Pokemon.PokemonIndex.paily) { pokemon = new Paily(); }
        else if (idx == Pokemon.PokemonIndex.goBook) { pokemon = new GoBook(); }
        else { pokemon = new Esang(); }

        pokemon.index = idx;
        ConfigureAtlasForSide(pokemon, isPlayerSide);
        return pokemon;
    }

    public static void ConfigureAtlasForSide(Pokemon pokemon, bool isPlayerSide)
    {
        if (pokemon == null) { return; }

        if (pokemon.index == Pokemon.PokemonIndex.pikach)
        {
            pokemon.atlasResourcePath = "PikaSpriteAtlas";
            pokemon.spriteKeyChoice = "PIKACH";
            pokemon.spriteKeyBattleIdle = "PIKA2";
            pokemon.spriteKeyAttack = "PIKA4";
            pokemon.spriteKeySkill = "PIKA3";
            return;
        }
        if (pokemon.index == Pokemon.PokemonIndex.paily)
        {
            pokemon.atlasResourcePath = "PailySpriteAtlas";
            pokemon.spriteKeyChoice = "PAILY";
            pokemon.spriteKeyBattleIdle = "PAILY2";
            pokemon.spriteKeyAttack = "PAILY4";
            pokemon.spriteKeySkill = "PAILY3";
            return;
        }
        if (pokemon.index == Pokemon.PokemonIndex.goBook)
        {
            pokemon.atlasResourcePath = "GoBookSpriteAtlas";
            pokemon.spriteKeyChoice = "GOBOOK1";
            pokemon.spriteKeyBattleIdle = "GOBOOK2";
            pokemon.spriteKeyAttack = "GOBOOK4";
            pokemon.spriteKeySkill = "GOBOOK3";
            return;
        }

        pokemon.atlasResourcePath = "EsangSpriteAtlas";
        pokemon.spriteKeyChoice = "ESANGSEE";
        pokemon.spriteKeyBattleIdle = "ESANG2";
        pokemon.spriteKeyAttack = "ESANG4";
        pokemon.spriteKeySkill = "ESANG3";
    }

    private IEnumerator StartNextRoundFlow()
    {
        roundIndex = roundIndex + 1;
        _pendingRound = roundIndex;
        if (roundText != null) { roundText.text = "Round " + roundIndex.ToString(); }
        DecideFirstTurn();
        ShowFirstTurnLog();
        yield return null;
    }

    public static int GetRoundSnapshot()
    {
        return Instance != null ? Instance.roundIndex : _pendingRound;
    }

    public static void SetRoundFromSave(int round)
    {
        _pendingRound = Mathf.Max(1, round);
        if (Instance == null) { return; }

        Instance.roundIndex = _pendingRound;
        if (Instance.roundText != null)
        {
            Instance.roundText.text = "Round " + Instance.roundIndex.ToString();
        }
    }

    // Setting 포워딩 -----------------------------------------------------
    private void EnsureSettingForBattle()
    {
        if (settingsRef == null) { return; }
        Setting s = settingsRef.GetComponent<Setting>();
        if (s != null)
        {
            if (uiRoot != null) { s.uiRoot = uiRoot; }
            s.settingsSortOrder = settingsSortOrder;
            if (settingsRef.activeSelf) { settingsRef.SetActive(false); }
        }
    }

    public void OnClickSettingsButton()
    {
        if (settingsRef == null) { return; }
        settingsRef.SendMessage("OnClickSettingsButton", SendMessageOptions.DontRequireReceiver);
    }
    public void OnClickSettingsResume()
    {
        if (settingsRef == null) { return; }
        settingsRef.SendMessage("OnClickSettingsResume", SendMessageOptions.DontRequireReceiver);
    }
    public void OnClickSettingsRestart()
    {
        if (settingsRef == null) { return; }
        settingsRef.SendMessage("OnClickSettingsRestart", SendMessageOptions.DontRequireReceiver);
    }
    public void OnClickSettingsExitToStart()
    {
        if (settingsRef == null) { return; }
        settingsRef.SendMessage("OnClickSettingsExitToStart", SendMessageOptions.DontRequireReceiver);
    }
}
