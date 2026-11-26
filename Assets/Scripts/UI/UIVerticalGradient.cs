using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Graphic))]
public class UIVerticalGradient : BaseMeshEffect
{
    public Color top = Color.white;
    public Color bottom = Color.black;

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive()) return;
        Rect r = graphic.rectTransform.rect;
        var v = new UIVertex();
        for (int i = 0; i < vh.currentVertCount; i++)
        {
            vh.PopulateUIVertex(ref v, i);
            float k = Mathf.InverseLerp(r.yMin, r.yMax, v.position.y);
            v.color = Color.Lerp(bottom, top, k);
            vh.SetUIVertex(v, i);
        }
    }
}