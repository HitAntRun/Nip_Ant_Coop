using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class LanternSwing : MonoBehaviour
{
    [SerializeField] private float angle = 3f;
    [SerializeField] private float speed = 1.2f;
    [SerializeField] private bool randomPhase = true;


    private float phase;

    private void Awake()
    {
        phase = randomPhase ? Random.Range(0f, Mathf.PI * 2f) : 0f;
    }

    private void Update()
    {
        float z = Mathf.Sin(Time.time * speed + phase) * angle;
        transform.localRotation = Quaternion.Euler(0f, 0f, z);
    }
}
