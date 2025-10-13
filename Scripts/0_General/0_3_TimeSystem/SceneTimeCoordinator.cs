using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 场景时间协调器：监听阶段切换，并通过全局场景管理器(GlobalSceneManager)切换场景。
/// - 放置位置：建议放在 Start 场景的常驻对象上，Awake 中设置 DontDestroyOnLoad。
/// - Start/SaveFiles/Day/Night 均可复用同一实例。
/// </summary>
public class SceneTimeCoordinator : MonoBehaviour
{

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        // 改为严格依据 SceneSequenceConfig 的顺序前进，不再用阶段名硬编码场景
        MessageManager.Register<int>(MessageDefine.DAY_STARTED, OnDayStarted);
        MessageManager.Register<int>(MessageDefine.DAY_COMPLETED, OnDayCompleted);
    }

    private void OnDisable()
    {
        MessageManager.Remove<int>(MessageDefine.DAY_STARTED, OnDayStarted);
        MessageManager.Remove<int>(MessageDefine.DAY_COMPLETED, OnDayCompleted);
    }

    // 开始新的一天：顺序推进到下一个
    private void OnDayStarted(int day)
    {
        // 若当前是启动场景，则不自动推进；由玩家在Start界面选择
        var active = SceneManager.GetActiveScene().name;
        if (string.Equals(active, "0_StartScreen", System.StringComparison.Ordinal)) return;
        // 若当前在结算场景，开始新一天应回到“日循环起点”（一般为 2_DayMessageScreen）
        if (string.Equals(active, "5_SettlementScreen", System.StringComparison.Ordinal))
        {
            GlobalSceneManager.GoToName("2_DayMessageScreen");
            return;
        }
        GlobalSceneManager.Next();
    }

    // 当天完成：顺序推进，继续由顺序配置决定
    private void OnDayCompleted(int day)
    {
        GlobalSceneManager.Next();
    }

    // 兼容旧引用：不再使用
}


