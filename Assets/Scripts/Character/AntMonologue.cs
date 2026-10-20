using System.Collections;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

public class AntMonologue : MonoBehaviour
{
    [SerializeField] private TMP_Text label;

    [Header("Bubble")]
    [SerializeField] private SpriteRenderer bubble;
    [SerializeField] private Vector2 padding = new Vector2(0.3f, 0.2f);
    [SerializeField] private float bubbleWorldScale = 1f;

    [Header("Timing")]
    [SerializeField] private float showTime = 3f;
    [SerializeField] private float hideTime = 3f;
    [SerializeField] private float maxStartDelay = 2f;

    [Header("Pop (Pixel)")]
    [SerializeField] private float[] popInFrames  = { 0.5f, 1.2f, 0.9f, 1f };
    [SerializeField] private float[] popOutFrames = { 1.1f, 0.5f, 0f };
    [SerializeField] private float[] reactFrames  = { 1.25f, 0.9f, 1f };
    [SerializeField] private float   frameRate    = 24f;

    private float     popScale = 0f;
    private bool      shown;
    private Coroutine popCo;
    
    [Header("Occlusion")]
    [SerializeField] private LayerMask occluderMask;
    [SerializeField] private float rayBackDistance = 100f;
    [SerializeField] private float skin = 0.2f;
    [SerializeField] private float checkInterval = 0.1f;

    private Coroutine loop;
    private bool wantVisible;
    private bool notOccluded = true;
    private float nextCheck;
    private Transform cam;
    private Coroutine reactCo;
    private string idleLine;
    
    private void Awake()
    {
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Overflow;
        if (label == null) label = GetComponentInChildren<TMP_Text>(true);

        if (label != null)
        {
            var rt = label.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot     = new Vector2(0.5f, 0.5f);
            label.alignment = TextAlignmentOptions.Center;
            label.margin    = Vector4.zero;

            label.gameObject.SetActive(true);
        }

        popScale = 0f;
        shown = false;

        SetVisible(false);
        ApplyBubbleScale();
    }

    private void Start()
    {
        cam = Camera.main != null ? Camera.main.transform : null;
    }

    private void SetVisible(bool v)
    {
        if (bubble != null) bubble.gameObject.SetActive(v);
        else if (label != null) label.gameObject.SetActive(v);
    }
    
    private void PopIn()
    {
        shown = true;
        if (popCo != null) StopCoroutine(popCo);
        SetVisible(true);
        popCo = StartCoroutine(PlayFrames(popInFrames, false));
    }

    private void PopOut()
    {
        shown = false;
        if (popCo != null) StopCoroutine(popCo);
        popCo = StartCoroutine(PlayFrames(popOutFrames, true));
    }

    private IEnumerator PlayFrames(float[] frames, bool hideAtEnd)
    {
        float frameTime = 1f / Mathf.Max(1f, frameRate);

        for (int i = 0; i < frames.Length; i++)
        {
            popScale = frames[i];
            bool last = (i == frames.Length - 1);
            if (!(last && hideAtEnd))
                yield return new WaitForSeconds(frameTime);
        }

        if (hideAtEnd) SetVisible(false);
        popCo = null;
    }

    private float PopOutTime => popOutFrames.Length / Mathf.Max(1f, frameRate);

    private void LateUpdate()
    {
        ApplyBubbleScale();
        if (label == null || bubble == null) return;

        if (Time.time >= nextCheck)
        {
            nextCheck = Time.time + checkInterval;
            notOccluded = !IsOccluded();
        }

        bool visible = wantVisible && notOccluded;
        if (visible != shown)
        {
            if (visible) PopIn();
            else         PopOut();
        }
    }

    public void Begin(string line)
    {
        if (label == null || string.IsNullOrEmpty(line)) return;

        idleLine = line;
        label.text = Loc.Get(Loc.UI, line, "");
        FitBubble();

        if (loop != null) StopCoroutine(loop);
        loop = StartCoroutine(Loop());
    }

    public void React(string line, float duration)
    {
        if (label == null || string.IsNullOrEmpty(line)) return;

        if (loop != null)
        {
            StopCoroutine(loop);
            loop = null;
        }
        if(reactCo != null) StopCoroutine(reactCo);
        reactCo = StartCoroutine(ReactRoutine(line, duration));
    }

    private IEnumerator ReactRoutine(string line, float duration)
    {
        label.text = Loc.Get(Loc.UI, line, "");
        FitBubble();
        if (shown)
        {
            if (popCo != null) StopCoroutine(popCo);
            popCo = StartCoroutine(PlayFrames(reactFrames, false));
        }
        wantVisible = true;
        
        yield return new WaitForSeconds(duration);
        
        wantVisible = false;

        yield return new WaitForSeconds(PopOutTime);
        reactCo = null;

        if (!string.IsNullOrEmpty(idleLine))
        {
            label.text = Loc.Get(Loc.UI, idleLine, "");
            FitBubble();
            loop = StartCoroutine(Loop());
        }
    }
    
    private IEnumerator Loop()
    {
        yield return new WaitForSeconds(Random.Range(0f, maxStartDelay));

        while (true)
        {
            wantVisible = true;
            yield return new WaitForSeconds(showTime);
            wantVisible = false;
            yield return new WaitForSeconds(hideTime);
        }
    }

    private void FitBubble()
    {
        ApplyBubbleScale();
        if (bubble == null || label == null || bubble.sprite == null) return;

        Sprite  sp  = bubble.sprite;
        float   ppu = sp.pixelsPerUnit;
        Vector4 bd  = sp.border;
        float tail  = bd.y / ppu;
        float minW  = (bd.x + bd.z) / ppu;
        float minH  = (bd.y + bd.w) / ppu;

        Vector2 pref = label.GetPreferredValues(label.text, 0f, 0f);
        label.rectTransform.sizeDelta = pref;

        float k = label.transform.localScale.x;
        float w = Mathf.Max(pref.x * k + padding.x,        minW);
        float h = Mathf.Max(pref.y * k + padding.y + tail, minH);
        bubble.size = new Vector2(w, h);

        Vector3 lp = label.transform.localPosition;
        label.transform.localPosition = new Vector3(w * 0.5f, (tail + h) * 0.5f, lp.z);
    }
    
    private void ApplyBubbleScale()
    {
        if (bubble == null) return;
        Transform p = bubble.transform.parent;
        Vector3 ps = (p != null) ? p.lossyScale : Vector3.one;
        float sx = Mathf.Approximately(ps.x, 0f) ? 1f : Mathf.Abs(ps.x);
        float sy = Mathf.Approximately(ps.y, 0f) ? 1f : Mathf.Abs(ps.y);
        float s = bubbleWorldScale * popScale;
        bubble.transform.localScale = new Vector3(s / sx, s / sy, 1f);
    }

    private bool IsOccluded()
    {
        if (cam == null) return false;

        Vector3 dir    = cam.forward;
        Vector3 origin = transform.position - dir * rayBackDistance;
        float   dist   = rayBackDistance - skin;

        return Physics.Raycast(origin, dir, dist, occluderMask,
            QueryTriggerInteraction.Ignore);
    }
    
    public void FreezeForFade()
    {
        if (loop != null)    { StopCoroutine(loop);    loop = null; }
        if (reactCo != null) { StopCoroutine(reactCo); reactCo = null; }
        if (popCo != null)   { StopCoroutine(popCo);   popCo = null; }
        enabled = false;
    }
}