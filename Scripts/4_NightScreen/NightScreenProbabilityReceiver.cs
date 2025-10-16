using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// NightScreen场景中接收每日概率调整数据的控制器
/// 当消息传输到NightScreen后，在流程进入NightScreen时输出调整概率详细数值到控制台
/// </summary>
public class NightScreenProbabilityReceiver : MonoBehaviour
{
    [Title("概率数据接收器")]
    [LabelText("启用详细日志输出")][SerializeField] private bool enableDetailedLogging = true;
    [LabelText("启用完整25组合输出")][SerializeField] private bool enableFullCombinationOutput = false;
    
    [Title("运行时状态")]
    [LabelText("当前概率数据")][ShowInInspector][ReadOnly] private DailyProbabilityToNight? currentProbabilityData;
    [LabelText("是否已接收数据")][ShowInInspector][ReadOnly] private bool hasReceivedData = false;
    [LabelText("接收时间")][ShowInInspector][ReadOnly] private string receiveTime = "";
    
    private void OnEnable()
    {
        // 订阅每日概率传递消息
        MessageManager.Register<DailyProbabilityToNight>(MessageDefine.DAILY_PROBABILITY_TO_NIGHT, OnDailyProbabilityReceived);
        
        Debug.Log("[NightScreen] 概率数据接收器已启动，等待每日消息传递...");
    }
    
    private void OnDisable()
    {
        // 取消订阅
        MessageManager.Remove<DailyProbabilityToNight>(MessageDefine.DAILY_PROBABILITY_TO_NIGHT, OnDailyProbabilityReceived);
    }
    
    private void Start()
    {
        // 如果在Start时已经有数据，立即输出
        if (hasReceivedData && currentProbabilityData.HasValue)
        {
            OutputProbabilityDetails(currentProbabilityData.Value);
        }
        else
        {
            // 尝试从持久化存储中读取数据；若仍不可用，构造基础4%兜底
            LoadProbabilityDataFromPersistent();
            if (!hasReceivedData)
            {
                BuildAndBroadcastBaseline();
            }
        }
    }
    
    /// <summary>
    /// 接收每日概率数据的回调
    /// </summary>
    private void OnDailyProbabilityReceived(DailyProbabilityToNight data)
    {
        currentProbabilityData = data;
        hasReceivedData = true;
        receiveTime = System.DateTime.Now.ToString("HH:mm:ss");
        
        Debug.Log($"[NightScreen] 接收到每日概率数据: {data}");
        
        // 立即输出详细信息到控制台
        OutputProbabilityDetails(data);

        // 在夜晚场景内广播：概率数据就绪
        MessageManager.Send(MessageDefine.NIGHT_PROBABILITY_READY, data);
    }
    
    /// <summary>
    /// 输出概率调整详细信息到控制台
    /// </summary>
    private void OutputProbabilityDetails(DailyProbabilityToNight data)
    {
        Debug.Log("=".PadRight(80, '='));
        Debug.Log($"[NightScreen] 每日概率调整详细数值输出");
        Debug.Log($"游戏天数: {data.currentDay}");
        Debug.Log($"每日消息: {data.messageId}《{data.messageTitle}》");
        Debug.Log($"接收时间: {receiveTime}");
        Debug.Log("=".PadRight(80, '='));
        
        if (data.probabilityResult != null)
        {
            // 输出计算摘要
            Debug.Log(data.probabilityResult.calculationSummary);
            
            if (enableDetailedLogging)
            {
                OutputAdjustedCombinations(data.probabilityResult);
            }
            
            if (enableFullCombinationOutput)
            {
                OutputAllCombinations(data.probabilityResult);
            }
            
            OutputStatistics(data.probabilityResult);
        }
        
        Debug.Log("=".PadRight(80, '='));
    }
    
    /// <summary>
    /// 输出受调整影响的组合
    /// </summary>
    private void OutputAdjustedCombinations(VisitProbabilityCalculator.ProbabilityResult result)
    {
        Debug.Log("[NightScreen] 受调整影响的组合详情:");
        
        int adjustedCount = 0;
        foreach (var combo in result.combinations)
        {
            if (Mathf.Abs(combo.adjustment) > 0.01f)
            {
                Debug.Log($"  {combo}");
                adjustedCount++;
            }
        }
        
        if (adjustedCount == 0)
        {
            Debug.Log("  无组合受到调整影响（所有组合保持基础4%概率）");
        }
        else
        {
            Debug.Log($"  共 {adjustedCount} 个组合受到调整影响");
        }
    }
    
    /// <summary>
    /// 输出所有25个组合的完整信息
    /// </summary>
    private void OutputAllCombinations(VisitProbabilityCalculator.ProbabilityResult result)
    {
        Debug.Log("[NightScreen] 完整25组合概率分布:");
        
        foreach (var identity in VisitProbabilityCalculator.IDENTITIES)
        {
            Debug.Log($"  === {identity} ===");
            foreach (var state in VisitProbabilityCalculator.STATES)
            {
                var combo = VisitProbabilityCalculator.GetCombinationProbability(result, identity, state);
                if (combo != null)
                {
                    Debug.Log($"    {combo}");
                }
            }
        }
    }
    
