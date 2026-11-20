using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class EndlessTermiteSpawner : MonoBehaviour
{
    private struct Candidate
    {
        public Vector3 localPos;
        public Vector3 localNormal;
    }

    private EndlessRespawnSettings cfg;
    private Transform cube;
    private GameObject termitePrefab;
    private GameObject termite2Prefab;
    private GameTimer timer;
    private Camera cam;

    private readonly List<Candidate> candidates = new List<Candidate>();
    private readonly List<Termite> alive = new List<Termite>();
    private int pending;
    private float halfHeight = 0.5f;

    static float MeasureHalfHeight(GameObject prefab)
    {
        var sr = prefab != null ? prefab.GetComponent<SpriteRenderer>() : null;
        if (sr == null || sr.sprite == null) return 0.5f;
        if (sr.drawMode != SpriteDrawMode.Simple) return sr.size.y * 0.5f;
        return Mathf.Max(0f, -sr.sprite.bounds.min.y);
    }

    public void Begin(StageTermiteData stage, Transform cubeTransform, GameObject prefab, GameObject prefab2 = null)
    {
        cfg = stage.endless ?? new EndlessRespawnSettings();
        cube = cubeTransform;
        termitePrefab = prefab;
        termite2Prefab = prefab2;
        timer = FindFirstObjectByType<GameTimer>();
        cam = Camera.main;

        StopAllCoroutines();
        alive.Clear();
        pending = 0;

        halfHeight = MeasureHalfHeight(prefab);
        BuildCandidates();
        if (candidates.Count == 0)
        {
            Debug.LogWarning("[EndlessTermite] 배치 가능한 지점을 찾지 못함 (맵 MeshCollider 확인)");
            return;
        }

        AntCounter.instance?.SetEndless();

        int n = Mathf.Min(cfg.initialCount, cfg.maxAlive);
        for (int i = 0; i < n; i++)
            SpawnOne(null, pop: false);
    }

    void BuildCandidates()
    {
        candidates.Clear();
        Physics.SyncTransforms();

        var grounds = new List<Collider>();
        var own = cube.GetComponent<Collider>();
        if (own != null) grounds.Add(own);
        else
        {
            foreach (var c in cube.GetComponentsInChildren<Collider>(true))
                if (c.GetComponentInParent<Termite>() == null && c.GetComponentInParent<HiddenContainer>() == null)
                    grounds.Add(c);
        }
        if (grounds.Count == 0) return;

        Bounds b = grounds[0].bounds;
        foreach (var g in grounds) b.Encapsulate(g.bounds);

        Vector3 up = cube.up;
        float height = b.size.magnitude + 1f;
        float minCos = Mathf.Cos(cfg.maxSlopeAngle * Mathf.Deg2Rad);
        float spacingSq = cfg.candidateSpacing * cfg.candidateSpacing;

        for (int i = 0; i < cfg.sampleAttempts && candidates.Count < cfg.maxCandidates; i++)
        {
            var p = new Vector3(Random.Range(b.min.x, b.max.x), b.center.y, Random.Range(b.min.z, b.max.z));
            var origin = p + up * height;

            if (!Physics.Raycast(origin, -up, out var hit, height * 2f, ~0, QueryTriggerInteraction.Ignore))
                continue;
            if (!grounds.Contains(hit.collider)) continue;
            if (Vector3.Dot(hit.normal, up) < minCos) continue;
            if (!HasFloorAround(hit.point, grounds, up, height, minCos)) continue;

            var local = cube.InverseTransformPoint(hit.point);
            bool tooClose = false;
            foreach (var c in candidates)
                if ((c.localPos - local).sqrMagnitude < spacingSq) { tooClose = true; break; }
            if (tooClose) continue;

            candidates.Add(new Candidate
            {
                localPos = local,
                localNormal = cube.InverseTransformDirection(hit.normal).normalized
            });
        }

        Debug.Log($"[EndlessTermite] 후보 지점 {candidates.Count}개");
    }

    bool HasFloorAround(Vector3 point, List<Collider> grounds, Vector3 up, float height, float minCos)
    {
        if (cfg.edgeMargin <= 0f) return true;

        float baseH = Vector3.Dot(point, up);
        for (int d = 0; d < 8; d++)
        {
            float a = d * Mathf.PI * 0.25f;
            Vector3 dirLocal = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * cfg.edgeMargin;
            Vector3 probe = point + cube.TransformVector(dirLocal);

            if (!Physics.Raycast(probe + up * height, -up, out var h, height * 2f, ~0, QueryTriggerInteraction.Ignore))
                return false;
            if (!grounds.Contains(h.collider)) continue;
            if (Vector3.Dot(h.normal, up) < minCos) return false;
            if (Mathf.Abs(Vector3.Dot(h.point, up) - baseH) > cfg.maxEdgeStep) return false;
        }
        return true;
    }

    bool TryPick(Vector3? caughtLocal, bool checkCursor, bool checkAlive, out Candidate result)
    {
        float caughtSq = cfg.minDistFromCaught * cfg.minDistFromCaught;
        float aliveSq  = cfg.minDistFromAlive * cfg.minDistFromAlive;

        for (int i = 0; i < 40; i++)
        {
            var c = candidates[Random.Range(0, candidates.Count)];

            if (caughtLocal.HasValue && (c.localPos - caughtLocal.Value).sqrMagnitude < caughtSq)
                continue;

            if (checkCursor && cam != null)
            {
                var sp = cam.WorldToScreenPoint(cube.TransformPoint(c.localPos));
                if (sp.z > 0f && Vector2.Distance(sp, Input.mousePosition) < cfg.cursorAvoidPixels)
                    continue;
            }

            if (checkAlive)
            {
                bool near = false;
                foreach (var t in alive)
                {
                    if (t == null) continue;
                    if ((t.transform.localPosition - c.localPos).sqrMagnitude < aliveSq) { near = true; break; }
                }
                if (near) continue;
            }

            result = c;
            return true;
        }
        result = default;
        return false;
    }

    void SpawnOne(Vector3? caughtLocal, bool pop)
    {
        if (!TryPick(caughtLocal, pop, true, out var c) &&
            !TryPick(caughtLocal, pop, false, out c) &&
            !TryPick(caughtLocal, false, false, out c))
            c = candidates[Random.Range(0, candidates.Count)];

        float scale = Random.Range(cfg.scaleRange.x, cfg.scaleRange.y);

        float lift = halfHeight * scale + cfg.surfaceOffset;

        var info = new TermiteSpawnInfo
        {
            antType = TermiteSpawnInfo.AntType.Termite,
            localPosition = c.localPos + Vector3.up * lift,
            surfaceNormal = c.localNormal,
            scale = scale,
            isReal = true
        };

        var prefab = (termite2Prefab != null && Random.value < cfg.termite2Ratio)
            ? termite2Prefab
            : termitePrefab;
        var go = Instantiate(prefab, cube);        go.transform.localPosition = info.localPosition;

        var termite = go.GetComponent<Termite>();
        if (termite != null)
        {
            termite.Init(info);
            termite.Reported += OnReported;
            alive.Add(termite);
        }

        if (pop)
        {
            go.transform.localScale = Vector3.zero;
            go.transform.DOScale(Vector3.one * scale, cfg.popDuration).SetEase(Ease.OutBack).SetLink(go);
        }
        else
        {
            go.transform.localScale = Vector3.one * scale;
        }
    }

    void OnReported(Termite t)
    {
        t.Reported -= OnReported;
        alive.Remove(t);
        alive.RemoveAll(x => x == null);

        float elapsed = timer != null ? timer.Elapsed : 0f;
        float k = Mathf.InverseLerp(cfg.escalateStartTime, cfg.escalateFullTime, elapsed);
        float expected = Mathf.Lerp(1f, cfg.maxSpawnPerCatch, k);
        int count = Mathf.FloorToInt(expected);
        if (Random.value < expected - count) count++;

        int room = cfg.maxAlive - alive.Count - pending;
        count = Mathf.Clamp(count, 0, room);

        Vector3 caught = t.transform.localPosition;
        for (int i = 0; i < count; i++)
            StartCoroutine(SpawnLater(caught, cfg.respawnDelay + i * cfg.extraSpawnStagger));
    }

    IEnumerator SpawnLater(Vector3 caughtLocal, float delay)
    {
        pending++;
        yield return new WaitForSeconds(delay);
        pending--;

        if (StageFlow.instance != null && StageFlow.instance.IsEnded) yield break;
        if (alive.Count >= cfg.maxAlive) yield break;

        SpawnOne(caughtLocal, pop: true);
    }
}
