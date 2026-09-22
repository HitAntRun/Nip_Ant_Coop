using System;
using System.Collections;
using TMPro;
using UnityEngine.UI;
using UnityEngine;

public class InteractionPopup : MonoBehaviour
{
    public static InteractionPopup instance;

    [Header("UI")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private GameObject characterRoot;
    [SerializeField] private Image characterImage;
    [SerializeField] private TMP_Text label;

    [Header("Data")]
    [SerializeField] private InteractionLineData data;
    [SerializeField] private float showDuration = 1.5f;
    [SerializeField] private float bubbleDuration = 2f;  

    [Header("Options")]
    [SerializeField] private bool hideDuringTutorial = true;

    private ShuffleBag<string> correctBag;
    private ShuffleBag<string> wrongBag;
    private ShuffleBag<string> wrongBubbleBag;
    private ShuffleBag<string> correctBubbleBag;
    private Coroutine hideCo;

    void Awake()
    {
        instance = this;
        if (data != null)
        {
            correctBag = new ShuffleBag<string>(data.correctLines);
            correctBubbleBag = new ShuffleBag<string>(data.correctBubbleLines);
            wrongBag = new ShuffleBag<string>(data.wrongLines);
            wrongBubbleBag = new ShuffleBag<string>(data.wrongBubbleLines);
        }
        SetVisible(false);
    }

    private void OnDestroy()
    {

        if (instance == this) instance = null;
    }

    public void ShowTermiteFound(AntMonologue bubble = null)
    {
        if (bubble != null && correctBubbleBag != null)
            bubble.React(correctBubbleBag.Next(), 99f);
        
        if (IsTutorial) return;

        Show(correctBag != null ? correctBag.Next() : null,
            data != null ? data.correctFace : null);
    }

    public void ShowWrongTarget(AntMonologue bubble = null)
    {
        if (bubble != null && wrongBubbleBag != null)
            bubble.React(wrongBubbleBag.Next(), bubbleDuration);
        
        if (IsTutorial) return;

        Show(wrongBag != null ? wrongBag.Next() : null,
            data != null ? data.wrongFace : null);
    }
    
    public void Show(string line, Sprite face = null)
    {
        if (IsTutorial) return;
        if (string.IsNullOrEmpty(line)) return;
        
        if(label != null) label.text = Loc.Get(Loc.UI, line, "");
        if(characterImage != null && face != null) characterImage.sprite = face;

        SetVisible(true);
        
        if(hideCo != null) StopCoroutine(hideCo);
        hideCo = StartCoroutine(HideAfter(showDuration));
    }

    private IEnumerator HideAfter(float t)
    {
        yield return new WaitForSeconds(t);
        SetVisible(false);
        hideCo = null;
    }

    private void SetVisible(bool v)
    {
        if(dialoguePanel != null) dialoguePanel.SetActive(v);
        if(characterRoot != null) characterRoot.SetActive(v);
    }
    
    private bool IsTutorial =>
        hideDuringTutorial &&
        (GameFlow.CurrentStage == "Tutorial" ||
         (TutorialController.instance != null && TutorialController.instance.enabled));
}
