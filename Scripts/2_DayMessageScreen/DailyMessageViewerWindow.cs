using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 编辑器窗口：每日信息查看器
/// - 列表显示：索引号 / 天数 / 报纸序号 / 概率调整详情 / 是否传播
/// - 支持选择存档槽位查看、清空该槽位日志
/// - 当游戏运行中有新记录产生时，会自动刷新
/// </summary>
public class DailyMessageViewerWindow : EditorWindow
{
    private string _slotId = "1";
    private Vector2 _scroll;
    private List<DailyMessageLogger.Record> _cached = new List<DailyMessageLogger.Record>();
    private double _lastRefreshTime;

    [MenuItem("自制工具/每日信息系统/每日信息查看器")] 
    public static void Open()
    {
        var win = GetWindow<DailyMessageViewerWindow>("每日信息查看器");
        win.minSize = new Vector2(760, 300);
        win.Show();
    }

    private void OnEnable()
    {
        EditorApplication.update += OnEditorUpdate;
        Refresh();
    }

    private void OnDisable()
    {
        EditorApplication.update -= OnEditorUpdate;
    }

    private void OnEditorUpdate()
    {
        // 运行时每0.5秒自动刷新一次
        if (EditorApplication.timeSinceStartup - _lastRefreshTime > 0.5f)
        {
            _lastRefreshTime = EditorApplication.timeSinceStartup;
            Refresh();
        }
    }

    private void Refresh()
    {
        try
        {
            string key = "daily_message_log_" + (string.IsNullOrEmpty(_slotId) ? "1" : _slotId);
            if (ES3.KeyExists(key))
            {
                _cached = ES3.Load<List<DailyMessageLogger.Record>>(key) ?? new List<DailyMessageLogger.Record>();
            }
            else
            {
                _cached = new List<DailyMessageLogger.Record>();
            }
            Repaint();
        }
        catch
        {
            _cached = new List<DailyMessageLogger.Record>();
        }
    }

    private void OnGUI()
    {
        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("存档槽位:", GUILayout.Width(70));
        string newSlot = EditorGUILayout.TextField(_slotId, GUILayout.Width(60));
        if (newSlot != _slotId)
        {
            _slotId = string.IsNullOrEmpty(newSlot) ? "1" : newSlot;
            Refresh();
        }
        if (GUILayout.Button("刷新", GUILayout.Width(80))) Refresh();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("清空此槽位日志", GUILayout.Width(140)))
        {
            if (EditorUtility.DisplayDialog("清空日志", $"确定清空槽位 {_slotId} 的每日信息日志吗？", "确定", "取消"))
            {
                DailyMessageLogger.ClearForSlot(_slotId);
                Refresh();
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // 表头
        EditorGUILayout.BeginHorizontal("box");
        EditorGUILayout.LabelField("索引", GUILayout.Width(50));
        EditorGUILayout.LabelField("天数", GUILayout.Width(50));
        EditorGUILayout.LabelField("报纸序号", GUILayout.Width(70));
        EditorGUILayout.LabelField("ID/标题", GUILayout.Width(240));
        EditorGUILayout.LabelField("调整概率详情", GUILayout.ExpandWidth(true));
        EditorGUILayout.LabelField("已传播", GUILayout.Width(60));
        EditorGUILayout.EndHorizontal();

        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        if (_cached != null)
        {
            foreach (var r in _cached)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(r.logIndex.ToString(), GUILayout.Width(50));
                EditorGUILayout.LabelField(r.day.ToString(), GUILayout.Width(50));
                EditorGUILayout.LabelField(r.newspaperIndex.ToString(), GUILayout.Width(70));
                EditorGUILayout.LabelField(string.Format("{0} / {1}", r.id, r.title), GUILayout.Width(240));
                EditorGUILayout.LabelField(BuildAdjustmentsSummary(r), GUILayout.ExpandWidth(true));
                EditorGUILayout.LabelField(r.propagated ? "是" : "否", GUILayout.Width(60));
                EditorGUILayout.EndHorizontal();
            }
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("列表显示：索引号 / 天数 / 报纸序号 / 报纸中调整概率的详细内容 / 是否被消息传递到后续所有场景。删除对应存档时请同步清空该槽位日志。", MessageType.Info);
    }

    private string BuildAdjustmentsSummary(DailyMessageLogger.Record r)
    {
        if (r == null || r.adjustments == null || r.adjustments.Count == 0) return "(无)";
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int i = 0; i < r.adjustments.Count; i++)
        {
            var a = r.adjustments[i];
            if (i > 0) sb.Append(" | ");
            sb.AppendFormat("{0}/{1}/{2}/{3}: {4:+0.0;-0.0;0.0}% ({5:0.#}%→{6:0.#}%)",
                a.identity, a.state, a.gender, string.IsNullOrEmpty(a.npcId) ? "-" : a.npcId,
                a.deltaPercent, a.beforePercent, a.afterPercent);
        }
        return sb.ToString();
    }
}


