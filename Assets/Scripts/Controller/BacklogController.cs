using System.Collections;
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
    [SerializeField] private TMP_Text logText;
    [SerializeField] private ScrollRect scroll;

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

        var sb = new StringBuilder();
        string prevSpeaker = null;
        
        foreach (var l in runner.Log)
        {
            if (string.IsNullOrEmpty(l.speaker))
            {
                if (sb.Length > 0) sb.AppendLine();
                sb.AppendLine($"<i>{l.text}</i>");
                prevSpeaker = null;
                continue;
            }

            if (l.speaker != prevSpeaker)
            {
                if (sb.Length > 0) { sb.AppendLine(); }
                sb.AppendLine($"<color=#B5451B><b>{l.speaker}</b></color>");
                prevSpeaker = l.speaker;
            }
            else
            {
                sb.AppendLine();
            }
            sb.AppendLine(l.text);
        }
        if (logText != null) logText.text = sb.ToString().TrimEnd();

        IsOpen = true;
        if (overlay != null) overlay.SetActive(true);
        if (panel != null) panel.Open();

        StartCoroutine(ScrollToBottom());
    }
    
   IEnumerator ScrollToBottom()
    {
        yield return null;
        Canvas.ForceUpdateCanvases();
        if (scroll != null) scroll.verticalNormalizedPosition = 0f;
    }

    public void OnClickClose()
    {
        IsOpen = false;
        if (overlay != null) overlay.SetActive(false);
        if (panel != null) panel.Close();
    }
}
