using System.Collections;
using System.Collections.Generic;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueRunner : MonoBehaviour
{
    const string SpeakerPrefix = "char_";
    static Dictionary<string, string> speakerKeys;
    
    [Header("Dialogue UI")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text speakerText;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private float typeSpeed = 0.04f;

    [Header("Choice")]
    [SerializeField] private PanelPopup choicePopup;
    [SerializeField] private GameObject[] choiceSlots;
    [SerializeField] private TMP_Text[] choiceLabels;
    [SerializeField] private GameObject[] choiceChecks;
    [SerializeField] private float choiceInputDelay = 0.4f;
    public bool choosing;
    private string chosenNextStage;
    private string chosenNextScene;
    private float choiceReadyTime;
    public bool CanConfirm => choosing && Time.unscaledTime >= choiceReadyTime;
    
    [Header("Item Spotlight")]
    [SerializeField] private RectTransform itemRoot;
    [SerializeField] private RectTransform itemGlow;
    [SerializeField] private Image itemFlash;

    [SerializeField] private float popDuration = 0.35f;
    [SerializeField] private float glowRotateSpeed = 20f;
    [SerializeField] private float glowPulse = 0.08f;
    [SerializeField] private float flashDuration = 0.3f;
    private Coroutine itemFxCo;
    private Vector2 itemRootHome;
    [SerializeField] private CanvasGroup itemSpotlight;
    [SerializeField] private Image itemImage;
    [SerializeField] private float itemFadeDuration = 0.25f;
    private bool itemShowing;
    private Coroutine itemCo;

    [Header("Backlog")]
    [SerializeField] private string choiceSpeaker = "에밀";
    [SerializeField] private string choicePortrait = "";    

    [Header("Visuals")]
    [SerializeField] private BackgroundDatabase bgDB;
    [SerializeField] private RectTransform bgContainer;
    [SerializeField] private SpriteDatabase spriteDB;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image portraitImage;
    [SerializeField] private Vector2 actorSize = new Vector2(400, 400);
    private GameObject currentBgInstance;
    private string currentBgKey;
    [SerializeField] private float cutFlickerInterval = 1f;
    private Coroutine cutFlickerCo;

    [Header("Stage")]
    [SerializeField] private RectTransform characterRoot;
    [SerializeField] private RectTransform slotLeft, slotCenter, slotRight, slotOff;
    [SerializeField] private float centerPairOffset = 150f;
    
    [Header("Shake")]
    [SerializeField] private RectTransform shakeRoot;
    [SerializeField] private float shakeDuration = 0.3f;
    [SerializeField] private float shakeMagnitude = 15f;
    
    [Header("Actor Wiggle")]
    [SerializeField] private float wiggleCycleTime = 0.35f;

    [Header("Fade")]
    [SerializeField] private Fader fader;
    [SerializeField] private CanvasGroup characterGroup;       
    [SerializeField] private float characterFadeDuration = 0.5f;
    [SerializeField] private float actorFadeDuration = 0.5f;
    [SerializeField] private float flashInDuration = 0.08f;
    [SerializeField] private float flashOutDuration = 0.7f;
    private Dictionary<string, Coroutine> fades = new Dictionary<string, Coroutine>();

    [Header("CutScene")]
    [SerializeField] private GameObject narrationRoot;
    [SerializeField] private TMP_Text narrationText;
    [SerializeField] private CanvasGroup narrationGroup;
    [SerializeField] private Image cutsceneImage;
    [SerializeField] private float narrationFadeDuration = 0.35f;
    [SerializeField] private float breakFadeDuration = 0.45f;
    [SerializeField] private float narrationHold = 2f; 
    [SerializeField] private GameObject[] hideInNarration;
    [SerializeField] private GameObject logButton;

    [SerializeField] private float cutFadeOut = 0.8f;
    [SerializeField] private float cutBlackHold = 0.3f;
    [SerializeField] private float cutFadeIn = 1.0f;
    [SerializeField] private bool autoBlackBackdrop = true;

    [SerializeField] private bool useCutsceneDrift = true;
    [SerializeField] private float driftAmplitude = 4f;
    [SerializeField] private float driftRotation = 0.25f;
    [SerializeField] private float driftSpeed = 0.4f;
    [SerializeField] private float driftOverscale = 1.03f;
    
    [SerializeField] private ShutterFx shutterFx;
    [SerializeField] private GlitchFx glitchFx;   // 비워두면 처음 쓸 때 자동 생성
    
    [SerializeField] private float centerTextOffsetY = 444f;   // 가운데 대사일 때 TextGroup을 올릴 양
    private RectTransform textGroupRt;
    private Vector2 textGroupHome;
    private bool textGroupHomeSaved;
    
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
    public string Summary => data == null ? null
        : Loc.Get(Loc.Story, $"{data.storyId}_summary", data.summary);

    private int selectedIndex;

    private Dictionary<string, Image> stage = new Dictionary<string, Image>();
    private Dictionary<string, Coroutine> movers = new Dictionary<string, Coroutine>();

    public bool IsActive => current != null;
    
    [System.Serializable]
    public struct LogLine { public string speaker, text, portrait, locKey; }

    private readonly List<LogLine> log = new List<LogLine>();
    private string lastLoggedNodeId;
    public IReadOnlyList<LogLine> Log => log;

    void Awake()
    {
        if (shakeRoot != null) shakeHome = shakeRoot.anchoredPosition;
        if (itemRoot != null) itemRootHome = itemRoot.anchoredPosition;
        SetupCutsceneLayers();
        for (int i = 0; i < choiceSlots.Length; i++)
        {
            int idx = i;
            var btn = choiceSlots[i].GetComponentInParent<Button>(true);
            if (btn != null) btn.onClick.AddListener(() => ClickChoice(idx));
        }
    }
    
    void OnEnable()  { LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged; }
    void OnDisable() { LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged; }
    
    static void BuildSpeakerKeys()
    {
        speakerKeys = new Dictionary<string, string>();
        var ko = LocalizationSettings.AvailableLocales.GetLocale("ko");
        var table = LocalizationSettings.StringDatabase.GetTable(Loc.UI, ko);
        if (table == null) return;

        foreach (var entry in table.Values)
        {
            if (entry == null || string.IsNullOrEmpty(entry.Value)) continue;
            if (!entry.Key.StartsWith(SpeakerPrefix)) continue;
            speakerKeys[entry.Value.Trim()] = entry.Key;
        }
    }
    
    public static string LocalizeSpeaker(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return raw;
        if (speakerKeys == null || speakerKeys.Count == 0) BuildSpeakerKeys();

        var name = raw.Trim();
        return speakerKeys.TryGetValue(name, out var k) ? Loc.Get(Loc.UI, k, raw) : raw;
    }
    
    // -------------------- 재생 --------------------
    public void Play(string storyId, int startIndex = 0)
    {
        string path = Path.Combine(Application.streamingAssetsPath, storyId + ".json");
        
        if (!File.Exists(path))
        {
            Debug.LogError($"[DialogueRunner] 스토리 파일 없음: {storyId}.json");
            return;
        }
        data = JsonConvert.DeserializeObject<DialogueData>(File.ReadAllText(path));
        ending = false;
        chosenNextStage = null;
        chosenNextScene = null;
        if (data == null) return;

        GameFlow.LastStoryStage = storyId;
        if (data.chapterNumber > 0)
        {
            GameFlow.ChapterNumber = data.chapterNumber;
            GameFlow.ChapterStoryId = data.storyId;
        }
        if (!string.IsNullOrEmpty(data.location)) GameFlow.Location = data.location;
        if (data.day > 0) GameFlow.Day = data.day;
        SaveManager.Save(force: true);  
        
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
            ApplyHideInNarration(false);

        ApplyBackground(data.background, false);

        startIndex = Mathf.Clamp(startIndex, 0, data.nodes.Count - 1);
        var first        = data.nodes[startIndex];
        string startMode = string.IsNullOrEmpty(first.mode) ? "dialogue" : first.mode;
        bool   startNar  = (startMode == "narration");

        currentMode   = startMode;
        transitioning = false;
        if (narrationRoot != null) narrationRoot.SetActive(startNar);

        if (hideInNarration != null)
            ApplyHideInNarration(startNar);

        string startBg = !string.IsNullOrEmpty(first.bg) ? first.bg : data.background;
        ApplyBackground(startBg, startNar);
        StartCoroutine(IntroRoutine(startIndex));
    }

    void Show(DialogueNode node)
    {
        if (typing != null)
        {
            StopCoroutine(typing);
            typing = null;
        }
        if (showCo != null) StopCoroutine(showCo);
        showCo = StartCoroutine(ShowRoutine(node));
    }

    IEnumerator ShowRoutine(DialogueNode node)
    {
        string m = string.IsNullOrEmpty(node.mode) ? "dialogue" : node.mode;
        bool modeChanged = (m != currentMode);
        bool bgChanged   = !string.IsNullOrEmpty(node.bg);
        bool doCutFade = node.fade && bgChanged && cutsceneImage != null
                      && (string.IsNullOrEmpty(node.mode) ? currentMode : node.mode) == "narration";

        bool doFade = node.fadeBreak && fader != null && (modeChanged || bgChanged)
                      && !isIntro && !doCutFade;
        bool targetNar = (string.IsNullOrEmpty(node.mode) ? currentMode : node.mode) == "narration";
        bool doShutter = node.shutter && bgChanged && shutterFx != null && targetNar
                          && !doCutFade;
        bool doFlash = node.flash && bgChanged && fader != null && !doFade && !doCutFade && !doShutter;
        if (node.glitch && bgChanged && glitchFx == null) glitchFx = CreateGlitchFx();
        bool doGlitch = node.glitch && bgChanged && glitchFx != null && !doFade && !doCutFade && !doShutter;
        isIntro = false;
        bool sceneChange = doFade && bgChanged;
        bool imageFaded = false;
        bool shutterAfterFade = false;

        transitioning = true;
        yield return FadeOutActors(node, sceneChange);
        transitioning = false;
        
        if (doFade)
        {
            transitioning = true;
            yield return fader.FadeOut(breakFadeDuration);
            if (sceneChange) ClearStage();
        }

        if (modeChanged)
        {
            bool nar = (m == "narration");
            panel.SetActive(!nar);
            if (characterRoot != null) characterRoot.gameObject.SetActive(!nar);
            if (portraitImage != null && nar) portraitImage.gameObject.SetActive(false);
            if (narrationRoot != null) narrationRoot.SetActive(nar);
            
            if (hideInNarration != null)
                ApplyHideInNarration(nar);
            currentMode = m;
        }
        
        if (m != "narration") StopCutFlicker();

        if (currentMode != "narration" && !node.scroll)
        {
            bool showUI = !node.fullscreenBg;
            panel.SetActive(showUI);
            if (characterRoot != null) characterRoot.gameObject.SetActive(showUI);
            if (!showUI && portraitImage != null) portraitImage.gameObject.SetActive(false);
        }
        
        if (node.scroll)
        {
            panel.SetActive(false);
            if (characterRoot != null) characterRoot.gameObject.SetActive(false);
        }
        
        if (doFlash)
        {
            transitioning = true;
            yield return fader.FlashIn(Color.white, flashInDuration);
        }
        
        if (bgChanged)
        {
            bool nar = currentMode == "narration";
            bool isPrefab = bgDB != null && bgDB.Get(node.bg) != null;

            if (doCutFade && !isPrefab && spriteDB != null && !node.bg.Contains("|"))
            {
                Sprite s = spriteDB.Get(node.bg);
                if (s != null)
                {
                    transitioning = true;
                    imageFaded = true;
                    yield return FadeSwapRoutine(s);
                    transitioning = false;
                }
            }
            else if (doShutter && doFade)
            {
                ApplyBackground("black", nar);
                shutterAfterFade = true;
            }
            else if (doShutter)
            {
                transitioning = true;
                if (!string.IsNullOrEmpty(node.sfx))
                {
                    lastSfxNodeId = node.id;
                    SoundManager.EnsureExists();
                    SoundManager.instance?.PlaySfx(node.sfx);
                }
                yield return shutterFx.Play(() => ApplyBackground(node.bg, nar));
                transitioning = false;
            }
            else if (doGlitch)
            {
                transitioning = true;
                PlayNodeSfx(node);   // 지지직 소리를 효과 시작과 동시에
                yield return glitchFx.Play(() => ApplyBackground(node.bg, nar));
                transitioning = false;
            }
            else ApplyBackground(node.bg, nar);
        }
        

        if (!string.IsNullOrEmpty(node.bgm)) ApplyBgm(node.bgm);
        
        if (!string.IsNullOrEmpty(node.location))
        {
            GameFlow.Location = node.location;
            ChapterHeader.instance?.Refresh();
        }
        if (node.day > 0)
        {
            GameFlow.Day = node.day;
            ChapterHeader.instance?.Refresh();
        }
        
        Target.text = "";
        Target.maxVisibleCharacters = 0;
        ApplyTextPosition(node);

        if (currentMode != "narration")
        {
            ApplyPortrait(node);
            if (!sceneChange && !node.scroll) ApplyActors(node);   ApplyActors(node);
            speakerText.text = LocalizeSpeaker(node.speaker);
        }

        if (doFade)
        {
            yield return fader.FadeIn(breakFadeDuration);
            transitioning = false;
        }
        if (shutterAfterFade)
        {
            PlayNodeSfx(node);
            transitioning = true;
            yield return shutterFx.Play(() => ApplyBackground(node.bg, currentMode == "narration"));
            transitioning = false;
        }
        
        if (doFlash)
        {
            StartCoroutine(FlashOutAndRelease());
        }
        if (sceneChange && currentMode != "narration" && !node.scroll) ApplyActors(node);
            ApplyActors(node);    

        if (!string.IsNullOrEmpty(node.item)) ShowItem(node.item);
        if (node.shake) Shake();
        
        if (!string.IsNullOrEmpty(node.sfx) && node.id != lastSfxNodeId)
        {
            lastSfxNodeId = node.id;
            SoundManager.EnsureExists();
            SoundManager.instance?.PlaySfx(node.sfx);
        }

        if (currentMode == "narration" && narrationGroup != null)
        {
            if (imageFaded)
                narrationGroup.alpha = 1f;
            else
            {
                narrationGroup.alpha = 0f;
                StartCoroutine(FadeCanvas(narrationGroup, 0f, 1f, narrationFadeDuration));
            }
        }
        
        if (node.scroll)
        {
            typing = StartCoroutine(ScrollRoutine(node));
            yield break;
        }

        AddLog(node, currentMode);
        if (typing != null) StopCoroutine(typing);
        typing = StartCoroutine(TypeText(Loc.Get(Loc.Story, node.locKey, node.text)));
    }

    GlitchFx CreateGlitchFx()
    {
        if (backgroundImage == null) return null;
        var parent = backgroundImage.rectTransform.parent as RectTransform;
        if (parent == null) return null;

        // 배경(backgroundImage / bgContainer) 바로 위에 깔리도록
        int index = backgroundImage.rectTransform.GetSiblingIndex();
        if (bgContainer != null && bgContainer.parent == parent)
            index = Mathf.Max(index, bgContainer.GetSiblingIndex());

        return GlitchFx.Create(parent, index + 1, backgroundImage.rectTransform, bgContainer);
    }

    void ApplyBackground(string key, bool narration)
    {
        if (string.IsNullOrEmpty(key)) return;
        
        if (narration && key.Contains("|") && cutsceneImage != null)
        {
            StartCutFlicker(key);
            return;
        }
        
        if (narration && key == "black" && cutsceneImage != null)
        {
            SetImageAlpha(cutsceneImage, 0f);
            return;
        }

        var prefab = bgDB != null ? bgDB.Get(key) : null;
        if (prefab != null)
        {
            if (key == currentBgKey && currentBgInstance != null) return;
            if (currentBgInstance != null) Destroy(currentBgInstance);
            currentBgInstance = Instantiate(prefab, bgContainer);
            
            var cg = currentBgInstance.GetComponent<CanvasGroup>();
            if (cg == null) cg = currentBgInstance.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;
            cg.interactable = false;
            
            currentBgKey = key;
            if (backgroundImage != null) backgroundImage.enabled = false;
            return;
        }

        Sprite s = spriteDB != null ? spriteDB.Get(key) : null;
        if (s == null) return;

        if (narration && cutsceneImage != null)
        {
            StopCutFlicker();
            cutsceneImage.sprite = s;
            SetImageAlpha(cutsceneImage, 1f);
            return;
        }

        if (currentBgInstance != null) { Destroy(currentBgInstance); currentBgInstance = null; }
        currentBgKey = key;
        if (backgroundImage != null)
        {
            backgroundImage.enabled = true;
            backgroundImage.sprite = s;
        }
    }
    
    void ApplyTextPosition(DialogueNode node)
    {
        if (narrationText == null) return;
        if (!textGroupHomeSaved)
        {
            textGroupRt = narrationText.rectTransform.parent as RectTransform;   // TextGroup
            if (textGroupRt == null) return;
            textGroupHome = textGroupRt.anchoredPosition;
            textGroupHomeSaved = true;
        }
        textGroupRt.anchoredPosition = node.centerText
            ? textGroupHome + new Vector2(0f, centerTextOffsetY)
            : textGroupHome;
    }
    
    void ApplyHideInNarration(bool nar)
    {
        if (hideInNarration != null)
            foreach (var go in hideInNarration)
                if (go != null) go.SetActive(!nar);

        if (data != null && data.hideChapterHeader && ChapterHeader.instance != null)
            ChapterHeader.instance.gameObject.SetActive(false);
        
        if (data != null && data.hideLogButton && logButton != null)
            logButton.SetActive(false);
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
    
    IEnumerator FlashOutAndRelease()
    {
        yield return fader.FlashOut(flashOutDuration);
        transitioning = false;
    }
    
    void PlayNodeSfx(DialogueNode node)
    {
        if (string.IsNullOrEmpty(node.sfx) || node.id == lastSfxNodeId) return;
        lastSfxNodeId = node.id;
        SoundManager.EnsureExists();
        SoundManager.instance?.PlaySfx(node.sfx);
    }
    
    IEnumerator ShutterOpenAndRelease(DialogueNode node, bool nar)
    {
        yield return shutterFx.Play(() => ApplyBackground(node.bg, nar));  // 닫힘 → 교체 → 열림
        transitioning = false;
    }

    IEnumerator IntroRoutine(int startIndex)
    {
        bool nar = (currentMode == "narration");
        
        panel.SetActive(false);
        if (characterRoot != null) characterRoot.gameObject.SetActive(false);
        if (characterGroup != null) characterGroup.alpha = 0f;

        if (fader != null)
            yield return fader.FadeIn();

        // Stage1 이후 첫 스토리: 대사 시작 전에 Tab(프로필) 안내
        if (ProfileGuide.ShouldShow)
            yield return ProfileGuide.GetOrCreate().Run();
        
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

        if (characterGroup != null)
        {
            if (!nar) yield return FadeCanvas(characterGroup, 0f, 1f, characterFadeDuration);
            else      characterGroup.alpha = 1f;
        }
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

        string stage = string.IsNullOrEmpty(chosenNextStage) ? data.nextStage : chosenNextStage;
        string scene = string.IsNullOrEmpty(chosenNextScene) ? data.nextScene : chosenNextScene;
        
        var br = data.stageBranch;
        if (br != null && !string.IsNullOrEmpty(br.checkStage))
        {
            bool cleared = GameFlow.IsAllFound(br.checkStage);
            stage = cleared ? br.clearedStage : br.failedStage;
        }

        if (!string.IsNullOrEmpty(scene))
        {
            if (data.skipLoading) SceneRouter.LoadDirect(scene, stage);
            else                  SceneRouter.Load(scene, stage);
        }
        else if (!string.IsNullOrEmpty(stage))
            GameFlow.CurrentStage = stage;
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
        var t = Target;
        bool typingSfxOn = currentMode != "narration" && !string.IsNullOrEmpty(full)
                                                      && t.gameObject.activeInHierarchy;
        t.maxVisibleCharacters = 0;
        t.text = full;
        t.ForceMeshUpdate();
        int total = string.IsNullOrEmpty(full) ? 0 : t.textInfo.characterCount;
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
        if (itemShowing)
        {
            HideItem();
            return;
        }

        if (current.choices != null && current.choices.Count > 0) return;
        Next();
    }
    
    void SelectChoice(int index)
    {
        if (index == selectedIndex) return;
        selectedIndex = index;
        UpdateChecks();
        SoundManager.instance?.PlayUiSfx("ui_hover");
    }

    void Next()
    {
        if (current == null || !current.keepSfx)
            SoundManager.instance?.StopAllSfx();
        if (current.next == null) { EndDialogue(); return; }
        current = data.nodes.FirstOrDefault(n => n.id == current.next);
        if (current == null) { EndDialogue(); return; }
        Show(current);
    }

    void Choose(string nextId)
    {
        SoundManager.instance?.StopAllSfx();
        if (string.IsNullOrEmpty(nextId)) { EndDialogue(); return; }
        current = data.nodes.FirstOrDefault(n => n.id == nextId);
        if (current == null) { EndDialogue(); return; }
        Show(current);
    }

    public void Skip()
    {
        HideItem(true);
        transitioning = false;
        if (showCo != null) { StopCoroutine(showCo); showCo = null; }
        if (fader != null) fader.ResetOverlay();
        ResetCutsceneOverlay();
        
        if (ending || data == null || current == null) return;
        if (typing != null)
        {
            StopCoroutine(typing);
            typing = null;
        }
        isTyping = false;
        SoundManager.instance?.StopTyping();
        SoundManager.instance?.StopAllSfx();

        if (choosing) return;

        if (current.choices != null && current.choices.Count > 0)
        {
            Show(current);
            return;
        }

        string lastDlgBg = null;
        string lastCutBg = null;
        string lastBgm = null;
        string lastLocation = null;
        int lastDay = 0;
        
        var guard = new HashSet<string>();
        string logMode = currentMode;

        DialogueNode node = current;
        while (true)
        {
            if (node.id != null && !guard.Add(node.id)) break;

            if (!string.IsNullOrEmpty(node.mode)) logMode = node.mode;
            AddLog(node, logMode);
            
            ApplyActors(node);
            string nodeMode = string.IsNullOrEmpty(node.mode) ? "dialogue" : node.mode;
            if (!string.IsNullOrEmpty(node.bg))
            {
                if (nodeMode == "narration") lastCutBg = node.bg;
                else                         lastDlgBg = node.bg;
            }
            if (!string.IsNullOrEmpty(node.bgm)) lastBgm = node.bgm;
            if (!string.IsNullOrEmpty(node.location)) lastLocation = node.location;
            if(node.day > 0) lastDay = node.day;


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

        ApplyStateBeforeJump(lastDlgBg, lastCutBg, node);
        if(!string.IsNullOrEmpty(lastBgm) && string.IsNullOrEmpty(node.bgm))
            ApplyBgm(lastBgm);
        if (!string.IsNullOrEmpty(lastLocation) && string.IsNullOrEmpty(node.location))
        {
            GameFlow.Location = lastLocation;
            ChapterHeader.instance?.Refresh();
        }
        if (lastDay > 0 && node.day <= 0)
        {
            GameFlow.Day = lastDay;
            ChapterHeader.instance?.Refresh();
        }
        current = node;
        Show(current);
    }
    
    IEnumerator ScrollRoutine(DialogueNode node)
    {
        transitioning = true;
        var scroller = currentBgInstance != null
            ? currentBgInstance.GetComponentInChildren<VerticalBgScroller>()
            : null;
        if (scroller != null) yield return scroller.Play();

        panel.SetActive(true);
        if (characterRoot != null) characterRoot.gameObject.SetActive(true);
        ApplyActors(node);
        transitioning = false;
        typing = null;
        if (current != node) yield break;

        if (string.IsNullOrEmpty(node.text)) { Next(); yield break; }
        AddLog(node, currentMode);
        typing = StartCoroutine(TypeText(Loc.Get(Loc.Story, node.locKey, node.text)));
    }

    IEnumerator AutoAdvance(DialogueNode node)
    {
        yield return new WaitForSeconds(HoldOf(node));
        typing = null;
        if (current == node) Next();
    }

    void EndDialogue()
    {
        HideItem(true);
        if (ending) return;
        ending = true;
        current = null;
        StartCoroutine(OutroRoutine());
    }
    
    void AddLog(DialogueNode node, string mode)
    {
        if (node == null || string.IsNullOrEmpty(node.text)) return;
        if (node.id == lastLoggedNodeId) return;

        bool nar = mode == "narration";
        lastLoggedNodeId = node.id;
        log.Add(new LogLine {
            speaker  = nar ? "" : node.speaker,
            text     = node.text,
            portrait = nar ? "" : node.portrait,
            locKey   = node.locKey
        });
    }

    // ------------------------------------------------------------- 선택지
    void ShowChoices()
    {
        choosing = true;
        choiceReadyTime = Time.unscaledTime + choiceInputDelay;
        selectedIndex = 0;

        for (int i = 0; i < choiceSlots.Length; i++)
        {
            bool used = i < current.choices.Count;
            choiceSlots[i].SetActive(used);
            if (used) choiceLabels[i].text =
                Loc.Get(Loc.Story, $"{current.locKey}_choice{i}", current.choices[i].text);
        }
        UpdateChecks();
        if (choicePopup != null) choicePopup.Open();
    }
    
    public void ClickChoice(int index)
    {
        if (!CanConfirm || index >= current.choices.Count) return;
        SoundManager.instance?.PlayUiSfx("story_click");
        selectedIndex = index;
        Confirm();
    }
    
    public void Hover(int index)
    {
        if (!choosing || index >= current.choices.Count) return;
        SelectChoice(index);
    }

    void UpdateChecks()
    {
        for (int i = 0; i < choiceChecks.Length; i++)
            choiceChecks[i].SetActive(i == selectedIndex);
    }

    public void Move(int dir)
    {
        int count = current.choices.Count;
        SelectChoice((selectedIndex + dir + count) % count);
    }

    public void Confirm()
    {
        if (!CanConfirm) return;
        choosing = false;
        if (choicePopup != null) choicePopup.Close();
        
        var choice = current.choices[selectedIndex];
        if (!string.IsNullOrEmpty(choice.nextStage))
            chosenNextStage = choice.nextStage;
        if (!string.IsNullOrEmpty(choice.nextScene))
            chosenNextScene = choice.nextScene;
        log.Add(new LogLine {
            speaker  = choiceSpeaker,
            text     = choice.text,
            portrait = choicePortrait,
            locKey   = $"{current.locKey}_choice{selectedIndex}"
        });
        
        Choose(current.choices[selectedIndex].next);
    }
    
    void ShowItem(string key)
    {
        Sprite s = spriteDB != null ? spriteDB.Get(key) : null;
        if (s == null || itemSpotlight == null) return;

        itemImage.sprite = s;
        itemSpotlight.gameObject.SetActive(true);
        itemShowing = true;
        if (itemCo != null) StopCoroutine(itemCo);
        itemCo = StartCoroutine(FadeCanvas(itemSpotlight, 0f, 1f, itemFadeDuration));
        if (itemFxCo != null) StopCoroutine(itemFxCo);
        itemFxCo = StartCoroutine(ItemFxRoutine());
    }
    IEnumerator ItemFxRoutine()
    {
        if (itemFlash != null)
        {
            itemFlash.sprite = itemImage.sprite;
            SetImageAlpha(itemFlash, 1f);
        }

        float t = 0f;
        while (t < popDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / popDuration);
            float s = p < 0.6f
                ? Mathf.Lerp(0.6f, 1.1f, p / 0.6f)
                : Mathf.Lerp(1.1f, 1.0f, (p - 0.6f) / 0.4f);
            itemRoot.localScale = Vector3.one * s;

            if (itemFlash != null)
                SetImageAlpha(itemFlash, 1f - Mathf.Clamp01(t / flashDuration));
            yield return null;
        }
        itemRoot.localScale = Vector3.one;
        if (itemFlash != null) SetImageAlpha(itemFlash, 0f);

        float time = 0f;
        while (true)
        {
            time += Time.deltaTime;

            if (itemGlow != null)
            {
                itemGlow.Rotate(0f, 0f, -glowRotateSpeed * Time.deltaTime);
                float g = 1f + Mathf.Sin(time * 2f) * glowPulse;
                itemGlow.localScale = Vector3.one * g;
            }
            yield return null;
        }
    }

    void HideItem(bool instant = false)
    {
        if (itemFxCo != null) { StopCoroutine(itemFxCo); itemFxCo = null; }
        if (itemRoot != null)
        {
            itemRoot.anchoredPosition = itemRootHome;
            itemRoot.localScale = Vector3.one;
        }
        if (itemSpotlight == null) return;
        itemShowing = false;
        if (itemCo != null) StopCoroutine(itemCo);
        if (instant) { itemSpotlight.alpha = 0f; itemSpotlight.gameObject.SetActive(false); return; }
        itemCo = StartCoroutine(HideItemRoutine());
    }

    IEnumerator HideItemRoutine()
    {
        yield return FadeCanvas(itemSpotlight, itemSpotlight.alpha, 0f, itemFadeDuration);
        itemSpotlight.gameObject.SetActive(false);
    }

    // ------------------------------------------------------------- 무대(배우)
    Vector3 GetSlotPos(string s) => s switch
    {
        "Left"        => slotLeft.position,
        "Right"       => slotRight.position,
        "Off"         => slotOff.position,
        "CenterLeft"  => slotCenter.TransformPoint(new Vector3(-centerPairOffset, 0f, 0f)),
        "CenterRight" => slotCenter.TransformPoint(new Vector3( centerPairOffset, 0f, 0f)),
        _             => slotCenter.position,
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
                    float aspect = s.rect.width / s.rect.height;
                    img.rectTransform.sizeDelta = new Vector2(actorSize.y * aspect, actorSize.y);
                }
                else Debug.LogWarning($"스프라이트 '{a.sprite}' 를 DB에서 못 찾음");
            }

            StopMover(a.id);
            img.rectTransform.position = GetSlotPos(a.slot);
            
            img.rectTransform.localScale = new Vector3(a.flip ? -1f : 1f, 1f, 1f);
            
            if (a.wiggle > 0)
            {
                Vector3 dest = string.IsNullOrEmpty(a.wiggleTo) ? slotLeft.position : GetSlotPos(a.wiggleTo);
                movers[a.id] = StartCoroutine(WiggleRoutine(img.rectTransform, dest, a.wiggle));
            }
            
            if (a.fadeIn)
                StartActorFade(a.id, img, a.brightness);
            else
            {
                if (fades.TryGetValue(a.id, out var running) && running != null)
                {
                    StopCoroutine(running);
                    fades.Remove(a.id);
                }
                img.color = new Color(a.brightness, a.brightness, a.brightness, 1f);
            }
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
        StopMover(id);
    }
    
    IEnumerator FadeOutActors(DialogueNode node, bool all)
    {
        if (currentMode == "narration") yield break;

        var ids = new List<string>();
        if (all)
        {
            ids.AddRange(stage.Keys);
        }
        else if (node.actors != null)
        {
            foreach (var a in node.actors)
                if (a.slot == "Off" && a.fadeIn) ids.Add(a.id);
        }

        var leaving = new List<Image>();
        var startA  = new List<float>();
        foreach (var id in ids)
        {
            if (!stage.TryGetValue(id, out var img) || img == null) continue;
            if (fades.TryGetValue(id, out var c) && c != null) StopCoroutine(c);
            fades.Remove(id);
            leaving.Add(img);
            startA.Add(img.color.a);
        }
        if (leaving.Count == 0) yield break;

        float t = 0f;
        while (t < actorFadeDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / actorFadeDuration);
            for (int i = 0; i < leaving.Count; i++)
            {
                if (leaving[i] == null) continue;
                var col = leaving[i].color;
                col.a = Mathf.Lerp(startA[i], 0f, k);
                leaving[i].color = col;
            }
            yield return null;
        }
    }

    void ClearStage()
    {
        HideItem(true);
        foreach (var kv in fades) if (kv.Value != null) StopCoroutine(kv.Value);
        fades.Clear();

        foreach (var kv in stage) if (kv.Value != null) Destroy(kv.Value.gameObject);
        stage.Clear();
        foreach (var kv in movers)
            if (kv.Value != null) StopCoroutine(kv.Value);
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
    
    void StopMover(string id)
    {
        if (movers.TryGetValue(id, out var c) && c != null) StopCoroutine(c);
        movers.Remove(id);
    }

    IEnumerator WiggleRoutine(RectTransform rt, Vector3 dest, int count)
    {
        Vector3 home = rt.position;
        float total = wiggleCycleTime * count;
        float t = 0f;
        while (t < total)
        {
            if (rt == null) yield break;
            t += Time.deltaTime;
            float k = (1f - Mathf.Cos(t / wiggleCycleTime * Mathf.PI * 2f)) * 0.5f;
            rt.position = Vector3.Lerp(home, dest, k);
            yield return null;
        }
        if (rt != null) rt.position = home;
    }
    
    void SetupCutsceneLayers()
    {
        if (cutsceneImage == null) return;
        var imgRt = cutsceneImage.rectTransform;
        var parent = imgRt.parent as RectTransform;
        if (parent == null) return;

        int index = imgRt.GetSiblingIndex();

        if (autoBlackBackdrop)
        {
            var black = new GameObject("CutsceneBlack", typeof(RectTransform), typeof(Image));
            var brt = (RectTransform)black.transform;
            brt.SetParent(parent, false);
            Stretch(brt);
            brt.SetSiblingIndex(index);
            index++;
            var bimg = black.GetComponent<Image>();
            bimg.color = Color.black;
            bimg.raycastTarget = false;
        }

        if (useCutsceneDrift && parent.GetComponent<CutsceneDrift>() == null)
        {
            var driftGo = new GameObject("CutsceneDrift", typeof(RectTransform));
            var drt = (RectTransform)driftGo.transform;
            drt.SetParent(parent, false);
            Stretch(drt);
            drt.SetSiblingIndex(index);

            imgRt.SetParent(drt, false);

            var drift = driftGo.AddComponent<CutsceneDrift>();
            drift.Configure(driftAmplitude, driftRotation, driftSpeed, driftOverscale);
        }
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
    }

    IEnumerator FadeSwapRoutine(Sprite next)
    {
        StopCutFlicker();
        float t = 0f;
        while (t < cutFadeOut)
        {
            t += Time.deltaTime;
            SetImageAlpha(cutsceneImage, 1f - Mathf.SmoothStep(0f, 1f, t / cutFadeOut));
            yield return null;
        }
        SetImageAlpha(cutsceneImage, 0f);

        cutsceneImage.sprite = next;
        if (cutBlackHold > 0f) yield return new WaitForSeconds(cutBlackHold);

        t = 0f;
        while (t < cutFadeIn)
        {
            t += Time.deltaTime;
            SetImageAlpha(cutsceneImage, Mathf.SmoothStep(0f, 1f, t / cutFadeIn));
            yield return null;
        }
        SetImageAlpha(cutsceneImage, 1f);
    }
    
    void StartCutFlicker(string key)
    {
        StopCutFlicker();
        var frames = new List<Sprite>();
        foreach (var k in key.Split('|'))
        {
            var s = spriteDB != null ? spriteDB.Get(k.Trim()) : null;
            if (s != null) frames.Add(s);
        }
        if (frames.Count == 0) return;

        cutsceneImage.sprite = frames[0];
        SetImageAlpha(cutsceneImage, 1f);
        if (frames.Count > 1)
            cutFlickerCo = StartCoroutine(CutFlickerRoutine(frames));
    }

    IEnumerator CutFlickerRoutine(List<Sprite> frames)
    {
        int i = 0;
        while (true)
        {
            yield return new WaitForSeconds(cutFlickerInterval);
            i = (i + 1) % frames.Count;
            cutsceneImage.sprite = frames[i];
        }
    }

    void StopCutFlicker()
    {
        if (cutFlickerCo != null) { StopCoroutine(cutFlickerCo); cutFlickerCo = null; }
    }

    static void SetImageAlpha(Image img, float a)
    {
        if (img == null) return;
        var c = img.color;
        c.a = a;
        img.color = c;
    }

    void ResetCutsceneOverlay()
    {
        SetImageAlpha(cutsceneImage, 1f);
    }

    void ApplyStateBeforeJump(string lastDlgBg, string lastCutBg, DialogueNode target)
    {
        if (!string.IsNullOrEmpty(lastCutBg))
            ApplyBackground(lastCutBg, true);
        if (!string.IsNullOrEmpty(lastDlgBg) && string.IsNullOrEmpty(target.bg))
            ApplyBackground(lastDlgBg, false);
    }

    void ApplyBgm(string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        SoundManager.EnsureExists();
        SoundManager.instance?.PlayBgmById(id);
    }
    
    void OnLocaleChanged(Locale _)
    {
        if (current == null) return;

        if (typing != null) { StopCoroutine(typing); typing = null; }

        Target.text = Loc.Get(Loc.Story, current.locKey, current.text);
        Target.ForceMeshUpdate();
        Target.maxVisibleCharacters = Target.textInfo.characterCount;

        if (currentMode != "narration")
            speakerText.text = LocalizeSpeaker(current.speaker);

        if (current.choices != null && current.choices.Count > 0) ShowChoices();
    }
}