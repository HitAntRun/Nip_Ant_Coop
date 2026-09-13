using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueRunner : MonoBehaviour
{
    [Header("Dialogue UI")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text speakerText;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private float typeSpeed = 0.04f;

    [Header("Choice UI")]
    [SerializeField] private PanelPopup choicePopup;
    [SerializeField] private GameObject[] choiceSlots;
    [SerializeField] private TMP_Text[] choiceLabels;
    [SerializeField] private GameObject[] choiceChecks;
    public bool choosing;

    [Header("Visuals")]
    [SerializeField] private SpriteDatabase spriteDB;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image portraitImage;
    [SerializeField] private Vector2 actorSize = new Vector2(400, 400);

    [Header("Stage")]
    [SerializeField] private RectTransform characterRoot;
    [SerializeField] private RectTransform slotLeft, slotCenter, slotRight, slotOff;
    
    [Header("Shake")]
    [SerializeField] private RectTransform shakeRoot;
    [SerializeField] private float shakeDuration = 0.3f;
    [SerializeField] private float shakeMagnitude = 15f;

    [Header("Fade")]
    [SerializeField] private Fader fader;
    [SerializeField] private CanvasGroup characterGroup;       
    [SerializeField] private float characterFadeDuration = 0.5f;
    [SerializeField] private float actorFadeDuration = 0.5f;
    private Dictionary<string, Coroutine> fades = new Dictionary<string, Coroutine>();

    [Header("CutScene")]
    [SerializeField] private GameObject narrationRoot;
    [SerializeField] private TMP_Text narrationText;
    [SerializeField] private CanvasGroup narrationGroup;
    [SerializeField] private Image cutsceneImage;
    [SerializeField] private Image cutsceneImageWipe;
    [SerializeField] private float wipeDuration = 0.6f;
    [SerializeField] private float narrationFadeDuration = 0.35f;
    [SerializeField] private float breakFadeDuration = 0.45f;
    [SerializeField] private float narrationHold = 2f; 
    [SerializeField] private GameObject[] hideInNarration;
    
    [Header("Typing Sound")]
    [SerializeField] private int typingSfxEvery = 2;

    static bool IsPunct(char c) => c is '.' or ',' or '!' or '?' or '…' or '·' or '"' or '\'' or '(' or ')' or '\n';

    private string currentMode = "dialogue";
    private Coroutine showCo;
    private bool transitioning;
    private bool isIntro;

    private TMP_Text Target => currentMode == "narration" ? narrationText : dialogueText;
    bool  IsAuto(DialogueNode n) => n != null && (n.mode == "narration" || n.hold >= 0f);
    float HoldOf(DialogueNode n) => n.hold >= 0f ? n.hold : narrationHold;
    
    
    private Vector2 shakeHome;
    private Coroutine shakeCo;

    private DialogueData data;
    private DialogueNode current;
    private Coroutine typing;
    private bool isTyping;
    private string lastSfxNodeId;

    private bool ending;
    public string Summary => data != null ? data.summary : null;

    private int selectedIndex;

    private Dictionary<string, Image> stage = new Dictionary<string, Image>();
    private Dictionary<string, Coroutine> movers = new Dictionary<string, Coroutine>();

    public bool IsActive => current != null;
    
    [System.Serializable]
    public struct LogLine { public string speaker, text, portrait; }

    private readonly List<LogLine> log = new List<LogLine>();
    private string lastLoggedNodeId;
    public IReadOnlyList<LogLine> Log => log;

    void Awake()
    {
        if (shakeRoot != null) shakeHome = shakeRoot.anchoredPosition;
    }
    // -------------------- 재생 --------------------
    public void Play(string storyId, int startIndex = 0)
    {
        string path = Path.Combine(Application.streamingAssetsPath, storyId + ".json");
        data = JsonConvert.DeserializeObject<DialogueData>(File.ReadAllText(path));
        ending = false;
        if (data == null) return;

        if(!string.IsNullOrEmpty(data.chapterLabel)) GameFlow.ChapterLabel = data.chapterLabel;
        if (data.day > 0) GameFlow.Day = data.day;
        ChapterHeader.instance?.Refresh();
        SkipController.instance?.Refresh();
        ApplyBgm(data.bgm);
        ClearStage();
        lastSfxNodeId = null;
        log.Clear();
        lastLoggedNodeId = null;

        currentMode = "dialogue";
        transitioning = false;
        if(narrationRoot != null) narrationRoot.SetActive(false);
        
        if (hideInNarration != null)
            foreach (var go in hideInNarration)
                if (go != null) go.SetActive(true);

        if (!string.IsNullOrEmpty(data.background) && spriteDB != null && backgroundImage != null)
        {
            Sprite bg = spriteDB.Get(data.background);
            if (bg != null) backgroundImage.sprite = bg;
        }

        startIndex = Mathf.Clamp(startIndex, 0, data.nodes.Count - 1);
        var first        = data.nodes[startIndex];
        string startMode = string.IsNullOrEmpty(first.mode) ? "dialogue" : first.mode;
        bool   startNar  = (startMode == "narration");

        currentMode   = startMode;
        transitioning = false;
        if (narrationRoot != null) narrationRoot.SetActive(startNar);

        if (hideInNarration != null)
            foreach (var go in hideInNarration)
                if (go != null) go.SetActive(!startNar);

        string startBg = !string.IsNullOrEmpty(first.bg) ? first.bg : data.background;
        if (!string.IsNullOrEmpty(startBg) && spriteDB != null)
        {
            Sprite s = spriteDB.Get(startBg);
            if (s != null)
            {
                if (startNar && cutsceneImage != null) cutsceneImage.sprite = s;
                else if (backgroundImage != null)      backgroundImage.sprite = s;
            }
        }

        if (startNar && !string.IsNullOrEmpty(data.background)
                     && spriteDB != null && backgroundImage != null)
        {
            Sprite bg = spriteDB.Get(data.background);
            if (bg != null) backgroundImage.sprite = bg;
        }
        StartCoroutine(IntroRoutine(startIndex));
    }

    void Show(DialogueNode node)
    {
        if (showCo != null) StopCoroutine(showCo);
        showCo = StartCoroutine(ShowRoutine(node));
    }

    IEnumerator ShowRoutine(DialogueNode node)
    {
        string m = string.IsNullOrEmpty(node.mode) ? "dialogue" : node.mode;
        bool modeChanged = (m != currentMode);
        bool bgChanged   = !string.IsNullOrEmpty(node.bg);
        bool doWipe = node.wipe && bgChanged && cutsceneImageWipe != null
                      && (string.IsNullOrEmpty(node.mode) ? currentMode : node.mode) == "narration";

        bool doFade = node.fadeBreak && fader != null && (modeChanged || bgChanged)
                      && !isIntro && !doWipe;      // ← && !doWipe 추가
        isIntro = false;
        
        if (doFade)
        {
            transitioning = true;
            yield return fader.FadeOut(breakFadeDuration);
        }

        if (modeChanged)
        {
            bool nar = (m == "narration");
            panel.SetActive(!nar);
            if (characterRoot != null) characterRoot.gameObject.SetActive(!nar);
            if (portraitImage != null && nar) portraitImage.gameObject.SetActive(false);
            if (narrationRoot != null) narrationRoot.SetActive(nar);
            
            if (hideInNarration != null)
                foreach (var go in hideInNarration)
                    if (go != null) go.SetActive(!nar);
            currentMode = m;
        }

        if (bgChanged && spriteDB != null)
        {
            Sprite s = spriteDB.Get(node.bg);
            if (s != null)
            {
                if (currentMode == "narration" && cutsceneImage != null)
                {
                    if (doWipe)
                    {
                        transitioning = true;
                        yield return WipeRoutine(s);
                        transitioning = false;
                    }
                    else cutsceneImage.sprite = s;
                }
                else if (backgroundImage != null)
                    backgroundImage.sprite = s;
            }
        }

        if (!string.IsNullOrEmpty(node.bgm)) ApplyBgm(node.bgm);
        
        Target.text = "";
        Target.maxVisibleCharacters = 0;

        if (currentMode != "narration")
        {
            ApplyPortrait(node);
            ApplyActors(node);
            speakerText.text = node.speaker;
        }

        if (doFade)
        {
            yield return fader.FadeIn(breakFadeDuration);
            transitioning = false;
        }

        if (node.shake) Shake();
        
        if (!string.IsNullOrEmpty(node.sfx) && node.id != lastSfxNodeId)
        {
            lastSfxNodeId = node.id;
            SoundManager.EnsureExists();
            SoundManager.instance?.PlaySfx(node.sfx);
        }

        if (currentMode == "narration" && narrationGroup != null)
        {
            narrationGroup.alpha = 0f;
            StartCoroutine(FadeCanvas(narrationGroup, 0f, 1f, narrationFadeDuration));
        }
        
        if (node.id != lastLoggedNodeId && !string.IsNullOrEmpty(node.text))
        {
            lastLoggedNodeId = node.id;
            log.Add(new LogLine {
                speaker = (currentMode == "narration") ? "" : node.speaker,
                text    = node.text,
                portrait = (currentMode == "narration") ? "" : node.portrait
            });
        }
        if (typing != null) StopCoroutine(typing);
        typing = StartCoroutine(TypeText(node.text));
    }

    void ApplyPortrait(DialogueNode node)
    {
        if (portraitImage != null)
        {
            if (!string.IsNullOrEmpty(node.portrait) && spriteDB != null)
            {
                Sprite p = spriteDB.Get(node.portrait);
                if (p != null)
                {
                    portraitImage.sprite = p;
                    float b = node.portraitBrightness;
                    portraitImage.color = new Color(b, b, b, 1f);
                    portraitImage.gameObject.SetActive(true);
                }
            }
            else portraitImage.gameObject.SetActive(false);
        }
    }

    IEnumerator IntroRoutine(int startIndex)
    {
        bool nar = (currentMode == "narration");
        
        panel.SetActive(false);
        if (characterRoot != null) characterRoot.gameObject.SetActive(false);
        if (characterGroup != null) characterGroup.alpha = 0f;

        if (fader != null)
            yield return fader.FadeIn();
        
        if (!nar)
        {
            if (characterRoot != null) characterRoot.gameObject.SetActive(true);
            panel.SetActive(true);
        }

        for (int k = 0; k < startIndex; k++)
            ApplyActors(data.nodes[k]);

        current = data.nodes[startIndex];
        isIntro = true;
        Show(current);

        if (!nar && characterGroup != null)
            yield return FadeCanvas(characterGroup, 0f, 1f, characterFadeDuration);
    }

    IEnumerator OutroRoutine()
    {
        if (fader != null)
            yield return fader.FadeOut();
        
        if (narrationRoot != null) narrationRoot.SetActive(false);
        panel.SetActive(false);
        if(characterRoot != null)
            characterRoot.gameObject.SetActive(false);

        if(data == null) yield break;

        if (!string.IsNullOrEmpty(data.nextScene))
        {
            if (data.skipLoading) SceneRouter.LoadDirect(data.nextScene, data.nextStage);
            else                  SceneRouter.Load(data.nextScene, data.nextStage);
        }
        else if (!string.IsNullOrEmpty(data.nextStage))
            GameFlow.CurrentStage = data.nextStage;
    }
    IEnumerator FadeCanvas(CanvasGroup cg, float from, float to, float dur)
    {
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            cg.alpha = Mathf.Lerp(from, to, t / dur);
            yield return null;
        }
        cg.alpha = to;
    }

    IEnumerator TypeText(string full)
    {
        isTyping = true;
        bool typingSfxOn = (currentMode != "narration");

        var t = Target;
        t.maxVisibleCharacters = 0;
        t.text = full;
        t.ForceMeshUpdate();
        int total = t.textInfo.characterCount;
        for (int i = 0; i <= total; i++)
        {
            t.maxVisibleCharacters = i;

            if (typingSfxOn && i > 0 && i % typingSfxEvery == 0)
            {
                char c = t.textInfo.characterInfo[i - 1].character;
                if (!char.IsWhiteSpace(c) && !IsPunct(c))
                    SoundManager.instance?.PlayTyping();
            }

            yield return new WaitForSeconds(typeSpeed);
        }
        isTyping = false;
        SoundManager.instance?.StopTyping(); 
        isTyping = false;

        if (current.choices != null && current.choices.Count > 0) { ShowChoices(); yield break; }

        if (IsAuto(current))
        {
            yield return new WaitForSeconds(HoldOf(current));
            typing = null;
            Next();
        }
    }

    // ------------------------------------------------------------- 입력/진행

    public void OnClick()
    {
        if (current == null || transitioning) return;
        if (IsAuto(current)) return;  

        if (isTyping)
        {
            if (typing != null) StopCoroutine(typing);
            var t = Target;
            t.maxVisibleCharacters = t.textInfo.characterCount;
            isTyping = false;
            SoundManager.instance?.StopTyping();
            if (current.choices != null && current.choices.Count > 0) ShowChoices();
            return;
        }

        if (current.choices != null && current.choices.Count > 0) return;
        Next();
    }

    void Next()
    {
        if (current.next == null) { EndDialogue(); return; }
        current = data.nodes.FirstOrDefault(n => n.id == current.next);
        if (current == null) { EndDialogue(); return; }
        Show(current);
    }

    void Choose(string nextId)
    {
        if (string.IsNullOrEmpty(nextId)) { EndDialogue(); return; }
        current = data.nodes.FirstOrDefault(n => n.id == nextId);
        if (current == null) { EndDialogue(); return; }
        Show(current);
    }

    public void Skip()
    {
        transitioning = false;
        if (showCo != null) { StopCoroutine(showCo); showCo = null; }
        if (fader != null) fader.ResetOverlay();
        
        if (ending || data == null || current == null) return;
        if (typing != null)
        {
            StopCoroutine(typing);
            typing = null;
        }
        isTyping = false;
        SoundManager.instance?.StopTyping();

        if (choosing) return;

        if (current.choices != null && current.choices.Count > 0)
        {
            Show(current);
            return;
        }

        string lastBg = null;
        string lastMode = null;
        string lastBgm = null;
        
        var guard = new HashSet<string>();

        DialogueNode node = current;
        while (true)
        {
            if (node.id != null && !guard.Add(node.id)) break;
            
            ApplyActors(node);
            if (!string.IsNullOrEmpty(node.bg)) lastBg = node.bg;
            if (!string.IsNullOrEmpty(node.mode)) lastMode = node.mode;
            if (!string.IsNullOrEmpty(node.bgm)) lastBgm = node.bgm;

            if (node.endHere || string.IsNullOrEmpty(node.next))
            {
                node = null;
                break;
            }

            var nextNode = data.nodes.FirstOrDefault(n => n.id == node.next);
            if (nextNode == null)
            {
                node = null;
                break;
            }

            if (nextNode.choices != null && nextNode.choices.Count > 0)
            {
                node = nextNode;
                break;
            }
            node = nextNode;
        }

        if (node == null || node == current)
        {
            EndDialogue();
            return;
        }

        ApplyStateBeforeJump(lastMode, lastBg, node);
        if(!string.IsNullOrEmpty(lastBgm) && string.IsNullOrEmpty(node.bgm))
            ApplyBgm(lastBgm);
        current = node;
        Show(current);
    }

    void EndDialogue()
    {
        if (ending) return;
        ending = true;
        current = null;
        StartCoroutine(OutroRoutine());
    }

    // ------------------------------------------------------------- 선택지
    void ShowChoices()
    {
        choosing = true;
        selectedIndex = 0;

        for (int i = 0; i < choiceSlots.Length; i++)
        {
            bool used = i < current.choices.Count;
            choiceSlots[i].SetActive(used);
            if (used) choiceLabels[i].text = current.choices[i].text;
        }
        UpdateChecks();
        if (choicePopup != null) choicePopup.Open();
    }

    void UpdateChecks()
    {
        for (int i = 0; i < choiceChecks.Length; i++)
            choiceChecks[i].SetActive(i == selectedIndex);
    }

    public void Move(int dir)
    {
        int count = current.choices.Count;
        selectedIndex = (selectedIndex + dir + count) % count;
        UpdateChecks();
    }

    public void Confirm()
    {
        choosing = false;
        if (choicePopup != null) choicePopup.Close();
        Choose(current.choices[selectedIndex].next);
    }

    // ------------------------------------------------------------- 무대(배우)
    Transform GetSlot(string s) => s switch
    {
        "Left"  => slotLeft,
        "Right" => slotRight,
        "Off"   => slotOff,
        _       => slotCenter,
    };

    void ApplyActors(DialogueNode node)
    {
        if (node.actors == null) return;

        foreach (var a in node.actors)
        {
            if (a.slot == "Off") { RemoveActor(a.id); continue; }

            Image img = GetOrCreateActor(a.id, out bool isNew);

            if (!string.IsNullOrEmpty(a.sprite) && spriteDB != null)
            {
                Sprite s = spriteDB.Get(a.sprite);
                if (s != null)
                {
                    img.sprite = s;
                    img.rectTransform.sizeDelta = actorSize;  
                }
                else Debug.LogWarning($"스프라이트 '{a.sprite}' 를 DB에서 못 찾음");
            }

            Transform target = GetSlot(a.slot);
            if (target != null) img.rectTransform.position = target.position;
            
            img.rectTransform.localScale = new Vector3(a.flip ? -1f : 1f, 1f, 1f);
            
            if (a.fadeIn)
                StartActorFade(a.id, img, a.brightness);                        // 서서히 등장
            else
                img.color = new Color(a.brightness, a.brightness, a.brightness, 1f);
        }
    }

    Image GetOrCreateActor(string id, out bool isNew)
    {
        if (stage.TryGetValue(id, out var img) && img != null) { isNew = false; return img; }

        var go = new GameObject("Actor_" + id, typeof(Image));
        go.transform.SetParent(characterRoot, false);
        img = go.GetComponent<Image>();
        stage[id] = img;
        isNew = true;
        return img;
    }
    
    void StartActorFade(string id, Image img, float brightness)
    {
        if (fades.TryGetValue(id, out var c) && c != null) StopCoroutine(c);
        fades[id] = StartCoroutine(ActorFadeRoutine(img, brightness));
    }
    
    IEnumerator ActorFadeRoutine(Image img, float brightness)
    {
        float t = 0f;
        while (t < actorFadeDuration)
        {
            if (img == null) yield break;
            t += Time.deltaTime;
            float alpha = Mathf.Clamp01(t / actorFadeDuration);
            img.color = new Color(brightness, brightness, brightness, alpha);
            yield return null;
        }
        if (img == null) yield break;
        img.color = new Color(brightness, brightness, brightness, 1f);
    }
    void RemoveActor(string id)
    {
        if (fades.TryGetValue(id, out var c) && c != null) StopCoroutine(c);
        fades.Remove(id);

        if (stage.TryGetValue(id, out var img) && img != null) Destroy(img.gameObject);
        stage.Remove(id);
        movers.Remove(id);
    }

    void ClearStage()
    {
        foreach (var kv in fades) if (kv.Value != null) StopCoroutine(kv.Value);
        fades.Clear();

        foreach (var kv in stage) if (kv.Value != null) Destroy(kv.Value.gameObject);
        stage.Clear();
        movers.Clear();
    }
    
    void Shake()
    {
        if (shakeRoot == null) return;
        if (shakeCo != null) StopCoroutine(shakeCo);
        shakeCo = StartCoroutine(ShakeRoutine());
    }

    IEnumerator ShakeRoutine()
    {
        float t = 0f;
        while (t < shakeDuration)
        {
            t += Time.deltaTime;
            float damper = 1f - (t / shakeDuration); 
            Vector2 off = Random.insideUnitCircle * shakeMagnitude * damper;
            shakeRoot.anchoredPosition = shakeHome + off;
            yield return null;
        }
        shakeRoot.anchoredPosition = shakeHome; 
    }
    
    IEnumerator WipeRoutine(Sprite next)
    {
        cutsceneImageWipe.sprite = next;
        cutsceneImageWipe.fillAmount = 0f;
        cutsceneImageWipe.gameObject.SetActive(true);

        float t = 0f;
        while (t < wipeDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / wipeDuration);
            k = 1f - (1f - k) * (1f - k);
            cutsceneImageWipe.fillAmount = k;
            yield return null;
        }

        cutsceneImage.sprite = next; 
        cutsceneImageWipe.fillAmount = 0f;
    }

    void ApplyStateBeforeJump(string lastMode, string lastBg, DialogueNode target)
    {
        if (!string.IsNullOrEmpty(lastMode)) currentMode = lastMode;
        if (!string.IsNullOrEmpty(lastBg) && string.IsNullOrEmpty(target.bg) && spriteDB != null)
        {
            Sprite s = spriteDB.Get(lastBg);
            if (s != null)
            {
                if (currentMode == "narration" && cutsceneImage != null) cutsceneImage.sprite = s;
                else if (backgroundImage != null) backgroundImage.sprite = s;
            }
        }
    }

    void ApplyBgm(string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        SoundManager.EnsureExists();
        SoundManager.instance?.PlayBgmById(id);
    }
}