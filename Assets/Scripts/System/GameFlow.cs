using System.Collections.Generic;
using System.Linq;

public static class GameFlow
{
    
    static Dictionary<string, int> termitesFound = new Dictionary<string, int>();
    private static int chapterNumber = 0;
    private static string location = "";

    public static int TotalTermitesFound => termitesFound.Values.Sum();
    
    static string currentStage = "Prologue";
    static int day = 1;

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

    public static int ChapterNumber
    {
        get => chapterNumber;
        set
        {
            if (chapterNumber == value) return;
            chapterNumber = value;
            SaveManager.MarkDirty();
        }
    }

    public static string Location
    {
        get => location;
        set
        {
            if (location == value) return;
            location = value;
            SaveManager.MarkDirty();
        }
    }

    public static int Day
    {
        get => day;
        set { if (day == value) return; day = value; SaveManager.MarkDirty(); }
    }
}
