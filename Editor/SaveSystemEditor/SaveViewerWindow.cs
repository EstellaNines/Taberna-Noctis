using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;

public class SaveViewerWindow : OdinEditorWindow
{
    private const string MENU_PATH = "自制工具/保存系统/存档监控器";

    [MenuItem(MENU_PATH)]
    private static void Open()
    {
        var w = GetWindow<SaveViewerWindow>(false, "Save Viewer", true);
        w.Show();
    }

    private readonly List<SlotEntry> _entries = new List<SlotEntry>();
    private bool _pendingRefresh;
    private double _nextRefreshAt;

    [BoxGroup("状态"), ShowInInspector, ReadOnly, LabelText("存档文件路径")]
    private string saveFilePath => Path.Combine(Application.persistentDataPath, "ES3File.es3");

    [BoxGroup("刷新"), ShowInInspector, LabelText("自动刷新")]
    public bool autoRefresh = true;

    [BoxGroup("刷新"), ShowInInspector, LabelText("刷新间隔(秒)"), PropertyRange(0.2, 5)]
    public float refreshInterval = 1.0f;

    [BoxGroup("刷新"), Button("立即刷新")]
    private void DoRefresh() { RefreshEntries(); }

    [Title("存档槽位 (1..3)")]
    [ShowInInspector]
    [TableList(AlwaysExpanded = true, IsReadOnly = true)]
    private List<SlotRow> rows = new List<SlotRow>();

    // ==== UI Toolkit state ====
    private bool _useUITK = true;
    [SerializeField] private int _selectedSlotIndex = -1; // 热重载后恢复选择
    private ListView _slotListView;
    private DropdownField _slotDropdown;
    private TextField _jsonField;
    private Label _filePathLabel;
    private Toggle _autoRefreshToggle;
    private Slider _refreshIntervalSlider;
    private Button _refreshBtn;
    private Button _deleteBtn;
    private Button _clearAllBtn;

    // 中文字段映射与取值器（用于行内下拉）
    public static readonly (string key, string label, Func<SaveData, string> getter)[] DataFields = new (string, string, Func<SaveData, string>)[]
    {
        ("saveSlotID", "存档槽位ID", d => d != null ? d.saveSlotID.ToString() : ""),
        ("saveSlotName", "存档名", d => d?.saveSlotName ?? ""),
        ("lastSaveDateTime", "最后保存时间", d => d?.lastSaveDateTime ?? ""),
        ("totalPlayTimeSeconds", "总游玩秒数", d => d != null ? d.totalPlayTimeSeconds.ToString("F0") : "0"),
        ("saveVersion", "存档版本", d => d != null ? d.saveVersion.ToString() : "0"),
        ("currentDay", "当前天数", d => d != null ? d.currentDay.ToString() : "0"),
        ("currentPhase", "当前阶段", d => d != null ? d.currentPhase.ToString() : ""),
        ("daySubPhase", "白天子阶段", d => d != null ? d.daySubPhase.ToString() : ""),
        ("clock", "时钟(时:分)", d => d != null ? $"{d.clockHour:D2}:{d.clockMinute:D2}" : "00:00"),
        ("phaseRemainingTime", "阶段剩余秒", d => d != null ? d.phaseRemainingTime.ToString("F0") : "0"),
        ("currentMoney", "当前金币", d => d != null ? d.currentMoney.ToString() : "0"),
        ("todayIncome", "今日收入", d => d != null ? d.todayIncome.ToString() : "0"),
        ("todayExpense", "今日支出", d => d != null ? d.todayExpense.ToString() : "0"),
        ("todayAverageScore", "今日平均评分", d => d != null ? d.todayAverageScore.ToString("F1") : "0"),
        ("starRating", "星级", d => d != null ? d.starRating.ToString() : "0"),
        ("consecutivePerfectDays", "累计完美天数", d => d != null ? d.consecutivePerfectDays.ToString() : "0"),
    };

    // 选中查看的槽位
    [Title("详情与操作")]
    [ShowInInspector, LabelText("选择槽位"), ValueDropdown(nameof(GetSlotDropdown))]
    private string selectedSlot = "1";

