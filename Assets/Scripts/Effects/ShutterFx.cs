using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ShutterFx : MonoBehaviour
{
    [SerializeField] private Image iris;            // M_IrisShutter 머티리얼을 쓰는 Image
    [SerializeField] private Image flash;           // 흰 플래시 (선택)
    [SerializeField] private float closeTime    = 0.09f;
    [SerializeField] private float holdTime     = 0.05f;
    [SerializeField] private float openTime     = 0.22f;
    [SerializeField] private float rotateAmount = 1.2f;   // 닫힐 때 날 회전량(라디안)
    [SerializeField] private float flashAlpha   = 0.5f;
    [SerializeField] private float flashTime    = 0.2f;
    [SerializeField] private float revealOpenTime   = 0.9f;  // 첫 컷 열림 시간
    [SerializeField] private float revealFlashAlpha = 0.3f;  // 첫 컷 플래시 세기

    static readonly int OpenId   = Shader.PropertyToID("_Open");
    static readonly int RotateId = Shader.PropertyToID("_Rotate");
    static readonly int AspectId = Shader.PropertyToID("_Aspect");

    private Material mat;

    void Awake()
    {
        // 프레임의 Mask가 머티리얼을 복사해버리면 값 변경이 반영 안 되므로 끔.
        // Iris는 프레임과 크기가 같아서 밖으로 안 넘침.
        iris.maskable = false;
        mat = new Material(iris.material);
        iris.material = mat;
        SetOpen(1f);
        SetFlash(0f);
    }

    void UpdateAspect()
    {
        var r = iris.rectTransform.rect;
        if (r.height > 0f) mat.SetFloat(AspectId, r.width / r.height);
    }

    void SetOpen(float k)
    {
        mat.SetFloat(OpenId, k);
        mat.SetFloat(RotateId, (1f - k) * rotateAmount);  // 닫힐수록 회전
        iris.enabled = k < 0.999f;                        // 다 열리면 그리지 않음
    }

    void SetFlash(float a)
    {
        if (flash == null) return;
        var c = flash.color; c.a = a; flash.color = c;
    }

    public IEnumerator Play(Action onClosed)
    {
        iris.color = Color.white;
        UpdateAspect();

        // 닫힘: 프레임 밖 구간은 빠르게, 화면에 들어오면 천천히
        yield return Tween(closeTime, k => { float e = 1f - k; SetOpen(e * e); });
        SetOpen(0f);

        onClosed?.Invoke();
        if (holdTime > 0f) yield return new WaitForSeconds(holdTime);

        if (flash != null) StartCoroutine(Tween(flashTime, k => SetFlash(flashAlpha * (1f - k))));

        // 열림: 화면 안 구간은 천천히, 프레임 밖으로 나갈 땐 빠르게
        yield return Tween(openTime, k => SetOpen(k * k));
        SetOpen(1f);
    }
    // 검은 화면과 구분 안 되게 '검정 날'로 닫아둠
    public void SetClosedBlack()
    {
        UpdateAspect();
        iris.color = Color.black;          // 셰이더가 날 색에 곱함 → 완전 검정
        SetOpen(0f);
    }

// 검은 화면 → 천천히 열리면서 이미지 등장
    public IEnumerator PlayReveal()
    {
        UpdateAspect();
        if (flash != null)
            StartCoroutine(Tween(flashTime, k => SetFlash(revealFlashAlpha * (1f - k))));

        yield return Tween(revealOpenTime, k =>
        {
            float e = Mathf.SmoothStep(0f, 1f, k);                 // 천천히 시작 → 천천히 끝
            iris.color = Color.Lerp(Color.black, Color.white, e);  // 열리면서 날 디테일 드러남
            SetOpen(e);
        });

        iris.color = Color.white;
        SetOpen(1f);
    }

    IEnumerator Tween(float d, Action<float> f)
    {
        float t = 0f;
        while (t < d) { t += Time.deltaTime; f(Mathf.Clamp01(t / d)); yield return null; }
        f(1f);
    }

    void OnDestroy() { if (mat != null) Destroy(mat); }
}