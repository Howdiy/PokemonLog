using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using UnityEngine.U2D;
using Random = UnityEngine.Random;

public class PokemonGamemanager : MonoBehaviour
{
    /// <summary> @ Setting 프리팹/연결 </summary>
    public GameObject settingsPrefab;
    public Transform settingsParentHint;
    public Transform settingsUiRootHint;

    // @ UI
    public Transform uiRoot;
    public TextMeshProUGUI titleText;
    public Button[] bts;
    [SerializeField] private Button goBattleBt;     // @ goBattle 버튼(프리팹 아님, Canvas 자식)

    public Button StartBT;
    public Button ContinueBT;
    public Button ExitBT;

    // @ 씬 인덱스 상수
    private const int SCENE_INDEX_PokemonStart = 0;  // @ "PokemonStart"
    private const int SCENE_INDEX_PokemonBattle = 1;  // @ "PokemonBattle"
    private const int SCENE_INDEX_PokemonChoices = 2;  // @ "PokemonChoices"

    // @ 저장 플래그 키
    private const string SAVE_FLAG_KEY = "POKEMON_SAVE_FLAG_V1";

    // @ 선택 결과 및 팀(트레이너가 관리)
    public static PokemonTrainer playerTrainer = new PokemonTrainer("Player", 3);
    public static PokemonTrainer enemyTrainer = new PokemonTrainer("Enemy", 3);

    // @ 전투에 전달할 현재 포인터
    public static Pokemon myPokemonG;
    public static Pokemon otherPokemonG;

    // @ 내부 상태
    private static bool _enemyBuiltOnce = false;

    // =========================================================
    // @ DTO
    // =========================================================
    [Serializable]
    public class PokemonDTO
    {
        public int idx;
        public int type;
        public string name;
        public int hp;
        public int atk;
        public int def;
        public int speed;
    }

    [Serializable] // JsonUtility 사용 위해 필요. :contentReference[oaicite:2]{index=2}
    public class SaveDTO
    {
        public PokemonDTO[] player;
        public PokemonDTO[] enemy;
        public int playerActive;
        public int enemyActive;
        public int round;
        public int sceneIndex;
    }

    // @ 저장 경로
    private static string SavePath
    {
        get
        {
            string dir = Application.persistentDataPath;
            string path = Path.Combine(dir, "pokemon_save.json");
            return path;
        }
    }

    public static bool HasSaveData()
    {
        bool fileExists = File.Exists(SavePath);
        if (fileExists)
        {
            if (PlayerPrefs.HasKey(SAVE_FLAG_KEY))
            {
                return true;
            }
        }
        return false;
    }

    private void Awake()
    {
        EnsureSettingPrefabUnderCanvas();
    }

    /// <summary> @ 시작 시 초기 표시/준비 </summary>
    private void Start()
    {
        // @ 선택 씬에서만 goBattle 표시 상태를 갱신
        EnsureEnemyTeamIfNeeded(true);
        UpdateChoiceUI();
        UpdateGoBattleActive();

        // @ 시작 씬일 때만 Start/Continue/Exit 자동 배선
        WireStartMenuButtons();

        // @ PokemonChoices 씬에서 goBattleBt 클릭 배선 보장
        {
            int activeIndex = SceneManager.GetActiveScene().buildIndex; // :contentReference[oaicite:3]{index=3}
            if (activeIndex == SCENE_INDEX_PokemonChoices)
            {
                if (goBattleBt != null)
                {
                    goBattleBt.onClick.RemoveAllListeners();
                    goBattleBt.onClick.AddListener(OnGbtClick);
                }
            }
        }
    }