    // 多列化视图（只读镜像）
    [ShowInInspector, LabelText("数据详情"), InlineProperty, HideLabel]
    private SaveDataView selectedView = new SaveDataView();

    // 独立 JSON 查看（可滚动且高度更大）
    [ShowInInspector, LabelText("JSON 预览"), MultiLineProperty(18)]
    private string selectedJson = string.Empty;

    // 操作区
    [HorizontalGroup("ops", 0.5f), Button("删除所选槽位")]
    private void DeleteSelectedSlot()
    {
        if (EditorUtility.DisplayDialog("删除确认", $"确定删除槽位 {selectedSlot} 吗？", "删除", "取消"))
        {
            if (SaveManager.Instance != null) SaveManager.Instance.DeleteSaveSlot(selectedSlot);
            // 也清理备份
            var key = "save_slot_" + selectedSlot + "_backup";
            if (ES3.KeyExists(key)) ES3.DeleteKey(key);
            RefreshEntries();
        }
    }

    [HorizontalGroup("ops"), Button("清理全部存档文件")]
    private void ClearAllSaveFiles()
    {
        if (!EditorUtility.DisplayDialog("危险操作", "将删除全部槽位与 ES3 文件，确定继续？", "是", "否")) return;
        for (int i = 1; i <= 3; i++)
        {
            var key = "save_slot_" + i;
            var backup = key + "_backup";
            if (ES3.KeyExists(key)) ES3.DeleteKey(key);
            if (ES3.KeyExists(backup)) ES3.DeleteKey(backup);
        }
        try { if (File.Exists(saveFilePath)) File.Delete(saveFilePath); } catch { }
        AssetDatabase.Refresh();
        RefreshEntries();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        // 监听保存事件，实时刷新
        MessageManager.Register<string>(MessageDefine.SAVE_COMPLETED, OnSaveEvent);
        MessageManager.Register<string>(MessageDefine.SAVE_LOADED, OnSaveEvent);
        EditorApplication.update += OnEditorUpdate;
        RefreshEntries();
        if (_useUITK)
        {
            if (rootVisualElement.childCount == 0) CreateGUI();
            else RefreshUITK();
        }
    }

    // 关闭 Odin 的 IMGUI 输出，避免与 UITK 叠加导致“文字错位/重复”
    protected override void OnImGUI()
    {
        if (_useUITK) return; // UITK 渲染
        base.OnImGUI();        // 回落到 Odin IMGUI
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        MessageManager.Remove<string>(MessageDefine.SAVE_COMPLETED, OnSaveEvent);
        MessageManager.Remove<string>(MessageDefine.SAVE_LOADED, OnSaveEvent);
        EditorApplication.update -= OnEditorUpdate;
    }

    private void OnSaveEvent(string _)
    {
        _pendingRefresh = true;
        Repaint();
    }

    private void OnEditorUpdate()
    {
        if (_pendingRefresh || (autoRefresh && EditorApplication.timeSinceStartup >= _nextRefreshAt))
        {
            _pendingRefresh = false;
            _nextRefreshAt = EditorApplication.timeSinceStartup + refreshInterval;
            RefreshEntries();
            Repaint();
        }
    }

    private void RefreshEntries()
    {
        _entries.Clear();
        rows.Clear();
        for (int i = 1; i <= 3; i++)
        {
            var slotId = i.ToString();
            var key = "save_slot_" + slotId;
            bool exists = ES3.KeyExists(key);
            SaveData data = null;
            string json = string.Empty;
            if (exists)
            {
                try
                {
                    data = ES3.Load<SaveData>(key);
                    json = JsonUtility.ToJson(data, true);
                }
                catch (Exception e)
                {
                    json = "<Load Error> " + e.Message;
                }
            }
            _entries.Add(new SlotEntry { slotId = slotId, exists = exists, data = data, json = json });
        }
        BuildRows();
        UpdateSelectedDetails();
        RefreshUITK();
    }

