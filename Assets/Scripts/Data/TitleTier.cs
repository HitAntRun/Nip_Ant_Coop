using UnityEngine;

[System.Serializable]
public class TitleTier
{
    public string locKey;
    public string fallback;
    public int minFound;
    public Color color = Color.black;
    [Range(0f, 1f)] public float minAccuracy;
}