using UnityEngine;
using UnityEngine.EventSystems;

public class InputManager : MonoBehaviour
{
    private RotateController _rotateController;
    private MagnifierController _magnifier;
    private DialogueRunner _dialogue;

    static bool NextPressed() =>
        Input.GetKeyDown(KeyCode.Space)
        || Input.GetKeyDown(KeyCode.Return)
        || Input.GetKeyDown(KeyCode.KeypadEnter);

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
                if (Input.GetKeyDown(KeyCode.UpArrow))   _dialogue.Move(-1);
                if (Input.GetKeyDown(KeyCode.DownArrow)) _dialogue.Move(1);
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

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            var t = hit.collider.GetComponent<Termite>();
            if (t != null) t.Judge();
        }
    }
}