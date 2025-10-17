using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TabernaNoctis.QueueSystem;
using TabernaNoctis.CharacterDesign;
using Sirenix.OdinInspector;

/// <summary>
/// 顾客生成管理器：NightScreen场景中的顾客到访系统核心
/// - 基于每日概率进行加权随机抽取
/// - 管理队列（PooledQueue，容量18）
/// - 实现冷却机制（10位顾客到访后解冻）
/// - 保底机制（队列达到15位时触发）
/// - 跨天持久化状态
/// </summary>
public class CustomerSpawnManager : MonoBehaviour
{
    public static CustomerSpawnManager Instance { get; private set; }

    [Title("生成配置")]
    [LabelText("基础生成间隔（秒）")][SerializeField] private float baseSpawnInterval = 20f;
    [LabelText("队列容量")][SerializeField] private int queueCapacity = 18;
    [LabelText("保底阈值")][SerializeField] private int guaranteeThreshold = 15;
    [LabelText("冷却要求（到访数）")][SerializeField] private int cooldownRequirement = 10;
    [LabelText("初始快速生成数量")][SerializeField] private int initialFastSpawnCount = 5;

    [Title("运行时状态")]
    [LabelText("是否正在生成")][ShowInInspector][ReadOnly] private bool isSpawning = false;
    [LabelText("生成计时器")][ShowInInspector][ReadOnly] private float spawnTimer = 0f;
    [LabelText("队列数量")][ShowInInspector][ReadOnly] private int queueCount = 0;
    [LabelText("可用池数量")][ShowInInspector][ReadOnly] private int availableCount = 0;
    [LabelText("冷却池数量")][ShowInInspector][ReadOnly] private int cooldownCount = 0;
    [LabelText("全局到访计数")][ShowInInspector][ReadOnly] private int globalVisitorCount = 0;
    [LabelText("总生成计数")][ShowInInspector][ReadOnly] private int totalSpawnedCount = 0;

    // 核心数据
    private PooledQueue<NpcCharacterData> customerQueue;
    private List<NpcCharacterData> availablePool = new List<NpcCharacterData>();
    private Dictionary<string, int> cooldownPool = new Dictionary<string, int>();
    private List<string> guaranteeIds = new List<string>(3);
    private HashSet<string> visitedIds = new HashSet<string>();

    [Title("数据引用")]
    [LabelText("角色索引SO")][SerializeField] private CharacterRolesIndex characterRolesIndex;
    [LabelText("NPC数据库SO（可选）")][SerializeField] private NpcDatabase npcDatabase;

    // 依赖数据
    private DailyProbabilityToNight? probabilityData;
    private Dictionary<string, NpcCharacterData> npcLookup = new Dictionary<string, NpcCharacterData>();

    // 状态标记
    private bool hasInitialized = false;
    private bool hasReceivedProbability = false;

