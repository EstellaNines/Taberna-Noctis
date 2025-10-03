using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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

    // ========== 累计游玩时间 ==========
    private double totalPlayTime = 0;

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

    // ========== 公共属性 ==========
    public int CurrentDay => currentDay;
    public TimePhase CurrentPhase => currentPhase;
    public DaySubPhase CurrentDaySubPhase => daySubPhase;
    public GameClock GameClock => gameClock;
    public float PhaseRemainingTime => phaseDuration - phaseTimer;
    public float PhaseDuration => phaseDuration;
    public double TotalPlayTime => totalPlayTime;
    public bool IsTimerPaused => isTimerPaused;

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
        // 初始化第一个阶段
        InitializePhase(currentPhase);

        // 发送初始化完成消息
        MessageManager.Send(MessageDefine.TIME_SYSTEM_INITIALIZED);
        MessageManager.Send(MessageDefine.DAY_STARTED, currentDay);
    }

    void Update()
    {
        if (isTimerPaused) return;

        float deltaTime = Time.deltaTime;
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

        // 初始化游戏时钟
        gameClock.Initialize(config.startHour, config.startMinute, config.timeScale);

        Debug.Log($"[TimeSystem] 初始化阶段: {phase}, 时长={phaseDuration}秒, " +
                  $"起始时间={config.startHour:D2}:{config.startMinute:D2}, 流速={config.timeScale}");
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
                MessageManager.Send(MessageDefine.DAY_COMPLETED);
                Debug.Log($"[TimeSystem] 夜晚结束，进入结算");
                break;
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
        currentDay++;
        currentPhase = TimePhase.Morning;
        daySubPhase = DaySubPhase.MorningStocking;
        isTimerPaused = false;
        InitializePhase(TimePhase.Morning);

        MessageManager.Send(MessageDefine.DAY_STARTED, currentDay);
        MessageManager.Send(MessageDefine.PHASE_CHANGED, currentPhase);
        MessageManager.Send(MessageDefine.DAY_SUBPHASE_CHANGED, daySubPhase);
        Debug.Log($"[TimeSystem] 开始新的一天：第{currentDay}天");
    }

    /// <summary>
    /// 暂停计时器
    /// </summary>
    public void PauseTimer()
    {
        isTimerPaused = true;
        Debug.Log($"[TimeSystem] 计时器已暂停");
    }

    /// <summary>
    /// 继续计时器
    /// </summary>
    public void ResumeTimer()
    {
        isTimerPaused = false;
        Debug.Log($"[TimeSystem] 计时器已恢复");
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
            sceneName = "DayScreen"
        };
        phaseConfigs[1] = new TimePhaseConfig
        {
            phase = TimePhase.Afternoon,
            durationSeconds = 180f,
            startHour = 14,
            startMinute = 0,
            timeScale = 80f,
            sceneName = "DayScreen"
        };
        phaseConfigs[2] = new TimePhaseConfig
        {
            phase = TimePhase.Night,
            durationSeconds = 300f,
            startHour = 19,
            startMinute = 0,
            timeScale = 96f,
            sceneName = "NightScreen"
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

