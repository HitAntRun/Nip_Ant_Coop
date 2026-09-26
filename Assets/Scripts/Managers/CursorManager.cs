using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CursorManager : MonoBehaviour
{
    public static CursorManager instance { get; private set;}

    [Header("Cursor")]
    public Texture2D cursorTexture;
    public Vector2 hotspot = Vector2.zero;
    public CursorMode mode = CursorMode.Auto;

    [Header("Option")]
    public string[] hiddenScenes = { SceneRouter.IngameScene };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (instance != null) return;
        var prefab = Resources.Load<GameObject>("CursorManager");
        if (prefab == null) return;
        Instantiate(prefab);
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
        Apply(SceneManager.GetActiveScene().name);
    }

    private void OnDestroy()
    {
        if (instance == this) SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene s, LoadSceneMode m) => Apply(s.name);

    private void OnApplicationFocus(bool focus)
    {
        if (focus) Apply(SceneManager.GetActiveScene().name);
    }

    void Apply(string sceneName)
    {
        bool hide = System.Array.IndexOf(hiddenScenes, sceneName) >= 0;
        if (PauseMenu.instance != null && PauseMenu.instance.IsOpen) hide = false;
        SetHidden(hide);
    }

    public void SetHidden(bool hide)
    {
        if (hide)
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            Cursor.visible = false;
        }
        else
        {
            Cursor.SetCursor(cursorTexture, Vector2.zero, CursorMode.Auto);
            Cursor.visible = true;
        }
    }
}

