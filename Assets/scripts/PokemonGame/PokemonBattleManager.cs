using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// @ PokemonBattleManager
/// @ 씬 인덱스: Start=0, Battle=1, Choices=2
/// @ PlayerTeam/EnemyTeam는 PokemonGamemanager의 정적 리스트 사용
/// @ 커맨드 4버튼 SetActive()로 보임/숨김
/// @ 스킬 1~4 버튼 텍스트를 현재 플레이어 포켓몬의 skillNames로 반영
/// @ 교체 UI 2슬롯(플레이어 전용), 적은 UI 없이 코드로만 행동
/// @ Setting 프리팹: 씬 인스턴스 참조 또는 에셋만 연결해도 자동 Instantiate
/// </summary>
public class PokemonBattleManager : MonoBehaviour
{
    // 싱글톤
    public static PokemonBattleManager instance;

    // 'Setting'PreFab 관련
    [Header("Setting Reference")]
    [SerializeField] private Setting settingsRef;
    [SerializeField] private GameObject settingsPrefab;
    [SerializeField] private Transform uiRoot;
    private GameObject _settingsInst;

    // 배틀중인 포켓몬 정보 표시
    [Header("Infos")]
    [SerializeField] private PokemonInfo myInfo;
    [SerializeField] private PokemonInfo otherInfo;

    [Header("Texts")]
    [SerializeField] public TextMeshProUGUI textLog;
    [SerializeField] private TextMeshProUGUI roundText;

    [Header("Game Over")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TextMeshProUGUI gameOverText;
    [SerializeField] private Button gameOverExitBt;

    public const string ROUND_SNAPSHOT_PREF_KEY = "POKEMON_ROUND_SNAPSHOT_V1";

    private class DefenseBuffState
    {
        public int amount;
        public int remainingTurns;
    }

    [System.Serializable]
    public class SkillCooldownSnapshot
    {
        public bool isPlayer;
        public int teamIndex;
        public int[] cooldowns = new int[4];
    }

    [System.Serializable]
    public class DefenseBuffSnapshot
    {
        public bool isPlayer;
        public int teamIndex;
        public int amount;
        public int remainingTurns;
    }

    [System.Serializable]
    public class RoundSnapshot
    {
        public int round;
        public int playerIndex;
        public int enemyIndex;
        public int[] playerHp;
        public int[] enemyHp;
        public SkillCooldownSnapshot[] cooldowns;
        public DefenseBuffSnapshot[] defenseBuffs;
    }

    private readonly Dictionary<Pokemon, int[]> _skillCooldowns = new Dictionary<Pokemon, int[]>();
    private readonly Dictionary<Pokemon, DefenseBuffState> _defenseBuffStates = new Dictionary<Pokemon, DefenseBuffState>();
    private bool _isGameOver = false;

    private enum EnemyActionType
    {
        NormalAttack,
        Skill,
        Switch
    }

    private struct EnemyDecision
    {
        public EnemyActionType actionType;
        public int skillIndex;
        public int switchToIndex;
    }

    // 플레이어 행동 버튼화
    [Header("Command Buttons")]
    [SerializeField] private Button attackBt;
    [SerializeField] private Button bagBt;
    [SerializeField] private Button battleSkillBt;
    [SerializeField] private Button pokemonListBt;

    // 플레이어 포켓몬 스킬 버튼
    [Header("Skill 1-4 Buttons")]
    [SerializeField] private Button skill1Bt;
    [SerializeField] private Button skill2Bt;
    [SerializeField] private Button skill3Bt;
    [SerializeField] private Button skill4Bt;

    // 상점패널 및 교체패널
    [Header("Shop Panel")]
    [SerializeField] private GameObject shopPanel;

    [Header("Switch Panel (2 Slots)")]
    [SerializeField] private GameObject switchPanel;
    [SerializeField] private Button switchBt0;
    [SerializeField] private Button switchBt1;
    [SerializeField] private TextMeshProUGUI switchBt0Text;
    [SerializeField] private TextMeshProUGUI switchBt1Text;

    // 인게임 진행 상태
    private int playerIndex = -1;
    private int enemyIndex = -1;
    private int round = 1;

    private Pokemon PlayerCur
    {
        get
        {
            if (PokemonGamemanager.PlayerTeam == null) { return null; }
            if (playerIndex < 0) { return null; }
            if (playerIndex >= PokemonGamemanager.PlayerTeam.Count) { return null; }
            return PokemonGamemanager.PlayerTeam[playerIndex];
        }
    }

    private Pokemon EnemyCur
    {
        get
        {
            if (PokemonGamemanager.EnemyTeam == null) { return null; }
            if (enemyIndex < 0) { return null; }
            if (enemyIndex >= PokemonGamemanager.EnemyTeam.Count) { return null; }
            return PokemonGamemanager.EnemyTeam[enemyIndex];
        }
    }

    private void Awake()
    {
        instance = this;
        EnsureSettingsInstanceOrBind();
    }

    private void Start()
    {
        if (settingsRef == null)
        {
            if (_settingsInst != null) { settingsRef = _settingsInst.GetComponent<Setting>(); }
            if (settingsRef == null)
            {
                Setting find = GameObject.FindObjectOfType<Setting>();
                if (find != null) { settingsRef = find; }
            }
        }

        playerIndex = PokemonGamemanager.FirstAliveIndex(PokemonGamemanager.PlayerTeam);
        if (playerIndex < 0) { playerIndex = 0; }

        enemyIndex = PokemonGamemanager.FirstAliveIndex(PokemonGamemanager.EnemyTeam);
        if (enemyIndex < 0) { enemyIndex = 0; }

        WireCommandButtons();
        WireSkillButtons();
        WireSwitchButtons();

        HideAllSubPanelsAtStart();
        if (gameOverPanel != null) { gameOverPanel.SetActive(false); }
        WireGameOverButton();

        if (myInfo != null) { myInfo.isPlayerSide = true; }
        if (otherInfo != null) { otherInfo.isPlayerSide = false; }

        LoadRoundSnapshotIfAvailable();
        RefreshInfos();
        RefreshRoundLabel();
        RefreshSkillButtonLabelsFromPlayer();
        RefreshSwitchButtonLabels();

        BeginPlayerTurn(false);
        LogNow("Round " + round.ToString() + " start");
    }

    /// <summary>
    /// 설정프리팹 관련
    /// </summary>
    // Setting 인스턴스 보장
    private void EnsureSettingsInstanceOrBind()
    {
        Setting exists = GameObject.FindObjectOfType<Setting>();
        if (exists != null)
        {
            settingsRef = exists;
            return;
        }

        if (settingsRef == null)
        {
            if (settingsPrefab != null)
            {
                Transform parent = uiRoot;
                if (parent == null)
                {
                    Canvas cv = GameObject.FindObjectOfType<Canvas>();
                    if (cv != null) { parent = cv.transform; }
                }

                if (parent != null) { _settingsInst = GameObject.Instantiate(settingsPrefab, parent); }
                else { _settingsInst = GameObject.Instantiate(settingsPrefab); }

                if (_settingsInst != null)
                {
                    if (_settingsInst.activeSelf == false) { _settingsInst.SetActive(true); }
                    settingsRef = _settingsInst.GetComponent<Setting>();
                }
            }
        }
    }

    /// <summary>
    /// 버튼별 바인딩
    /// </summary>
    // 플레이어 행동 버튼
    private void WireCommandButtons()
    {
        if (attackBt != null) { attackBt.onClick.RemoveAllListeners(); attackBt.onClick.AddListener(OnClickAttack); }
        if (bagBt != null) { bagBt.onClick.RemoveAllListeners(); bagBt.onClick.AddListener(OnClickBag); }
        if (battleSkillBt != null) { battleSkillBt.onClick.RemoveAllListeners(); battleSkillBt.onClick.AddListener(OnClickBattleSkillOpen); }
        if (pokemonListBt != null) { pokemonListBt.onClick.RemoveAllListeners(); pokemonListBt.onClick.AddListener(OnClickOpenSwitchPanel); }
    }
    //플레이어 포켓몬 스킬 버튼
    private void WireSkillButtons()
    {
        if (skill1Bt != null) { skill1Bt.onClick.RemoveAllListeners(); skill1Bt.onClick.AddListener(OnClickSkill1); }
        if (skill2Bt != null) { skill2Bt.onClick.RemoveAllListeners(); skill2Bt.onClick.AddListener(OnClickSkill2); }
        if (skill3Bt != null) { skill3Bt.onClick.RemoveAllListeners(); skill3Bt.onClick.AddListener(OnClickSkill3); }
        if (skill4Bt != null) { skill4Bt.onClick.RemoveAllListeners(); skill4Bt.onClick.AddListener(OnClickSkill4); }
    }
        if (_isGameOver) { active = false; }

        if (attackBt != null)
        {
            GameObject g = attackBt.gameObject;
            if (g != null) { g.SetActive(active); }
            attackBt.interactable = active;
        }
        if (bagBt != null)
        {
            GameObject g = bagBt.gameObject;
            if (g != null) { g.SetActive(active); }
            bagBt.interactable = active;
        }
        if (battleSkillBt != null)
        {
            GameObject g = battleSkillBt.gameObject;
            if (g != null) { g.SetActive(active); }
            battleSkillBt.interactable = active;
        }
        if (pokemonListBt != null)
        {
            GameObject g = pokemonListBt.gameObject;
            if (g != null) { g.SetActive(active); }
            pokemonListBt.interactable = active;
        }
    }

