using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameTimer : MonoBehaviour
{
    [SerializeField] private Slider slider;
    [SerializeField] private TMP_Text timeText;
    [SerializeField] private bool waitForIntro = true;
    [SerializeField] private GameObject warning;
    [SerializeField] private float warningThreshold = 20f;
    [SerializeField] private WarningVignette vignette;
    [SerializeField] private ArrowShake clockIdleShake;
    [SerializeField] private AlarmShake clockAlarmShake;
    
    [Header("Fill Sprites")]
    [SerializeField] private Sprite fillGreen;
    [SerializeField] private Sprite fillYellow;
    [SerializeField] private Sprite fillRed;
    [SerializeField] private float yellowAt = 40f;
    [SerializeField] private float redAt = 20f;

    private Image fillImage;

    private float totalTime = 60f;
    private float remaining;
    private bool running;
    private bool unlimited;

    void Start()
    {
        if (slider != null && slider.fillRect != null)
            fillImage = slider.fillRect.GetComponent<Image>();
        
        if (warning != null) warning.SetActive(false);
        unlimited = (GameFlow.CurrentStage == "Tutorial");

        if (unlimited)
        {
            if (slider != null) slider.value = 1f;
            if (timeText != null) timeText.text = "∞";
            running = false;
            return;
        }
        remaining = totalTime;
        running = !waitForIntro;
        UpdateUI();
    }

    void Update()
    {
        if (!running) return;
        
        remaining -= Time.deltaTime;

        if (remaining <= 0f)
        {
            remaining = 0f;
            running = false;
            OnTimeUp();
        }

        UpdateUI();
    }
    
    public void Begin()
    {
        if (!unlimited) running = true;
    }

    void UpdateUI()
    {
        if (slider != null) slider.value = remaining / totalTime;
        int min = Mathf.FloorToInt(remaining / 60f);
        int sec = Mathf.FloorToInt(remaining % 60f);
        timeText.text = string.Format("{0:00}:{1:00}", min, sec);
        
        bool show = !unlimited && remaining <= warningThreshold;
        SetClockAlarm(show);

        if (warning != null && warning.activeSelf != show) warning.SetActive(show);

        if (vignette != null)
        {
            vignette.SetOn(show);
            vignette.SetUrgent(remaining <= 5f);
        }
        
        Sprite target = remaining > yellowAt ? fillGreen
            : remaining > redAt    ? fillYellow
            : fillRed;
        SetFillSprite(target);
    }

    void OnTimeUp()
    {
        if (StageFlow.instance != null) StageFlow.instance.Finish();
    }
    
    void SetFillSprite(Sprite s)
    {
        if (fillImage != null && s != null && fillImage.sprite != s)
            fillImage.sprite = s;
    }
    
    void SetClockAlarm(bool alarm)
    {
        if (clockIdleShake == null || clockAlarmShake == null) return;
        if (clockAlarmShake.enabled == alarm) return;

        if (alarm)
        {
            clockIdleShake.enabled = false;
            clockAlarmShake.enabled = true;
        }
        else
        {
            clockAlarmShake.enabled = false;
            clockIdleShake.enabled = true;
        }
    }
}
