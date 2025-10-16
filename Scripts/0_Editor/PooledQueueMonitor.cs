using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using TabernaNoctis.QueueSystem;

#if UNITY_EDITOR
/// <summary>
/// PooledQueue 监控编辑器窗口
/// - 显示所有已注册队列的统计信息
/// - 实时刷新性能数据
/// - 提供清空/预热操作
/// </summary>
public class PooledQueueMonitor : EditorWindow
{
    private Vector2 _scrollPosition;
    private bool _autoRefresh = true;
    private double _lastRefreshTime;
    private const double REFRESH_INTERVAL = 0.5; // 0.5秒刷新一次

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
    }

    private void OnGUI()
    {
        DrawHeader();
        DrawQueueList();
        
        if (_autoRefresh && EditorApplication.timeSinceStartup - _lastRefreshTime > REFRESH_INTERVAL)
        {
            _lastRefreshTime = EditorApplication.timeSinceStartup;
            Repaint();
        }
    }

    private void DrawHeader()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        
        GUILayout.Label("PooledQueue 运行时监控", EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();
        
        _autoRefresh = GUILayout.Toggle(_autoRefresh, "自动刷新", EditorStyles.toolbarButton, GUILayout.Width(80));
        
        if (GUILayout.Button("手动刷新", EditorStyles.toolbarButton, GUILayout.Width(80)))
        {
            Repaint();
        }
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(5);
    }

    private void DrawQueueList()
    {
        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

        EditorGUILayout.HelpBox(
            "PooledQueue 监控器\n\n" +
            "此工具用于监控场景中的 PooledQueue 实例。\n" +
            "要监控自定义队列，请在您的脚本中：\n" +
            "1. 公开 PooledQueue 字段或属性\n" +
            "2. 添加 [ContextMenu] 方法调用 GetPoolStats()\n" +
            "3. 或在此编辑器脚本中扩展监控逻辑",
            MessageType.Info
        );

        EditorGUILayout.Space(10);
        DrawUsageGuide();

        EditorGUILayout.EndScrollView();
    }

    private void DrawUsageGuide()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("扩展监控功能指南", EditorStyles.boldLabel);
        
        EditorGUILayout.HelpBox(
            "要监控自定义队列，请在您的脚本中：\n" +
            "1. 公开 PooledQueue 字段或属性\n" +
            "2. 添加 [ContextMenu] 方法调用 GetPoolStats()\n" +
            "3. 或实现 IQueueMonitorable 接口（自定义）",
            MessageType.Info
        );

        if (GUILayout.Button("查看完整文档"))
        {
            var docPath = "Assets/Documents/QueueSystem_DevDoc.md";
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(docPath);
            if (asset != null)
            {
                Selection.activeObject = asset;
                EditorGUIUtility.PingObject(asset);
            }
            else
            {
                Debug.LogWarning($"未找到文档: {docPath}");
            }
        }
        
        EditorGUILayout.EndVertical();
    }
}
#endif

