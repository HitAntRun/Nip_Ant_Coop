using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 붙어 있는 Image의 sprite를 지켜보다가, 등록된 sprite가 보이면
/// 그 위에 개미 눈 깜빡임(EyeBlinker)과 빛 깜빡임(LightFlicker)을 자동으로 만들어 준다.
/// sprite가 바뀌거나 Image가 꺼지면 정리한다. (예: SC_Story의 Background에 붙여 End_B 엔딩 컷에 사용)
/// </summary>
[RequireComponent(typeof(Image))]
public class SpriteAmbience : MonoBehaviour
{
    [Serializable]
    public class Entry
    {
        public Sprite sprite;        // 이 sprite가 보일 때
        public TextAsset eyes;       // *_eyes.json (없으면 눈 깜빡임 없음)
        public Sprite lightShade;    // *_light_shade.png (없으면 빛 깜빡임 없음)
    }

    [SerializeField] Entry[] entries;

    [Header("Eyes")]
    [SerializeField] Vector2 blinkInterval = new(1.5f, 5f);

    [Header("Light")]
    [SerializeField] Color shadeColor = new Color32(0x0B, 0x07, 0x05, 0xFF);
    [SerializeField] float firstFlickerDelay = 1.2f;   // 컷이 짧아서 첫 깜빡임은 빨리
    [SerializeField] float flickerPeriod = 3.5f;
    [SerializeField] float maxDark = 0.85f;
    [SerializeField] float offTime = 0.5f;

    Image target;
    Sprite shownFor;
    GameObject root;

    void Awake() => target = GetComponent<Image>();

    void LateUpdate()
    {
        var s = target.enabled ? target.sprite : null;
        if (s != shownFor) Rebuild(s);
    }

    void OnDisable()
    {
        Clear();
        shownFor = null;
    }

    void Rebuild(Sprite s)
    {
        Clear();
        shownFor = s;

        Entry e = null;
        if (s != null && entries != null)
            foreach (var x in entries)
                if (x != null && x.sprite == s) { e = x; break; }
        if (e == null) return;

        // Image의 자식으로 만들어서 흔들림/글리치 등 배경 움직임을 그대로 따라가게 함
        root = new GameObject("Ambience", typeof(RectTransform));
        root.SetActive(false); // 설정을 끝낸 뒤 켜야 Awake/OnEnable이 올바른 값으로 돈다
        var rt = (RectTransform)root.transform;
        rt.SetParent(transform, false);
        Stretch(rt);
        root.AddComponent<Canvas>(); // 깜빡일 때 다른 UI까지 다시 그리지 않도록

        if (e.eyes != null)
        {
            var go = new GameObject("Eyes", typeof(RectTransform));
            var ert = (RectTransform)go.transform;
            ert.SetParent(rt, false);
            Stretch(ert);
            go.AddComponent<EyeBlinker>().Setup(e.eyes, blinkInterval);
        }

        if (e.lightShade != null)
        {
            var go = new GameObject("LightShade", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var lrt = (RectTransform)go.transform;
            lrt.SetParent(rt, false);
            Stretch(lrt);
            var img = go.GetComponent<Image>();
            img.sprite = e.lightShade;
            img.raycastTarget = false;
            img.color = new Color(shadeColor.r, shadeColor.g, shadeColor.b, 0f);
            go.AddComponent<LightFlicker>().Setup(img, firstFlickerDelay, flickerPeriod, maxDark, offTime);
        }

        root.SetActive(true);
    }

    void Clear()
    {
        if (root != null) Destroy(root);
        root = null;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}
