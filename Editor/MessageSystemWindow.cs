using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Text;
#if UNITY_EDITOR
using UnityEditor.UIElements;
using UnityEngine.UIElements;
#endif
#if ODIN_INSPECTOR || SIRENIX_ODIN_INSPECTOR
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
#endif
using VInspector;

// 消息系统监控与操作平台（Odin优先），菜单：自制工具/消息系统/消息系统
public class MessageSystemWindow : EditorWindow
{
    // ==== UI Toolkit state ====
    private bool _useUITK = true;
    [SerializeField] private int _logSelectedIndex = -1; // 若未来需要选中行，可持久化
    private TextField _searchKeyField;
    private ListView _keyListView;
    private List<(string key, System.Type payloadType, int subscribers)> _keyItems = new();

    private ListView _typeListView;
    private List<(System.Type payloadType, int subscribers)> _typeItems = new();

    private TextField _keyField;
    private TextField _stringField;
    private IntegerField _intField;
    private FloatField _floatField;

    private Toggle _logToggle;
    private IntegerField _maxLogField;
    private TextField _filterChannelField;
    private TextField _filterTypeField;
    private ListView _logListView;
    private List<MessageManager.MessageLogEntry> _logItems = new();

    private Button _applyLogCfgBtn;
    private Button _clearLogBtn;
    private Button _exportLogBtn;

	private Vector2 _scroll;
	private string _sendKey = "TEST_MESSAGE";
	private string _sendString = "Hello";
	private int _sendInt = 1;
	private float _sendFloat = 1f;
	private string _search = string.Empty;
	private bool _logOn = true;
	private int _maxLog = 500;
    private Vector2 _logScroll;
    private enum SortCol { Frame, Time, Channel, Type, Payload }
    private SortCol _sortCol = SortCol.Frame;
    private bool _asc = false;
    private string _filterChannel = string.Empty;
    private string _filterType = string.Empty;
    private float _wFrame = 70f, _wTime = 70f, _wChannel = 180f, _wType = 140f;
    private int _dragCol = -1;
    private Vector2 _dragStart;
    private float _startWidth;

