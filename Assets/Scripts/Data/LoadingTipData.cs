using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using Random = UnityEngine.Random;

[CreateAssetMenu(fileName = "LoadingTips", menuName = "Tips/Loading Tips")]
public class LoadingTipData : ScriptableObject
{
    [System.Serializable]
    public class Tip
    {
        public LocalizedString text;
    }
    
    [SerializeField] private Tip[] tips;

    static int lastIndex = -1;

    public LocalizedString PickRandom(string targetScene)
    {
        if (tips == null || tips.Length == 0) return null;

        var pool = new List<int>();
        for (int i = 0; i < tips.Length; i++)
        {
            if (tips[i].text == null || tips[i].text.IsEmpty) continue;
            pool.Add(i);
        }

        if (pool.Count == 0) return null;
        if (pool.Count > 1) pool.Remove(lastIndex);

        lastIndex = pool[Random.Range(0, pool.Count)];
        return tips[lastIndex].text;
    }
}