    private void SetSkillButtonsInteractable(bool interactable)
    {
        if (_isGameOver) { interactable = false; }

        if (skill1Bt != null) { skill1Bt.interactable = interactable && skill1Bt.gameObject.activeInHierarchy; }
        if (skill2Bt != null) { skill2Bt.interactable = interactable && skill2Bt.gameObject.activeInHierarchy; }
        if (skill3Bt != null) { skill3Bt.interactable = interactable && skill3Bt.gameObject.activeInHierarchy; }
        if (skill4Bt != null) { skill4Bt.interactable = interactable && skill4Bt.gameObject.activeInHierarchy; }
    }

    private void RefreshSkillButtonStates()
    {
        Pokemon p = PlayerCur;
        bool baseInteractable = (_isGameOver == false) && (p != null);

        if (skill1Bt != null) { skill1Bt.interactable = baseInteractable && !IsSkillOnCooldown(p, 0); }
        if (skill2Bt != null) { skill2Bt.interactable = baseInteractable && !IsSkillOnCooldown(p, 1); }
        if (skill3Bt != null) { skill3Bt.interactable = baseInteractable && !IsSkillOnCooldown(p, 2); }
        if (skill4Bt != null) { skill4Bt.interactable = baseInteractable && !IsSkillOnCooldown(p, 3); }
    }

    private void WireGameOverButton()
    {
        if (gameOverExitBt == null) { return; }
        gameOverExitBt.onClick.RemoveAllListeners();
        gameOverExitBt.onClick.AddListener(() =>
        {
            ClearSavedSnapshot();
            SceneManager.LoadScene(PokemonGamemanager.SCENE_INDEX_PokemonStart);
        });
    }

    private void LockInputs()
    {
        SetCommandButtonsActive(false);
        SetSkillButtonsInteractable(false);
        if (shopPanel != null) { shopPanel.SetActive(false); }
        if (switchPanel != null) { switchPanel.SetActive(false); }
    }

    private int[] GetOrCreateCooldownArray(Pokemon pokemon)
    {
        if (pokemon == null) { return null; }
        if (_skillCooldowns.TryGetValue(pokemon, out int[] arr)) { return arr; }

        arr = new int[4];
        _skillCooldowns[pokemon] = arr;
        return arr;
    }

    private bool IsSkillOnCooldown(Pokemon pokemon, int skillIndex)
    {
        if (pokemon == null) { return false; }
        if (skillIndex < 0) { return false; }
        int[] arr = GetOrCreateCooldownArray(pokemon);
        if (arr == null) { return false; }
        if (skillIndex >= arr.Length) { return false; }
        return arr[skillIndex] > 0;
    }

