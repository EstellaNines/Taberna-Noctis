using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "TN/DailyMessage/DailyMessagesData", fileName = "DailyMessagesData")]
public class DailyMessagesData : ScriptableObject
{
    [Header("Source JSON (Resources)")]
    [Tooltip("Resources path to DailyMessages.json, e.g. DailyMessage/DailyMessages")] 
    public string jsonResourcePath = "DailyMessage/DailyMessages"; // without .json

    [Serializable]
    public class Root
    {
        public int version;
        public List<Entry> messages;
    }

    [Serializable]
    public class Entry
    {
        public string id;
        public string title;
        public string imagePath; // Resources path to image sprite
        public List<Adjustment> adjustments;
    }

    [Serializable]
    public class Adjustment
    {
        public string identity;
        public string state;
        public string gender; // male/female/any
        public string npcId;   // optional
        public float deltaPercent; // +/- percent
    }

    [Serializable]
    public struct DailyMessageApplied
    {
        public string id;
        public string title;
        public string imagePath;
        public List<Adjustment> adjustments;
    }

    private Root _cache;
    [Header("Data (filled from JSON)")]
    [Tooltip("Optional: populated via importer; used when not loading from Resources.")]
    public List<Entry> messagesInSO = new List<Entry>();

    public Root Load()
    {
        if (_cache != null) return _cache;
        var path = string.IsNullOrEmpty(jsonResourcePath) ? "DailyMessage/DailyMessages" : jsonResourcePath;
        var ta = Resources.Load<TextAsset>(path);
        if (ta == null)
        {
            Debug.LogWarning($"[DailyMessagesData] TextAsset not found at Resources path: {path}");
            if (path != "DailyMessage/DailyMessages")
            {
                ta = Resources.Load<TextAsset>("DailyMessage/DailyMessages");
                if (ta == null)
                {
                    Debug.LogWarning("[DailyMessagesData] Fallback path DailyMessage/DailyMessages also not found.");
                    return new Root { version = 1, messages = new List<Entry>() };
                }
            }
            else
            {
                return new Root { version = 1, messages = new List<Entry>() };
            }
        }
        if (string.IsNullOrEmpty(ta.text))
        {
            Debug.LogWarning($"[DailyMessagesData] TextAsset is empty at path: {path}");
            return new Root { version = 1, messages = new List<Entry>() };
        }
        try
        {
            _cache = JsonUtility.FromJson<Root>(ta.text);
        }
        catch (Exception e)
        {
            Debug.LogError($"[DailyMessagesData] Parse error: {e.Message}");
            _cache = new Root { version = 1, messages = new List<Entry>() };
        }
        if (_cache.messages == null) _cache.messages = new List<Entry>();
        if (_cache.messages.Count == 0)
        {
            string preview = ta.text.Length > 200 ? ta.text.Substring(0, 200) + "..." : ta.text;
            Debug.LogWarning($"[DailyMessagesData] Loaded JSON but messages.Count==0. Path={path}, Preview=\n{preview}");
        }
        return _cache;
    }

    public Entry GetRandomEntry(int? seed = null)
    {
        // Prefer data already populated on the SO via importer
        List<Entry> source = (messagesInSO != null && messagesInSO.Count > 0) ? messagesInSO : null;
        if (source == null)
        {
            var root = Load();
            if (root.messages == null || root.messages.Count == 0) return null;
            source = root.messages;
        }
        if (seed.HasValue)
        {
            var rng = new System.Random(seed.Value);
            int idx = rng.Next(0, source.Count);
            return source[Mathf.Clamp(idx, 0, source.Count - 1)];
        }
        else
        {
            int idx = UnityEngine.Random.Range(0, source.Count);
            return source[Mathf.Clamp(idx, 0, source.Count - 1)];
        }
    }
}


