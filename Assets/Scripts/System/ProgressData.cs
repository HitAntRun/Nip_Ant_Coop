using System;
using System.Collections.Generic;

[Serializable]
public class ProgressData
{
    public const int CurrentVersion = 1;
    
    public int version = CurrentVersion;

    public string currentStage = "Prologue_1";
    public string chapterLabel = "";
    public string chapterStoryId;
    public int day = 1;

    public long savedAtUnix;
    public static ProgressData NewGame() => new ProgressData();
    
    public Dictionary<string, int> termitesFound = new Dictionary<string, int>();

    public DateTime SavedAtLocal =>
        DateTimeOffset.FromUnixTimeSeconds(savedAtUnix).ToLocalTime().DateTime;
}