    private void RegisterSkillUse(Pokemon pokemon, int skillIndex)
    {
        if (pokemon == null) { return; }
        if (skillIndex < 0) { return; }
        int[] arr = GetOrCreateCooldownArray(pokemon);
        if (arr == null) { return; }
        if (skillIndex >= arr.Length) { return; }

        arr[skillIndex] = 2;

        if (pokemon == PlayerCur)
        {
            RefreshSkillButtonStates();
        }
    }

    private void AdvanceCooldownsForTeam(bool isPlayerTeam)
    {
        List<Pokemon> team = isPlayerTeam ? PokemonGamemanager.PlayerTeam : PokemonGamemanager.EnemyTeam;
        if (team == null) { return; }

        foreach (Pokemon member in team)
        {
            if (member == null) { continue; }
            if (_skillCooldowns.TryGetValue(member, out int[] arr))
            {
                for (int i = 0; i < arr.Length; i = i + 1)
                {
                    if (arr[i] > 0) { arr[i] = arr[i] - 1; }
                }
            }
        }
    }

    private void AdvanceDefenseBuffsForTeam(bool isPlayerTeam)
    {
        List<Pokemon> team = isPlayerTeam ? PokemonGamemanager.PlayerTeam : PokemonGamemanager.EnemyTeam;
        if (team == null) { return; }

        foreach (Pokemon member in team)
        {
            if (member == null) { continue; }
            if (_defenseBuffStates.TryGetValue(member, out DefenseBuffState state))
            {
                state.remainingTurns = Mathf.Max(0, state.remainingTurns - 1);
                if (state.remainingTurns <= 0)
                {
                    RemoveDefenseBuff(member);
                }
            }
        }
    }

    private void BeginPlayerTurn(bool advanceCounters)
    {
        if (_isGameOver) { return; }

        CheckAndHandleGameOver();
        if (_isGameOver) { return; }

        if (switchPanel != null) { switchPanel.SetActive(false); }
        if (shopPanel != null) { shopPanel.SetActive(false); }

        if (advanceCounters)
        {
            AdvanceCooldownsForTeam(true);
            AdvanceDefenseBuffsForTeam(true);
        }

        RefreshInfos();
        RefreshRoundLabel();
        RefreshSkillButtonLabelsFromPlayer();
        RefreshSwitchButtonLabels();

        SetCommandButtonsActive(true);
        SetSkillButtonsInteractable(true);
        RefreshSkillButtonStates();

        SaveRoundSnapshotToPrefs();

        RefreshSkillButtonStates();
        if (switchBt1 != null) { switchBt1.onClick.RemoveAllListeners(); switchBt1.onClick.AddListener(OnClickSwitch1); }
    }

    /// <summary>
    /// 오브젝트 표시 제어
    /// </summary>
    // 시작시 상점 및 교체 패널 비활성화
    private void HideAllSubPanelsAtStart()
    {
        if (shopPanel != null) { shopPanel.SetActive(false); }
        if (switchPanel != null) { switchPanel.SetActive(false); }
    }

    private void SetCommandButtonsActive(bool active)
    {
        if (attackBt != null) { GameObject g = attackBt.gameObject; if (g != null) { g.SetActive(active ? true : false); } }
        if (bagBt != null) { GameObject g = bagBt.gameObject; if (g != null) { g.SetActive(active ? true : false); } }
        if (battleSkillBt != null) { GameObject g = battleSkillBt.gameObject; if (g != null) { g.SetActive(active ? true : false); } }
        if (pokemonListBt != null) { GameObject g = pokemonListBt.gameObject; if (g != null) { g.SetActive(active ? true : false); } }
    }

    private void RefreshInfos()
    {
        Pokemon p = PlayerCur;
        Pokemon e = EnemyCur;

        if (myInfo != null) { myInfo.Bind(p); }
        if (otherInfo != null) { otherInfo.Bind(e); }
    }

    private void RefreshRoundLabel()
    {
        if (roundText != null) { roundText.text = "Round " + round.ToString(); }
    }

    private void RefreshSkillButtonLabelsFromPlayer()
    {
        Pokemon p = PlayerCur;
        if (p == null) { return; }

        string n0 = "";
        string n1 = "";
        string n2 = "";
        string n3 = "";

        if (p.skillNames != null)
        {
            if (p.skillNames.Length > 0) { n0 = p.skillNames[0]; }
            if (p.skillNames.Length > 1) { n1 = p.skillNames[1]; }
            if (p.skillNames.Length > 2) { n2 = p.skillNames[2]; }
            if (p.skillNames.Length > 3) { n3 = p.skillNames[3]; }
        }

        SafeSetButtonText(skill1Bt, n0);
        SafeSetButtonText(skill2Bt, n1);
        SafeSetButtonText(skill3Bt, n2);
        SafeSetButtonText(skill4Bt, n3);
    }

    private void SafeSetButtonText(Button bt, string txt)
    {
        if (bt == null) { return; }
        TextMeshProUGUI t = bt.GetComponentInChildren<TextMeshProUGUI>();
        if (t == null) { return; }
        t.text = (txt == null) ? "" : txt;
    }

    /// <summary>
    /// 교체(2슬롯) 관련
    /// </summary>
    private int _switchSlot0Index = -1;
    private int _switchSlot1Index = -1;

    private void RefreshSwitchButtonLabels()
    {
        List<Pokemon> team = PokemonGamemanager.PlayerTeam;
        if (team == null) { return; }

        List<int> cands = new List<int>();
        int i = 0;
        while (i < team.Count)
        {
            if (i != playerIndex)
            {
                Pokemon p = team[i];
                if (p != null)
                {
                    if (p.Hp > 0)
                    {
                        cands.Add(i);
                        if (cands.Count == 2) { i = team.Count; }
                    }
                }
            }
            i = i + 1;
        }

        string s0 = "";
        string s1 = "";

        if (cands.Count > 0)
        {
            Pokemon p0 = team[cands[0]];
            if (p0 != null) { s0 = p0.name; }
        }
        if (cands.Count > 1)
        {
            Pokemon p1 = team[cands[1]];
            if (p1 != null) { s1 = p1.name; }
        }

        if (switchBt0Text != null) { switchBt0Text.text = s0; }
        if (switchBt1Text != null) { switchBt1Text.text = s1; }

        _switchSlot0Index = (cands.Count > 0) ? cands[0] : -1;
        _switchSlot1Index = (cands.Count > 1) ? cands[1] : -1;

        bool hasSlot0 = _switchSlot0Index >= 0;
        bool hasSlot1 = _switchSlot1Index >= 0;

        if (switchBt0 != null) { switchBt0.interactable = hasSlot0; }
        if (switchBt1 != null) { switchBt1.interactable = hasSlot1; }
    }

