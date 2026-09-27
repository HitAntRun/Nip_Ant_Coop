using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class AntPulse : MonoBehaviour
{
    [SerializeField] private float amplitude = 0.08f;
    [SerializeField] private float period = 1.8f;
    [SerializeField] private bool randomPhase = true;

    private Vector3 baseScale;
    private float phase;

    private void Start()
    {
        baseScale = transform.localScale;
        phase = randomPhase ? Random.Range(0f, Mathf.PI * 2f) : 0f;
    }

    private void Update()
    {
        float t = (Mathf.Sin(Time.time * (Mathf.PI * 2f / period) + phase) + 1f) * 0.5f;
        transform.localScale = baseScale * (1f + amplitude * t);
    }
}
