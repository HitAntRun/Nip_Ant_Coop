using System.Collections;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    public static CameraShake instance;

    [SerializeField] private float duration = 0.25f;
    [SerializeField] private float strength = 0.2f;
    [SerializeField] private float frequency = 30f;

    private Vector3 basePos;
    private Coroutine running;

    void Awake()
    {
        instance = this;
        basePos = transform.localPosition;
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    public void Shake() => Shake(duration, strength);

    public void Shake(float dur, float str)
    {
        if (running != null) StopCoroutine(running);
        transform.localPosition = basePos;
        running = StartCoroutine(Run(dur, str));
    }

    IEnumerator Run(float dur, float str)
    {
        float t = 0f, next = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            if (t >= next)
            {
                next += 1f / frequency;
                float k = 1f - Mathf.Clamp01(t / dur);              
                Vector2 r = Random.insideUnitCircle.normalized * str * k;
                Vector3 w = transform.right * r.x + transform.up * r.y;
                Vector3 local = transform.parent != null
                    ? transform.parent.InverseTransformDirection(w)
                    : w;
                transform.localPosition = basePos + local;
            }
            yield return null;
        }
        transform.localPosition = basePos;
        running = null;
    }

    void OnDisable()
    {
        transform.localPosition = basePos;
        running = null;
    }
}