    private void OnClickSwitch0()
    {
        if (_isGameOver) { return; }
        if (_switchSlot0Index < 0) { return; }
        LockInputs();
        StartCoroutine(CoPlayerSwitchThenEnemy(_switchSlot0Index));
    }

    private void OnClickSwitch1()
    {
        if (_isGameOver) { return; }
        if (_switchSlot1Index < 0) { return; }
        LockInputs();
        StartCoroutine(CoPlayerSwitchThenEnemy(_switchSlot1Index));
    }

    private bool ApplyPlayerSwitch(int toIndex)
    {
        List<Pokemon> team = PokemonGamemanager.PlayerTeam;
        if (team == null) { return false; }
        if (toIndex < 0) { return false; }
        if (toIndex >= team.Count) { return false; }

        Pokemon cand = team[toIndex];
        if (cand == null) { return false; }
        if (cand.Hp <= 0) { return false; }

        playerIndex = toIndex;

        RefreshInfos();
        RefreshSkillButtonLabelsFromPlayer();
        RefreshSwitchButtonLabels();

        return true;
    }

    private void OnClickAttack()
    {
        if (_isGameOver) { return; }
        if (PlayerCur == null) { return; }
        if (EnemyCur == null) { return; }

        LockInputs();
        StartCoroutine(CoPlayerAttackThenEnemy());
    }

    private void OnClickBag()
    {
        if (_isGameOver) { return; }
        if (shopPanel != null) { shopPanel.SetActive(true); }
    }

    private void OnClickBattleSkillOpen()
    {
        if (_isGameOver) { return; }
        LogNow("Select a skill");
    }

    private void OnClickOpenSwitchPanel()
    {
        if (_isGameOver) { return; }
        if (switchPanel != null)
        {
            RefreshSwitchButtonLabels();
            switchPanel.SetActive(true);
        }
    }

    /// <summary>
    ///     1번 스킬 버튼 클릭 처리
    /// </summary>
    private void OnClickSkill1()
    {
        OnClickSkillIndex(0);
    }

    /// <summary>
    ///     2번 스킬 버튼 클릭 처리
    /// </summary>
    private void OnClickSkill2()
    {
        OnClickSkillIndex(1);
    }

    /// <summary>
    ///     3번 스킬 버튼 클릭 처리
    /// </summary>
    private void OnClickSkill3()
    {
        OnClickSkillIndex(2);
    }

    /// <summary>
    ///     4번 스킬 버튼 클릭 처리
    /// </summary>
    private void OnClickSkill4()
    {
        OnClickSkillIndex(3);
    }

    /// <summary>
    ///     플레이어 스킬 선택 공통 처리
    /// </summary>
    private void OnClickSkillIndex(int idx)
    {
        if (_isGameOver) { return; }
        if (PlayerCur == null) { return; }
        if (EnemyCur == null) { return; }

        if (IsSkillOnCooldown(PlayerCur, idx))
        {
            RefreshSkillButtonStates();
            LogNow("That skill is on cooldown.");
            return;
        }

        LockInputs();
        StartCoroutine(CoPlayerUseSkillThenEnemy(idx));
    }

    private IEnumerator CoPlayerSwitchThenEnemy(int toIndex)
    {
        bool switched = ApplyPlayerSwitch(toIndex);
        if (!switched)
        {
            BeginPlayerTurn(false);
            yield break;
        }

        if (switchPanel != null) { switchPanel.SetActive(false); }
        string switchedName = (PlayerCur != null) ? PlayerCur.name : "a new Pokemon";
        LogNow("Player switched to " + switchedName + "!");

        yield return new WaitForSeconds(0.35f);

        if (!_isGameOver && !IsEnemyAllDown())
        {
            yield return StartCoroutine(CoEnemyActionAuto());
        }

        if (!_isGameOver)
        {
            BeginPlayerTurn(true);
        }
    }

    private IEnumerator CoPlayerAttackThenEnemy()
    {
        if (PlayerCur == null) { yield break; }
        if (EnemyCur == null) { yield break; }

        LogNow("Player normal attack");

        if (myInfo != null) { yield return StartCoroutine(myInfo.NormalAttackSequence(otherInfo)); }

        yield return StartCoroutine(PlayerCur.Attack(EnemyCur, -1));

        TryApplyCounterDamage(EnemyCur, PlayerCur);

        yield return StartCoroutine(AfterAnyDamageAndCheckKOs());

        if (_isGameOver) { yield break; }

        if (!IsEnemyAllDown())
        {
            yield return StartCoroutine(CoEnemyActionAuto());
        }

        if (!_isGameOver)
        {
            BeginPlayerTurn(true);
        }
    }

    private IEnumerator CoPlayerUseSkillThenEnemy(int skillIndex)
    {
        if (PlayerCur == null) { yield break; }
        if (EnemyCur == null) { yield break; }

        LogNow("Player used a skill");

        bool isMelee = false;
        bool isRanged = false;
        bool isHeal = false;
        bool isDefense = false;
        bool hasSkill = false;

        if (PlayerCur.skillTypeBehaviours != null && skillIndex >= 0 && skillIndex < PlayerCur.skillTypeBehaviours.Length)
        {
            SkillTpye model = PlayerCur.skillTypeBehaviours[skillIndex];
            if (model != null)
            {
                hasSkill = true;
                if (model is MeleeAttackType) { isMelee = true; }
                else if (model is RangedAttackType) { isRanged = true; }
                else if (model is HealType) { isHeal = true; }
                else if (model is DefenseType) { isDefense = true; }
            }
        }

        if (hasSkill)
        {
            RegisterSkillUse(PlayerCur, skillIndex);
        }

        if (isMelee)
        {
            if (myInfo != null) { yield return StartCoroutine(myInfo.MeleeSkillSequence(otherInfo, skillIndex)); }
        }
        else if (isRanged)
        {
            if (myInfo != null) { yield return StartCoroutine(myInfo.RangedSkillSequence(otherInfo, skillIndex)); }
        }
        else if (isHeal)
        {
            if (myInfo != null) { yield return StartCoroutine(myInfo.HealSkillSequence(skillIndex)); }
        }
        else if (isDefense)
        {
            if (myInfo != null) { yield return StartCoroutine(myInfo.DefenseSkillSequence(skillIndex)); }
        }
        else
        {
            if (myInfo != null) { yield return new WaitForSeconds(0.25f); }
        }

        yield return StartCoroutine(PlayerCur.Attack(EnemyCur, skillIndex));

        TryApplyCounterDamage(EnemyCur, PlayerCur);

        yield return StartCoroutine(AfterAnyDamageAndCheckKOs());

        if (_isGameOver) { yield break; }

        if (!IsEnemyAllDown())
        {
            yield return StartCoroutine(CoEnemyActionAuto());
        }

        if (!_isGameOver)
        {
            BeginPlayerTurn(true);
        }
    }

