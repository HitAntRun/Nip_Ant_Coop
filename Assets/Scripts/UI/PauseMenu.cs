using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    public static PauseMenu instance;

    [SerializeField] private PanelPopup panel;
    [SerializeField] private GameObject mainPage;
    [SerializeField] private GameObject optionPage;
    [SerializeField] private string mainMenuScene = "SC_Main";
    
    public bool IsOpen { get; private set; }

    private void Awake()
    {
        instance = this;
        if(panel != null) panel.gameObject.SetActive(false);
    }

    private void OnDestroy() { if(instance == this) instance = null; }

    public void Toggle()
    {
        if (!IsOpen)
        {
            Open();
            return;
        }

        if (optionPage != null && optionPage.activeSelf)
        {
            ShowMain();
            return;
        }
        Close();
    }

    public void Open()
    {
        if (panel == null) return;
        ShowMain();
        IsOpen = true;
        panel.Open();
        Time.timeScale = 0f;
        CursorManager.instance?.SetHidden(false);
    }

    public void ShowMain()
    {
        if(optionPage) optionPage.SetActive(false);
        if(mainPage) mainPage.SetActive(true);
    }

    public void ShowOption()
    {
        if (mainPage) mainPage.SetActive(false);
        if (optionPage) optionPage.SetActive(true);
    }

    public void RestartStage()
    {
        Time.timeScale = 1f;
        IsOpen = false;
        SceneRouter.Load(SceneManager.GetActiveScene().name);
    }

    public void GoToMain()
    {
        Time.timeScale = 1f;
        IsOpen = false;
        SceneRouter.Load(mainMenuScene);
    }

    public void Close()
    {
        if (panel == null) return;
        IsOpen = false;
        panel.Close();
        Time.timeScale = 1f;
        CursorManager.instance?.Refresh();
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
