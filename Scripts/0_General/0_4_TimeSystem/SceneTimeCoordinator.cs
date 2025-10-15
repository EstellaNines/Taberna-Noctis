using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 场景时间协调器：监听阶段切换，并通过全局场景管理器(GlobalSceneManager)切换场景。
/// - 放置位置：建议放在 Start 场景的常驻对象上，Awake 中设置 DontDestroyOnLoad。
/// - Start/SaveFiles/Day/Night 均可复用同一实例。
/// </summary>
public class SceneTimeCoordinator : MonoBehaviour
{
    [Header("场景名配置")]
#if ODIN_INSPECTOR
    [Sirenix.OdinInspector.LabelText("Day 场景名")]
#endif
    [SerializeField] private string daySceneName = "3_DayScreen";
#if ODIN_INSPECTOR
    [Sirenix.OdinInspector.LabelText("Night 场景名")]
#endif
    [SerializeField] private string nightSceneName = "4_NightScreen";

    // 避免在结算→新一天时，PHASE_CHANGED( Morning ) 抢先把场景切到 DayScreen
    private bool suppressNextPhaseChange; // 一次性屏蔽

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        // 改为严格依据 SceneSequenceConfig 的顺序前进，不再用阶段名硬编码场景
        MessageManager.Register<int>(MessageDefine.DAY_STARTED, OnDayStarted);
        MessageManager.Register<int>(MessageDefine.DAY_COMPLETED, OnDayCompleted);
        MessageManager.Register<TimePhase>(MessageDefine.PHASE_CHANGED, OnPhaseChanged);
        SceneManager.activeSceneChanged += OnActiveSceneChanged;
    }

    private void OnDisable()
    {
        MessageManager.Remove<int>(MessageDefine.DAY_STARTED, OnDayStarted);
        MessageManager.Remove<int>(MessageDefine.DAY_COMPLETED, OnDayCompleted);
        MessageManager.Remove<TimePhase>(MessageDefine.PHASE_CHANGED, OnPhaseChanged);
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
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
            // 抑制紧随其后的 PHASE_CHANGED(Morning)
            suppressNextPhaseChange = true;
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

    private void OnPhaseChanged(TimePhase phase)
    {
        if (suppressNextPhaseChange)
        {
            suppressNextPhaseChange = false;
            return;
        }

        // 早上/下午 → Day 场景；夜晚 → Night 场景
        string target = (phase == TimePhase.Night) ? nightSceneName : daySceneName;

        // 若已经在目标场景则不重复加载
        var active = SceneManager.GetActiveScene().name;
        // 在 DayMessageScreen 期间忽略阶段切换（由流程按钮推进）
        if (string.Equals(active, "2_DayMessageScreen", System.StringComparison.Ordinal)) return;
        // 在结算期间忽略阶段切换（由玩家确认后开始新一天）
        if (string.Equals(active, "5_SettlementScreen", System.StringComparison.Ordinal)) return;
        if (string.Equals(active, target, System.StringComparison.Ordinal)) return;

        GlobalSceneManager.LoadWithLoadingScreen(target, LoadSceneMode.Single);
    }

    private void OnActiveSceneChanged(Scene oldScene, Scene newScene)
    {
        var tsm = TimeSystemManager.Instance;
        if (tsm == null) return;
        var name = newScene.name;
        if (string.Equals(name, "2_DayMessageScreen", System.StringComparison.Ordinal))
        {
            // 停在日间消息界面时冻结时间
            tsm.PauseTimer();
            return;
        }
        if (string.Equals(name, daySceneName, System.StringComparison.Ordinal) ||
            string.Equals(name, nightSceneName, System.StringComparison.Ordinal))
        {
            // 进入实际游玩场景时恢复计时（Loading 时由 TimeSystemManager 自己暂停/恢复）
            if (tsm.IsTimerPaused) tsm.ResumeTimer();
        }
    }

    // 兼容旧引用：不再使用
}


