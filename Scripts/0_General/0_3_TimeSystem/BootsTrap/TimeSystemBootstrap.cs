using UnityEngine;

/// <summary>
/// 与 GlobalSceneManager 同步的启动方式：
/// 在任何场景首次加载前，确保 TimeSystemManager 存在（若没有手动放置）。
/// </summary>
public static class TimeSystemBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureTimeSystem()
    {
        if (TimeSystemManager.Instance != null) return;

        var go = new GameObject("TimeSystemManager");
        go.AddComponent<TimeSystemManager>();
        Object.DontDestroyOnLoad(go);
    }
}


