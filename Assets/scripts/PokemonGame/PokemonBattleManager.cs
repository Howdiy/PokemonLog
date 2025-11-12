using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// @ 전투 진행 매니저(사람 vs 알고리즘)
/// @ 적 측은 UI/오브젝트 없이 내부 로직으로만 동작
/// @ 교체 UI는 플레이어 쪽만 2슬롯 제공
/// </summary>
public class PokemonBattleManager : MonoBehaviour
{
    // @ 싱글톤
    public static PokemonBattleManager instance;
    public static PokemonBattleManager Instance { get; private set; }

    // @ 라운드 스냅샷(저장/로딩 연계용)
    private static int _pendingRound = 1;

    // @ 전투 UI
    public PokemonInfo myInfo;
    public PokemonInfo otherInfo;
    public TextMeshProUGUI textLog;
    public TextMeshProUGUI roundText;

    // @ 명령 버튼(인스펙터 직접 연결)
    public GameObject commandRoot;
    public Button attackBt;
    public Button bagBt;
    public Button skillsBt;
    public Button pokemonListBt;
    public Button[] commandBts;

    // @ 스킬 버튼(플레이어 1~4)
    public Button[] skill1_4;

    // @ 상점 및 교체 패널(플레이어만)
    public GameObject ShopPanel;
    public Button[] shopItemButtons;
    public GameObject switchPanel;
    public Button[] switchButtons;   // 정확히 2개 연결 권장

    // @ 일반공격 FX 프리팹(플레이어/적 공용)
    public GameObject NormalAttackFxPerfsb;

    // @ 게임오버 패널
    public GameObject gameOverPanel;
    public TextMeshProUGUI gameOverText;
    public Button gameOverButton;

    // @ 내부 전투 상태
    private Pokemon myPokemonB;
    private Pokemon otherPokemonB;
    private int roundIndex = 1;

    // @ 턴/쿨다운
    private bool isPlayerTurn = true;
    private int playerTurnTick = 0;
    private int enemyTurnTick = 0;
    private int[] playerLastUsedTurn = new int[4] { -999, -999, -999, -999 };
    private int[] enemyLastUsedTurn = new int[4] { -999, -999, -999, -999 };

    // @ 스위치 슬롯 -> 팀 인덱스(플레이어 2칸)
    private int[] _switchSlotToTeamIndex = new int[2] { -1, -1 };

    // @ 상태
    private bool isGameOverShown = false;

    // =====================================================================
    // 저장/로딩 연계
    // =====================================================================
    public static int GetRoundSnapshot()
    {
        int hasInstance = (Instance != null) ? 1 : 0;
        if (hasInstance == 1) { return Instance.roundIndex; }
        return _pendingRound;
    }

    public static void SetRoundFromSave(int r)
    {
        int rr = (r <= 0) ? 1 : r;
        _pendingRound = rr;

        int hasInstance = (Instance != null) ? 1 : 0;
        if (hasInstance == 1)
        {
            Instance.roundIndex = rr;
            if (Instance.roundText != null)
            {
                Instance.roundText.text = "Round " + rr.ToString();
            }
        }
    }

    // =====================================================================
    // 라이프사이클
    // =====================================================================
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

        roundIndex = (_pendingRound <= 0) ? 1 : _pendingRound;

        InitializeBattleState();
        BindCommandButtons();
        BindSkillButtons();
        BindSwitchButtons();   // 플레이어 2슬롯
        BindGameOverButton();
        HideAllSubPanelsAtStart();
        UpdateRoundText();
        DecideFirstTurn();

