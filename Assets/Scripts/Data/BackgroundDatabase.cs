using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Dialogue/Background Database")]
public class BackgroundDatabase : ScriptableObject
{
    [System.Serializable]
    public class Entry
    {
        public string name;
        public GameObject prefab;
    }

    public List<Entry> entries = new List<Entry>();
    private Dictionary<string, GameObject> map;

    public GameObject Get(string key)
    {
        if (string.IsNullOrEmpty(key)) return null;
        if (map == null)
        {
            map = new Dictionary<string, GameObject>();
            foreach (var e in entries)
                if (!string.IsNullOrEmpty(e.name)) map[e.name] = e.prefab;
        }

        return map.TryGetValue(key, out var p) ? p : null;
    }
}