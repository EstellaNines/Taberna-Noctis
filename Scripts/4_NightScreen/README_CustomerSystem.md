# NightScreen 顾客到访系统 - 集成指南

## 概述

NightScreen 顾客到访系统是一个完整的顾客生成、排队、冷却和持久化解决方案。该系统使用高性能队列、随机系统、时间系统和保存系统，实现了复杂的顾客到访逻辑。

## 核心组件

### 1. CustomerSpawnManager（核心管理器）

- **位置**: `Scripts/4_NightScreen/CustomerSpawnManager.cs`
- **职责**: 顾客生成、队列管理、冷却机制、保底机制
- **单例模式**: `CustomerSpawnManager.Instance`

### 2. CustomerCooldownData（数据结构）

- **位置**: `Scripts/0_General/0_9_CharacterDesign/CustomerCooldownData.cs`
- **职责**: 冷却数据和完整状态的序列化

### 3. CustomerQueueUI（可选 UI）

- **位置**: `Scripts/4_NightScreen/CustomerQueueUI.cs`
- **职责**: 队列状态的可视化显示

### 4. CustomerSystemTester（测试工具）

- **位置**: `Scripts/4_NightScreen/CustomerSystemTester.cs`
- **职责**: 系统功能验证和调试

## 快速集成步骤

### 步骤 1: 场景设置

1. 在 **NightScreen** 场景中创建空 GameObject，命名为 `CustomerSpawnManager`
2. 添加 `CustomerSpawnManager` 组件
3. 配置参数：
   - 基础生成间隔: 20 秒
   - 队列容量: 18
   - 保底阈值: 15
   - 冷却要求: 10

### 步骤 2: UI 设置（可选）

1. 创建 UI Canvas 和相关 UI 元素
2. 添加 `CustomerQueueUI` 组件
3. 配置 UI 引用：
   - queueInfoText: 显示队列信息的 TextMeshProUGUI
   - customerListParent: 顾客列表的父 Transform
   - customerItemPrefab: 顾客项的预制体

### 步骤 3: 测试验证

1. 添加 `CustomerSystemTester` 组件（开发阶段）
2. 使用 Inspector 中的按钮测试各项功能
3. 检查 Console 输出确认系统正常工作

## 系统流程

### 初始化流程

```
1. CustomerSpawnManager.Start()
2. 加载 NpcDatabase（50个顾客）
3. 订阅消息（概率数据、状态加载、顾客到访）
4. 创建默认状态或从保存恢复
5. 等待 NIGHT_PROBABILITY_READY 消息
6. 开始生成顾客
```

### 生成流程

```
1. 每20秒触发一次（受时间倍率影响）
2. 检查队列容量（< 18）
3. 从可用池加权随机抽取顾客
4. 顾客入队，从可用池移除，加入冷却池
5. 检查保底触发（队列达到15位）
6. 广播 CUSTOMER_SPAWNED 消息
```

### 冷却流程

```
1. 顾客到访时触发 CUSTOMER_VISITED 消息
2. 全局到访计数 +1
3. 所有冷却池顾客的剩余计数 -1
4. 计数归零的顾客解冻，重新加入可用池
```

### 保底流程

```
1. 队列达到15位时触发
2. 记录最后3位顾客ID到保底池
3. 营业结束时检查保底池
4. 未到访的保底顾客直接归还可用池（不进冷却）
```

## 消息系统

### 发送的消息

- `CUSTOMER_SPAWNED`: 顾客生成时
- `CUSTOMER_DEQUEUED`: 顾客出队时

### 接收的消息

- `NIGHT_PROBABILITY_READY`: 每日概率数据就绪
- `CUSTOMER_STATE_LOADED`: 从保存系统加载状态
- `CUSTOMER_VISITED`: 顾客到访（触发冷却）

## API 接口

### 主要方法

```csharp
// 获取队列状态
int GetQueueCount()
List<NpcCharacterData> GetQueuedCustomers()
(int queueCount, int availableCount, int cooldownCount, int visitedCount) GetStats()

// 队列操作
NpcCharacterData DequeueCustomer()
NpcCharacterData PeekNextCustomer()

// 生命周期
void StopSpawning()
void OnNightEnd()

// 状态管理
NightCustomerState ExportState()
```

### 使用示例

