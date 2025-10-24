using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

/// <summary>
/// 时间系统管理器（核心单例）
/// 负责管理游戏的天数、时段、游戏时钟、阶段推进等核心功能
/// </summary>
public class TimeSystemManager : MonoBehaviour
{
    // ========== 单例 ==========
    public static TimeSystemManager Instance { get; private set; }

    // ========== 核心数据 ==========
    [Header("当前状态")]
#if ODIN_INSPECTOR
    [LabelText("当前天数")]
#endif
    [SerializeField] private int currentDay = 1;
#if ODIN_INSPECTOR
    [LabelText("当前阶段")]
#endif
    [SerializeField] private TimePhase currentPhase = TimePhase.Morning;
#if ODIN_INSPECTOR
    [LabelText("白天子阶段")]
#endif
    [SerializeField] private DaySubPhase daySubPhase = DaySubPhase.MorningStocking;

    // ========== 游戏时钟 ==========
    private GameClock gameClock;

    // ========== 阶段计时器 ==========
    private float phaseTimer;              // 已过去的真实秒数
    private float phaseDuration;           // 当前阶段总时长
    private bool isTimerPaused = false;    // 计时器是否暂停
    private bool inSettlementFlow = false; // 是否处于“夜晚结束→结算界面”流程

    // ========== 累计游玩时间 ==========
    private double totalPlayTime = 0;
    private float globalTimeScale = 1f;     // 全局时间倍率（影响 deltaTime）

    // ========== 自动保存设置 ==========
#if ODIN_INSPECTOR
    [LabelText("启用自动保存")]
#endif
    [SerializeField] private bool enableAutoSave = true;
#if ODIN_INSPECTOR
    [LabelText("自动保存提前秒数")]
#endif
    [SerializeField, Min(0.1f)] private float autoSaveLeadSeconds = 2f; // 距离阶段结束还剩多少真实秒时触发
    private bool autoSaveFiredThisPhase = false; // 防重复

    // ========== 配置数据 ==========
    [Header("阶段配置")]
#if ODIN_INSPECTOR
    [LabelText("阶段配置列表")]
#endif
    [SerializeField] private TimePhaseConfig[] phaseConfigs;

    [Header("行为设置")]
    [Tooltip("当下午阶段到达18:00后，是否自动开始夜晚（跳转到NightScreen）\n关闭后将暂停等待手动开始营业")]
#if ODIN_INSPECTOR
    [LabelText("下午结束自动开始夜晚")]
#endif
    [SerializeField] private bool autoStartNightOnAfternoonEnd = true;

	[Header("加载场景设置")]
#if ODIN_INSPECTOR
	[LabelText("Loading 场景名")]
#endif
	[SerializeField] private string loadingScreenName = "S_LoadingScreen";

    // ========== 公共属性 ==========
    public int CurrentDay => currentDay;
    public TimePhase CurrentPhase => currentPhase;
    public DaySubPhase CurrentDaySubPhase => daySubPhase;
    public GameClock GameClock => gameClock;
    public float PhaseRemainingTime => Mathf.Max(0f, phaseDuration - phaseTimer);
    public float PhaseDuration => phaseDuration;
    public double TotalPlayTime => totalPlayTime;
    public bool IsTimerPaused => isTimerPaused;
    public float GlobalTimeScale => globalTimeScale;

    // ========== Unity生命周期 ==========

    void Awake()
    {
        // 单例模式
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 初始化游戏时钟
        gameClock = new GameClock();

        // 若未在 Inspector 配置，则使用默认阶段配置，保证可在任意场景直接工作
        EnsureDefaultPhaseConfigs();
    }

    void Start()
    {
        // 可选：根据当前激活场景设置起始阶段（用于直接运行某个场景时的纠偏）
        TrySetPhaseByActiveSceneOnStart();

        // 初始化第一个阶段
        InitializePhase(currentPhase);

        // 发送初始化完成消息
        MessageManager.Send(MessageDefine.TIME_SYSTEM_INITIALIZED);
        MessageManager.Send(MessageDefine.DAY_STARTED, currentDay);
    }

    void OnEnable()
    {
        SceneManager.activeSceneChanged += OnActiveSceneChanged;
    }

    void OnDisable()
    {
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
    }

