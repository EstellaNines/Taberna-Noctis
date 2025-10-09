using UnityEngine;

/// <summary>
/// 场景时间协调器：监听阶段切换，并通过全局场景管理器(GlobalSceneManager)切换场景。
/// - 放置位置：建议放在 Start 场景的常驻对象上，Awake 中设置 DontDestroyOnLoad。
/// - Start/SaveFiles/Day/Night 均可复用同一实例。
/// </summary>
public class SceneTimeCoordinator : MonoBehaviour
{
    [Header("场景名映射")]
    [SerializeField] private string daySceneName = "3_DayScreen";
    [SerializeField] private string nightSceneName = "4_NightScreen";
    [SerializeField] private string settlementSceneName = "5_SettlementScreen";

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        MessageManager.Register<TimePhase>(MessageDefine.PHASE_CHANGED, OnPhaseChanged);
        // 夜晚完成后，直接跳转到结算场景
        MessageManager.Register<int>(MessageDefine.DAY_COMPLETED, OnDayCompleted);
    }

    private void OnDisable()
    {
        MessageManager.Remove<TimePhase>(MessageDefine.PHASE_CHANGED, OnPhaseChanged);
        MessageManager.Remove<int>(MessageDefine.DAY_COMPLETED, OnDayCompleted);
    }

    private void OnPhaseChanged(TimePhase phase)
    {
        switch (phase)
        {
            case TimePhase.Morning:
            case TimePhase.Afternoon:
                RequestLoad(daySceneName);
                break;
            case TimePhase.Night:
                RequestLoad(nightSceneName);
                break;
        }
    }

    private void OnDayCompleted(int day)
    {
        if (!string.IsNullOrEmpty(settlementSceneName))
        {
            RequestLoad(settlementSceneName);
        }
    }

    private void RequestLoad(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return;
        // 在进入 Loading 前先冻结计时
        if (TimeSystemManager.Instance != null)
        {
            TimeSystemManager.Instance.PauseTimer();
        }
        // 交由全局场景管理器处理：带 LoadingScreen 的异步切换
        GlobalSceneManager.LoadWithLoadingScreen(sceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
    }
}


