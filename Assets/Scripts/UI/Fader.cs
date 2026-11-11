using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Fader : MonoBehaviour
{
    [SerializeField] private Image fadeOverlay;
    [SerializeField] private float fadeDuration = 1f;
    [SerializeField] private bool fadeInOnStart = false;
    [SerializeField] private int   flashBlinks = 2;
    [SerializeField] private float blinkFrame  = 0.05f;
    [SerializeField] private int   flashSteps = 4;
    [SerializeField] private float flashFps   = 12f; 

    private Color savedColor;

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

    public IEnumerator FadeTo(float to, float dur, bool disableAtEnd = false)
    {
        if (fadeOverlay == null) yield break;
        float from = fadeOverlay.gameObject.activeSelf ? fadeOverlay.color.a : 0f;
        yield return Fade(from, to, disableAtEnd, dur);
    }
    
    public void SetAlpha(float a)
    {
        if (fadeOverlay == null) return;
        fadeOverlay.gameObject.SetActive(a > 0f);
        Color c = fadeOverlay.color; c.a = a; fadeOverlay.color = c;
    }
    
    public void ResetOverlay()
    {
        if (fadeOverlay == null) return;
        Color c = fadeOverlay.color; c.a = 0f; fadeOverlay.color = c;
        fadeOverlay.gameObject.SetActive(false);
    }
    
    public IEnumerator FlashIn(Color color, float dur)
    {
        if (fadeOverlay == null) yield break;
        savedColor = fadeOverlay.color;
        fadeOverlay.gameObject.SetActive(true);

        Color on = color;  on.a = 1f;
        Color off = color; off.a = 0f;

        for (int i = 0; i < flashBlinks; i++)
        {
            fadeOverlay.color = on;  yield return new WaitForSeconds(blinkFrame);
            fadeOverlay.color = off; yield return new WaitForSeconds(blinkFrame);
        }
        fadeOverlay.color = on;
    }
    public IEnumerator FlashOut(float dur)
    {
        if (fadeOverlay == null) yield break;
        Color c = fadeOverlay.color;

        float t = 0f, tick = 0f, step = 1f / flashFps;
        while (t < dur)
        {
            t += Time.deltaTime;
            tick += Time.deltaTime;
            if (tick >= step)
            {
                tick = 0f;
                float k = Mathf.Clamp01(t / dur);
                int   idx = Mathf.Min(Mathf.FloorToInt(k * flashSteps), flashSteps);
                float a = 1f - (float)idx / flashSteps;
                a = a * a;                               
                c.a = a;
                fadeOverlay.color = c;
            }
            yield return null;
        }
        Color r = savedColor; r.a = 0f;
        fadeOverlay.color = r;
        fadeOverlay.gameObject.SetActive(false);
    }
}