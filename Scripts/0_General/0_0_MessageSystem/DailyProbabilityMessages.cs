using System.Collections.Generic;

/// <summary>
/// 每日概率相关的消息数据结构
/// </summary>

/// <summary>
/// 传递到NightScreen的概率调整数据
/// </summary>
[System.Serializable]
public struct DailyProbabilityToNight
{
    public string messageId;           // 每日消息ID
    public string messageTitle;        // 每日消息标题
    public int currentDay;            // 当前游戏天数
    public VisitProbabilityCalculator.ProbabilityResult probabilityResult; // 完整的概率计算结果
    public List<DailyMessagesData.Adjustment> originalAdjustments; // 原始调整数据
    
    public override string ToString()
    {
        return $"每日概率数据 - 天数:{currentDay}, 消息:{messageId}《{messageTitle}》, 调整项:{originalAdjustments?.Count ?? 0}个";
    }
}
