using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Fader : MonoBehaviour
{
    [SerializeField] private Image fadeOverlay;
    [SerializeField] private float fadeDuration = 1f;
    [SerializeField] private bool fadeInOnStart = false;

    public IEnumerator FadeIn()  => Fade(1f, 0f, true,  fadeDuration);
    public IEnumerator FadeOut() => Fade(0f, 1f, false, fadeDuration);
    public IEnumerator FadeIn(float dur)  => Fade(1f, 0f, true,  dur);
    public IEnumerator FadeOut(float dur) => Fade(0f, 1f, false, dur);


    void Start()
    {
        if (fadeInOnStart)
            StartCoroutine(FadeIn());
    }
    
    IEnumerator Fade(float from, float to, bool disableAtEnd, float dur)
    {
        if (fadeOverlay == null) yield break;

        fadeOverlay.gameObject.SetActive(true);
        Color c = fadeOverlay.color;
        c.a = from;
        fadeOverlay.color = c;

        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = t / dur;
            k = k * k;
            c.a = Mathf.Lerp(from, to, k);
            fadeOverlay.color = c;
            yield return null;
        }
        c.a = to;
        fadeOverlay.color = c;

        if (disableAtEnd) fadeOverlay.gameObject.SetActive(false); 
    }
    
    public void ResetOverlay()
    {
        if (fadeOverlay == null) return;
        Color c = fadeOverlay.color; c.a = 0f; fadeOverlay.color = c;
        fadeOverlay.gameObject.SetActive(false);
    }
}