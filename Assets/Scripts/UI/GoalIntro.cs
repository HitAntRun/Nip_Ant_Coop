using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class GoalIntro : MonoBehaviour
{
    public static GoalIntro instance;

    [Header("Refs")]
    [SerializeField] private RectTransform sign;
    [SerializeField] private TMP_Text goalText;
    [SerializeField] private Fader fader;
    [SerializeField] private GameTimer timer;

    [Header("Text")]
    [SerializeField] private string goalKey = "goal_find_termite";
    [SerializeField] private string goalFormat = "숨겨진 흰개미 {0}마리를 찾아라!";

    [Header("Spotlight")]
    [SerializeField, Range(0f, 1f)] private float spotlightAlpha = 0.7f;

    [Header("Drop")]
    [SerializeField] private float dropHeight = 700f;
    [SerializeField] private float dropDuration = 0.6f;
    [SerializeField] private float swingAngle = 4f;
    [SerializeField] private float holdTime = 2f;
    [SerializeField] private float riseDuration = 0.4f;

    [SerializeField] private string[] skipStages = { "Tutorial" };

    public bool IsShowing { get; private set; }
    private Vector2 shownPos;
    private bool skipRequested;

    void Awake()
    {
        instance = this;
        shownPos = sign.anchoredPosition;
        sign.anchoredPosition = shownPos + Vector2.up * dropHeight;
        sign.gameObject.SetActive(false);
        IsShowing = System.Array.IndexOf(skipStages, GameFlow.CurrentStage) < 0;
        if (fader != null) fader.SetAlpha(IsShowing ? spotlightAlpha : 1f);
    }
    void OnDestroy() { if (instance == this) instance = null; }

    IEnumerator Start()
    {
        yield return null;
        int count = AntCounter.instance != null ? AntCounter.instance.Total : 0;

        if (!IsShowing || count <= 0)
        {
            IsShowing = false;
            if (fader != null) yield return fader.FadeTo(0f, 1f, true);
            if (timer != null) timer.Begin();
            yield break;
        }

        goalText.text = string.Format(Loc.Get(Loc.UI, goalKey, goalFormat), count);

        sign.gameObject.SetActive(true);
        sign.localEulerAngles = Vector3.zero;
        Sequence drop = DOTween.Sequence().SetLink(sign.gameObject)
            .Append(sign.DOAnchorPos(shownPos, dropDuration).SetEase(Ease.OutBack, 1.3f))
            .Append(sign.DOPunchRotation(new Vector3(0, 0, swingAngle), 0.8f, 6, 0.5f));
        yield return drop.WaitForCompletion();

        skipRequested = false;
        float t = 0f;
        while (t < holdTime && !skipRequested) { t += Time.deltaTime; yield return null; }

        yield return sign.DOAnchorPos(shownPos + Vector2.up * dropHeight, riseDuration)
                         .SetEase(Ease.InBack).SetLink(sign.gameObject).WaitForCompletion();
        sign.gameObject.SetActive(false);
        if (fader != null) fader.ResetOverlay();
        
        IsShowing = false;
        if (timer != null) timer.Begin();
    }

    public void Skip() => skipRequested = true;
}