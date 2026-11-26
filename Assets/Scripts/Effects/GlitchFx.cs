using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 지지직(노이즈) 전환 효과. 강도가 최고일 때 onPeak 콜백으로 배경을 교체한다.
// 씬에 직접 배치해도 되고, 없으면 DialogueRunner가 GlitchFx.Create()로 런타임에 만든다.
public class GlitchFx : MonoBehaviour
{
    [SerializeField] private RawImage noise;                 // 전체화면 노이즈
    [SerializeField] private List<RectTransform> jitterTargets = new List<RectTransform>(); // 가로로 흔들 대상(배경)
    [SerializeField] private Image[] bars;                   // 가로 줄(선택)
    [SerializeField] private Vector2Int noiseRes = new Vector2Int(160, 90);
    [SerializeField] private float rampIn = 0.35f, peakHold = 0.15f, rampOut = 0.5f;
    [SerializeField] private float maxAlpha = 0.85f, jitterPx = 24f, noiseFps = 24f;

    Texture2D tex;
    Color32[] px;
    readonly List<Vector2> homes = new List<Vector2>();
    bool initialized;

    // 런타임 생성: parent 안에 전체화면으로 깔고, siblingIndex 위치에 둔다.
    public static GlitchFx Create(RectTransform parent, int siblingIndex, params RectTransform[] targets)
    {
        var go = new GameObject("GlitchFx", typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        Stretch(rt);
        rt.SetSiblingIndex(Mathf.Clamp(siblingIndex, 0, parent.childCount - 1));

        var fx = go.AddComponent<GlitchFx>();

        var noiseGo = new GameObject("Noise", typeof(RectTransform), typeof(RawImage));
        noiseGo.transform.SetParent(rt, false);
        Stretch((RectTransform)noiseGo.transform);
        fx.noise = noiseGo.GetComponent<RawImage>();

        var barList = new List<Image>();
        for (int i = 0; i < 4; i++)
        {
            var barGo = new GameObject("Bar" + i, typeof(RectTransform), typeof(Image));
            var brt = (RectTransform)barGo.transform;
            brt.SetParent(rt, false);
            brt.anchorMin = new Vector2(0f, 0.5f);
            brt.anchorMax = new Vector2(1f, 0.5f);
            brt.sizeDelta = new Vector2(0f, 12f);
            var img = barGo.GetComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0.45f);
            img.raycastTarget = false;
            barList.Add(img);
        }
        fx.bars = barList.ToArray();

        foreach (var t in targets) if (t != null) fx.jitterTargets.Add(t);

        fx.Init();
        return fx;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    void Awake() => Init();

    void Init()
    {
        if (initialized || noise == null) return;
        initialized = true;

        tex = new Texture2D(noiseRes.x, noiseRes.y, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Repeat;
        px = new Color32[noiseRes.x * noiseRes.y];

        noise.texture = tex;
        noise.raycastTarget = false;
        SetAlpha(0f);
        noise.enabled = false;
        if (bars != null) foreach (var b in bars) if (b) b.enabled = false;
    }

    public IEnumerator Play(Action onPeak)
    {
        Init();
        homes.Clear();
        foreach (var t in jitterTargets) homes.Add(t != null ? t.anchoredPosition : Vector2.zero);
        noise.enabled = true;

        yield return Run(rampIn, k => k);        // 점점 지지직
        onPeak?.Invoke();                         // 최고조에서 배경 교체
        yield return Run(peakHold, k => 1f);
        yield return Run(rampOut, k => 1f - k);  // 잦아듦

        SetAlpha(0f);
        noise.enabled = false;
        if (bars != null) foreach (var b in bars) if (b) b.enabled = false;
        ResetTargets();
    }

    void ResetTargets()
    {
        for (int i = 0; i < jitterTargets.Count && i < homes.Count; i++)
            if (jitterTargets[i] != null) jitterTargets[i].anchoredPosition = homes[i];
    }

    void OnDisable() { if (homes.Count > 0) ResetTargets(); }

    IEnumerator Run(float dur, Func<float, float> intensity)
    {
        float t = 0f, next = 0f;
        while (t < dur)
        {
            float k = intensity(Mathf.Clamp01(t / dur));
            if (t >= next) { Step(k); next += 1f / noiseFps; }
            t += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    void Step(float k)
    {
        for (int i = 0; i < px.Length; i++)
        {
            byte v = (byte)UnityEngine.Random.Range(0, 256);
            px[i] = new Color32(v, v, v, 255);
        }
        tex.SetPixels32(px);
        tex.Apply(false);
        noise.uvRect = new Rect(UnityEngine.Random.value, UnityEngine.Random.value, 1f, 1f);
        SetAlpha(maxAlpha * k);

        for (int i = 0; i < jitterTargets.Count && i < homes.Count; i++)
        {
            if (jitterTargets[i] == null) continue;
            float x = Mathf.Round(UnityEngine.Random.Range(-1f, 1f) * jitterPx * k);
            jitterTargets[i].anchoredPosition = homes[i] + new Vector2(x, 0f);
        }

        if (bars == null) return;
        float halfH = ((RectTransform)transform).rect.height * 0.5f;
        foreach (var b in bars)
        {
            if (!b) continue;
            b.enabled = UnityEngine.Random.value < k;
            if (!b.enabled) continue;
            var rt = b.rectTransform;
            rt.anchoredPosition = new Vector2(UnityEngine.Random.Range(-40f, 40f) * k,
                                              UnityEngine.Random.Range(-halfH, halfH));
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, UnityEngine.Random.Range(4f, 40f));
        }
    }

    void SetAlpha(float a)
    {
        var c = noise.color;
        c.a = a;
        noise.color = c;
    }
}
