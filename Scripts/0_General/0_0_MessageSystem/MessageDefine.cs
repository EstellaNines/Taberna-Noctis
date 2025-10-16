using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MessageDefine
{
    // 统一Key定义处，避免硬编码
    public static readonly string TEST_MESSAGE = "TEST_MESSAGE";
    public static readonly string LOADING_REQUEST = "LOADING_REQUEST";

    // ========== 时间系统消息 ==========
    public static readonly string TIME_SYSTEM_INITIALIZED = "TIME_SYSTEM_INITIALIZED";
    public static readonly string DAY_STARTED = "DAY_STARTED";              // 携带 int 天数
    public static readonly string PHASE_CHANGED = "PHASE_CHANGED";          // 携带 TimePhase
    public static readonly string DAY_SUBPHASE_CHANGED = "DAY_SUBPHASE_CHANGED"; // 携带 DaySubPhase
    public static readonly string GAME_CLOCK_TICK = "GAME_CLOCK_TICK";      // 携带 string "08:35"
    public static readonly string PHASE_TIME_WARNING = "PHASE_TIME_WARNING"; // 剩余<30秒
    public static readonly string DAY_COMPLETED = "DAY_COMPLETED";          // 夜晚结束，进入结算

    // ========== 存档系统消息 ==========
    public static readonly string SAVE_REQUESTED = "SAVE_REQUESTED";
    public static readonly string SAVE_COMPLETED = "SAVE_COMPLETED";
    public static readonly string SAVE_LOADED = "SAVE_LOADED";

    // ========== 每日消息系统 ==========
    public static readonly string DAILY_MESSAGE_APPLIED = "DAILY_MESSAGE_APPLIED";
    public static readonly string DAILY_PROBABILITY_TO_NIGHT = "DAILY_PROBABILITY_TO_NIGHT";
    // Night 场景内广播：概率数据就绪
    public static readonly string NIGHT_PROBABILITY_READY = "NIGHT_PROBABILITY_READY";
    
    // ========== 每日消息查看器 ==========
    // 可选：用于编辑器查看器主动请求刷新
    public static readonly string DAILY_MESSAGE_LOG_REFRESH = "DAILY_MESSAGE_LOG_REFRESH";
}
