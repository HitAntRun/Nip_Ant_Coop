public static class GameFlow
{
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
