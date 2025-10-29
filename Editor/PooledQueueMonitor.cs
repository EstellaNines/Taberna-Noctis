using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TabernaNoctis.QueueSystem;
using TabernaNoctis.CharacterDesign;

#if UNITY_EDITOR
/// <summary>
/// PooledQueue 监控编辑器窗口
/// - 自动发现并显示所有场景中的队列
/// - 实时刷新队列统计信息
/// - 提供队列详情查看窗口
/// - 支持队列操作（清空、预热等）
/// </summary>
public class PooledQueueMonitor : EditorWindow
{
    private Vector2 _scrollPosition;
    private bool _autoRefresh = true;
    private double _lastRefreshTime;
    private const double REFRESH_INTERVAL = 0.5; // 0.5秒刷新一次
    
    private List<QueueInfo> _discoveredQueues = new List<QueueInfo>();
    
    /// <summary>
    /// 队列信息结构
    /// </summary>
    public class QueueInfo
    {
        public string name;
        public string typeName;
        public MonoBehaviour owner;
        public object queueInstance;
        public MethodInfo countMethod;
        public MethodInfo toArrayMethod;
        public MethodInfo clearMethod;
        public MethodInfo getStatsMethod;
        
        public int Count => (int)(countMethod?.Invoke(queueInstance, null) ?? 0);
        public System.Array ToArray()
        {
            var obj = toArrayMethod?.Invoke(queueInstance, null);
            return obj as System.Array ?? System.Array.Empty<object>();
        }
        public string GetStats()
        {
            var obj = getStatsMethod?.Invoke(queueInstance, null);
            return obj != null ? obj.ToString() : "无统计信息";
        }
        public void Clear() => clearMethod?.Invoke(queueInstance, null);
    }

    [MenuItem("自制工具/队列系统/队列监控器 &Q")]
    public static void ShowWindow()
    {
        var window = GetWindow<PooledQueueMonitor>("队列监控器");
        window.minSize = new Vector2(600, 400);
        window.Show();
    }

    private void OnEnable()
    {
        _lastRefreshTime = EditorApplication.timeSinceStartup;
        DiscoverQueues();
    }

    private void OnGUI()
    {
        DrawHeader();
        DrawQueueList();
        
        if (_autoRefresh && EditorApplication.timeSinceStartup - _lastRefreshTime > REFRESH_INTERVAL)
        {
            _lastRefreshTime = EditorApplication.timeSinceStartup;
            DiscoverQueues(); // 定期重新发现队列
            Repaint();
        }
    }
    
