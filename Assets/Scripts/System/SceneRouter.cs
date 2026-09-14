using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneRouter : MonoBehaviour
{
    public const string LoadingScene = "SC_Loading";
    public const string StoryScene = "SC_Story";
    public const string IngameScene = "SC_Ingame";
    
    public static string NextScene { get; private set; }
    public static float MinLoadingTime { get; private set; } = 1.8f;

    public static void Load(string sceneName, string stageId = null, float minTime = -1f)
    {
        if (!string.IsNullOrEmpty(stageId))
            GameFlow.CurrentStage = stageId;

        SaveManager.Save();
        
        NextScene = sceneName;
        if (minTime >= 0f) MinLoadingTime = minTime;

        SceneManager.LoadScene(LoadingScene);
    }
    public static void LoadDirect(string sceneName, string stageId = null)
    {
        if (!string.IsNullOrEmpty(stageId))
            GameFlow.CurrentStage = stageId;

        SaveManager.Save();

        NextScene = sceneName;
        SceneManager.LoadScene(sceneName);
    }
}
