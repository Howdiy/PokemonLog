using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PokemonGamemanager : MonoBehaviour
{
    /// <summary> @ Setting 프리팹/연결 </summary>
    public GameObject settingsPrefab;         // Settings Prefab
    public Transform settingsParentHint;      // Settings Parent Hint
    public Transform settingsUiRootHint;      // Settings Ui Root Hint
    public Transform uiRoot;                  // Ui Root
    public int settingsSortOrder = 5000;      // 정렬 우선순위
    public GameObject settingsRef;            // Settings Ref

    /// <summary> @ 공용 UI </summary>
    public TextMeshProUGUI titleText;         // Title Text (TextMeshProUGUI)
    public Button[] bts;                      // 예비 버튼 슬롯

    /// <summary> @ Start 씬 전용 버튼 </summary>
    public Button StartBT;
    public Button ContinueBT;
    public Button ExitBT;

    /// <summary> @ Choices 씬 전용 버튼 </summary>
    public Button goBattleBt;                 // goBattle 오브젝트(씬 내 Canvas의 자식)

    /// <summary> @ 씬 인덱스 </summary>
    private const int SCENE_INDEX_PokemonStart = 0;  /// "PokemonStart"
    private const int SCENE_INDEX_PokemonBattle = 1;  /// "PokemonBattle"
    private const int SCENE_INDEX_PokemonChoices = 2;  /// "PokemonChoices"

    /// <summary> @ 저장 플래그 키 </summary>
    private const string SAVE_FLAG_KEY = "POKEMON_SAVE_FLAG_V1";

    /// <summary> @ 전역 전투 상태(씬 간 공유) </summary>
    public static Pokemon myPokemonG;
    public static Pokemon otherPokemonG;
    public static Pokemon[] playerTeam3 = new Pokemon[3];
    public static int playerActiveIndex = 0;
    public static Pokemon[] enemyTeam3 = new Pokemon[3];
    public static int enemyActiveIndex = 0;

    /// <summary> @ 저장 경로 </summary>
    private static string SavePath
    {
        get
        {
            string dir = Application.persistentDataPath;
            return Path.Combine(dir, "pokemon_save.json");
        }
    }

    // =========================================================
    // DTO
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
        public PokemonDTO[] player = new PokemonDTO[3];
        public int playerActive;
        public PokemonDTO[] enemy = new PokemonDTO[3];
        public int enemyActive;
        public int round;
        public string sceneName;
    }

    // =========================================================
    // 생성/변환 유틸
    // =========================================================
    private static Pokemon CreateByIndex(int index)
    {
        Pokemon p = null;
        if (index == 0) { p = new Pika(); }
        else if (index == 1) { p = new Paily(); }
        else if (index == 2) { p = new GoBook(); }
        else if (index == 3) { p = new Esang(); }
        return p;
    }

    private static int FirstNullIndex(Pokemon[] arr)
    {
        for (int i = 0; i < arr.Length; i++)
        {
            if (arr[i] == null)
            {
                return i;
            }
        }
        return -1;
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

    private static Pokemon FromDTO(PokemonDTO d)
    {
        if (d == null) { return null; }
        Pokemon p = null;
        if (d.idx == (int)Pokemon.PokemonIndex.pikach) { p = new Pika(); }
        else if (d.idx == (int)Pokemon.PokemonIndex.paily) { p = new Paily(); }
        else if (d.idx == (int)Pokemon.PokemonIndex.goBook) { p = new GoBook(); }
        else if (d.idx == (int)Pokemon.PokemonIndex.eSang) { p = new Esang(); }
        if (p == null) { return null; }

        p.Hp = d.hp;
        p.atk = d.atk;
        p.def = d.def;
        p.speed = d.speed;
        return p;
    }

    // =========================================================
    // Setting 연동
    // =========================================================
    private void EnsureSettingInstance()
    {
        if (settingsRef == null)
        {
            if (settingsPrefab != null)
            {
                Transform parentT = (settingsParentHint != null) ? settingsParentHint : uiRoot;
                GameObject go;
                if (parentT != null) { go = Instantiate(settingsPrefab, parentT); }
                else { go = Instantiate(settingsPrefab); }
                settingsRef = go;
            }
        }

        if (settingsRef != null)
        {
            Setting s = settingsRef.GetComponent<Setting>();
            if (s != null)
            {
                Transform uiT = (settingsUiRootHint != null) ? settingsUiRootHint : uiRoot;
                if (uiT != null) { s.uiRoot = uiT; }
                s.settingsSortOrder = settingsSortOrder;
            }
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

    // =========================================================
    // 저장 플래그/파일
    // =========================================================
    private static void MarkSaveFlag()
    {
        PlayerPrefs.SetInt(SAVE_FLAG_KEY, 1);
        PlayerPrefs.Save();
    }

    private static void ClearSaveFlag()
    {
        PlayerPrefs.DeleteKey(SAVE_FLAG_KEY);
        PlayerPrefs.Save();
    }

    private static bool HasSaveData()
    {
        int f = PlayerPrefs.GetInt(SAVE_FLAG_KEY, 0);
        if (f != 1) { return false; }
        if (!File.Exists(SavePath)) { return false; }
        return true;
    }

    public static void AutoSave(string sceneName)
    {
        SaveDTO dto = new SaveDTO();
        dto.player[0] = ToDTO(playerTeam3[0]);
        dto.player[1] = ToDTO(playerTeam3[1]);
        dto.player[2] = ToDTO(playerTeam3[2]);
        dto.playerActive = playerActiveIndex;

        dto.enemy[0] = ToDTO(enemyTeam3[0]);
        dto.enemy[1] = ToDTO(enemyTeam3[1]);
        dto.enemy[2] = ToDTO(enemyTeam3[2]);
        dto.enemyActive = enemyActiveIndex;

        dto.round = PokemonBattleManager.GetRoundSnapshot();
        dto.sceneName = sceneName;

        string json = JsonUtility.ToJson(dto, true);
        File.WriteAllText(SavePath, json);
        MarkSaveFlag();
    }

    private static void ApplySave(SaveDTO dto)
    {
        playerTeam3[0] = FromDTO(dto.player[0]);
        playerTeam3[1] = FromDTO(dto.player[1]);
        playerTeam3[2] = FromDTO(dto.player[2]);
        playerActiveIndex = dto.playerActive;

        enemyTeam3[0] = FromDTO(dto.enemy[0]);
        enemyTeam3[1] = FromDTO(dto.enemy[1]);
        enemyTeam3[2] = FromDTO(dto.enemy[2]);
        enemyActiveIndex = dto.enemyActive;

        myPokemonG = playerTeam3[playerActiveIndex];
        otherPokemonG = enemyTeam3[enemyActiveIndex];
        PokemonBattleManager.SetRoundFromSave(dto.round);
    }

    // =========================================================
    // 수명주기
    // =========================================================
    private void Start()
    {
        EnsureSettingInstance();

        string sceneName = SceneManager.GetActiveScene().name;

        if (sceneName == "PokemonStart")
        {
            bool hasSave = HasSaveData();
            if (ContinueBT != null)
            {
                ContinueBT.interactable = hasSave ? true : false;
            }
        }

        if (sceneName == "PokemonChoices")
        {
            if (goBattleBt != null)
            {
                goBattleBt.onClick.RemoveAllListeners();
                goBattleBt.onClick.AddListener(OnGbtClick);
                goBattleBt.interactable = false;
                if (!goBattleBt.gameObject.activeSelf) { goBattleBt.gameObject.SetActive(true); }
            }
        }
    }

    private void OnApplicationQuit()
    {
        string scene = SceneManager.GetActiveScene().name;
        AutoSave(scene);
    }

    // =========================================================
    // 버튼 콜백
    // =========================================================
    public void NewGame()
    {
        try
        {
            if (File.Exists(SavePath))
            {
                File.Delete(SavePath);
            }
        }
        catch (Exception e)
        {
            Debug.Log("세이브파일 삭제 실패");
            Debug.Log(e.Message);
        }
        ClearSaveFlag();

        Array.Clear(playerTeam3, 0, playerTeam3.Length);
        Array.Clear(enemyTeam3, 0, enemyTeam3.Length);
        playerActiveIndex = 0;
        enemyActiveIndex = 0;
        myPokemonG = null;
        otherPokemonG = null;

        SceneManager.LoadScene(SCENE_INDEX_PokemonChoices);  /// "PokemonChoices"
    }

    public void ContinueGame()
    {
        if (!HasSaveData()) { return; }
        SaveDTO dto = JsonUtility.FromJson<SaveDTO>(File.ReadAllText(SavePath));
        ApplySave(dto);

        int loadIndex = SCENE_INDEX_PokemonBattle;
        if (!string.IsNullOrEmpty(dto.sceneName))
        {
            if (dto.sceneName == "PokemonBattle")
            {
                loadIndex = SCENE_INDEX_PokemonBattle;
            }
            else if (dto.sceneName == "PokemonChoices")
            {
                loadIndex = SCENE_INDEX_PokemonChoices;
            }
            else
            {
                loadIndex = SCENE_INDEX_PokemonBattle;
            }
        }
        SceneManager.LoadScene(loadIndex);
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    /// <summary> @ Choices: 포켓몬 선택 </summary>
    public void OnPokemonClick(int index)
    {
        Pokemon choice = CreateByIndex(index);
        if (choice == null) { return; }

        int slot = FirstNullIndex(playerTeam3);
        if (slot >= 0)
        {
            playerTeam3[slot] = choice;
            if (titleText != null)
            {
                titleText.text = "선택 " + (slot + 1).ToString() + " / 3";
            }
        }

        bool full = (FirstNullIndex(playerTeam3) < 0) ? true : false;
        if (goBattleBt != null)
        {
            goBattleBt.interactable = full ? true : false;
        }
        if (titleText != null && full)
        {
            titleText.text = "선택 완료  전투 시작을 눌러줘";
        }
    }

    /// <summary> @ goBattle 클릭 → 검증 → 저장플래그 → 배틀 씬(인덱스) </summary>
    private void OnGbtClick()
    {
        Debug.Log("@ GBT_CLICK");

        if (FirstNullIndex(playerTeam3) >= 0)
        {
            Debug.Log("@ BLOCK | 팀 미완성");
            return;
        }

        BuildRandomEnemyTeam3();

        playerActiveIndex = 0;
        enemyActiveIndex = 0;
        myPokemonG = playerTeam3[playerActiveIndex];
        otherPokemonG = enemyTeam3[enemyActiveIndex];

        if (myPokemonG == null || otherPokemonG == null)
        {
            Debug.Log("@ BEFORE_LOAD | BLOCK: null team");
            return;
        }

        MarkSaveFlag();
        Debug.Log("@ LOAD index:1 // PokemonBattle");
        SceneManager.LoadScene(SCENE_INDEX_PokemonBattle);  /// "PokemonBattle"
    }

    /// <summary> @ 적 팀 3마리 무작위 구성(중복 없음) </summary>
    private static void BuildRandomEnemyTeam3()
    {
        List<int> pool = new List<int> { 0, 1, 2, 3 };
        int c0 = UnityEngine.Random.Range(0, pool.Count);
        int v0 = pool[c0]; pool.RemoveAt(c0);
        int c1 = UnityEngine.Random.Range(0, pool.Count);
        int v1 = pool[c1]; pool.RemoveAt(c1);
        int c2 = UnityEngine.Random.Range(0, pool.Count);
        int v2 = pool[c2]; pool.RemoveAt(c2);

        enemyTeam3[0] = CreateByIndex(v0);
        enemyTeam3[1] = CreateByIndex(v1);
        enemyTeam3[2] = CreateByIndex(v2);
    }
}