    /// <summary>
    /// 自动发现场景中的所有PooledQueue实例
    /// </summary>
    private void DiscoverQueues()
    {
        _discoveredQueues.Clear();
        
        if (!Application.isPlaying) return;
        
        // 查找所有MonoBehaviour组件
        var allComponents = FindObjectsOfType<MonoBehaviour>();
        
        foreach (var component in allComponents)
        {
            if (component == null) continue;
            
            var type = component.GetType();
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            
            foreach (var field in fields)
            {
                if (IsPooledQueueType(field.FieldType))
                {
                    var queueInstance = field.GetValue(component);
                    if (queueInstance != null)
                    {
                        var queueInfo = CreateQueueInfo(field.Name, component, queueInstance, field.FieldType);
                        if (queueInfo != null)
                        {
                            _discoveredQueues.Add(queueInfo);
                        }
                    }
                }
            }
            
            // 也检查属性
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var property in properties)
            {
                if (IsPooledQueueType(property.PropertyType) && property.CanRead)
                {
                    try
                    {
                        var queueInstance = property.GetValue(component);
                        if (queueInstance != null)
                        {
                            var queueInfo = CreateQueueInfo(property.Name, component, queueInstance, property.PropertyType);
                            if (queueInfo != null)
                            {
                                _discoveredQueues.Add(queueInfo);
                            }
                        }
                    }
                    catch
                    {
                        // 忽略无法访问的属性
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// 检查类型是否为PooledQueue
    /// </summary>
    private bool IsPooledQueueType(System.Type type)
    {
        if (type.IsGenericType)
        {
            var genericTypeDef = type.GetGenericTypeDefinition();
            return genericTypeDef == typeof(PooledQueue<>);
        }
        return false;
    }
    
    /// <summary>
    /// 创建队列信息对象
    /// </summary>
    private QueueInfo CreateQueueInfo(string fieldName, MonoBehaviour owner, object queueInstance, System.Type queueType)
    {
        var queueInfo = new QueueInfo
        {
            name = $"{owner.name}.{fieldName}",
            typeName = queueType.Name,
            owner = owner,
            queueInstance = queueInstance
        };
        
        // 获取方法引用
        queueInfo.countMethod = queueType.GetProperty("Count")?.GetGetMethod();
        queueInfo.toArrayMethod = queueType.GetMethod("ToArray");
        queueInfo.clearMethod = queueType.GetMethod("Clear");
        queueInfo.getStatsMethod = queueType.GetMethod("GetPoolStats");
        
        return queueInfo;
    }

    private void DrawHeader()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        
        GUILayout.Label($"PooledQueue 监控器 ({_discoveredQueues.Count} 个队列)", EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();
        
        _autoRefresh = GUILayout.Toggle(_autoRefresh, "自动刷新", EditorStyles.toolbarButton, GUILayout.Width(80));
        
        if (GUILayout.Button("手动刷新", EditorStyles.toolbarButton, GUILayout.Width(80)))
        {
            DiscoverQueues();
            Repaint();
        }
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(5);
    }

    private void DrawQueueList()
    {
        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
        try
        {
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("请在运行时使用此监控器", MessageType.Warning);
                return;
            }

            if (_discoveredQueues.Count == 0)
            {
                EditorGUILayout.HelpBox("未发现任何PooledQueue实例\n\n确保场景中有使用PooledQueue的组件", MessageType.Info);
                return;
            }

            // 绘制队列列表
            foreach (var queueInfo in _discoveredQueues)
            {
                DrawQueueItem(queueInfo);
                EditorGUILayout.Space(5);
            }
        }
        finally
        {
            EditorGUILayout.EndScrollView();
        }
    }
    
    /// <summary>
    /// 绘制单个队列项
    /// </summary>
    private void DrawQueueItem(QueueInfo queueInfo)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        // 标题行
        EditorGUILayout.BeginHorizontal();
        
        // 队列名称和类型
        EditorGUILayout.LabelField($"📋 {queueInfo.name}", EditorStyles.boldLabel, GUILayout.Width(200));
        EditorGUILayout.LabelField($"类型: {queueInfo.typeName}", GUILayout.Width(150));
        EditorGUILayout.LabelField($"数量: {queueInfo.Count}", GUILayout.Width(80));
        
        GUILayout.FlexibleSpace();
        
        // 操作按钮
        if (GUILayout.Button("详情", GUILayout.Width(50)))
        {
            ShowQueueDetailWindow(queueInfo);
        }
        
        if (GUILayout.Button("清空", GUILayout.Width(50)))
        {
            if (EditorUtility.DisplayDialog("确认清空", $"确定要清空队列 '{queueInfo.name}' 吗？", "确定", "取消"))
            {
                queueInfo.Clear();
            }
        }
        
        EditorGUILayout.EndHorizontal();
        
        // 组件信息
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"组件: {queueInfo.owner.GetType().Name}", GUILayout.Width(200));
        EditorGUILayout.LabelField($"GameObject: {queueInfo.owner.name}", GUILayout.Width(150));
        
        if (GUILayout.Button("定位", GUILayout.Width(50)))
        {
            Selection.activeGameObject = queueInfo.owner.gameObject;
            EditorGUIUtility.PingObject(queueInfo.owner.gameObject);
        }
        
        EditorGUILayout.EndHorizontal();
        
        // 统计信息
        if (queueInfo.getStatsMethod != null)
        {
            var stats = queueInfo.GetStats();
            if (!string.IsNullOrEmpty(stats))
            {
                EditorGUILayout.LabelField("统计:", EditorStyles.miniLabel);
                EditorGUILayout.LabelField(stats, EditorStyles.wordWrappedMiniLabel);
            }
        }
        
        EditorGUILayout.EndVertical();
    }
    
    /// <summary>
    /// 显示队列详情窗口
    /// </summary>
    private void ShowQueueDetailWindow(QueueInfo queueInfo)
    {
        QueueDetailWindow.ShowWindow(queueInfo);
    }
}

/// <summary>
/// 队列详情窗口
/// </summary>
public class QueueDetailWindow : EditorWindow
{
    private PooledQueueMonitor.QueueInfo _queueInfo;
    private Vector2 _scrollPosition;
    private bool _autoRefresh = true;
    private double _lastRefreshTime;
    private const double REFRESH_INTERVAL = 1.0; // 1秒刷新一次
    
