using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PauseMenu : MonoBehaviour
{
    public static PauseMenu instance;

    [SerializeField] private PanelPopup panel;
    
    public bool IsOpen { get; private set; }

    private void Awake()
    {
        instance = this;
        if(panel != null) panel.gameObject.SetActive(false);
    }

    private void OnDestroy() { if(instance == this) instance = null; }

    public void Toggle()
    {
        if (IsOpen) Close();
        else Open();
    }

    public void Open()
    {
        if (panel == null) return;
        IsOpen = true;
        panel.Open();
        Time.timeScale = 0f;
        CursorManager.instance?.SetHidden(false);
    }

    public void Close()
    {
        if (panel == null) return;
        IsOpen = false;
        panel.Close();
        Time.timeScale = 1f;
        CursorManager.instance?.SetHidden(true);
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