    private IEnumerator CoEnemyActionAuto()
    {
        if (_isGameOver) { yield break; }
        if (EnemyCur == null) { yield break; }
        if (PlayerCur == null) { yield break; }

        AdvanceCooldownsForTeam(false);
        AdvanceDefenseBuffsForTeam(false);

        EnemyDecision decision = DecideEnemyAction();

        if (decision.actionType == EnemyActionType.Switch)
        {
            bool switched = ApplyEnemySwitch(decision.switchToIndex);
            if (switched)
            {
                LogNow("Enemy switched Pokemon!");
                yield return new WaitForSeconds(0.5f);
            }
            yield break;
        }

        if (decision.actionType == EnemyActionType.Skill)
        {
            yield return StartCoroutine(CoEnemyUseSkill(decision.skillIndex));
            yield break;
        }

        yield return StartCoroutine(CoEnemyNormalAttack());
    }

    private IEnumerator CoEnemyNormalAttack()
    {
        if (EnemyCur == null) { yield break; }
        if (PlayerCur == null) { yield break; }

        LogNow("Enemy attack!");

        if (otherInfo != null) { yield return StartCoroutine(otherInfo.NormalAttackSequence(myInfo)); }

        yield return StartCoroutine(EnemyCur.Attack(PlayerCur, -1));

        TryApplyCounterDamage(PlayerCur, EnemyCur);

        yield return StartCoroutine(AfterAnyDamageAndCheckKOs());
    }

    private IEnumerator CoEnemyUseSkill(int skillIndex)
    {
        if (EnemyCur == null) { yield break; }
        if (PlayerCur == null) { yield break; }

        bool isMelee = false;
        bool isRanged = false;
        bool isHeal = false;
        bool isDefense = false;
        bool hasSkill = false;

        if (EnemyCur.skillTypeBehaviours != null)
        {
            if (skillIndex >= 0 && skillIndex < EnemyCur.skillTypeBehaviours.Length)
            {
                SkillTpye model = EnemyCur.skillTypeBehaviours[skillIndex];
                if (model != null)
                {
                    hasSkill = true;
                    if (model is MeleeAttackType) { isMelee = true; }
                    else if (model is RangedAttackType) { isRanged = true; }
                    else if (model is HealType) { isHeal = true; }
                    else if (model is DefenseType) { isDefense = true; }
                }
            }
        }

        if (!hasSkill)
        {
            yield return StartCoroutine(CoEnemyNormalAttack());
            yield break;
        }

        RegisterSkillUse(EnemyCur, skillIndex);

        string skillName = "a skill";
        if (EnemyCur.skillNames != null)
        {
            if (skillIndex >= 0 && skillIndex < EnemyCur.skillNames.Length)
            {
                string candidate = EnemyCur.skillNames[skillIndex];
                if (!string.IsNullOrEmpty(candidate)) { skillName = candidate; }
            }
        }
        LogNow("Enemy used " + skillName + "!");

        if (isMelee)
        {
            if (otherInfo != null) { yield return StartCoroutine(otherInfo.MeleeSkillSequence(myInfo, skillIndex)); }
        }
        else if (isRanged)
        {
            if (otherInfo != null) { yield return StartCoroutine(otherInfo.RangedSkillSequence(myInfo, skillIndex)); }
        }
        else if (isHeal)
        {
            if (otherInfo != null) { yield return StartCoroutine(otherInfo.HealSkillSequence(skillIndex)); }
        }
        else if (isDefense)
        {
            if (otherInfo != null) { yield return StartCoroutine(otherInfo.DefenseSkillSequence(skillIndex)); }
        }
        else
        {
            if (otherInfo != null) { yield return new WaitForSeconds(0.25f); }
        }

        yield return StartCoroutine(EnemyCur.Attack(PlayerCur, skillIndex));

        TryApplyCounterDamage(PlayerCur, EnemyCur);

        yield return StartCoroutine(AfterAnyDamageAndCheckKOs());
    }

    private bool ApplyEnemySwitch(int toIndex)
    {
        List<Pokemon> team = PokemonGamemanager.EnemyTeam;
        if (team == null) { return false; }
        if (toIndex < 0) { return false; }
        if (toIndex >= team.Count) { return false; }

        Pokemon cand = team[toIndex];
        if (cand == null) { return false; }
        if (cand.Hp <= 0) { return false; }

        enemyIndex = toIndex;
        RefreshInfos();
        return true;
    }

