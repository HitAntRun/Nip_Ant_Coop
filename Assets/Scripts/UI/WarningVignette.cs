using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class WarningVignette : MonoBehaviour
{
    [SerializeField] private float minAlpha = 0.15f;
    [SerializeField] private float maxAlpha = 0.6f;
    [SerializeField] private float pulseDuration = 0.5f;
    
    [SerializeField] private float flashPeak = 0.6f;
    [SerializeField] private float flashDuration = 0.4f;

    public static WarningVignette instance;
    private Tween flash;

    private Image image;
    private Tween pulse;
    private bool on;

    void Awake()
    {
        instance = this;
        image = GetComponent<Image>();
        image.raycastTarget = false;
        SetAlpha(0f);
    }

    public void SetOn(bool value)
    {
        if (on == value) return;
        on = value;
        pulse?.Kill();
        flash?.Kill();

        if (on)
            StartPulse();
        else
            pulse = image.DOFade(0f, 0.2f);
    }

    public void SetUrgent(bool urgent)
    {
        if (pulse != null && on) pulse.timeScale = urgent ? 2f : 1f;
    }

    void SetAlpha(float a) { var c = image.color; c.a = a; image.color = c; }

    void StartPulse()
    {
        SetAlpha(minAlpha);
        pulse = image.DOFade(maxAlpha, pulseDuration)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);
    }

    public void Flash()
    {
        flash?.Kill();
        pulse?.Kill();
        SetAlpha(flashPeak);

        if (on)
            flash = image.DOFade(minAlpha, flashDuration).SetEase(Ease.OutQuad)
                .OnComplete(() => { if (on) StartPulse(); });
        else
            flash = image.DOFade(0f, flashDuration).SetEase(Ease.OutQuad);
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
        pulse?.Kill();
        flash?.Kill();
    }
}