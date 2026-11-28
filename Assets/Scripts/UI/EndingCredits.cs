using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

// 엔딩 크레딧: The → End → 플레이 기록(타이틀 → 숫자) → 등장인물 스크롤 → Directed by → Thank you → 메인
// UI는 실행 시 코드로 생성 (씬에는 이 컴포넌트 하나만 있으면 됨)
public class EndingCredits : MonoBehaviour
{
    [Serializable]
    public class CastEntry
    {
        public string locKey;   // UIDataTable 키 (char_xxx). 없으면 비워두고 ko/en 사용
        public string ko;
        public string en;
        public CastEntry() { }
        public CastEntry(string locKey, string ko, string en) { this.locKey = locKey; this.ko = ko; this.en = en; }
    }

    [Header("Data")]
    [SerializeField] private TitleTable titleTable;
    [SerializeField] private string mainSceneName = "SC_Main";
    [SerializeField] private string endABgm = "BGM_End_A";
    [SerializeField] private string endBBgm = "BGM_End_B";
    [SerializeField] private bool useTitleColor = false;   // 칭호 색 사용 (어두운 색은 검은 배경에서 잘 안 보임)

    [Header("Text Style")]
    [SerializeField] private TMP_FontAsset font;
    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private float theEndSize = 120f;
    [SerializeField] private float recordSize = 44f;
    [SerializeField] private Color recordLabelColor = new Color(0.72f, 0.72f, 0.72f, 1f);  // 기록 타이틀 (회색)
    [SerializeField] private Color recordValueColor = new Color(1f, 0.82f, 0.35f, 1f);    // 기록 숫자 (노란색)
    [SerializeField] private float recordLabelWidth = 440f;
    [SerializeField] private float recordValueWidth = 220f;
    [SerializeField] private float castHeaderSize = 60f;
    [SerializeField] private float castSize = 46f;
    [SerializeField] private float centerSize = 64f;

    [Header("The End")]
    [SerializeField] private float startDelay = 1f;
    [SerializeField] private float theToEndDelay = 0.8f;
    [SerializeField] private float afterTheEndDelay = 1.5f;

    [Header("Play Record")]
    [SerializeField] private float labelToValueDelay = 0.5f;
    [SerializeField] private float rowDelay = 0.7f;
    [SerializeField] private float afterRecordDelay = 2.5f;

    [Header("Cast Scroll")]
    [SerializeField] private float scrollSpeed = 70f;       // 1080 기준 px/초
    [SerializeField] private float fastMultiplier = 4f;     // 클릭 / Space 누르고 있으면 빨라짐
    [SerializeField] private float recordToCastGap = 600f;
    [SerializeField] private float castSpacing = 70f;
    [SerializeField] private CastEntry[] cast =
    {
        new CastEntry("char_emil",      "에밀",          "Emil"),
        new CastEntry("char_teacher",   "선생 개미",      "Teacher Ant"),
        new CastEntry("char_student1",  "학생 개미1",     "Student Ant1"),
        new CastEntry("char_student2",  "학생 개미2",     "Student Ant2"),
        new CastEntry("",               "흰개미",        "Termite"),
        new CastEntry("char_soldier",   "병정 개미",      "Soldier Ant"),
        new CastEntry("char_official1", "간부 개미1",     "Official Ant1"),
        new CastEntry("char_official2", "간부 개미2",     "Official Ant2"),
        new CastEntry("char_principal", "교장 개미",      "Principal Ant"),
        new CastEntry("",               "검은 여왕 개미",  "Black Queen Ant"),
        new CastEntry("",               "흰 여왕 개미",    "White Queen Ant"),
    };

    [Header("Center Fade")]
    [SerializeField] private string directedBy = "Directed by HitAndRun";
    [SerializeField] private string thankYou = "Thank you for playing";
    [SerializeField] private float fadeTime = 1f;
    [SerializeField] private float holdTime = 2f;
    [SerializeField] private float betweenDelay = 0.5f;