    private EnemyDecision DecideEnemyAction()
    {
        EnemyDecision decision = new EnemyDecision
        {
            actionType = EnemyActionType.NormalAttack,
            skillIndex = -1,
            switchToIndex = -1
        };

        Pokemon enemy = EnemyCur;
        Pokemon player = PlayerCur;
        if (enemy == null || player == null)
        {
            return decision;
        }

        float playerAdvantage = Pokemon.battleType[(int)player.type, (int)enemy.type];

        if (enemy.Hp <= Mathf.Max(20, enemy.def * 2))
        {
            if (!IsSkillOnCooldown(enemy, 3))
            {
                decision.actionType = EnemyActionType.Skill;
                decision.skillIndex = 3;
                return decision;
            }
        }

        if (playerAdvantage > 1.4f)
        {
            int switchIdx = FindEnemySwitchCandidate(playerAdvantage);
            if (switchIdx >= 0)
            {
                decision.actionType = EnemyActionType.Switch;
                decision.switchToIndex = switchIdx;
                return decision;
            }

            if (!_defenseBuffStates.ContainsKey(enemy) && !IsSkillOnCooldown(enemy, 2))
            {
                decision.actionType = EnemyActionType.Skill;
                decision.skillIndex = 2;
                return decision;
            }
        }

        int attackSkill = ChooseEnemyAttackSkill();
        if (attackSkill >= 0)
        {
            decision.actionType = EnemyActionType.Skill;
            decision.skillIndex = attackSkill;
            return decision;
        }

        return decision;
    }

    private int FindEnemySwitchCandidate(float currentThreat)
    {
        List<Pokemon> team = PokemonGamemanager.EnemyTeam;
        if (team == null) { return -1; }
        if (PlayerCur == null) { return -1; }

        int bestIndex = -1;
        float bestMultiplier = currentThreat;
        for (int i = 0; i < team.Count; i = i + 1)
        {
            if (i == enemyIndex) { continue; }
            Pokemon cand = team[i];
            if (cand == null) { continue; }
            if (cand.Hp <= 0) { continue; }

            float mult = Pokemon.battleType[(int)PlayerCur.type, (int)cand.type];
            if (mult < bestMultiplier - 0.1f)
            {
                bestMultiplier = mult;
                bestIndex = i;
            }
            else if (Mathf.Abs(mult - bestMultiplier) < 0.05f)
            {
                if (EnemyCur != null && cand.Hp > EnemyCur.Hp + 15)
                {
                    bestMultiplier = mult;
                    bestIndex = i;
                }
            }
        }
        return bestIndex;
    }

    private int ChooseEnemyAttackSkill()
    {
        Pokemon enemy = EnemyCur;
        Pokemon player = PlayerCur;
        if (enemy == null || player == null) { return -1; }

        float baseDamage = ComputeExpectedDamage(enemy, player, -1);
        int bestSkill = -1;
        float bestDamage = baseDamage;

        for (int i = 0; i < 2; i = i + 1)
        {
            if (IsSkillOnCooldown(enemy, i)) { continue; }
            float dmg = ComputeExpectedDamage(enemy, player, i);
            if (dmg > bestDamage * 1.05f)
            {
                bestDamage = dmg;
                bestSkill = i;
            }
        }

        return bestSkill;
    }

    private float ComputeExpectedDamage(Pokemon attacker, Pokemon defender, int skillIndex)
    {
        if (attacker == null || defender == null) { return 0f; }

        float typeMul = Pokemon.battleType[(int)attacker.type, (int)defender.type];
        float raw = attacker.atk - (defender.def * 0.5f);
        if (raw < 1f) { raw = 1f; }
        float dmg = raw * typeMul;
        int adjustedDamage = (int)((dmg <= 0f) ? 1f : dmg);

        if (skillIndex >= 0 && attacker.skillTypeBehaviours != null)
        {
            if (skillIndex < attacker.skillTypeBehaviours.Length)
            {
                SkillTpye model = attacker.skillTypeBehaviours[skillIndex];
                if (model != null)
                {
                    adjustedDamage = model.ComputeDamageOverride(attacker, defender, adjustedDamage);
                }
            }
        }

        return adjustedDamage;
    }

    public void ApplyDefenseBuffRuntime(Pokemon target, int amount, int durationTurns)
    {
        if (target == null) { return; }
        if (amount <= 0) { return; }
        if (durationTurns < 1) { durationTurns = 1; }

        if (_defenseBuffStates.TryGetValue(target, out DefenseBuffState existing))
        {
            target.def = Mathf.Max(0, target.def - existing.amount);
            _defenseBuffStates.Remove(target);
        }

        target.def = target.def + amount;
        _defenseBuffStates[target] = new DefenseBuffState { amount = amount, remainingTurns = durationTurns };
    }

    private void RestoreDefenseBuffState(Pokemon target, int amount, int remainingTurns)
    {
        if (target == null) { return; }
        _defenseBuffStates[target] = new DefenseBuffState { amount = amount, remainingTurns = remainingTurns };
    }

    private void RemoveDefenseBuff(Pokemon target)
    {
        if (target == null) { return; }
        if (_defenseBuffStates.TryGetValue(target, out DefenseBuffState state))
        {
            target.def = Mathf.Max(0, target.def - state.amount);
            _defenseBuffStates.Remove(target);
        }
    }

    private void ClearStateForPokemon(Pokemon target)
    {
        if (target == null) { return; }
        _skillCooldowns.Remove(target);
        RemoveDefenseBuff(target);
    }

    public bool TryApplyCounterDamage(Pokemon defender, Pokemon attacker)
    {
        if (defender == null || attacker == null) { return false; }
        if (!_defenseBuffStates.TryGetValue(defender, out DefenseBuffState state)) { return false; }
        if (state.remainingTurns <= 0) { return false; }
        if (Random.value > 0.2f) { return false; }

        int dmg = Mathf.Max(1, Mathf.RoundToInt(attacker.Hp * 0.1f));
        attacker.Hp = attacker.Hp - dmg;
        LogNow(defender.name + " countered! " + attacker.name + " took " + dmg + " damage.");
        return true;
    }

    private void ShowGameOver(string message)
    {
        if (_isGameOver) { return; }
        _isGameOver = true;

        SetCommandButtonsActive(false);
        SetSkillButtonsInteractable(false);
        if (shopPanel != null) { shopPanel.SetActive(false); }
        if (switchPanel != null) { switchPanel.SetActive(false); }

        if (gameOverPanel != null) { gameOverPanel.SetActive(true); }
        if (gameOverText != null) { gameOverText.text = message; }

        ClearSavedSnapshot();
    }

    private void CheckAndHandleGameOver()
    {
        if (_isGameOver) { return; }

        bool playerDown = PokemonGamemanager.FirstAliveIndex(PokemonGamemanager.PlayerTeam) < 0;
        bool enemyDown = IsEnemyAllDown();

        if (playerDown)
        {
            ShowGameOver("Defeat...");
            return;
        }

        if (round >= 5)
        {
            string message = enemyDown ? "All rounds cleared!" : "Reached round 5! Challenge complete.";
            ShowGameOver(message);
        }
    }