    void Update()
    {
        if (isTimerPaused) return;

        float deltaTime = Time.deltaTime * Mathf.Max(0f, globalTimeScale);
#if UNITY_EDITOR
        // 调试时间倍率（仅编辑器）
        deltaTime *= debugTimeScale;
#endif

        // 更新累计游玩时间
        totalPlayTime += deltaTime;

        // 更新游戏时钟
        gameClock.Update(deltaTime);

        // 更新阶段计时器
        phaseTimer += deltaTime;

        // 自动保存：在阶段即将结束时
        if (enableAutoSave && !autoSaveFiredThisPhase)
        {
            float remaining = PhaseRemainingTime;
            if (remaining <= autoSaveLeadSeconds && remaining >= 0f)
            {
                TryAutoSaveForPhase();
                autoSaveFiredThisPhase = true;
            }
        }

        // 检查阶段是否结束
        if (phaseTimer >= phaseDuration)
        {
            AdvanceToNextPhase();
        }
    }

    // ========== 核心方法 ==========

    /// <summary>
    /// 初始化指定阶段
    /// </summary>
    private void InitializePhase(TimePhase phase)
    {
        TimePhaseConfig config = GetPhaseConfig(phase);
        if (config == null)
        {
            Debug.LogError($"[TimeSystem] 找不到阶段配置: {phase}");
            return;
        }

        // 重置计时器
        phaseTimer = 0f;
        phaseDuration = config.durationSeconds;
        autoSaveFiredThisPhase = false; // 新阶段开始，重置自动保存标记

        // 初始化游戏时钟
        gameClock.Initialize(config.startHour, config.startMinute, config.timeScale);

        Debug.Log($"[TimeSystem] 初始化阶段: {phase}, 时长={phaseDuration}秒, " +
                  $"起始时间={config.startHour:D2}:{config.startMinute:D2}, 流速={config.timeScale}");
    }

    [Header("启动阶段检测")]
#if ODIN_INSPECTOR
    [LabelText("按当前场景设置起始阶段")]
#endif
    [SerializeField] private bool setPhaseByActiveSceneOnStart = true;

    private void TrySetPhaseByActiveSceneOnStart()
    {
        if (!setPhaseByActiveSceneOnStart) return;
        var active = SceneManager.GetActiveScene().name;

        string morningScene = GetPhaseSceneNameSafe(TimePhase.Morning);
        string afternoonScene = GetPhaseSceneNameSafe(TimePhase.Afternoon);
        string nightScene = GetPhaseSceneNameSafe(TimePhase.Night);

        // Day 场景：早上起步（随后自动推进到下午）
        if (!string.IsNullOrEmpty(morningScene) && string.Equals(active, morningScene, StringComparison.Ordinal) ||
            !string.IsNullOrEmpty(afternoonScene) && string.Equals(active, afternoonScene, StringComparison.Ordinal))
        {
            currentPhase = TimePhase.Morning;
            daySubPhase = DaySubPhase.MorningStocking;
            isTimerPaused = false;
            Debug.Log("[TimeSystem] 依据当前场景设置起始阶段 -> Morning");
            return;
        }

        // Night 场景：直接夜晚
        if (!string.IsNullOrEmpty(nightScene) && string.Equals(active, nightScene, StringComparison.Ordinal))
        {
            currentPhase = TimePhase.Night;
            isTimerPaused = false;
            Debug.Log("[TimeSystem] 依据当前场景设置起始阶段 -> Night");
            return;
        }
    }

    private string GetPhaseSceneNameSafe(TimePhase phase)
    {
        var cfg = GetPhaseConfig(phase);
        return cfg != null ? cfg.sceneName : string.Empty;
    }

