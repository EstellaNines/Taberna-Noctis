using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using TabernaNoctis.CharacterDesign;

/// <summary>
/// 顾客NPC到来监控器：实时监控顾客系统运行状态
/// </summary>
public class CustomerMonitorWindow : EditorWindow
{
    [MenuItem("自制工具/人物设计/顾客到来监控器")]
    public static void ShowWindow()
    {
        var window = GetWindow<CustomerMonitorWindow>();
        window.titleContent = new GUIContent("顾客到来监控器");
        window.minSize = new Vector2(800, 600);
    }

    // UI 元素
    private ListView _customerListView;
    private Label _statsLabel;
    private Label _systemStatusLabel;
    private Button _refreshButton;
    private Button _clearButton;
    private Button _exportButton;
    private Toggle _autoRefreshToggle;
    private TextField _searchField;

    // 数据（记录顾客活动日志：入队→到访→服务完成）
    private readonly List<CustomerMonitorData> _customerLog = new List<CustomerMonitorData>();
    private DailyProbabilityToNight? _currentProbabilityData;
    private bool _isMonitoring = false;
    private double _lastUpdateTime = 0;
    private const double UPDATE_INTERVAL = 1.0; // 1秒更新一次
    private int _customerSeq = 0;

    // 统计数据
    private int _totalVisits = 0;
    private int _currentQueueCount = 0;
    private int _availableCount = 0;
    private int _cooldownCount = 0;

    private void CreateGUI()
    {
        InitializeUI();
        LoadCustomerData();
        StartMonitoring();
    }

    private void InitializeUI()
    {
        var root = rootVisualElement;
        
        // 加载样式表
        var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Scripts/0_Editor/CustomerMonitorWindow.uss");
        if (styleSheet != null)
        {
            root.styleSheets.Add(styleSheet);
        }
        
        root.style.paddingLeft = 10;
        root.style.paddingRight = 10;
        root.style.paddingTop = 10;
        root.style.paddingBottom = 10;

        // 创建主容器
        var mainContainer = new VisualElement();
        mainContainer.AddToClassList("customer-monitor-container");
        root.Add(mainContainer);

        // 工具栏
        CreateToolbar(mainContainer);

        // 系统状态
        CreateSystemStatus(mainContainer);

        // 主内容区域（单列：仅列表）
        var contentContainer = new VisualElement();
        contentContainer.style.flexDirection = FlexDirection.Row;
        contentContainer.style.flexGrow = 1;
        mainContainer.Add(contentContainer);

        // 顾客列表（仅到访）
        CreateCustomerList(contentContainer);

        // 底部：统计信息
        CreateStatsPanel(mainContainer);
    }

    private void CreateToolbar(VisualElement parent)
    {
        var toolbar = new VisualElement();
        toolbar.AddToClassList("toolbar");
        parent.Add(toolbar);

        // 刷新按钮
        _refreshButton = new Button(RefreshData) { text = "刷新数据" };
        toolbar.Add(_refreshButton);

        // 清除按钮
        _clearButton = new Button(ClearData) { text = "清除历史" };
        toolbar.Add(_clearButton);

        // 导出按钮
        _exportButton = new Button(ExportData) { text = "导出数据" };
        toolbar.Add(_exportButton);

        // 自动刷新开关
        _autoRefreshToggle = new Toggle("自动刷新");
        _autoRefreshToggle.value = true;
        toolbar.Add(_autoRefreshToggle);

        // 搜索框
        _searchField = new TextField("搜索:");
        _searchField.RegisterValueChangedCallback(OnSearchChanged);
        toolbar.Add(_searchField);
    }

    private void CreateSystemStatus(VisualElement parent)
    {
        _systemStatusLabel = new Label("系统状态：未连接");
        _systemStatusLabel.AddToClassList("system-status");
        _systemStatusLabel.style.color = Color.red;
        parent.Add(_systemStatusLabel);
    }

