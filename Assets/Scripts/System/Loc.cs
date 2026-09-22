using UnityEngine.Localization.Settings;

public static class Loc
{
    public const string UI    = "UIDataTable";
    public const string Story = "StoryTable";

    public static string Get(string table, string key, string fallback = null)
    {
        if (string.IsNullOrEmpty(key)) return fallback;
        try
        {
            var t = LocalizationSettings.StringDatabase.GetTable(table);
            if (t == null) return fallback;
            var e = t.GetEntry(key);
            if (e == null) return fallback;
            var v = e.GetLocalizedString();
            return string.IsNullOrEmpty(v) ? fallback : v;
        }
        catch { return fallback; }
    }
}