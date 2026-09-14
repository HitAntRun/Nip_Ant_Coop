using System;

[Serializable]
public class ProgressData
{
    public const int CurrentVersion = 1;
    
    public int version = CurrentVersion;

    public string currentStage = "Prologue_1";
    public string chapterLabel = "Prologue";
    public int day = 1;

    public long savedAtUnix;
    public static ProgressData NewGame() => new ProgressData();
    
    public DateTime SavedAtLocal =>
        DateTimeOffset.FromUnixTimeSeconds(savedAtUnix).ToLocalTime().DateTime;
}