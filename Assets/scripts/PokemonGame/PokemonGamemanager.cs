using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PokemonGamemanager : MonoBehaviour
{
    // 씬의 인텍스
    public const int SCENE_INDEX_PokemonStart = 0;
    public const int SCENE_INDEX_PokemonBattle = 1;
    public const int SCENE_INDEX_PokemonChoices = 2;

    // 저장 키값
    private const string SAVE_FLAG_KEY = "POKEMON_SAVE_FLAG_V1";

    // 저장 벨류
    private static string SavePath
    {
        get
        {
            string p = Application.persistentDataPath + "/pokemon_save_v1.json";
            return p;
        }
    }

    // 'Setting'Prefab 연결 필드
    [Header("Setting Prefab Link")]
    [SerializeField] private GameObject settingsPrefab;   // 프리팹
    [SerializeField] private Transform uiRoot;            // 프리팹의 부모 위치지정
    private GameObject _settingsInst;                     // 런타임중 인스턴스
    private Setting _settingsRef;                         // Component Ref

    // PokemonStart씬의 연결할 필드
    [Header("Start Scene UI")]
    [SerializeField] private Button startBt;
    [SerializeField] private Button continueBt;
    [SerializeField] private Button exitBt;

    // PokemonChoices씬의 연결할 필드
    [Header("Choices Scene UI")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private Button goBattleBt;  // @ Only This One
    [SerializeField] private Button pika;
    [SerializeField] private Button paily;
    [SerializeField] private Button goBook;
    [SerializeField] private Button eSang;

    // 팀의 역할을 할 리스트 할당
    public static List<Pokemon> PlayerTeam = new List<Pokemon>(3);
    public static List<Pokemon> EnemyTeam = new List<Pokemon>(3);

    // 적의 팀 구성완료 상태 확인용
    private static bool _enemyBuiltOnce = false;

    // 팀의 포켓몬 저장
    [Serializable]
    private class SaveDTO
    {
        public int[] playerTeamIdx = new int[3];
        public int[] enemyTeamIdx = new int[3];
    }

    /// <summary>
    /// 런타임 사전 세팅
    /// </summary>
    private void Awake()
    {   // 설정프리팹 생성
        EnsureSettingsInstanceOrBind();
    }

    private void Start()
    {
        int cur = SceneManager.GetActiveScene().buildIndex;
        if (cur == SCENE_INDEX_PokemonStart)
        {
            InitStartScene();
            return;
        }

        if (cur == SCENE_INDEX_PokemonChoices)
        {
            InitChoicesScene();
            return;
        }
        // @ Nothing Here
        if (cur == SCENE_INDEX_PokemonBattle)   { return; }
    }

    /// <summary>
    /// 설정프리팹 인스턴트화 와 바인딩 설정
    /// </summary>
    private void EnsureSettingsInstanceOrBind()
    {
        // 현재씬 인스턴스화 시도
        Setting exists = GameObject.FindObjectOfType<Setting>();
        if (exists != null)
        {
            _settingsRef = exists;
            return;
        }

        // 프리팹이 없는 경우 씬에서 인스턴트화하기
        if (_settingsRef == null)
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
                else   { _settingsInst = GameObject.Instantiate(settingsPrefab); }

                if (_settingsInst != null)
                {
                    if (_settingsInst.activeSelf == false)  { _settingsInst.SetActive(true); }
                    _settingsRef = _settingsInst.GetComponent<Setting>();
                }
            }
        }
    }

    /// <summary>
    /// PokemonStart씬에서 사용
    /// </summary>
    private void InitStartScene()
    {
        WireButton(startBt, OnClickStart);
        WireButton(exitBt, OnClickExit);

        bool hasFlag = PlayerPrefs.GetInt(SAVE_FLAG_KEY, 0) == 1;
        bool hasFile = File.Exists(SavePath);
        bool canContinue = false;
        if (hasFlag)
        {
            if (hasFile)
            {
                canContinue = true;
            }
        }

        if (continueBt != null)
        {
            continueBt.gameObject.SetActive(canContinue ? true : false);
            if (canContinue)
            {
                WireButton(continueBt, OnClickContinue);
            }
            else
            {
                continueBt.onClick.RemoveAllListeners();
            }
        }
    }

    /// <summary>
    /// PokemonChoices씬에서 사용
    /// </summary>
    private void InitChoicesScene()
    {
        // 플레이어 팀의 초기화
        if (PlayerTeam == null) { PlayerTeam = new List<Pokemon>(3); }
        PlayerTeam.Clear();

        // 적 팀의 초기화
        if (!_enemyBuiltOnce)
        {
            EnemyTeam = BuildRandomEnemyTeam3();
            _enemyBuiltOnce = true;
        }

        WireButton(pika, OnClickPika);
        WireButton(paily, OnClickPaily);
        WireButton(goBook, OnClickGoBook);
        WireButton(eSang, OnClickEsang);

        // goBattle버튼 비활성화 
        if (goBattleBt != null)
        {
            goBattleBt.onClick.RemoveAllListeners();
            goBattleBt.onClick.AddListener(OnGbtClick);
            goBattleBt.gameObject.SetActive(false);
        }

        if (titleText != null)
        {
            titleText.text = "포켓몬 선택 시작";
        }
    }

    private static void WireButton(Button bt, UnityEngine.Events.UnityAction action)
    {
        if (bt == null) { return; }
        bt.onClick.RemoveAllListeners();
        bt.onClick.AddListener(action);
    }

    // 포켓몬 선택버튼 
    private void OnClickPika() { OnPokemonClick(0); }
    private void OnClickPaily() { OnPokemonClick(1); }
    private void OnClickGoBook() { OnPokemonClick(2); }
    private void OnClickEsang() { OnPokemonClick(3); }

    /// <summary>
    /// 플레이어의 팀 구성 관련
    /// </summary>
    public void OnPokemonClick(int index)
    {
        if (PlayerTeam == null) { PlayerTeam = new List<Pokemon>(3); }
        // 플레이어 팀에 3마리가 있는지 확인용
        int canAdd = (PlayerTeam.Count < 3) ? 1 : 0;
        if (canAdd == 0)
        {
            if (titleText != null)  { titleText.text = "3 마리 선택 완료"; }
            return;
        }

        Pokemon p = CreateByIndex(index);
        PlayerTeam.Add(p);

        if (titleText != null)
        {
            int c = PlayerTeam.Count;
            titleText.text = p.name + " 선택 " + c.ToString() + " / 3";
        }

        int isFull = (PlayerTeam.Count == 3) ? 1 : 0;
        if (isFull == 1)
        {
            if (goBattleBt != null) { goBattleBt.gameObject.SetActive(true); }
            if (titleText != null)  { titleText.text = "전투 시작 준비 완료"; }
        }
    }

    // goBattle버튼 활성화 조건
    private void OnGbtClick()
    {
        // 플레이어와 적 모두 팀에 포켓몬 3마리가 존재해야 활성화됨
        int okPlayer = (PlayerTeam != null) ? PlayerTeam.Count : 0;
        int okEnemy = (EnemyTeam != null) ? EnemyTeam.Count : 0;

        if (okPlayer == 3)
        {
            if (okEnemy == 3)
            {
                MarkSaveFlag();
                SaveToFile();
                SceneManager.LoadScene(SCENE_INDEX_PokemonBattle);
                return;
            }
        }

        if (titleText != null)  { titleText.text = "팀 준비가 부족합니다"; }
    }

    /// <summary>
    /// PokemonStart씬 관련
    /// </summary>
    private void OnClickStart()
    {
        // @ New Game Reset
        PlayerTeam = new List<Pokemon>(3);
        EnemyTeam = new List<Pokemon>(3);
        _enemyBuiltOnce = false;
        PlayerPrefs.SetInt(SAVE_FLAG_KEY, 0);
        PlayerPrefs.Save();
        SceneManager.LoadScene(SCENE_INDEX_PokemonChoices);
    }

    private void OnClickContinue()
    {
        bool hasFlag = PlayerPrefs.GetInt(SAVE_FLAG_KEY, 0) == 1;
        bool hasFile = File.Exists(SavePath);

        if (hasFlag)
        {
            if (hasFile)
            {
                bool ok = LoadFromFile();
                if (ok)
                {
                    SceneManager.LoadScene(SCENE_INDEX_PokemonBattle);
                    return;
                }
            }
        }

        if (titleText != null)  { titleText.text = "저장 데이터를 찾을 수 없음"; }
    }

    private void OnClickExit()
    {
        Application.Quit();
    }

    /// <summary>
    /// 선택한 포켓몬으로 팀 생성
    /// </summary>
    private static List<Pokemon> BuildRandomEnemyTeam3()
    {
        List<int> pool = new List<int>();
        pool.Add(0);
        pool.Add(1);
        pool.Add(2);
        pool.Add(3);

        List<Pokemon> team = new List<Pokemon>(3);

        for (int i = 0; i < 3; i = i + 1)
        {
            if (pool.Count <= 0)
            {
                pool.Add(0); pool.Add(1); pool.Add(2); pool.Add(3);
            }
            int pick = UnityEngine.Random.Range(0, pool.Count);
            int idx = pool[pick];
            pool.RemoveAt(pick);
            team.Add(CreateByIndex(idx));
        }

        return team;
    }

    private static Pokemon CreateByIndex(int idx)
    {
        if (idx == 0) { return new Pika(); }
        if (idx == 1) { return new Paily(); }
        if (idx == 2) { return new GoBook(); }
        return new Esang();
    }

    private static int GetIndexFromPokemon(Pokemon p)
    {
        if (p is Pika) { return 0; }
        if (p is Paily) { return 1; }
        if (p is GoBook) { return 2; }
        return 3;
    }

    /// <summary>
    /// 저장 및 불러오기
    /// </summary>
    private void MarkSaveFlag()
    {
        PlayerPrefs.SetInt(SAVE_FLAG_KEY, 1);
        PlayerPrefs.Save();
    }

    private void SaveToFile()
    {
        try
        {
            SaveDTO dto = new SaveDTO();

            if (PlayerTeam == null) { PlayerTeam = new List<Pokemon>(3); }
            if (EnemyTeam == null) { EnemyTeam = new List<Pokemon>(3); }

            int i = 0;
            while (i < 3)
            {
                if (i < PlayerTeam.Count)
                {
                    dto.playerTeamIdx[i] = GetIndexFromPokemon(PlayerTeam[i]);
                }
                else
                {
                    dto.playerTeamIdx[i] = 0;
                }
                i = i + 1;
            }

            int j = 0;
            while (j < 3)
            {
                if (j < EnemyTeam.Count)
                {
                    dto.enemyTeamIdx[j] = GetIndexFromPokemon(EnemyTeam[j]);
                }
                else
                {
                    dto.enemyTeamIdx[j] = 0;
                }
                j = j + 1;
            }

            string json = JsonUtility.ToJson(dto);
            File.WriteAllText(SavePath, json);
        }
        catch (Exception e)
        {
            Debug.Log("세이브파일 저장 실패");
            Debug.Log(e.Message);
        }
    }

    private bool LoadFromFile()
    {
        try
        {
            bool exists = File.Exists(SavePath);
            if (!exists) { return false; }

            string json = File.ReadAllText(SavePath);
            SaveDTO dto = JsonUtility.FromJson<SaveDTO>(json);

            PlayerTeam = new List<Pokemon>(3);
            EnemyTeam = new List<Pokemon>(3);

            int i = 0;
            while (i < 3)
            {
                int idx = dto.playerTeamIdx[i];
                PlayerTeam.Add(CreateByIndex(idx));
                i = i + 1;
            }

            int j = 0;
            while (j < 3)
            {
                int idx = dto.enemyTeamIdx[j];
                EnemyTeam.Add(CreateByIndex(idx));
                j = j + 1;
            }

            return true;
        }
        catch (Exception e)
        {
            Debug.Log("세이브파일 로드 실패");
            Debug.Log(e.Message);
            return false;
        }
    }

    /// <summary>
    /// 공동 유틸 파트 
    /// </summary>
    // 저장된 첫번째 포켓몬의 인덱스 반환. 없으면 -1
    public static int FirstAliveIndex(List<Pokemon> team)
    {
        if (team == null) { return -1; }
        int i = 0;
        while (i < team.Count)
        {
            Pokemon p = team[i];
            if (p != null)
            {
                if (p.Hp > 0)
                {
                    return i;
                }
            }
            i = i + 1;
        }
        return -1;
    }
    // 사용 가능한 포켓몬 선택
    public static Pokemon SelectAvailablePokemon(bool preferFirst, bool allowZeroHp)
    {
        if (PlayerTeam == null) { return null; }

        if (preferFirst)
        {
            int fi = FirstAliveIndex(PlayerTeam);
            if (fi >= 0)    { return PlayerTeam[fi]; }
        }

        int i = 0;
        while (i < PlayerTeam.Count)
        {
            Pokemon p = PlayerTeam[i];
            if (p != null)
            {
                if (allowZeroHp)    { return p; }
                else
                {   if (p.Hp > 0)   { return p; }   }
            }
            i = i + 1;
        }
        return null;
    }

    // 프로그램 종료시 자동저장
    private void OnApplicationQuit()
    {
        if (PlayerTeam != null)
        {
            int ok = (PlayerTeam.Count == 3) ? 1 : 0;
            if (ok == 1)
            {
                MarkSaveFlag();
                SaveToFile();
            }
        }
    }
}
