using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

public class ButtonHover : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler
{
    
    [Header("SFX")]
    [SerializeField] private string hoverSfxId = "ui_hover";
    [SerializeField] private string clickSfxId = "";
    [SerializeField] private bool playHoverSfx = true;

    private static float lastHoverSfxTime = -999f;
    private const float HoverSfxInterval = 0.05f;
    
    private float enabledTime;
    
    [System.Serializable]
    public class LabelEntry
    {
        public TMP_Text text;
        public Color normal = new Color32(0x8C, 0x8C, 0x8C, 0xFF);
        public Color hover  = Color.white;
        public Color selected = Color.white;
        public bool moveOnPress = true;
        public float selectedScale = 1.12f;

        [HideInInspector] public Vector2 home;
    }

    [Header("Labels")]
    [SerializeField] private LabelEntry[] labels;

    [Header("Press")]
    [SerializeField] private Vector2 pressOffset = new Vector2(0f, -4f);
    [SerializeField] private float moveDuration = 0.08f;
    [SerializeField] private float fadeDuration = 0.12f;

    private bool hovering, pressed;
    private bool selected;

    void Awake()
    {
        foreach (var e in labels)
        {
            if (e == null || e.text == null) continue;
            e.home = e.text.rectTransform.anchoredPosition;
            e.text.color = e.normal;
        }
    }
    
    void OnEnable() { enabledTime = Time.unscaledTime; }
    void OnDisable() { KillAll(); }

    void KillAll()
    {
        foreach (var e in labels)
        {
            if (e == null || e.text == null) continue;
            e.text.rectTransform.DOKill();
            e.text.DOKill();
        }
    }

    public void OnPointerEnter(PointerEventData p)
    {
        hovering = true;
        Apply();
        PlayHoverSfx();
    }

    public void OnPointerDown(PointerEventData p)
    {
        pressed = true;
        Apply();

        if (!string.IsNullOrEmpty(clickSfxId))
            SoundManager.instance?.PlaySfx(clickSfxId);
    }
    public void OnPointerExit (PointerEventData p) { hovering = false; pressed = false; Apply(); }
    public void OnPointerUp   (PointerEventData p) { pressed  = false; Apply(); }

    void Apply()
    {
        foreach (var e in labels)
        {
            if (e == null || e.text == null) continue;

            Vector2 off = (pressed && e.moveOnPress) ? pressOffset : Vector2.zero;

            e.text.rectTransform.DOKill();
            e.text.rectTransform
                  .DOAnchorPos(e.home + off, moveDuration, true)
                  .SetUpdate(true)
                  .SetLink(e.text.gameObject);
            
            Color target = selected ? e.selected : (hovering ? e.hover : e.normal);
            
            e.text.DOKill();
            e.text.DOColor(target, fadeDuration)
                .SetUpdate(true)
                .SetLink(e.text.gameObject);
            
            float s = selected ? e.selectedScale : 1f;
            e.text.rectTransform.DOScale(s, fadeDuration)
                .SetUpdate(true)
                .SetLink(e.text.gameObject);
        }
    }

    public void SetSelected(bool value)
    {
        if (selected == value) return;
        selected = value;
        Apply();
    }
    
    private void PlayHoverSfx()
    {
        if (!playHoverSfx || string.IsNullOrEmpty(hoverSfxId)) return;

        if (Time.unscaledTime - enabledTime < 0.1f) return;

        if (Time.unscaledTime - lastHoverSfxTime < HoverSfxInterval) return;
        lastHoverSfxTime = Time.unscaledTime;

        SoundManager.instance?.PlaySfx(hoverSfxId);
    }
}