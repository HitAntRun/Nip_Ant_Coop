using System;
using System.Collections;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// [임시 테스트 기능] 빌드에서도 동작하는 StoryTester 대용
// SC_Main → 게임시작(PlayPanel)에서 ₩ 3번 → 오른쪽 위 입력창에 스토리 ID 입력 + Enter
// → 이어하기가 해당 챕터로 지정됨. Shift+Esc로 닫기.
// 출시 전에 Enabled를 false로 바꾸거나 이 파일을 삭제할 것.
public class DebugStoryJump : MonoBehaviour
{
    public static readonly bool Enabled = true;

    const string MainSceneName = "SC_Main";
    const int PressCount = 3;
    const float PressWindow = 0.8f;   // 이 시간 안에 3번 눌러야 열림

    public static bool IsOpen { get; private set; }

    MainMenu mainMenu;
    GameObject panelRoot;
    TMP_InputField input;
    TMP_Text message;

    int presses;
    float firstPressTime;

    // ---------------- 자동 생성 (씬 수정 불필요) ----------------
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => IsOpen = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        if (!Enabled) return;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != MainSceneName) return;
        if (FindFirstObjectByType<DebugStoryJump>() != null) return;

        var menu = FindFirstObjectByType<MainMenu>(FindObjectsInactive.Include);
        if (menu == null) return;

        var go = new GameObject("DebugStoryJump");
        SceneManager.MoveGameObjectToScene(go, scene);
        go.AddComponent<DebugStoryJump>().Build(menu);
    }

    void OnDestroy() => IsOpen = false;

    // ---------------- 입력 ----------------
    void Update()
    {
        if (mainMenu == null) return;

        bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        if (IsOpen)
        {
            if (shift && Input.GetKeyDown(KeyCode.Escape)) Close();
            return;
        }

        if (!mainMenu.IsPlayOpen) { presses = 0; return; }

        if (WonPressed())
        {
            if (presses == 0 || Time.unscaledTime - firstPressTime > PressWindow)
            {
                presses = 0;
                firstPressTime = Time.unscaledTime;
            }
            if (++presses >= PressCount) { presses = 0; Open(); }
        }
    }

    // 한글 키보드 ₩ 키: Windows는 Backslash, Mac은 BackQuote 위치
    static bool WonPressed() =>
        Input.GetKeyDown(KeyCode.BackQuote) ||
        Input.GetKeyDown(KeyCode.Backslash) ||
        Input.inputString.Contains('₩');

    void Open()
    {
        IsOpen = true;
        panelRoot.SetActive(true);
        input.text = "";
        message.text = "Story ID (ex: CH3_1_1)";
        StartCoroutine(Refocus());
    }

    void Close()
    {
        IsOpen = false;
        input.DeactivateInputField();
        panelRoot.SetActive(false);
    }

    IEnumerator Refocus()
    {
        yield return null;
        input.Select();
        input.ActivateInputField();
    }

    void OnSubmit(string raw)
    {
        string name = raw.Trim();
        if (name.Length == 0) return;

        string id = FindStoryId(name);
        if (id == null)
        {
            message.text = $"<color=#FF7070>Not found: {name}</color>";
        }
        else
        {
            GameFlow.LastStoryStage = id;
            SaveManager.Save(force: true);
            mainMenu.RefreshPlayPanel();   // 이어하기 버튼 갱신
            message.text = $"<color=#7CFF8A>Continue -> {id}</color>";
        }

        input.text = "";
        StartCoroutine(Refocus());
    }

    static string FindStoryId(string name)
    {
        string dir = Application.streamingAssetsPath;
        if (!Directory.Exists(dir)) return null;
        return Directory.GetFiles(dir, "*.json")
            .Select(Path.GetFileNameWithoutExtension)
            .FirstOrDefault(n => string.Equals(n, name, StringComparison.OrdinalIgnoreCase));
    }

    // ---------------- UI 생성 ----------------
    void Build(MainMenu menu)
    {
        mainMenu = menu;

        var src = FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(t => t.font != null);
        TMP_FontAsset font = src != null ? src.font : TMP_Settings.defaultFontAsset;

        var canvasGo = new GameObject("DebugCanvas", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        panelRoot = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        panelRoot.SetActive(false);
        panelRoot.transform.SetParent(canvasGo.transform, false);
        var prt = (RectTransform)panelRoot.transform;
        prt.anchorMin = prt.anchorMax = prt.pivot = new Vector2(1, 1);
        prt.anchoredPosition = new Vector2(-20, -20);
        prt.sizeDelta = new Vector2(420, 110);
        panelRoot.GetComponent<Image>().color = new Color(0, 0, 0, 0.8f);

        message = CreateText("Message", panelRoot.transform, font, 20, new Color(1, 1, 1, 0.85f));
        var mrt = message.rectTransform;
        mrt.anchorMin = new Vector2(0, 1);
        mrt.anchorMax = new Vector2(1, 1);
        mrt.pivot = new Vector2(0.5f, 1);
        mrt.anchoredPosition = new Vector2(0, -10);
        mrt.sizeDelta = new Vector2(-24, 30);

        var fieldGo = new GameObject("Input", typeof(RectTransform), typeof(Image));
        fieldGo.transform.SetParent(panelRoot.transform, false);
        var frt = (RectTransform)fieldGo.transform;
        frt.anchorMin = new Vector2(0, 0);
        frt.anchorMax = new Vector2(1, 0);
        frt.pivot = new Vector2(0.5f, 0);
        frt.anchoredPosition = new Vector2(0, 12);
        frt.sizeDelta = new Vector2(-24, 46);
        var bg = fieldGo.GetComponent<Image>();
        bg.color = new Color(1, 1, 1, 0.12f);

        var area = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
        area.transform.SetParent(fieldGo.transform, false);
        var art = (RectTransform)area.transform;
        art.anchorMin = Vector2.zero;
        art.anchorMax = Vector2.one;
        art.offsetMin = new Vector2(10, 4);
        art.offsetMax = new Vector2(-10, -4);

        var placeholder = CreateText("Placeholder", area.transform, font, 24, new Color(1, 1, 1, 0.35f));
        Stretch(placeholder.rectTransform);
        placeholder.text = "CH3_1_1";
        placeholder.fontStyle = FontStyles.Italic;

        var text = CreateText("Text", area.transform, font, 24, Color.white);
        Stretch(text.rectTransform);

        input = fieldGo.AddComponent<TMP_InputField>();
        input.textViewport = art;
        input.textComponent = text;
        input.placeholder = placeholder;
        input.targetGraphic = bg;
        input.lineType = TMP_InputField.LineType.SingleLine;
        if (font != null) input.fontAsset = font;
        input.pointSize = 24;
        // ₩ / ` / \ 가 입력창에 찍히지 않게
        input.onValidateInput = (t, i, c) => (c == '₩' || c == '`' || c == '\\') ? '\0' : c;
        input.onSubmit.AddListener(OnSubmit);
    }

    static TextMeshProUGUI CreateText(string name, Transform parent, TMP_FontAsset font, float size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        t.fontSize = size;
        t.color = color;
        t.alignment = TextAlignmentOptions.MidlineLeft;
        t.raycastTarget = false;
        return t;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