	[MenuItem("自制工具/消息系统/消息系统")] private static void Open()
	{
		var w = GetWindow<MessageSystemWindow>(false, "消息系统", true);
		w.minSize = new Vector2(560, 480);
	}

#if UNITY_EDITOR
    private void CreateGUI()
    {
        titleContent = new GUIContent("消息系统");
        rootVisualElement.style.paddingLeft = 6;
        rootVisualElement.style.paddingRight = 6;
        rootVisualElement.style.paddingTop = 6;
        rootVisualElement.style.paddingBottom = 6;
        rootVisualElement.Clear();

        // 载入彩色 USS（优先 Assets/Editor/UITK，兼容旧路径）
        var sheet =
            AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Editor/UITK/EditorColors.uss")
            ?? AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Scripts/Editor/UITK/EditorColors.uss")
            ?? AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Scripts/0_Editor/UITK/EditorColors.uss");
        if (sheet != null) rootVisualElement.styleSheets.Add(sheet);
        rootVisualElement.AddToClassList("app-root");

        // 使用垂直分割：上半部分 订阅与发送；下半部分 日志
        var split = new TwoPaneSplitView(0, 320, TwoPaneSplitViewOrientation.Vertical);
        rootVisualElement.Add(split);

        var top = new ScrollView();
        split.Add(top);
        var bottom = new ScrollView();
        split.Add(bottom);

        // --- 上：订阅概览与发送区 ---
        var titleA = new Label("字符串Key通道订阅概览");
        titleA.AddToClassList("section-title");
        top.Add(titleA);
        var keyRow = new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = 4 } };
        _searchKeyField = new TextField("搜索Key") { value = string.Empty };
        _searchKeyField.RegisterValueChangedCallback(_ => RefreshKeySnapshot());
        keyRow.Add(_searchKeyField);
        top.Add(keyRow);

        _keyListView = new ListView
        {
            virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
            selectionType = SelectionType.None,
            showBorder = true
        };
        _keyListView.style.height = 140;
        _keyListView.makeItem = () => new VisualElement { style = { flexDirection = FlexDirection.Row } };
        _keyListView.bindItem = (ve, i) =>
        {
            ve.Clear();
            if (i < 0 || i >= _keyItems.Count) return;
            var item = _keyItems[i];
            ve.AddToClassList((i % 2 == 0) ? "row-even" : "row-odd");
            ve.Add(new Label($"Key: {item.key}") { style = { width = 240 } });
            ve.Add(new Label($"Type: {(item.payloadType != null ? item.payloadType.Name : "null")}") { style = { width = 180 } });
            ve.Add(new Label($"Subs: {item.subscribers}") { style = { flexGrow = 1 } });
            var btn = new Button(() => { MessageManager.ClearKey(item.key); RefreshKeySnapshot(); }) { text = "清空此Key" };
            btn.style.width = 100;
            ve.Add(btn);
        };
        _keyListView.AddToClassList("list");
        top.Add(_keyListView);

        top.Add(new VisualElement { style = { height = 6 } });
        var titleB = new Label("类型通道订阅概览");
        titleB.AddToClassList("section-title");
        top.Add(titleB);
        _typeListView = new ListView
        {
            virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
            selectionType = SelectionType.None,
            showBorder = true
        };
        _typeListView.style.height = 120;
        _typeListView.makeItem = () => new VisualElement { style = { flexDirection = FlexDirection.Row } };
        _typeListView.bindItem = (ve, i) =>
        {
            ve.Clear();
            if (i < 0 || i >= _typeItems.Count) return;
            var item = _typeItems[i];
            ve.AddToClassList((i % 2 == 0) ? "row-even" : "row-odd");
            ve.Add(new Label($"Type: {(item.payloadType != null ? item.payloadType.Name : "null")}  Subs: {item.subscribers}"));
        };
        top.Add(_typeListView);

        top.Add(new VisualElement { style = { height = 6 } });
        var titleC = new Label("测试发送（字符串Key通道）");
        titleC.AddToClassList("section-title");
        top.Add(titleC);
        _keyField = new TextField("Key") { value = _sendKey };
        _stringField = new TextField("String") { value = _sendString };
        _intField = new IntegerField("Int") { value = _sendInt };
        _floatField = new FloatField("Float") { value = _sendFloat };
        top.Add(_keyField); top.Add(_stringField); top.Add(_intField); top.Add(_floatField);

        var sendRow = new VisualElement { style = { flexDirection = FlexDirection.Row } };
        var btnStr = new Button(() => MessageManager.Send<string>(_keyField.value, _stringField.value)) { text = "Send String" };
        var btnInt = new Button(() => MessageManager.Send<int>(_keyField.value, _intField.value)) { text = "Send Int" };
        var btnFlt = new Button(() => MessageManager.Send<float>(_keyField.value, _floatField.value)) { text = "Send Float" };
        btnStr.AddToClassList("btn"); btnStr.AddToClassList("btn-primary");
        btnInt.AddToClassList("btn"); btnInt.AddToClassList("btn-secondary");
        btnFlt.AddToClassList("btn"); btnFlt.AddToClassList("btn-success");
        sendRow.Add(btnStr);
        sendRow.Add(btnInt);
        sendRow.Add(btnFlt);
        top.Add(sendRow);

        top.Add(new VisualElement { style = { height = 6 } });
        var titleD = new Label("测试发送（类型通道）");
        titleD.AddToClassList("section-title");
        top.Add(titleD);
        var sendTypeRow = new VisualElement { style = { flexDirection = FlexDirection.Row } };
        var btnT1 = new Button(() => MessageManager.Send<string>(_stringField.value)) { text = "Send 类型: string" };
        var btnT2 = new Button(() => MessageManager.Send<int>(_intField.value)) { text = "Send 类型: int" };
        var btnT3 = new Button(() => MessageManager.Send<float>(_floatField.value)) { text = "Send 类型: float" };
        btnT1.AddToClassList("btn"); btnT1.AddToClassList("btn-primary");
        btnT2.AddToClassList("btn"); btnT2.AddToClassList("btn-secondary");
        btnT3.AddToClassList("btn"); btnT3.AddToClassList("btn-success");
        sendTypeRow.Add(btnT1); sendTypeRow.Add(btnT2); sendTypeRow.Add(btnT3);
        top.Add(sendTypeRow);

        top.Add(new VisualElement { style = { height = 6 } });
        var clearAllBtn = new Button(() => { MessageManager.Clear(); RefreshKeySnapshot(); RefreshTypeSnapshot(); RefreshLogs(); }) { text = "清空全部" };
        clearAllBtn.AddToClassList("btn"); clearAllBtn.AddToClassList("btn-danger");
        top.Add(clearAllBtn);

        // --- 下：日志区 ---
        var titleE = new Label("实时消息日志(时间戳/帧号)");
        titleE.AddToClassList("section-title");
        bottom.Add(titleE);
        var cfgRow = new VisualElement { style = { flexDirection = FlexDirection.Row } };
        _logToggle = new Toggle("启用日志") { value = _logOn };
        _maxLogField = new IntegerField("最大条数") { value = _maxLog };
        _applyLogCfgBtn = new Button(() => { _logOn = _logToggle.value; _maxLog = Mathf.Max(1, _maxLogField.value); MessageManager.SetLogEnabled(_logOn); MessageManager.SetMaxLog(_maxLog); RefreshLogs(); }) { text = "应用" };
        _clearLogBtn = new Button(() => { MessageManager.ClearLog(); RefreshLogs(); }) { text = "清空日志" };
        _applyLogCfgBtn.AddToClassList("btn"); _applyLogCfgBtn.AddToClassList("btn-success");
        _clearLogBtn.AddToClassList("btn"); _clearLogBtn.AddToClassList("btn-warn");
        cfgRow.Add(_logToggle);
        cfgRow.Add(_maxLogField);
        cfgRow.Add(_applyLogCfgBtn);
        cfgRow.Add(_clearLogBtn);
        bottom.Add(cfgRow);

        var filterRow = new VisualElement { style = { flexDirection = FlexDirection.Row } };
        _filterChannelField = new TextField("按Channel过滤") { value = _filterChannel };
        _filterTypeField = new TextField("按Type过滤") { value = _filterType };
        _exportLogBtn = new Button(() => ExportJson(MessageManager.GetLogSnapshot())) { text = "导出JSON" };
        _exportLogBtn.AddToClassList("btn"); _exportLogBtn.AddToClassList("btn-primary");
        _filterChannelField.RegisterValueChangedCallback(_ => { _filterChannel = _filterChannelField.value; RefreshLogs(); });
        _filterTypeField.RegisterValueChangedCallback(_ => { _filterType = _filterTypeField.value; RefreshLogs(); });
        filterRow.Add(_filterChannelField);
        filterRow.Add(_filterTypeField);
        filterRow.Add(_exportLogBtn);
        bottom.Add(filterRow);

        _logListView = new ListView
        {
            virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
            selectionType = SelectionType.Single,
            showBorder = true
        };
        _logListView.style.height = 260;
        _logListView.makeItem = () => new VisualElement { style = { flexDirection = FlexDirection.Row } };
        _logListView.bindItem = (ve, i) =>
        {
            ve.Clear();
            if (i < 0 || i >= _logItems.Count) return;
            var e = _logItems[i];
            ve.AddToClassList((i % 2 == 0) ? "row-even" : "row-odd");
            ve.Add(new Label(e.frame.ToString()) { style = { width = _wFrame } });
            ve.Add(new Label(e.time.ToString("F3")) { style = { width = _wTime } });
            ve.Add(new Label(e.channel) { style = { width = _wChannel } });
            ve.Add(new Label(e.typeName) { style = { width = _wType } });
            var payload = new Label(e.payload) { style = { flexGrow = 1 } };
            payload.style.whiteSpace = WhiteSpace.Normal;
            ve.Add(payload);
        };
        _logListView.selectionChanged += _ => { _logSelectedIndex = _logListView.selectedIndex; };
        bottom.Add(_logListView);

        // 初始填充
        RefreshKeySnapshot();
        RefreshTypeSnapshot();
        RefreshLogs();
    }

    private void RefreshKeySnapshot()
    {
        var sSnap = MessageManager.GetStringBusSnapshot();
        _keyItems.Clear();
        string q = _searchKeyField != null ? _searchKeyField.value.Trim().ToLower() : string.Empty;
        for (int i = 0; i < sSnap.Count; i++)
        {
            if (!string.IsNullOrEmpty(q) && (sSnap[i].key == null || !sSnap[i].key.ToLower().Contains(q))) continue;
            _keyItems.Add((sSnap[i].key, sSnap[i].payloadType, sSnap[i].subscribers));
        }
        if (_keyListView != null) { _keyListView.itemsSource = _keyItems; _keyListView.Rebuild(); }
    }

    private void RefreshTypeSnapshot()
    {
        var tSnap = MessageManager.GetTypeBusSnapshot();
        _typeItems.Clear();
        for (int i = 0; i < tSnap.Count; i++)
        {
            _typeItems.Add((tSnap[i].payloadType, tSnap[i].subscribers));
        }
        if (_typeListView != null) { _typeListView.itemsSource = _typeItems; _typeListView.Rebuild(); }
    }

    private void RefreshLogs()
    {
        var logs = MessageManager.GetLogSnapshot();
        // filter
        var filtered = new List<MessageManager.MessageLogEntry>(logs.Count);
        for (int i = 0; i < logs.Count; i++)
        {
            var e = logs[i];
            if (!string.IsNullOrEmpty(_filterChannel) && (e.channel == null || e.channel.IndexOf(_filterChannel, System.StringComparison.OrdinalIgnoreCase) < 0)) continue;
            if (!string.IsNullOrEmpty(_filterType) && (e.typeName == null || e.typeName.IndexOf(_filterType, System.StringComparison.OrdinalIgnoreCase) < 0)) continue;
            filtered.Add(e);
        }
        // sort
        filtered.Sort((a, b) =>
        {
            int cmp = 0;
            switch (_sortCol)
            {
                case SortCol.Frame: cmp = a.frame.CompareTo(b.frame); break;
                case SortCol.Time: cmp = a.time.CompareTo(b.time); break;
                case SortCol.Channel: cmp = string.Compare(a.channel, b.channel, System.StringComparison.Ordinal); break;
                case SortCol.Type: cmp = string.Compare(a.typeName, b.typeName, System.StringComparison.Ordinal); break;
                case SortCol.Payload: cmp = string.Compare(a.payload, b.payload, System.StringComparison.Ordinal); break;
            }
            return _asc ? cmp : -cmp;
        });
        _logItems = filtered;
        if (_logListView != null) { _logListView.itemsSource = _logItems; _logListView.Rebuild(); }
    }
