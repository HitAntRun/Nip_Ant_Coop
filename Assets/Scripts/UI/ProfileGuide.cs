using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

// Stage1 이후 첫 스토리에서, 대사가 나오기 전에 Tab(프로필) 키를 안내한다.
// 흐름: 화면 어둡게 + 안내 문구 → 플레이어가 Tab(또는 클릭)으로 패널을 엶 → 패널을 닫으면 대사 시작.
// 씬에 직접 배치해 디자인을 잡아도 되고, 없으면 GetOrCreate()가 기본 UI로 자동 생성한다.
public class ProfileGuide : MonoBehaviour
{
    public static ProfileGuide instance;

    [Header("Refs (비워두면 기본 UI 자동 생성)")]
    [SerializeField] private CanvasGroup root;
    [SerializeField] private TMP_Text message;

    [Header("Text")]
    [SerializeField] private string messageKey = "guide_profile";
    [SerializeField] private string messageFallback = "<b>[Tab]</b> 키를 눌러 지금까지의 기록을 확인해보세요";

    [Header("Look")]
    [SerializeField, Range(0f, 1f)] private float dimAlpha = 0.6f;
    [SerializeField] private float defaultFontSize = 36f;
    [SerializeField] private float fadeDuration = 0.3f;
    [SerializeField] private float pulseScale = 1.06f;
    [SerializeField] private float pulseDuration = 0.6f;

    public bool IsShowing { get; private set; }

    // 해금됐고(Stage1 클리어) 아직 안내를 못 봤을 때만
    public static bool ShouldShow =>
        ProfilePanel.instance != null
        && ProfilePanel.instance.IsUnlocked
        && SaveManager.Current != null
        && !SaveManager.Current.profileGuideSeen;

    public static ProfileGuide GetOrCreate()
    {
        if (instance != null) return instance;

        Canvas canvas = ProfilePanel.instance.GetComponentInParent<Canvas>(true).rootCanvas;
        var go = new GameObject("ProfileGuide", typeof(RectTransform));
        go.transform.SetParent(canvas.transform, false);
        go.transform.SetAsLastSibling();
        return go.AddComponent<ProfileGuide>();   // Awake에서 instance 설정
    }

    void Awake()
    {
        instance = this;
        if (root == null) BuildDefault();
        root.alpha = 0f;
        root.blocksRaycasts = false;
    }

    void OnDestroy() { if (instance == this) instance = null; }

    void OnEnable()  => LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
    void OnDisable() => LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
    void OnLocaleChanged(Locale _) => RefreshText();

    void RefreshText()
    {
        if (message != null) message.text = Loc.Get(Loc.UI, messageKey, messageFallback);
    }

    public IEnumerator Run()
    {
        ProfilePanel panel = ProfilePanel.instance;
        if (panel == null) yield break;

        IsShowing = true;
        RefreshText();

        root.blocksRaycasts = true;   // 안내 중 스킵·백로그 버튼 클릭 막기
        yield return root.DOFade(1f, fadeDuration).SetUpdate(true).SetLink(gameObject).WaitForCompletion();

        Tween pulse = message != null
            ? message.transform.DOScale(pulseScale, pulseDuration)
                .SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine)
                .SetUpdate(true).SetLink(gameObject)
            : null;

        // 플레이어가 패널을 열 때까지 대기 (Tab은 ProfilePanel이 직접 받음, 마우스 클릭도 허용)
        while (panel != null && !panel.IsOpen)
        {
            bool paused = PauseMenu.instance != null && PauseMenu.instance.IsOpen;
            root.alpha = paused ? 0f : 1f;          // 일시정지 메뉴를 가리지 않게
            root.blocksRaycasts = !paused;

            if (!paused && Input.GetMouseButtonDown(0)) panel.Open();
            yield return null;
        }

        pulse?.Kill();
        if (message != null) message.transform.localScale = Vector3.one;
        root.blocksRaycasts = false;
        root.DOFade(0f, fadeDuration).SetUpdate(true).SetLink(gameObject);

        MarkSeen();   // 한 번 열어본 순간 본 것으로 처리

        // 패널을 닫으면 대사 시작
        while (panel != null && panel.IsOpen) yield return null;
        IsShowing = false;
    }

    static void MarkSeen()
    {
        if (SaveManager.Current == null) return;
        SaveManager.Current.profileGuideSeen = true;
        SaveManager.Save(force: true);
    }

    // ───────── 기본 UI (씬에 직접 배치하지 않았을 때) ─────────

    void BuildDefault()
    {
        Stretch((RectTransform)transform);
        root = gameObject.AddComponent<CanvasGroup>();

        var dimGo = new GameObject("Dim", typeof(RectTransform), typeof(Image));
        dimGo.transform.SetParent(transform, false);
        Stretch((RectTransform)dimGo.transform);
        var dim = dimGo.GetComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, dimAlpha);
        dim.raycastTarget = true;

        var textGo = new GameObject("Message", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(transform, false);
        var rt = (RectTransform)textGo.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(1400f, 200f);

        var tmp = textGo.GetComponent<TextMeshProUGUI>();
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = defaultFontSize;
        tmp.color = Color.white;
        tmp.raycastTarget = false;

        // 기본 TMP 폰트엔 한글이 없을 수 있으니 프로필 패널의 폰트를 빌려 씀
        TMP_Text sample = ProfilePanel.instance != null
            ? ProfilePanel.instance.GetComponentInChildren<TMP_Text>(true) : null;
        if (sample != null)
        {
            tmp.font = sample.font;
            tmp.fontSharedMaterial = sample.fontSharedMaterial;
        }
        message = tmp;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}
