using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;

using UnityEngine;
using UnityEngine.UIElements;

// 场景顺序管理窗口：可视化 Build Settings 中的场景顺序，支持增删改查、插入与上下移动；
// 监控场景变化；一键打开、步进与自动顺序播放；持久化到 EditorBuildSettings。
public class SceneSequenceWindow : EditorWindow
{
    [Serializable]
    private class SceneEntry
    {
        public string path;           // 绝对工程内路径
        public string name;           // 仅显示名
        public bool enabled;          // 是否参与构建
    }

    private readonly List<SceneEntry> _entries = new List<SceneEntry>();
    private ListView _listView;
    private Label _activeSceneLabel;
    private ObjectField _addField;
    private IntegerField _insertIndexField;
    private FloatField _autoDelayField;
    private Toggle _autoLoopToggle;
    private Button _playBtn, _stopBtn, _nextBtn, _prevBtn, _applyBtn, _refreshBtn;

    private bool _autoPlaying;
    private double _nextAutoAt;
    private int _playCursor;

    [MenuItem("自制工具/场景/场景顺序管理器")] private static void Open()
    {
        var w = GetWindow<SceneSequenceWindow>(false, "场景顺序", true);
        w.minSize = new Vector2(680, 420);
    }

    private void OnEnable()
    {
        EditorSceneManager.activeSceneChangedInEditMode += OnActiveSceneChanged;
        EditorSceneManager.sceneOpened += OnSceneOpened;
        EditorApplication.update += OnEditorUpdate;
    }

    private void OnDisable()
    {
        EditorSceneManager.activeSceneChangedInEditMode -= OnActiveSceneChanged;
        EditorSceneManager.sceneOpened -= OnSceneOpened;
        EditorApplication.update -= OnEditorUpdate;
        _autoPlaying = false;
    }

    private void OnActiveSceneChanged(UnityEngine.SceneManagement.Scene a, UnityEngine.SceneManagement.Scene b)
    {
        RefreshActiveSceneLabel();
    }

    private void OnSceneOpened(UnityEngine.SceneManagement.Scene scn, OpenSceneMode mode)
    {
        RefreshActiveSceneLabel();
        // 打开后若处于自动播放，推进游标
        if (_autoPlaying)
        {
            // 将游标移动到刚打开场景索引
            int idx = _entries.FindIndex(e => e.path == scn.path);
            if (idx >= 0) _playCursor = idx;
            _nextAutoAt = EditorApplication.timeSinceStartup + Mathf.Max(0.2f, _autoDelayField.value);
        }
    }

    private void OnEditorUpdate()
    {
        if (_autoPlaying && EditorApplication.timeSinceStartup >= _nextAutoAt)
        {
            StepNext();
            _nextAutoAt = EditorApplication.timeSinceStartup + Mathf.Max(0.2f, _autoDelayField.value);
        }
    }

