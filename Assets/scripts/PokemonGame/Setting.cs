using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// @ 설정 패널 전담 컴포넌트(공용)
/// </summary>
public class Setting : MonoBehaviour
{
    public Transform uiRoot;
    public int settingsSortOrder = 5000;

    public GameObject settingsButton;
    public GameObject settingsPanel;

    public Button settingsRestartBtn;
    public Button settingsResumeBtn;
    public Button settingsExitBtn;

    public float initialPosX = 0f;
    public float initialPosY = 0f;

    public string restartSceneName = "";
    public string exitToStartSaveSceneName = "PokemonStart";

    private readonly List<GameObject> _hiddenBySettings = new List<GameObject>();

    void Awake()
    {
        WireButtons();
        ApplyInitialPlacement();
    }

    void WireButtons()
    {
        if (settingsButton != null)
        {
            if (settingsPanel != null)
            {
                if (settingsPanel.activeSelf)
                {
                    settingsPanel.SetActive(false);
                }
            }

            Button b = settingsButton.GetComponent<Button>();
            if (b != null)
            {
                b.onClick.RemoveAllListeners();
                b.onClick.AddListener(OnClickSettingsButton);
            }
        }

        if (settingsRestartBtn != null)
        {
            settingsRestartBtn.onClick.RemoveAllListeners();
            settingsRestartBtn.onClick.AddListener(OnClickSettingsRestart);
        }

        if (settingsResumeBtn != null)
        {
            settingsResumeBtn.onClick.RemoveAllListeners();
            settingsResumeBtn.onClick.AddListener(OnClickSettingsResume);
        }

        if (settingsExitBtn != null)
        {
            settingsExitBtn.onClick.RemoveAllListeners();
            settingsExitBtn.onClick.AddListener(OnClickSettingsExitToStart);
        }
    }

    void ApplyInitialPlacement()
    {
        if (settingsPanel != null)
        {
            Transform t = settingsPanel.transform;
            Vector3 p = t.localPosition;
            p.x = initialPosX;
            p.y = initialPosY;
            p.z = 0f;
            t.localPosition = p;
        }
    }

    public void OnClickSettingsButton()
    {
        if (settingsPanel == null) { return; }
        bool next = !settingsPanel.activeSelf;
        SetSettingsMode(next);
    }

    public void OnClickSettingsResume()
    {
        SetSettingsMode(false);
    }

    public void OnClickSettingsRestart()
    {
        SetSettingsMode(false);
        Scene active = SceneManager.GetActiveScene();
        // @ 현재 씬 재시작 (이름→인덱스)
        SceneManager.LoadScene(active.buildIndex);
    }

    public void OnClickSettingsExitToStart()
    {
        SetSettingsMode(false);
        if (exitToStartSaveSceneName != null)
        {
            if (exitToStartSaveSceneName.Length > 0)
            {
                SceneManager.LoadScene(0);   // @ "PokemonStart"
                return;
            }
        }
        SceneManager.LoadScene(0);
    }

    public void SetSettingsMode(bool on)
    {
        if (settingsPanel == null) { return; }

        if (on)
        {
            // @ 패널만 활성화 & 항상 위로 정렬은 기존 BringSettingsToFront로 처리
            BringSettingsToFront();
            settingsPanel.SetActive(true);

            // @ 자식 버튼들도 보장 @ 비활성화였다면 활성화
            if (settingsRestartBtn != null)
            {
                settingsRestartBtn.gameObject.SetActive(true);
            }
            if (settingsResumeBtn != null)
            {
                settingsResumeBtn.gameObject.SetActive(true);
            }
            if (settingsExitBtn != null)
            {
                settingsExitBtn.gameObject.SetActive(true);
            }
        }
        else
        {
            // @ 패널만 비활성화 @ 다른 오브젝트는 건드리지 않음
            settingsPanel.SetActive(false);
        }
    }

    void CacheAndHideChildren(Transform root, GameObject ignore)
    {
        int n = root.childCount;
        for (int i = 0; i < n; i++)
        {
            Transform c = root.GetChild(i);
            if (ignore != null)
            {
                if (c == ignore.transform) { continue; }
            }
            bool ok = IsDescendant(ignore != null ? ignore.transform : null, c);
            if (!ok)
            {
                GameObject go = c.gameObject;
                if (go.activeSelf)
                {
                    _hiddenBySettings.Add(go);
                    go.SetActive(false);
                }
            }
        }
    }

    void RevealHidden()
    {
        for (int i = 0; i < _hiddenBySettings.Count; i++)
        {
            if (_hiddenBySettings[i] != null)
            {
                _hiddenBySettings[i].SetActive(true);
            }
        }
        _hiddenBySettings.Clear();
    }

    Transform GetUIRoot()
    {
        if (uiRoot != null) { return uiRoot; }
        if (settingsPanel != null) { return settingsPanel.transform.parent; }
        return null;
    }

    bool IsDescendant(Transform parent, Transform t)
    {
        if (parent == null) { return false; }
        if (t == null) { return false; }
        return t.IsChildOf(parent);
    }
    /// <summary>
    /// @ 런타임: uiRoot가 비어 있을 때만 씬의 Canvas(또는 힌트)로 지정
    /// </summary>
    public void EnsureUiRootAtRuntime(Transform root)
    {
        if (uiRoot == null)
        {
            uiRoot = root;
        }
    }

    /// <summary>
    /// @ 런타임: 설정 패널을 화면 중앙(0,0,0)으로 강제 배치
    /// </summary>
    public void CenterPanelAtRuntime()
    {
        if (settingsPanel != null)
        {
            Transform t = settingsPanel.transform;
            Vector3 p = t.localPosition;
            p.x = 0f;
            p.y = 0f;
            p.z = 0f;
            t.localPosition = p;

            // @ 초기값도 중앙으로 동기화
            initialPosX = 0f;
            initialPosY = 0f;
        }
    }

    /// <summary>
    /// @ 런타임: Nested Canvas 정렬 무시 + 정렬 순서 지정(최상단 보장)
    /// </summary>
    public void EnsureNestedCanvasTopmostAtRuntime(int order)
    {
        Canvas sc = GetComponent<Canvas>();
        if (sc == null)
        {
            sc = gameObject.AddComponent<Canvas>();
            gameObject.AddComponent<GraphicRaycaster>();
        }
        sc.overrideSorting = true;
        sc.sortingOrder = order;
    }

    // @ 패널을 항상 위로 보이게 정렬 보장 + 같은 부모 내 마지막 형제로 이동
    public void BringSettingsToFront()
    {
        // @ Nested Canvas 최상단 정렬 보장
        EnsureNestedCanvasTopmostAtRuntime(settingsSortOrder);

        // @ 같은 Canvas 안에서 가장 위로
        if (settingsPanel != null) { settingsPanel.transform.SetAsLastSibling();}
        // @ 패널이 비어 있으면 루트 자체를 위로
        else { transform.SetAsLastSibling(); }
    }
}
