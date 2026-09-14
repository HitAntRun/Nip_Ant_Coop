using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private string startStage = "Prologue";
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private string progressFormat = "진행중인 챕터:{0}";
    [SerializeField] private GameObject continueButton;

    private void Start()
    {
        Time.timeScale = 1f;
    
        if(continueButton != null)
            continueButton.SetActive(SaveManager.HasSave);
        if (progressText != null)
        {
            progressText.gameObject.SetActive(SaveManager.HasSave);
            progressText.text = string.Format(progressFormat, GameFlow.ChapterLabel);
        }
            
    }

    public void OnClickContinue()
    {
        SceneRouter.Load(SceneRouter.StoryScene, GameFlow.CurrentStage);
    }

    public void OnClickNewGame()
    {
        SaveManager.StartNewGame(startStage);
        SceneRouter.Load(SceneRouter.StoryScene, startStage);
    }
    public void OnClickPlay()
    {
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
