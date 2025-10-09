using UnityEngine;
using DG.Tweening;

/// <summary>
/// DOTween 启动容量初始化：避免运行时容量自动扩容产生的警告。
/// 仅设置 Tweens/Sequences 容量，不改变全局播放或更新行为，以减少对现有动画的影响。
/// </summary>
public static class DOTweenBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        // 根据项目实际峰值调整，先设置为 1000 Tweens / 200 Sequences，避免频繁自动扩容。
        DOTween.SetTweensCapacity(1000, 200);
    }
}


