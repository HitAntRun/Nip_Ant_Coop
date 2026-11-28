using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// main_theme.png 위에 개미 눈 덮개를 자동 생성하고 랜덤하게 깜빡이게 한다.
/// 이 컴포넌트를 Background와 똑같이 화면 전체로 늘린 RectTransform(EyeLayer)에 붙인다.
/// </summary>
public class EyeBlinker : MonoBehaviour
{
    [System.Serializable] class EyeData { public int group, x, y, w, h; public string color; }
    [System.Serializable] class EyeFile { public int imageWidth, imageHeight; public EyeData[] eyes; }

    [SerializeField] TextAsset eyeJson;                    // main_theme_eyes.json
    [SerializeField] Vector2 interval = new(2f, 7f);       // 한 개미가 다시 깜빡이기까지 걸리는 시간 범위
    [SerializeField] float closedTime = 0.1f;              // 완전히 감은 시간
    [SerializeField] float halfTime = 0.04f;               // 반쯤 감은 프레임 시간 (0이면 생략)
    [SerializeField, Range(0, 1)] float doubleBlinkChance = 0.2f;

    readonly List<List<Image>> groups = new();

    /// <summary>런타임에 코드로 붙일 때 사용 (비활성 상태에서 호출한 뒤 활성화).</summary>
    public void Setup(TextAsset json, Vector2 interval)
    {
        eyeJson = json;
        this.interval = interval;
    }

    void Awake()
    {
        if (eyeJson == null) { Debug.LogError("[EyeBlinker] eyeJson이 비어 있어요.", this); return; }

        var data = JsonUtility.FromJson<EyeFile>(eyeJson.text);
        float W = data.imageWidth, H = data.imageHeight;
        var byGroup = new Dictionary<int, List<Image>>();

        foreach (var e in data.eyes)
        {
            var go = new GameObject($"Eye_{e.group}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(transform, false);
            rt.pivot = new Vector2(0.5f, 1f); // 위에서 아래로 감기도록 위쪽 기준
            // 픽셀 좌표(왼쪽 위 원점) → 정규화 앵커. 배경과 같이 늘어나므로 해상도가 바뀌어도 맞는다.
            rt.anchorMin = new Vector2(e.x / W, 1f - (e.y + e.h) / H);
            rt.anchorMax = new Vector2((e.x + e.w) / W, 1f - e.y / H);
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            var img = go.GetComponent<Image>();   // 스프라이트 없이 단색 사각형
            ColorUtility.TryParseHtmlString(e.color, out var c);
            img.color = c;
            img.raycastTarget = false;
            img.enabled = false;

            if (!byGroup.TryGetValue(e.group, out var list)) byGroup[e.group] = list = new List<Image>();
            list.Add(img);
        }
        groups.AddRange(byGroup.Values);
    }

    void OnEnable()
    {
        foreach (var g in groups) StartCoroutine(Loop(g));
    }

    IEnumerator Loop(List<Image> eyes)
    {
        Set(eyes, 0f);
        yield return new WaitForSecondsRealtime(Random.Range(0f, interval.y)); // 시작 타이밍 분산
        while (true)
        {
            yield return Blink(eyes);
            if (Random.value < doubleBlinkChance)
            {
                yield return new WaitForSecondsRealtime(0.12f);
                yield return Blink(eyes);
            }
            yield return new WaitForSecondsRealtime(Random.Range(interval.x, interval.y));
        }
    }

    IEnumerator Blink(List<Image> eyes)
    {
        if (halfTime > 0f) { Set(eyes, 0.5f); yield return new WaitForSecondsRealtime(halfTime); }
        Set(eyes, 1f); yield return new WaitForSecondsRealtime(closedTime);
        if (halfTime > 0f) { Set(eyes, 0.5f); yield return new WaitForSecondsRealtime(halfTime); }
        Set(eyes, 0f);
    }

    static void Set(List<Image> eyes, float closed)
    {
        foreach (var img in eyes)
        {
            img.enabled = closed > 0f;
            if (closed > 0f) img.rectTransform.localScale = new Vector3(1f, closed, 1f);
        }
    }
}