    private void BuildRows()
    {
        foreach (var e in _entries)
        {
            var row = new SlotRow();
            row.Slot = e.slotId;
            row.Exists = e.exists;
            row.Day = e.data != null ? e.data.currentDay : 0;
            row.Phase = e.data != null ? e.data.currentPhase.ToString() : "--";
            row.Money = e.data != null ? e.data.currentMoney : 0;
            row.Stars = e.data != null ? e.data.starRating : 0;
            row.Data = e.data;
            row.DataKey = row.DataKey ?? "currentDay";
            row.Json = e.json;
            rows.Add(row);
        }
    }

    private IEnumerable<ValueDropdownItem<string>> GetSlotDropdown()
    {
        if (_entries == null || _entries.Count == 0)
        {
            for (int i = 1; i <= 3; i++) yield return new ValueDropdownItem<string>($"槽位{i}", i.ToString());
            yield break;
        }
        foreach (var e in _entries) yield return new ValueDropdownItem<string>($"槽位{e.slotId}", e.slotId);
    }

    private void UpdateSelectedDetails()
    {
        var e = _entries.FirstOrDefault(x => x.slotId == selectedSlot) ?? _entries.FirstOrDefault();
        if (e != null)
        {
            selectedSlot = e.slotId;
            selectedView.From(e.data);
            selectedJson = e.json ?? string.Empty;
        }
        else
        {
            selectedJson = string.Empty;
            selectedView.From(null);
        }
    }

    private class SlotEntry
    {
        public string slotId;
        public bool exists;
        public SaveData data;
        public string json;
    }

    // ================= UI Toolkit =================
    private void CreateGUI()
    {
        titleContent = new GUIContent("Save Viewer");
        var root = rootVisualElement;
        root.style.paddingLeft = 6; root.style.paddingRight = 6; root.style.paddingTop = 6; root.style.paddingBottom = 6;
        root.Clear();

        // 载入彩色 USS（优先 Editor/，兼容旧路径）
        var sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Editor/UITK/EditorColors.uss")
                   ?? AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Scripts/Editor/UITK/EditorColors.uss")
                   ?? AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Scripts/0_Editor/UITK/EditorColors.uss");
        if (sheet != null) root.styleSheets.Add(sheet);
        root.AddToClassList("app-root");

        // Header: file path + refresh controls
        var header = new VisualElement { style = { flexDirection = FlexDirection.Column, marginBottom = 6 } };
        header.AddToClassList("section");
        _filePathLabel = new Label($"存档文件路径: {saveFilePath}");
        header.Add(_filePathLabel);
        var refreshRow = new VisualElement { style = { flexDirection = FlexDirection.Row, marginTop = 4 } };
        refreshRow.AddToClassList("toolbar-row");
        _autoRefreshToggle = new Toggle("自动刷新") { value = autoRefresh };
        _autoRefreshToggle.RegisterValueChangedCallback(e => autoRefresh = e.newValue);
        _refreshIntervalSlider = new Slider("刷新间隔(秒)", 0.2f, 5f) { value = refreshInterval, style = { width = 260 } };
        _refreshIntervalSlider.RegisterValueChangedCallback(e => refreshInterval = e.newValue);
        _refreshBtn = new Button(() => DoRefresh()) { text = "立即刷新" };
        _refreshBtn.AddToClassList("btn"); _refreshBtn.AddToClassList("btn-primary");
        refreshRow.Add(_autoRefreshToggle);
        refreshRow.Add(_refreshIntervalSlider);
        refreshRow.Add(_refreshBtn);
        header.Add(refreshRow);
        root.Add(header);

        // Slots list (1..3)
        _slotListView = new ListView
        {
            selectionType = SelectionType.Single,
            virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
            showBorder = true,
            style = { height = 180 }
        };
        _slotListView.AddToClassList("list");
        _slotListView.makeItem = () => new VisualElement { style = { flexDirection = FlexDirection.Row } };
        _slotListView.bindItem = (ve, i) =>
        {
            ve.Clear();
            ve.AddToClassList((i % 2 == 0) ? "row-even" : "row-odd");
            var e = _entries[i];
            ve.Add(new Label($"槽位 {e.slotId}") { style = { width = 70 } });
            ve.Add(new Label($"存在: {(e.exists ? "是" : "否")}") { style = { width = 80 } });
            ve.Add(new Label($"天数: {(e.data != null ? e.data.currentDay.ToString() : "0")}") { style = { width = 80 } });
            ve.Add(new Label($"阶段: {(e.data != null ? e.data.currentPhase.ToString() : "--")}") { style = { width = 140 } });
            ve.Add(new Label($"金币: {(e.data != null ? e.data.currentMoney.ToString() : "0")}") { style = { width = 80 } });
            ve.Add(new Label($"星级: {(e.data != null ? e.data.starRating.ToString() : "0")}") { style = { width = 60 } });
        };
        _slotListView.selectionChanged += sel =>
        {
            var idx = _slotListView.selectedIndex;
            if (idx >= 0 && idx < _entries.Count)
            {
                selectedSlot = _entries[idx].slotId;
                _selectedSlotIndex = idx;
                UpdateSelectedDetails();
                RefreshUITK();
            }
        };
        root.Add(_slotListView);

        // Ops row
        var ops = new VisualElement { style = { flexDirection = FlexDirection.Row, marginTop = 6 } };
        _deleteBtn = new Button(() => DeleteSelectedSlot()) { text = "删除所选槽位" };
        _clearAllBtn = new Button(() => ClearAllSaveFiles()) { text = "清理全部存档文件" };
        _deleteBtn.AddToClassList("btn"); _deleteBtn.AddToClassList("btn-danger");
        _clearAllBtn.AddToClassList("btn"); _clearAllBtn.AddToClassList("btn-warn");
        ops.Add(_deleteBtn); ops.Add(_clearAllBtn);
        root.Add(ops);

        // Details (selected)
        var details = new VisualElement { style = { flexDirection = FlexDirection.Column, marginTop = 6 } };
        _slotDropdown = new DropdownField("选择槽位", new List<string> { "1", "2", "3" }, selectedSlot);
        _slotDropdown.RegisterValueChangedCallback(e => { selectedSlot = e.newValue; UpdateSelectedDetails(); RefreshUITK(); });
        details.Add(_slotDropdown);
        _jsonField = new TextField("JSON 预览") { multiline = true };
        _jsonField.style.height = 300;
        details.Add(_jsonField);
        root.Add(details);

        RefreshUITK();
    }