    public static void ShowWindow(PooledQueueMonitor.QueueInfo queueInfo)
    {
        var window = GetWindow<QueueDetailWindow>($"队列详情 - {queueInfo.name}");
        window._queueInfo = queueInfo;
        window.minSize = new Vector2(500, 300);
        window.Show();
    }
    
    private void OnGUI()
    {
        if (_queueInfo == null)
        {
            EditorGUILayout.HelpBox("队列信息无效", MessageType.Error);
            return;
        }
        
        DrawHeader();
        DrawQueueContent();
        
        if (_autoRefresh && EditorApplication.timeSinceStartup - _lastRefreshTime > REFRESH_INTERVAL)
        {
            _lastRefreshTime = EditorApplication.timeSinceStartup;
            Repaint();
        }
    }
    
    private void DrawHeader()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        
        GUILayout.Label($"队列: {_queueInfo.name} (数量: {_queueInfo.Count})", EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();
        
        _autoRefresh = GUILayout.Toggle(_autoRefresh, "自动刷新", EditorStyles.toolbarButton, GUILayout.Width(80));
        
        if (GUILayout.Button("刷新", EditorStyles.toolbarButton, GUILayout.Width(50)))
        {
            Repaint();
        }
        
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(5);
    }
    
    private void DrawQueueContent()
    {
        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
        try
        {
            // 队列基本信息
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("队列信息", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"名称: {_queueInfo.name}");
            EditorGUILayout.LabelField($"类型: {_queueInfo.typeName}");
            EditorGUILayout.LabelField($"所属组件: {_queueInfo.owner.GetType().Name}");
            EditorGUILayout.LabelField($"GameObject: {_queueInfo.owner.name}");
            EditorGUILayout.LabelField($"当前数量: {_queueInfo.Count}");
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(10);
            
            // 统计信息
            if (_queueInfo.getStatsMethod != null)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("性能统计", EditorStyles.boldLabel);
                var stats = _queueInfo.GetStats();
                EditorGUILayout.LabelField(stats, EditorStyles.wordWrappedLabel);
                EditorGUILayout.EndVertical();
                
                EditorGUILayout.Space(10);
            }
            
            // 队列内容
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("队列内容", EditorStyles.boldLabel);
            
            var items = _queueInfo.ToArray();
            if (items.Length == 0)
            {
                EditorGUILayout.LabelField("队列为空", EditorStyles.centeredGreyMiniLabel);
            }
            else
            {
                for (int i = 0; i < items.Length; i++)
                {
                    DrawQueueItem(i, items.GetValue(i));
                }
            }
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(10);
            
            // 操作按钮
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("清空队列"))
            {
                if (EditorUtility.DisplayDialog("确认清空", $"确定要清空队列 '{_queueInfo.name}' 吗？", "确定", "取消"))
                {
                    _queueInfo.Clear();
                }
            }
            
            if (GUILayout.Button("定位组件"))
            {
                Selection.activeGameObject = _queueInfo.owner.gameObject;
                EditorGUIUtility.PingObject(_queueInfo.owner.gameObject);
            }
            
            EditorGUILayout.EndHorizontal();
        }
        finally
        {
            EditorGUILayout.EndScrollView();
        }
    }
    
    private void DrawQueueItem(int index, object item)
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
        
        // 索引
        EditorGUILayout.LabelField($"[{index}]", GUILayout.Width(40));
        
        // 内容
        if (item == null)
        {
            EditorGUILayout.LabelField("null", EditorStyles.centeredGreyMiniLabel);
        }
        else
        {
            // 尝试显示有意义的信息
            string displayText = GetItemDisplayText(item);
            EditorGUILayout.LabelField(displayText, EditorStyles.wordWrappedLabel);
            
            // 如果是Unity对象，提供定位功能
            if (item is UnityEngine.Object unityObj)
            {
                if (GUILayout.Button("定位", GUILayout.Width(50)))
                {
                    Selection.activeObject = unityObj;
                    EditorGUIUtility.PingObject(unityObj);
                }
            }
        }
        
        EditorGUILayout.EndHorizontal();
    }
    
    private string GetItemDisplayText(object item)
    {
        if (item == null) return "null";
        
        // 特殊处理NpcCharacterData
        if (item is NpcCharacterData npcData)
        {
            return $"NPC: {npcData.displayName} ({npcData.state}, {npcData.gender}) - ID: {npcData.id}";
        }
        
        // 其他Unity对象
        if (item is UnityEngine.Object unityObj)
        {
            return $"{item.GetType().Name}: {unityObj.name}";
        }
        
        // 普通对象
        return $"{item.GetType().Name}: {item.ToString()}";
    }
}
#endif