    public void CreateGUI()
    {
        // 载入彩色样式（优先 Editor/ 路径，兼容旧路径）
        string ussPath = ResolveUssPath("Assets/Scripts/Editor/UITK/EditorColors.uss", "Assets/Scripts/0_Editor/UITK/EditorColors.uss");
        var sheet =
            AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Editor/UITK/EditorColors.uss")
            ?? AssetDatabase.LoadAssetAtPath<StyleSheet>(ussPath);
        if (sheet != null) rootVisualElement.styleSheets.Add(sheet);
        rootVisualElement.AddToClassList("app-root");
        rootVisualElement.style.paddingLeft = 6;
        rootVisualElement.style.paddingRight = 6;
        rootVisualElement.style.paddingTop = 6;
        rootVisualElement.style.paddingBottom = 6;

        var split = new TwoPaneSplitView(0, 160, TwoPaneSplitViewOrientation.Vertical);
        rootVisualElement.Add(split);

        var top = new ScrollView();
        top.AddToClassList("section");
        split.Add(top);

        var bottom = new ScrollView();
        bottom.AddToClassList("section");
        split.Add(bottom);

        // 顶部：状态与操作
        var title = new Label("场景顺序管理器");
        title.AddToClassList("section-title");
        top.Add(title);

        _activeSceneLabel = new Label();
        top.Add(_activeSceneLabel);

        var row1 = new VisualElement { style = { flexDirection = FlexDirection.Row } };
        _refreshBtn = new Button(RefreshFromBuildSettings) { text = "刷新" };
        _applyBtn = new Button(ApplyToBuildSettings) { text = "应用到BuildSettings" };
        _refreshBtn.AddToClassList("btn"); _refreshBtn.AddToClassList("btn-primary");
        _applyBtn.AddToClassList("btn"); _applyBtn.AddToClassList("btn-success");
        row1.Add(_refreshBtn);
        row1.Add(_applyBtn);
        top.Add(row1);

        var row2 = new VisualElement { style = { flexDirection = FlexDirection.Row } };
        _addField = new ObjectField("添加场景") { objectType = typeof(SceneAsset), allowSceneObjects = false };
        var addBtn = new Button(() => AddSceneAtEnd(_addField.value as SceneAsset)) { text = "添加到末尾" };
        _insertIndexField = new IntegerField("插入下标") { value = 0 };
        var insertBtn = new Button(() => InsertSceneAt(_insertIndexField.value, _addField.value as SceneAsset)) { text = "在下标插入" };
        addBtn.AddToClassList("btn"); addBtn.AddToClassList("btn-secondary");
        insertBtn.AddToClassList("btn"); insertBtn.AddToClassList("btn-secondary");
        row2.Add(_addField); row2.Add(addBtn); row2.Add(_insertIndexField); row2.Add(insertBtn);
        top.Add(row2);

        var row3 = new VisualElement { style = { flexDirection = FlexDirection.Row } };
        _prevBtn = new Button(StepPrev) { text = "上一个" };
        _nextBtn = new Button(StepNext) { text = "下一个" };
        _autoDelayField = new FloatField("自动间隔(秒)") { value = 1.0f };
        _autoLoopToggle = new Toggle("循环") { value = false };
        _playBtn = new Button(StartAuto) { text = "自动播放" };
        _stopBtn = new Button(StopAuto) { text = "停止" };
        _prevBtn.AddToClassList("btn"); _prevBtn.AddToClassList("btn-ghost");
        _nextBtn.AddToClassList("btn"); _nextBtn.AddToClassList("btn-ghost");
        _playBtn.AddToClassList("btn"); _playBtn.AddToClassList("btn-primary");
        _stopBtn.AddToClassList("btn"); _stopBtn.AddToClassList("btn-warn");
        row3.Add(_prevBtn); row3.Add(_nextBtn); row3.Add(_autoDelayField); row3.Add(_autoLoopToggle); row3.Add(_playBtn); row3.Add(_stopBtn);
        top.Add(row3);

        // 列表
        _listView = new ListView
        {
            selectionType = SelectionType.Single,
            virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
            showBorder = true,
            style = { height = 320 }
        };
        _listView.AddToClassList("list");
        _listView.makeItem = () => new VisualElement { style = { flexDirection = FlexDirection.Row } };
        _listView.bindItem = (ve, i) =>
        {
            ve.Clear();
            if (i < 0 || i >= _entries.Count) return;
            var e = _entries[i];
            ve.AddToClassList((i % 2 == 0) ? "row-even" : "row-odd");

            var enableToggle = new Toggle() { value = e.enabled, tooltip = "是否参与构建" };
            enableToggle.RegisterValueChangedCallback(v => { e.enabled = v.newValue; });
            ve.Add(enableToggle);

            var nameLabel = new Label(e.name) { style = { width = 260 } };
            ve.Add(nameLabel);

            var pathLabel = new Label(e.path) { style = { flexGrow = 1 } };
            pathLabel.style.whiteSpace = WhiteSpace.Normal;
            ve.Add(pathLabel);

            var openBtn = new Button(() => OpenSceneAt(i)) { text = "打开" };
            var upBtn = new Button(() => MoveUp(i)) { text = "上移" };
            var downBtn = new Button(() => MoveDown(i)) { text = "下移" };
            var delBtn = new Button(() => RemoveAt(i)) { text = "删除" };
            openBtn.AddToClassList("btn"); openBtn.AddToClassList("btn-primary");
            upBtn.AddToClassList("btn"); downBtn.AddToClassList("btn");
            delBtn.AddToClassList("btn"); delBtn.AddToClassList("btn-danger");
            ve.Add(openBtn); ve.Add(upBtn); ve.Add(downBtn); ve.Add(delBtn);
        };
        _listView.selectionChanged += sel =>
        {
            // 选中时更新播放游标
            int idx = _listView.selectedIndex;
            if (idx >= 0 && idx < _entries.Count) _playCursor = idx;
        };
        bottom.Add(_listView);

        // 初始数据
        RefreshFromBuildSettings();
        RefreshActiveSceneLabel();
    }

