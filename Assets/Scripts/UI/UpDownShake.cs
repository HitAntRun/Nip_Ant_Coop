using System.Collections;
using UnityEngine;

public class UpDownShake : MonoBehaviour
{
    [SerializeField] private float[] offsets = { 0f, 3f, 6f, 3f };
    [SerializeField] private float stepTime = 0.15f;

    private RectTransform rt;
    private Vector2 basePos;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        basePos = rt.anchoredPosition;
    }

    void OnEnable()  { StartCoroutine(Loop()); }
    void OnDisable() { rt.anchoredPosition = basePos; }

    IEnumerator Loop()
    {
        int i = 0;
        while (true)
        {
            rt.anchoredPosition = basePos + new Vector2(0f, offsets[i]);
            i = (i + 1) % offsets.Length;
            yield return new WaitForSecondsRealtime(stepTime);
        }
    }
}