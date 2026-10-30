using System.Collections;
using UnityEngine;

public class AlarmShake : MonoBehaviour
{
    [SerializeField] private float angle = 12f;
    [SerializeField] private float jitter = 2f;
    [SerializeField] private int shakeCount = 4;
    [SerializeField] private float stepTime = 0.05f;
    [SerializeField] private float pauseTime = 1f;

    private RectTransform rt;
    private Vector2 basePos;
    private Quaternion baseRot;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        basePos = rt.anchoredPosition;
        baseRot = rt.localRotation;
    }

    void OnEnable()  { StartCoroutine(Loop()); }
    void OnDisable() { ResetPose(); }

    IEnumerator Loop()
    {
        var step = new WaitForSeconds(stepTime);
        var pause = new WaitForSeconds(pauseTime);

        while (true)
        {
            for (int i = 0; i < shakeCount; i++)
            {
                SetPose(+1); yield return step;
                SetPose(-1); yield return step;
            }
            ResetPose();
            yield return pause;
        }
    }

    void SetPose(int dir)
    {
        rt.localRotation = baseRot * Quaternion.Euler(0f, 0f, angle * dir);
        rt.anchoredPosition = basePos + new Vector2(jitter * dir, 0f);
    }

    void ResetPose()
    {
        rt.localRotation = baseRot;
        rt.anchoredPosition = basePos;
    }
}