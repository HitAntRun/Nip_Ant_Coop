using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 빛 모양 오버레이(main_theme_light_shade.png)를 잠깐 진하게 해서 빛이 깜빡이는 것처럼 보이게 한다.
/// Image 색은 어두운 색(예: #0B0705), 알파는 0으로 두고 시작.
/// </summary>
public class LightFlicker : MonoBehaviour
{
    [SerializeField] Graphic shade;
    [SerializeField] float period = 6f;
    [SerializeField] float jitter = 0.5f;       // 매번 ±jitter초 흔들기
    [SerializeField] float maxDark = 0.85f;
    [SerializeField] float offTime = 0.5f;     // 완전히 꺼져 있는 시간
    [SerializeField] bool stepped = true;      // true: 픽셀아트식 뚝뚝 끊기는 깜빡임, false: 부드러운 페이드
    [SerializeField] float firstDelay = -1f;   // 첫 깜빡임까지 시간 (음수면 period 사용) — 짧게 보이는 컷씬용

    Sequence seq;
    bool first;

    /// <summary>런타임에 코드로 붙일 때 사용 (비활성 상태에서 호출한 뒤 활성화).</summary>
    public void Setup(Graphic shade, float firstDelay, float period, float maxDark, float offTime)
    {
        this.shade = shade;
        this.firstDelay = firstDelay;
        this.period = period;
        this.maxDark = maxDark;
        this.offTime = offTime;
    }

    void Reset() => shade = GetComponent<Graphic>();

    void OnEnable()
    {
        SetAlpha(0f);
        first = true;
        Schedule();
    }

    void OnDisable() => seq?.Kill();

    void Schedule()
    {
        seq = DOTween.Sequence().SetUpdate(true).SetLink(gameObject)
            .AppendInterval(first && firstDelay >= 0f
                ? firstDelay
                : Mathf.Max(0.5f, period + Random.Range(-jitter, jitter)));
        first = false;

        if (stepped)
        {
            // 1) 전조: 짧게 두 번 끊김
            Step(maxDark * 0.6f, 0.08f);
            Step(0f,             0.10f);
            Step(maxDark * 0.7f, 0.06f);
            Step(0.1f,           0.12f);
            // 2) 확실히 꺼짐
            Step(maxDark,        offTime);
            // 3) 다시 켜지려다 실패하고 켜짐
            Step(maxDark * 0.4f, 0.07f);
            Step(maxDark,        0.15f);
            Step(maxDark * 0.3f, 0.08f);
            Step(maxDark * 0.6f, 0.06f);
            Step(0f, 0f);
        }
        else
        {
            seq.Append(shade.DOFade(maxDark, 0.04f))
               .Append(shade.DOFade(0.15f, 0.06f))
               .Append(shade.DOFade(maxDark * 0.8f, 0.05f))
               .Append(shade.DOFade(0f, 0.12f));
        }
        seq.OnComplete(Schedule);
    }

    void Step(float alpha, float hold)
    {
        seq.AppendCallback(() => SetAlpha(alpha));
        if (hold > 0f) seq.AppendInterval(hold);
    }

    void SetAlpha(float a)
    {
        var c = shade.color; c.a = a; shade.color = c;
    }
}