    private void UpdateContinueButtonAvailability()
    {
        if (ContinueBT == null) { return; }
        bool has = HasSaveData();
        ContinueBT.interactable = has;
        ContinueBT.enabled = has;
        CanvasGroup cg = ContinueBT.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.interactable = has;
            cg.blocksRaycasts = has;
            cg.alpha = has ? 1f : 0.5f;
        }
    }

    /// <summary> @ 텍스트 안내와 선택 수 표시 </summary>
    private void UpdateChoiceUI()
    {
        if (titleText != null)
        {
            int filled = playerTrainer != null ? playerTrainer.CountFilled() : 0;
            titleText.text = "포켓몬 선택\n" +
                             "바깥은 혼자 돌아다니긴 위험하단다\n" +
                             "이 아이들 중 하나를 데려가렴\n" +
                             "(내 팀 " + filled.ToString() + " / 3)";
        }
    }

    /// <summary> @ 적 팀 3마리 무작위 구성(중복 없이 3종) </summary>
    private static void BuildRandomEnemyTeam3()
    {
        if (enemyTrainer == null)
        {
            enemyTrainer = new PokemonTrainer("Enemy", 3);
        }

        List<Pokemon.PokemonIndex> pool = new List<Pokemon.PokemonIndex>();
        pool.Add(Pokemon.PokemonIndex.pikach);
        pool.Add(Pokemon.PokemonIndex.paily);
        pool.Add(Pokemon.PokemonIndex.goBook);
        pool.Add(Pokemon.PokemonIndex.eSang); // 프로젝트 enum에 맞춤

        enemyTrainer.ClearTeam();

        int i = 0;
        while (i < 3)
        {
            if (pool.Count > 0)
            {
                int r = Random.Range(0, pool.Count);
                Pokemon.PokemonIndex pick = pool[r];
                pool.RemoveAt(r);
                Pokemon pokemon = CreateByIndex(pick, false);
                enemyTrainer.SetPokemonAt(i, pokemon);
            }
            i = i + 1;
        }

        otherPokemonG = enemyTrainer.ActivePokemon;
    }

    /// <summary> @ 적 팀 구성 필요 시 1회만 빌드 </summary>
    private static void EnsureEnemyTeamIfNeeded(bool allowBuildEarly)
    {
        if (_enemyBuiltOnce)
        {
            return;
        }

        bool playerFull = false;
        if (playerTrainer != null)
        {
            if (playerTrainer.IsTeamFull())
            {
                playerFull = true;
            }
        }

        if (playerFull)
        {
            BuildRandomEnemyTeam3();
            _enemyBuiltOnce = true;
            return;
        }

        if (allowBuildEarly)
        {
            BuildRandomEnemyTeam3();
            _enemyBuiltOnce = true;
        }
    }

    // =========================================================
    // @ Setting 프리팹 UI 배치
    // =========================================================
    private void EnsureSettingPrefabUnderCanvas()
    {
        if (settingsPrefab == null)
        {
            return;
        }

        GameObject exist = GameObject.Find("Setting(Clone)");
        if (exist == null)
        {
            exist = Instantiate(settingsPrefab);
        }

        Transform parent = settingsParentHint != null ? settingsParentHint : transform;
        exist.transform.SetParent(parent, false);

        Setting go = exist.GetComponent<Setting>();
        if (go != null)
        {
            Transform uiRootTarget = settingsUiRootHint != null ? settingsUiRootHint : uiRoot;
            go.uiRoot = uiRootTarget != null ? uiRootTarget : exist.transform;
        }

        int settingsSortOrder = 5000;

        Canvas goCanvas = exist.GetComponent<Canvas>();
        if (goCanvas == null)
        {
            goCanvas = exist.AddComponent<Canvas>();
        }

        goCanvas.overrideSorting = true;
        goCanvas.sortingOrder = settingsSortOrder;

        settingsRef = exist;
    }

    // =========================================================
    // @ Start 씬 : Start/Continue/Exit 버튼 바인딩
    // =========================================================
    private void WireStartMenuButtons()
    {
        int buildIndex = SceneManager.GetActiveScene().buildIndex;
        if (buildIndex == SCENE_INDEX_PokemonStart)
        {
            if (StartBT != null)
            {
                StartBT.onClick.RemoveAllListeners();
                StartBT.onClick.AddListener(NewGame);
            }
            if (ContinueBT != null)
            {
                ContinueBT.onClick.RemoveAllListeners();
                ContinueBT.onClick.AddListener(ContinueGame);
                UpdateContinueButtonAvailability();
            }
            if (ExitBT != null)
            {
                ExitBT.onClick.RemoveAllListeners();
                ExitBT.onClick.AddListener(QuitGame);
            }
        }
    }

    /// <summary> @ 전투 시작 버튼 통합 핸들러 </summary>
    private void OnGbtClick()
    {
        // @ 0. 기본 상태 로그
        Debug.Log("@ GBT_CLICK @ playerTrainer:" + (playerTrainer != null) + " | enemyTrainer:" + (enemyTrainer != null));

        if (playerTrainer != null)
        {
            Debug.Log("@ PLAYER_TEAM @ filled:" + playerTrainer.CountFilled().ToString() + " | activeIndex:" + playerTrainer.ActiveIndex.ToString());
        }
        if (enemyTrainer != null)
        {
            Debug.Log("@ ENEMY_TEAM @ filled:" + enemyTrainer.CountFilled().ToString() + " | activeIndex:" + enemyTrainer.ActiveIndex.ToString());
        }

        // @ 1. 준비 체크
        bool playerReady = false;
        if (playerTrainer != null)
        {
            if (playerTrainer.IsTeamFull())
            {
                if (playerTrainer.HasHealthyPokemon())
                {
                    playerReady = true;
                }
            }
        }

        bool enemyReady = false;
        if (enemyTrainer != null)
        {
            if (enemyTrainer.IsTeamFull())
            {
                if (enemyTrainer.HasHealthyPokemon())
                {
                    enemyReady = true;
                }
            }
        }

        // @ 2. 출전 포켓몬 결정 + 포인터 기록 + 로그
        Pokemon pAct = null;
        Pokemon eAct = null;

        if (playerReady)
        {
            pAct = SelectAvailablePokemon(true, true);
            if (pAct != null) { myPokemonG = pAct; }
        }

        if (enemyReady)
        {
            eAct = SelectAvailablePokemon(false, true);
            if (eAct != null) { otherPokemonG = eAct; }
        }

        Debug.Log("@ BEFORE_LOAD @ myPokemonG:" + (myPokemonG != null ? myPokemonG.name : "null") +
                  " | otherPokemonG:" + (otherPokemonG != null ? otherPokemonG.name : "null"));

        // @ 3. 전환 또는 원인 메시지
        if (playerReady)
        {
            if (enemyReady)
            {
                if (myPokemonG != null)
                {
                    if (otherPokemonG != null)
                    {
                        PlayerPrefs.SetInt(SAVE_FLAG_KEY, 1);
                        PlayerPrefs.Save();
                        Debug.Log("@ LOAD @ index:" + SCENE_INDEX_PokemonBattle.ToString() + " // PokemonBattle");
                        SceneManager.LoadScene(SCENE_INDEX_PokemonBattle);  // @ "PokemonBattle"
                        return;
                    }
                    else
                    {
                        Debug.Log("@ BLOCK @ reason:ENEMY_ACTIVE_NULL");
                    }
                }
                else
                {
                    Debug.Log("@ BLOCK @ reason:PLAYER_ACTIVE_NULL");
                }
            }
            else
            {
                Debug.Log("@ BLOCK @ reason:ENEMY_NOT_READY");
            }
        }
        else
        {
            Debug.Log("@ BLOCK @ reason:PLAYER_NOT_READY");
        }

        if (titleText != null)
        {
            if (!playerReady) { titleText.text = "전투에 나설 수 있는 내 팀이 필요해."; }
            else
            {
                if (!enemyReady) { titleText.text = "적 팀 준비가 끝날 때까지 기다려 줘."; }
                else { titleText.text = "전투를 시작할 포켓몬이 없습니다."; }
            }
        }
    }

    /// <summary> @ goBattle 버튼 표시/비표시 </summary>
    private void UpdateGoBattleActive()
    {
        bool show = false;
        bool playerReady = false;
        if (playerTrainer != null)
        {
            if (playerTrainer.IsTeamFull())
            {
                if (playerTrainer.HasHealthyPokemon())
                {
                    playerReady = true;
                }
            }
        }
        if (playerReady)
        {
            bool enemyReady = false;
            if (enemyTrainer != null)
            {
                if (enemyTrainer.IsTeamFull())
                {
                    if (enemyTrainer.HasHealthyPokemon())
                    {
                        enemyReady = true;
                    }
                }
            }
            if (enemyReady)
            {
                Pokemon a = SelectAvailablePokemon(true, true);
                if (a != null)
                {
                    Pokemon b = SelectAvailablePokemon(false, true);
                    if (b != null) { show = true; }
                }
            }
        }
        if (goBattleBt != null)
        {
            goBattleBt.interactable = show;
            if (goBattleBt.gameObject.activeSelf != show) { goBattleBt.gameObject.SetActive(show); }
        }
    }

    private void UpdateChoiceGuideText()
    {
        if (titleText == null) { return; }
        int filled = 0;
        if (playerTrainer != null) { filled = playerTrainer.CountFilled(); }
        titleText.text = "포켓몬 선택 진행중 @ 내 팀 " + filled.ToString() + " / 3";
    }

    private static Pokemon CreateByIndex(Pokemon.PokemonIndex idx, bool isPlayer)
    {
        Pokemon p = null;
        if (idx == Pokemon.PokemonIndex.pikach)
        {
            p = new Pika();
        }
        else
        {
            if (idx == Pokemon.PokemonIndex.paily)
            {
                p = new Paily();
            }
            else
            {
                if (idx == Pokemon.PokemonIndex.goBook)
                {
                    p = new GoBook();
                }
                else
                {
                    p = new Esang();
                }
            }
        }

        PokemonBattleManager.ConfigureAtlasForSide(p, isPlayer);
        return p;
    }

    public static Pokemon SelectAvailablePokemon(bool forPlayer, bool includeCurrent)
    {
        PokemonTrainer t = forPlayer ? playerTrainer : enemyTrainer;
        if (t == null)
        {
            if (forPlayer) { myPokemonG = null; } else { otherPokemonG = null; }
            return null;
        }

        Pokemon a = t.ActivePokemon;
        if (includeCurrent)
        {
            if (a != null)
            {
                if (a.Hp > 0)
                {
                    if (forPlayer) { myPokemonG = a; } else { otherPokemonG = a; }
                    return a;
                }
            }
        }

        Pokemon[] team = t.Team;
        if (team == null)
        {
            if (forPlayer) { myPokemonG = null; } else { otherPokemonG = null; }
            t.SetActiveIndex(-1);
            return null;
        }

        int i = 0;
        while (i < team.Length)
        {
            Pokemon c = team[i];
            if (c != null)
            {
                if (c.Hp > 0)
                {
                    t.SetActiveIndex(i);
                    if (forPlayer) { myPokemonG = c; } else { otherPokemonG = c; }
                    return c;
                }
            }
            i = i + 1;
        }

        t.SetActiveIndex(-1);
        if (forPlayer) { myPokemonG = null; } else { otherPokemonG = null; }
        return null;
    }

    // =========================================================
    // @ 저장/불러오기
    // =========================================================
    public static void AutoSave(int sceneIndex)
    {
        SaveDTO dto = new SaveDTO();
        dto.player = new PokemonDTO[3];
        dto.enemy = new PokemonDTO[3];

        Pokemon[] pTeam = playerTrainer != null ? playerTrainer.Team : null;
        Pokemon[] eTeam = enemyTrainer != null ? enemyTrainer.Team : null;

        dto.player[0] = (pTeam != null && pTeam.Length > 0 && pTeam[0] != null) ? ToDTO(pTeam[0]) : null;
        dto.player[1] = (pTeam != null && pTeam.Length > 1 && pTeam[1] != null) ? ToDTO(pTeam[1]) : null;
        dto.player[2] = (pTeam != null && pTeam.Length > 2 && pTeam[2] != null) ? ToDTO(pTeam[2]) : null;
        dto.playerActive = playerTrainer != null ? playerTrainer.ActiveIndex : -1;

        dto.enemy[0] = (eTeam != null && eTeam.Length > 0 && eTeam[0] != null) ? ToDTO(eTeam[0]) : null;
        dto.enemy[1] = (eTeam != null && eTeam.Length > 1 && eTeam[1] != null) ? ToDTO(eTeam[1]) : null;
        dto.enemy[2] = (eTeam != null && eTeam.Length > 2 && eTeam[2] != null) ? ToDTO(eTeam[2]) : null;
        dto.enemyActive = enemyTrainer != null ? enemyTrainer.ActiveIndex : -1;

        dto.round = PokemonBattleManager.GetRoundSnapshot();
        dto.sceneIndex = sceneIndex;

        string json = JsonUtility.ToJson(dto, true);
        File.WriteAllText(SavePath, json);

        PlayerPrefs.SetInt(SAVE_FLAG_KEY, 1);
        PlayerPrefs.Save();
    }

    private static PokemonDTO ToDTO(Pokemon p)
    {
        if (p == null) { return null; }
        PokemonDTO d = new PokemonDTO();
        d.idx = (int)p.index;
        d.type = (int)p.type;
        d.name = p.name;
        d.hp = p.Hp;
        d.atk = p.atk;
        d.def = p.def;
        d.speed = p.speed;
        return d;
    }

    private static Pokemon FromDTO(PokemonDTO d, bool isPlayer)
    {
        Pokemon.PokemonIndex idx = (Pokemon.PokemonIndex)d.idx;
        Pokemon p = CreateByIndex(idx, isPlayer);
        p.name = d.name;
        p.Hp = d.hp;
        p.atk = d.atk;
        p.def = d.def;
        p.speed = d.speed;
        p.type = (Pokemon.Tpye)d.type;
        return p;
    }

    public void ContinueGame()
    {
        if (!HasSaveData())
        {
            UpdateContinueButtonAvailability();
            return;
        }

        try
        {
            string json = File.ReadAllText(SavePath);
            SaveDTO dto = JsonUtility.FromJson<SaveDTO>(json);

            if (playerTrainer == null) { playerTrainer = new PokemonTrainer("Player", 3); }
            if (enemyTrainer == null) { enemyTrainer = new PokemonTrainer("Enemy", 3); }

            Pokemon[] rp = new Pokemon[3];
            if (dto.player != null)
            {
                if (dto.player.Length > 0 && dto.player[0] != null) { rp[0] = FromDTO(dto.player[0], true); }
                if (dto.player.Length > 1 && dto.player[1] != null) { rp[1] = FromDTO(dto.player[1], true); }
                if (dto.player.Length > 2 && dto.player[2] != null) { rp[2] = FromDTO(dto.player[2], true); }
            }
            playerTrainer.LoadTeam(rp, dto.playerActive);

            Pokemon[] re = new Pokemon[3];
            if (dto.enemy != null)
            {
                if (dto.enemy.Length > 0 && dto.enemy[0] != null) { re[0] = FromDTO(dto.enemy[0], false); }
                if (dto.enemy.Length > 1 && dto.enemy[1] != null) { re[1] = FromDTO(dto.enemy[1], false); }
                if (dto.enemy.Length > 2 && dto.enemy[2] != null) { re[2] = FromDTO(dto.enemy[2], false); }
            }
            enemyTrainer.LoadTeam(re, dto.enemyActive);

            _enemyBuiltOnce = enemyTrainer.IsTeamFull();

            myPokemonG = playerTrainer.ActivePokemon;
            otherPokemonG = enemyTrainer.ActivePokemon;

            PokemonBattleManager.SetRoundFromSave(dto.round);

            int loadIndex = SCENE_INDEX_PokemonBattle;
            if (dto != null)
            {
                if (dto.sceneIndex == SCENE_INDEX_PokemonStart)
                {
                    loadIndex = SCENE_INDEX_PokemonStart;
                }
                else
                {
                    if (dto.sceneIndex == SCENE_INDEX_PokemonChoices)
                    {
                        loadIndex = SCENE_INDEX_PokemonChoices;
                    }
                }
            }
            SceneManager.LoadScene(loadIndex);
        }
        catch (Exception ex)
        {
            Debug.LogError("세이브 불러오기 실패: " + ex.Message);
        }
    }

    private static void ApplySave(SaveDTO dto)
    {
        if (playerTrainer == null)
        {
            playerTrainer = new PokemonTrainer("Player", 3);
        }
        if (enemyTrainer == null)
        {
            enemyTrainer = new PokemonTrainer("Enemy", 3);
        }

        Pokemon[] rp = new Pokemon[3];
        if (dto.player != null)
        {
            if (dto.player.Length > 0 && dto.player[0] != null) { rp[0] = FromDTO(dto.player[0], true); }
            if (dto.player.Length > 1 && dto.player[1] != null) { rp[1] = FromDTO(dto.player[1], true); }
            if (dto.player.Length > 2 && dto.player[2] != null) { rp[2] = FromDTO(dto.player[2], true); }
        }
        playerTrainer.LoadTeam(rp, dto.playerActive);

        Pokemon[] re = new Pokemon[3];
        if (dto.enemy != null)
        {
            if (dto.enemy.Length > 0 && dto.enemy[0] != null) { re[0] = FromDTO(dto.enemy[0], false); }
            if (dto.enemy.Length > 1 && dto.enemy[1] != null) { re[1] = FromDTO(dto.enemy[1], false); }
            if (dto.enemy.Length > 2 && dto.enemy[2] != null) { re[2] = FromDTO(dto.enemy[2], false); }
        }
        enemyTrainer.LoadTeam(re, dto.enemyActive);
    }

    public void NewGame()
    {
        if (File.Exists(SavePath))
        {
            try { File.Delete(SavePath); }
            catch (Exception ex) { Debug.LogError("세이브파일 삭제 실패: " + ex.Message); }
        }
        if (PlayerPrefs.HasKey(SAVE_FLAG_KEY)) { PlayerPrefs.DeleteKey(SAVE_FLAG_KEY); PlayerPrefs.Save(); }

        if (playerTrainer == null) { playerTrainer = new PokemonTrainer("Player", 3); } else { playerTrainer.ClearTeam(); }
        if (enemyTrainer == null) { enemyTrainer = new PokemonTrainer("Enemy", 3); } else { enemyTrainer.ClearTeam(); }

        _enemyBuiltOnce = false; // 필드명 일치(언더스코어)
        myPokemonG = null;
        otherPokemonG = null;

        SceneManager.LoadScene(SCENE_INDEX_PokemonChoices);  // @ "PokemonChoices"
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    private void OnApplicationQuit()
    {
        AutoSave(SceneManager.GetActiveScene().buildIndex); // 현재 씬 인덱스 사용. :contentReference[oaicite:4]{index=4}
    }

    // =========================================================
    // @ 설정 버튼 래퍼
    // =========================================================
    public GameObject settingsRef;
    public void OnClickSettingsButton() { if (settingsRef != null) { settingsRef.GetComponent<Setting>().OnClickSettingsButton(); } }
    public void OnClickSettingsResume() { if (settingsRef != null) { settingsRef.GetComponent<Setting>().OnClickSettingsResume(); } }
    public void OnClickSettingsRestart() { if (settingsRef != null) { settingsRef.GetComponent<Setting>().OnClickSettingsRestart(); } }
    public void OnClickSettingsExitToStart() { if (settingsRef != null) { settingsRef.GetComponent<Setting>().OnClickSettingsExitToStart(); } }
}
