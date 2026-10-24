using UnityEngine;
using UnityEngine.EventSystems;

public class InputManager : MonoBehaviour
{
    private RotateController _rotateController;
    private MagnifierController _magnifier;
    private DialogueRunner _dialogue;
    [SerializeField] private LayerMask clickMask;
    
    [SerializeField, Range(0.5f, 1.2f)]
    private float judgeRadiusScale = 1f;

    static bool NextPressed() =>
        Input.GetKeyDown(KeyCode.Space)
        || Input.GetKeyDown(KeyCode.Return)
        || Input.GetKeyDown(KeyCode.KeypadEnter);
    
    static bool UpPressed() =>
        Input.GetKeyDown(KeyCode.UpArrow)
        || Input.GetKeyDown(KeyCode.W);

    static bool DownPressed() =>
        Input.GetKeyDown(KeyCode.DownArrow)
        || Input.GetKeyDown(KeyCode.S);

    private void Start()
    {
        _rotateController = FindFirstObjectByType<RotateController>();
        _magnifier = FindFirstObjectByType<MagnifierController>();
        _dialogue = FindFirstObjectByType<DialogueRunner>();
    }

    void Update()
    {
        var pause = PauseMenu.instance;
        if (pause != null)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                pause.Toggle();
                return;
            }
            if (pause.IsOpen) return;
        }
        
        var skip = SkipController.instance;
        if (skip != null && skip.IsOpen) return;
        
        var backlog = BacklogController.instance;
        if (backlog != null && backlog.IsOpen) return;
        
        if (_dialogue != null && _dialogue.IsActive)
        {
            if (_dialogue.choosing)
            {
                if (UpPressed())   _dialogue.Move(-1);
                if (DownPressed()) _dialogue.Move(1);
                if(NextPressed()) _dialogue.Confirm();
            }
            else
            {
                bool overUI = EventSystem.current != null
                              && EventSystem.current.IsPointerOverGameObject();
                if (NextPressed()|| (Input.GetMouseButtonDown(0) && !overUI))
                    _dialogue.OnClick();
            }
            return;
        }

        var tut = TutorialController.instance;

        if (tut != null && tut.enabled && tut.IsBlocking)
        {
            if (NextPressed() || Input.GetMouseButtonDown(0))
                tut.Advance();
            return;
        }
        

        if (Input.GetMouseButtonDown(1) && _magnifier != null)
        {
            if (Allowed(TutorialController.Trigger.Magnify))
                _magnifier.ToggleSearchMode();
            else if (tut != null && tut.enabled)
                tut.ShowHint(HintFor(tut.CurrentTrigger));
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (Allowed(TutorialController.Trigger.ReportAnt))
                JudgeAnt();
            else if (tut != null && tut.enabled)
                tut.ShowHint(HintFor(tut.CurrentTrigger));
        }

        if (_magnifier != null && _magnifier.IsSearchMode)
            return;

        if (Input.GetKeyDown(KeyCode.A))
        {
            if (Allowed(TutorialController.Trigger.Rotate) && _rotateController != null)
                _rotateController.Rotate(-90);
            else if (tut != null && tut.enabled)
                tut.ShowHint(HintFor(tut.CurrentTrigger));
        }
        if (Input.GetKeyDown(KeyCode.D))
        {
            if (Allowed(TutorialController.Trigger.Rotate) && _rotateController != null)
                _rotateController.Rotate(90);
            else if (tut != null && tut.enabled)
                tut.ShowHint(HintFor(tut.CurrentTrigger));
        }
    }

    bool Allowed(TutorialController.Trigger t)
        => TutorialController.instance == null || TutorialController.instance.Allows(t);

    string HintFor(TutorialController.Trigger t) => t switch
    {
        TutorialController.Trigger.Rotate    => Loc.Get(Loc.UI, "hint_rotate",  "A,D키를 눌러 지형을 회전시켜보자!"),
        TutorialController.Trigger.Magnify   => Loc.Get(Loc.UI, "hint_magnify", "마우스 우클릭으로 확대해보자!"),
        TutorialController.Trigger.ReportAnt => Loc.Get(Loc.UI, "hint_report",  "흰개미를 찾아 신고해보자!"),
        _ => ""
    };

    void JudgeAnt()
    {
        Camera cam = (_magnifier != null) ? _magnifier.mainCam : Camera.main;
        if (cam == null) return;

        if (_magnifier != null)
        {
            _magnifier.GetJudgeCircle(out Vector2 center, out float radius);
            Termite best = FindTermiteInCircle(cam, center, radius * judgeRadiusScale);
            if (best != null) { best.Judge(); return; }
        }

        Vector2 mouse = Input.mousePosition;

        if (_magnifier != null && _magnifier.IsSearchMode)
        {
            Termite best = FindTermiteInCircle(cam, mouse);
            if (best != null) { best.Judge(); return; }
        }

        Ray ray = cam.ScreenPointToRay(mouse);
        var hits = Physics.RaycastAll(ray, 2000f, clickMask);
        if (hits.Length == 0) return;
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        for (int i = 0; i < hits.Length; i++)
        {
            var t = hits[i].collider.GetComponentInParent<Termite>();
            if (t != null) { t.Judge(); return; }

            var box = hits[i].collider.GetComponentInParent<HiddenContainer>();
            if (box == null) continue;

            if (box.IsOpen)
            {
                for (int j = i + 1; j < hits.Length; j++)
                {
                    var inner = hits[j].collider.GetComponentInParent<Termite>();
                    if (inner != null && inner.transform.IsChildOf(box.transform))
                    { inner.Judge(); return; }
                }
            }
            box.Toggle();
            return;
        }
    }
    
    bool IsOccluded(Camera cam, Termite t, Vector3 worldPos, Vector3 screenPos)
    {
        Ray r = cam.ScreenPointToRay(screenPos);
        float dist = Vector3.Distance(r.origin, worldPos) - 0.01f;
        var hits = Physics.RaycastAll(r, dist, clickMask);

        foreach (var h in hits)
        {
            var box = h.collider.GetComponentInParent<HiddenContainer>();
            if (box == null) continue;
            if (box.IsOpen && t.transform.IsChildOf(box.transform)) continue;

            return true;
        }
        return false;
    }
    Termite FindTermiteInCircle(Camera cam, Vector2 mouse)
    {
        float radius = _magnifier.SearchJudgeRadiusPx * judgeRadiusScale;
        Termite best = null;
        float bestDist = float.MaxValue;

        foreach (var t in FindObjectsByType<Termite>(FindObjectsSortMode.None))
        {
            if (t.IsDying) continue;

            var col = t.GetComponentInChildren<Collider>();
            Vector3 worldPos = col != null ? col.bounds.center : t.transform.position;

            Vector3 sp = cam.WorldToScreenPoint(worldPos);
            if (sp.z < 0) continue;
            if (IsOccluded(cam, t, worldPos, sp)) continue;

            float d = Vector2.Distance(mouse, sp);
            if (d <= radius && d < bestDist)
            {
                best = t;
                bestDist = d;
            }
        }
        return best;
    }
    
    Termite FindTermiteInCircle(Camera cam, Vector2 center, float radius)
    {
        Termite best = null;
        float bestDist = float.MaxValue;

        foreach (var t in FindObjectsByType<Termite>(FindObjectsSortMode.None))
        {
            if (t.IsDying) continue;

            var col = t.GetComponentInChildren<Collider>();
            Vector3 worldPos = col != null ? col.bounds.center : t.transform.position;

            Vector3 sp = cam.WorldToScreenPoint(worldPos);
            if (sp.z < 0) continue;

            float d = Vector2.Distance(center, sp);
            if (d <= radius && d < bestDist) { best = t; bestDist = d; }
        }
        return best;
    }
}