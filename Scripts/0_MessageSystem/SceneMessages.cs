using UnityEngine.SceneManagement;

public struct LoadingRequest
{
	public string targetSceneName;
	public LoadSceneMode mode;
	public override string ToString() => $"{targetSceneName} ({mode})";
}

// Loading 动效阶段信号（用于在不同动效间松耦合通知）
public struct LoadingAnimStage
{
    public string stage; // e.g. "GlassFinished", "ProgressStarted", "ProgressFinished"
}

// 所有 Loading 动效步骤完成（由 LoadingScreen 发送）
public struct LoadingSequenceCompleted
{
    public string targetSceneName;
}

// 请求激活已异步加载的场景（由全局场景管理器发送）
public struct ActivateLoadedScene
{
    public string targetSceneName;
}

// ============ 顺序场景切换控制消息 ============
// 通过这些消息由外部触发全局场景管理器进行顺序或定向切换
public struct GoNextScene { }
public struct GoPrevScene { }
public struct GoToSceneIndex { public int index; }
public struct GoToSceneName { public string sceneName; }


