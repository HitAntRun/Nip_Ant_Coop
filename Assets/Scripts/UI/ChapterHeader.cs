using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class ChapterHeader : MonoBehaviour
{
    public static ChapterHeader instance;
    private const string EmptyLocationKey = "loc_empty";

    [SerializeField] private TMP_Text chapterNumberText;
    [SerializeField] private TMP_Text locationText;
    [SerializeField] private TMP_Text dayText;
    [SerializeField] private string dayFormat = "개미력 {0}일";

    void Awake()
    {
        instance = this;
    }
    void OnLocaleChanged(Locale _) => Refresh();
    void OnEnable()  { LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged; }
    void OnDisable() { LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged; }

    private void OnDestroy()
    {
        if(instance == this) instance = null;
    }

    void Start() => Refresh();

    public void Refresh()
    {
        if (chapterNumberText != null)
            chapterNumberText.text = GameFlow.ChapterNumber.ToString("D2");
        if (locationText != null)
        {
            string key = GameFlow.Location;
            locationText.text = (key == EmptyLocationKey)
                ? ""
                : Loc.Get(Loc.UI, key, key);
        }
            
        if (dayText != null)
            dayText.text = string.Format(
                Loc.Get(Loc.UI, "ant_date", dayFormat), GameFlow.Day);
    }
}
