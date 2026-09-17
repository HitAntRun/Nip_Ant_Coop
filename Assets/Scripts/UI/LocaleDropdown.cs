using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class LocaleDropdown : MonoBehaviour
{
    public const string PrefKey = "locale_code";

    [SerializeField] private TMP_Dropdown dropdown;

    private readonly List<Locale> locales = new List<Locale>();
    private bool ready;

    private void Awake()
    {
        if (dropdown == null) dropdown = GetComponent<TMP_Dropdown>();
    }

    private IEnumerator Start()
    {
        yield return LocalizationSettings.InitializationOperation;

        locales.Clear();
        locales.AddRange(LocalizationSettings.AvailableLocales.Locales);

        var options = new List<TMP_Dropdown.OptionData>();
        foreach (var l in locales)
            options.Add(new TMP_Dropdown.OptionData(DisplayName(l)));

        dropdown.options = options;
        Sync();

        ready = true;
        dropdown.onValueChanged.AddListener(OnChanged);
    }

    private void OnEnable()
    {
        if (ready) Sync();
    }

    private void OnDestroy()
    {
        if (dropdown != null) dropdown.onValueChanged.RemoveListener(OnChanged);
    }

    private void Sync()
    {
        int current = locales.IndexOf(LocalizationSettings.SelectedLocale);
        dropdown.SetValueWithoutNotify(Mathf.Max(0, current));
        dropdown.RefreshShownValue();
    }

    private void OnChanged(int index)
    {
        if (!ready || index < 0 || index >= locales.Count) return;

        var locale = locales[index];
        LocalizationSettings.SelectedLocale = locale;

        PlayerPrefs.SetString(PrefKey, locale.Identifier.Code);
        PlayerPrefs.Save();
    }

    private static string DisplayName(Locale l)
    {
        var ci = l.Identifier.CultureInfo;
        return ci != null ? ci.NativeName : l.LocaleName;
    }
}