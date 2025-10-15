using UnityEngine;

/// <summary>
/// 确保 SceneTimeCoordinator 在任意场景都自动存在（无需从 Start 场景链式进入）。
/// 行为与 TimeSystemBootstrap 一致：首次场景加载前检查并创建常驻对象。
/// </summary>
public static class SceneTimeCoordinatorBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureCoordinator()
    {
        if (Object.FindObjectOfType<SceneTimeCoordinator>() != null) return;

        var go = new GameObject("SceneTimeCoordinator");
        go.AddComponent<SceneTimeCoordinator>();
        Object.DontDestroyOnLoad(go);
    }
}