    [Header("Termite Silhouette")]
    [SerializeField] private Sprite termiteSilhouette;
    [SerializeField] private float termiteHeight = 300f;      // 화면에 보이는 상반신 높이
    [SerializeField] private float termiteDashTime = 0.6f;    // 오른쪽 → 왼쪽 통과 시간
    [SerializeField] private float termiteBobHeight = 12f;    // 달릴 때 위아래 흔들림
    [SerializeField] private float termiteBobSpeed = 30f;
    [SerializeField] private Color backlightColor = Color.black; // 지나가는 순간의 배경색 (검정이면 빨간 눈만 보임)
    [SerializeField] private float backlightFade = 0.15f;

    private Image bgImage;
    private RectTransform termite;
    private RectTransform canvasRect;
    private RectTransform content;
    private LayoutElement topSpacer;
    private RectTransform theEndRow;
    private TMP_Text theText, endText, centerText;
    private TMP_Text[] labels, values;

    private static bool IsEnglish
    {
        get
        {
            var l = LocalizationSettings.SelectedLocale;
            return l != null && l.Identifier.Code.StartsWith("en");
        }
    }
    private static string L(string ko, string en) => IsEnglish ? en : ko;

    private IEnumerator Start()
    {
        yield return LocalizationSettings.InitializationOperation;

        BuildUI();
        FillRecords();
        HideAll();

        // The End가 화면 세로 중앙에 오도록 위쪽 여백 계산
        yield return null;
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        topSpacer.preferredHeight = Mathf.Max(0f, canvasRect.rect.height * 0.5f - theEndRow.rect.height * 0.5f);
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        content.anchoredPosition = Vector2.zero;

        // 1. The → End
        yield return new WaitForSeconds(startDelay);
        theText.alpha = 1f;
        yield return new WaitForSeconds(theToEndDelay);
        endText.alpha = 1f;
        yield return new WaitForSeconds(afterTheEndDelay);

        // 2. 플레이 기록: 타이틀 → 숫자, 한 줄씩
        for (int i = 0; i < labels.Length; i++)
        {
            labels[i].alpha = 1f;
            yield return new WaitForSeconds(labelToValueDelay);
            values[i].alpha = 1f;
            yield return new WaitForSeconds(rowDelay);
        }
        yield return new WaitForSeconds(afterRecordDelay);

        // 3. 등장인물 스크롤: 콘텐츠 맨 아래가 화면 위로 다 올라갈 때까지
        float targetY = content.rect.height;
        while (content.anchoredPosition.y < targetY)
        {
            bool fast = Input.GetMouseButton(0) || Input.GetKey(KeyCode.Space);
            float speed = scrollSpeed * (fast ? fastMultiplier : 1f);
            content.anchoredPosition += Vector2.up * speed * Time.deltaTime;
            yield return null;
        }

        // 4~5. 가운데 페이드 인/아웃
        yield return new WaitForSeconds(betweenDelay);
        yield return FadeInOut(centerText, directedBy);
        yield return new WaitForSeconds(betweenDelay);
        yield return FadeInOut(centerText, thankYou);

        // 7. 흰개미 실루엣이 빠르게 지나감
        yield return new WaitForSeconds(0.4f);
        yield return TermiteDash();
        yield return new WaitForSeconds(0.6f);

        // 6. 클리어: 세이브 초기화 → 메인에는 새 게임만 남음
        SaveManager.DeleteSave();
        SceneRouter.LoadDirect(mainSceneName);
    }

    // ───────── 플레이 기록 (ProfilePanel과 같은 데이터·계산) ─────────

