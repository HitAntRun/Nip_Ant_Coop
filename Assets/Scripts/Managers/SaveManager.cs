using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class SaveManager
{
    public static ProgressData Current { get; private set; }
    public static bool HasSave { get; private set; }

    private static bool dirty;

    public static void Load()
    {
        var data = SaveStorage.Read<ProgressData>(SavePaths.Progress);
        HasSave = data != null;

        if (data != null) data = Migrate(data);
        Current = data ?? ProgressData.NewGame();

        Apply(Current);
        dirty = false;
    }

    static ProgressData Migrate(ProgressData d)
    {
        d.version = ProgressData.CurrentVersion;
        return d;
    }

    static void Apply(ProgressData d)
    {
        GameFlow.CurrentStage = d.currentStage;
        GameFlow.ChapterLabel = d.chapterLabel;
        GameFlow.Day = d.day;
    }

    static void Collect(ProgressData d)
    {
        d.currentStage = GameFlow.CurrentStage;
        d.chapterLabel = GameFlow.ChapterLabel;
        d.day = GameFlow.Day;
        d.savedAtUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }

    public static void MarkDirty() => dirty = true;

    public static void Save(bool force = false)
    {
        if (!force && !dirty) return;
        
        Current ??= ProgressData.NewGame();
        Collect(Current);
        SaveStorage.Write(SavePaths.Progress, Current);

        HasSave = true;
        dirty = false;
    }

    public static void StartNewGame(string startStage)
    {
        Current = ProgressData.NewGame();
        Current.currentStage = startStage;
        Apply(Current);
        Save(force : true);
    }

    public static void DeleteSave()
    {
        SaveStorage.Delete(SavePaths.Progress);
        Current = ProgressData.NewGame();
        Apply(Current);
        HasSave = false;
        dirty = false;
    }
}
