using UnityEngine;
using UnityEngine.UI;

public class OptionPanel : MonoBehaviour
{
    [Header("Volume")]
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;

    [Header("Mute")]
    [SerializeField] private Toggle muteToggle;
    [SerializeField] private Image muteImage;
    [SerializeField] private Sprite soundOnSprite;
    [SerializeField] private Sprite soundOffSprite;
    [SerializeField] private CanvasGroup volumeGroup;

    private void Awake()
    {
        bgmSlider.onValueChanged.AddListener(OnBgmChanged);
        sfxSlider.onValueChanged.AddListener(OnSfxChanged);
        muteToggle.onValueChanged.AddListener(OnMuteChanged);
    }

    private void OnDestroy()
    {
        bgmSlider.onValueChanged.RemoveListener(OnBgmChanged);
        sfxSlider.onValueChanged.RemoveListener(OnSfxChanged);
        muteToggle.onValueChanged.RemoveListener(OnMuteChanged);
    }

    private void OnEnable()
    {
        var sm = SoundManager.instance;
        if (sm == null) return;

        bgmSlider.SetValueWithoutNotify(sm.GetBgmVolume() * bgmSlider.maxValue);
        sfxSlider.SetValueWithoutNotify(sm.GetSfxVolume() * sfxSlider.maxValue);
        muteToggle.SetIsOnWithoutNotify(sm.IsMuted);
        RefreshMuteVisual(sm.IsMuted);
    }

    private void OnDisable() => PlayerPrefs.Save();

    private void OnBgmChanged(float v)
    {
        SoundManager.instance?.SetBgmVolume(v / bgmSlider.maxValue);
    }

    private void OnSfxChanged(float v)
    {
        SoundManager.instance?.SetSfxVolume(v / sfxSlider.maxValue);
    }

    private void OnMuteChanged(bool muted)
    {
        SoundManager.instance?.SetMuted(muted);
        RefreshMuteVisual(muted);
    }

    private void RefreshMuteVisual(bool muted)
    {
        if (muteImage != null)
            muteImage.sprite = muted ? soundOffSprite : soundOnSprite;
        if (volumeGroup != null)
            volumeGroup.alpha = muted ? 0.5f : 1f;
    }
}