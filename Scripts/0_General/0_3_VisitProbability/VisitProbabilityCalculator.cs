using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 新版25组合到访概率计算器
/// 5种身份 × 5种状态 = 25种组合，每组合基础4%，支持每日消息调整、截断保护、归一化处理
/// </summary>
public static class VisitProbabilityCalculator
{
    // 5种身份
    public static readonly string[] IDENTITIES = { "CompanyEmployee", "SmallLeader", "Freelancer", "Boss", "Student" };
    
    // 5种状态
    public static readonly string[] STATES = { "Busy", "Irritable", "Melancholy", "Picky", "Friendly" };
    
    // 2种性别
    public static readonly string[] GENDERS = { "male", "female" };
    
    // 设计参数
    public const float BASE_PROBABILITY_PERCENT = 4.0f;  // 每组合基础概率4%
    public const float MIN_CLAMP_PERCENT = 0.5f;         // 最小截断0.5%
    public const float MAX_CLAMP_PERCENT = 30.0f;        // 最大截断30%
    public const float ADJUSTMENT_RANGE_MIN = -15.0f;    // 调整范围最小-15%
    public const float ADJUSTMENT_RANGE_MAX = 15.0f;     // 调整范围最大+15%
    
    /// <summary>
    /// 25组合概率数据结构
    /// </summary>
    [Serializable]
    public class CombinationProbability
    {
        public string identity;
        public string state;
        public float baseProbability;      // 基础概率4%
        public float adjustment;           // 每日消息调整值
        public float rawProbability;       // 调整后原始概率
        public float clampedProbability;   // 截断后概率
        public float normalizedProbability; // 归一化后概率
        public float maleProbability;      // 男性概率（归一化后÷2）
        public float femaleProbability;    // 女性概率（归一化后÷2）
        
        public CombinationProbability(string identity, string state)
        {
            this.identity = identity;
            this.state = state;
            this.baseProbability = BASE_PROBABILITY_PERCENT;
            this.adjustment = 0f;
            RecalculateAll();
        }
        
        public void SetAdjustment(float delta)
        {
            this.adjustment = Mathf.Clamp(delta, ADJUSTMENT_RANGE_MIN, ADJUSTMENT_RANGE_MAX);
            RecalculateRaw();
        }
        
        private void RecalculateRaw()
        {
            rawProbability = baseProbability + adjustment;
            clampedProbability = Mathf.Clamp(rawProbability, MIN_CLAMP_PERCENT, MAX_CLAMP_PERCENT);
        }
        
        public void SetNormalizedProbability(float normalized)
        {
            normalizedProbability = normalized;
            maleProbability = normalized * 0.5f;
            femaleProbability = normalized * 0.5f;
        }
        
        private void RecalculateAll()
        {
            RecalculateRaw();
            // 归一化需要在外部统一计算
        }
        
        public override string ToString()
        {
            return $"{identity}×{state}: 基础{baseProbability:F1}% + 调整{adjustment:+0.0;-0.0;0.0}% = 原始{rawProbability:F1}% → 截断{clampedProbability:F1}% → 归一化{normalizedProbability:F2}% (男{maleProbability:F2}%/女{femaleProbability:F2}%)";
        }
    }
    
    /// <summary>
    /// 完整的概率计算结果
    /// </summary>
    [Serializable]
    public class ProbabilityResult
    {
        public List<CombinationProbability> combinations = new List<CombinationProbability>();
        public float totalClampedSum;      // 截断后总和
        public float normalizationFactor;  // 归一化因子
        public string calculationSummary;  // 计算摘要
        
        public override string ToString()
        {
            return $"25组合概率计算结果: 截断总和{totalClampedSum:F2}%, 归一化因子{normalizationFactor:F4}, 组合数{combinations.Count}";
        }
    }
    
