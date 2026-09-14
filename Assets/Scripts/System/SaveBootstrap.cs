using UnityEngine;

public class SaveBootstrap : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        SaveManager.Load();

        var go = new GameObject("[SaveBootstrap]");
        go.AddComponent<SaveBootstrap>();
        DontDestroyOnLoad(go);
    }

    void OnApplicationQuit() => SaveManager.Save();
    void OnApplicationPause(bool paused) { if (paused) SaveManager.Save(); }
    void OnApplicationFocus(bool focused) { if (!focused) SaveManager.Save(); }
}