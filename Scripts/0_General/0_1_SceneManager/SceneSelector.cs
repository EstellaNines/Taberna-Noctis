using UnityEngine;

// 可扩展的场景选择策略：实现者根据上下文返回目标场景名与模式
public abstract class SceneSelector : ScriptableObject
{
    public abstract string GetTargetSceneName();
    public virtual UnityEngine.SceneManagement.LoadSceneMode GetMode() => UnityEngine.SceneManagement.LoadSceneMode.Single;
}


