using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class AntCounter : MonoBehaviour
{
    public static AntCounter instance;

    [SerializeField] private Transform iconContainer;
    [SerializeField] private GameObject iconPrefab;
    [SerializeField] private string crossChildName = "Cross";
    
    [SerializeField] private float stampStartScale = 2.2f;
    [SerializeField] private float stampStartAngle = -25f;
    [SerializeField] private float stampDuration = 0.25f;
    [SerializeField] private float iconShakeStrength = 8f;
    [SerializeField] private Color foundIconTint = new Color(0.6f, 0.6f, 0.6f, 1f);
    
    private readonly List<GameObject> crosses = new List<GameObject>();
    
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
        BuildIcons();
        UpdateUI();
    }

    public void AddFound()
    {
        found++;
        UpdateUI();
        PlayStamp(found - 1);
        
        string stage = GameFlow.CurrentStage;
        int others = GameFlow.TotalTermitesFound - GameFlow.TermitesFoundIn(stage);
        Debug.Log($"[Termite] {stage}: {found}/{total} | 누적 {others + found}");
        
        if (found >= total) OnAllFound();
    }

    void BuildIcons()
    {
        for(int i = iconContainer.childCount - 1; i >= 0; i--)
            Destroy(iconContainer.GetChild(i).gameObject);
        crosses.Clear();

        for (int i = 0; i < total; i++)
        {
            GameObject icon = Instantiate(iconPrefab, iconContainer);
            Transform cross = icon.transform.Find(crossChildName);
            if (cross != null)
            {
                cross.gameObject.SetActive(false);
                crosses.Add(cross.gameObject);
            }
        }
    }
    void UpdateUI()
    {
        for (int i = 0; i < crosses.Count; i++)
            crosses[i].SetActive(i < found);
    }

    void OnAllFound()
    {
        if (GameFlow.CurrentStage == "Tutorial") return;
        if (StageFlow.instance != null) StageFlow.instance.Finish();
    }
    
    void PlayStamp(int index)
    {
        if (index < 0 || index >= crosses.Count) return;

        GameObject cross = crosses[index];
        RectTransform crossRect = (RectTransform)cross.transform;
        Image crossImg = cross.GetComponent<Image>();
        RectTransform iconRect = (RectTransform)cross.transform.parent;
        Image iconImg = iconRect.GetComponent<Image>();

        cross.SetActive(true);
        crossRect.DOKill();
        iconRect.DOKill();

        crossRect.localScale = Vector3.one * stampStartScale;
        crossRect.localEulerAngles = new Vector3(0, 0, stampStartAngle);
        if (crossImg != null) { crossImg.DOKill(); crossImg.color = new Color(1, 1, 1, 0); }

        Sequence seq = DOTween.Sequence().SetLink(cross);
        seq.Append(crossRect.DOScale(1f, stampDuration).SetEase(Ease.InBack));
        seq.Join(crossRect.DORotate(Vector3.zero, stampDuration).SetEase(Ease.OutQuad));
        if (crossImg != null) seq.Join(crossImg.DOFade(1f, stampDuration * 0.5f));

        seq.AppendCallback(() =>
        {
            iconRect.DOShakeAnchorPos(0.2f, iconShakeStrength, 20).SetLink(iconRect.gameObject);
            if (iconImg != null) iconImg.DOColor(foundIconTint, 0.15f).SetLink(iconImg.gameObject);
        });
        seq.Append(crossRect.DOPunchScale(Vector3.one * 0.15f, 0.2f, 6, 0.5f));
    }
}