using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 游戏时钟类
/// 负责管理游戏内的时、分显示，并根据时间流速实时更新
/// </summary>
[System.Serializable]
public class GameClock
{
    // ========== 公共属性 ==========
    public int Hour { get; private set; }       // 0-23
    public int Minute { get; private set; }     // 0-59

    // ========== 内部字段 ==========
    private float accumulatedSeconds;           // 累积的游戏秒数
    private int startHour;                      // 阶段起始时
    private float timeScale;                    // 时间流速（游戏秒/真实秒）

    // ========== 核心方法 ==========

    /// <summary>
    /// 初始化游戏时钟
    /// </summary>
    /// <param name="hour">起始小时</param>
    /// <param name="minute">起始分钟</param>
    /// <param name="scale">时间流速</param>
    public void Initialize(int hour, int minute, float scale)
    {
        startHour = hour;
        Hour = hour;
        Minute = minute;
        timeScale = scale;
        accumulatedSeconds = 0;
    }

    /// <summary>
    /// 更新游戏时钟（每帧调用）
    /// </summary>
    /// <param name="deltaTime">真实时间增量</param>
    public void Update(float deltaTime)
    {
        // 累积游戏时间
        accumulatedSeconds += deltaTime * timeScale;

        // 转换为游戏分钟
        int totalMinutes = Mathf.FloorToInt(accumulatedSeconds / 60f);

        // 计算时、分
        Hour = startHour + (totalMinutes / 60);
        Minute = totalMinutes % 60;

        // 处理跨天（夜晚从19:00到次日3:00）
        if (Hour >= 24)
        {
            Hour -= 24;
        }
    }

    /// <summary>
    /// 获取格式化的时间字符串
    /// </summary>
    /// <returns>格式："08:35"</returns>
    public string GetTimeString()
    {
        return $"{Hour:D2}:{Minute:D2}";
    }

    /// <summary>
    /// 获取当前累积的游戏秒数（用于调试）
    /// </summary>
    public float GetAccumulatedSeconds()
    {
        return accumulatedSeconds;
    }
}