    /// <summary>
    /// 计算25组合概率分布
    /// </summary>
    /// <param name="adjustments">每日消息调整列表</param>
    /// <returns>完整的概率计算结果</returns>
    public static ProbabilityResult CalculateProbabilities(List<DailyMessagesData.Adjustment> adjustments = null)
    {
        var result = new ProbabilityResult();
        
        // 1. 初始化25个组合
        foreach (var identity in IDENTITIES)
        {
            foreach (var state in STATES)
            {
                result.combinations.Add(new CombinationProbability(identity, state));
            }
        }
        
        // 2. 应用每日消息调整
        if (adjustments != null)
        {
            foreach (var adjustment in adjustments)
            {
                ApplyAdjustment(result.combinations, adjustment);
            }
        }
        
        // 3. 计算截断后总和
        result.totalClampedSum = 0f;
        foreach (var combo in result.combinations)
        {
            result.totalClampedSum += combo.clampedProbability;
        }
        
        // 4. 归一化处理
        if (result.totalClampedSum > 0f)
        {
            result.normalizationFactor = 100f / result.totalClampedSum;
            foreach (var combo in result.combinations)
            {
                float normalized = (combo.clampedProbability / result.totalClampedSum) * 100f;
                combo.SetNormalizedProbability(normalized);
            }
        }
        else
        {
            // 异常情况：所有概率都是0，平均分配
            result.normalizationFactor = 1f;
            float averageProbability = 100f / result.combinations.Count;
            foreach (var combo in result.combinations)
            {
                combo.SetNormalizedProbability(averageProbability);
            }
        }
        
        // 5. 生成计算摘要
        result.calculationSummary = GenerateCalculationSummary(result, adjustments);
        
        return result;
    }
    
    /// <summary>
    /// 应用单个调整到匹配的组合
    /// </summary>
    private static void ApplyAdjustment(List<CombinationProbability> combinations, DailyMessagesData.Adjustment adjustment)
    {
        foreach (var combo in combinations)
        {
            bool identityMatch = string.IsNullOrEmpty(adjustment.identity) || combo.identity == adjustment.identity;
            bool stateMatch = string.IsNullOrEmpty(adjustment.state) || combo.state == adjustment.state;
            
            if (identityMatch && stateMatch)
            {
                combo.SetAdjustment(combo.adjustment + adjustment.deltaPercent);
            }
        }
    }
    
    /// <summary>
    /// 生成计算摘要
    /// </summary>
    private static string GenerateCalculationSummary(ProbabilityResult result, List<DailyMessagesData.Adjustment> adjustments)
    {
        var summary = new System.Text.StringBuilder();
        summary.AppendLine("=== 25组合概率计算摘要 ===");
        summary.AppendLine($"基础模型: {IDENTITIES.Length}身份 × {STATES.Length}状态 = {result.combinations.Count}组合");
        summary.AppendLine($"基础概率: 每组合 {BASE_PROBABILITY_PERCENT}%");
        
        if (adjustments != null && adjustments.Count > 0)
        {
            summary.AppendLine($"每日消息调整: {adjustments.Count}项");
            foreach (var adj in adjustments)
            {
                summary.AppendLine($"  - {adj.identity}×{adj.state}: {adj.deltaPercent:+0.0;-0.0;0.0}%");
            }
        }
        else
        {
            summary.AppendLine("每日消息调整: 无");
        }
        
        summary.AppendLine($"截断保护: [{MIN_CLAMP_PERCENT}%, {MAX_CLAMP_PERCENT}%]");
        summary.AppendLine($"截断后总和: {result.totalClampedSum:F2}%");
        summary.AppendLine($"归一化因子: {result.normalizationFactor:F4}");
        summary.AppendLine($"最终总和: 100.00% (25组合 + 50性别分配)");
        
        return summary.ToString();
    }
    
    /// <summary>
    /// 获取特定身份+状态的概率信息
    /// </summary>
    public static CombinationProbability GetCombinationProbability(ProbabilityResult result, string identity, string state)
    {
        return result.combinations.Find(c => c.identity == identity && c.state == state);
    }
    
    /// <summary>
    /// 获取特定身份+状态+性别的最终概率
    /// </summary>
    public static float GetFinalProbability(ProbabilityResult result, string identity, string state, string gender)
    {
        var combo = GetCombinationProbability(result, identity, state);
        if (combo == null) return 0f;
        
        return gender.ToLower() == "male" ? combo.maleProbability : combo.femaleProbability;
    }
}
