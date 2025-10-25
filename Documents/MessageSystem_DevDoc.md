## 消息系统（Message System）开发文档

### 功能综述

消息系统提供基于字符串通道与类型通道的轻量级发布/订阅能力，采用 `MessageManager` 静态类集中管理注册、移除与广播，内部以线程安全的字典维护订阅关系，并内置环形日志记录每次消息发送（时间、帧号、通道、载荷类型与 ToString 摘要），为跨系统解耦（时间、存档、合成、菜单、配方书、UI 等）提供统一、可观测的事件总线。

### 用例图

```mermaid
flowchart LR
    Dev([开发者])
    subgraph MessageBus[Message System]
      UC1[注册订阅 Register]
      UC2[移除订阅 Remove]
      UC3[广播消息 Send]
      UC4[日志与快照]
    end
    Dev --> UC1
    Dev --> UC2
    Dev --> UC3
    Dev --> UC4
```

### 时序图（字符串通道广播）

```mermaid
sequenceDiagram
    participant P as Publisher(发送方)
    participant MM as MessageManager
    participant S1 as Subscriber A
    participant S2 as Subscriber B
    Note over MM: s_Dict : Dictionary<string, IMessageData>
    P->>MM: Register<T>("KEY", actionA)
    P->>MM: Register<T>("KEY", actionB)
    P->>MM: Send<T>("KEY", payload)
    MM-->>S1: actionA.Invoke(payload)
    MM-->>S2: actionB.Invoke(payload)
    MM->>MM: Log(channel="key:KEY", type=T)
```

### 时序图（类型通道广播）

```mermaid
sequenceDiagram
    participant P as Publisher(发送方)
    participant MM as MessageManager
    participant S as Subscriber
    Note over MM: s_TypeDict : Dictionary<Type, IMessageData>
    P->>MM: Register<T>(action)
    P->>MM: Send<T>(payload)
    MM-->>S: action.Invoke(payload)
    MM->>MM: Log(channel="type:T", type=T)
```

### ER 图（数据与通道关系）

```mermaid
erDiagram
    MESSAGE_MANAGER ||--o{ STRING_BUS : manages
    MESSAGE_MANAGER ||--o{ TYPE_BUS : manages
    STRING_BUS ||--o{ SUBSCRIBER : holds
    TYPE_BUS ||--o{ SUBSCRIBER : holds
    MESSAGE_MANAGER ||--o{ MESSAGE_LOG : records

    STRING_BUS {
        string key
        Type payloadType
        int subscribers
    }
    TYPE_BUS {
        Type typeKey
        int subscribers
    }
    MESSAGE_LOG {
        double time
        int frame
        string channel
        string typeName
        string payloadSummary
    }
```

### 类图（核心结构）

```mermaid
classDiagram
    class MessageManager {
        <<static>>
        -Dictionary~string, IMessageData~ s_Dict
        -Dictionary~Type, IMessageData~ s_TypeDict
        +Register~T~(string key, UnityAction~T~)
        +Remove~T~(string key, UnityAction~T~)
        +Send~T~(string key, T data)
        +Register~T~(UnityAction~T~)
        +Remove~T~(UnityAction~T~)
        +Send~T~(T data)
        +GetStringBusSnapshot() List
        +GetTypeBusSnapshot() List
        +SetLogEnabled(bool)
        +GetLogEnabled() bool
        +SetMaxLog(int)
        +GetMaxLog() int
        +GetLogSnapshot() List
        +ClearLog()
    }
    interface IMessageData
    class MessageData~T~ {
        +UnityAction~T~ MessageEvents
    }
```

### 代码功能分析

- 注册/移除（字符串通道）：

```24:48:Scripts/0_General/0_0_MessageSystem/MessageManager.cs
    public static void Register<T>(string key, UnityAction<T> action)
    {
        if (string.IsNullOrEmpty(key) || action == null) return;
        lock (s_Lock)
        {
            if (s_Dict.TryGetValue(key, out var prev))
            {
                if (prev is MessageData<T> md)
                {
                    md.MessageEvents += action;
                }
                else
                {
                    // 类型不匹配则覆盖为新的泛型通道
                    s_Dict[key] = new MessageData<T>(action);
                }
            }
            else
            {
                s_Dict.Add(key, new MessageData<T>(action));
            }
        }
    }
```

- 广播（字符串通道）与日志：

```67:77:Scripts/0_General/0_0_MessageSystem/MessageManager.cs
    public static void Send<T>(string key, T data)
    {
        if (string.IsNullOrEmpty(key)) return;
        IMessageData prev;
        lock (s_Lock)
        {
            s_Dict.TryGetValue(key, out prev);
        }
        (prev as MessageData<T>)?.MessageEvents?.Invoke(data);
        Log("key:" + key, typeof(T), data);
    }
```

- 注册/移除/广播（类型通道）：

```144:196:Scripts/0_General/0_0_MessageSystem/MessageManager.cs
    public static void Register<T>(UnityAction<T> action) { /* 以 typeof(T) 为键加入 s_TypeDict */ }
    public static void Remove<T>(UnityAction<T> action) { /* 从 s_TypeDict 解除绑定 */ }
    public static void Send<T>(T data) { /* 根据 typeof(T) 找到委托并 Invoke；并写入日志 */ }
```

- 日志与快照（运维/调试）：

```198:246:Scripts/0_General/0_0_MessageSystem/MessageManager.cs
    public static void SetLogEnabled(bool on)
    public static bool GetLogEnabled()
    public static void SetMaxLog(int max)
    public static int GetMaxLog()
    public static List<MessageLogEntry> GetLogSnapshot()
    public static void ClearLog()
    // 内部: TrimLogIfNeeded 保持上限；Log 记录 frame/time/channel/type/payload
```

### 关键消息清单（示例）

- 合成相关：`COCKTAIL_CRAFTED`，`COCKTAIL_CRAFTED_DETAIL`，`CRAFTING_SLOT_CONTENT_UPDATED`，`CRAFTING_SLOT_CLEARED`
- 存档相关：`SAVE_REQUESTED`，`SAVE_COMPLETED`，`SAVE_LOADED`
- 时间相关：`PHASE_CHANGED`，`DAY_COMPLETED`
- UI/交互：`CARD_DRAG_STARTED`，`CARD_DRAG_ENDED`，`CARD_CLICKED`

### 设计要点与边界

- 线程安全：通过 `s_Lock` 与 `s_LogLock` 保护注册表与日志；仍建议主线程使用。
- 类型安全：通道以 `T` 泛型绑定载荷类型，避免装箱与转换错误。
- 容错策略：字符串通道类型不匹配时覆盖为新 `MessageData<T>`，保证后续一致性。
- 可观测性：提供日志开关、上限与快照导出，便于编辑器窗口/调试工具使用。
- 清理策略：支持 `Clear()` 与 `ClearKey()`，防止场景切换后残留订阅。

### 集成建议

- 尽量优先使用“类型通道”以获得编译期类型安全；需跨语言/弱类型场景再使用“字符串通道”。
- 对频繁广播的消息适当关闭日志或调低上限，避免 Editor 频繁分配。
- 在 OnEnable/OnDisable 成对注册/移除，避免悬空委托与重复订阅。
