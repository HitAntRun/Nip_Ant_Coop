using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class TermiteSpawner : MonoBehaviour
{
    [SerializeField] private Transform cubeTransform;
    [SerializeField] private GameObject termitePrefab;
    [SerializeField] private GameObject blackAntPrefab;
    [SerializeField] private StageTermiteData currentStage;
    [SerializeField] private AntMonologueData antMonologueData;
    [SerializeField] private int speakerCount = 3;
    void Start()
    {
        if (currentStage != null)
            SpawnStage(currentStage);
    }

    public void SpawnStage(StageTermiteData stage)
    {
        currentStage = stage;
        ClearExisting();

        int realCount = 0;
        var blackAnts = new List<AntMonologue>();

        var boxes = new Dictionary<string, HiddenContainer>();
        foreach (var c in cubeTransform.GetComponentsInChildren<HiddenContainer>(true))
        {
            c.ResetState();
            boxes[c.Id] = c;
        }
        foreach (var info in stage.termites)
        {
            var isBlack = info.antType == TermiteSpawnInfo.AntType.BlackAnt;
            var prefab = info.antType == TermiteSpawnInfo.AntType.BlackAnt ? blackAntPrefab : termitePrefab;
            HiddenContainer box = null;
            bool inBox = !string.IsNullOrEmpty(info.containerId);

            if (inBox && !boxes.TryGetValue(info.containerId, out box))
            {
                Debug.LogWarning($"containerId '{info.containerId}' 상자를 찾을 수 없음");
                inBox = false;
            }
            GameObject t = Instantiate(prefab, inBox ? box.AntAnchor : cubeTransform);
            t.transform.localPosition = inBox ? Vector3.zero : info.localPosition;
            if (inBox)
            {
                Vector3 want = cubeTransform.lossyScale * info.scale;
                Vector3 p    = t.transform.parent.lossyScale;
                t.transform.localScale = new Vector3(want.x / p.x, want.y / p.y, want.z / p.z);
            }
            else
            {
                t.transform.localScale = Vector3.one * info.scale;
            }

            
            var termite = t.GetComponent<Termite>();
            if(termite != null) termite.Init(info);
            if (inBox && termite != null) box.SetHidden(termite);
            
            if (isBlack && !inBox)
            {
                var m = t.GetComponent<AntMonologue>();
                if (m != null) blackAnts.Add(m);
            }

            if (info.isReal) realCount++;
        }

        AntCounter.instance?.SetTotal(realCount);
        AssignMonologues(blackAnts);
    }

    private void AssignMonologues(List<AntMonologue> ants)
    {
        if (antMonologueData == null || antMonologueData.lines.Count == 0) return;
        if (ants.Count == 0) return;

        for (int i = ants.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (ants[i], ants[j]) = (ants[j], ants[i]);
        }

        var pool = new List<string>(antMonologueData.lines);
        for (int i = pool.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }

        int count = Mathf.Min(speakerCount, ants.Count);
        count = Mathf.Min(count, pool.Count);

        for (int i = 0; i < count; i++)
            ants[i].Begin(pool[i]);
    }
    private void ClearExisting()
    {
        for (int i = cubeTransform.childCount - 1; i >= 0; i--)
        {
            var child = cubeTransform.GetChild(i);
            foreach (var t in cubeTransform.GetComponentsInChildren<Termite>(true))
                Destroy(t.gameObject);
        }
    }
}