    private void CreateCustomerList(VisualElement parent)
    {
        var listContainer = new VisualElement();
        listContainer.style.width = Length.Percent(100);
        parent.Add(listContainer);

        var listTitle = new Label("顾客列表");
        listTitle.style.fontSize = 16;
        listTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
        listTitle.style.marginBottom = 5;
        listContainer.Add(listTitle);

        // 创建列表视图
        _customerListView = new ListView();
        _customerListView.style.flexGrow = 1;
        _customerListView.style.borderLeftWidth = 1;
        _customerListView.style.borderRightWidth = 1;
        _customerListView.style.borderTopWidth = 1;
        _customerListView.style.borderBottomWidth = 1;
        _customerListView.style.borderLeftColor = Color.gray;
        _customerListView.style.borderRightColor = Color.gray;
        _customerListView.style.borderTopColor = Color.gray;
        _customerListView.style.borderBottomColor = Color.gray;
        _customerListView.itemsSource = _customerLog;
        _customerListView.makeItem = MakeCustomerListItem;
        _customerListView.bindItem = BindCustomerListItem;
        // 使用动态高度虚拟化，避免固定高度要求为正的异常
        _customerListView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
        listContainer.Add(_customerListView);
    }

    private VisualElement MakeCustomerListItem()
    {
        // 使用 Foldout 形成行内可展开详情
        var fold = new Foldout();
        fold.value = false;

        var header = new VisualElement();
        header.style.flexDirection = FlexDirection.Row;
        header.style.alignItems = Align.Center;
        header.style.paddingLeft = 5;
        header.style.paddingRight = 5;
        header.style.paddingTop = 2;
        header.style.paddingBottom = 2;
        header.style.borderBottomWidth = 1;
        header.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);

        var sequenceLabel = new Label();
        sequenceLabel.name = "sequence";
        sequenceLabel.style.width = 40;
        sequenceLabel.style.fontSize = 12;
        sequenceLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        header.Add(sequenceLabel);

        var nameLabel = new Label();
        nameLabel.name = "name";
        nameLabel.style.width = 200;
        nameLabel.style.fontSize = 12;
        header.Add(nameLabel);

        var probabilityLabel = new Label();
        probabilityLabel.name = "probability";
        probabilityLabel.style.width = 90;
        probabilityLabel.style.fontSize = 12;
        probabilityLabel.style.color = Color.cyan;
        header.Add(probabilityLabel);

        var indexLabel = new Label();
        indexLabel.name = "index";
        indexLabel.style.width = 160;
        indexLabel.style.fontSize = 10;
        indexLabel.style.color = Color.gray;
        header.Add(indexLabel);

        // 去掉 Foldout 自带的文本
        fold.Q<Label>()?.RemoveFromHierarchy();
        fold.Add(header);

        var details = new VisualElement();
        details.name = "details";
        details.style.marginTop = 6;
        details.style.marginLeft = 8;
        fold.Add(details);

