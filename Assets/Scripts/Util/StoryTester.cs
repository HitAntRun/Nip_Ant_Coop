using UnityEngine;

public class StoryTester : MonoBehaviour
{
    [SerializeField] private DialogueRunner runner;
    [SerializeField] private string overrideStoryId = ""; 
    [SerializeField] private int startFromLine = 1;

    static bool overrideUsed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => overrideUsed = false;

    void Start()
    {
        bool useOverride = !overrideUsed && !string.IsNullOrEmpty(overrideStoryId);
        string id = useOverride ? overrideStoryId : GameFlow.CurrentStage;
        overrideUsed = true;

        runner.Play(id, useOverride ? Mathf.Max(0, startFromLine - 1) : 0);
    }
}