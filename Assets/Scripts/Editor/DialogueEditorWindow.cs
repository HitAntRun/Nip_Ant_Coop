using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Newtonsoft.Json;

public class DialogueEditorWindow : EditorWindow
{
    private DialogueData data = new DialogueData();
    private string filePath;
    private Vector2 scroll;

    private string[] files = new string[0];
    private Vector2 listScroll;

    private string DialogueFolder => Application.streamingAssetsPath;

    static readonly string[] Slots = { "Left", "Center", "Right", "Off" };

    // 대화 종료 후 이동할 씬 목록. 0번은 "이동 안 함" 의미.
    const string NoScene = "(없음 - 여기서 멈춤)";
    static readonly string[] EndScenes = { NoScene, "SC_Ingame", "SC_Story", "SC_Main" };

    // 가로 스크롤 없이 모든 내용이 들어가는 최소 창 크기
    const float SideBarWidth   = 170f;   // 왼쪽 파일 목록 폭
    const float MinRightWidth  = 380f;   // 오른쪽 편집 영역이 필요로 하는 최소 폭
    const float LabelWidth     = 105f;   // 라벨 열 폭 (기본 150 → 축소)

    [MenuItem("Tools/Dialogue Editor")]
    static void Open()
    {
        var w = GetWindow<DialogueEditorWindow>("Dialogue Editor");
        w.minSize = new Vector2(SideBarWidth + MinRightWidth + 30f, 520f);
    }

    void OnEnable() => RefreshFileList();

    void RefreshFileList()
    {
        files = Directory.Exists(DialogueFolder)
            ? Directory.GetFiles(DialogueFolder, "*.json")
            : new string[0];
    }