    private void SaveRoundSnapshotToPrefs()
    {
        if (_isGameOver) { return; }
        RoundSnapshot snapshot = GetRoundSnapshot();
        if (snapshot == null) { return; }

        string json = JsonUtility.ToJson(snapshot);
        PlayerPrefs.SetString(ROUND_SNAPSHOT_PREF_KEY, json);
        PlayerPrefs.Save();
    }

    private void LoadRoundSnapshotIfAvailable()
    {
        if (!PlayerPrefs.HasKey(ROUND_SNAPSHOT_PREF_KEY)) { return; }
        string json = PlayerPrefs.GetString(ROUND_SNAPSHOT_PREF_KEY, string.Empty);
        if (string.IsNullOrEmpty(json)) { return; }

        try
        {
            RoundSnapshot snapshot = JsonUtility.FromJson<RoundSnapshot>(json);
            if (snapshot != null)
            {
                SetRoundFromSave(snapshot);
            }
        }
        catch
        {
        }
    }

    public RoundSnapshot GetRoundSnapshot()
    {
        if (PokemonGamemanager.PlayerTeam == null) { return null; }
        if (PokemonGamemanager.EnemyTeam == null) { return null; }

        RoundSnapshot snapshot = new RoundSnapshot();
        snapshot.round = round;
        snapshot.playerIndex = playerIndex;
        snapshot.enemyIndex = enemyIndex;

        snapshot.playerHp = BuildHpArray(PokemonGamemanager.PlayerTeam);
        snapshot.enemyHp = BuildHpArray(PokemonGamemanager.EnemyTeam);

        List<SkillCooldownSnapshot> cooldowns = new List<SkillCooldownSnapshot>();
        AppendCooldownSnapshots(PokemonGamemanager.PlayerTeam, true, cooldowns);
        AppendCooldownSnapshots(PokemonGamemanager.EnemyTeam, false, cooldowns);
        snapshot.cooldowns = cooldowns.ToArray();

        List<DefenseBuffSnapshot> buffSnaps = new List<DefenseBuffSnapshot>();
        foreach (var kv in _defenseBuffStates)
        {
            Pokemon key = kv.Key;
            DefenseBuffState value = kv.Value;
            int teamIndex = FindPokemonIndex(PokemonGamemanager.PlayerTeam, key);
            bool isPlayer = true;
            if (teamIndex < 0)
            {
                teamIndex = FindPokemonIndex(PokemonGamemanager.EnemyTeam, key);
                isPlayer = false;
            }
            if (teamIndex < 0) { continue; }

            DefenseBuffSnapshot snap = new DefenseBuffSnapshot
            {
                isPlayer = isPlayer,
                teamIndex = teamIndex,
                amount = value.amount,
                remainingTurns = value.remainingTurns
            };
            buffSnaps.Add(snap);
        }
        snapshot.defenseBuffs = buffSnaps.ToArray();

        return snapshot;
    }

    public void SetRoundFromSave(RoundSnapshot snapshot)
    {
        if (snapshot == null) { return; }

        round = snapshot.round;
        playerIndex = snapshot.playerIndex;
        enemyIndex = snapshot.enemyIndex;

        ApplyHpArray(PokemonGamemanager.PlayerTeam, snapshot.playerHp);
        ApplyHpArray(PokemonGamemanager.EnemyTeam, snapshot.enemyHp);

        _skillCooldowns.Clear();
        if (snapshot.cooldowns != null)
        {
            foreach (SkillCooldownSnapshot entry in snapshot.cooldowns)
            {
                List<Pokemon> team = entry.isPlayer ? PokemonGamemanager.PlayerTeam : PokemonGamemanager.EnemyTeam;
                if (team == null) { continue; }
                if (entry.teamIndex < 0 || entry.teamIndex >= team.Count) { continue; }
                Pokemon target = team[entry.teamIndex];
                if (target == null) { continue; }

                int[] arr = GetOrCreateCooldownArray(target);
                if (arr == null) { continue; }
                for (int i = 0; i < arr.Length && i < entry.cooldowns.Length; i = i + 1)
                {
                    arr[i] = entry.cooldowns[i];
                }
            }
        }

        _defenseBuffStates.Clear();
        if (snapshot.defenseBuffs != null)
        {
            foreach (DefenseBuffSnapshot entry in snapshot.defenseBuffs)
            {
                List<Pokemon> team = entry.isPlayer ? PokemonGamemanager.PlayerTeam : PokemonGamemanager.EnemyTeam;
                if (team == null) { continue; }
                if (entry.teamIndex < 0 || entry.teamIndex >= team.Count) { continue; }
                Pokemon target = team[entry.teamIndex];
                if (target == null) { continue; }
                RestoreDefenseBuffState(target, entry.amount, entry.remainingTurns);
            }
        }

        RefreshInfos();
        RefreshRoundLabel();
        RefreshSkillButtonLabelsFromPlayer();
        RefreshSwitchButtonLabels();
    }

    private int[] BuildHpArray(List<Pokemon> team)
    {
        if (team == null) { return new int[0]; }
        int[] arr = new int[team.Count];
        for (int i = 0; i < team.Count; i = i + 1)
        {
            Pokemon p = team[i];
            arr[i] = (p != null) ? p.Hp : 0;
        }
        return arr;
    }

    private void ApplyHpArray(List<Pokemon> team, int[] values)
    {
        if (team == null) { return; }
        if (values == null) { return; }
        int count = Mathf.Min(team.Count, values.Length);
        for (int i = 0; i < count; i = i + 1)
        {
            Pokemon p = team[i];
            if (p == null) { continue; }
            p.Hp = values[i];
        }
    }

