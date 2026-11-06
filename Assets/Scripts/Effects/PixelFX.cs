using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PixelFX : MonoBehaviour
{
    public static PixelFX instance;

    [System.Serializable]
    public class Burst
    {
        [Header("Pixels")]
        public int count = 8;
        [Tooltip("아트 픽셀 단위 크기 (min, max)")]
        public Vector2 sizeRange = new Vector2(1, 2);
        [Tooltip("캔버스 단위/초")]
        public Vector2 speedRange = new Vector2(120, 260);
        public float upBias = 80f;
        public Vector2 lifeRange = new Vector2(0.25f, 0.45f);
        public float gravity = -900f;
        public float drag = 4f;
        public Color[] colors = { Color.white };

        [Header("Ring (점선 충격파)")]
        public int ringDots = 12;
        [Tooltip("아트 픽셀 단위 반지름 (start, end)")]
        public Vector2 ringRadius = new Vector2(3, 9);
        public float ringDuration = 0.22f;
        public Color ringColor = Color.white;

        [Header("Sparkle (십자 반짝이)")]
        public int sparkles = 0;
        public float sparkleRadius = 90f;
        public float sparkleLife = 0.5f;
        public float sparkleDelayMax = 0.35f;
        public Color sparkleColor = Color.white;
    }

    [Header("Common")]
    [Tooltip("아트 픽셀 1칸 = 캔버스 단위 (1080p 기준)")]
    [SerializeField] private float pixel = 4f;
    [Tooltip("픽셀 애니메이션 갱신 fps (0 = 매 프레임)")]
    [SerializeField] private float animFps = 20f;
    [SerializeField] private int sortingOrder = 500;
    [SerializeField] private int poolSize = 96;

    [Header("Presets")]
    public Burst click = DefaultClick();
    public Burst found = DefaultFound();
    public Burst wrong = DefaultWrong();

    enum Kind { Pixel, Ring, Sparkle }

    class P
    {
        public RectTransform rt;
        public Image img;
        public Kind kind;
        public Vector2 pos, vel, center;
        public float age, life, size, angle, r0, r1, delay, gravity, drag;
        public int index;
        public Color color;
    }

    readonly List<P> pool = new List<P>();
    readonly List<P> live = new List<P>();
    RectTransform root;
    Sprite square, plus3, plus5;
    float stepAcc;
    MagnifierController mag;


    void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;

        BuildCanvas();
        square = MakeSprite(new[] { "#" });
        plus3 = MakeSprite(new[] {
            ".#.",
            "###",
            ".#." });
        plus5 = MakeSprite(new[] {
            "..#..",
            "..#..",
            "##.##",
            "..#..",
            "..#.." });

        for (int i = 0; i < poolSize; i++) pool.Add(CreateP());
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }


    public void PlayClick(Vector2 screenPos) => Play(click, screenPos);
    public void PlayFound(Vector2 screenPos) => Play(found, screenPos);

    public void PlayFoundAtWorld(Vector3 worldPos) => PlayFound(WorldToFxScreen(worldPos));

    public void PlayWrong(Vector2 screenPos) => Play(wrong, screenPos);
    public void PlayWrongAtWorld(Vector3 worldPos) => PlayWrong(WorldToFxScreen(worldPos));


    void Play(Burst b, Vector2 screenPos)
    {
        if (b == null || root == null) return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screenPos, null, out Vector2 c);

        for (int i = 0; i < b.count; i++)
        {
            float a = Random.Range(0f, Mathf.PI * 2f);
            var p = Spawn(Kind.Pixel, square, Pick(b.colors));
            p.pos = c;
            p.vel = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * Random.Range(b.speedRange.x, b.speedRange.y)
                    + Vector2.up * b.upBias;
            p.life = Random.Range(b.lifeRange.x, b.lifeRange.y);
            p.size = Mathf.Max(1f, Mathf.Round(Random.Range(b.sizeRange.x, b.sizeRange.y)));
            p.gravity = b.gravity;
            p.drag = b.drag;
        }

        for (int i = 0; i < b.ringDots; i++)
        {
            var p = Spawn(Kind.Ring, square, b.ringColor);
            p.center = c;
            p.index = i;
            p.angle = i * Mathf.PI * 2f / b.ringDots;
            p.r0 = b.ringRadius.x * pixel;
            p.r1 = b.ringRadius.y * pixel;
            p.life = b.ringDuration;
            p.size = 1f;
        }

        for (int i = 0; i < b.sparkles; i++)
        {
            var p = Spawn(Kind.Sparkle, plus3, b.sparkleColor);
            p.pos = c + Random.insideUnitCircle * b.sparkleRadius;
            p.delay = Random.Range(0f, b.sparkleDelayMax);
            p.life = b.sparkleLife;
            p.img.enabled = p.delay <= 0f;
        }
        stepAcc = 999f;
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;

        bool draw = true;
        if (animFps > 0f)
        {
            float step = 1f / animFps;
            stepAcc += dt;
            draw = stepAcc >= step;
            if (draw) stepAcc = stepAcc >= 1f ? 0f : stepAcc % step;
        }

        for (int i = live.Count - 1; i >= 0; i--)
        {
            var p = live[i];

            if (p.delay > 0f) { p.delay -= dt; continue; }

            p.age += dt;
            float t = p.age / p.life;
            if (t >= 1f) { Release(p); live.RemoveAt(i); continue; }

            if (p.kind == Kind.Pixel)
            {
                p.vel *= 1f / (1f + p.drag * dt);
                p.vel.y += p.gravity * dt;
                p.pos += p.vel * dt;
            }

            if (draw) Apply(p, t);
        }
    }

    void Apply(P p, float t)
    {
        p.img.enabled = true;

        switch (p.kind)
        {
            case Kind.Pixel:
            {
                float s = t > 0.6f ? Mathf.Max(1f, p.size - 1f) : p.size;
                p.rt.sizeDelta = Vector2.one * s * pixel;
                p.rt.anchoredPosition = Snap(p.pos);
                break;
            }
            case Kind.Ring:
            {
                float e = 1f - (1f - t) * (1f - t);
                float r = Mathf.Lerp(p.r0, p.r1, e);
                Vector2 pos = p.center + new Vector2(Mathf.Cos(p.angle), Mathf.Sin(p.angle)) * r;
                p.rt.sizeDelta = Vector2.one * pixel;
                p.rt.anchoredPosition = Snap(pos);
                if (t > 0.6f && (p.index % 2 == 1)) p.img.enabled = false;
                break;
            }
            case Kind.Sparkle:
            {
                bool big = t > 0.25f && t < 0.7f;
                p.img.sprite = big ? plus5 : plus3;
                p.rt.sizeDelta = Vector2.one * (big ? 5f : 3f) * pixel;
                p.rt.anchoredPosition = Snap(p.pos);
                break;
            }
        }
    }


    Vector2 WorldToFxScreen(Vector3 world)
    {
        if (mag == null) mag = FindFirstObjectByType<MagnifierController>();
        Camera cam = (mag != null && mag.mainCam != null) ? mag.mainCam : Camera.main;
        if (cam == null) return Input.mousePosition;

        Vector2 sp = cam.WorldToScreenPoint(world);

        if (mag != null && mag.IsSearchMode && mag.magnifierRoot != null)
        {
            Vector2 c = mag.magnifierRoot.position;
            sp = c + (sp - c) * mag.searchMagnification;
        }
        return sp;
    }

    Vector2 Snap(Vector2 v) =>
        new Vector2(Mathf.Round(v.x / pixel) * pixel, Mathf.Round(v.y / pixel) * pixel);

    static Color Pick(Color[] cs) =>
        (cs == null || cs.Length == 0) ? Color.white : cs[Random.Range(0, cs.Length)];

    P Spawn(Kind kind, Sprite sprite, Color color)
    {
        P p;
        if (pool.Count > 0) { p = pool[pool.Count - 1]; pool.RemoveAt(pool.Count - 1); }
        else p = CreateP();

        p.kind = kind;
        p.age = 0f; p.delay = 0f; p.vel = Vector2.zero;
        p.color = color;
        p.img.sprite = sprite;
        p.img.color = color;
        p.img.enabled = false;
        p.rt.gameObject.SetActive(true);
        live.Add(p);
        return p;
    }

    void Release(P p)
    {
        p.rt.gameObject.SetActive(false);
        pool.Add(p);
    }

    P CreateP()
    {
        var go = new GameObject("px", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(root, false);
        var p = new P { rt = (RectTransform)go.transform, img = go.GetComponent<Image>() };
        p.img.raycastTarget = false;
        go.SetActive(false);
        return p;
    }

    void BuildCanvas()
    {
        var go = new GameObject("PixelFX_Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        go.transform.SetParent(transform, false);

        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        root = (RectTransform)go.transform;
    }

    static Sprite MakeSprite(string[] rows)
    {
        int h = rows.Length, w = rows[0].Length;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                tex.SetPixel(x, h - 1 - y, rows[y][x] == '#' ? Color.white : Color.clear);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 1f);
    }


    static Burst DefaultClick() => new Burst
    {
        count = 7,
        sizeRange = new Vector2(1, 2),
        speedRange = new Vector2(120, 260),
        upBias = 80f,
        lifeRange = new Vector2(0.25f, 0.45f),
        gravity = -900f,
        drag = 4f,
        colors = new Color[]
        {
            new Color32(0xE8, 0xD3, 0xA2, 0xFF),
            new Color32(0xC9, 0xA4, 0x6B, 0xFF),
            new Color32(0x8E, 0x64, 0x40, 0xFF),
        },
        ringDots = 12,
        ringRadius = new Vector2(3, 9),
        ringDuration = 0.22f,
        ringColor = new Color32(0xFF, 0xF6, 0xDD, 0xE6),
        sparkles = 0,
    };

    static Burst DefaultFound() => new Burst
    {
        count = 16,
        sizeRange = new Vector2(1, 3),
        speedRange = new Vector2(250, 520),
        upBias = 60f,
        lifeRange = new Vector2(0.4f, 0.7f),
        gravity = -600f,
        drag = 3f,
        colors = new Color[]
        {
            new Color32(0xFF, 0xF3, 0xB0, 0xFF),
            new Color32(0xFF, 0xD4, 0x47, 0xFF),
            new Color32(0xFF, 0x9B, 0x3D, 0xFF),
            Color.white,
        },
        ringDots = 20,
        ringRadius = new Vector2(4, 22),
        ringDuration = 0.4f,
        ringColor = new Color32(0xFF, 0xE6, 0x80, 0xFF),
        sparkles = 5,
        sparkleRadius = 90f,
        sparkleLife = 0.5f,
        sparkleDelayMax = 0.35f,
        sparkleColor = Color.white,
    };

    static Burst DefaultWrong() => new Burst
    {
        count = 6,
        sizeRange = new Vector2(1, 2),
        speedRange = new Vector2(60, 140),
        upBias = 40f,
        lifeRange = new Vector2(0.3f, 0.5f),
        gravity = -700f,
        drag = 5f,
        colors = new Color[]
        {
            new Color32(0x3A, 0x2E, 0x2A, 0xFF),
            new Color32(0x5C, 0x4A, 0x42, 0xFF),
            new Color32(0x8A, 0x84, 0x80, 0xFF),
        },
        ringDots = 0,
        sparkles = 0,
    };
}
