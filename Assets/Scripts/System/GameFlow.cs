public static class GameFlow
{
    static string currentStage = "Prologue";
    static string chapterLabel = "Prologue";
    static int day = 1;

    public static string CurrentStage
    {
        get => currentStage;
        set => currentStage = value;
    }
    
    public static string LastStoryStage { get; set; } = "Prologue";

    public static string ChapterLabel
    {
        get => chapterLabel;
        set { if (chapterLabel == value) return; chapterLabel = value; SaveManager.MarkDirty(); }
    }

    public static int Day
    {
        get => day;
        set { if (day == value) return; day = value; SaveManager.MarkDirty(); }
    }
}
