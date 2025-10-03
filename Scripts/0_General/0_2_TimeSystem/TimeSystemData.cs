using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 时间系统数据结构（可序列化）
/// 用于存档保存和读取
/// </summary>
[System.Serializable]
public class TimeSystemData
{
    // ========== 天数与时段 ==========
    public int currentDay = 1;
    public TimePhase currentPhase = TimePhase.Morning;
    public DaySubPhase daySubPhase = DaySubPhase.MorningStocking;

    // ========== 游戏时钟快照 ==========
    public int clockHour = 8;
    public int clockMinute = 0;
    public float phaseRemainingTime = 180f;  // 当前阶段剩余真实秒数

    // ========== 累计游玩时间 ==========
    public double totalPlayTimeSeconds = 0;

    // ========== 进度标记 ==========
    public int totalDaysCompleted = 0;       // 已完成天数
    public bool tutorialCompleted = false;   // 教学完成标记
}

