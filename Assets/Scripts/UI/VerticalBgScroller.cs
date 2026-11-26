using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class VerticalBgScroller : MonoBehaviour
{
    [SerializeField] private RectTransform content;
    [SerializeField] private float speed = 400f;      // 초당 이동량 (1920x1080 기준 px)
    [SerializeField] private float startDelay = 0.8f; // 페이드인 후 출발 전 대기
    [SerializeField] private float endHold = 0.8f;    // 맨 위 도착 후 대기
    [SerializeField] private AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private bool pixelSnap = true;   // 픽셀아트 떨림 방지

    void Awake() => SetY(0f);   // 처음엔 맨 아래가 보이게

    public IEnumerator Play()
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        float viewH = ((RectTransform)transform).rect.height;
        float distance = Mathf.Max(0f, content.rect.height - viewH);
        float duration = distance / Mathf.Max(1f, speed);

        SetY(0f);
        if (startDelay > 0f) yield return new WaitForSeconds(startDelay);

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            SetY(-distance * ease.Evaluate(Mathf.Clamp01(t / duration)));
            yield return null;
        }
        SetY(-distance);
        if (endHold > 0f) yield return new WaitForSeconds(endHold);
    }

    void SetY(float y)
    {
        if (pixelSnap) y = Mathf.Round(y);
        content.anchoredPosition = new Vector2(content.anchoredPosition.x, y);
    }
}