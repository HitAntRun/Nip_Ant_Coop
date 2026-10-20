using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class CutsceneDrift : MonoBehaviour
{
    [SerializeField] float amplitude = 4f;
    [SerializeField] float rotation = 0.25f;
    [SerializeField] private float speed = 0.4f;
    [SerializeField] float overscale = 1.03f;
    [SerializeField] bool useUnscaledTime = false;

    RectTransform rt;
    Vector2 homePos;
    Quaternion homeRot;
    Vector3 homeScale;
    float seedX, seedY, seedR;

    public void Configure(float amplitude, float rotation, float speed, float overscale)
    {
        this.amplitude = amplitude;
        this.rotation = rotation;
        this.speed = speed;
        this.overscale = overscale;
        if (rt != null && isActiveAndEnabled) rt.localScale = homeScale * overscale;
    }

    void Awake()
    {
        rt = (RectTransform)transform;
        homePos = rt.anchoredPosition;
        homeRot = rt.localRotation;
        homeScale = rt.localScale;
        seedX = Random.value * 100f;
        seedY = Random.value * 100f + 37f;
        seedR = Random.value * 100f + 71f;
    }

    void OnEnable()
    {
        if (rt != null) rt.localScale = homeScale * overscale;
    }

    void OnDisable()
    {
        if (rt == null) return;
        rt.anchoredPosition = homePos;
        rt.localRotation = homeRot;
        rt.localScale = homeScale;
    }

    void Update()
    {
        float t = (useUnscaledTime ? Time.unscaledTime : Time.time) * speed;
        float x = (Mathf.PerlinNoise(seedX, t) - 0.5f) * 2f;
        float y = (Mathf.PerlinNoise(seedY, t) - 0.5f) * 2f;
        float r = (Mathf.PerlinNoise(seedR, t) - 0.5f) * 2f;

        rt.anchoredPosition = homePos + new Vector2(x, y) * amplitude;
        rt.localRotation = homeRot * Quaternion.Euler(0f, 0f, r * rotation);
    }
}
