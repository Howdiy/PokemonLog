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
    /// <summary> @ goBattle 버튼 오브젝트(표시/비표시는 SetActive 사용) </summary>
    public GameObject battleStart;

    // @ Setting 프리팹/연결
    public Setting settingsRef;
    public GameObject settingPrefab;
    public Transform settingsUiRootHint;

    // @ UI
    public Transform uiRoot;
    public int settingsSortOrder = 5000;

    /// <summary> @ 선택 안내 텍스트 </summary>
    public TextMeshProUGUI titleText;

    /// <summary> @ 선택용 버튼들(인스펙터 배열) </summary>
    public Button[] bts;

    public Button goBattleButton;
    public Button StartBT;
    public Button ContinueBT;
    public Button ExitBT;

    // @ 선택 결과 및 팀(트레이너가 관리)
    public static PokemonTrainer playerTrainer = new PokemonTrainer("Player", 3);
    public static PokemonTrainer enemyTrainer = new PokemonTrainer("Enemy", 3);

    // @ 배틀로 넘길 포인터
    public static Pokemon myPokemonG;
    public static Pokemon otherPokemonG;

    // @ 내부 플래그: 적 팀을 이미 구성했는가
    private static bool _enemyBuiltOnce = false;

    // @ 저장 DTO
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

    [Serializable]
    public class SaveDTO
    {
        public PokemonDTO[] player;
        public PokemonDTO[] enemy;
        public int playerActive;
        public int enemyActive;
        public int round;
        public string sceneName;
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
        return File.Exists(SavePath);
    }

    private void Awake()
    {
        // @ Setting 프리팹 인스턴스 생성·부모 지정·uiRoot·정렬·초기위치 보정
        EnsureSettingPrefabUnderCanvas();
    }

    /// <summary> @ 시작 시 선택 씬이라면 초기 goBattle 표시/적팀 준비 </summary>
    private void Start()
    {
        // @ 선택 씬에서만 goBattle 표시 상태를 갱신
        EnsureEnemyTeamIfNeeded(true);
        UpdateChoiceUI();       // @ 텍스트 갱신
        UpdateGoBattleActive(); // @ 버튼 표시 갱신

        // @ 시작 씬일 때만 Start/Continue/Exit 자동 배선
        WireStartMenuButtons();
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
        List<Pokemon.PokemonIndex> pool = new List<Pokemon.PokemonIndex>();
        pool.Add(Pokemon.PokemonIndex.pikach);
        pool.Add(Pokemon.PokemonIndex.paily);
        pool.Add(Pokemon.PokemonIndex.goBook);
        pool.Add(Pokemon.PokemonIndex.eSang);

        if (enemyTrainer == null)
        {
            enemyTrainer = new PokemonTrainer("Enemy", 3);
        }

        enemyTrainer.ClearTeam();

        for (int i = 0; i < enemyTrainer.TeamSize && i < 3; i++)
        {
            if (pool.Count > 0)
            {
                int r = Random.Range(0, pool.Count);
                Pokemon.PokemonIndex pick = pool[r];
                pool.RemoveAt(r);
                Pokemon pokemon = CreateByIndex(pick, false);
                enemyTrainer.SetPokemonAt(i, pokemon);
            }
        }

        otherPokemonG = enemyTrainer.ActivePokemon;
    }

    /// <summary> @ 인덱스로 포켓몬 생성 </summary>
    public static Pokemon SelectAvailablePokemon(bool forPlayer, bool includeCurrentSlot)
    {
        PokemonTrainer trainer = forPlayer ? playerTrainer : enemyTrainer;
        if (trainer == null)
        {
            if (forPlayer)
            {
                myPokemonG = null;
            }
            else
            {
                otherPokemonG = null;
            }
            return null;
        }

        Pokemon[] team = trainer.Team;
        if (team == null || team.Length == 0)
        {
            if (forPlayer)
            {
                myPokemonG = null;
            }
            else
            {
                otherPokemonG = null;
            }
            trainer.SetActiveIndex(-1);
            return null;
        }

        Pokemon selected = trainer.SelectAvailablePokemon(includeCurrentSlot);
        if (forPlayer)
        {
            myPokemonG = selected;
        }
        else
        {
            otherPokemonG = selected;
        }

        return selected;
    }

    // =========================================================
    // @ PokemonStart : Start/Continue/Exit 버튼 바인딩(람다 미사용)
    // =========================================================
    private void WireStartMenuButtons()
    {
        string scene = SceneManager.GetActiveScene().name;
        if (scene == "PokemonStart")
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
            }
            if (ExitBT != null)
            {
                ExitBT.onClick.RemoveAllListeners();
                ExitBT.onClick.AddListener(QuitGame);
            }

            UpdateContinueButtonAvailability();
        }
    }

    private void UpdateContinueButtonAvailability()
    {
        if (ContinueBT == null)
        {
            return;
        }

        bool hasSave = HasSaveData();

        ContinueBT.interactable = hasSave;
        ContinueBT.enabled = hasSave;

        CanvasGroup cg = ContinueBT.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.interactable = hasSave;
            cg.blocksRaycasts = hasSave;
            cg.alpha = hasSave ? 1f : 0.5f;
            return;
        }

        if (ContinueBT.targetGraphic != null)
        {
            Color color = ContinueBT.targetGraphic.color;
            color.a = hasSave ? 1f : 0.5f;
            ContinueBT.targetGraphic.color = color;
        }
    }

    /// <summary> @ 포켓몬 선택(새 이름) </summary>
    public void OnPokemonClick(int index)
    {
        Pokemon choice = CreateByIndex((Pokemon.PokemonIndex)index, true);
        if (playerTrainer != null)
        {
            if (!playerTrainer.TryAddPokemon(choice, out _))
            {
                playerTrainer.ReplaceFirstPokemon(choice);
            }

            myPokemonG = playerTrainer.ActivePokemon;
        }

        EnsureEnemyTeamIfNeeded(false);

        UpdateChoiceUI();
        UpdateGoBattleActive();
    }

    /// <summary> @ 인스펙터에서 잘못 입력된 기존 메서드명을 유지 </summary>
    public void OnPokemonCilck(int index)
    {
        OnPokemonClick(index);
    }

    /// <summary> @ 플레이어 팀이 가득 찼다면 적 팀을 자동으로 구성 </summary>
    private void EnsureEnemyTeamIfNeeded(bool allowBuildEarly)
    {
        // @ 이미 만들어졌으면 패스
        if (_enemyBuiltOnce) { return; }

        bool playerFull = playerTrainer != null && playerTrainer.IsTeamFull();
        if (playerFull)
        {
            BuildRandomEnemyTeam3();
            _enemyBuiltOnce = true;
            return;
        }

        // @ 초기 진입 시에도 적 팀을 미리 만들어 둘 수 있음(옵션)
        if (allowBuildEarly)
        {
            BuildRandomEnemyTeam3();
            _enemyBuiltOnce = true;
        }
    }

    // =========================================================
    // @ GoBattle
    // =========================================================
    /// <summary> @ 인스펙터에 연결되어 있던 기존 함수 이름 유지 </summary>
    public void BattleStart()
    {
        SceneManager.LoadScene("PokemonChoices");
    }

    /// <summary> @ goBattle 눌렀을 때 처리 </summary>
    public void OnClickGoBattle()
    {
        bool playerReady = playerTrainer != null && playerTrainer.IsTeamFull() && playerTrainer.HasHealthyPokemon();
        bool enemyReady = enemyTrainer != null && enemyTrainer.IsTeamFull() && enemyTrainer.HasHealthyPokemon();

        if (playerReady && enemyReady)
        {
            Pokemon playerActive = SelectAvailablePokemon(true, true);
            Pokemon enemyActive = SelectAvailablePokemon(false, true);

            if (playerActive != null && enemyActive != null)
            {
                SceneManager.LoadScene("PokemonBattle");
                return;
            }
        }

        if (titleText != null)
        {
            if (!playerReady)
            {
                titleText.text = "전투에 나설 수 있는 내 팀이 필요해.";
            }
            else if (!enemyReady)
            {
                titleText.text = "적 팀 준비가 끝날 때까지 기다려 줘.";
            }
            else
            {
                titleText.text = "전투를 시작할 포켓몬이 없습니다.";
            }
        }
    }

    /// <summary> @ goBattle 버튼 표시 여부 결정(SetActive) </summary>
    private void UpdateGoBattleActive()
    {
        bool show = false;

        bool playerReady = playerTrainer != null && playerTrainer.IsTeamFull() && playerTrainer.HasHealthyPokemon();
        if (playerReady)
        {
            bool enemyReady = enemyTrainer != null && enemyTrainer.IsTeamFull() && enemyTrainer.HasHealthyPokemon();
            if (enemyReady)
            {
                Pokemon playerActive = SelectAvailablePokemon(true, true);
                Pokemon enemyActive = SelectAvailablePokemon(false, true);
                show = (playerActive != null && enemyActive != null);
            }
        }

        if (battleStart != null)
        {
            if (battleStart.activeSelf != show)
            {
                battleStart.SetActive(show);
            }
        }

        if (goBattleButton != null)
        {
            goBattleButton.interactable = show;
            if (goBattleButton.gameObject.activeSelf != show)
            {
                goBattleButton.gameObject.SetActive(show);
            }
        }
    }

    private static Pokemon CreateByIndex(Pokemon.PokemonIndex idx, bool isPlayer)
    {
        Pokemon p = null;
        if (idx == Pokemon.PokemonIndex.pikach)
        {
            p = new Pika();
        }
        else if (idx == Pokemon.PokemonIndex.paily)
        {
            p = new Paily();
        }
        else if (idx == Pokemon.PokemonIndex.goBook)
        {
            p = new GoBook();
        }
        else
        {
            p = new Esang();
        }

        PokemonBattleManager.ConfigureAtlasForSide(p, isPlayer);
        return p;
    }

    // =========================================================
    // @ Setting 프리팹 보장(프리팹→씬 Canvas 하위, uiRoot 지정, 정렬, 시작 위치 중앙)
    // =========================================================
    private void EnsureSettingPrefabUnderCanvas()
    {
        // @ 씬 내 Canvas 찾기
        Canvas c = GameObject.FindObjectOfType<Canvas>();
        if (c == null)
        {
            return;
        }

        // @ 이미 Setting 인스턴스가 있으면 그걸 사용
        if (settingsRef != null)
        {
            // @ 부모 지정: worldPositionStays=false 로 UI 좌표계 유지
            settingsRef.transform.SetParent(c.transform, false);

            // @ uiRoot 자동 할당
            if (settingsRef.uiRoot == null)
            {
                settingsRef.uiRoot = c.transform;
                // @ 런타임 중앙 배치
                if (settingsRef.settingsPanel != null)
                {
                    Transform t = settingsRef.settingsPanel.transform;
                    Vector3 p = t.localPosition;
                    p.x = 0f;
                    p.y = 0f;
                    p.z = 0f;
                    t.localPosition = p;
                }
                // @ 최상단 정렬(중첩 캔버스)
                settingsRef.EnsureNestedCanvasTopmostAtRuntime(settingsSortOrder);
            }

            // @ 패널 시작 위치 중앙
            if (settingsRef.settingsPanel != null)
            {
                Transform t = settingsRef.settingsPanel.transform;
                Vector3 p = t.localPosition;
                p.x = 0f; p.y = 0f; p.z = 0f;
                t.localPosition = p;

                settingsRef.initialPosX = 0f;
                settingsRef.initialPosY = 0f;
            }

            // @ Nested Canvas 정렬 무시 및 순서 보장
            Canvas sc = settingsRef.GetComponent<Canvas>();
            if (sc == null)
            {
                sc = settingsRef.gameObject.AddComponent<Canvas>();
                settingsRef.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            }
            sc.overrideSorting = true;
            sc.sortingOrder = settingsSortOrder;

            return;
        }

        // @ 프리팹으로부터 새 인스턴스 생성
        GameObject go = null;
        if (settingPrefab != null)
        {
            go = GameObject.Instantiate(settingPrefab);
        }
        if (go == null)
        {
            return;
        }
        go.name = "Setting";

        // @ 부모 지정: worldPositionStays=false 로 UI 좌표계 유지
        go.transform.SetParent(c.transform, false);

        // @ Setting 컴포넌트 확보
        Setting exist = go.GetComponent<Setting>();
        if (exist == null)
        {
            exist = go.AddComponent<Setting>();
            exist.settingsSortOrder = settingsSortOrder;
        }
        // @ 런타임 중앙 배치
        if (exist.settingsPanel != null)
        {
            Transform t = exist.settingsPanel.transform;
            Vector3 p = t.localPosition;
            p.x = 0f;
            p.y = 0f;
            p.z = 0f;
            t.localPosition = p;
        }
        // @ 최상단 정렬(중첩 캔버스)
        exist.EnsureNestedCanvasTopmostAtRuntime(settingsSortOrder);


        // @ uiRoot 자동 할당
        if (settingsUiRootHint != null)
        {
            exist.uiRoot = settingsUiRootHint;
        }
        else
        {
            exist.uiRoot = c.transform;
        }

        // @ 패널 시작 위치 중앙
        if (exist.settingsPanel != null)
        {
            Transform t = exist.settingsPanel.transform;
            Vector3 p = t.localPosition;
            p.x = 0f; p.y = 0f; p.z = 0f;
            t.localPosition = p;

            exist.initialPosX = 0f;
            exist.initialPosY = 0f;
        }

        // @ Nested Canvas 정렬 무시 및 순서 보장
        Canvas goCanvas = go.GetComponent<Canvas>();
        if (goCanvas == null)
        {
            goCanvas = go.AddComponent<Canvas>();
            go.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }
        goCanvas.overrideSorting = true;
        goCanvas.sortingOrder = settingsSortOrder;

        // @ 외부에서 settingsRef 사용 가능하도록 보관
        settingsRef = exist;
    }

    // =========================================================
    // @ 저장/불러오기
    // =========================================================
    public static void AutoSave(string sceneName)
    {
        SaveDTO dto = new SaveDTO();
        dto.player = new PokemonDTO[3];
        dto.enemy = new PokemonDTO[3];

        Pokemon[] playerTeam = (playerTrainer != null) ? playerTrainer.Team : null;
        Pokemon[] enemyTeam = (enemyTrainer != null) ? enemyTrainer.Team : null;

        dto.player[0] = (playerTeam != null && playerTeam.Length > 0 && playerTeam[0] != null) ? ToDTO(playerTeam[0]) : null;
        dto.player[1] = (playerTeam != null && playerTeam.Length > 1 && playerTeam[1] != null) ? ToDTO(playerTeam[1]) : null;
        dto.player[2] = (playerTeam != null && playerTeam.Length > 2 && playerTeam[2] != null) ? ToDTO(playerTeam[2]) : null;

        dto.playerActive = (playerTrainer != null) ? playerTrainer.ActiveIndex : -1;

        dto.enemy[0] = (enemyTeam != null && enemyTeam.Length > 0 && enemyTeam[0] != null) ? ToDTO(enemyTeam[0]) : null;
        dto.enemy[1] = (enemyTeam != null && enemyTeam.Length > 1 && enemyTeam[1] != null) ? ToDTO(enemyTeam[1]) : null;
        dto.enemy[2] = (enemyTeam != null && enemyTeam.Length > 2 && enemyTeam[2] != null) ? ToDTO(enemyTeam[2]) : null;
        dto.enemyActive = (enemyTrainer != null) ? enemyTrainer.ActiveIndex : -1;

        dto.round = PokemonBattleManager.GetRoundSnapshot();
        dto.sceneName = sceneName;

        string json = JsonUtility.ToJson(dto, true);
        File.WriteAllText(SavePath, json);
    }

    public void ContinueGame()
    {
        if (!HasSaveData())
        {
            UpdateContinueButtonAvailability();
            return;
        }
        string json = File.ReadAllText(SavePath);
        SaveDTO dto = JsonUtility.FromJson<SaveDTO>(json);
        ApplySave(dto);

        string loadScene = "PokemonBattle";
        if (!string.IsNullOrEmpty(dto.sceneName))
        {
            if (dto.sceneName == "PokemonStart")
            {
                loadScene = "PokemonStart";
            }
            else
            {
                if (dto.sceneName == "PokemonChoices")
                {
                    loadScene = "PokemonChoices";
                }
            }
        }
        SceneManager.LoadScene(loadScene);
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

        Pokemon[] restoredPlayer = new Pokemon[3];
        if (dto.player != null)
        {
            if (dto.player.Length > 0 && dto.player[0] != null) { restoredPlayer[0] = FromDTO(dto.player[0], true); }
            if (dto.player.Length > 1 && dto.player[1] != null) { restoredPlayer[1] = FromDTO(dto.player[1], true); }
            if (dto.player.Length > 2 && dto.player[2] != null) { restoredPlayer[2] = FromDTO(dto.player[2], true); }
        }
        playerTrainer.LoadTeam(restoredPlayer, dto.playerActive);

        Pokemon[] restoredEnemy = new Pokemon[3];
        if (dto.enemy != null)
        {
            if (dto.enemy.Length > 0 && dto.enemy[0] != null) { restoredEnemy[0] = FromDTO(dto.enemy[0], false); }
            if (dto.enemy.Length > 1 && dto.enemy[1] != null) { restoredEnemy[1] = FromDTO(dto.enemy[1], false); }
            if (dto.enemy.Length > 2 && dto.enemy[2] != null) { restoredEnemy[2] = FromDTO(dto.enemy[2], false); }
        }
        enemyTrainer.LoadTeam(restoredEnemy, dto.enemyActive);

        _enemyBuiltOnce = enemyTrainer.IsTeamFull();

        myPokemonG = playerTrainer.ActivePokemon;
        otherPokemonG = enemyTrainer.ActivePokemon;

        PokemonBattleManager.SetRoundFromSave(dto.round);
    }

    private static PokemonDTO ToDTO(Pokemon p)
    {
        if (p == null)
        {
            return null;
        }
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

    public void NewGame()
    {
        if (File.Exists(SavePath))
        {
            try
            {
                File.Delete(SavePath);
            }
            catch
            {
                // @ 삭제 실패해도 무시
            }
        }

        if (playerTrainer == null)
        {
            playerTrainer = new PokemonTrainer("Player", 3);
        }
        else
        {
            playerTrainer.ClearTeam();
        }

        if (enemyTrainer == null)
        {
            enemyTrainer = new PokemonTrainer("Enemy", 3);
        }
        else
        {
            enemyTrainer.ClearTeam();
        }

        _enemyBuiltOnce = false;

        myPokemonG = null;
        otherPokemonG = null;

        PokemonBattleManager.SetRoundFromSave(1);

        SceneManager.LoadScene("PokemonChoices");
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    private void OnApplicationQuit()
    {
        string scene = SceneManager.GetActiveScene().name;
        AutoSave(scene);
    }

    // =========================================================
    // @ 설정 버튼 래퍼
    // =========================================================
    public void OnClickSettingsButton() { if (settingsRef != null) { settingsRef.OnClickSettingsButton(); } }
    public void OnClickSettingsResume() { if (settingsRef != null) { settingsRef.OnClickSettingsResume(); } }
    public void OnClickSettingsRestart() { if (settingsRef != null) { settingsRef.OnClickSettingsRestart(); } }
    public void OnClickSettingsExitToStart() { if (settingsRef != null) { settingsRef.OnClickSettingsExitToStart(); } }
}