#endif

    private void OnGUI()
	{
        if (_useUITK) return; // 使用 UITK 渲染，避免与 IMGUI 叠加
		_scroll = EditorGUILayout.BeginScrollView(_scroll);

#if ODIN_INSPECTOR || SIRENIX_ODIN_INSPECTOR
		SirenixEditorGUI.Title("字符串Key通道订阅概览", null, TextAlignment.Left, true);
#else
		GUILayout.Label("字符串Key通道订阅概览", EditorStyles.boldLabel);
#endif
		_search = EditorGUILayout.TextField("搜索Key", _search);
		var sSnap = MessageManager.GetStringBusSnapshot();
		for (int i = 0; i < sSnap.Count; i++)
		{
			if (!string.IsNullOrEmpty(_search) && !sSnap[i].key.ToLower().Contains(_search.ToLower())) continue;
			GUILayout.BeginHorizontal();
			GUILayout.Label($"Key: {sSnap[i].key}", GUILayout.Width(240));
			GUILayout.Label($"Type: {(sSnap[i].payloadType != null ? sSnap[i].payloadType.Name : "null")}", GUILayout.Width(180));
			GUILayout.Label($"Subs: {sSnap[i].subscribers}");
			if (GUILayout.Button("清空此Key", GUILayout.Width(100))) MessageManager.ClearKey(sSnap[i].key);
			GUILayout.EndHorizontal();
		}

		GUILayout.Space(8);
#if ODIN_INSPECTOR || SIRENIX_ODIN_INSPECTOR
		SirenixEditorGUI.Title("类型通道订阅概览", null, TextAlignment.Left, true);
#else
		GUILayout.Label("类型通道订阅概览", EditorStyles.boldLabel);
#endif
		var tSnap = MessageManager.GetTypeBusSnapshot();
		for (int i = 0; i < tSnap.Count; i++)
		{
			GUILayout.Label($"Type: {tSnap[i].payloadType.Name}  Subs: {tSnap[i].subscribers}");
		}

		GUILayout.Space(8);
#if ODIN_INSPECTOR || SIRENIX_ODIN_INSPECTOR
		SirenixEditorGUI.Title("测试发送（字符串Key通道）", null, TextAlignment.Left, true);
#else
		GUILayout.Label("测试发送（字符串Key通道）", EditorStyles.boldLabel);
#endif
		_sendKey = EditorGUILayout.TextField("Key", _sendKey);
		_sendString = EditorGUILayout.TextField("String", _sendString);
		_sendInt = EditorGUILayout.IntField("Int", _sendInt);
		_sendFloat = EditorGUILayout.FloatField("Float", _sendFloat);
		EditorGUILayout.BeginHorizontal();
		if (GUILayout.Button("Send String")) MessageManager.Send<string>(_sendKey, _sendString);
		if (GUILayout.Button("Send Int")) MessageManager.Send<int>(_sendKey, _sendInt);
		if (GUILayout.Button("Send Float")) MessageManager.Send<float>(_sendKey, _sendFloat);
		EditorGUILayout.EndHorizontal();

		GUILayout.Space(8);
#if ODIN_INSPECTOR || SIRENIX_ODIN_INSPECTOR
		SirenixEditorGUI.Title("测试发送（类型通道）", null, TextAlignment.Left, true);
#else
		GUILayout.Label("测试发送（类型通道）", EditorStyles.boldLabel);
#endif
		if (GUILayout.Button("Send 类型: string")) MessageManager.Send<string>(_sendString);
		if (GUILayout.Button("Send 类型: int")) MessageManager.Send<int>(_sendInt);
		if (GUILayout.Button("Send 类型: float")) MessageManager.Send<float>(_sendFloat);

		GUILayout.Space(8);
		if (GUILayout.Button("清空全部")) MessageManager.Clear();

		GUILayout.Space(12);
#if ODIN_INSPECTOR || SIRENIX_ODIN_INSPECTOR
		SirenixEditorGUI.Title("实时消息日志(时间戳/帧号)", null, TextAlignment.Left, true);
#else
		GUILayout.Label("实时消息日志(时间戳/帧号)", EditorStyles.boldLabel);
#endif
		EditorGUILayout.BeginHorizontal();
		_logOn = EditorGUILayout.ToggleLeft("启用日志", _logOn, GUILayout.Width(100));
		_maxLog = EditorGUILayout.IntField("最大条数", _maxLog, GUILayout.Width(220));
		if (GUILayout.Button("应用", GUILayout.Width(60))) { MessageManager.SetLogEnabled(_logOn); MessageManager.SetMaxLog(_maxLog); }
		if (GUILayout.Button("清空日志", GUILayout.Width(80))) { MessageManager.ClearLog(); }
		EditorGUILayout.EndHorizontal();

		var logs = MessageManager.GetLogSnapshot();
		// 排序
		logs.Sort((a, b) =>
		{
			int cmp = 0;
			switch (_sortCol)
			{
				case SortCol.Frame: cmp = a.frame.CompareTo(b.frame); break;
				case SortCol.Time: cmp = a.time.CompareTo(b.time); break;
				case SortCol.Channel: cmp = string.Compare(a.channel, b.channel, System.StringComparison.Ordinal); break;
				case SortCol.Type: cmp = string.Compare(a.typeName, b.typeName, System.StringComparison.Ordinal); break;
				case SortCol.Payload: cmp = string.Compare(a.payload, b.payload, System.StringComparison.Ordinal); break;
			}
			return _asc ? cmp : -cmp;
		});

        // 表头 + 可拖拽列宽
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        HeaderButton("Frame", SortCol.Frame, _wFrame, 0);
        HeaderSplitter(0);
        HeaderButton("Time(s)", SortCol.Time, _wTime, 1);
        HeaderSplitter(1);
        HeaderButton("Channel", SortCol.Channel, _wChannel, 2);
        HeaderSplitter(2);
        HeaderButton("Type", SortCol.Type, _wType, 3);
        HeaderSplitter(3);
        HeaderButton("Payload", SortCol.Payload, 0, -1);
        EditorGUILayout.EndHorizontal();

        // 过滤与导出
        EditorGUILayout.BeginHorizontal();
        _filterChannel = EditorGUILayout.TextField("按Channel过滤", _filterChannel);
        _filterType = EditorGUILayout.TextField("按Type过滤", _filterType);
        if (GUILayout.Button("导出JSON", GUILayout.Width(100))) ExportJson(logs);
        EditorGUILayout.EndHorizontal();

		// 行滚动
		_logScroll = EditorGUILayout.BeginScrollView(_logScroll, GUILayout.Height(220));
        for (int i = 0; i < logs.Count; i++)
		{
			var e = logs[i];
            if (!string.IsNullOrEmpty(_filterChannel) && (e.channel == null || e.channel.IndexOf(_filterChannel, System.StringComparison.OrdinalIgnoreCase) < 0)) continue;
            if (!string.IsNullOrEmpty(_filterType) && (e.typeName == null || e.typeName.IndexOf(_filterType, System.StringComparison.OrdinalIgnoreCase) < 0)) continue;
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(e.frame.ToString(), GUILayout.Width(_wFrame));
            GUILayout.Label(e.time.ToString("F3"), GUILayout.Width(_wTime));
            GUILayout.Label(e.channel, GUILayout.Width(_wChannel));
            GUILayout.Label(e.typeName, GUILayout.Width(_wType));
			GUILayout.Label(e.payload);
			EditorGUILayout.EndHorizontal();
		}
		EditorGUILayout.EndScrollView();

		EditorGUILayout.EndScrollView();
	}

    private void HeaderButton(string title, SortCol col, float width, int colIndex)
	{
        var style = EditorStyles.toolbarButton;
        GUIContent gc = new GUIContent(title + (_sortCol == col ? (_asc ? " ▲" : " ▼") : string.Empty));
        GUILayoutOption opt = (col == SortCol.Payload || width <= 0f) ? GUILayout.ExpandWidth(true) : GUILayout.Width(width);
        Rect r = GUILayoutUtility.GetRect(gc, style, opt);
        if (GUI.Button(r, gc, style))
        {
            if (_sortCol == col) _asc = !_asc; else { _sortCol = col; _asc = false; }
        }
	}

    private void HeaderSplitter(int colIndex)
    {
        Rect rect = GUILayoutUtility.GetRect(4f, 16f, GUILayout.Width(4f));
        EditorGUIUtility.AddCursorRect(rect, MouseCursor.SplitResizeLeftRight);
        int controlId = GUIUtility.GetControlID(FocusType.Passive);
        Event e = Event.current;
        switch (e.rawType)
        {
            case EventType.MouseDown:
                if (rect.Contains(e.mousePosition))
                {
                    _dragCol = colIndex;
                    _dragStart = e.mousePosition;
                    _startWidth = GetColWidth(colIndex);
                    GUIUtility.hotControl = controlId;
                    e.Use();
                }
                break;
            case EventType.MouseDrag:
                if (GUIUtility.hotControl == controlId && _dragCol == colIndex)
                {
                    float delta = e.mousePosition.x - _dragStart.x;
                    SetColWidth(colIndex, Mathf.Max(40f, _startWidth + delta));
                    Repaint();
                    e.Use();
                }
                break;
            case EventType.MouseUp:
                if (GUIUtility.hotControl == controlId)
                {
                    GUIUtility.hotControl = 0;
                    _dragCol = -1;
                    e.Use();
                }
                break;
        }
        EditorGUI.DrawRect(new Rect(rect.x + 1, rect.y, 1, rect.height), new Color(0, 0, 0, 0.35f));
    }

    private float GetColWidth(int idx)
    {
        switch (idx)
        {
            case 0: return _wFrame;
            case 1: return _wTime;
            case 2: return _wChannel;
            case 3: return _wType;
        }
        return 100f;
    }

    private void SetColWidth(int idx, float w)
    {
        switch (idx)
        {
            case 0: _wFrame = w; break;
            case 1: _wTime = w; break;
            case 2: _wChannel = w; break;
            case 3: _wType = w; break;
        }
    }

    private void ExportJson(List<MessageManager.MessageLogEntry> logs)
    {
        string path = EditorUtility.SaveFilePanel("导出消息日志为JSON", Application.dataPath, "MessageLogs.json", "json");
        if (string.IsNullOrEmpty(path)) return;
        try
        {
            var sb = new StringBuilder();
            sb.Append('[');
            for (int i = 0; i < logs.Count; i++)
            {
                var e = logs[i];
                sb.Append('{');
                sb.AppendFormat("\"time\":{0},\"frame\":{1},\"channel\":\"{2}\",\"type\":\"{3}\",\"payload\":\"{4}\"",
                    System.Convert.ToString(e.time, System.Globalization.CultureInfo.InvariantCulture),
                    e.frame,
                    EscapeJson(e.channel),
                    EscapeJson(e.typeName),
                    EscapeJson(e.payload));
                sb.Append('}');
                if (i < logs.Count - 1) sb.Append(',');
            }
            sb.Append(']');
            File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
            EditorUtility.RevealInFinder(path);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("导出JSON失败: " + ex.Message);
        }
    }

    private static string EscapeJson(string s)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        s = s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
        return s;
    }
}