    private void FillRecords()
    {
        int found = GameFlow.TotalTermitesFound;
        int wrong = GameFlow.TotalWrongReports;
        int tries = found + wrong;
        float acc = tries == 0 ? 0f : (float)found / tries;

        values[0].text = found.ToString();
        values[1].text = wrong.ToString();
        values[2].text = tries == 0 ? "-" : $"{acc * 100f:0.#}%";

        if (titleTable != null && titleTable.tiers != null && titleTable.tiers.Length >= 2)
        {
            TitleTier title = titleTable.CurrentTitle(found, acc);
            values[3].text = titleTable.Name(title);
            if (useTitleColor) values[3].color = title.color;
        }
        else values[3].text = "-";
    }

    private void HideAll()
    {
        theText.alpha = 0f;
        endText.alpha = 0f;
        foreach (var t in labels) t.alpha = 0f;
        foreach (var t in values) t.alpha = 0f;
        centerText.alpha = 0f;
    }

    // ───────── 페이드 ─────────

    private IEnumerator FadeInOut(TMP_Text text, string str)
    {
        text.text = str;
        yield return Fade(text, 0f, 1f);
        yield return new WaitForSeconds(holdTime);
        yield return Fade(text, 1f, 0f);
    }

    private IEnumerator Fade(TMP_Text text, float from, float to)
    {
        float t = 0f;
        while (t < fadeTime)
        {
            t += Time.deltaTime;
            text.alpha = Mathf.Lerp(from, to, t / fadeTime);
            yield return null;
        }
        text.alpha = to;
    }

    // ───────── 흰개미 실루엣 ─────────

    private IEnumerator TermiteDash()
    {
        if (termite == null) yield break;

        yield return FadeBackground(Color.black, backlightColor);
        termite.gameObject.SetActive(true);

        float endX = -(canvasRect.rect.width + termite.sizeDelta.x);
        float t = 0f;
        while (t < termiteDashTime)
        {
            t += Time.deltaTime;
            float x = Mathf.Lerp(0f, endX, t / termiteDashTime);
            // 0 ~ -bob 사이로만 흔들어서 아래쪽에 빈틈이 생기지 않게
            float y = Mathf.Abs(Mathf.Sin(t * termiteBobSpeed)) * termiteBobHeight - termiteBobHeight;
            termite.anchoredPosition = new Vector2(x, y);
            yield return null;
        }

        termite.gameObject.SetActive(false);
        yield return FadeBackground(backlightColor, Color.black);
    }

    private IEnumerator FadeBackground(Color from, Color to)
    {
        float t = 0f;
        while (t < backlightFade)
        {
            t += Time.deltaTime;
            bgImage.color = Color.Lerp(from, to, t / backlightFade);
            yield return null;
        }
        bgImage.color = to;
    }

    // ───────── UI 생성 ─────────

    // 첫 프레임 전에 검은 배경부터 깔아서 씬 전환 직후 빈 화면이 보이지 않게
    private void Awake()
    {
        BuildCanvas();

        // 직전 엔딩의 BGM을 이어서 재생 (이미 같은 곡이 나오고 있으면 끊기지 않고 계속됨)
        SoundManager.EnsureExists();
        SoundManager.instance?.StopAllSfx(); 
        string bgmId = GameFlow.LastStoryStage == "End_B" ? endBBgm : endABgm;
        SoundManager.instance?.PlayBgmById(bgmId);
    }

