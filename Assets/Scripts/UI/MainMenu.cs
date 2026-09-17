using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MainMenu : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject playPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject confirmPanel;
    
    [Header("Play Panel")]
    [SerializeField] private GameObject continueButton;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private string progressFormat = "진행중인 챕터: {0}";
    
    [Header("Flow")]
    [SerializeField] private string startStage = "Prologue";

    [Header("Animation")]
    [SerializeField] private GameObject confirmRoot;
    [SerializeField] private PanelPopup confirmPopup;

    private bool HasProgress => SaveManager.HasSave && GameFlow.LastStoryStage != startStage;
    
    private void Start()
    {
        Time.timeScale = 1f;
        ShowMain();
    }
    
    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape)) return;

        if (confirmRoot != null && confirmRoot.activeSelf) { CloseConfirm(); return; }

        if (playPanel != null && playPanel.activeSelf)     { ShowMain(); return; }
        if (settingsPanel != null && settingsPanel.activeSelf) { ShowMain(); return; }
    }

    public void ShowOnly(GameObject target)
    {
        if (mainPanel != null) mainPanel.SetActive(mainPanel == target);
        if (playPanel != null) playPanel.SetActive(playPanel == target);
        if(settingsPanel != null) settingsPanel.SetActive(settingsPanel == target);
    }

    public void ShowMain()
    {
        ShowOnly(mainPanel);
    }
    
    public void ShowPlay()
    {
        ShowOnly(playPanel);
        RefreshPlayPanel();
    }

    public void ShowSettings()
    {
        ShowOnly(settingsPanel);
    }
    
    private void RefreshPlayPanel()
    {
        bool has = HasProgress;

        if (continueButton != null) continueButton.SetActive(has);

        if (progressText != null)
        {
            progressText.gameObject.SetActive(has);
            if (has) progressText.text = string.Format(progressFormat, GameFlow.ChapterLabel);
        }
    }
    
    public void OnClickContinue()
    {
        SceneRouter.Load(SceneRouter.StoryScene, GameFlow.LastStoryStage);
    }
    
    public void OnClickNewGame()
    {
        if (HasProgress && confirmRoot != null)
        {
            OpenConfirm();
            return;
        }
        StartNewGame();
    }

    public void OnConfirmYes() { CloseConfirm(); StartNewGame(); }
    public void OnConfirmNo()  { CloseConfirm(); }
    
    private void OpenConfirm()
    {
        if (confirmRoot != null) confirmRoot.SetActive(true);
        confirmPopup?.Open();
    }

    private void CloseConfirm()
    {
        confirmPopup?.Close();
        if (confirmRoot != null) confirmRoot.SetActive(false);
    }
    
    private void StartNewGame()
    {
        SaveManager.StartNewGame(startStage);
        SceneRouter.Load(SceneRouter.StoryScene, startStage);
    }

    public void OnClickQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
