using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class WarningVignette : MonoBehaviour
{
    [SerializeField] private float minAlpha = 0.15f;
    [SerializeField] private float maxAlpha = 0.6f;
    [SerializeField] private float pulseDuration = 0.5f;

    private Image image;
    private Tween pulse;
    private bool on;

    void Awake()
    {
        image = GetComponent<Image>();
        image.raycastTarget = false;
        SetAlpha(0f);
    }

    public void SetOn(bool value)
    {
        if (on == value) return;
        on = value;
        pulse?.Kill();

        if (on)
        {
            SetAlpha(minAlpha);
            pulse = image.DOFade(maxAlpha, pulseDuration)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
        }
        else
        {
            pulse = image.DOFade(0f, 0.2f);
        }
    }

    public void SetUrgent(bool urgent)
    {
        if (pulse != null && on) pulse.timeScale = urgent ? 2f : 1f;
    }

    void SetAlpha(float a) { var c = image.color; c.a = a; image.color = c; }

    void OnDestroy() => pulse?.Kill();
}