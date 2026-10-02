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
    }

    [SerializeField] private Fader fader;
    [SerializeField] private Route[] routes;
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
        
        if(AntCounter.instance != null)
            GameFlow.RecordTermites(GameFlow.CurrentStage, AntCounter.instance.Found);
        SaveManager.Save(force: true);
        
        StartCoroutine(Routine(r.nextStoryId));
    }

    IEnumerator Routine(string storyId)
    {
        if (fader != null) yield return fader.FadeOut();
        SceneRouter.Load(SceneRouter.StoryScene, storyId);
    }
}
