using System;
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Renderer))]
public class CrocoUVAnimation : MonoBehaviour
{
    const int MaxFrames = 16;

    [Tooltip("Crocotile에서 export된 uvAnimation.txt")]
    public TextAsset animationData;

    [Tooltip("재생할 애니메이션 이름 (비우면 첫 번째)")]
    public string animationName = "WeedAnim";

    [Serializable] class UVPoint { public float x; public float y; }
    [Serializable] class Frame { public UVPoint[] uv; public float duration; }
    [Serializable] class Anim { public string name; public int tileset; public Frame[] frame; }
    [Serializable] class Wrapper { public Anim[] items; }

    static readonly int FrameCountId = Shader.PropertyToID("_AnimFrameCount");
    static readonly int LoopId = Shader.PropertyToID("_AnimLoop");
    static readonly int UV01Id = Shader.PropertyToID("_AnimUV01");
    static readonly int UV23Id = Shader.PropertyToID("_AnimUV23");
    static readonly int EndsId = Shader.PropertyToID("_AnimEnds");

    MaterialPropertyBlock _mpb;

    void OnEnable() => Apply();
    void OnValidate() => Apply();

    public void Apply()
    {
        var rend = GetComponent<Renderer>();
        if (rend == null) return;
        if (_mpb == null) _mpb = new MaterialPropertyBlock();
        rend.GetPropertyBlock(_mpb);

        var uv01 = new Vector4[MaxFrames];
        var uv23 = new Vector4[MaxFrames];
        var ends = new float[MaxFrames];
        int count = 0;
        float total = 0f;

        Anim anim = Load();
        if (anim != null && anim.frame != null)
        {
            count = Mathf.Min(anim.frame.Length, MaxFrames);
            if (anim.frame.Length > MaxFrames)
                Debug.LogWarning($"[CrocoUVAnimation] 프레임이 {MaxFrames}개를 넘어서 앞의 {MaxFrames}개만 씁니다.", this);

            for (int i = 0; i < count; i++)
            {
                var f = anim.frame[i];
                Vector2 P(int k) => (f.uv != null && k < f.uv.Length) ? new Vector2(f.uv[k].x, f.uv[k].y) : Vector2.zero;
                var p0 = P(0); var p1 = P(1); var p2 = P(2); var p3 = P(3);
                uv01[i] = new Vector4(p0.x, p0.y, p1.x, p1.y);
                uv23[i] = new Vector4(p2.x, p2.y, p3.x, p3.y);
                total += Mathf.Max(0.0001f, f.duration);
                ends[i] = total;
            }
        }
        else
        {
            Debug.LogWarning("[CrocoUVAnimation] 애니메이션 데이터를 못 읽었습니다. Animation Data 칸을 확인하세요.", this);
        }

        _mpb.SetFloat(FrameCountId, count);
        _mpb.SetFloat(LoopId, total);
        _mpb.SetVectorArray(UV01Id, uv01);
        _mpb.SetVectorArray(UV23Id, uv23);
        _mpb.SetFloatArray(EndsId, ends);
        rend.SetPropertyBlock(_mpb);
    }

    Anim Load()
    {
        if (animationData == null) return null;
        var w = JsonUtility.FromJson<Wrapper>("{\"items\":" + animationData.text + "}");
        if (w?.items == null || w.items.Length == 0) return null;
        if (string.IsNullOrEmpty(animationName)) return w.items[0];
        foreach (var a in w.items)
            if (a.name == animationName) return a;
        Debug.LogWarning($"[CrocoUVAnimation] '{animationName}' 애니메이션을 못 찾았습니다.", this);
        return null;
    }
}