    private void RefreshUITK()
    {
        if (!_useUITK || rootVisualElement == null || rootVisualElement.childCount == 0) return;
        if (_slotListView != null)
        {
            _slotListView.itemsSource = _entries;
            _slotListView.Rebuild();
            if (_selectedSlotIndex >= 0 && _selectedSlotIndex < _entries.Count)
                _slotListView.selectedIndex = _selectedSlotIndex;
        }
        if (_slotDropdown != null) _slotDropdown.value = selectedSlot;
        if (_jsonField != null)
        {
            var e = _entries.FirstOrDefault(x => x.slotId == selectedSlot) ?? _entries.FirstOrDefault();
            _jsonField.value = e != null ? (e.json ?? string.Empty) : string.Empty;
        }
        if (_filePathLabel != null) _filePathLabel.text = $"存档文件路径: {saveFilePath}";
        if (_autoRefreshToggle != null) _autoRefreshToggle.value = autoRefresh;
        if (_refreshIntervalSlider != null) _refreshIntervalSlider.value = refreshInterval;
    }
}

[Serializable]
public class SlotRow
{
    [TableColumnWidth(40, Resizable = false), LabelText("槽位")] public string Slot;
    [TableColumnWidth(60, Resizable = false), LabelText("存在")] public bool Exists;
    [TableColumnWidth(50, Resizable = false), LabelText("天数")] public int Day;
    [TableColumnWidth(80), LabelText("阶段")] public string Phase;
    [TableColumnWidth(70, Resizable = false), LabelText("金币")] public int Money;
    [TableColumnWidth(50, Resizable = false), LabelText("星级")] public int Stars;

    // 行内下拉：只显示一个关键数据（减少拥挤），中文字段名
    [TableColumnWidth(260, Resizable = true), LabelText("数据(下拉查看)")]
    [ValueDropdown("GetDataDropdown"), ShowInInspector]
    public string DataKey;

