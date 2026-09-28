using System;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class LightPulse : MonoBehaviour
{
    [Range(0f, 1f)] [SerializeField] private float minAlpha = 0.4f;
    [Range(0f, 1f)] [SerializeField] private float maxAlpha = 1f;
    [SerializeField] private float speed = 1.5f;
    [SerializeField] private bool randomPhase = true;

    private Image img;
    private float phase;

    private void Awake()
    {
        img = GetComponent<Image>();
        phase = randomPhase ? Random.Range(0f, Mathf.PI * 2f) : 0f;
    }

    private void Update()
    {
        float t = (Mathf.Sin(Time.time * speed + phase) + 1f) * 0.5f;
        Color c = img.color;
        c.a = Mathf.Lerp(minAlpha, maxAlpha, t);
        img.color = c;
    }
}