```csharp
// 获取下一位顾客
var nextCustomer = CustomerSpawnManager.Instance.PeekNextCustomer();
if (nextCustomer != null)
{
    Debug.Log($"下一位顾客: {nextCustomer.displayName}");
}

// 顾客服务完成后
var servedCustomer = CustomerSpawnManager.Instance.DequeueCustomer();
if (servedCustomer != null)
{
    // 触发到访事件（冷却机制）
    MessageManager.Send(MessageDefine.CUSTOMER_VISITED, servedCustomer);
}

// 获取系统状态
var stats = CustomerSpawnManager.Instance.GetStats();
Debug.Log($"队列: {stats.queueCount}, 可用: {stats.availableCount}");
```

## 配置参数

### CustomerSpawnManager 参数

- **baseSpawnInterval**: 基础生成间隔（秒），默认 20 秒
- **queueCapacity**: 队列最大容量，默认 18
- **guaranteeThreshold**: 保底触发阈值，默认 15
- **cooldownRequirement**: 冷却要求（到访数），默认 10

### CustomerQueueUI 参数

- **maxDisplayCount**: 最大显示顾客数，默认 10
- **refreshInterval**: UI 刷新间隔，默认 1 秒

## 依赖系统

### 必需依赖

- **PooledQueue**: 高性能队列系统
- **MessageManager**: 消息传递系统
- **NpcDatabase**: 顾客数据库（50 个顾客）
- **VisitProbabilityCalculator**: 概率计算器
- **SaveManager**: 保存系统

### 可选依赖

- **TimeSystemManager**: 时间倍率支持
- **Odin Inspector**: 增强的 Inspector 显示

## 调试和监控

### 调试工具

1. **CustomerSystemTester**: 完整的测试套件
2. **Odin Inspector**: 实时状态监控
3. **Console 日志**: 详细的操作记录

### 常用调试命令

```csharp
// 手动生成顾客
CustomerSpawnManager.Instance.DebugSpawnCustomer();

// 输出详细状态
CustomerSpawnManager.Instance.DebugLogState();

// 清空队列
CustomerSpawnManager.Instance.DebugClearQueue();
```

### 监控指标

- 队列长度变化
- 可用池数量
- 冷却池状态
- 全局到访计数
- 保底触发频率

## 性能优化

### 已实现的优化

- **零 GC 队列**: 使用对象池避免内存分配
- **批量操作**: 冷却更新一次处理所有顾客
- **延迟加载**: 按需创建 UI 元素
- **消息缓存**: 避免重复订阅

### 性能建议

1. 避免频繁调用 `GetQueuedCustomers()`（会创建新列表）
2. 使用 `GetStats()` 获取统计信息而非直接访问队列
3. UI 刷新间隔不要设置过小（推荐 1 秒以上）
4. 测试时注意时间倍率对性能的影响

## 故障排除

### 常见问题

**Q: 顾客不生成**
A: 检查是否收到 `NIGHT_PROBABILITY_READY` 消息，确认 NpcDatabase 已加载

**Q: 冷却机制不工作**
A: 确认发送了 `CUSTOMER_VISITED` 消息，检查冷却池状态

**Q: 保存/加载失败**
A: 检查 SaveData 是否包含 nightCustomerState 字段，确认序列化正常

**Q: UI 不更新**
A: 检查 CustomerQueueUI 的消息订阅，确认 refreshInterval 设置合理

### 日志关键字

- `[CustomerSpawnManager]`: 核心管理器日志
- `[CustomerSystemTester]`: 测试工具日志
- `[CustomerQueueUI]`: UI 组件日志

## 扩展指南

### 添加新功能

1. 继承或扩展 `CustomerSpawnManager`
2. 添加新的消息类型到 `MessageDefine`
3. 扩展 `NightCustomerState` 数据结构
4. 更新保存/加载逻辑

### 自定义概率算法

1. 修改 `PickWeightedCustomer()` 方法
2. 实现自定义的 `WeightedRandom()` 算法
3. 添加新的概率参数到配置

### 集成其他系统

1. 通过消息系统与其他组件通信
2. 扩展 `ExportState()` 和 `RestoreFromState()` 方法
3. 添加新的依赖注入点

---

**版本**: 1.0  
**最后更新**: 2025-10-16  
**兼容性**: Unity 2021.3+, C# 8.0+
