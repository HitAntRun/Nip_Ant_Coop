using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class ButtonAnim : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private Sprite[] frames = new Sprite[3];
    [SerializeField] private float frameTime = 0.04f;

    private Image image;
    private Button button;
    private Coroutine playing;
    private int current;
    private bool pressed;

    private void Awake()
    {
        image = GetComponent<Image>();
        TryGetComponent(out button);
        SetFrame(0);
    }

    public void OnPointerDown(PointerEventData e)
    {
        if (e.button != PointerEventData.InputButton.Left) return;
        if (button != null && !button.interactable) return;

        pressed = true;
        PlayTo(frames.Length - 1);
    }

    public void OnPointerUp(PointerEventData e)
    {
        if (e.button != PointerEventData.InputButton.Left) return;
        if (!pressed) return;

        pressed = false;
        PlayTo(0);
    }

    private void PlayTo(int target)
    {
        if (playing != null) StopCoroutine(playing);
        playing = StartCoroutine(StepTo(target));
    }

    private IEnumerator StepTo(int target)
    {
        int dir = target > current ? 1 : -1;
        while (current != target)
        {
            SetFrame(current + dir);
            if (current != target)
                yield return new WaitForSecondsRealtime(frameTime);
        }
        playing = null;
    }

    private void SetFrame(int i)
    {
        current = i;
        image.sprite = frames[i];
    }

    private void OnDisable()
    {
        pressed = false;
        playing = null;
        if (image != null) SetFrame(0);
    }
}