using System.Collections;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

public class LanguageSelector : MonoBehaviour
{
    [System.Serializable]
    public class Entry
    {
        public string localeCode;
        public Button button;
        public ButtonHover hover;
    }

    [SerializeField] private Entry[] entries;

    private bool started;

    private IEnumerator Start()
    {
        yield return LocalizationSettings.InitializationOperation;

        foreach (var e in entries)
        {
            if (e?.button == null) continue;
            string code = e.localeCode;
            e.button.onClick.AddListener(() => Select(code));
        }

        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
        started = true;
        Refresh();
    }

    private void OnEnable()
    {
        if (started) Refresh();
    }

    private void OnDestroy()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;

        foreach (var e in entries)
            if (e?.button != null) e.button.onClick.RemoveAllListeners();
    }

    private void OnLocaleChanged(Locale _) => Refresh();

    private void Select(string code)
    {
        var locale = LocalizationSettings.AvailableLocales.GetLocale(code);
        if (locale == null)
        {
            Debug.LogWarning($"[LanguageSelector] 로케일 없음: {code}");
            return;
        }
        if (LocalizationSettings.SelectedLocale == locale) return;

        LocalizationSettings.SelectedLocale = locale;

        PlayerPrefs.SetString(LocaleBoot.PrefKey, code);
        PlayerPrefs.Save();
    }

    private void Refresh()
    {
        var current = LocalizationSettings.SelectedLocale;
        string code = current != null ? current.Identifier.Code : "";

        foreach (var e in entries)
        {
            if (e?.hover == null) continue;
            e.hover.SetSelected(e.localeCode == code);
        }
    }
}