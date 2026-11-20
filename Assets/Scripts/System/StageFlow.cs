using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StageFlow : MonoBehaviour
{
    public static StageFlow instance;

    [System.Serializable]
    public class Route
    {
        public string stageId;
        public string nextStoryId;
        public List<string> endLines = new List<string>();
    }

    [SerializeField] private Fader fader;
    [SerializeField] private Route[] routes;

    [Header("End Lines")]
    [SerializeField] private List<string> clearLines = new List<string>();
    [SerializeField] private List<string> missLines = new List<string>();
    [SerializeField] private float lineDuration = 1.5f;
    [SerializeField] private float delayBeforeFade = 1f;
    private bool ended;

    public bool IsEnded => ended;

    void Awake() { instance = this; }
    private void OnDestroy() { if(instance == this) instance = null; }

    public void Finish()
    {
        if (ended) return;

        var r = System.Array.Find(routes, x => x.stageId == GameFlow.CurrentStage);
        if (r == null || string.IsNullOrEmpty(r.nextStoryId))
            return;

        ended = true;

        if (AntCounter.instance != null)
        {
            GameFlow.RecordTermites(GameFlow.CurrentStage, AntCounter.instance.Found);
            GameFlow.RecordWrong(GameFlow.CurrentStage, AntCounter.instance.Wrong);
            GameFlow.RecordTotal(GameFlow.CurrentStage, AntCounter.instance.Total);
        }
            
        
        SaveManager.Save(force: true);
        
        StartCoroutine(Routine(r));
    }

    IEnumerator Routine(Route r)
    {
        bool hasRouteLines = r.endLines != null && r.endLines.Count > 0;
        bool allFound = IsAllFound();
        List<string> lines = hasRouteLines ? r.endLines : (allFound ? clearLines : missLines);
        bool positive = !hasRouteLines && allFound;

        if (InteractionPopup.instance != null && lines != null && lines.Count > 0)
        {
            string line = lines[UnityEngine.Random.Range(0, lines.Count)];
            InteractionPopup.instance.ShowHold(line, positive);
            yield return new WaitForSeconds(lineDuration);
            yield return new WaitForSeconds(delayBeforeFade);
        }

        if (fader != null) yield return fader.FadeOut();
        SceneRouter.Load(SceneRouter.StoryScene, r.nextStoryId);
    }

    bool IsAllFound()
    {
        var c = AntCounter.instance;
        return c != null && c.Total > 0 && c.Found >= c.Total;
    }
}
