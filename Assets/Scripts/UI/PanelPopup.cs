using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PanelPopup : MonoBehaviour
{
    [SerializeField] private float[] openSteps  = { 0.15f, 0.15f, 0.55f, 0.55f };
    [SerializeField] private float[] closeSteps = { 0.55f, 0.15f };
    [SerializeField] private float stepTime = 0.05f;
    
    [SerializeField] private bool smooth = true;
    [SerializeField] private float openTime = 0.15f;
    [SerializeField] private float closeTime = 0.1f;
    
    [SerializeField] private float animFps = 24f;
    [SerializeField] private int overshootPixels = 1;
    [SerializeField] private float pixelUnit = 4f;
    [SerializeField] private float minHeight = 16f;

    [SerializeField] private RectTransform[] frames;
    [SerializeField] private CanvasGroup group;

    private class FrameInfo { public RectTransform rt; public float full; }

    private readonly List<FrameInfo> frameInfos = new List<FrameInfo>();
    private readonly List<CanvasGroup> hiddenWhileAnimating = new List<CanvasGroup>();
    private bool inited;
    private bool scaleMode;
    private Coroutine running;

    public bool IsOpen => gameObject.activeSelf;
    
    public void Open()
    {
        gameObject.SetActive(true);
        Init();
        if (running == null) CaptureFullHeights();
        Restart(OpenRoutine());
    }

    public void Close()
    {
        if (!gameObject.activeSelf) return;
        Init();
        Restart(CloseRoutine());
    }
    
    private IEnumerator OpenRoutine()
    {
        BlockInput(true);
        ShowContent(false);

        if (smooth)
        {
            yield return Animate(0f, 1f, openTime, true);

            if (overshootPixels > 0)
            {
                Apply(1f, overshootPixels * pixelUnit); 
                yield return new WaitForSecondsRealtime(animFps > 0f ? 1f / animFps : 0.04f);
            }
        }
        else
            foreach (float r in openSteps)
            {
                Apply(r);
                yield return new WaitForSecondsRealtime(stepTime);
            }

        Apply(1f);
        ShowContent(true);
        BlockInput(false);
        running = null;
    }

    private IEnumerator CloseRoutine()
    {
        BlockInput(true);
        ShowContent(false);

        if (smooth)
            yield return Animate(1f, 0f, closeTime, false);
        else
            foreach (float r in closeSteps)
            {
                Apply(r);
                yield return new WaitForSecondsRealtime(stepTime);
            }

        running = null;
        gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        if (running != null) { StopCoroutine(running); running = null; }
        if (!inited) return;
        Apply(1f);
        ShowContent(true);
        BlockInput(false);
    }

    private void Restart(IEnumerator routine)
    {
        if (running != null) StopCoroutine(running);
        running = StartCoroutine(routine);
    }
    
    private IEnumerator Animate(float from, float to, float time, bool easeOut)
    {
        float t = 0f;
        while (t < time)
        {
            t += Time.unscaledDeltaTime;
            float tq = animFps > 0f ? Mathf.Floor(t * animFps) / animFps : t;
            float p = Mathf.Clamp01(tq / time);
            float e = easeOut ? 1f - Mathf.Pow(1f - p, 2f) : p * p;
            Apply(Mathf.Lerp(from, to, e));
            yield return null;
        }
        Apply(to);
    }
    private void Init()
    {
        if (inited) return;
        inited = true;

        var list = new List<RectTransform>();

        if (frames != null && frames.Length > 0)
        {
            foreach (var f in frames) if (f != null) list.Add(f);
        }
        else if (IsSliced(transform))
        {
            list.Add((RectTransform)transform);
        }
        else
        {
            var candidates = new List<RectTransform>();
            float maxArea = 0f;
            foreach (Transform c in transform)
            {
                if (!IsSliced(c)) continue;
                var rt = (RectTransform)c;
                candidates.Add(rt);
                maxArea = Mathf.Max(maxArea, Area(rt));
            }
            foreach (var rt in candidates)
                if (Area(rt) >= maxArea * 0.5f) list.Add(rt);
        }

        foreach (var rt in list) frameInfos.Add(new FrameInfo { rt = rt });
        scaleMode = frameInfos.Count == 0;
        if (scaleMode) return;

        foreach (var f in frameInfos)
            foreach (Transform c in f.rt) AddHidden(c);

        bool rootIsFrame = frameInfos.Exists(f => f.rt == transform);
        if (!rootIsFrame)
        {
            foreach (Transform c in transform)
            {
                if (frameInfos.Exists(f => f.rt == c)) continue;
                if (IsFullScreen((RectTransform)c)) continue;
                AddHidden(c);
            }
        }
    }

    private void CaptureFullHeights()
    {
        foreach (var f in frameInfos) f.full = f.rt.rect.height;
    }

    private void AddHidden(Transform t)
    {
        var cg = t.GetComponent<CanvasGroup>();
        if (cg == null) cg = t.gameObject.AddComponent<CanvasGroup>();
        hiddenWhileAnimating.Add(cg);
    }
    
    private void Apply(float ratio, float extraPx = 0f)
    {
        foreach (var f in frameInfos)
        {
            float h = ratio >= 1f
                ? f.full
                : Mathf.Max(minHeight, Mathf.Round(f.full * ratio / pixelUnit) * pixelUnit);
            f.rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, h + extraPx);
        }
    }

    private void ShowContent(bool on)
    {
        foreach (var cg in hiddenWhileAnimating)
        {
            if (cg == null) continue;
            cg.alpha = on ? 1f : 0f;
            cg.blocksRaycasts = on;
        }
    }

    private void BlockInput(bool block)
    {
        if (group == null) return;
        group.alpha = 1f; 
        group.interactable = !block;
        group.blocksRaycasts = true;
    }
    
    private static bool IsSliced(Transform t)
    {
        var img = t.GetComponent<Image>();
        return img != null && img.type == Image.Type.Sliced;
    }

    private static bool IsFullScreen(RectTransform rt)
    {
        return rt.anchorMin == Vector2.zero && rt.anchorMax == Vector2.one;
    }

    private static float Area(RectTransform rt)
    {
        var r = rt.rect;
        return Mathf.Abs(r.width * r.height);
    }
}