    /// <summary>
    /// 输出统计信息
    /// </summary>
    private void OutputStatistics(VisitProbabilityCalculator.ProbabilityResult result)
    {
        Debug.Log("[NightScreen] 概率分布统计:");
        
        float minProb = float.MaxValue;
        float maxProb = float.MinValue;
        float totalProb = 0f;
        int adjustedCount = 0;
        
        foreach (var combo in result.combinations)
        {
            if (combo.normalizedProbability < minProb) minProb = combo.normalizedProbability;
            if (combo.normalizedProbability > maxProb) maxProb = combo.normalizedProbability;
            totalProb += combo.normalizedProbability;
            if (Mathf.Abs(combo.adjustment) > 0.01f) adjustedCount++;
        }
        
        Debug.Log($"  最小概率: {minProb:F2}%");
        Debug.Log($"  最大概率: {maxProb:F2}%");
        Debug.Log($"  总概率: {totalProb:F2}% (应为100%)");
        Debug.Log($"  受调整组合数: {adjustedCount}/{result.combinations.Count}");
        Debug.Log($"  归一化因子: {result.normalizationFactor:F4}");
        Debug.Log($"  截断后总和: {result.totalClampedSum:F2}%");
    }
    
    /// <summary>
    /// 手动触发概率输出（调试用）
    /// </summary>
    [Button("手动输出当前概率数据")]
    private void ManualOutputProbabilityData()
    {
        if (hasReceivedData && currentProbabilityData.HasValue)
        {
            OutputProbabilityDetails(currentProbabilityData.Value);
        }
        else
        {
            Debug.LogWarning("[NightScreen] 尚未接收到概率数据，无法输出");
        }
    }
    
    /// <summary>
    /// 从持久化存储中读取概率数据
    /// </summary>
    private void LoadProbabilityDataFromPersistent()
    {
        try
        {
            if (PlayerPrefs.HasKey("DailyProbabilityData"))
            {
                string json = PlayerPrefs.GetString("DailyProbabilityData");
                int savedDay = PlayerPrefs.GetInt("DailyProbabilityDay", -1);
                
                if (!string.IsNullOrEmpty(json))
                {
                    var data = JsonUtility.FromJson<DailyProbabilityToNight>(json);
                    currentProbabilityData = data;
                    hasReceivedData = true;
                    receiveTime = "从存储读取";
                    
                    Debug.Log($"[NightScreen] 从持久化存储读取到概率数据: 天数{savedDay}, 消息{data.messageId}");
                    
                    // 立即输出详细信息
                    OutputProbabilityDetails(data);

                    // 在夜晚场景内广播：概率数据就绪
                    MessageManager.Send(MessageDefine.NIGHT_PROBABILITY_READY, data);
                }
                else
                {
                    Debug.LogWarning("[NightScreen] 持久化存储中的概率数据为空");
                }
            }
            else
            {
                Debug.Log("[NightScreen] 持久化存储中未找到概率数据，等待消息传递...");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[NightScreen] 从持久化存储读取概率数据失败: {e.Message}");
        }
    }

    /// <summary>
    /// 构造并广播基础模型（25 组合各 4% 基线）作为兜底。
    /// </summary>
    private void BuildAndBroadcastBaseline()
    {
        var baseline = new DailyProbabilityToNight
        {
            messageId = "baseline",
            messageTitle = "Baseline 4%",
            currentDay = TimeSystemManager.Instance != null ? TimeSystemManager.Instance.CurrentDay : 0,
            probabilityResult = VisitProbabilityCalculator.CalculateProbabilities(null),
            originalAdjustments = null
        };

        currentProbabilityData = baseline;
        hasReceivedData = true;
        receiveTime = "兜底: 基线4%";

        Debug.Log("[NightScreen] 未获取每日调整，使用基线4%兜底概率。");
        OutputProbabilityDetails(baseline);

        // 夜晚场景内广播：概率数据就绪（兜底）
        MessageManager.Send(MessageDefine.NIGHT_PROBABILITY_READY, baseline);
    }
    
    /// <summary>
    /// 清除当前数据（调试用）
    /// </summary>
    [Button("清除当前数据")]
    private void ClearCurrentData()
    {
        currentProbabilityData = null;
        hasReceivedData = false;
        receiveTime = "";
        
        // 同时清除持久化存储
        PlayerPrefs.DeleteKey("DailyProbabilityData");
        PlayerPrefs.DeleteKey("DailyProbabilityDay");
        PlayerPrefs.Save();
        
        Debug.Log("[NightScreen] 已清除当前概率数据和持久化存储");
    }
    
    /// <summary>
    /// 手动从持久化存储重新加载数据（调试用）
    /// </summary>
    [Button("重新加载持久化数据")]
    private void ReloadPersistentData()
    {
        LoadProbabilityDataFromPersistent();
    }
}
