using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// 简版泛型消息系统（字符串Key + 泛型负载），线程安全最小化：使用锁保护字典
public static class MessageManager
{
    private static readonly Dictionary<string, IMessageData> s_Dict = new Dictionary<string, IMessageData>();
    private static readonly Dictionary<System.Type, IMessageData> s_TypeDict = new Dictionary<System.Type, IMessageData>();
    private static readonly object s_Lock = new object();

    // ===== 内建日志（时间戳/帧号缓冲） =====
    public struct MessageLogEntry
    {
        public double time;
        public int frame;
        public string channel; // key:xxx 或 type:FullName
        public string typeName; // 负载类型名
        public string payload;  // ToString()
    }
    private static readonly object s_LogLock = new object();
    private static readonly List<MessageLogEntry> s_Log = new List<MessageLogEntry>(256);
    private static int s_MaxLog = 500;
    private static bool s_LogEnabled = true;

    public static void Register<T>(string key, UnityAction<T> action)
    {
        if (string.IsNullOrEmpty(key) || action == null) return;
        lock (s_Lock)
        {
            if (s_Dict.TryGetValue(key, out var prev))
            {
                if (prev is MessageData<T> md)
                {
                    md.MessageEvents += action;
                }
                else
                {
                    // 类型不匹配则覆盖为新的泛型通道
                    s_Dict[key] = new MessageData<T>(action);
                }
            }
            else
            {
                s_Dict.Add(key, new MessageData<T>(action));
            }
        }
    }

    public static void Remove<T>(string key, UnityAction<T> action)
    {
        if (string.IsNullOrEmpty(key) || action == null) return;
        lock (s_Lock)
        {
            if (s_Dict.TryGetValue(key, out var prev) && prev is MessageData<T> md)
            {
                md.MessageEvents -= action;
                // 若该Key已无订阅者，可选择移除
                if (md.MessageEvents == null)
                {
                    s_Dict.Remove(key);
                }
            }
        }
    }

    public static void Send<T>(string key, T data)
    {
        if (string.IsNullOrEmpty(key)) return;
        IMessageData prev;
        lock (s_Lock)
        {
            s_Dict.TryGetValue(key, out prev);
        }
        (prev as MessageData<T>)?.MessageEvents?.Invoke(data);
        Log("key:" + key, typeof(T), data);
    }

    public static void Clear()
    {
        lock (s_Lock)
        {
            s_Dict.Clear();
            s_TypeDict.Clear();
        }
    }

    // ===== 快照/辅助 =====
    public static List<(string key, System.Type payloadType, int subscribers)> GetStringBusSnapshot()
    {
        var result = new List<(string, System.Type, int)>();
        lock (s_Lock)
        {
            foreach (var kv in s_Dict)
            {
                var md = kv.Value;
                System.Type payload = null;
                int cnt = 0;
                if (md != null)
                {
                    var t = md.GetType();
                    if (t.IsGenericType) payload = t.GenericTypeArguments[0];
                    var field = t.GetField("MessageEvents");
                    var del = field != null ? field.GetValue(md) as System.Delegate : null;
                    if (del != null) cnt = del.GetInvocationList().Length;
                }
                result.Add((kv.Key, payload, cnt));
            }
        }
        return result;
    }

    public static List<(System.Type payloadType, int subscribers)> GetTypeBusSnapshot()
    {
        var result = new List<(System.Type, int)>();
        lock (s_Lock)
        {
            foreach (var kv in s_TypeDict)
            {
                var md = kv.Value;
                int cnt = 0;
                if (md != null)
                {
                    var t = md.GetType();
                    var field = t.GetField("MessageEvents");
                    var del = field != null ? field.GetValue(md) as System.Delegate : null;
                    if (del != null) cnt = del.GetInvocationList().Length;
                }
                result.Add((kv.Key, cnt));
            }
        }
        return result;
    }

    public static void ClearKey(string key)
    {
        if (string.IsNullOrEmpty(key)) return;
        lock (s_Lock)
        {
            s_Dict.Remove(key);
        }
    }

    // ===== 基于泛型参数的消息系统（按类型作为Key） =====
    public static void Register<T>(UnityAction<T> action)
    {
        if (action == null) return;
        var key = typeof(T);
        lock (s_Lock)
        {
            if (s_TypeDict.TryGetValue(key, out var prev))
            {
                if (prev is MessageData<T> md)
                {
                    md.MessageEvents += action;
                }
                else
                {
                    s_TypeDict[key] = new MessageData<T>(action);
                }
            }
            else
            {
                s_TypeDict.Add(key, new MessageData<T>(action));
            }
        }
    }

    public static void Remove<T>(UnityAction<T> action)
    {
        if (action == null) return;
        var key = typeof(T);
        lock (s_Lock)
        {
            if (s_TypeDict.TryGetValue(key, out var prev) && prev is MessageData<T> md)
            {
                md.MessageEvents -= action;
                if (md.MessageEvents == null)
                {
                    s_TypeDict.Remove(key);
                }
            }
        }
    }

    public static void Send<T>(T data)
    {
        var key = typeof(T);
        IMessageData prev;
        lock (s_Lock)
        {
            s_TypeDict.TryGetValue(key, out prev);
        }
        (prev as MessageData<T>)?.MessageEvents?.Invoke(data);
        Log("type:" + key.FullName, typeof(T), data);
    }

    // ===== 日志控制/访问 =====
    public static void SetLogEnabled(bool on) { lock (s_LogLock) s_LogEnabled = on; }
    public static bool GetLogEnabled() { lock (s_LogLock) return s_LogEnabled; }
    public static void SetMaxLog(int max)
    {
        if (max < 0) max = 0;
        lock (s_LogLock)
        {
            s_MaxLog = max;
            TrimLogIfNeeded();
        }
    }
    public static int GetMaxLog() { lock (s_LogLock) return s_MaxLog; }
    public static List<MessageLogEntry> GetLogSnapshot()
    {
        lock (s_LogLock)
        {
            return new List<MessageLogEntry>(s_Log);
        }
    }
    public static void ClearLog() { lock (s_LogLock) s_Log.Clear(); }

    private static void Log<T>(string channel, System.Type t, T data)
    {
        lock (s_LogLock)
        {
            if (!s_LogEnabled) return;
            var e = new MessageLogEntry
            {
                time = Time.realtimeSinceStartupAsDouble,
                frame = Time.frameCount,
                channel = channel,
                typeName = t != null ? t.Name : "null",
                payload = data != null ? data.ToString() : "null"
            };
            s_Log.Add(e);
            TrimLogIfNeeded();
        }
    }
    private static void TrimLogIfNeeded()
    {
        if (s_MaxLog <= 0)
        {
            s_Log.Clear();
            return;
        }
        int extra = s_Log.Count - s_MaxLog;
        if (extra > 0) s_Log.RemoveRange(0, extra);
    }
}