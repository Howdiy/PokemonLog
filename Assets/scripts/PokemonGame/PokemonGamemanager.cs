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

    // @ 선택 결과 및 팀(객체 배열 유지)
    public static Pokemon[] playerTeam3 = new Pokemon[3];
    public static Pokemon[] enemyTeam3 = new Pokemon[3];
    public static int playerActiveIndex = 0;
    public static int enemyActiveIndex = 0;

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

        // @ 필요시 이미지 클릭을 버튼으로 보장(현재 미사용이므로 주석 유지)
        WireChoiceImagesAsButtons();
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
            int filled = CountFilled(playerTeam3);
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

        enemyTeam3 = new Pokemon[3];

        for (int i = 0; i < 3; i++)
        {
            if (pool.Count > 0)
            {
                int r = Random.Range(0, pool.Count);
                Pokemon.PokemonIndex pick = pool[r];
                pool.RemoveAt(r);
                enemyTeam3[i] = CreateByIndex(pick);
            }
        }

        enemyActiveIndex = 0;
    }

    /// <summary> @ 팀 배열의 첫 null 인덱스 반환(없으면 -1) </summary>
    public static int FirstNullIndex(Pokemon[] arr)
    {
        if (arr == null) { return 0; }
        for (int i = 0; i < arr.Length; i++)
        {
            if (arr[i] == null) { return i; }
        }
        return -1;
    }

    /// <summary> @ 팀이 3칸 모두 찼는지 검사 </summary>
    private static bool TeamIsFull(Pokemon[] arr)
    {
        int idx = FirstNullIndex(arr);
        if (idx < 0) { return true; }
        return false;
    }

    /// <summary> @ 채워진 수 카운트 </summary>
    private static int CountFilled(Pokemon[] arr)
    {
        int c = 0;
        if (arr != null)
        {
            for (int i = 0; i < arr.Length; i++)
            {
                if (arr[i] != null) { c = c + 1; }
            }
        }
        return c;
    }

    /// <summary> @ 인덱스로 포켓몬 생성 </summary>
    private static Pokemon CreateByIndex(Pokemon.PokemonIndex idx)
    {
        if (idx == Pokemon.PokemonIndex.pikach) { return new Pika(); }
        if (idx == Pokemon.PokemonIndex.paily) { return new Paily(); }
        if (idx == Pokemon.PokemonIndex.goBook) { return new GoBook(); }
        return new Esang();
    }

    public static Pokemon SelectAvailablePokemon(bool forPlayer, bool includeCurrentSlot)
    {
        Pokemon[] team = forPlayer ? playerTeam3 : enemyTeam3;
        if (team == null || team.Length == 0)
        {
            if (forPlayer)
            {
                playerActiveIndex = -1;
                myPokemonG = null;
            }
            else
            {
                enemyActiveIndex = -1;
                otherPokemonG = null;
            }
            return null;
        }

        int length = team.Length;
        int currentIndex = forPlayer ? playerActiveIndex : enemyActiveIndex;
        if (currentIndex < 0 || currentIndex >= length)
        {
            currentIndex = 0;
        }

        int searchStart = includeCurrentSlot ? currentIndex : ((currentIndex + 1) % length);
        for (int offset = 0; offset < length; offset++)
        {
            int idx = (searchStart + offset) % length;
            Pokemon candidate = team[idx];
            if (candidate != null && candidate.Hp > 0)
            {
                if (forPlayer)
                {
                    playerActiveIndex = idx;
                    myPokemonG = candidate;
                }
                else
                {
                    enemyActiveIndex = idx;
                    otherPokemonG = candidate;
                }
                return candidate;
            }
        }

        if (forPlayer)
        {
            playerActiveIndex = -1;
            myPokemonG = null;
        }
        else
        {
            enemyActiveIndex = -1;
            otherPokemonG = null;
        }
        return null;
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

    // =========================================================
    // @ PokemonChoices : 이미지 클릭을 버튼으로 보장(람다 미사용)
    // =========================================================
    private void WireChoiceImagesAsButtons()
    {
        // 사용하지 않는 이미지 버튼 자동 바인딩은 주석으로 유지
        //AttachButtonIfMissingAndBind(Pika, OnClickPickPika);
        //AttachButtonIfMissingAndBind(Paily, OnClickPickPaily);
        //AttachButtonIfMissingAndBind(GoBook, OnClickPickGoBook);
        //AttachButtonIfMissingAndBind(eSang, OnClickPickEsang);
    }

    private void AttachButtonIfMissingAndBind(Image img, UnityEngine.Events.UnityAction handler)
    {
        if (img == null)
        {
            return;
        }

        Button b = img.GetComponent<Button>();
        if (b == null)
        {
            b = img.gameObject.AddComponent<Button>();
            ColorBlock cb = b.colors;
            cb.colorMultiplier = 1f;
            b.colors = cb;
            Navigation nav = b.navigation;
            nav.mode = Navigation.Mode.Automatic;
            b.navigation = nav;
            b.targetGraphic = img;
        }

        if (b != null)
        {
            b.onClick.RemoveAllListeners();
            b.onClick.AddListener(handler);
        }
    }

    // =========================================================
    // @ PokemonChoices : 선택 처리
    // =========================================================
    private void OnClickPickPika()
    {
        OnPokemonClick((int)Pokemon.PokemonIndex.pikach);
    }
    private void OnClickPickPaily()
    {
        OnPokemonClick((int)Pokemon.PokemonIndex.paily);
    }
    private void OnClickPickGoBook()
    {
        OnPokemonClick((int)Pokemon.PokemonIndex.goBook);
    }
    private void OnClickPickEsang()
    {
        OnPokemonClick((int)Pokemon.PokemonIndex.eSang);
    }
    
    /// <summary> @ 포켓몬 선택(새 이름) </summary>
    public void OnPokemonClick(int index)
    {
        Pokemon choice = CreateByIndex((Pokemon.PokemonIndex)index);

        int empty = FirstNullIndex(playerTeam3);
        if (empty >= 0)
        {
            playerTeam3[empty] = choice;
            if (empty == 0)
            {
                playerActiveIndex = 0;
            }
        }
        else
        {
            // @ 이미 3칸이 찼다면 첫 칸 교체
            playerTeam3[0] = choice;
            playerActiveIndex = 0;
        }

        // @ 플레이어 팀이 다 찼다면 적 팀도 준비되어야 함
        EnsureEnemyTeamIfNeeded(false);

        UpdateChoiceUI();
        UpdateGoBattleActive();
    }

    /// <summary> @ 플레이어 팀이 가득 찼다면 적 팀을 자동으로 구성 </summary>
    private void EnsureEnemyTeamIfNeeded(bool allowBuildEarly)
    {
        // @ 이미 만들어졌으면 패스
        if (_enemyBuiltOnce) { return; }

        bool playerFull = TeamIsFull(playerTeam3);
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
        OnClickGoBattle();
    }

    /// <summary> @ goBattle 눌렀을 때 처리 </summary>
    public void OnClickGoBattle()
    {
        // @ 안전 가드: 두 팀 모두 3마리여야 함
        bool playerFull = TeamIsFull(playerTeam3);
        if (playerFull)
        {
            bool enemyFull = TeamIsFull(enemyTeam3);
            if (enemyFull)
            {
                Pokemon playerActive = SelectAvailablePokemon(true, true);
                Pokemon enemyActive = SelectAvailablePokemon(false, true);

                if (playerActive != null && enemyActive != null)
                {
                    SceneManager.LoadScene("PokemonBattle");
                    return;
                }
            }
        }

        // @ 미충족 시 안내
        if (titleText != null)
        {
            titleText.text = "양 팀이 모두 준비되어야 전투를 시작할 수 있어.";
        }
    }

    /// <summary> @ goBattle 버튼 표시 여부 결정(SetActive) </summary>
    private void UpdateGoBattleActive()
    {
        bool show = false;

        bool playerFull = TeamIsFull(playerTeam3);
        if (playerFull)
        {
            bool enemyFull = TeamIsFull(enemyTeam3);
            if (enemyFull)
            {
                show = true;
            }
        }

        if (battleStart != null)
        {
            if (battleStart.activeSelf != show)
            {
                battleStart.SetActive(show);
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

        if (p == null)
        {
            p = new Pika();
        }

        if (isPlayer)
        {
            // @ 추가 설정이 필요한 경우 여기에
        }
        else
        {
            // @ 적 포켓몬 설정이 필요한 경우 여기에
        }

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

        dto.player[0] = (playerTeam3 != null && playerTeam3.Length > 0 && playerTeam3[0] != null) ? ToDTO(playerTeam3[0]) : null;
        dto.player[1] = (playerTeam3 != null && playerTeam3.Length > 1 && playerTeam3[1] != null) ? ToDTO(playerTeam3[1]) : null;
        dto.player[2] = (playerTeam3 != null && playerTeam3.Length > 2 && playerTeam3[2] != null) ? ToDTO(playerTeam3[2]) : null;

        dto.playerActive = playerActiveIndex;

        dto.enemy[0] = (enemyTeam3 != null && enemyTeam3.Length > 0 && enemyTeam3[0] != null) ? ToDTO(enemyTeam3[0]) : null;
        dto.enemy[1] = (enemyTeam3 != null && enemyTeam3.Length > 1 && enemyTeam3[1] != null) ? ToDTO(enemyTeam3[1]) : null;
        dto.enemy[2] = (enemyTeam3 != null && enemyTeam3.Length > 2 && enemyTeam3[2] != null) ? ToDTO(enemyTeam3[2]) : null;
        dto.enemyActive = enemyActiveIndex;

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
        playerTeam3 = new Pokemon[3];
        enemyTeam3 = new Pokemon[3];
        _enemyBuiltOnce = false;

        if (dto.player != null)
        {
            if (dto.player.Length > 0 && dto.player[0] != null) { playerTeam3[0] = FromDTO(dto.player[0], true); }
            if (dto.player.Length > 1 && dto.player[1] != null) { playerTeam3[1] = FromDTO(dto.player[1], true); }
            if (dto.player.Length > 2 && dto.player[2] != null) { playerTeam3[2] = FromDTO(dto.player[2], true); }
        }

        if (dto.enemy != null)
        {
            if (dto.enemy.Length > 0 && dto.enemy[0] != null) { enemyTeam3[0] = FromDTO(dto.enemy[0], false); }
            if (dto.enemy.Length > 1 && dto.enemy[1] != null) { enemyTeam3[1] = FromDTO(dto.enemy[1], false); }
            if (dto.enemy.Length > 2 && dto.enemy[2] != null) { enemyTeam3[2] = FromDTO(dto.enemy[2], false); }
            _enemyBuiltOnce = TeamIsFull(enemyTeam3);
        }

        playerActiveIndex = dto.playerActive;
        enemyActiveIndex = dto.enemyActive;

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

        playerTeam3 = new Pokemon[3];
        enemyTeam3 = new Pokemon[3];
        playerActiveIndex = 0;
        enemyActiveIndex = 0;
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
