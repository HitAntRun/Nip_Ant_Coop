using TMPro;
using UnityEngine;

public class AntCounter : MonoBehaviour
{
    public static AntCounter instance;

    [SerializeField] private TMP_Text countText;

    private int total;
    private int found;

    public int Found => found;
    public int Total => total;

    void Awake() { instance = this; }
    void OnDestroy() { if (instance == this) instance = null; }

    public void SetTotal(int t)
    {
        total = t;
        found = 0;
        UpdateUI();
    }

    public void AddFound()
    {
        found++;
        UpdateUI();
        
        string stage = GameFlow.CurrentStage;
        int others = GameFlow.TotalTermitesFound - GameFlow.TermitesFoundIn(stage);
        Debug.Log($"[Termite] {stage}: {found}/{total} | 누적 {others + found}");
        
        if (found >= total) OnAllFound();
    }

    void UpdateUI()
    {
        if (countText != null) countText.text = $"{found}/{total}";
    }

    void OnAllFound()
    {
        if (GameFlow.CurrentStage == "Tutorial") return;
        if (StageFlow.instance != null) StageFlow.instance.Finish();
    }
}