        if (commandRoot != null)
        {
            if (commandRoot.activeSelf == false) { commandRoot.SetActive(true); }
        }
        ApplyCommandInteractivityByLog();
        ShowFirstTurnLog();
    }

    private void OnDestroy()
    {
        if (Instance == this) { Instance = null; }
        if (instance == this) { instance = null; }
    }

    // =====================================================================
    // 초기화
    // =====================================================================
    private void InitializeBattleState()
    {
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

        if (myPokemonB == null)
        {
            ShowGameOver("전투를 시작할 포켓몬이 없습니다");
            return;
        }

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
            if (otherPokemonB != null)
            {
                otherPokemonB.info = otherInfo;
                otherInfo.ApplyBattleIdlePose();
            }
        }

        RefreshSwitchButtonLabels();
    }

    // =====================================================================
    // 버튼 바인딩
    // =====================================================================
    private void BindCommandButtons()
    {
        if (attackBt != null)
        {
            attackBt.onClick.RemoveAllListeners();
            attackBt.onClick.AddListener(OnClickAttackCommand);
        }
        if (bagBt != null)
        {
            bagBt.onClick.RemoveAllListeners();
            bagBt.onClick.AddListener(OpenShop);
        }
        if (skillsBt != null)
        {
            skillsBt.onClick.RemoveAllListeners();
            skillsBt.onClick.AddListener(OpenSkillPanel);
        }
        if (pokemonListBt != null)
        {
            pokemonListBt.onClick.RemoveAllListeners();
            pokemonListBt.onClick.AddListener(OpenSwitch);
        }

        if (commandBts != null)
        {
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
    }

    private void BindSkillButtons()
    {
        if (skill1_4 == null) { return; }

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

        SetSkillButtonsActive(false);
    }

    private void BindSwitchButtons()
    {
        if (switchButtons == null) { return; }

        if (switchButtons.Length > 0)
        {
            if (switchButtons[0] != null)
            {
                switchButtons[0].onClick.RemoveAllListeners();
                switchButtons[0].onClick.AddListener(OnClickSwitch0);
            }
        }
        if (switchButtons.Length > 1)
        {
            if (switchButtons[1] != null)
            {
                switchButtons[1].onClick.RemoveAllListeners();
                switchButtons[1].onClick.AddListener(OnClickSwitch1);
            }
        }

        RefreshSwitchButtonLabels();
    }

    private void BindGameOverButton()
    {
        if (gameOverButton != null)
        {
            gameOverButton.onClick.RemoveAllListeners();
            gameOverButton.onClick.AddListener(OnClickGameOverOK);
        }
        if (gameOverPanel != null)
        {
            if (gameOverPanel.activeSelf) { gameOverPanel.SetActive(false); }
        }
    }

    private void HideAllSubPanelsAtStart()
    {
        if (ShopPanel != null) { ShopPanel.SetActive(false); }
        if (switchPanel != null) { switchPanel.SetActive(false); }
        if (textLog != null) { textLog.gameObject.SetActive(false); }
    }

    // =====================================================================
    // 커맨드
    // =====================================================================
    public void OnClickAttackCommand()
    {
        if (!isPlayerTurn) { return; }
        StartCoroutine(PerformAttack(myPokemonB, otherPokemonB, -1));
    }

    public void OpenSkillPanel()
    {
        if (!isPlayerTurn) { return; }
        SetSkillButtonsActive(true);
        RefreshPlayerSkillCooldownUI();
        ApplyCommandInteractivityByLog();
    }

    public void OpenSwitch()
    {
        if (switchPanel != null)
        {
            RefreshSwitchButtonLabels();
            switchPanel.SetActive(true);
        }
        ApplyCommandInteractivityByLog();
    }

    public void OnClickSkill0() { OnSkillClick(0); }
    public void OnClickSkill1() { OnSkillClick(1); }
    public void OnClickSkill2() { OnSkillClick(2); }
    public void OnClickSkill3() { OnSkillClick(3); }

    private void OnSkillClick(int skillIdx)
    {
        if (!isPlayerTurn) { return; }

        bool allow = true;
        if (skillIdx >= 0)
        {
            if (skillIdx < playerLastUsedTurn.Length)
            {
                int last = playerLastUsedTurn[skillIdx];
                int diff = playerTurnTick - last;
                if (diff <= 1) { allow = false; }
            }
        }
        if (!allow) { return; }

        if (skillIdx >= 0)
        {
            if (skillIdx < playerLastUsedTurn.Length)
            {
                playerLastUsedTurn[skillIdx] = playerTurnTick;
            }
        }

        SetSkillButtonsActive(false);
        StartCoroutine(PerformAttack(myPokemonB, otherPokemonB, skillIdx));
    }

    private void SetSkillButtonsActive(bool active)
    {
        if (skill1_4 == null) { return; }
        for (int i = 0; i < skill1_4.Length; i = i + 1)
        {
            if (skill1_4[i] != null)
            {
                skill1_4[i].gameObject.SetActive(active ? true : false);
            }
        }
        if (!active)
        {
            if (textLog != null) { textLog.gameObject.SetActive(false); }
            ApplyCommandInteractivityByLog();
        }
    }

    private void SetCommandsInteractable(bool enable)
    {
        if (attackBt != null) { attackBt.interactable = enable ? true : false; }
        if (bagBt != null) { bagBt.interactable = enable ? true : false; }
        if (skillsBt != null) { skillsBt.interactable = enable ? true : false; }
        if (pokemonListBt != null) { pokemonListBt.interactable = enable ? true : false; }

        if (commandBts != null)
        {
            for (int i = 0; i < commandBts.Length; i = i + 1)
            {
                if (commandBts[i] != null)
                {
                    commandBts[i].interactable = enable ? true : false;
                }
            }
        }
    }

    private void ApplyCommandInteractivityByLog()
    {
        bool block = false;
        if (textLog != null)
        {
            if (textLog.gameObject.activeSelf) { block = true; }
        }
        SetCommandsInteractable(block ? true == false : true);
    }

    private void RefreshPlayerSkillCooldownUI()
    {
        if (skill1_4 == null) { return; }
        for (int i = 0; i < skill1_4.Length; i = i + 1)
        {
            Button b = skill1_4[i];
            if (b == null) { continue; }

            bool can = true;
            if (i < playerLastUsedTurn.Length)
            {
                int last = playerLastUsedTurn[i];
                int diff = playerTurnTick - last;
                if (diff <= 1) { can = false; }
            }
            b.interactable = can ? true : false;
        }
    }

    // =====================================================================
    // 전투 실행(연출 -> 데미지 -> 로그 -> 상태정리 -> 턴전환)
    // =====================================================================
    private IEnumerator PerformAttack(Pokemon attacker, Pokemon defender, int skillIndex)
    {
        if (attacker == null) { yield break; }
        if (defender == null) { yield break; }

        if (textLog != null)
        {
            if (textLog.gameObject.activeSelf) { textLog.gameObject.SetActive(false); }
        }
        ApplyCommandInteractivityByLog();

        PokemonInfo selfInfo = null;
        PokemonInfo targetInfo = null;

        if (attacker == myPokemonB)
        {
            selfInfo = myInfo;
            targetInfo = otherInfo;
        }
        else
        {
            selfInfo = otherInfo;
            targetInfo = myInfo;
        }

        if (selfInfo != null) { selfInfo.ApplyAttackPose(); }

        bool isNormal = (skillIndex < 0) ? true : false;
        bool isMelee = false;
        bool isRanged = false;

        if (!isNormal)
        {
            if (attacker.skillTypeBehaviours != null)
            {
                if (skillIndex < attacker.skillTypeBehaviours.Length)
                {
                    SkillTpye model = attacker.skillTypeBehaviours[skillIndex];
                    if (model != null)
                    {
                        if (model is MeleeAttackType) { isMelee = true; }
                        else
                        {
                            if (model is RangedAttackType) { isRanged = true; }
                        }
                    }
                }
            }
        }

        if (selfInfo != null)
        {
            if (isNormal)
            {
                yield return StartCoroutine(selfInfo.NormalAttackSequence(targetInfo, NormalAttackFxPerfsb));
            }
            else
            {
                if (isMelee)
                {
                    yield return StartCoroutine(selfInfo.MeleeSkillSequence(targetInfo, NormalAttackFxPerfsb));
                }
                else
                {
                    if (isRanged)
                    {
                        yield return StartCoroutine(selfInfo.RangedSkillSequence(targetInfo, NormalAttackFxPerfsb));
                    }
                    else
                    {
                        yield return new WaitForSeconds(0.25f);
                    }
                }
            }
        }

        yield return StartCoroutine(attacker.Attack(defender, skillIndex));

        if (selfInfo != null) { selfInfo.ApplyBattleIdlePose(); }
        if (targetInfo != null) { targetInfo.ApplyBattleIdlePose(); }

        yield return StartCoroutine(CheckAndResolveFaintStates());
        if (isGameOverShown) { yield break; }

        string doneLabel = "공격";
        if (!isNormal)
        {
            if (isMelee) { doneLabel = "근접 스킬"; }
            else
            {
                if (isRanged) { doneLabel = "원거리 스킬"; }
                else { doneLabel = "스킬"; }
            }
        }
        if (textLog != null)
        {
            textLog.text = attacker.name + "의 " + doneLabel;
            textLog.gameObject.SetActive(true);
            ApplyCommandInteractivityByLog();
        }
        yield return new WaitForSeconds(0.75f);
        if (textLog != null)
        {
            textLog.gameObject.SetActive(false);
            ApplyCommandInteractivityByLog();
        }

        isPlayerTurn = isPlayerTurn ? false : true;

        if (!isPlayerTurn)
        {
            enemyTurnTick = enemyTurnTick + 1;
            yield return StartCoroutine(EnemyActOnceThenPass());
            if (isGameOverShown) { yield break; }

            isPlayerTurn = true;
            playerTurnTick = playerTurnTick + 1;
            RefreshPlayerSkillCooldownUI();
            ApplyCommandInteractivityByLog();
            UpdateRoundText();
        }
        else
        {
            playerTurnTick = playerTurnTick + 1;
            RefreshPlayerSkillCooldownUI();
            ApplyCommandInteractivityByLog();
            UpdateRoundText();
        }
    }

    private IEnumerator CheckAndResolveFaintStates()
    {
        if (myPokemonB != null)
        {
            bool faint = (myPokemonB.Hp <= 0) ? true : false;
            if (faint)
            {
                yield return StartCoroutine(DoSwitchPlayer());
                if (myPokemonB == null)
                {
                    ShowGameOver("내 포켓몬이 모두 기절하였다");
                    yield break;
                }
            }
        }
        if (otherPokemonB != null)
        {
            bool faintE = (otherPokemonB.Hp <= 0) ? true : false;
            if (faintE)
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

        bool willUseSkill = false;
        int chosenSkill = -1;

        if (otherPokemonB.skillNames != null)
        {
            if (otherPokemonB.skillNames.Length > 0)
            {
                int sMax = 4;
                if (otherPokemonB.skillNames.Length < sMax) { sMax = otherPokemonB.skillNames.Length; }

                int s = 0;
                while (s < sMax)
                {
                    bool blocked = false;
                    if (s < enemyLastUsedTurn.Length)
                    {
                        int last = enemyLastUsedTurn[s];
                        int diff = enemyTurnTick - last;
                        if (diff <= 1) { blocked = true; }
                    }
                    if (!blocked)
                    {
                        willUseSkill = true;
                        chosenSkill = s;
                        break;
                    }
                    s = s + 1;
                }
            }
        }

        if (willUseSkill)
        {
            if (chosenSkill >= 0)
            {
                if (chosenSkill < enemyLastUsedTurn.Length)
                {
                    enemyLastUsedTurn[chosenSkill] = enemyTurnTick;
                }
            }
            yield return StartCoroutine(PerformAttack(otherPokemonB, myPokemonB, chosenSkill));
        }
        else
        {
            yield return StartCoroutine(PerformAttack(otherPokemonB, myPokemonB, -1));
        }

        isPlayerTurn = true;
    }

    // =====================================================================
    // 교체 및 라운드 증가
    // =====================================================================
    private IEnumerator DoSwitchPlayer()
    {
        int nextIdx = FindFirstAliveIndexExcept(PokemonGamemanager.playerTeam3, PokemonGamemanager.playerActiveIndex);
        if (nextIdx >= 0)
        {
            myPokemonB = PokemonGamemanager.playerTeam3[nextIdx];
            PokemonGamemanager.playerActiveIndex = nextIdx;
            if (myInfo != null)
            {
                myInfo.targetPokemon = myPokemonB;
                myPokemonB.info = myInfo;
                myInfo.ApplyBattleIdlePose();
            }
        }
        else
        {
            myPokemonB = null;
        }
        yield return null;
    }

    private IEnumerator DoSwitchEnemy()
    {
        int nextIdx = FindFirstAliveIndexExcept(PokemonGamemanager.enemyTeam3, PokemonGamemanager.enemyActiveIndex);
        if (nextIdx >= 0)
        {
            otherPokemonB = PokemonGamemanager.enemyTeam3[nextIdx];
            PokemonGamemanager.enemyActiveIndex = nextIdx;
            if (otherInfo != null)
            {
                otherInfo.targetPokemon = otherPokemonB;
                otherPokemonB.info = otherInfo;
                otherInfo.ApplyBattleIdlePose();
            }
            yield return null;
            yield break;
        }

        // @ 적 팀 전멸 -> 라운드 증가
        yield return StartCoroutine(OnEnemyTeamClearedAndIncreaseRound());
    }

    private IEnumerator OnEnemyTeamClearedAndIncreaseRound()
    {
        if (textLog != null)
        {
            textLog.text = "라운드 클리어!";
            textLog.gameObject.SetActive(true);
            ApplyCommandInteractivityByLog();
        }
        yield return new WaitForSeconds(0.6f);
        if (textLog != null)
        {
            textLog.gameObject.SetActive(false);
            ApplyCommandInteractivityByLog();
        }

        HandleRoundClear();
        yield return null;
    }

    private void HandleRoundClear()
    {
        roundIndex = roundIndex + 1;
        _pendingRound = roundIndex;
        UpdateRoundText();

        if (roundIndex >= 5)
        {
            ShowGameOver("5 라운드에 도달하였다");
            return;
        }

        RebuildEnemyTeamForNextRound();
        DecideFirstTurn();
    }

    private void RebuildEnemyTeamForNextRound()
    {
        List<int> pool = new List<int>();
        pool.Add(0);
        pool.Add(1);
        pool.Add(2);
        pool.Add(3);

        int c0 = Random.Range(0, pool.Count);
        int v0 = pool[c0];
        pool.RemoveAt(c0);

        int c1 = Random.Range(0, pool.Count);
        int v1 = pool[c1];
        pool.RemoveAt(c1);

        int c2 = Random.Range(0, pool.Count);
        int v2 = pool[c2];
        pool.RemoveAt(c2);

        PokemonGamemanager.enemyTeam3[0] = CreateByIndexShared((Pokemon.PokemonIndex)v0, false);
        PokemonGamemanager.enemyTeam3[1] = CreateByIndexShared((Pokemon.PokemonIndex)v1, false);
        PokemonGamemanager.enemyTeam3[2] = CreateByIndexShared((Pokemon.PokemonIndex)v2, false);
        PokemonGamemanager.enemyActiveIndex = 0;

        otherPokemonB = FirstAlive(PokemonGamemanager.enemyTeam3);
        if (otherInfo != null)
        {
            otherInfo.targetPokemon = otherPokemonB;
            if (otherPokemonB != null)
            {
                otherPokemonB.info = otherInfo;
                otherInfo.ApplyBattleIdlePose();
            }
        }
    }

    // =====================================================================
    // 상점(플레이어만)
    // =====================================================================
    private enum ShopKind { HealHP, BuffATK, BuffDEF, BuffSPD }
    private struct ShopItem { public ShopKind kind; public int value; public string label; }
    private readonly List<ShopItem> _currentShopItems = new List<ShopItem>();

    private void OpenShop()
    {
        if (!isPlayerTurn) { StartCoroutine(EnemyActOnceThenPass()); return; }
        if (ShopPanel == null) { return; }
        if (shopItemButtons == null) { return; }

        _currentShopItems.Clear();
        _currentShopItems.Add(RandomShopItem());
        _currentShopItems.Add(RandomShopItem());
        _currentShopItems.Add(RandomShopItem());
        _currentShopItems.Add(RandomShopItem());
        _currentShopItems.Add(RandomShopItem());

        ShopPanel.SetActive(true);

        for (int i = 0; i < shopItemButtons.Length; i = i + 1)
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

        ApplyCommandInteractivityByLog();
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

    private ShopItem RandomShopItem()
    {
        int r = Random.Range(0, 4);
        ShopItem it = new ShopItem();
        if (r == 0)
        {
            it.kind = ShopKind.HealHP;
            it.value = Random.Range(10, 31);
            it.label = "HP 회복 +" + it.value.ToString();
            return it;
        }
        if (r == 1)
        {
            it.kind = ShopKind.BuffATK;
            it.value = Random.Range(2, 7);
            it.label = "공격 +" + it.value.ToString();
            return it;
        }
        if (r == 2)
        {
            it.kind = ShopKind.BuffDEF;
            it.value = Random.Range(2, 7);
            it.label = "방어 +" + it.value.ToString();
            return it;
        }
        it.kind = ShopKind.BuffSPD;
        it.value = Random.Range(2, 7);
        it.label = "속도 +" + it.value.ToString();
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
            textLog.text = p.name + " 아이템 사용";
            textLog.gameObject.SetActive(true);
            ApplyCommandInteractivityByLog();
        }
    }

    // =====================================================================
    // 스위치(플레이어 2슬롯)
    // =====================================================================
    public void OnClickSwitch0() { OnClickSwitchIndex(0); }
    public void OnClickSwitch1() { OnClickSwitchIndex(1); }

    private void OnClickSwitchIndex(int slot)
    {
        if (!isPlayerTurn) { return; }
        if (slot < 0) { return; }
        if (slot >= _switchSlotToTeamIndex.Length) { return; }

        int teamIdx = _switchSlotToTeamIndex[slot];
        if (teamIdx < 0) { return; }

        Pokemon[] team = PokemonGamemanager.playerTeam3;
        if (team == null) { return; }
        if (teamIdx >= team.Length) { return; }
        if (teamIdx == PokemonGamemanager.playerActiveIndex) { return; }

        Pokemon cand = team[teamIdx];
        if (cand == null) { return; }
        if (cand.Hp <= 0) { return; }

        myPokemonB = cand;
        PokemonGamemanager.playerActiveIndex = teamIdx;

        if (myInfo != null)
        {
            myInfo.targetPokemon = myPokemonB;
            myPokemonB.info = myInfo;
            myInfo.ApplyBattleIdlePose();
        }

        if (switchPanel != null) { switchPanel.SetActive(false); }
    }

    private void RefreshSwitchButtonLabels()
    {
        if (switchButtons == null) { return; }
        Pokemon[] team = PokemonGamemanager.playerTeam3;
        int activeIdx = PokemonGamemanager.playerActiveIndex;

        List<int> candidates = new List<int>();
        if (team != null)
        {
            for (int i = 0; i < team.Length; i = i + 1)
            {
                if (i == activeIdx) { continue; }
                Pokemon p = team[i];
                if (p != null)
                {
                    if (p.Hp > 0) { candidates.Add(i); }
                }
            }
        }

        int slotsToFill = (switchButtons.Length >= 2) ? 2 : switchButtons.Length;

        for (int slot = 0; slot < slotsToFill; slot = slot + 1)
        {
            Button bt = switchButtons[slot];
            if (bt == null) { continue; }

            int mappedIdx = (slot < candidates.Count) ? candidates[slot] : -1;
            _switchSlotToTeamIndex[slot] = mappedIdx;

            TextMeshProUGUI label = bt.GetComponentInChildren<TextMeshProUGUI>();
            if (mappedIdx >= 0)
            {
                Pokemon p = team[mappedIdx];
                if (label != null)
                {
                    label.text = (p != null) ? p.name : "-";
                }
                bt.interactable = true;
            }
            else
            {
                if (label != null) { label.text = "-"; }
                bt.interactable = false;
            }
        }

        for (int slot = 2; slot < switchButtons.Length; slot = slot + 1)
        {
            Button bt = switchButtons[slot];
            if (bt == null) { continue; }
            TextMeshProUGUI label = bt.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null) { label.text = "-"; }
            bt.interactable = false;
        }
    }

    // =====================================================================
    // 유틸
    // =====================================================================
    private static Pokemon FirstAlive(Pokemon[] team)
    {
        if (team == null) { return null; }
        for (int i = 0; i < team.Length; i = i + 1)
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
        for (int i = 0; i < team.Length; i = i + 1)
        {
            if (team[i] == p) { return i; }
        }
        return -1;
    }

    private static int FindFirstAliveIndexExcept(Pokemon[] team, int activeIndex)
    {
        if (team == null) { return -1; }
        for (int i = 0; i < team.Length; i = i + 1)
        {
            if (i == activeIndex) { continue; }
            Pokemon p = team[i];
            if (p != null)
            {
                if (p.Hp > 0) { return i; }
            }
        }
        return -1;
    }

    public static Pokemon CreateByIndexShared(Pokemon.PokemonIndex idx, bool isPlayerSide)
    {
        Pokemon pokemon;
        if (idx == Pokemon.PokemonIndex.pikach)
        {
            pokemon = new Pika();
        }
        else
        {
            if (idx == Pokemon.PokemonIndex.paily)
            {
                pokemon = new Paily();
            }
            else
            {
                if (idx == Pokemon.PokemonIndex.goBook)
                {
                    pokemon = new GoBook();
                }
                else
                {
                    pokemon = new Esang();
                }
            }
        }

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

    // =====================================================================
    // 라운드 UI/선턴
    // =====================================================================
    private void UpdateRoundText()
    {
        if (roundText != null)
        {
            roundText.text = "Round " + roundIndex.ToString();
        }
    }

    private void DecideFirstTurn()
    {
        if (myPokemonB == null) { isPlayerTurn = true; return; }
        if (otherPokemonB == null) { isPlayerTurn = true; return; }

        if (myPokemonB.speed > otherPokemonB.speed) { isPlayerTurn = true; return; }
        if (myPokemonB.speed < otherPokemonB.speed) { isPlayerTurn = false; return; }

        bool odd = (roundIndex % 2 == 1) ? true : false;
        isPlayerTurn = odd ? true : false;
    }

    private void ShowFirstTurnLog()
    {
        if (textLog == null) { return; }
        string msg = isPlayerTurn ? "선제공격 판정 성공" : "선제공격 판정 실패";
        textLog.text = msg;
        textLog.gameObject.SetActive(true);
        ApplyCommandInteractivityByLog();
        StartCoroutine(HideLogAfter(0.75f));
    }

    private IEnumerator HideLogAfter(float sec)
    {
        yield return new WaitForSeconds(sec);
        if (textLog != null)
        {
            textLog.gameObject.SetActive(false);
            ApplyCommandInteractivityByLog();
        }
    }

    // =====================================================================
    // 게임오버
    // =====================================================================
    private void ShowGameOver(string message)
    {
        if (isGameOverShown) { return; }
        isGameOverShown = true;

        SetCommandsInteractable(false);
        if (commandRoot != null) { commandRoot.SetActive(false); }
        SetSkillButtonsActive(false);

        if (textLog != null)
        {
            textLog.gameObject.SetActive(false);
        }

        if (gameOverText != null)
        {
            gameOverText.text = message;
            gameOverText.ForceMeshUpdate();
        }
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
    }

    private void OnClickGameOverOK()
    {
        SceneManager.LoadScene(0);  // PokemonStart
    }
}
