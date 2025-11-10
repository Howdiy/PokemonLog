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

    // =========================================================
    // @ 저장 DTO
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

    // =========================================================
    // @ 저장 경로 & PlayerPrefs 플래그
    // =========================================================
    private const string SAVE_FLAG_KEY = "POKEMON_SAVE_FLAG_V1";

    private static string SavePath
    {
        get
        {
            string dir = Application.persistentDataPath;
            string path = Path.Combine(dir, "pokemon_save.json");
            return path;
        }
    }

    /// <summary> @ 파일이 존재하고 PlayerPrefs 플래그가 있을 때만 '진짜 저장 있음'으로 간주 </summary>
    public static bool HasSaveData()
    {
        bool fileExists = File.Exists(SavePath);
        bool flagExists = PlayerPrefs.HasKey(SAVE_FLAG_KEY);
        if (fileExists)
        {
            if (flagExists)
            {
                return true;
            }
        }
        return false;
    }

    private static void MarkSaveFlag()
    {
        PlayerPrefs.SetInt(SAVE_FLAG_KEY, 1);
        PlayerPrefs.Save();
    }

    private static void ClearSaveFlag()
    {
        if (PlayerPrefs.HasKey(SAVE_FLAG_KEY))
        {
            PlayerPrefs.DeleteKey(SAVE_FLAG_KEY);
            PlayerPrefs.Save();
        }
    }

    private static void DeleteSaveFileIfExists()
    {
        if (File.Exists(SavePath))
        {
            try
            {
                File.Delete(SavePath);
            }
            catch
            {
                // @ 파일 삭제 실패는 무시
            }
        }
    }

    // =========================================================
    // @ 라이프사이클
    // =========================================================
    private void Awake()
    {
        EnsureSettingPrefabUnderCanvas();
    }

    /// <summary> @ 시작 시 선택 씬이라면 초기 goBattle 표시/적팀 준비, Start 씬이면 버튼 상태 갱신 및 바인딩 </summary>
    private void Start()
    {
        string scene = SceneManager.GetActiveScene().name;

        EnsureEnemyTeamIfNeeded(true);
        UpdateChoiceUI();
        UpdateGoBattleActive();

        WireStartMenuButtonsIfStartScene(scene);
        WireGoBattleButtonIfChoicesScene(scene);
    }

    // =========================================================
    // @ UI 텍스트
    // =========================================================
    private void UpdateChoiceUI()
    {
        if (titleText != null)
        {
            int filled = playerTrainer != null ? playerTrainer.CountFilled() : 0;
            titleText.text = "포켓몬 선택\n"
                           + "바깥은 혼자 돌아다니긴 위험하단다\n"
                           + "이 아이들 중 하나를 데려가렴\n"
                           + "(내 팀 " + filled.ToString() + " / 3)";
        }
    }

    // =========================================================
    // @ 적 팀 구성
    // =========================================================
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

    public static Pokemon SelectAvailablePokemon(bool forPlayer, bool includeCurrentSlot)
    {
        PokemonTrainer trainer = forPlayer ? playerTrainer : enemyTrainer;
        if (trainer == null)
        {
            if (forPlayer) { myPokemonG = null; } else { otherPokemonG = null; }
            return null;
        }

        Pokemon[] team = trainer.Team;
        if (team == null)
        {
            if (forPlayer) { myPokemonG = null; } else { otherPokemonG = null; }
            trainer.SetActiveIndex(-1);
            return null;
        }

        Pokemon selected = trainer.SelectAvailablePokemon(includeCurrentSlot);
        if (forPlayer) { myPokemonG = selected; } else { otherPokemonG = selected; }
        return selected;
    }

    // =========================================================
    // @ Start 씬 버튼 바인딩 + Continue 비활성 조건 보장
    // =========================================================
    private void WireStartMenuButtonsIfStartScene(string sceneName)
    {
        if (sceneName != "PokemonStart") { return; }

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

    private void UpdateContinueButtonAvailability()
    {
        if (ContinueBT == null) { return; }

        bool hasSave = HasSaveData();

        ContinueBT.interactable = hasSave;
        ContinueBT.enabled = hasSave;

        CanvasGroup cg = ContinueBT.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.interactable = hasSave;
            cg.blocksRaycasts = hasSave;
            cg.alpha = hasSave ? 1f : 0.5f;
        }
        else
        {
            if (ContinueBT.targetGraphic != null)
            {
                Color color = ContinueBT.targetGraphic.color;
                color.a = hasSave ? 1f : 0.5f;
                ContinueBT.targetGraphic.color = color;
            }
        }
    }

    // =========================================================
    // @ Pokemon 선택 처리 (인스펙터에 남아있는 기존 메서드명 유지)
    // =========================================================
    public void OnPokemonClick(int index)
    {
        Pokemon choice = CreateByIndex((Pokemon.PokemonIndex)index, true);
        if (playerTrainer != null)
        {
            int dummy;
            if (!playerTrainer.TryAddPokemon(choice, out dummy))
            {
                playerTrainer.ReplaceFirstPokemon(choice);
            }
            myPokemonG = playerTrainer.ActivePokemon;
        }

        EnsureEnemyTeamIfNeeded(false);
        UpdateChoiceUI();
        UpdateGoBattleActive();
    }

    public void OnPokemonCilck(int index)  // 기존 철자 보존
    {
        OnPokemonClick(index);
    }

    private void EnsureEnemyTeamIfNeeded(bool allowBuildEarly)
    {
        if (_enemyBuiltOnce) { return; }

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
    // @ goBattle: 런타임 바인딩 + 표시 상태 관리 + 클릭시 전환
    // =========================================================
    private void WireGoBattleButtonIfChoicesScene(string sceneName)
    {
        if (sceneName != "PokemonChoices") { return; }

        // @ 안전 바인딩: 인스펙터에 없더라도 동작 보장
        if (goBattleButton == null && battleStart != null)
        {
            Button b = battleStart.GetComponent<Button>();
            if (b != null)
            {
                goBattleButton = b;
            }
        }

        if (goBattleButton != null)
        {
            goBattleButton.onClick.RemoveAllListeners();
            goBattleButton.onClick.AddListener(OnClickGoBattle);
        }
    }

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
                Pokemon playerActive = SelectAvailablePokemon(true, true);
                Pokemon enemyActive = SelectAvailablePokemon(false, true);
                if (playerActive != null)
                {
                    if (enemyActive != null)
                    {
                        show = true;
                    }
                }
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

    /// <summary> @ 인스펙터에 연결되어 있던 기존 함수 이름 유지 @ 잘못된 씬 로드를 교정 </summary>
    public void BattleStart()
    {
        // @ 과거에는 PokemonChoices로 재로딩되어 전환이 안 되는 문제가 있었음
        // @ 이제는 동일한 진입점을 사용하여 전투씬 전환을 보장
        OnClickGoBattle();
    }

    /// <summary> @ goBattle 눌렀을 때 처리: 조건 재검증 후 전투씬으로 전환 </summary>
    public void OnClickGoBattle()
    {
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

        if (playerReady)
        {
            if (enemyReady)
            {
                Pokemon playerActive = SelectAvailablePokemon(true, true);
                Pokemon enemyActive = SelectAvailablePokemon(false, true);

                if (playerActive != null)
                {
                    if (enemyActive != null)
                    {
                        // @ 전투 진입 직전 저장 플래그를 표준화
                        MarkSaveFlag();
                        SceneManager.LoadScene("PokemonBattle");
                        return;
                    }
                }
            }
        }

        if (titleText != null)
        {
            if (!playerReady)
            {
                titleText.text = "전투에 나설 수 있는 내 팀이 필요해.";
            }
            else
            {
                if (!enemyReady)
                {
                    titleText.text = "적 팀 준비가 끝날 때까지 기다려 줘.";
                }
                else
                {
                    titleText.text = "전투를 시작할 포켓몬이 없습니다.";
                }
            }
        }
    }

    // =========================================================
    // @ Setting 프리팹 보장(프리팹→씬 Canvas 하위, uiRoot 지정, 정렬, 시작 위치 중앙)
    // =========================================================
    private void EnsureSettingPrefabUnderCanvas()
    {
        Canvas c = GameObject.FindObjectOfType<Canvas>();
        if (c == null)
        {
            return;
        }

        if (settingsRef != null)
        {
            settingsRef.transform.SetParent(c.transform, false);

            if (settingsRef.uiRoot == null)
            {
                settingsRef.uiRoot = c.transform;

                if (settingsRef.settingsPanel != null)
                {
                    Transform t = settingsRef.settingsPanel.transform;
                    Vector3 p = t.localPosition;
                    p.x = 0f; p.y = 0f; p.z = 0f;
                    t.localPosition = p;
                }
                settingsRef.EnsureNestedCanvasTopmostAtRuntime(settingsSortOrder);
            }

            if (settingsRef.settingsPanel != null)
            {
                Transform t = settingsRef.settingsPanel.transform;
                Vector3 p = t.localPosition;
                p.x = 0f; p.y = 0f; p.z = 0f;
                t.localPosition = p;

                settingsRef.initialPosX = 0f;
                settingsRef.initialPosY = 0f;
            }

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
        go.transform.SetParent(c.transform, false);

        Setting exist = go.GetComponent<Setting>();
        if (exist == null)
        {
            exist = go.AddComponent<Setting>();
            exist.settingsSortOrder = settingsSortOrder;
        }

        if (exist.settingsPanel != null)
        {
            Transform t = exist.settingsPanel.transform;
            Vector3 p = t.localPosition;
            p.x = 0f; p.y = 0f; p.z = 0f;
            t.localPosition = p;
        }
        exist.EnsureNestedCanvasTopmostAtRuntime(settingsSortOrder);

        if (settingsUiRootHint != null)
        {
            exist.uiRoot = settingsUiRootHint;
        }
        else
        {
            exist.uiRoot = c.transform;
        }

        if (exist.settingsPanel != null)
        {
            Transform t = exist.settingsPanel.transform;
            Vector3 p = t.localPosition;
            p.x = 0f; p.y = 0f; p.z = 0f;
            t.localPosition = p;

            exist.initialPosX = 0f;
            exist.initialPosY = 0f;
        }

        Canvas goCanvas = go.GetComponent<Canvas>();
        if (goCanvas == null)
        {
            goCanvas = go.AddComponent<Canvas>();
            go.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }
        goCanvas.overrideSorting = true;
        goCanvas.sortingOrder = settingsSortOrder;

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

        // @ 저장 파일과 PlayerPrefs 플래그를 동시에 세팅하여 기준을 일치화
        MarkSaveFlag();
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

    public void NewGame()
    {
        // @ 세이브 완전 삭제: 파일 + 플래그 동시 정리
        DeleteSaveFileIfExists();
        ClearSaveFlag();

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

    // =========================================================
    // @ 유틸
    // =========================================================
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
}
