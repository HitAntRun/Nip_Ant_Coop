using TMPro;
using UnityEngine.UI;
using UnityEngine;

public class BacklogEntry : MonoBehaviour
{
    [SerializeField] private GameObject header;
    [SerializeField] private Image portrait;
    [SerializeField] private TMP_Text speakerName;
    [SerializeField] private TMP_Text body;

    public void SetSpeaker(string speaker, Sprite portraitSprite)
    {
        bool isNarration = string.IsNullOrEmpty(speaker);
        header.SetActive(!isNarration);

        if (isNarration)
        {
            body.fontStyle = FontStyles.Italic;
            return;
        }
        
        body.fontStyle = FontStyles.Normal;
        speakerName.text = speaker;

        if (portraitSprite != null)
        {
            portrait.sprite = portraitSprite;
            portrait.enabled = true;
        }
        else
        {
            portrait.enabled = false;
        }
    }
    
    public void SetBody(string text) => body.text = text;
}