    [ShowInInspector, HideInTables]
    [InlineProperty]
    public SaveData Data;

    [TableColumnWidth(0, Resizable = true), LabelText("JSON 预览")]
    [MultiLineProperty(10)]
    public string Json;

    private IEnumerable<ValueDropdownItem<string>> GetDataDropdown()
    {
        foreach (var f in SaveViewerWindow.DataFields)
        {
            var value = f.getter?.Invoke(Data) ?? string.Empty;
            yield return new ValueDropdownItem<string>($"{f.label}: {value}", f.key);
        }
    }
}

// ========== 详情面板的只读视图（多列显示 + 中文标签） ==========
[Serializable]
public class SaveDataView
{
    [HorizontalGroup("r1"), LabelText("存档槽位ID"), ReadOnly] public string saveSlotID;
    [HorizontalGroup("r1"), LabelText("存档名"), ReadOnly] public string saveSlotName;
    [HorizontalGroup("r1"), LabelText("最后保存时间"), ReadOnly] public string lastSaveDateTime;

    [HorizontalGroup("r2"), LabelText("总游玩秒数"), ReadOnly] public double totalPlayTimeSeconds;
    [HorizontalGroup("r2"), LabelText("存档版本"), ReadOnly] public int saveVersion;
    [HorizontalGroup("r2"), LabelText("当前天数"), ReadOnly] public int currentDay;

    [HorizontalGroup("r3"), LabelText("当前阶段"), ReadOnly] public string currentPhase;
    [HorizontalGroup("r3"), LabelText("白天子阶段"), ReadOnly] public string daySubPhase;
    [HorizontalGroup("r3"), LabelText("时钟(时)"), ReadOnly] public int clockHour;

    [HorizontalGroup("r4"), LabelText("时钟(分)"), ReadOnly] public int clockMinute;
    [HorizontalGroup("r4"), LabelText("阶段剩余秒"), ReadOnly] public float phaseRemainingTime;
    [HorizontalGroup("r4"), LabelText("当前金币"), ReadOnly] public int currentMoney;

    [HorizontalGroup("r5"), LabelText("今日收入"), ReadOnly] public int todayIncome;
    [HorizontalGroup("r5"), LabelText("今日支出"), ReadOnly] public int todayExpense;
    [HorizontalGroup("r5"), LabelText("今日平均评分"), ReadOnly] public float todayAverageScore;

    [HorizontalGroup("r6"), LabelText("星级"), ReadOnly] public int starRating;
    [HorizontalGroup("r6"), LabelText("累计完美天数"), ReadOnly] public int consecutivePerfectDays;

    public void From(SaveData data)
    {
        if (data == null)
        {
            saveSlotID = saveSlotName = lastSaveDateTime = currentPhase = daySubPhase = string.Empty;
            totalPlayTimeSeconds = 0; saveVersion = 0; currentDay = 0; clockHour = 0; clockMinute = 0;
            phaseRemainingTime = 0; currentMoney = 0; todayIncome = 0; todayExpense = 0; todayAverageScore = 0;
            starRating = 0; consecutivePerfectDays = 0;
            return;
        }
        saveSlotID = data.saveSlotID.ToString();
        saveSlotName = data.saveSlotName;
        lastSaveDateTime = data.lastSaveDateTime;
        totalPlayTimeSeconds = data.totalPlayTimeSeconds;
        saveVersion = data.saveVersion;
        currentDay = data.currentDay;
        currentPhase = data.currentPhase.ToString();
        daySubPhase = data.daySubPhase.ToString();
        clockHour = data.clockHour;
        clockMinute = data.clockMinute;
        phaseRemainingTime = data.phaseRemainingTime;
        currentMoney = data.currentMoney;
        todayIncome = data.todayIncome;
        todayExpense = data.todayExpense;
        todayAverageScore = data.todayAverageScore;
        starRating = data.starRating;
        consecutivePerfectDays = data.consecutivePerfectDays;
        // data 中无 totalRecipesCount 字段，若需要显示配方数量，取 unlockedRecipes.Count
        // 为避免编译错误，这里不再读取不存在的字段。
    }
}


