using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;

public static class SaveStorage
{
    static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
    {
        Formatting = Formatting.Indented,
        NullValueHandling = NullValueHandling.Ignore,
        MissingMemberHandling = MissingMemberHandling.Ignore,
    };

    static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false);

    public static bool Exists(string path) => File.Exists(path) || File.Exists(path + ".bak");
    
    public static void Write<T>(string path, T data)
    {
        try
        {
            SavePaths.EnsureDirectories();

            string tmp = path + ".tmp";
            string bak = path + ".bak";

            File.WriteAllText(tmp, JsonConvert.SerializeObject(data, Settings), Utf8NoBom);

            if (File.Exists(path))
            {
                if (File.Exists(bak)) File.Delete(bak);
                File.Move(path, bak);
            }
            File.Move(tmp, path);
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveStorage] 저장 실패: {path}\n{e}");
        }
    }
    
    public static T Read<T>(string path) where T : class
    {
        var data = TryRead<T>(path);
        if (data != null) return data;

        var backup = TryRead<T>(path + ".bak");
        if (backup != null)
        {
            Debug.LogWarning($"[SaveStorage] 본 파일 손상. 백업에서 복구: {path}");
            return backup;
        }
        return null;
    }

    static T TryRead<T>(string path) where T : class
    {
        try
        {
            if (!File.Exists(path)) return null;
            string json = File.ReadAllText(path, Utf8NoBom);
            if (string.IsNullOrWhiteSpace(json)) return null;
            return JsonConvert.DeserializeObject<T>(json, Settings);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SaveStorage] 읽기 실패: {path}\n{e.Message}");
            return null;
        }
    }

    public static void Delete(string path)
    {
        foreach (var p in new[] { path, path + ".bak", path + ".tmp" })
        {
            try { if (File.Exists(p)) File.Delete(p); }
            catch (Exception e) { Debug.LogWarning($"[SaveStorage] 삭제 실패: {p}\n{e.Message}"); }
        }
    }
}