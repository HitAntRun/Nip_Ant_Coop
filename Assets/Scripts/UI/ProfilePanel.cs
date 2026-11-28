using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

[RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
public class ProfilePanel : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private TMP_Text foundText;
    [SerializeField] private TMP_Text wrongText;
    [SerializeField] private TMP_Text accuracyText;

    [Header("Title")]
    [SerializeField] private TitleTable titleTable;      // 칭호 단계·판정 (엔딩 크레딧과 공유)

    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text nextTitleText;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private Image progressFill;
    [SerializeField] private float fillDuration = 0.5f;
    [SerializeField] private float fillDelay = 0.2f;

#if UNITY_EDITOR
    [Header("Debug (에디터 전용)")]
    [SerializeField] private bool useDebugValues;
    [SerializeField] private int debugFound;
    [SerializeField] private int debugWrong;
#endif

    [Header("Unlock")]
    [Tooltip("이 스테이지를 끝낸 뒤부터 Tab으로 열 수 있음")]
    [SerializeField] private string unlockAfterStage = "Stage1_1";

    [Header("Slide")]
    [SerializeField] private float duration = 0.35f;
    [SerializeField] private Ease openEase = Ease.OutBack;
    [SerializeField] private Ease closeEase = Ease.InBack;
    [SerializeField] private float overshoot = 1.7f;

    public static ProfilePanel instance;
    public bool IsOpen => isOpen;
    public bool IsUnlocked => GameFlow.HasStageRecord(unlockAfterStage);
    private RectTransform rect;
    private CanvasGroup group;
    private float shownX;
    private float hiddenX;
    private bool isOpen;
    private Tween tween;
    private Tween fillTween;

    private void Awake()
    {
        instance = this;
        rect = GetComponent<RectTransform>();
        group = GetComponent<CanvasGroup>();

        shownX = rect.anchoredPosition.x;
        RectTransform parent = (RectTransform)rect.parent;
        float anchorFromLeft = parent.rect.width * rect.anchorMin.x;
        float rightOfPivot   = rect.rect.width * (1f - rect.pivot.x);
        hiddenX = -anchorFromLeft - rightOfPivot;

        rect.anchoredPosition = new Vector2(hiddenX, rect.anchoredPosition.y);
        SetInteractable(false);
    }

    private void Update()
    {
        bool paused = PauseMenu.instance != null && PauseMenu.instance.IsOpen;

        if (!paused && (isOpen || IsUnlocked) && Input.GetKeyDown(KeyCode.Tab))
            Toggle();

        group.blocksRaycasts = isOpen && !paused;
    }

    private void Refresh()
    {
        int found = GameFlow.TotalTermitesFound;
        int wrong = GameFlow.TotalWrongReports;

#if UNITY_EDITOR
        if (useDebugValues) { found = debugFound; wrong = debugWrong; }
#endif
        int tries = found + wrong;
        float acc = tries == 0 ? 0f : (float)found / tries;

        if (foundText)    foundText.text    = found.ToString();
        if (wrongText)    wrongText.text    = wrong.ToString();
        if (accuracyText) accuracyText.text = tries == 0 ? "-" : $"{acc * 100f:0.#}%";

        RefreshTitle(found, acc);
    }

    // ───────── 칭호 ─────────

    private string Name(TitleTier t) => titleTable.Name(t);

    private void RefreshTitle(int found, float acc)
    {
        if (titleTable == null || titleTable.tiers == null || titleTable.tiers.Length < 2) return;

        TitleTier[] tiers = titleTable.tiers;
        int last = tiers.Length - 1;
        int countIdx = titleTable.CountIndex(found);
        TitleTier current = titleTable.CurrentTitle(found, acc);

        if (titleText) titleText.text = Name(current);
        if (titleText) titleText.color = current.color;

        // 최고 칭호
        if (current == tiers[last])
        {
            if (nextTitleText) nextTitleText.text = Loc.Get(Loc.UI, "title_max", "최고 칭호 달성!");
            if (progressText)  progressText.text  = "";
            AnimateFill(1f);
            return;
        }

        // 현재 구간: tiers[baseIdx] → tiers[baseIdx + 1]
        int baseIdx = Mathf.Min(countIdx, last - 1);
        TitleTier next = tiers[baseIdx + 1];
        int from = tiers[baseIdx].minFound;
        int to   = next.minFound;
        float ratio = Mathf.Clamp01((float)(found - from) / (to - from));

        if (nextTitleText)
            nextTitleText.text = string.Format(
                Loc.Get(Loc.UI, "title_next", "{0}"), Name(next));

        if (progressText)
        {
            if (found >= to && acc < next.minAccuracy)
                progressText.text = string.Format(
                    Loc.Get(Loc.UI, "title_need_acc", "정확도 {0}% 필요"),
                    Mathf.RoundToInt(next.minAccuracy * 100f));
            else
                progressText.text = $"{found} / {to}";
        }

        AnimateFill(ratio);
    }

    private void AnimateFill(float target)
    {
        if (!progressFill) return;
        fillTween?.Kill();
        progressFill.fillAmount = 0f;
        fillTween = progressFill.DOFillAmount(target, fillDuration)
            .SetDelay(fillDelay)
            .SetEase(Ease.OutCubic)
            .SetUpdate(true);
    }

    // ───────── 열기/닫기 ─────────

    public void Toggle()
    {
        if (isOpen) Close();
        else Open();
    }

    public void Open()
    {
        Refresh();
        isOpen = true;
        tween?.Kill();
        SetInteractable(true);
        tween = rect.DOAnchorPosX(shownX, duration)
            .SetEase(openEase, overshoot)
            .SetUpdate(true);
    }

    public void Close()
    {
        isOpen = false;
        tween?.Kill();
        SetInteractable(false);
        tween = rect.DOAnchorPosX(hiddenX, duration)
            .SetEase(closeEase, overshoot)
            .SetUpdate(true);
    }

    private void SetInteractable(bool on)
    {
        group.interactable = on;
        group.blocksRaycasts = on;
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
        tween?.Kill();
        fillTween?.Kill();
    }
}