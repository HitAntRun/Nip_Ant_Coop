using System.Collections.Generic;
using System.Linq;

public static class GameFlow
{
    
    static Dictionary<string, int> termitesFound = new Dictionary<string, int>();

    public static int TotalTermitesFound => termitesFound.Values.Sum();
    
    static string currentStage = "Prologue";
    static int day = 1;
    private static string chapterLabel = "";

    public static string CurrentStage
    {
        get => currentStage;
        set => currentStage = value;
    }
    
    public static string LastStoryStage { get; set; } = "Prologue";

    static string chapterStoryId = "Prologue";
    public static int TermitesFoundIn(string stageId)
            => termitesFound.TryGetValue(stageId, out var n) ? n : 0;

    public static void RecordTermites(string stageId, int found)
    {
        termitesFound[stageId] = found;
        SaveManager.MarkDirty();
    }
    
    public static void SetTermites(Dictionary<string, int> src)
        => termitesFound = src != null ? new Dictionary<string, int>(src) : new Dictionary<string, int>();

    public static Dictionary<string, int> CopyTermites()
        => new Dictionary<string, int>(termitesFound);
    
    public static string ChapterStoryId
    {
        get => chapterStoryId;
        set { if (chapterStoryId == value) return; chapterStoryId = value; SaveManager.MarkDirty(); }
    }

    public static string ChapterLabel
    {
        get => chapterLabel;
        set
        {
            if (chapterLabel == value) return;
            chapterLabel = value;
            SaveManager.MarkDirty();
        }
    }

    public static int Day
    {
        get => day;
        set { if (day == value) return; day = value; SaveManager.MarkDirty(); }
    }
}