    private void BuildCanvas()
    {
        var canvasGo = new GameObject("CreditsCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        canvasGo.transform.SetParent(transform, false);
        canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasRect = (RectTransform)canvasGo.transform;

        var bg = NewRect("Background", canvasRect);
        Stretch(bg);
        bgImage = bg.gameObject.AddComponent<Image>();
        bgImage.color = Color.black;
        bgImage.raycastTarget = false;
    }

    private void BuildUI()
    {
        // 스크롤 콘텐츠 (위쪽 가운데 기준)
        content = NewRect("ScrollContent", canvasRect);
        content.anchorMin = content.anchorMax = new Vector2(0.5f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.sizeDelta = new Vector2(1600f, 0f);
        var vlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        topSpacer = Spacer(content, 0f);

        // The End
        theEndRow = NewRow("TheEnd", content, theEndSize * 0.3f);
        theText = NewText("The", theEndRow, "The", theEndSize, TextAlignmentOptions.Center);
        endText = NewText("End", theEndRow, "End", theEndSize, TextAlignmentOptions.Center);

        Spacer(content, 120f);

        // 플레이 기록
        string[] labelTexts =
        {
            L("신고한 흰개미 수", "Termites Reported"),
            L("잘못 신고한 횟수", "False Reports"),
            L("신고 정확도", "Report Accuracy"),
            L("칭호", "Title"),
        };
        labels = new TMP_Text[labelTexts.Length];
        values = new TMP_Text[labelTexts.Length];
        for (int i = 0; i < labelTexts.Length; i++)
        {
            var row = NewRow($"Record_{i}", content, 40f);
            // 타이틀·숫자 모두 왼쪽 정렬 + 고정 폭 → 줄마다 같은 세로선에서 시작
            labels[i] = NewText("Label", row, labelTexts[i], recordSize, TextAlignmentOptions.Left);
            values[i] = NewText("Value", row, "", recordSize, TextAlignmentOptions.Left);
            labels[i].color = recordLabelColor;
            values[i].color = recordValueColor;
            SetWidth(labels[i], recordLabelWidth);
            SetWidth(values[i], recordValueWidth);
            if (i < labelTexts.Length - 1) Spacer(content, 24f);
        }

        // 등장인물
        Spacer(content, recordToCastGap);
        NewText("CastHeader", content, L("등장인물", "Cast"), castHeaderSize, TextAlignmentOptions.Center);
        Spacer(content, castSpacing * 1.5f);
        foreach (var c in cast)
        {
            string name = Loc.Get(Loc.UI, c.locKey, IsEnglish ? c.en : c.ko);
            NewText("Cast", content, name, castSize, TextAlignmentOptions.Center);
            Spacer(content, castSpacing);
        }

        // 가운데 페이드 텍스트 (스크롤과 별개)
        centerText = NewText("CenterText", canvasRect, "", centerSize, TextAlignmentOptions.Center);
        var crt = centerText.rectTransform;
        crt.anchorMin = crt.anchorMax = crt.pivot = new Vector2(0.5f, 0.5f);
        crt.sizeDelta = new Vector2(1600f, 300f);
        crt.anchoredPosition = Vector2.zero;

        // 흰개미 실루엣: 화면 오른쪽 밖, 아래 가장자리에 붙여서 상반신만 보이게
        if (termiteSilhouette != null)
        {
            termite = NewRect("TermiteSilhouette", canvasRect);
            var img = termite.gameObject.AddComponent<Image>();
            img.sprite = termiteSilhouette;
            img.raycastTarget = false;
            float aspect = termiteSilhouette.rect.width / termiteSilhouette.rect.height;
            termite.anchorMin = termite.anchorMax = new Vector2(1f, 0f);
            termite.pivot = Vector2.zero;
            termite.sizeDelta = new Vector2(termiteHeight * aspect, termiteHeight);
            termite.anchoredPosition = Vector2.zero;
            termite.gameObject.SetActive(false);
        }
    }

    private static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    private static LayoutElement Spacer(Transform parent, float height)
    {
        var le = NewRect("Spacer", parent).gameObject.AddComponent<LayoutElement>();
        le.preferredHeight = height;
        return le;
    }

    private static RectTransform NewRow(string name, Transform parent, float spacing)
    {
        var row = NewRect(name, parent);
        var hlg = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.spacing = spacing;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        return row;
    }

    private TMP_Text NewText(string name, Transform parent, string text, float size, TextAlignmentOptions align)
    {
        var rt = NewRect(name, parent);
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        t.text = text;
        t.fontSize = size;
        t.color = textColor;
        t.alignment = align;
        t.raycastTarget = false;
        return t;
    }

    private static void SetWidth(TMP_Text t, float width)
    {
        t.gameObject.AddComponent<LayoutElement>().preferredWidth = width;
    }
}
