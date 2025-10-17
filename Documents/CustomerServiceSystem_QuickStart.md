# 顾客服务系统 - 快速开始指南

## 系统概述

顾客服务系统实现了完整的顾客到访流程，无需对象池，使用场景固定组件复用显示所有顾客。

## 核心特性

- ?7?3 **简化架构**：单一 CustomerNpcBehavior 组件复用
- ?7?3 **完整动画**：侧身走路 → 淡出 → 立绘淡入
- ?7?3 **智能切换**：自动处理上一位顾客淡出
- ?7?3 **分级服务**：前 5 位快速切换，第 6 位起等待 20 秒
- ?7?3 **品尝流程**：卡牌拖拽 → 随机 3-5 秒 → 自动结算

## 场景配置（5 分钟快速设置）

### 步骤 1：创建 GameObject 层级

在 NightScreen 场景中创建：

```
CustomerServiceSystem
├── CustomerServiceManager (组件)
└── CustomerDisplay
    ├── CustomerNpcBehavior (组件)
    ├── WalkingContainer
    │   ├── WalkingSilhouette (Image)
    │   ├── WalkingStartPoint (Transform) - 起始位置
    │   └── WalkingEndPoint (Transform) - 柜台位置
    └── CustomerContainer
        ├── CanvasGroup (组件)
        ├── Portrait (Image)
        ├── NameText (TextMeshProUGUI)
        └── StateText (TextMeshProUGUI)
```

### 步骤 2：配置 CustomerServiceManager

| 字段                | 值     | 说明                                        |
| ------------------- | ------ | ------------------------------------------- |
| customerBehavior    | [拖入] | CustomerDisplay 的 CustomerNpcBehavior 组件 |
| initialDelay        | 3      | Night 开始后延迟秒数                        |
| normalSpawnInterval | 20     | 第 6 位起的服务间隔                         |
| fastServeCount      | 5      | 前 N 位快速服务                             |
| minDrinkingTime     | 3      | 最短品尝时间                                |
| maxDrinkingTime     | 5      | 最长品尝时间                                |

### 步骤 3：配置 CustomerNpcBehavior

| 字段                | 说明                             |
| ------------------- | -------------------------------- |
| walkingContainer    | WalkingContainer GameObject      |
| walkingSilhouette   | WalkingSilhouette Image          |
| walkingStartPoint   | 起始位置 Transform（屏幕左侧）   |
| walkingEndPoint     | 结束位置 Transform（柜台前）     |
| customerContainer   | CustomerContainer GameObject     |
| customerCanvasGroup | CustomerContainer 的 CanvasGroup |
| portraitImage       | Portrait Image                   |
| nameText            | NameText TMP                     |
| stateText           | StateText TMP                    |
| walkDuration        | 3 秒                             |
| walkBobHeight       | 10px                             |
| fadeOutStartTime    | 0.8 秒                           |

## 游戏流程

### 完整服务流程

1. **Night 开始** → 延迟 3 秒
2. **第 1 位顾客出队** → 侧身走路动画（3 秒）→ 淡出淡入 → 等待玩家
3. **玩家拖拽鸡尾酒卡牌** → 触发品尝流程
4. **顾客品尝** 10-15 秒（随机）
5. **自动结算** → 给钱+小费+评价
6. **顾客淡出离场**
7. **第 2-5 位** → 立即出队（重复步骤 2-6，无 20 秒等待）
8. **第 5 位完成** → 开始 20 秒倒计时
9. **第 6 位及之后** → 每次服务完成后等待 20 秒再出队

### 关键时间节点

| 事件            | 时间     | 说明                     |
| --------------- | -------- | ------------------------ |
| Night 开始      | 0 秒     | 触发 CustomerServiceLoop |
| 第 1 位顾客显示 | 3 秒     | initialDelay 后出队      |
| 第 2-5 位切换   | 即时     | 无等待，立即切换         |
| 第 6 位出队     | +20 秒   | 从第 5 位完成后计时      |
| 后续顾客        | 每+20 秒 | 服务完成后计时           |

## 测试方法

### 使用 CustomerNpcBehaviorTester

1. **单个顾客入场测试**：

   - `测试：完整入场动画（手动NPC）`
   - `测试：完整入场动画（随机NPC）`

2. **顾客切换测试**：

   - `测试：2个顾客完整进场流程` - 验证顾客淡出切换

3. **状态测试**：
   - 点击彩色按钮测试不同状态的 NPC

### 使用 CustomerServiceManager 调试

1. **模拟卡牌拖拽**：

   - 运行场景，等待顾客显示
   - 点击`模拟交付鸡尾酒`按钮
   - 观察品尝 → 结算 → 离场流程

2. **强制出队**：

   - `强制出队下一位顾客` - 跳过等待时间

3. **查看状态**：
   - `显示服务状态` - 查看详细运行信息

## 消息系统集成

### 需要发送的消息（卡牌系统）

```csharp
// 当玩家拖拽鸡尾酒卡牌给顾客时
MessageManager.Send(MessageDefine.COCKTAIL_DELIVERED, cocktailData);
```

### 监听的消息

- `PHASE_CHANGED` - Night 阶段切换
- `COCKTAIL_DELIVERED` - 鸡尾酒交付
- `CUSTOMER_VISITED` - 顾客服务完成（冷却机制监听）

### 发送的消息

- `CUSTOMER_DEQUEUED` - 顾客出队开始服务
- `CUSTOMER_VISITED` - 顾客服务完成离场
- `SERVICE_PAYMENT_COMPLETE` - 结算完成（未来）

## 常见问题

### Q: 顾客不出队？

**检查清单**：

1. ?7?7 Night 阶段是否已开始？
2. ?7?7 CustomerSpawnManager 是否已初始化并生成队列？
3. ?7?7 CustomerServiceManager 的 initialDelay 是否正确？
4. ?7?7 查看 Console 日志确认

### Q: 第 6 位顾客没有等待 20 秒？

**检查**：

- currentServedCount 是否正确计数
- fastServeCount 是否设为 5
- 查看 nextServeCountdown 倒计时

### Q: 顾客切换时直接消失？

**原因**：上一位顾客的离场动画可能未播放
**解决**：CustomerNpcBehavior.Initialize()已自动处理，确保该组件正常工作

### Q: 如何与卡牌系统集成？

**步骤**：

1. 在卡牌拖拽完成后发送`COCKTAIL_DELIVERED`消息
2. 传递鸡尾酒数据对象
3. CustomerServiceManager 会自动处理后续流程

## 性能优化

- ?7?3 单实例复用，无频繁 Instantiate/Destroy
- ?7?3 使用高性能 PooledQueue 管理队列数据
- ?7?3 DOTween 动画优化
- ?7?3 时间倍率支持（加速测试）

## 下一步扩展

1. **实现 CalculatePaymentAndRating()**方法
2. **集成实际的鸡尾酒卡牌系统**
3. **添加顾客满意度计算**
4. **显示金币和小费增加动画**
5. **优化品尝时间和动画效果**

---

**系统现已就绪，可以开始在 Unity 中配置和测试！** ?0?4