    private void AppendCooldownSnapshots(List<Pokemon> team, bool isPlayer, List<SkillCooldownSnapshot> snapshots)
    {
        if (team == null) { return; }
        for (int i = 0; i < team.Count; i = i + 1)
        {
            Pokemon p = team[i];
            if (p == null) { continue; }
            int[] arr = GetOrCreateCooldownArray(p);
            SkillCooldownSnapshot snap = new SkillCooldownSnapshot
            {
                isPlayer = isPlayer,
                teamIndex = i
            };
            if (arr != null)
            {
                for (int j = 0; j < snap.cooldowns.Length && j < arr.Length; j = j + 1)
                {
                    snap.cooldowns[j] = arr[j];
                }
            }
            snapshots.Add(snap);
        }
    }

    private int FindPokemonIndex(List<Pokemon> team, Pokemon target)
    {
        if (team == null) { return -1; }
        for (int i = 0; i < team.Count; i = i + 1)
        {
            if (team[i] == target) { return i; }
        }
        return -1;
    }

    private void ClearSavedSnapshot()
    {
        if (PlayerPrefs.HasKey(ROUND_SNAPSHOT_PREF_KEY))
        {
            PlayerPrefs.DeleteKey(ROUND_SNAPSHOT_PREF_KEY);
            PlayerPrefs.Save();
        }
    }

                Pokemon defeated = EnemyCur;
                ClearStateForPokemon(defeated);

                    LogNow("Enemy switched");
                    LogNow("Round cleared");

                Pokemon defeatedPlayer = PlayerCur;
                ClearStateForPokemon(defeatedPlayer);

                    LogNow("Auto switch");
                    LogNow("Player party defeated");
        CheckAndHandleGameOver();

        bool isHeal = false;
        bool isDefense = false;

        if (PlayerCur != null)
        {
            if (PlayerCur.skillTypeBehaviours != null)
            {
                if (skillIndex >= 0)
                {
                    if (skillIndex < PlayerCur.skillTypeBehaviours.Length)
                    {
                        SkillTpye model = PlayerCur.skillTypeBehaviours[skillIndex];
                        if (model != null)
                        {
                            if (model is MeleeAttackType) { isMelee = true; }
                            else
                            {
                                if (model is RangedAttackType) { isRanged = true; }
                                else
                                {
                                    if (model is HealType) { isHeal = true; }
                                    else
                                    {
                                        if (model is DefenseType) { isDefense = true; }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        // 스킬별 연출 판별
        if (isMelee)
        {
            if (myInfo != null) { yield return StartCoroutine(myInfo.MeleeSkillSequence(otherInfo, skillIndex)); }
        }
        else
        {
            if (isRanged)
            {
                if (myInfo != null) { yield return StartCoroutine(myInfo.RangedSkillSequence(otherInfo, skillIndex)); }
            }
            else
            {
                if (isHeal)
                {
                    if (myInfo != null) { yield return StartCoroutine(myInfo.HealSkillSequence(skillIndex)); }
                }
                else
                {
                    if (isDefense)
                    {
                        if (myInfo != null) { yield return StartCoroutine(myInfo.DefenseSkillSequence(skillIndex)); }
                    }
                    else
                    {
                        if (myInfo != null) { yield return new WaitForSeconds(0.25f); }
                    }
                }
            }
        }

        // 대미지/효과 계산
        yield return StartCoroutine(PlayerCur.Attack(EnemyCur, skillIndex));

        // KO/라운드 판정시 실행
        yield return StartCoroutine(AfterAnyDamageAndCheckKOs());

        // 적 행동
        if (!IsEnemyAllDown()) { yield return StartCoroutine(CoEnemyActionAuto()); }

        SetCommandButtonsActive(true);
    }

    private IEnumerator CoEnemyActionAuto()
    {
        if (EnemyCur == null) { yield break; }
        if (PlayerCur == null) { yield break; }

        LogNow("적의 공격");

        // 연출
        if (otherInfo != null) { yield return StartCoroutine(otherInfo.NormalAttackSequence(myInfo)); }

        // 대미지
        yield return StartCoroutine(EnemyCur.Attack(PlayerCur, -1));

        // KO/라운드
        yield return StartCoroutine(AfterAnyDamageAndCheckKOs());
    }

    /// <summary>
    /// KO/라운드 종료
    /// </summary>
    private IEnumerator AfterAnyDamageAndCheckKOs()
    {
        // 적 다운 -> 교체 또는 라운드 증가
        if (EnemyCur != null)
        {
            if (EnemyCur.Hp <= 0)
            {
                int nextEnemy = NextAliveIndex(PokemonGamemanager.EnemyTeam, enemyIndex);
                if (nextEnemy >= 0)
                {
                    enemyIndex = nextEnemy;
                    RefreshInfos();
                    LogNow("적 교체");
                }
                else
                {
                    round = round + 1;
                    RefreshRoundLabel();
                    LogNow("라운드 클리어");
                }
            }
        }

        // 플레이어 다운 -> 자동 교체
        if (PlayerCur != null)
        {
            if (PlayerCur.Hp <= 0)
            {
                int nextPlayer = NextAliveIndex(PokemonGamemanager.PlayerTeam, playerIndex);
                if (nextPlayer >= 0)
                {
                    playerIndex = nextPlayer;
                    RefreshInfos();
                    RefreshSkillButtonLabelsFromPlayer();
                    RefreshSwitchButtonLabels();
                    LogNow("자동 교체");
                }
                else
                {
                    LogNow("플레이어 전멸");
                }
            }
        }

        yield return null;
    }

    private int NextAliveIndex(List<Pokemon> team, int after)
    {
        if (team == null) { return -1; }
        int count = team.Count;
        int i = 0;
        while (i < count)
        {
            int idx = i;
            if (idx != after)
            {
                Pokemon p = team[idx];
                if (p != null)
                {
                    if (p.Hp > 0) { return idx; }
                }
            }
            i = i + 1;
        }
        return -1;
    }

    private bool IsEnemyAllDown()
    {
        List<Pokemon> team = PokemonGamemanager.EnemyTeam;
        if (team == null) { return true; }
        int i = 0;
        while (i < team.Count)
        {
            Pokemon p = team[i];
            if (p != null)
            {
                if (p.Hp > 0) { return false; }
            }
            i = i + 1;
        }
        return true;
    }

    // 텍스트 로그 탐색
    private void LogNow(string s)
    {
        if (textLog == null) { return; }
        textLog.text = s;
    }
}