    #region Unity 生命周期

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        InitializeSystem();
    }

    private void OnEnable()
    {
        // 订阅消息
        MessageManager.Register<DailyProbabilityToNight>(MessageDefine.NIGHT_PROBABILITY_READY, OnProbabilityReady);
        MessageManager.Register<NightCustomerState>(MessageDefine.CUSTOMER_STATE_LOADED, OnCustomerStateLoaded);
        MessageManager.Register<NpcCharacterData>(MessageDefine.CUSTOMER_VISITED, OnCustomerVisited);
    }

    private void OnDisable()
    {
        // 取消订阅
        MessageManager.Remove<DailyProbabilityToNight>(MessageDefine.NIGHT_PROBABILITY_READY, OnProbabilityReady);
        MessageManager.Remove<NightCustomerState>(MessageDefine.CUSTOMER_STATE_LOADED, OnCustomerStateLoaded);
        MessageManager.Remove<NpcCharacterData>(MessageDefine.CUSTOMER_VISITED, OnCustomerVisited);
    }

    private void Update()
    {
        if (!isSpawning || !hasReceivedProbability) return;

        // 前N个顾客已经在StartSpawning中立即生成，这里只处理后续的定时生成
        if (totalSpawnedCount < initialFastSpawnCount) return;

        // 使用时间倍率
        float effectiveDeltaTime = Time.deltaTime;
        if (TimeSystemManager.Instance != null)
        {
            effectiveDeltaTime *= TimeSystemManager.Instance.GlobalTimeScale;
        }

        spawnTimer += effectiveDeltaTime;

        if (spawnTimer >= baseSpawnInterval)
        {
            spawnTimer = 0f;
            TrySpawnCustomer();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    #endregion

    #region 初始化

    private void InitializeSystem()
    {
        Debug.Log("[CustomerSpawnManager] 初始化顾客生成系统...");

        // 1. 创建队列
        customerQueue = new PooledQueue<NpcCharacterData>(queueCapacity + 5);

        // 2. 加载 NPC 数据库
        LoadNpcDatabase();

        // 3. 尝试从保存系统恢复状态，否则创建默认状态
        if (!TryRestoreFromSave())
        {
            CreateDefaultState();
        }

        hasInitialized = true;
        Debug.Log($"[CustomerSpawnManager] 初始化完成 - 可用池:{availablePool.Count}, 冷却池:{cooldownPool.Count}");
    }

    private void LoadNpcDatabase()
    {
        Debug.Log("[CustomerSpawnManager] 开始加载 NPC 数据...");

        // 方法1：优先使用直接引用的 CharacterRolesIndex
        if (characterRolesIndex != null)
        {
            LoadFromCharacterRolesIndex();
            return;
        }

        // 方法2：使用直接引用的 NpcDatabase
        if (npcDatabase != null)
        {
            LoadFromNpcDatabase();
            return;
        }

        // 方法3：Fallback - 尝试从 Resources 加载 NpcDatabase
        npcDatabase = Resources.Load<NpcDatabase>("NpcDatabase");
        if (npcDatabase != null)
        {
            Debug.LogWarning("[CustomerSpawnManager] 使用 Resources.Load 加载 NpcDatabase（建议使用直接引用）");
            LoadFromNpcDatabase();
            return;
        }

        // 所有方法都失败
        Debug.LogError("[CustomerSpawnManager] 无法加载 NPC 数据！");
        Debug.LogError("[CustomerSpawnManager] 请在 Inspector 中设置 CharacterRolesIndex 或 NpcDatabase 引用");
        Debug.LogError("[CustomerSpawnManager] 或确保 NpcDatabase.asset 存在于 Resources 文件夹中");
    }

    private void LoadFromCharacterRolesIndex()
    {
        Debug.Log($"[CustomerSpawnManager] 从 CharacterRolesIndex 加载数据: {characterRolesIndex.name}");
        
        npcLookup.Clear();
        int totalNpcs = 0;

        // 遍历所有身份
        foreach (var roleData in characterRolesIndex.roles)
        {
            if (roleData == null) continue;

            Debug.Log($"[CustomerSpawnManager] 加载身份: {roleData.identityId}, NPC数量: {roleData.npcAssets.Count}");

            // 遍历该身份下的所有NPC
            foreach (var npc in roleData.npcAssets)
            {
                if (npc != null && !string.IsNullOrEmpty(npc.id))
                {
                    npcLookup[npc.id] = npc;
                    totalNpcs++;
                }
            }
        }

        Debug.Log($"[CustomerSpawnManager] 从 CharacterRolesIndex 加载完成，共 {totalNpcs} 个顾客");
    }

    private void LoadFromNpcDatabase()
    {
        Debug.Log($"[CustomerSpawnManager] 从 NpcDatabase 加载数据: {npcDatabase.name}");
        
        npcLookup.Clear();
        foreach (var npc in npcDatabase.allNpcs)
        {
            if (npc != null && !string.IsNullOrEmpty(npc.id))
            {
                npcLookup[npc.id] = npc;
            }
        }

        Debug.Log($"[CustomerSpawnManager] 从 NpcDatabase 加载完成，共 {npcLookup.Count} 个顾客");
    }

    private bool TryRestoreFromSave()
    {
        // 这里会通过消息系统接收保存的状态
        // 实际恢复逻辑在 OnCustomerStateLoaded 中处理
        return false; // 暂时返回false，让系统创建默认状态
    }

    private void CreateDefaultState()
    {
        Debug.Log("[CustomerSpawnManager] 创建默认顾客状态...");

        availablePool.Clear();
        cooldownPool.Clear();
        guaranteeIds.Clear();
        visitedIds.Clear();
        globalVisitorCount = 0;
        totalSpawnedCount = 0;
        spawnTimer = 0f;

        // 将所有顾客加入可用池
        foreach (var npc in npcLookup.Values)
        {
            if (npc != null)
            {
                availablePool.Add(npc);
            }
        }

        Debug.Log($"[CustomerSpawnManager] 默认状态创建完成，可用池包含 {availablePool.Count} 个顾客");
        UpdateDebugInfo();
    }

    #endregion

    #region 消息处理

    private void OnProbabilityReady(DailyProbabilityToNight data)
    {
        Debug.Log($"[CustomerSpawnManager] 接收到每日概率数据: {data}");
        probabilityData = data;
        hasReceivedProbability = true;

        // 如果系统已初始化，开始生成
        if (hasInitialized)
        {
            StartSpawning();
        }
    }

    private void OnCustomerStateLoaded(NightCustomerState state)
    {
        if (state == null)
        {
            Debug.Log("[CustomerSpawnManager] 接收到空的顾客状态，使用默认状态");
            return;
        }

        Debug.Log($"[CustomerSpawnManager] 恢复顾客状态: {state}");
        RestoreFromState(state);
    }

    private void OnCustomerVisited(NpcCharacterData npc)
    {
        if (npc == null) return;

        Debug.Log($"[CustomerSpawnManager] 顾客到访: {npc.id}");
        
        globalVisitorCount++;
        visitedIds.Add(npc.id);

        // 更新冷却池中所有顾客的计数
        var toUnfreeze = new List<string>();
        var cooldownKeys = cooldownPool.Keys.ToList();
        
        foreach (var id in cooldownKeys)
        {
            cooldownPool[id]--;
            if (cooldownPool[id] <= 0)
            {
                toUnfreeze.Add(id);
            }
        }

        // 解冻顾客，重新加入可用池
        foreach (var id in toUnfreeze)
        {
            if (npcLookup.TryGetValue(id, out var unfrozenNpc))
            {
                availablePool.Add(unfrozenNpc);
                Debug.Log($"[CustomerSpawnManager] 顾客解冻: {id}");
            }
            cooldownPool.Remove(id);
        }

        UpdateDebugInfo();
    }

    #endregion

    #region 生成逻辑

    private void StartSpawning()
    {
        if (isSpawning) return;

        Debug.Log("[CustomerSpawnManager] 开始生成顾客...");
        isSpawning = true;
        spawnTimer = 0f;

        // 立即生成前N个顾客
        Debug.Log($"[CustomerSpawnManager] 立即生成前 {initialFastSpawnCount} 个顾客...");
        for (int i = 0; i < initialFastSpawnCount; i++)
        {
            if (!TrySpawnCustomer())
            {
                Debug.LogWarning($"[CustomerSpawnManager] 第 {i + 1} 个顾客生成失败，停止快速生成");
                break;
            }
        }
        
        Debug.Log($"[CustomerSpawnManager] 快速生成完成，已生成 {totalSpawnedCount} 个顾客");
        Debug.Log($"[CustomerSpawnManager] 从第 {totalSpawnedCount + 1} 个顾客开始使用 {baseSpawnInterval} 秒间隔");
    }

    public void StopSpawning()
    {
        Debug.Log("[CustomerSpawnManager] 停止生成顾客");
        isSpawning = false;
    }

    private bool TrySpawnCustomer()
    {
        // 1. 检查队列容量
        if (customerQueue.Count >= queueCapacity)
        {
            Debug.Log("[CustomerSpawnManager] 队列已满，跳过生成");
            return false;
        }

        // 2. 检查可用池
        if (availablePool.Count == 0)
        {
            Debug.Log("[CustomerSpawnManager] 可用池为空，跳过生成");
            return false;
        }

        // 3. 加权随机抽取
        var selectedNpc = PickWeightedCustomer();
        if (selectedNpc == null)
        {
            Debug.LogWarning("[CustomerSpawnManager] 加权抽取失败");
            return false;
        }

        // 4. 入队
        customerQueue.Enqueue(selectedNpc);
        totalSpawnedCount++;
        Debug.Log($"[CustomerSpawnManager] 顾客入队: {selectedNpc.id} (队列:{customerQueue.Count}/{queueCapacity}, 总生成:{totalSpawnedCount})");

        // 5. 从可用池移除，加入冷却池
        availablePool.Remove(selectedNpc);
        cooldownPool[selectedNpc.id] = cooldownRequirement;

        // 6. 检查保底触发
        if (customerQueue.Count == guaranteeThreshold)
        {
            TriggerGuarantee();
        }

        // 7. 广播生成事件
        MessageManager.Send(MessageDefine.CUSTOMER_SPAWNED, selectedNpc);

        UpdateDebugInfo();
        return true;
    }

    private NpcCharacterData PickWeightedCustomer()
    {
        if (!probabilityData.HasValue || availablePool.Count == 0)
        {
            return null;
        }

        // 构建权重列表
        var weights = new List<float>();
        foreach (var npc in availablePool)
        {
            float probability = VisitProbabilityCalculator.GetFinalProbability(
                probabilityData.Value.probabilityResult,
                npc.identityId,
                npc.state,
                npc.gender
            );
            weights.Add(probability);
        }

        // 加权随机选择
        int selectedIndex = WeightedRandom(weights);
        return availablePool[selectedIndex];
    }

    private int WeightedRandom(List<float> weights)
    {
        if (weights.Count == 0) return -1;

        float total = 0f;
        foreach (var weight in weights)
        {
            total += weight;
        }

        if (total <= 0f)
        {
            // 如果所有权重都是0，随机选择
            return UnityEngine.Random.Range(0, weights.Count);
        }

        float randomValue = UnityEngine.Random.value * total;
        float cumulative = 0f;

        for (int i = 0; i < weights.Count; i++)
        {
            cumulative += weights[i];
            if (randomValue <= cumulative)
            {
                return i;
            }
        }

        return weights.Count - 1;
    }

    private void TriggerGuarantee()
    {
        Debug.Log("[CustomerSpawnManager] 触发保底机制");
        
        guaranteeIds.Clear();
        var queueArray = customerQueue.ToArray();
        int startIndex = Math.Max(0, queueArray.Length - 3);
        
        for (int i = startIndex; i < queueArray.Length; i++)
        {
            guaranteeIds.Add(queueArray[i].id);
        }

        Debug.Log($"[CustomerSpawnManager] 保底池: [{string.Join(", ", guaranteeIds)}]");
    }

    #endregion

    #region 队列操作

    /// <summary>
    /// 出队顾客（由顾客交互系统调用）
    /// </summary>
    public NpcCharacterData DequeueCustomer()
    {
        if (customerQueue.TryDequeue(out var npc))
        {
            Debug.Log($"[CustomerSpawnManager] 顾客出队: {npc.id}");
            MessageManager.Send(MessageDefine.CUSTOMER_DEQUEUED, npc);
            UpdateDebugInfo();
            return npc;
        }
        return null;
    }

    /// <summary>
    /// 查看队首顾客
    /// </summary>
    public NpcCharacterData PeekNextCustomer()
    {
        customerQueue.TryPeek(out var npc);
        return npc;
    }

    /// <summary>
    /// 获取当前队列数量
    /// </summary>
    public int GetQueueCount()
    {
        return customerQueue?.Count ?? 0;
    }

    /// <summary>
    /// 获取队列中的顾客列表（用于UI显示）
    /// </summary>
    public List<NpcCharacterData> GetQueuedCustomers()
    {
        if (customerQueue == null) return new List<NpcCharacterData>();
        return new List<NpcCharacterData>(customerQueue.ToArray());
    }

    /// <summary>
    /// 获取系统统计信息
    /// </summary>
    public (int queueCount, int availableCount, int cooldownCount, int visitedCount) GetStats()
    {
        return (
            customerQueue?.Count ?? 0,
            availablePool.Count,
            cooldownPool.Count,
            globalVisitorCount
        );
    }

    #endregion

    #region 营业结束处理

    /// <summary>
    /// 营业结束时调用：处理保底机制
    /// </summary>
    public void OnNightEnd()
    {
        Debug.Log("[CustomerSpawnManager] 营业结束，处理保底机制...");

        // 检查保底池中未到访的顾客
        foreach (var guaranteeId in guaranteeIds)
        {
            if (!visitedIds.Contains(guaranteeId))
            {
                // 直接归还可用池，不进冷却
                if (cooldownPool.ContainsKey(guaranteeId))
                {
                    cooldownPool.Remove(guaranteeId);
                }

                if (npcLookup.TryGetValue(guaranteeId, out var npc))
                {
                    availablePool.Add(npc);
                    Debug.Log($"[CustomerSpawnManager] 保底顾客归还可用池: {guaranteeId}");
                }
            }
        }

        guaranteeIds.Clear();
        StopSpawning();
        UpdateDebugInfo();
    }

    #endregion

    #region 状态导出/恢复

    /// <summary>
    /// 导出当前状态（用于保存系统）
    /// </summary>
    public NightCustomerState ExportState()
    {
        var state = new NightCustomerState
        {
            globalVisitorCount = this.globalVisitorCount,
            totalSpawnedCount = this.totalSpawnedCount,
            spawnTimer = this.spawnTimer
        };

        // 导出队列
        if (customerQueue != null)
        {
            var queueArray = customerQueue.ToArray();
            foreach (var npc in queueArray)
            {
                state.queuedNpcIds.Add(npc.id);
            }
        }

        // 导出可用池
        foreach (var npc in availablePool)
        {
            state.availablePool.Add(npc.id);
        }

        // 导出冷却池
        foreach (var kvp in cooldownPool)
        {
            state.cooldownPool.Add(new CustomerCooldownData(kvp.Key, kvp.Value));
        }

        // 导出保底和已访问
        state.guaranteeIds.AddRange(guaranteeIds);
        state.visitedIds.AddRange(visitedIds);

        Debug.Log($"[CustomerSpawnManager] 导出状态: {state}");
        return state;
    }

    /// <summary>
    /// 从状态恢复（用于保存系统）
    /// </summary>
    private void RestoreFromState(NightCustomerState state)
    {
        // 恢复基础数据
        globalVisitorCount = state.globalVisitorCount;
        totalSpawnedCount = state.totalSpawnedCount; // 恢复总生成计数
        spawnTimer = state.spawnTimer;

        // 恢复队列
        customerQueue?.Clear();
        foreach (var npcId in state.queuedNpcIds)
        {
            if (npcLookup.TryGetValue(npcId, out var npc))
            {
                customerQueue.Enqueue(npc);
            }
        }

        // 恢复可用池
        availablePool.Clear();
        foreach (var npcId in state.availablePool)
        {
            if (npcLookup.TryGetValue(npcId, out var npc))
            {
                availablePool.Add(npc);
            }
        }

        // 恢复冷却池
        cooldownPool.Clear();
        foreach (var cooldownData in state.cooldownPool)
        {
            cooldownPool[cooldownData.npcId] = cooldownData.remainingCooldown;
        }

        // 恢复保底和已访问
        guaranteeIds.Clear();
        guaranteeIds.AddRange(state.guaranteeIds);
        
        visitedIds.Clear();
        foreach (var id in state.visitedIds)
        {
            visitedIds.Add(id);
        }

        UpdateDebugInfo();
        Debug.Log($"[CustomerSpawnManager] 状态恢复完成");
    }

    #endregion

    #region 调试和UI

    private void UpdateDebugInfo()
    {
        queueCount = customerQueue?.Count ?? 0;
        availableCount = availablePool.Count;
        cooldownCount = cooldownPool.Count;
    }

    [Title("调试操作")]
    [Button("手动生成顾客")]
    private void DebugSpawnCustomer()
    {
        if (Application.isPlaying)
        {
            TrySpawnCustomer();
        }
    }

    [Button("清空队列")]
    private void DebugClearQueue()
    {
        if (Application.isPlaying && customerQueue != null)
        {
            customerQueue.Clear();
            UpdateDebugInfo();
        }
    }

    [Button("输出状态详情")]
    private void DebugLogState()
    {
        if (!Application.isPlaying) return;

        Debug.Log($"=== CustomerSpawnManager 状态 ===");
        Debug.Log($"队列: {queueCount}/{queueCapacity}");
        Debug.Log($"可用池: {availableCount}");
        Debug.Log($"冷却池: {cooldownCount}");
        Debug.Log($"全局到访: {globalVisitorCount}");
        Debug.Log($"保底池: [{string.Join(", ", guaranteeIds)}]");
        Debug.Log($"已到访: {visitedIds.Count}");
        
        if (cooldownPool.Count > 0)
        {
            Debug.Log("冷却详情:");
            foreach (var kvp in cooldownPool)
            {
                Debug.Log($"  {kvp.Key}: {kvp.Value}");
            }
        }
    }

    #endregion
}
