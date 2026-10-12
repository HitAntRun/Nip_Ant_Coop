using UnityEngine;

public class YBillboard : MonoBehaviour
{
    private Transform cam;

    void Start() => cam = Camera.main.transform;

    void LateUpdate()
    {
        Vector3 f = cam.forward;
        f.y = 0f;
        if (f.sqrMagnitude < 0.0001f) return;
        transform.rotation = Quaternion.LookRotation(f.normalized, Vector3.up);
    }
}