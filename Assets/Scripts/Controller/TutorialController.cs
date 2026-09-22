using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TutorialController : MonoBehaviour
{
    public static TutorialController instance;
    
    [SerializeField] private string nextSceneName = SceneRouter.IngameScene;
    [SerializeField] private string nextStageId   = "Stage1_1";
    [SerializeField] private Fader fader;
    [SerializeField] private bool skipLoading = true;
    
    public enum Trigger { Continue, ReportAnt, Rotate, Magnify }

    [System.Serializable]
    public class Step
    {
        [TextArea] public string text;
        public string locKey;
        public Trigger trigger;
        public Sprite image;
    }

    [SerializeField] private GameObject tutorialPanel;
    [SerializeField] private TMP_Text tutorialText;
    [SerializeField] private Image stepImage;
    [SerializeField] private List<Step> steps = new();

    private int index = -1;
    private bool waitingForAction = false;
    private bool showingHint = false;

    void Awake() { instance = this; }
    void OnDestroy() { if (instance == this) instance = null; }

    public bool IsBlocking =>
        enabled && index >= 0 && index < steps.Count && (!waitingForAction || showingHint);

    public Trigger CurrentTrigger =>
        (index >= 0 && index < steps.Count) ? steps[index].trigger : Trigger.Continue;

    void Start()
    {
        if (GameFlow.CurrentStage != "Tutorial")
        {
            if (tutorialPanel != null) tutorialPanel.SetActive(false);
            if (stepImage != null) stepImage.gameObject.SetActive(false);
            enabled = false;
            return;
        }
        Next();
    }

    void Next()
    {
        index++;
        waitingForAction = false;
        showingHint = false;
        if (index >= steps.Count) { EndTutorial(); return; }

        if (tutorialPanel != null) tutorialPanel.SetActive(true);
        if (tutorialText != null)  tutorialText.text = Loc.Get(Loc.UI, steps[index].locKey, steps[index].text);
        ShowStepImage(steps[index].image);
    }

    void ShowStepImage(Sprite s)
    {
        if (stepImage == null) return;
        if (s != null)
        {
            stepImage.sprite = s;
            stepImage.rectTransform.sizeDelta = new Vector2(400, 400);
            stepImage.gameObject.SetActive(true);
        }
        else stepImage.gameObject.SetActive(false);
    }

    public void Advance()
    {
        if (index < 0 || index >= steps.Count) return;

        if (showingHint)
        {
            showingHint = false;
            if (tutorialPanel != null) tutorialPanel.SetActive(false);
            if (stepImage != null) stepImage.gameObject.SetActive(false);
            return;
        }

        if (waitingForAction) return;

        if (steps[index].trigger == Trigger.Continue)
        {
            Next();
        }
        else
        {
            if (tutorialPanel != null) tutorialPanel.SetActive(false);
            if (stepImage != null) stepImage.gameObject.SetActive(false);
            waitingForAction = true;
        }
    }

    public void ShowHint(string message)
    {
        if (index < 0 || index >= steps.Count) return;
        showingHint = true;
        if (tutorialPanel != null) tutorialPanel.SetActive(true);
        if (tutorialText != null) tutorialText.text = message;
        ShowStepImage(steps[index].image);
    }

    public void NotifyReportAnt() => Check(Trigger.ReportAnt);
    public void NotifyRotate()    => Check(Trigger.Rotate);
    public void NotifyMagnify()   => Check(Trigger.Magnify);

    void Check(Trigger t)
    {
        if (index < 0 || index >= steps.Count) return;
        if (!waitingForAction || showingHint) return;
        if (steps[index].trigger == t) Next();
    }

    public bool Allows(Trigger t)
    {
        if (!enabled) return true;
        if (index < 0 || index >= steps.Count) return true;
        if (showingHint) return false;
        if (!waitingForAction) return false;
        return steps[index].trigger == t;
    }

    void EndTutorial()
    {
        if (tutorialPanel != null) tutorialPanel.SetActive(false);
        if (stepImage != null) stepImage.gameObject.SetActive(false);

        var mag = FindFirstObjectByType<MagnifierController>();
        if (mag != null && mag.IsSearchMode) mag.ToggleSearchMode();

        StartCoroutine(EndRoutine());
    }

    IEnumerator EndRoutine()
    {
        if (fader != null)
            yield return fader.FadeOut();

        if (skipLoading) SceneRouter.LoadDirect(nextSceneName, nextStageId);
        else             SceneRouter.Load(nextSceneName, nextStageId);
    }
}