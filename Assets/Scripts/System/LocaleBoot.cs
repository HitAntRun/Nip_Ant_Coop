using UnityEngine;
using UnityEngine.Localization.Settings;

public static class LocaleBoot
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Apply()
    {
        string code = PlayerPrefs.GetString(LocaleDropdown.PrefKey, "");
        if (string.IsNullOrEmpty(code)) return;

        var locale = LocalizationSettings.AvailableLocales.GetLocale(code);
        if (locale != null) LocalizationSettings.SelectedLocale = locale;
    }
}