        return fold;
    }

    private void BindCustomerListItem(VisualElement element, int index)
    {
        if (index < 0 || index >= _customerLog.Count) return;

        var data = _customerLog[index];

        element.Q<Label>("sequence").text = data.sequence.ToString("D2");
        element.Q<Label>("name").text = data.customerName;
        element.Q<Label>("probability").text = $"{data.currentProbability:F2}%";
        element.Q<Label>("index").text = data.customerIndex;

        var details = element.Q<VisualElement>("details");
        details.Clear();
        AddDetailRow(details, "身份", data.identityId);
        AddDetailRow(details, "状态", data.state);
        AddDetailRow(details, "性别", data.gender == "male" ? "男" : "女");
        AddDetailRow(details, "初始心情", data.initialMood.ToString());
        AddDetailRow(details, "保底池", data.isInGuaranteePool ? "是" : "否");
        AddDetailRow(details, "最后到访", data.lastVisitTime != default ? data.lastVisitTime.ToString("HH:mm:ss") : "刚刚");
    }

    private void AddDetailRow(VisualElement parent, string key, string value)
    {
        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.marginBottom = 2;
        var k = new Label(key + ":");
        k.style.width = 80;
        k.style.fontSize = 11;
        k.style.color = Color.gray;
        var v = new Label(value);
        v.style.fontSize = 11;
        row.Add(k);
        row.Add(v);
        parent.Add(row);
    }

    // 右侧详情面板已移除（采用行内展开）

    private void CreateStatsPanel(VisualElement parent)
    {
        _statsLabel = new Label("统计信息：加载中...");
        _statsLabel.style.fontSize = 12;
        _statsLabel.style.marginTop = 10;
        _statsLabel.style.paddingTop = 5;
        _statsLabel.style.borderTopWidth = 1;
        _statsLabel.style.borderTopColor = Color.gray;
        parent.Add(_statsLabel);
    }

    // 详情绘制改为行内展开，不再单独存在面板与段落

    private void StartMonitoring()
    {
        _isMonitoring = true;
        
        // 订阅消息
        MessageManager.Register<DailyProbabilityToNight>(MessageDefine.NIGHT_PROBABILITY_READY, OnProbabilityDataReceived);
        MessageManager.Register<NpcCharacterData>(MessageDefine.CUSTOMER_SPAWNED, OnCustomerSpawned);
        MessageManager.Register<NpcCharacterData>(MessageDefine.CUSTOMER_DEQUEUED, OnCustomerDequeued);
        MessageManager.Register<NpcCharacterData>(MessageDefine.CUSTOMER_VISITED, OnCustomerVisited);

        UpdateSystemStatus("监控中...", Color.green);
        
        EditorApplication.update += OnEditorUpdate;
    }

    private void StopMonitoring()
    {
        _isMonitoring = false;
        
        // 取消订阅
        MessageManager.Remove<DailyProbabilityToNight>(MessageDefine.NIGHT_PROBABILITY_READY, OnProbabilityDataReceived);
        MessageManager.Remove<NpcCharacterData>(MessageDefine.CUSTOMER_SPAWNED, OnCustomerSpawned);
        MessageManager.Remove<NpcCharacterData>(MessageDefine.CUSTOMER_DEQUEUED, OnCustomerDequeued);
        MessageManager.Remove<NpcCharacterData>(MessageDefine.CUSTOMER_VISITED, OnCustomerVisited);

        UpdateSystemStatus("已停止", Color.red);
        
        EditorApplication.update -= OnEditorUpdate;
    }

    private void OnEditorUpdate()
    {
        if (!_isMonitoring || !_autoRefreshToggle.value) return;

        var currentTime = EditorApplication.timeSinceStartup;
        if (currentTime - _lastUpdateTime >= UPDATE_INTERVAL)
        {
            _lastUpdateTime = currentTime;
            UpdateCustomerData();
        }
    }

    private void LoadCustomerData()
    {
        // 初始化为清空的顾客活动日志
        _customerLog.Clear();
        _customerListView?.RefreshItems();
        UpdateStats();
    }

    private void LoadFromAssets() {}

    private void UpdateCustomerData()
    {
        var spawnManager = FindObjectOfType<CustomerSpawnManager>();
        if (spawnManager != null)
        {
            var stats = spawnManager.GetStats();
            _currentQueueCount = stats.queueCount;
            _availableCount = stats.availableCount;
            _cooldownCount = stats.cooldownCount;
        }

        // 更新顾客日志中的概率（若有最新概率数据）
        if (_currentProbabilityData.HasValue)
        {
            foreach (var customer in _customerLog)
            {
                float probability = VisitProbabilityCalculator.GetFinalProbability(
                    _currentProbabilityData.Value.probabilityResult,
                    customer.identityId,
                    customer.state,
                    customer.gender
                );
                customer.UpdateProbability(probability);
            }
        }

        _customerListView?.RefreshItems();
        UpdateStats();
    }

    private void UpdateStats()
    {
        var totalCustomers = _customerLog.Count;
        var queuedCount = _customerLog.Count(c => c.status == CustomerStatus.InQueue);
        var visitingCount = _customerLog.Count(c => c.status == CustomerStatus.Spawned);
        var completedCount = _customerLog.Count(c => c.status == CustomerStatus.Visited);
        
        _statsLabel.text = $"统计信息：总记录 {totalCustomers} | 队列中 {queuedCount} | 服务中 {visitingCount} | 已完成 {completedCount}";
    }

    private void UpdateSystemStatus(string status, Color color)
    {
        _systemStatusLabel.text = $"系统状态：{status}";
        _systemStatusLabel.style.color = color;
    }

    #region 消息处理

    private void OnProbabilityDataReceived(DailyProbabilityToNight data)
    {
        _currentProbabilityData = data;
        UpdateSystemStatus($"已连接 - 天数{data.currentDay}, 消息{data.messageId}", Color.green);
        UpdateCustomerData();
    }

    private void OnCustomerSpawned(NpcCharacterData npc)
    {
        float prob = 0f;
        if (_currentProbabilityData.HasValue)
        {
            prob = VisitProbabilityCalculator.GetFinalProbability(
                _currentProbabilityData.Value.probabilityResult,
                npc.identityId, npc.state, npc.gender
            );
        }

        var item = new CustomerMonitorData(npc, ++_customerSeq);
        item.UpdateProbability(prob);
        item.UpdateStatus(CustomerStatus.InQueue); // 入队状态
        _customerLog.Add(item);

        _customerListView?.RefreshItems();
        UpdateStats();
    }

    private void OnCustomerDequeued(NpcCharacterData npc)
    {
        // 找到对应的记录并更新状态为"服务中"
        var customer = _customerLog.FirstOrDefault(c => c.customerIndex == npc.id && c.status == CustomerStatus.InQueue);
        if (customer != null)
        {
            customer.UpdateStatus(CustomerStatus.Spawned); // 开始服务
        }
        
        _customerListView?.RefreshItems();
        UpdateStats();
    }

    private void OnCustomerVisited(NpcCharacterData npc)
    {
        // 找到对应的记录并更新状态为"已完成服务"
        var customer = _customerLog.FirstOrDefault(c => c.customerIndex == npc.id && c.status == CustomerStatus.Spawned);
        if (customer != null)
        {
            customer.UpdateStatus(CustomerStatus.Visited); // 服务完成
            _totalVisits++;
        }
        
        _customerListView?.RefreshItems();
        UpdateStats();
    }

    #endregion

    #region 按钮事件

    private void RefreshData()
    {
        LoadCustomerData();
        UpdateCustomerData();
        Debug.Log("[CustomerMonitor] 数据已刷新");
    }

    private void ClearData()
    {
        _customerLog.Clear();
        _totalVisits = 0;
        _customerListView?.RefreshItems();
        UpdateStats();
        Debug.Log("[CustomerMonitor] 历史数据已清除");
    }

    private void ExportData()
    {
        var path = EditorUtility.SaveFilePanel("导出顾客监控数据", "", "customer_monitor_data", "csv");
        if (string.IsNullOrEmpty(path)) return;

        try
        {
            var csv = "序号,角色名称,当前概率,角色索引号,身份,状态,性别,初始心情,当前状态,队列位置,剩余冷却,总生成次数,总到访次数,最后生成时间,最后到访时间\n";

            foreach (var customer in _customerLog)
            {
                csv += $"{customer.sequence},{customer.customerName},{customer.currentProbability:F4},{customer.customerIndex},{customer.identityId},{customer.state},{customer.gender},{customer.initialMood},{customer.GetStatusText()},{customer.queuePosition},{customer.cooldownRemaining},{customer.totalSpawns},{customer.totalVisits},{customer.lastSpawnTime:yyyy-MM-dd HH:mm:ss},{customer.lastVisitTime:yyyy-MM-dd HH:mm:ss}\n";
            }

            System.IO.File.WriteAllText(path, csv, System.Text.Encoding.UTF8);
            Debug.Log($"[CustomerMonitor] 数据已导出到: {path}");
            EditorUtility.DisplayDialog("导出成功", $"数据已导出到:\n{path}", "确定");
        }
        catch (Exception e)
        {
            Debug.LogError($"[CustomerMonitor] 导出失败: {e.Message}");
            EditorUtility.DisplayDialog("导出失败", $"导出过程中发生错误:\n{e.Message}", "确定");
        }
    }

    private void OnSearchChanged(ChangeEvent<string> evt)
    {
        var searchText = evt.newValue.ToLower();
        
        if (string.IsNullOrEmpty(searchText))
        {
            _customerListView.itemsSource = _customerLog;
        }
        else
        {
            var filteredData = _customerLog.Where(c =>
                c.customerName.ToLower().Contains(searchText) ||
                c.customerIndex.ToLower().Contains(searchText) ||
                c.identityId.ToLower().Contains(searchText) ||
                c.state.ToLower().Contains(searchText)
            ).ToList();

            _customerListView.itemsSource = filteredData;
        }
        
        _customerListView.RefreshItems();
    }

    #endregion

    private void OnDestroy()
    {
        StopMonitoring();
    }
}
