using System;
using UnityEngine;

/// <summary>
/// 顾客监控数据结构：用于在监控器中显示顾客信息
/// </summary>
[Serializable]
public class CustomerMonitorData
{
    [Header("基本信息")]
    public int sequence;                    // 序号
    public string customerName;             // 角色名称
    public float currentProbability;        // 当前概率（%）
    public string customerIndex;            // 角色索引号（如：CompanyEmployee_001_M）
    
    [Header("详细信息")]
    public string identityId;               // 身份ID（如：CompanyEmployee）
    public string state;                    // 状态（Busy/Irritable/Melancholy/Picky/Friendly）
    public string gender;                   // 性别（male/female）
    public int initialMood;                 // 初始心情值
    public string portraitPath;             // 立绘路径
    
    [Header("运行时状态")]
    public CustomerStatus status;           // 当前状态
    public int queuePosition;               // 队列位置（-1表示不在队列中）
    public int cooldownRemaining;           // 剩余冷却计数
    public DateTime lastUpdateTime;         // 最后更新时间
    public bool isInGuaranteePool;          // 是否在保底池中
    
    [Header("统计信息")]
    public int totalVisits;                 // 总到访次数
    public int totalSpawns;                 // 总生成次数
    public DateTime lastVisitTime;          // 最后到访时间
    public DateTime lastSpawnTime;          // 最后生成时间

    public CustomerMonitorData()
    {
        lastUpdateTime = DateTime.Now;
        status = CustomerStatus.Available;
        queuePosition = -1;
        cooldownRemaining = 0;
        totalVisits = 0;
        totalSpawns = 0;
    }

    public CustomerMonitorData(NpcCharacterData npc, int seq) : this()
    {
        sequence = seq;
        customerName = npc.name;
        customerIndex = npc.id;
        identityId = npc.identityId;
        state = npc.state;
        gender = npc.gender;
        initialMood = npc.initialMood;
        portraitPath = npc.portraitPath;
        currentProbability = 0f; // 将在运行时计算
    }

    /// <summary>
    /// 更新概率数据
    /// </summary>
    public void UpdateProbability(float probability)
    {
        currentProbability = probability;
        lastUpdateTime = DateTime.Now;
    }

    /// <summary>
    /// 更新状态
    /// </summary>
    public void UpdateStatus(CustomerStatus newStatus, int queuePos = -1, int cooldown = 0)
    {
        status = newStatus;
        queuePosition = queuePos;
        cooldownRemaining = cooldown;
        lastUpdateTime = DateTime.Now;

        // 更新统计
        switch (newStatus)
        {
            case CustomerStatus.Spawned:
                totalSpawns++;
                lastSpawnTime = DateTime.Now;
                break;
            case CustomerStatus.Visited:
                totalVisits++;
                lastVisitTime = DateTime.Now;
                break;
        }
    }

    /// <summary>
    /// 获取状态显示文本
    /// </summary>
    public string GetStatusText()
    {
        switch (status)
        {
            case CustomerStatus.Available:
                return "可用";
            case CustomerStatus.InQueue:
                return $"队列中 (#{queuePosition})";
            case CustomerStatus.Cooldown:
                return $"冷却中 ({cooldownRemaining})";
            case CustomerStatus.Spawned:
                return "已生成";
            case CustomerStatus.Visited:
                return "已到访";
            case CustomerStatus.Guarantee:
                return "保底池";
            default:
                return "未知";
        }
    }

    /// <summary>
    /// 获取状态颜色
    /// </summary>
    public Color GetStatusColor()
    {
        switch (status)
        {
            case CustomerStatus.Available:
                return Color.green;
            case CustomerStatus.InQueue:
                return Color.cyan;
            case CustomerStatus.Cooldown:
                return Color.yellow;
            case CustomerStatus.Spawned:
                return Color.blue;
            case CustomerStatus.Visited:
                return Color.magenta;
            case CustomerStatus.Guarantee:
                return Color.red;
            default:
                return Color.gray;
        }
    }

    /// <summary>
    /// 更新状态
    /// </summary>
    public void UpdateStatus(CustomerStatus newStatus)
    {
        status = newStatus;
        lastUpdateTime = DateTime.Now;
        
        // 根据状态更新相关字段
        switch (newStatus)
        {
            case CustomerStatus.Spawned:
                totalSpawns++;
                lastSpawnTime = DateTime.Now;
                break;
            case CustomerStatus.Visited:
                totalVisits++;
                lastVisitTime = DateTime.Now;
                break;
        }
    }

    public override string ToString()
    {
        return $"[{sequence:D2}] {customerName} ({currentProbability:F2}%) - {GetStatusText()}";
    }
}

/// <summary>
/// 顾客状态枚举
/// </summary>
public enum CustomerStatus
{
    Available,      // 可用（在可用池中）
    InQueue,        // 在队列中
    Cooldown,       // 冷却中
    Spawned,        // 已生成
    Visited,        // 已到访
    Guarantee       // 在保底池中
}