    /// <summary>
    /// 推进到下一阶段
    /// </summary>
    private void AdvanceToNextPhase()
    {
        switch (currentPhase)
        {
            case TimePhase.Morning:
                // 早上 → 下午
                currentPhase = TimePhase.Afternoon;
                daySubPhase = DaySubPhase.AfternoonRecipe;
                InitializePhase(TimePhase.Afternoon);
                MessageManager.Send(MessageDefine.PHASE_CHANGED, currentPhase);
                MessageManager.Send(MessageDefine.DAY_SUBPHASE_CHANGED, daySubPhase);
                Debug.Log($"[TimeSystem] 早上结束，进入下午阶段");
                break;

            case TimePhase.Afternoon:
                // 下午结束：根据行为设置选择自动进入夜晚或等待手动开始
                if (autoStartNightOnAfternoonEnd)
                {
                    Debug.Log("[TimeSystem] 下午结束，自动开始夜晚");
                    StartNightPhase();
                }
                else
                {
                    daySubPhase = DaySubPhase.AfternoonMenu;
                    isTimerPaused = true;
                    MessageManager.Send(MessageDefine.DAY_SUBPHASE_CHANGED, daySubPhase);
                    Debug.Log($"[TimeSystem] 下午结束，等待玩家确认开始营业");
                }
                break;

            case TimePhase.Night:
                // 夜晚 → 结算
                isTimerPaused = true;
                inSettlementFlow = true;
                // 保底保存一次夜晚结束后的数据
                try
                {
                    if (SaveManager.Instance != null)
                    {
                        SaveManager.Instance.SaveAfterNight();
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[AutoSave] 夜晚结束保存失败: {e.Message}");
                }
                // 发送天数完成（携带当日天数）
                MessageManager.Send<int>(MessageDefine.DAY_COMPLETED, currentDay);
                Debug.Log($"[TimeSystem] 夜晚结束，进入结算");
                break;
        }
    }

    private void OnActiveSceneChanged(Scene oldScene, Scene newScene)
    {
        if (!string.IsNullOrEmpty(loadingScreenName) && string.Equals(newScene.name, loadingScreenName, System.StringComparison.Ordinal))
        {
            // 进入 Loading：标记为因加载而暂停
            PauseForLoading();
        }
        else
        {
            // 离开 Loading：仅当此前是因 Loading 暂停时才恢复
            if (pausedByLoading)
            {
                if (!inSettlementFlow)
                {
                    ResumeTimer();
                }
                else
                {
                    // 仍在结算流程：保持暂停，避免再次推进阶段
                    pausedByLoading = false; // 清除加载标记
                }
            }
        }
    }

    /// <summary>
    /// 开始夜晚阶段（玩家点击"开始营业"后调用）
    /// </summary>
    public void StartNightPhase()
    {
        currentPhase = TimePhase.Night;
        isTimerPaused = false;
        InitializePhase(TimePhase.Night);
        MessageManager.Send(MessageDefine.PHASE_CHANGED, currentPhase);
        Debug.Log($"[TimeSystem] 开始夜晚营业，第{currentDay}天");
    }

    /// <summary>
    /// 开始新的一天（结算后调用）
    /// </summary>
    public void StartNewDay()
    {
        // 仅允许在结算流程中调用，防止重复调用导致天数被加两次
        if (!inSettlementFlow)
        {
            Debug.LogWarning("[TimeSystem] StartNewDay 被忽略：当前不在结算流程，避免重复加天");
            return;
        }
        // 开始新一天前进行自动保存（保存点3）
        if (enableAutoSave && SaveManager.Instance != null)
        {
            try { SaveManager.Instance.SaveNewDay(); } catch { }
        }

        currentDay++;
        currentPhase = TimePhase.Morning;
        daySubPhase = DaySubPhase.MorningStocking;
        isTimerPaused = false;
        inSettlementFlow = false; // 退出结算流程
        InitializePhase(TimePhase.Morning);

        MessageManager.Send(MessageDefine.DAY_STARTED, currentDay);
        MessageManager.Send(MessageDefine.PHASE_CHANGED, currentPhase);
        MessageManager.Send(MessageDefine.DAY_SUBPHASE_CHANGED, daySubPhase);
        Debug.Log($"[TimeSystem] 开始新的一天：第{currentDay}天");
    }

    /// <summary>
    /// 根据当前阶段触发对应的自动保存
    /// </summary>
    private void TryAutoSaveForPhase()
    {
        if (SaveManager.Instance == null) return;
        try
        {
            if (currentPhase == TimePhase.Morning)
            {
                // 早上结束前：检查点保存
                SaveManager.Instance.SaveCheckpoint();
                Debug.Log("[AutoSave] 早上即将结束，已自动保存（Checkpoint）");
            }
            else if (currentPhase == TimePhase.Afternoon)
            {
                // 白天结束 → 夜晚前
                SaveManager.Instance.SaveBeforeNight();
                Debug.Log("[AutoSave] 下午即将结束，已自动保存（SaveBeforeNight）");
            }
            else if (currentPhase == TimePhase.Night)
            {
                // 夜晚结束 → 结算前
                SaveManager.Instance.SaveAfterNight();
                Debug.Log("[AutoSave] 夜晚即将结束，已自动保存（SaveAfterNight）");
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[AutoSave] 自动保存失败: {e.Message}");
        }
    }

    /// <summary>
    /// 暂停计时器
    /// </summary>
    private bool pausedByLoading = false; // 标记：是否因加载而暂停

    /// <summary>
    /// 一般性暂停（非加载原因）
    /// </summary>
    public void PauseTimer()
    {
        isTimerPaused = true;
        pausedByLoading = false;
        Debug.Log($"[TimeSystem] 计时器已暂停");
    }

    /// <summary>
    /// 因加载而暂停：用于 Loading 期间，离开 Loading 时会自动恢复
    /// </summary>
    public void PauseForLoading()
    {
        isTimerPaused = true;
        pausedByLoading = true;
        Debug.Log($"[TimeSystem] 因加载暂停计时");
    }

    /// <summary>
    /// 继续计时器
    /// </summary>
    public void ResumeTimer()
    {
        isTimerPaused = false;
        pausedByLoading = false;
        Debug.Log($"[TimeSystem] 计时器已恢复");
    }

    /// <summary>
    /// 设置全局时间倍率（仅影响时间系统的推进，不改 Unity 的 Time.timeScale）
    /// </summary>
    public void SetGlobalTimeScale(float scale)
    {
        globalTimeScale = Mathf.Max(0f, scale);
        Debug.Log($"[TimeSystem] 全局时间倍率 = {globalTimeScale:F2}");
    }

    /// <summary>
    /// 获取阶段配置
    /// </summary>
    private TimePhaseConfig GetPhaseConfig(TimePhase phase)
    {
        foreach (var config in phaseConfigs)
        {
            if (config.phase == phase)
            {
                return config;
            }
        }
        return null;
    }

    /// <summary>
    /// 若没有在 Inspector 配置阶段参数，则提供一组默认配置，确保本管理器可被动态创建并立即运行。
    /// </summary>
    private void EnsureDefaultPhaseConfigs()
    {
        if (phaseConfigs != null && phaseConfigs.Length > 0) return;

        phaseConfigs = new TimePhaseConfig[3];
		phaseConfigs[0] = new TimePhaseConfig
		{
			phase = TimePhase.Morning,
			durationSeconds = 180f,
			startHour = 8,
			startMinute = 0,
			timeScale = 80f,
            sceneName = "3_DayScreen"
		};
		phaseConfigs[1] = new TimePhaseConfig
		{
			phase = TimePhase.Afternoon,
			durationSeconds = 180f,
			startHour = 14,
			startMinute = 0,
			timeScale = 80f,
            sceneName = "4_AfternoonScreen"
		};
		phaseConfigs[2] = new TimePhaseConfig
		{
			phase = TimePhase.Night,
			durationSeconds = 300f,
			startHour = 19,
			startMinute = 0,
			timeScale = 96f,
            sceneName = "5_NightScreen"
		};
    }

    // ========== 提供给面板/工具的查询与跳转 ==========

    public struct PhaseInfo
    {
        public int startHour;
        public int startMinute;
        public float durationSeconds;
        public float timeScale;
    }

    /// <summary>
    /// 查询阶段配置的只读快照，供外部面板使用
    /// </summary>
    public bool TryGetPhaseInfo(TimePhase phase, out PhaseInfo info)
    {
        var cfg = GetPhaseConfig(phase);
        if (cfg == null)
        {
            info = default;
            return false;
        }
        info = new PhaseInfo
        {
            startHour = cfg.startHour,
            startMinute = cfg.startMinute,
            durationSeconds = cfg.durationSeconds,
            timeScale = cfg.timeScale
        };
        return true;
    }

    /// <summary>
    /// 跳转到指定阶段的指定时刻（hour:minute），用于编辑器面板快速定位。
    /// </summary>
    public void JumpToPhaseTime(TimePhase targetPhase, int hour, int minute)
    {
        var cfg = GetPhaseConfig(targetPhase);
        if (cfg == null) return;

        // 切换到目标阶段并复位计时
        currentPhase = targetPhase;
        isTimerPaused = false;
        phaseTimer = 0f;
        phaseDuration = cfg.durationSeconds;
        gameClock.Initialize(cfg.startHour, cfg.startMinute, cfg.timeScale);

        // 计算从阶段起点到目标时刻的“游戏秒”，再折算为需要推进的真实秒
        int offsetMinutes = (hour - cfg.startHour) * 60 + (minute - cfg.startMinute);
        if (offsetMinutes < 0) offsetMinutes = 0;

        // 最大分钟数 = 阶段的游戏总时长（秒）/60 = durationSeconds*timeScale/60
        int maxMinutes = Mathf.FloorToInt((cfg.durationSeconds * cfg.timeScale) / 60f);
        if (offsetMinutes > maxMinutes) offsetMinutes = maxMinutes;

        float offsetGameSeconds = offsetMinutes * 60f;
        float requiredRealSeconds = offsetGameSeconds / Mathf.Max(1e-6f, cfg.timeScale);

        // 推进时钟到目标时间，并同步阶段计时器
        gameClock.Update(requiredRealSeconds);
        phaseTimer = Mathf.Min(requiredRealSeconds, phaseDuration);

        // 仅通知时间被设置（不发 PHASE_CHANGED，避免 SceneTimeCoordinator 触发切场景）
        if (gameClock != null)
        {
            MessageManager.Send(MessageDefine.GAME_CLOCK_TICK, gameClock.GetTimeString());
        }
    }

    // ========== 存档接口 ==========

    /// <summary>
    /// 生成时间系统数据快照（用于存档）
    /// </summary>
    public TimeSystemData GenerateTimeSystemData()
    {
        TimeSystemData data = new TimeSystemData
        {
            currentDay = currentDay,
            currentPhase = currentPhase,
            daySubPhase = daySubPhase,
            clockHour = gameClock.Hour,
            clockMinute = gameClock.Minute,
            phaseRemainingTime = PhaseRemainingTime,
            totalPlayTimeSeconds = totalPlayTime,
            totalDaysCompleted = currentDay - 1
        };

        return data;
    }

    /// <summary>
    /// 加载时间系统数据（用于读档）
    /// </summary>
    public void LoadTimeSystemData(TimeSystemData data)
    {
        currentDay = data.currentDay;
        currentPhase = data.currentPhase;
        daySubPhase = data.daySubPhase;
        totalPlayTime = data.totalPlayTimeSeconds;

        // 恢复时钟状态
        TimePhaseConfig config = GetPhaseConfig(currentPhase);
        if (config != null)
        {
            gameClock.Initialize(data.clockHour, data.clockMinute, config.timeScale);
            phaseTimer = config.durationSeconds - data.phaseRemainingTime;
            phaseDuration = config.durationSeconds;
        }

        Debug.Log($"[TimeSystem] 读档完成: 第{currentDay}天, {currentPhase}, " +
                  $"{gameClock.GetTimeString()}, 剩余{data.phaseRemainingTime:F0}秒");

        // 发送恢复消息
        MessageManager.Send(MessageDefine.DAY_STARTED, currentDay);
        MessageManager.Send(MessageDefine.PHASE_CHANGED, currentPhase);
    }

    // ========== 调试功能 ==========

#if UNITY_EDITOR
    [Header("调试设置")]
#if ODIN_INSPECTOR
    [LabelText("调试时间倍率")]
#endif
    [SerializeField] private float debugTimeScale = 1f;

    [ContextMenu("跳转到下午")]
    private void DebugJumpToAfternoon()
    {
        currentPhase = TimePhase.Afternoon;
        daySubPhase = DaySubPhase.AfternoonRecipe;
        InitializePhase(TimePhase.Afternoon);
        Debug.Log("[TimeSystem] 调试：跳转到下午");
    }

    [ContextMenu("跳转到夜晚")]
    private void DebugJumpToNight()
    {
        currentPhase = TimePhase.Night;
        isTimerPaused = false;
        InitializePhase(TimePhase.Night);
        Debug.Log("[TimeSystem] 调试：跳转到夜晚");
    }

    [ContextMenu("打印当前状态")]
    private void DebugPrintStatus()
    {
        Debug.Log($"========== 时间系统状态 ==========");
        Debug.Log($"天数: {currentDay}");
        Debug.Log($"阶段: {currentPhase}");
        Debug.Log($"子阶段: {daySubPhase}");
        Debug.Log($"游戏时间: {gameClock.GetTimeString()}");
        Debug.Log($"剩余时间: {PhaseRemainingTime:F1}秒");
        Debug.Log($"暂停状态: {isTimerPaused}");
        Debug.Log($"累计游玩: {totalPlayTime:F0}秒");
        Debug.Log($"============================");
    }
#endif
}

