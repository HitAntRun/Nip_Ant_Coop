using UnityEngine;

// 칭호 단계와 판정 규칙 — ProfilePanel과 EndingCredits가 같은 에셋을 공유
[CreateAssetMenu(menuName = "Data/TitleTable")]
public class TitleTable : ScriptableObject
{
    public TitleTier[] tiers;                    // 뉴비 → 감시자 → 매의 눈 → 신고왕 (낮은 순)
    public TitleTier hiddenTier;                 // 의심병자
    [Range(0f, 1f)] public float hiddenMaxAccuracy = 0.5f;

    // 정확도를 빼고 신고 수만으로 도달한 단계
    public int CountIndex(int found)
    {
        for (int i = tiers.Length - 1; i >= 0; i--)
            if (found >= tiers[i].minFound) return i;
        return 0;
    }

    // 판정 순서: 신고왕 → 의심병자 → 나머지
    public TitleTier CurrentTitle(int found, float acc)
    {
        int last = tiers.Length - 1;
        int countIdx = CountIndex(found);

        if (countIdx == last && acc >= tiers[last].minAccuracy)
            return tiers[last];

        if (hiddenTier != null && found >= hiddenTier.minFound && acc < hiddenMaxAccuracy)
            return hiddenTier;

        return tiers[Mathf.Min(countIdx, last - 1)];
    }

    public string Name(TitleTier t) => Loc.Get(Loc.UI, t.locKey, t.fallback);
}
