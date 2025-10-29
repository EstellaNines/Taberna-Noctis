using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using TabernaNoctis.CharacterDesign;
using TabernaNoctis.NightScreen;

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

    // 数据
    private readonly List<NpcCharacterData> _queuedCustomers = new List<NpcCharacterData>();
    private CustomerServiceManager _serviceManager;
    private CustomerSpawnManager _spawnManager;
    private bool _isMonitoring = false;
    private double _lastUpdateTime = 0;
    private const double UPDATE_INTERVAL = 0.5; // 0.5秒更新一次

    // 当前服务状态
    private NpcCharacterData _currentServingCustomer;

    private void CreateGUI()
    {
        InitializeUI();
        LoadCustomerData();
        StartMonitoring();
    }

    private void InitializeUI()
    {
        var root = rootVisualElement;
        
        // 加载样式表（优先 Editor/，兼容旧路径）
        var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Editor/CustomerMonitorWindow.uss")
                          ?? AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Scripts/Editor/CustomerMonitorWindow.uss")
                          ?? AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Scripts/0_Editor/CustomerMonitorWindow.uss");
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

        // 主内容区域（两列：队列列表 + 当前服务）
        var contentContainer = new VisualElement();
        contentContainer.style.flexDirection = FlexDirection.Row;
        contentContainer.style.flexGrow = 1;
        mainContainer.Add(contentContainer);

        // 左侧：队列列表
        CreateQueueList(contentContainer);

        // 右侧：当前服务状态
        CreateCurrentServicePanel(contentContainer);

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

		// 强制随机入队（调试生成一位顾客）
		var forceSpawnButton = new Button(ForceRandomEnqueue) { text = "强制随机入队" };
		toolbar.Add(forceSpawnButton);

		// 清空所有冷却池
		var clearCooldownButton = new Button(ClearAllCooldown) { text = "清空冷却池" };
		toolbar.Add(clearCooldownButton);

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

    private void CreateQueueList(VisualElement parent)
    {
        var listContainer = new VisualElement();
        listContainer.style.width = Length.Percent(60);
        listContainer.style.marginRight = 10;
        parent.Add(listContainer);

        var listTitle = new Label("顾客队列");
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
        _customerListView.itemsSource = _queuedCustomers;
        _customerListView.makeItem = MakeQueueListItem;
        _customerListView.bindItem = BindQueueListItem;
        _customerListView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
        listContainer.Add(_customerListView);
    }

    private Label _currentServiceLabel;
    private Label _serviceStatusLabel;
    private Label _cooldownLabel;
    private Label _drinkingLabel;

    private void CreateCurrentServicePanel(VisualElement parent)
    {
        var serviceContainer = new VisualElement();
        serviceContainer.style.width = Length.Percent(40);
        serviceContainer.style.borderLeftWidth = 1;
        serviceContainer.style.borderRightWidth = 1;
        serviceContainer.style.borderTopWidth = 1;
        serviceContainer.style.borderBottomWidth = 1;
        serviceContainer.style.borderLeftColor = Color.gray;
        serviceContainer.style.borderRightColor = Color.gray;
        serviceContainer.style.borderTopColor = Color.gray;
        serviceContainer.style.borderBottomColor = Color.gray;
        serviceContainer.style.paddingLeft = 10;
        serviceContainer.style.paddingRight = 10;
        serviceContainer.style.paddingTop = 10;
        serviceContainer.style.paddingBottom = 10;
        parent.Add(serviceContainer);

        var serviceTitle = new Label("当前服务");
        serviceTitle.style.fontSize = 16;
        serviceTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
        serviceTitle.style.marginBottom = 10;
        serviceContainer.Add(serviceTitle);

        _currentServiceLabel = new Label("无顾客服务中");
        _currentServiceLabel.style.fontSize = 14;
        _currentServiceLabel.style.marginBottom = 5;
        serviceContainer.Add(_currentServiceLabel);

        _serviceStatusLabel = new Label("状态: 等待顾客");
        _serviceStatusLabel.style.fontSize = 12;
        _serviceStatusLabel.style.marginBottom = 5;
        serviceContainer.Add(_serviceStatusLabel);

        _cooldownLabel = new Label("冷却时间: --");
        _cooldownLabel.style.fontSize = 12;
        _cooldownLabel.style.marginBottom = 5;
        serviceContainer.Add(_cooldownLabel);

        _drinkingLabel = new Label("品尝时间: --");
        _drinkingLabel.style.fontSize = 12;
        _drinkingLabel.style.marginBottom = 5;
        serviceContainer.Add(_drinkingLabel);
    }

    private VisualElement MakeQueueListItem()
    {
        var container = new VisualElement();
        container.style.flexDirection = FlexDirection.Row;
        container.style.alignItems = Align.Center;
        container.style.paddingLeft = 5;
        container.style.paddingRight = 5;
        container.style.paddingTop = 3;
        container.style.paddingBottom = 3;
        container.style.borderBottomWidth = 1;
        container.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);

        var sequenceLabel = new Label();
        sequenceLabel.name = "sequence";
        sequenceLabel.style.width = 40;
        sequenceLabel.style.fontSize = 12;
        sequenceLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        container.Add(sequenceLabel);

        var indexLabel = new Label();
        indexLabel.name = "index";
        indexLabel.style.width = 120;
        indexLabel.style.fontSize = 10;
        indexLabel.style.color = Color.gray;
        container.Add(indexLabel);

        var nameLabel = new Label();
        nameLabel.name = "name";
        nameLabel.style.flexGrow = 1;
        nameLabel.style.fontSize = 12;
        container.Add(nameLabel);

		// 冷却显示
		var cooldownLabel = new Label();
		cooldownLabel.name = "cooldown";
		cooldownLabel.style.width = 90;
		cooldownLabel.style.fontSize = 10;
		cooldownLabel.style.unityTextAlign = TextAnchor.MiddleRight;
		container.Add(cooldownLabel);

        return container;
    }

    private void BindQueueListItem(VisualElement element, int index)
    {
        if (index < 0 || index >= _queuedCustomers.Count) return;

        var data = _queuedCustomers[index];

        element.Q<Label>("sequence").text = (index + 1).ToString("D2");
        element.Q<Label>("index").text = data.id;
        element.Q<Label>("name").text = data.displayName;

		// 冷却显示：CD 剩余/总需求（若不在冷却池则显示 "--"）
		var cdLabel = element.Q<Label>("cooldown");
		int remaining = GetCooldownRemainingForNpc(data.id);
		int requirement = GetCooldownRequirementTotal();
		if (remaining > 0)
		{
			cdLabel.text = $"CD {remaining}/{requirement}";
			cdLabel.style.color = new Color(1f, 0.8f, 0.2f, 1f); // 近似黄橙色
		}
		else
		{
			cdLabel.text = "CD --";
			cdLabel.style.color = Color.gray;
		}
    }

	// 读取冷却池中该NPC的剩余冷却（通过反射）
	private int GetCooldownRemainingForNpc(string npcId)
	{
		if (_spawnManager == null || string.IsNullOrEmpty(npcId)) return 0;
		try
		{
			var type = _spawnManager.GetType();
			var cooldownField = type.GetField("cooldownPool", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			var dict = cooldownField?.GetValue(_spawnManager) as System.Collections.IDictionary;
			if (dict != null && dict.Contains(npcId))
			{
				object val = dict[npcId];
				if (val is int i) return i;
			}
		}
		catch { }
		return 0;
	}

	// 读取总冷却需求（通过反射）
	private int GetCooldownRequirementTotal()
	{
		if (_spawnManager == null) return 0;
		try
		{
			var type = _spawnManager.GetType();
			var reqField = type.GetField("cooldownRequirement", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			object val = reqField?.GetValue(_spawnManager);
			if (val is int i) return i;
		}
		catch { }
		return 0;
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

    // 详情绘制改为行内展开，不再单独存在面板与段落

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
        
        // 查找管理器组件
        _spawnManager = FindObjectOfType<CustomerSpawnManager>();
        _serviceManager = FindObjectOfType<CustomerServiceManager>();
        
        if (_spawnManager == null)
        {
            UpdateSystemStatus("未找到CustomerSpawnManager", Color.red);
        }
        else if (_serviceManager == null)
        {
            UpdateSystemStatus("未找到CustomerServiceManager", Color.yellow);
        }
        else
        {
            UpdateSystemStatus("监控中...", Color.green);
        }
        
        EditorApplication.update += OnEditorUpdate;
    }

    private void StopMonitoring()
    {
        _isMonitoring = false;
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
        // 初始化队列数据
        _queuedCustomers.Clear();
        _customerListView?.RefreshItems();
        UpdateStats();
    }

    private void UpdateCustomerData()
    {
        if (!Application.isPlaying) return;

        // 更新队列数据
        if (_spawnManager != null)
        {
            var queuedCustomers = _spawnManager.GetQueuedCustomers();
            _queuedCustomers.Clear();
            _queuedCustomers.AddRange(queuedCustomers);
        }

        // 更新当前服务状态
        UpdateCurrentServiceStatus();

        _customerListView?.RefreshItems();
        UpdateStats();
    }

    private void UpdateCurrentServiceStatus()
    {
        if (_serviceManager == null) return;

        // 通过反射获取服务管理器的私有字段
        var serviceManagerType = _serviceManager.GetType();
        var isServicingField = serviceManagerType.GetField("isServicing", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var isDrinkingField = serviceManagerType.GetField("isDrinking", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var nextServeCountdownField = serviceManagerType.GetField("nextServeCountdown", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        bool isServicing = isServicingField?.GetValue(_serviceManager) as bool? ?? false;
        bool isDrinking = isDrinkingField?.GetValue(_serviceManager) as bool? ?? false;
        float nextServeCountdown = nextServeCountdownField?.GetValue(_serviceManager) as float? ?? 0f;

        // 获取当前服务的顾客（通过CustomerNpcBehavior）
        var behaviorField = serviceManagerType.GetField("customerBehavior", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var customerBehavior = behaviorField?.GetValue(_serviceManager) as CustomerNpcBehavior;
        
        if (isServicing && customerBehavior != null)
        {
            _currentServingCustomer = customerBehavior.CurrentData;
            if (_currentServingCustomer != null)
            {
                _currentServiceLabel.text = $"序号: 01 | ID: {_currentServingCustomer.id} | 姓名: {_currentServingCustomer.displayName}";
                
                if (isDrinking)
                {
                    _serviceStatusLabel.text = "状态: 品尝中";
                    _drinkingLabel.text = "品尝时间: 进行中...";
                }
                else
                {
                    _serviceStatusLabel.text = "状态: 等待鸡尾酒";
                    _drinkingLabel.text = "品尝时间: --";
                }
            }
        }
        else
        {
            _currentServingCustomer = null;
            _currentServiceLabel.text = "无顾客服务中";
            _serviceStatusLabel.text = "状态: 等待顾客";
            _drinkingLabel.text = "品尝时间: --";
        }

        // 更新冷却时间
        if (nextServeCountdown > 0)
        {
            _cooldownLabel.text = $"冷却时间: {nextServeCountdown:F1}秒";
        }
        else
        {
            _cooldownLabel.text = "冷却时间: --";
        }
    }

    private void UpdateStats()
    {
        var queueCount = _queuedCustomers.Count;
        var isServicing = _currentServingCustomer != null;
        
        string serviceStatus = isServicing ? "有顾客服务中" : "无顾客服务";
        
        if (_spawnManager != null)
        {
            var stats = _spawnManager.GetStats();
            _statsLabel.text = $"统计信息：队列中 {queueCount} 位 | {serviceStatus} | 总到访 {stats.visitedCount} 位 | 可用池 {stats.availableCount} | 冷却池 {stats.cooldownCount}";
        }
        else
        {
            _statsLabel.text = $"统计信息：队列中 {queueCount} 位 | {serviceStatus}";
        }
    }

    private void UpdateSystemStatus(string status, Color color)
    {
        _systemStatusLabel.text = $"系统状态：{status}";
        _systemStatusLabel.style.color = color;
    }


    #region 按钮事件

    private void RefreshData()
    {
        LoadCustomerData();
        UpdateCustomerData();
        Debug.Log("[CustomerMonitor] 数据已刷新");
    }

    private void ClearData()
    {
        _queuedCustomers.Clear();
        _customerListView?.RefreshItems();
        UpdateStats();
        Debug.Log("[CustomerMonitor] 队列数据已清除");
    }

    private void ExportData()
    {
        var path = EditorUtility.SaveFilePanel("导出顾客队列数据", "", "customer_queue_data", "csv");
        if (string.IsNullOrEmpty(path)) return;

        try
        {
            var csv = "序号,顾客ID,顾客名称,状态,性别,身份\n";

            for (int i = 0; i < _queuedCustomers.Count; i++)
            {
                var customer = _queuedCustomers[i];
                csv += $"{i + 1},{customer.id},{customer.displayName},{customer.state},{customer.gender},{customer.identityId}\n";
            }

            // 添加当前服务顾客信息
            if (_currentServingCustomer != null)
            {
                csv += $"\n当前服务顾客:\n";
                csv += $"顾客ID,顾客名称,状态,性别,身份\n";
                csv += $"{_currentServingCustomer.id},{_currentServingCustomer.displayName},{_currentServingCustomer.state},{_currentServingCustomer.gender},{_currentServingCustomer.identityId}\n";
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

	// 强制随机入队（调用生成管理器的私有方法 TrySpawnCustomer/DebugSpawnCustomer）
	private void ForceRandomEnqueue()
	{
		if (_spawnManager == null)
		{
			Debug.LogWarning("[CustomerMonitor] 未找到 CustomerSpawnManager，无法入队");
			return;
		}

		try
		{
			var type = _spawnManager.GetType();
			var trySpawnMethod = type.GetMethod("TrySpawnCustomer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			object result = null;
			if (trySpawnMethod != null)
			{
				result = trySpawnMethod.Invoke(_spawnManager, null);
			}
			else
			{
				// 备用：调用调试方法
				var debugSpawn = type.GetMethod("DebugSpawnCustomer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
				debugSpawn?.Invoke(_spawnManager, null);
			}

			UpdateCustomerData();
			Debug.Log($"[CustomerMonitor] 强制随机入队{(result is bool b ? (b ? "成功" : "失败") : "（已触发）")}");
		}
		catch (Exception e)
		{
			Debug.LogError($"[CustomerMonitor] 强制随机入队失败: {e.Message}");
		}
	}

	// 清空所有冷却池（将冷却中的顾客立即解冻）
	private void ClearAllCooldown()
	{
		if (_spawnManager == null)
		{
			Debug.LogWarning("[CustomerMonitor] 未找到 CustomerSpawnManager，无法清空冷却池");
			return;
		}

		try
		{
			var type = _spawnManager.GetType();
			var cooldownField = type.GetField("cooldownPool", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			var availableField = type.GetField("availablePool", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			var npcLookupField = type.GetField("npcLookup", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

			var cooldown = cooldownField?.GetValue(_spawnManager);
			var available = availableField?.GetValue(_spawnManager) as System.Collections.IList;
			var npcLookup = npcLookupField?.GetValue(_spawnManager) as System.Collections.IDictionary;

			if (cooldown is System.Collections.IDictionary dict)
			{
				// 将冷却中的顾客全部放回可用池
				if (available != null && npcLookup != null)
				{
					var keys = new System.Collections.ArrayList(dict.Keys);
					foreach (var key in keys)
					{
						if (npcLookup.Contains(key))
						{
							available.Add(npcLookup[key]);
						}
					}
				}

				dict.Clear();
				UpdateCustomerData();
				Debug.Log("[CustomerMonitor] 冷却池已清空，所有顾客已解冻");
			}
			else
			{
				Debug.LogWarning("[CustomerMonitor] 反射获取冷却池失败");
			}
		}
		catch (Exception e)
		{
			Debug.LogError($"[CustomerMonitor] 清空冷却池失败: {e.Message}");
		}
	}

    private void OnSearchChanged(ChangeEvent<string> evt)
    {
        var searchText = evt.newValue.ToLower();
        
        if (string.IsNullOrEmpty(searchText))
        {
            _customerListView.itemsSource = _queuedCustomers;
        }
        else
        {
            var filteredData = _queuedCustomers.Where(c =>
                c.displayName.ToLower().Contains(searchText) ||
                c.id.ToLower().Contains(searchText) ||
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
