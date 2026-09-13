using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BacklogController : MonoBehaviour
{
    public static BacklogController instance;

    [SerializeField] private DialogueRunner runner;
    [SerializeField] private PanelPopup panel;
    [SerializeField] private GameObject overlay;
    [SerializeField] private ScrollRect scroll;
    [SerializeField] private RectTransform content;
    [SerializeField] private BacklogEntry entryPrefab;
    [SerializeField] private SpriteDatabase spriteDB;

    public readonly List<BacklogEntry> spawned = new List<BacklogEntry>();
    public bool IsOpen { get; private set; }
    
    void Awake() { instance = this; }

    private void OnDestroy()
    {
        if(instance == this)
            instance = null;
    }

    void Start()
    {
        if(panel != null) panel.gameObject.SetActive(false);
        if(overlay != null) overlay.SetActive(false);
    }

    public void OnClickOpen()
    {
        if (runner == null || IsOpen) return;

        Build();

        IsOpen = true;
        if (overlay != null) overlay.SetActive(true);
        if (panel != null) panel.Open();

        StartCoroutine(ScrollToBottom());
    }
    
    private void Build()
    {
        foreach (var e in spawned)
            if (e != null) Destroy(e.gameObject);
        spawned.Clear();

        string prevSpeaker = null;
        BacklogEntry cur = null;
        var sb = new StringBuilder();

        foreach (var l in runner.Log)
        {
            bool isNarration = string.IsNullOrEmpty(l.speaker);
            
            if (cur == null || isNarration || l.speaker != prevSpeaker)
            {
                if (cur != null) cur.SetBody(sb.ToString());
                sb.Clear();

                cur = Instantiate(entryPrefab, content);
                spawned.Add(cur);
                cur.SetSpeaker(l.speaker, spriteDB != null ? spriteDB.Get(l.portrait) : null);

                prevSpeaker = isNarration ? null : l.speaker;
            }

            if (sb.Length > 0) sb.Append("\n\n");
            sb.Append(l.text);
        }

        if (cur != null) cur.SetBody(sb.ToString());
    }
    
    IEnumerator ScrollToBottom()
    {
        yield return null;
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        yield return null;
        if (scroll != null) scroll.verticalNormalizedPosition = 0f;
    }

    public void OnClickClose()
    {
        IsOpen = false;
        if (overlay != null) overlay.SetActive(false);
        if (panel != null) panel.Close();
    }
}
