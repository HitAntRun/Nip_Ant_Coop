using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AntCounter : MonoBehaviour
{
    public static AntCounter instance;

    [SerializeField] private RectTransform iconRect;
    [SerializeField] private Image iconImg;
    [SerializeField] private RectTransform crossRect;
    [SerializeField] private Image crossImg;

    [Header("Count")]
    [SerializeField] private RectTransform countCard;
    [SerializeField] private TMP_Text countText;
    [SerializeField] private string countFormat = "{0}";

    private float stampStartScale = 2.2f;
    private float stampStartAngle = -25f;
    private float stampDuration = 0.25f;
    private float iconShakeStrength = 8f;
    private Color foundIconTint = new Color(0.6f, 0.6f, 0.6f, 1f);

    private float holdDuration = 0.4f;
    private float resetDuration = 0.2f;
    private bool keepCrossWhenCleared = true;

    private float flipHalfDuration = 0.12f;
    private float flipAngle = 90f;

    private int total;
    private int found;
    private int wrong;
    
    private Vector2 iconBasePos;
    private Color iconBaseColor;
    private Sequence stampSeq;
    private Sequence flipSeq;

    public int Found => found;
    public int Wrong => wrong;
    public int Total => total;
    public int Remaining => Mathf.Max(0, total - found);

    void Awake()
    {
        instance = this;
        iconBasePos = iconRect.anchoredPosition;
        iconBaseColor = iconImg.color;
    }

    void OnDestroy() { if (instance == this) instance = null; }

    public void SetTotal(int t)
    {
        total = t;
        found = 0;
        wrong = 0;
        ResetIcon();
        SetCountImmediate(Remaining);
    }

    public void AddFound()
    {
        found++;
        PlayStamp();
        PlayFlip(Remaining);

        string stage = GameFlow.CurrentStage;
        int others = GameFlow.TotalTermitesFound - GameFlow.TermitesFoundIn(stage);
        Debug.Log($"[Termite] {stage}: {found}/{total} | 누적 {others + found}");

        if (found >= total) OnAllFound();
    }

    public void AddWrong() => wrong++;

    void OnAllFound()
    {
        if (GameFlow.CurrentStage == "Tutorial") return;
        if (StageFlow.instance != null) StageFlow.instance.Finish();
    }
    
    void ResetIcon()
    {
        stampSeq?.Kill();
        iconRect.DOKill();
        iconImg.DOKill();

        crossRect.gameObject.SetActive(false);
        crossRect.localScale = Vector3.one;
        crossRect.localEulerAngles = Vector3.zero;
        iconRect.anchoredPosition = iconBasePos;
        iconImg.color = iconBaseColor;
    }

    void PlayStamp()
    {
        ResetIcon();

        crossRect.gameObject.SetActive(true);
        crossRect.localScale = Vector3.one * stampStartScale;
        crossRect.localEulerAngles = new Vector3(0, 0, stampStartAngle);
        crossImg.color = new Color(1, 1, 1, 0);

        stampSeq = DOTween.Sequence().SetLink(gameObject);
        stampSeq.Append(crossRect.DOScale(1f, stampDuration).SetEase(Ease.InBack));
        stampSeq.Join(crossRect.DORotate(Vector3.zero, stampDuration).SetEase(Ease.OutQuad));
        stampSeq.Join(crossImg.DOFade(1f, stampDuration * 0.5f));
        stampSeq.AppendCallback(() =>
        {
            iconRect.DOShakeAnchorPos(0.2f, iconShakeStrength, 20).SetLink(iconRect.gameObject);
            iconImg.DOColor(foundIconTint, 0.15f).SetLink(iconImg.gameObject);
        });
        stampSeq.Append(crossRect.DOPunchScale(Vector3.one * 0.15f, 0.2f, 6, 0.5f));

        if (found >= total && keepCrossWhenCleared) return;

        stampSeq.AppendInterval(holdDuration);
        stampSeq.Append(crossImg.DOFade(0f, resetDuration));
        stampSeq.Join(crossRect.DOScale(0.6f, resetDuration).SetEase(Ease.InBack));
        stampSeq.Join(iconImg.DOColor(iconBaseColor, resetDuration));
        stampSeq.OnComplete(() =>
        {
            crossRect.gameObject.SetActive(false);
            crossRect.localScale = Vector3.one;
            iconRect.anchoredPosition = iconBasePos;
        });
    }
    
    void SetCountImmediate(int value)
    {
        flipSeq?.Kill();
        countCard.localEulerAngles = Vector3.zero;
        countText.text = string.Format(countFormat, value);
    }

    void PlayFlip(int value)
    {
        flipSeq?.Kill();
        countCard.localEulerAngles = Vector3.zero;

        flipSeq = DOTween.Sequence().SetLink(countCard.gameObject);
        flipSeq.Append(countCard.DOLocalRotate(new Vector3(flipAngle, 0, 0), flipHalfDuration)
                                .SetEase(Ease.InQuad));
        flipSeq.AppendCallback(() =>
        {
            countText.text = string.Format(countFormat, value);
            countCard.localEulerAngles = new Vector3(-flipAngle, 0, 0);
        });
        flipSeq.Append(countCard.DOLocalRotate(Vector3.zero, flipHalfDuration * 1.5f)
                                .SetEase(Ease.OutBack));
    }
}