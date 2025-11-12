using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PokemonBattleManager : MonoBehaviour
{
    // @ 싱글톤
    public static PokemonBattleManager instance;
    public static PokemonBattleManager Instance { get; private set; }

    // @ 라운드 스냅샷(저장/로딩 연계용)
    private static int _pendingRound = 1;   // 씬 로드 전 임시 보관

    // @ Setting 포워딩 확장용
    public GameObject settingsRef;
    public int settingsSortOrder = 5000;
    public Transform uiRoot;

    // @ 전투 UI
    public PokemonInfo myInfo;
    public PokemonInfo otherInfo;
    public TextMeshProUGUI textLog;
    public TextMeshProUGUI roundText;

    // @ 명령 버튼(인스펙터 직접 연결용)  @ Attack / Bag / Skills / PokemonList
    public GameObject commandRoot;      // @ 4개 버튼의 부모 루트
    public Button attackBt;
    public Button bagBt;
    public Button skillsBt;
    public Button pokemonListBt;

    // @ 기존 배열 기반 바인딩도 유지(호환용)
    public Button[] commandBts;         // @ [0]=Attack [1]=Bag [2]=Skills [3]=PokemonList

    // @ 스킬 버튼(플레이어) 4개
    public Button[] skill1_4;           // @ 4개의 스킬 버튼

    // @ 상점 및 교체 패널
    public GameObject ShopPanel;
    public Button[] shopItemButtons;    // @ 5개
    public GameObject switchPanel;
    public Button[] switchButtons;      // @ 교체 버튼 3개

    // @ 일반공격 FX
    public GameObject playerNormalAttackFxPrefab;
    public GameObject enemyNormalAttackFxPrefab;
    public Transform playerFxAnchor;
    public Transform enemyFxAnchor;

    // @ 게임오버 패널
    public GameObject gameOverPanel;
    public TextMeshProUGUI gameOverText;
    public Button gameOverButton;

    // @ 내부 전투 상태
    private Pokemon myPokemonB;
    private Pokemon otherPokemonB;
    private int roundIndex = 1;

    // @ 턴/쿨다운 관리
    private bool isPlayerTurn = true;
    private int playerTurnTick = 0;
    private int enemyTurnTick = 0;
    private int[] playerLastUsedTurn = new int[4] { -999, -999, -999, -999 };
    private int[] enemyLastUsedTurn = new int[4] { -999, -999, -999, -999 };

    // @ 내부 플래그
    private bool isGameOverShown = false;

    // =====================================================================
    // @ 저장/로딩 연계: 외부에서 라운드 값을 세팅/조회
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
    // @ 라이프사이클
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

        // 저장에서 미리 전달된 라운드 반영
        roundIndex = (_pendingRound <= 0) ? 1 : _pendingRound;

        InitializeBattleState();
        BindCommandButtons();     // @ 인스펙터 버튼 + 배열형 호환 바인딩
        BindSkillButtons();       // @ 스킬 버튼 4개 바인딩
        BindSwitchButtons();      // @ 교체 버튼 바인딩
        BindGameOverButton();
        HideAllSubPanelsAtStart();
        UpdateRoundText();
        DecideFirstTurn();
        ShowFirstTurnLog();

        // @ 턴 시작 처리(명령 버튼 가시상태/스킬 쿨다운 UI 등)
        OnTurnBegan();
    }

    private void OnDestroy()
    {
        if (Instance == this) { Instance = null; }
        if (instance == this) { instance = null; }
    }

    // =====================================================================
    // @ 초기화
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
    }

    // =====================================================================
    // @ 버튼 바인딩
    // =====================================================================
    private void BindCommandButtons()
    {
        // @ 인스펙터 직접 연결 버튼 우선
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

        // @ 배열형도 남겨서 호환 유지
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

        // @ 초기 상태: 플레이어 턴이 아니면 루트 비활성
        if (commandRoot != null)
        {
            bool active = isPlayerTurn ? true : false;
            if (commandRoot.activeSelf != active)
            {
                commandRoot.SetActive(active ? true : false);
            }
        }
        SetCommandsInteractable(isPlayerTurn ? true : false);
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
        int len = switchButtons.Length;

        if (len > 0)
        {
            if (switchButtons[0] != null)
            {
                switchButtons[0].onClick.RemoveAllListeners();
                switchButtons[0].onClick.AddListener(OnClickSwitch0);
            }
        }
        if (len > 1)
        {
            if (switchButtons[1] != null)
            {
                switchButtons[1].onClick.RemoveAllListeners();
                switchButtons[1].onClick.AddListener(OnClickSwitch1);
            }
        }
        if (len > 2)
        {
            if (switchButtons[2] != null)
            {
                switchButtons[2].onClick.RemoveAllListeners();
                switchButtons[2].onClick.AddListener(OnClickSwitch2);
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
            if (gameOverPanel.activeSelf)
            {
                gameOverPanel.SetActive(false);
            }
        }
    }

    private void HideAllSubPanelsAtStart()
    {
        if (ShopPanel != null) { ShopPanel.SetActive(false); }
        if (switchPanel != null) { switchPanel.SetActive(false); }
    }

    // =====================================================================
    // @ 커맨드
    // =====================================================================
    public void OnClickAttackCommand()
    {
        if (!isPlayerTurn) { return; }

        if (textLog != null)
        {
            if (myPokemonB != null)
            {
                textLog.text = myPokemonB.name + " 이 공격하였다";
                textLog.gameObject.SetActive(true);
            }
        }

        StartCoroutine(PerformAttack(myPokemonB, otherPokemonB, -1));
    }

    public void OpenSkillPanel()
    {
        if (!isPlayerTurn) { return; }
        SetSkillButtonsActive(true);
        RefreshPlayerSkillCooldownUI();  // @ 열 때 한 번 더 쿨다운 반영
    }

    public void OpenSwitch()
    {
        if (switchPanel != null)
        {
            RefreshSwitchButtonLabels();
            switchPanel.SetActive(true);
        }
    }

    public void OnClickSkill0() { OnSkillClick(0); }
    public void OnClickSkill1() { OnSkillClick(1); }
    public void OnClickSkill2() { OnSkillClick(2); }
    public void OnClickSkill3() { OnSkillClick(3); }

    private void OnSkillClick(int skillIdx)
    {
        if (!isPlayerTurn) { return; }

        // @ 쿨다운 체크: playerTurnTick - lastUsed <= 1 이면 사용 불가
        bool allow = true;
        if (skillIdx >= 0)
        {
            if (skillIdx < playerLastUsedTurn.Length)
            {
                int last = playerLastUsedTurn[skillIdx];
                int diff = playerTurnTick - last;
                if (diff <= 1)
                {
                    allow = false;
                }
            }
        }
        if (!allow) { return; }

        if (textLog != null)
        {
            if (myPokemonB != null)
            {
                string sName = "스킬";
                if (myPokemonB.skillNames != null)
                {
                    bool okA = (skillIdx >= 0) ? true : false;
                    if (okA)
                    {
                        bool okB = (skillIdx < myPokemonB.skillNames.Length) ? true : false;
                        if (okB)
                        {
                            sName = myPokemonB.skillNames[skillIdx];
                        }
                    }
                }
                textLog.text = myPokemonB.name + " 이 " + sName + " 을 사용하였다";
                textLog.gameObject.SetActive(true);
            }
        }

        // @ 사용 턴 기록
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
        }
    }

    private void SetCommandsInteractable(bool enable)
    {
        // @ 인스펙터 버튼 우선
        if (attackBt != null) { attackBt.interactable = enable ? true : false; }
        if (bagBt != null) { bagBt.interactable = enable ? true : false; }
        if (skillsBt != null) { skillsBt.interactable = enable ? true : false; }
        if (pokemonListBt != null) { pokemonListBt.interactable = enable ? true : false; }

        // @ 배열형 호환
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

    private void SetCommandRootActive(bool active)
    {
        if (commandRoot == null) { return; }
        if (commandRoot.activeSelf != active)
        {
            commandRoot.SetActive(active ? true : false);
        }
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
                if (diff <= 1)
                {
                    can = false;
                }
            }
            b.interactable = can ? true : false;
        }
    }

    // =====================================================================
    // @ 전투 실행
    // =====================================================================
    private IEnumerator PerformAttack(Pokemon attacker, Pokemon defender, int skillIndex)
    {
        if (attacker == null) { yield break; }
        if (defender == null) { yield break; }

        if (attacker.info != null)
        {
            attacker.info.ApplyAttackPose();
            if (defender.info != null)
            {
                attacker.info.SpriteMove(defender.info);
            }
        }

        // @ 일반공격 FX
        if (skillIndex < 0)
        {
            SpawnNormalAttackFxFor(attacker);
        }

        yield return StartCoroutine(attacker.Attack(defender, skillIndex));

        if (defender.info != null) { defender.info.ApplySkillPose(); }
        yield return new WaitForSeconds(0.25f);
        if (attacker.info != null) { attacker.info.ApplyBattleIdlePose(); }
        if (defender.info != null) { defender.info.ApplyBattleIdlePose(); }

        yield return new WaitForSeconds(0.35f);
        if (textLog != null) { textLog.gameObject.SetActive(false); }

        yield return StartCoroutine(CheckAndResolveFaintStates());
        if (isGameOverShown) { yield break; }

        // @ 턴 전환
        isPlayerTurn = isPlayerTurn ? false : true;

        if (!isPlayerTurn)
        {
            // @ 적 턴 시작 처리
            OnTurnBegan();

            yield return StartCoroutine(EnemyActOnceThenPass());
            if (isGameOverShown) { yield break; }

            // @ 적이 끝났으면 플레이어 턴 시작 처리
            isPlayerTurn = true;
            OnTurnBegan();
        }
        else
        {
            // @ 플레이어 턴 시작 처리
            OnTurnBegan();
            UpdateRoundText(); // @ 라운드 표시는 유지(라운드 증가 없음)
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

        // @ 스킬 선택(쿨다운 중인 스킬 제외)
        bool willUseSkill = false;
        int chosenSkill = -1;

        if (otherPokemonB.skillNames != null)
        {
            if (otherPokemonB.skillNames.Length > 0)
            {
                // @ 스킬 후보를 순회하며 첫 사용 가능 스킬 선택(단순 전략)
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
                        if (diff <= 1)
                        {
                            blocked = true;
                        }
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
            if (textLog != null)
            {
                string sName = otherPokemonB.skillNames[chosenSkill];
                textLog.text = "적의 " + otherPokemonB.name + " 이 " + sName + " 을 사용하였다";
                textLog.gameObject.SetActive(true);
            }

            // @ 적 스킬 사용 기록
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
            if (textLog != null)
            {
                textLog.text = "적의 " + otherPokemonB.name + " 이 공격하였다";
                textLog.gameObject.SetActive(true);
            }
            yield return StartCoroutine(PerformAttack(otherPokemonB, myPokemonB, -1));
        }

        // @ 적 턴 종료 -> 플레이어 턴으로 넘어감(상위에서 OnTurnBegan 호출)
        isPlayerTurn = true;
    }

    // =====================================================================
    // @ 교체 및 라운드 증가 처리
    // =====================================================================
    private IEnumerator DoSwitchPlayer()
    {
        Pokemon next = NextAliveAfterIndex(PokemonGamemanager.playerTeam3, PokemonGamemanager.playerActiveIndex);
        myPokemonB = next;
        if (myPokemonB != null)
        {
            PokemonGamemanager.playerActiveIndex = IndexOf(PokemonGamemanager.playerTeam3, myPokemonB);
            if (myInfo != null)
            {
                myInfo.targetPokemon = myPokemonB;
                myPokemonB.info = myInfo;
                myInfo.ApplyBattleIdlePose();
            }
        }
        yield return null;
    }

    private IEnumerator DoSwitchEnemy()
    {
        Pokemon next = NextAliveAfterIndex(PokemonGamemanager.enemyTeam3, PokemonGamemanager.enemyActiveIndex);
        if (next != null)
        {
            otherPokemonB = next;
            PokemonGamemanager.enemyActiveIndex = IndexOf(PokemonGamemanager.enemyTeam3, otherPokemonB);
            if (otherInfo != null)
            {
                otherInfo.targetPokemon = otherPokemonB;
                otherPokemonB.info = otherInfo;
                otherInfo.ApplyBattleIdlePose();
            }
            yield return null;
            yield break;
        }

        // @ 여기까지 왔다면 적 팀 전멸 @ 라운드 증가
        yield return StartCoroutine(OnEnemyTeamClearedAndIncreaseRound());
    }

    private IEnumerator OnEnemyTeamClearedAndIncreaseRound()
    {
        if (textLog != null)
        {
            textLog.text = "라운드 클리어!";
            textLog.gameObject.SetActive(true);
        }
        yield return new WaitForSeconds(0.6f);

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
        ShowFirstTurnLog();
        OnTurnBegan();  // @ 새 라운드 첫 턴 시작 처리
    }

    private void RebuildEnemyTeamForNextRound()
    {
        // @ 적 팀 3마리 무작위 구성(중복 없음)
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
    // @ 상점 단순 로직
    // =====================================================================
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
        OnTurnBegan();  // @ 적 턴 시작 처리
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
        }
    }

    // =====================================================================
    // @ 유틸
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
    // @ 라운드 UI/턴 결정/로그
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
        if (myPokemonB == null)
        {
            isPlayerTurn = true;
            return;
        }
        if (otherPokemonB == null)
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
        bool odd = (roundIndex % 2 == 1) ? true : false;
        isPlayerTurn = odd ? true : false;
    }

    private void ShowFirstTurnLog()
    {
        if (textLog == null) { return; }
        string msg = isPlayerTurn ? "선제공격 판정 성공" : "선제공격 판정 실패";
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

    private void OnTurnBegan()
    {
        if (isPlayerTurn)
        {
            playerTurnTick = playerTurnTick + 1;
            SetCommandRootActive(true);
            SetCommandsInteractable(true);
            RefreshPlayerSkillCooldownUI();
        }
        else
        {
            enemyTurnTick = enemyTurnTick + 1;
            SetCommandsInteractable(false);
            SetCommandRootActive(false);
        }
    }

    // =====================================================================
    // @ 스위치 버튼/라벨, 일반공격 FX
    // =====================================================================
    private void RefreshSwitchButtonLabels()
    {
        if (switchButtons == null) { return; }
        Pokemon[] team = PokemonGamemanager.playerTeam3;
        int activeIdx = PokemonGamemanager.playerActiveIndex;

        for (int i = 0; i < switchButtons.Length; i = i + 1)
        {
            Button bt = switchButtons[i];
            if (bt == null) { continue; }

            TextMeshProUGUI label = bt.GetComponentInChildren<TextMeshProUGUI>();
            string textOut = "-";
            bool interact = false;

            if (team != null)
            {
                if (i < team.Length)
                {
                    Pokemon p = team[i];
                    if (p != null)
                    {
                        textOut = p.name;
                        if (p.Hp > 0)
                        {
                            if (i != activeIdx) { interact = true; }
                        }
                    }
                }
            }

            if (label != null) { label.text = textOut; }
            bt.interactable = interact ? true : false;
        }
    }

    private void SpawnNormalAttackFxFor(Pokemon attacker)
    {
        if (attacker == null) { return; }

        bool isPlayer = false;
        if (attacker == myPokemonB) { isPlayer = true; }

        GameObject fxPrefab = null;
        if (isPlayer)
        {
            fxPrefab = playerNormalAttackFxPrefab;
        }
        else
        {
            fxPrefab = enemyNormalAttackFxPrefab;
        }
        if (fxPrefab == null) { return; }

        Transform anchor = null;
        if (isPlayer)
        {
            if (playerFxAnchor != null) { anchor = playerFxAnchor; }
            else
            {
                if (myInfo != null)
                {
                    if (myInfo.image != null) { anchor = myInfo.image.transform; }
                }
            }
        }
        else
        {
            if (enemyFxAnchor != null) { anchor = enemyFxAnchor; }
            else
            {
                if (otherInfo != null)
                {
                    if (otherInfo.image != null) { anchor = otherInfo.image.transform; }
                }
            }
        }
        if (anchor == null) { return; }

        GameObject go = GameObject.Instantiate(fxPrefab, anchor.position, Quaternion.identity);
        go.transform.SetParent(anchor, true);
        GameObject.Destroy(go, 2f);
    }

    // @ 교체 버튼 동작
    public void OnClickSwitch0() { OnClickSwitchIndex(0); }
    public void OnClickSwitch1() { OnClickSwitchIndex(1); }
    public void OnClickSwitch2() { OnClickSwitchIndex(2); }

    private void OnClickSwitchIndex(int idx)
    {
        if (!isPlayerTurn) { return; }

        Pokemon[] team = PokemonGamemanager.playerTeam3;
        if (team == null) { return; }
        if (idx < 0) { return; }
        if (idx >= team.Length) { return; }

        if (idx == PokemonGamemanager.playerActiveIndex) { return; }

        Pokemon cand = team[idx];
        if (cand == null) { return; }
        if (cand.Hp <= 0) { return; }

        myPokemonB = cand;
        PokemonGamemanager.playerActiveIndex = idx;

        if (myInfo != null)
        {
            myInfo.targetPokemon = myPokemonB;
            myPokemonB.info = myInfo;
            myInfo.ApplyBattleIdlePose();
        }

        if (switchPanel != null) { switchPanel.SetActive(false); }

        isPlayerTurn = false;
        OnTurnBegan();
        StartCoroutine(EnemyActOnceThenPass());
    }

    // =====================================================================
    // @ 게임오버 처리
    // =====================================================================
    private void ShowGameOver(string message)
    {
        if (isGameOverShown) { return; }
        isGameOverShown = true;

        SetCommandsInteractable(false);
        SetSkillButtonsActive(false);
        SetCommandRootActive(false);

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
        SceneManager.LoadScene(0);  // @ PokemonStart
    }
}
