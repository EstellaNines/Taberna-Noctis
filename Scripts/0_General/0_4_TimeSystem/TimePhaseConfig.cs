using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 时间阶段配置
/// 用于在Inspector中配置每个阶段的参数
/// </summary>
[System.Serializable]
public class TimePhaseConfig
{
    [Header("阶段基本信息")]
    public TimePhase phase;           // 阶段类型

    [Header("时间设置")]
    public float durationSeconds;     // 真实时长（秒）
    public int startHour;             // 游戏起始时
    public int startMinute;           // 游戏起始分
    public float timeScale;           // 时间流速（游戏秒/真实秒）

    [Header("场景信息")]
    [Tooltip("该阶段对应的场景名称")]
    public string sceneName;          // 对应场景名称（可选）
}

