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
        SetCommandButtonsActive(true);

        if (myInfo != null) { myInfo.isPlayerSide = true; }
        if (otherInfo != null) { otherInfo.isPlayerSide = false; }
        RefreshInfos();
        RefreshRoundLabel();
        RefreshSkillButtonLabelsFromPlayer();
        RefreshSwitchButtonLabels();

        LogNow("Round " + round.ToString() + " 시작");
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
    // 교체할 포켓몬 선택 버튼
    private void WireSwitchButtons()
    {
        if (switchBt0 != null) { switchBt0.onClick.RemoveAllListeners(); switchBt0.onClick.AddListener(OnClickSwitch0); }
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
    }

    private void OnClickSwitch0()
    {
        if (_switchSlot0Index < 0) { return; }
        ApplyPlayerSwitch(_switchSlot0Index);
    }

    private void OnClickSwitch1()
    {
        if (_switchSlot1Index < 0) { return; }
        ApplyPlayerSwitch(_switchSlot1Index);
    }

    private void ApplyPlayerSwitch(int toIndex)
    {
        List<Pokemon> team = PokemonGamemanager.PlayerTeam;
        if (team == null) { return; }
        if (toIndex < 0) { return; }
        if (toIndex >= team.Count) { return; }

        Pokemon cand = team[toIndex];
        if (cand == null) { return; }
        if (cand.Hp <= 0) { return; }

        playerIndex = toIndex;

        RefreshInfos();
        RefreshSkillButtonLabelsFromPlayer();
        RefreshSwitchButtonLabels();

        if (switchPanel != null) { switchPanel.SetActive(false); }

        LogNow("포켓몬 교체");
    }

    /// <summary>
    /// 버튼 OnClick이벤트 커맨드
    /// </summary>
    private void OnClickAttack()
    {
        if (PlayerCur == null) { return; }
        if (EnemyCur == null) { return; }
        SetCommandButtonsActive(false);
        StartCoroutine(CoPlayerTurn_NormalAttackThenEnemy());
    }

    private void OnClickBag()
    {
        if (shopPanel != null) { shopPanel.SetActive(true); }
    }

    private void OnClickBattleSkillOpen()
    {
        LogNow("스킬 선택");
    }

    private void OnClickOpenSwitchPanel()
    {
        if (switchPanel != null)
        {
            RefreshSwitchButtonLabels();
            switchPanel.SetActive(true);
        }
    }
    // 스킬 버튼 OnClick이벤트 바인딩
    private void OnClickSkill1() { OnClickSkillIndex(0); }
    private void OnClickSkill2() { OnClickSkillIndex(1); }
    private void OnClickSkill3() { OnClickSkillIndex(2); }
    private void OnClickSkill4() { OnClickSkillIndex(3); }

    private void OnClickSkillIndex(int idx)
    {
        if (PlayerCur == null) { return; }
        if (EnemyCur == null) { return; }
        SetCommandButtonsActive(false);
        StartCoroutine(CoPlayerTurn_SkillThenEnemy(idx));
    }

    /// <summary>
    /// 턴 처리
    /// </summary>
    private IEnumerator CoPlayerTurn_NormalAttackThenEnemy()
    {
        LogNow("플레이어 일반공격");

        // 연출
        if (myInfo != null) { yield return StartCoroutine(myInfo.NormalAttackSequence(otherInfo)); }

        // 대미지 처리
        yield return StartCoroutine(PlayerCur.Attack(EnemyCur, -1));

        // KO/라운드 판정
        yield return StartCoroutine(AfterAnyDamageAndCheckKOs());

        // 적 행동 랜덤으로 처리
        if (!IsEnemyAllDown()) { yield return StartCoroutine(CoEnemyActionAuto()); }

        SetCommandButtonsActive(true);
    }

    private IEnumerator CoPlayerTurn_SkillThenEnemy(int skillIndex)
    {
        LogNow("플레이어 스킬 사용");

        // 스킬별 타입 판별(연출 시그니처 위함)
        bool isMelee = false;
        bool isRanged = false;
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
