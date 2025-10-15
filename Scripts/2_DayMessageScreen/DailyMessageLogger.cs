using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 每日信息运行时记录器：
/// - 订阅 DailyMessagesData.DailyMessageApplied（类型总线）
/// - 记录到当前存档槽位的持久化日志（ES3）
/// - 监听场景切换，为最新未标记的记录设置 propagated=true（已跨场景传播）
/// - 当存档被删除后（编辑器查看器会检测），可清空对应槽位的日志
/// </summary>
public class DailyMessageLogger : MonoBehaviour
{
    [Serializable]
    public class AdjustmentInfo
    {
        public string identity;
        public string state;
        public string gender;
        public string npcId;
        public float deltaPercent;
        public float beforePercent;
        public float afterPercent;
    }

    [Serializable]
    public class Record
    {
        public int logIndex;              // 槽位内序号（从1递增）
        public int day;                   // 天数
        public int newspaperIndex;        // 报纸序号（在数据源中的索引，找不到则为-1）
        public string id;                 // 消息ID
        public string title;              // 标题
        public string imagePath;          // 图片路径
        public List<AdjustmentInfo> adjustments = new List<AdjustmentInfo>();
        public bool propagated;           // 是否已跨场景传播
        public string timestamp;          // 记录时间
    }

    private static DailyMessageLogger _instance;
    public static DailyMessageLogger Instance => _instance;

    private string _currentSlotId = "1";
    private const string KeyPrefix = "daily_message_log_"; // + slotId

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (_instance != null) return;
        var go = new GameObject("DailyMessageLogger");
        _instance = go.AddComponent<DailyMessageLogger>();
        DontDestroyOnLoad(go);
    }

    private void OnEnable()
    {
        MessageManager.Register<string>(MessageDefine.SAVE_LOADED, OnSaveLoaded);
        MessageManager.Register<DailyMessagesData.DailyMessageApplied>(OnDailyMessageApplied);
        SceneManager.activeSceneChanged += OnActiveSceneChanged;
    }

    private void OnDisable()
    {
        MessageManager.Remove<string>(OnSaveLoaded);
        MessageManager.Remove<DailyMessagesData.DailyMessageApplied>(OnDailyMessageApplied);
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
    }

    private void OnSaveLoaded(string slotId)
    {
        if (!string.IsNullOrEmpty(slotId)) _currentSlotId = slotId;
    }

    private void OnDailyMessageApplied(DailyMessagesData.DailyMessageApplied payload)
    {
        try
        {
            var rec = new Record
            {
                day = TryGetCurrentDaySafe(),
                newspaperIndex = FindNewspaperIndex(payload),
                id = payload.id,
                title = payload.title,
                imagePath = payload.imagePath,
                propagated = false,
                timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            // 计算调整概率（基线20%）
            if (payload.adjustments != null)
            {
                foreach (var a in payload.adjustments)
                {
                    float before = 20f;
                    float after = Mathf.Max(0f, before + a.deltaPercent);
                    rec.adjustments.Add(new AdjustmentInfo
                    {
                        identity = a.identity,
                        state = a.state,
                        gender = a.gender,
                        npcId = a.npcId,
                        deltaPercent = a.deltaPercent,
                        beforePercent = before,
                        afterPercent = after
                    });
                }
            }

            // 写入到槽位日志
            var list = LoadRecords(_currentSlotId);
            rec.logIndex = list.Count + 1;
            list.Add(rec);
            SaveRecords(_currentSlotId, list);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[DailyMessageLogger] 记录失败: {e.Message}");
        }
    }

    private void OnActiveSceneChanged(Scene from, Scene to)
    {
        // 标记最近一条未传播的记录为已传播
        try
        {
            var list = LoadRecords(_currentSlotId);
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (!list[i].propagated)
                {
                    list[i].propagated = true;
                    SaveRecords(_currentSlotId, list);
                    break;
                }
            }
        }
        catch { }
    }

    private static string GetKey(string slotId)
    {
        return KeyPrefix + (string.IsNullOrEmpty(slotId) ? "1" : slotId);
    }

    private static List<Record> LoadRecords(string slotId)
    {
        string key = GetKey(slotId);
        if (ES3.KeyExists(key))
        {
            try { return ES3.Load<List<Record>>(key); } catch { }
        }
        return new List<Record>();
    }

    private static void SaveRecords(string slotId, List<Record> list)
    {
        string key = GetKey(slotId);
        ES3.Save(key, list);
    }

    public static void ClearForSlot(string slotId)
    {
        string key = GetKey(slotId);
        if (ES3.KeyExists(key)) ES3.DeleteKey(key);
    }

    private int TryGetCurrentDaySafe()
    {
        var tsm = TimeSystemManager.Instance;
        return tsm != null ? tsm.CurrentDay : 0;
    }

    private int FindNewspaperIndex(DailyMessagesData.DailyMessageApplied payload)
    {
        // 优先在场景中的 DailyMessagesData 搜索
        var all = FindObjectsOfType<DailyMessagesData>(includeInactive: true);
        foreach (var so in all)
        {
            int idx = IndexOfEntryById(so, payload.id);
            if (idx >= 0) return idx;
        }
        // 尝试从 Resources 加载一个默认路径的 SO 或 JSON 数据
        try
        {
            var so = Resources.Load<DailyMessagesData>("DailyMessagesData");
            if (so != null)
            {
                int idx2 = IndexOfEntryById(so, payload.id);
                if (idx2 >= 0) return idx2;
            }
        }
        catch { }
        return -1;
    }

    private int IndexOfEntryById(DailyMessagesData data, string id)
    {
        if (data == null || string.IsNullOrEmpty(id)) return -1;
        // 先查 SO 直存
        if (data.messagesInSO != null)
        {
            for (int i = 0; i < data.messagesInSO.Count; i++)
            {
                if (string.Equals(data.messagesInSO[i].id, id, StringComparison.Ordinal)) return i;
            }
        }
        // 再查 JSON 加载
        try
        {
            var root = data.Load();
            if (root != null && root.messages != null)
            {
                for (int i = 0; i < root.messages.Count; i++)
                {
                    if (string.Equals(root.messages[i].id, id, StringComparison.Ordinal)) return i;
                }
            }
        }
        catch { }
        return -1;
    }
}


