using System.IO;
using UnityEngine;

public static class SavePaths
{
    public const string CloudFolder = "cloud";
    public const string LocalFolder = "local";

    public static string CloudDir => Path.Combine(Application.persistentDataPath, CloudFolder);
    public static string LocalDir => Path.Combine(Application.persistentDataPath, LocalFolder);

    public static string Progress => Path.Combine(CloudDir, "progress.json");

    public static void EnsureDirectories()
    {
        Directory.CreateDirectory(CloudDir);
        Directory.CreateDirectory(LocalDir);
    }
}