    private static string ResolveUssPath(string preferred, string fallback)
    {
        // 若 Editor/ 已存在则直接返回该路径；否则按传入参数的优先级回退
        var editorPath = "Assets/Editor/UITK/EditorColors.uss";
        if (AssetDatabase.LoadAssetAtPath<StyleSheet>(editorPath) != null) return editorPath;
        var ss = AssetDatabase.LoadAssetAtPath<StyleSheet>(preferred);
        if (ss != null) return preferred;
        return fallback;
    }

    private void RefreshActiveSceneLabel()
    {
        var scn = EditorSceneManager.GetActiveScene();
        _activeSceneLabel.text = $"当前场景：{(scn.IsValid() ? scn.name : "<无>")}  @ {scn.path}";
    }

    private void RefreshFromBuildSettings()
    {
        _entries.Clear();
        foreach (var s in EditorBuildSettings.scenes)
        {
            var name = System.IO.Path.GetFileNameWithoutExtension(s.path);
            _entries.Add(new SceneEntry { path = s.path, name = name, enabled = s.enabled });
        }
        _listView.itemsSource = _entries;
        _listView.Rebuild();
        _playCursor = Mathf.Clamp(_playCursor, 0, Math.Max(0, _entries.Count - 1));
    }

    private void ApplyToBuildSettings()
    {
        var arr = _entries.Select(e => new EditorBuildSettingsScene(e.path, e.enabled)).ToArray();
        EditorBuildSettings.scenes = arr;
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        ShowNotification(new GUIContent("已写入 BuildSettings"));
    }

    private static string GetAssetPath(SceneAsset asset)
    {
        return asset != null ? AssetDatabase.GetAssetPath(asset) : null;
    }

    private void AddSceneAtEnd(SceneAsset asset)
    {
        var path = GetAssetPath(asset);
        if (string.IsNullOrEmpty(path)) return;
        var name = System.IO.Path.GetFileNameWithoutExtension(path);
        if (_entries.Any(e => e.path == path)) return;
        _entries.Add(new SceneEntry { path = path, name = name, enabled = true });
        _listView.Rebuild();
    }

    private void InsertSceneAt(int index, SceneAsset asset)
    {
        var path = GetAssetPath(asset);
        if (string.IsNullOrEmpty(path)) return;
        index = Mathf.Clamp(index, 0, _entries.Count);
        var name = System.IO.Path.GetFileNameWithoutExtension(path);
        if (_entries.Any(e => e.path == path)) return;
        _entries.Insert(index, new SceneEntry { path = path, name = name, enabled = true });
        _listView.Rebuild();
    }

    private void RemoveAt(int index)
    {
        if (index < 0 || index >= _entries.Count) return;
        _entries.RemoveAt(index);
        _listView.Rebuild();
        _playCursor = Mathf.Clamp(_playCursor, 0, Math.Max(0, _entries.Count - 1));
    }

    private void MoveUp(int index)
    {
        if (index <= 0 || index >= _entries.Count) return;
        var e = _entries[index];
        _entries.RemoveAt(index);
        _entries.Insert(index - 1, e);
        _listView.Rebuild();
        _listView.selectedIndex = index - 1;
    }

    private void MoveDown(int index)
    {
        if (index < 0 || index >= _entries.Count - 1) return;
        var e = _entries[index];
        _entries.RemoveAt(index);
        _entries.Insert(index + 1, e);
        _listView.Rebuild();
        _listView.selectedIndex = index + 1;
    }

    private void OpenSceneAt(int index)
    {
        if (index < 0 || index >= _entries.Count) return;
        var path = _entries[index].path;
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        }
    }

    private void StartAuto()
    {
        if (_entries.Count == 0) return;
        _autoPlaying = true;
        if (_listView.selectedIndex >= 0) _playCursor = _listView.selectedIndex;
        _nextAutoAt = EditorApplication.timeSinceStartup + Mathf.Max(0.2f, _autoDelayField.value);
    }

    private void StopAuto()
    {
        _autoPlaying = false;
    }

    private void StepPrev()
    {
        if (_entries.Count == 0) return;
        _playCursor = Mathf.Max(0, _playCursor - 1);
        OpenSceneAt(_playCursor);
    }

    private void StepNext()
    {
        if (_entries.Count == 0) return;
        _playCursor++;
        if (_playCursor >= _entries.Count)
        {
            if (_autoLoopToggle.value) _playCursor = 0; else { _playCursor = _entries.Count - 1; StopAuto(); return; }
        }
        OpenSceneAt(_playCursor);
    }
}
