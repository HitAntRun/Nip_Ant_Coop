using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ButtonClickSfx : MonoBehaviour
{
    [SerializeField] private string sfxId = "story_click";

    private Button button;

    void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(Play);
    }

    void OnDestroy()
    {
        if (button != null) button.onClick.RemoveListener(Play);
    }

    void Play() => SoundManager.instance?.PlayUiSfx(sfxId);
}