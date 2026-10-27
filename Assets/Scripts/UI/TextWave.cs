using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class TextWave : MonoBehaviour
{
    [SerializeField] float amplitude   = 4f;
    [SerializeField] float charDelay   = 0.08f;
    [SerializeField] float hopDuration = 0.3f;
    [SerializeField] float pauseTime   = 1.2f;
    [SerializeField] float pixelSnap = 2f;
    [SerializeField] bool useUnscaledTime = true;

    TMP_Text tmp;

    void Awake() => tmp = GetComponent<TMP_Text>();

    void LateUpdate()
    {
  
        tmp.ForceMeshUpdate();
        TMP_TextInfo info = tmp.textInfo;
        int count = info.characterCount;
        if (count == 0) return;

        float t = useUnscaledTime ? Time.unscaledTime : Time.time;
        float cycle = (count - 1) * charDelay + hopDuration + pauseTime;
        float local = t % cycle;

        for (int i = 0; i < count; i++)
        {
            TMP_CharacterInfo c = info.characterInfo[i];
            if (!c.isVisible) continue;

            float p = (local - i * charDelay) / hopDuration;
            if (p <= 0f || p >= 1f) continue;

            float y = Mathf.Sin(p * Mathf.PI) * amplitude;
            if (pixelSnap > 0f) y = Mathf.Round(y / pixelSnap) * pixelSnap;

            Vector3[] verts = info.meshInfo[c.materialReferenceIndex].vertices;
            int v = c.vertexIndex;
            Vector3 off = new Vector3(0f, y, 0f);
            verts[v] += off; verts[v + 1] += off; verts[v + 2] += off; verts[v + 3] += off;
        }

        for (int m = 0; m < info.meshInfo.Length; m++)
        {
            info.meshInfo[m].mesh.vertices = info.meshInfo[m].vertices;
            tmp.UpdateGeometry(info.meshInfo[m].mesh, m);
        }
    }
}