    //------------------- GUI -------------------
    void OnGUI()
    {
        bool doRefresh = false, doNew = false, doSave = false, doDelete = false;
        string loadTarget = null;

        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.BeginVertical(GUILayout.Width(SideBarWidth));
        if (GUILayout.Button("새로고침")) doRefresh = true;
        if (GUILayout.Button("+ 새 대화")) doNew = true;
        EditorGUILayout.Space(4);

        listScroll = EditorGUILayout.BeginScrollView(
            listScroll, false, false,
            GUIStyle.none, GUI.skin.verticalScrollbar, GUI.skin.scrollView);
        foreach (string f in files)
        {
            GUI.backgroundColor = (f == filePath) ? Color.cyan : Color.white;
            if (GUILayout.Button(Path.GetFileNameWithoutExtension(f)))
                loadTarget = f;
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();

        float rightWidth = Mathf.Max(MinRightWidth, position.width - SideBarWidth - 10f);
        EditorGUILayout.BeginVertical(GUILayout.Width(rightWidth));
        DrawEditor(rightWidth, ref doSave, ref doDelete);
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();

        // ── 레이아웃 그룹이 전부 닫힌 뒤에 실제 작업 수행 ──
        if (doRefresh) RefreshFileList();
        if (doNew)     NewFile();
        if (loadTarget != null) LoadFile(loadTarget);
        if (doSave)    Save();
        if (doDelete)  Delete();

        if (doRefresh || doNew || loadTarget != null || doSave || doDelete)
            Repaint();
    }

    void DrawEditor(float rightWidth, ref bool doSave, ref bool doDelete)
    {
        // 세로 스크롤바와 여백을 뺀, 실제로 쓸 수 있는 콘텐츠 폭.
        // 아래의 모든 컨트롤은 이 폭을 기준으로 명시적인 Width 를 받는다.
        float contentW = Mathf.Max(240f, rightWidth - 28f);
        float fieldW   = contentW - 6f;    // 최상위 컨트롤 폭
        float innerW   = contentW - 18f;   // "box" 안쪽 컨트롤 폭

        float prevLabelWidth = EditorGUIUtility.labelWidth;
        EditorGUIUtility.labelWidth = LabelWidth;

        // ── 상단 고정 툴바 (스크롤과 무관하게 항상 보임) ──
        EditorGUILayout.BeginHorizontal(GUILayout.Width(fieldW));
        if (GUILayout.Button("저장"))  doSave = true;
        if (GUILayout.Button("삭제"))  doDelete = true;
        EditorGUILayout.EndHorizontal();

        // 레이아웃 도중에는 리스트를 건드리지 않고, 플래그만 모아뒀다가 마지막에 처리한다.
        int moveIndex = -1, moveDir = 0;
        int deleteNode = -1;
        int addChoiceNode = -1;
        int removeChoiceNode = -1, removeChoiceIdx = -1;
        int addActorNode = -1;
        int removeActorNode = -1, removeActorIdx = -1;
        bool addNode = false;

        // ── 오른쪽 편집 영역 전체를 감싸는 스크롤뷰 (이 창에 단 하나, 세로만) ──
        scroll = EditorGUILayout.BeginScrollView(
            scroll, false, false,
            GUIStyle.none, GUI.skin.verticalScrollbar, GUI.skin.scrollView);
        EditorGUILayout.BeginVertical(GUILayout.Width(contentW));

        if (data == null)
        {
            EditorGUILayout.LabelField("왼쪽에서 대화를 선택하세요.", GUILayout.Width(fieldW));
        }
        else
        {
            // ── 파일 정보 ──
            data.storyId    = EditorGUILayout.TextField("챕터 이름", data.storyId, GUILayout.Width(fieldW));
            data.background = EditorGUILayout.TextField("시작 배경", data.background, GUILayout.Width(fieldW));
            data.bgm        = EditorGUILayout.TextField("BGM (비우면 유지)", data.bgm, GUILayout.Width(fieldW));

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("상단 UI 표시", EditorStyles.boldLabel, GUILayout.Width(fieldW));
            data.chapterLabel = EditorGUILayout.TextField("챕터 표시명", data.chapterLabel, GUILayout.Width(fieldW));
            data.day          = EditorGUILayout.IntField("개미력 (일)", data.day, GUILayout.Width(fieldW));
            Wrapped("비워두면 이전 값이 그대로 유지됩니다.", EditorStyles.miniLabel, fieldW);

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("스킵 요약문", GUILayout.Width(fieldW));
            data.summary = EditorGUILayout.TextArea(data.summary,
                                                    GUILayout.Width(fieldW), GUILayout.MinHeight(60));

            // ── 대사 목록 ──
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("대사 목록", EditorStyles.boldLabel, GUILayout.Width(fieldW));
            Wrapped("맨 위 대사부터 순서대로 진행됩니다. 선택지가 있으면 목적지로 갈라집니다. 무대는 바뀌는 배우만 적으면 유지됩니다.",
                    EditorStyles.helpBox, fieldW);
            EditorGUILayout.Space(4);

            for (int i = 0; i < data.nodes.Count; i++)
            {
                var n = data.nodes[i];
                EditorGUILayout.BeginVertical("box", GUILayout.Width(fieldW));

                EditorGUILayout.BeginHorizontal(GUILayout.Width(innerW));
                EditorGUILayout.LabelField($"대사 {i + 1}", EditorStyles.boldLabel);
                if (GUILayout.Button("▲", GUILayout.Width(26))) { moveIndex = i; moveDir = -1; }
                if (GUILayout.Button("▼", GUILayout.Width(26))) { moveIndex = i; moveDir = 1; }
                if (GUILayout.Button("삭제", GUILayout.Width(42))) deleteNode = i;
                EditorGUILayout.EndHorizontal();

                n.speaker  = EditorGUILayout.TextField("화자", n.speaker, GUILayout.Width(innerW));
                EditorGUILayout.LabelField("대사", GUILayout.Width(innerW));
                n.text     = EditorGUILayout.TextArea(n.text,
                                                      GUILayout.Width(innerW), GUILayout.MinHeight(40));
                n.portrait = EditorGUILayout.TextField("초상화(선택)", n.portrait, GUILayout.Width(innerW));
                n.portraitBrightness = EditorGUILayout.Slider("초상화 밝기", n.portraitBrightness, 0f, 1f,
                                                              GUILayout.Width(innerW));
                n.shake = EditorGUILayout.Toggle("화면 흔들기", n.shake, GUILayout.Width(innerW));

                EditorGUILayout.Space(2);
                EditorGUILayout.LabelField("컷씬 연출", EditorStyles.boldLabel, GUILayout.Width(innerW));

                bool nar = (n.mode == "narration");
                nar = EditorGUILayout.Toggle("컷씬 진행", nar, GUILayout.Width(innerW));
                n.mode = nar ? "narration" : "";

                n.bg        = EditorGUILayout.TextField("배경 교체(선택)", n.bg, GUILayout.Width(innerW));
                n.bgm       = EditorGUILayout.TextField("BGM 교체(선택)", n.bgm, GUILayout.Width(innerW));
                n.fadeBreak = EditorGUILayout.Toggle("암전 전환", n.fadeBreak, GUILayout.Width(innerW));

                if (nar)
                {
                    EditorGUI.indentLevel++;
                    n.wipe = EditorGUILayout.Toggle("좌→우 와이프", n.wipe, GUILayout.Width(innerW));
                    EditorGUI.indentLevel--;
                }
                
                n.sfx = EditorGUILayout.TextField("효과음(선택)", n.sfx, GUILayout.Width(innerW));

                EditorGUILayout.Space(2);
                if (n.choices.Count == 0)
                {
                    n.endHere = EditorGUILayout.Toggle("여기서 대화 종료", n.endHere, GUILayout.Width(innerW));
                    if (!n.endHere)
                    {
                        var opts = NodeOptions();
                        opts[0] = (i + 1 < data.nodes.Count) ? "→ 바로 다음 대사" : "→ 대화 종료(마지막)";
                        int sel    = TargetToIndex(n.jumpTo);
                        int newSel = EditorGUILayout.Popup("다음 대사", sel, opts, GUILayout.Width(innerW));
                        n.jumpTo   = IndexToTarget(newSel);
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("선택지", EditorStyles.boldLabel, GUILayout.Width(innerW));
                    float popupW  = Mathf.Clamp(innerW * 0.38f, 80f, 240f);
                    float choiceW = Mathf.Max(60f, innerW - popupW - 30f);
                    for (int j = 0; j < n.choices.Count; j++)
                    {
                        EditorGUILayout.BeginHorizontal(GUILayout.Width(innerW));
                        n.choices[j].text = EditorGUILayout.TextField(n.choices[j].text, GUILayout.Width(choiceW));
                        int sel = TargetToIndex(n.choices[j].next);
                        int newSel = EditorGUILayout.Popup(sel, NodeOptions(), GUILayout.Width(popupW));
                        n.choices[j].next = IndexToTarget(newSel);
                        if (GUILayout.Button("x", GUILayout.Width(20))) { removeChoiceNode = i; removeChoiceIdx = j; }
                        EditorGUILayout.EndHorizontal();
                    }
                }
                if (GUILayout.Button("선택지 추가", GUILayout.Width(innerW))) addChoiceNode = i;

                EditorGUILayout.Space(2);
                EditorGUILayout.LabelField("무대 (배우 / 그림 / 위치)", EditorStyles.boldLabel, GUILayout.Width(innerW));

                float idW     = Mathf.Clamp(innerW * 0.18f, 46f, 90f);
                float slotW   = Mathf.Clamp(innerW * 0.14f, 46f, 70f);
                float spriteW = Mathf.Max(60f, innerW - idW - slotW - 30f);

                for (int a = 0; a < n.actors.Count; a++)
                {
                    EditorGUILayout.BeginHorizontal(GUILayout.Width(innerW));
                    n.actors[a].id     = EditorGUILayout.TextField(n.actors[a].id, GUILayout.Width(idW));
                    n.actors[a].sprite = EditorGUILayout.TextField(n.actors[a].sprite, GUILayout.Width(spriteW));
                    int si  = SlotIndex(n.actors[a].slot);
                    int nsi = EditorGUILayout.Popup(si, Slots, GUILayout.Width(slotW));
                    n.actors[a].slot = Slots[nsi];
                    if (GUILayout.Button("x", GUILayout.Width(20))) { removeActorNode = i; removeActorIdx = a; }
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal(GUILayout.Width(innerW));
                    EditorGUIUtility.labelWidth = 34f;
                    n.actors[a].brightness = EditorGUILayout.Slider("밝기", n.actors[a].brightness, 0f, 1f,
                                                                    GUILayout.Width(Mathf.Max(70f, innerW - 140f)));
                    EditorGUIUtility.labelWidth = LabelWidth;
                    string fadeLabel = n.actors[a].slot == "Off" ? "페이드아웃" : "페이드인";
                    n.actors[a].fadeIn = GUILayout.Toggle(n.actors[a].fadeIn, fadeLabel, GUILayout.Width(66));
                    n.actors[a].flip   = GUILayout.Toggle(n.actors[a].flip,   "좌우반전", GUILayout.Width(66));
                    EditorGUILayout.EndHorizontal();
                }
                if (GUILayout.Button("배우 추가", GUILayout.Width(innerW))) addActorNode = i;

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(4);
            }

            if (GUILayout.Button("＋ 대사 추가", GUILayout.Width(fieldW), GUILayout.Height(28))) addNode = true;

            // ── 대화가 끝난 뒤 ──
            EditorGUILayout.Space(10);
            DrawEndSection(fieldW);
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.EndScrollView();

        // ── 레이아웃이 모두 닫힌 뒤에 리스트 변경 처리 ──
        if (data != null)
        {
            if (moveIndex >= 0) MoveNode(moveIndex, moveDir);
            if (deleteNode >= 0) data.nodes.RemoveAt(deleteNode);
            if (removeChoiceNode >= 0) data.nodes[removeChoiceNode].choices.RemoveAt(removeChoiceIdx);
            if (addChoiceNode >= 0) data.nodes[addChoiceNode].choices.Add(new Choice());
            if (removeActorNode >= 0) data.nodes[removeActorNode].actors.RemoveAt(removeActorIdx);
            if (addActorNode >= 0) data.nodes[addActorNode].actors.Add(new ActorState { slot = "Center", brightness = 1f });
            if (addNode) data.nodes.Add(new DialogueNode { id = NewId() });
        }

        EditorGUIUtility.labelWidth = prevLabelWidth;
    }

    // 긴 문구를 주어진 폭 안에서 줄바꿈해 그린다. (폭을 밀어내지 않도록)
    static void Wrapped(string text, GUIStyle style, float width)
    {
        var s = new GUIStyle(style) { wordWrap = true };
        float h = s.CalcHeight(new GUIContent(text), width);
        EditorGUILayout.LabelField(text, s, GUILayout.Width(width), GUILayout.Height(h));
    }

    // ---------------------------------------------------------------- 종료 후 목적지
    void DrawEndSection(float w)
    {
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("대화가 끝난 뒤", EditorStyles.boldLabel, GUILayout.Width(w));

        int idx = SceneIndex(data.nextScene);
        int newIdx = EditorGUILayout.Popup("다음 씬", idx, EndScenes, GUILayout.Width(w));
        data.nextScene = (newIdx == 0) ? "" : EndScenes[newIdx];

        using (new EditorGUI.DisabledScope(newIdx == 0))
        {
            data.nextStage = EditorGUILayout.TextField("다음 스테이지 ID", data.nextStage, GUILayout.Width(w));
            data.skipLoading = EditorGUILayout.Toggle("로딩 화면 생략", data.skipLoading, GUILayout.Width(w));
        }

        if (newIdx == 0)
        {
            Wrapped("→ 대화 종료 후 아무데도 가지 않습니다.", EditorStyles.miniLabel, w);
        }
        else if (string.IsNullOrEmpty(data.nextStage))
        {
            Wrapped("다음 스테이지 ID가 비어있습니다. 씬은 이동하지만 스테이지는 현재 값이 유지되어 같은 구간이 반복될 수 있습니다.",
                    EditorStyles.helpBox, w);
        }
        else
        {
            Wrapped($"→ {data.nextScene} 씬으로 이동 (스테이지: {data.nextStage})", EditorStyles.miniLabel, w);
        }

        EditorGUILayout.Space(4);
    }

    int SceneIndex(string s)
    {
        if (string.IsNullOrEmpty(s)) return 0;
        for (int k = 1; k < EndScenes.Length; k++)
            if (EndScenes[k] == s) return k;
        return 0;
    }

    // ---------------------------------------------------------------- 헬퍼
    string[] NodeOptions()
    {
        var opts = new string[data.nodes.Count + 1];
        opts[0] = "(대화 종료)";
        for (int k = 0; k < data.nodes.Count; k++)
            opts[k + 1] = $"대사 {k + 1}: {data.nodes[k].speaker} \"{Preview(data.nodes[k].text)}\"";
        return opts;
    }

    int TargetToIndex(string id)
    {
        if (string.IsNullOrEmpty(id)) return 0;
        for (int k = 0; k < data.nodes.Count; k++)
            if (data.nodes[k].id == id) return k + 1;
        return 0;
    }

    string IndexToTarget(int index)
        => (index <= 0) ? null : data.nodes[index - 1].id;

    static string Preview(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        s = s.Replace("\n", " ");
        return s.Length > 14 ? s.Substring(0, 14) + "…" : s;
    }

    int SlotIndex(string s)
    {
        for (int k = 0; k < Slots.Length; k++)
            if (Slots[k] == s) return k;
        return 1;
    }

    // ---------------------------------------------------------------- 동작
    void MoveNode(int i, int dir)
    {
        int j = i + dir;
        if (j < 0 || j >= data.nodes.Count) return;
        (data.nodes[i], data.nodes[j]) = (data.nodes[j], data.nodes[i]);
    }

    void NewFile()
    {
        data = new DialogueData();
        filePath = null;
        GUI.FocusControl(null);
    }

    void LoadFile(string path)
    {
        filePath = path;
        data = JsonConvert.DeserializeObject<DialogueData>(File.ReadAllText(path)) ?? new DialogueData();
        foreach (var n in data.nodes)
            if (string.IsNullOrEmpty(n.id)) n.id = NewId();
        GUI.FocusControl(null);
    }

    void Save()
    {
        if (string.IsNullOrEmpty(data.storyId))
        {
            EditorUtility.DisplayDialog("저장 불가", "챕터 이름을 먼저 입력하세요.", "확인");
            return;
        }

        AssignLinks();
        AssignLocKeys();

        if (!Directory.Exists(DialogueFolder)) Directory.CreateDirectory(DialogueFolder);
        if (string.IsNullOrEmpty(filePath))
            filePath = Path.Combine(DialogueFolder, data.storyId + ".json");

        File.WriteAllText(filePath, JsonConvert.SerializeObject(data, Formatting.Indented));
        AssetDatabase.Refresh();
        RefreshFileList();
    }

    void Delete()
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
        {
            EditorUtility.DisplayDialog("삭제 불가", "왼쪽에서 파일을 먼저 선택하세요.", "확인");
            return;
        }
        if (!EditorUtility.DisplayDialog("파일 삭제",
            $"'{Path.GetFileName(filePath)}' 을(를) 삭제할까요?", "삭제", "취소")) return;

        File.Delete(filePath);
        if (File.Exists(filePath + ".meta")) File.Delete(filePath + ".meta");
        filePath = null;
        data = new DialogueData();
        AssetDatabase.Refresh();
        RefreshFileList();
        GUI.FocusControl(null);
    }

    void AssignLinks()
    {
        foreach (var n in data.nodes)
            if (string.IsNullOrEmpty(n.id)) n.id = NewId();

        data.startNodeId = data.nodes.Count > 0 ? data.nodes[0].id : null;

        for (int i = 0; i < data.nodes.Count; i++)
        {
            var n = data.nodes[i];

            if (n.choices != null)
                foreach (var c in n.choices)
                    if (TargetToIndex(c.next) == 0) c.next = null;

            if (n.choices != null && n.choices.Count > 0)
                n.next = null;
            else if (n.endHere)
                n.next = null;
            else if (!string.IsNullOrEmpty(n.jumpTo) && TargetToIndex(n.jumpTo) > 0)
                n.next = n.jumpTo;
            else
                n.next = (i + 1 < data.nodes.Count) ? data.nodes[i + 1].id : null;
        }
    }
    
    void AssignLocKeys()
    {
        string prefix = data.storyId + "_";

        var used = new HashSet<int>();
        foreach (var n in data.nodes)
        {
            if (string.IsNullOrEmpty(n.locKey)) continue;
            if (n.locKey.StartsWith(prefix) &&
                int.TryParse(n.locKey.Substring(prefix.Length), out int num))
                used.Add(num);
        }

        int next = 1;
        var added = new List<DialogueNode>();
        foreach (var n in data.nodes)
        {
            if (!string.IsNullOrEmpty(n.locKey)) continue;
            while (used.Contains(next)) next++;
            n.locKey = prefix + next;
            used.Add(next);
            added.Add(n);
        }

        if (added.Count > 0)
            Debug.Log($"[Dialogue] 신규 locKey {added.Count}개\n" +
                      string.Join("\n", added.ConvertAll(n => $"{n.locKey}\t{n.text}")));
    }

    static string NewId() => "n_" + System.Guid.NewGuid().ToString("N").Substring(0, 8);
}