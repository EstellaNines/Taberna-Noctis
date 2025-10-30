## 随机系统开发文档（Daily Stable + ES3 持久化）

### 功能综述

随机系统提供“每日稳定”与“真随机”两种模式，统一以 IRandomSource 封装 Range/Shuffle/Deal/Pick/Weighted 等接口，并通过 ES3 持久化 UnityEngine.Random.State 实现状态延续；调用前后自动还原全局随机状态避免污染，辅以监控器与直方图等工具，兼顾生产不可预测性与 QA 可复现性。

### 目标与特性

- **每日稳定**: 按玩家 ID+日期+流 Key 生成稳定结果；跨天变化。
- **多流隔离**: 不同业务使用独立随机流，互不影响。
- **可持久化**: 使用 ES3 保存/恢复 `UnityEngine.Random.State`。
- **无全局污染**: 每次调用前后还原全局 `UnityEngine.Random.state`。
- **常用 API**: 区间取值、洗牌、发牌（无放回）、权重抽取。

### 架构图（逻辑）

```mermaid
flowchart TD
    Client[业务模块]
    RS[RandomService]
    HU[HashUtil]
    Store[ES3 Store]
    URS[UnityRandomStream]
    Exec[Execute: 切换与回写]

    Client -->|GetDaily/Persistent| RS
    RS -->|SeedFrom| HU
    RS -->|Load/Save| Store
    RS --> URS
    URS --> Exec
    Exec --> URS
```

### Mermaid 图例

#### 时序图：获取每日流并进行一次采样（含已存在/未存在分支）

```mermaid
sequenceDiagram
    participant Client as Game/系统模块
    participant RS as RandomService
    participant ES3 as ES3RandomStateStore
    participant HU as HashUtil
    participant UR as UnityEngine.Random
    participant URS as UnityRandomStream

    Client->>RS: GetDailyStream(streamKey, playerId, date, autoSave)
    RS->>ES3: Exists(key)
    alt 已存在
        RS->>ES3: Load(key)
        ES3-->>RS: state
        RS->>URS: new(state, saver)
    else 不存在
        RS->>HU: SeedFrom(playerId, date, streamKey)
        HU-->>RS: seed
        RS->>UR: InitState(seed)
        RS->>UR: 读取 state
        RS->>ES3: Save(key, state)
        RS->>URS: new(state, saver)
    end
    Client->>URS: Value01()/Range()/Shuffle()
    URS->>UR: 备份 original
    URS->>UR: 切换为流 state
    URS->>UR: 执行并取新state
    opt autoSave
        URS->>ES3: Save(key, state)
    end
    URS->>UR: 还原 original
    URS-->>Client: 结果
```

#### 用例图：典型交互

```mermaid
flowchart TD
    actorDev([关卡/系统开发])
    actorDesign([策划/运营])
    actorQA([QA/测试])
    actorTool([编辑器工具])

    ucDaily([生成每日消息])
    ucCard([卡组洗牌/发牌])
    ucMonitor([随机监控器预览/直方图])
    ucClear([清除ES3状态])

    actorDev --> ucDaily
    actorDev --> ucCard
    actorDesign --> ucDaily
    actorQA --> ucMonitor
    actorTool --> ucMonitor
    actorTool --> ucClear

    subgraph 后端支撑
    RSvc[RandomService] --> URS[UnityRandomStream]
    RSvc --> ES3[ES3 Store]
    RSvc --> Hash[HashUtil]
    end

    ucDaily --> RSvc
    ucCard --> RSvc
    ucMonitor --> RSvc
    ucClear --> ES3
```

#### ER 图：核心实体关系

```mermaid
erDiagram
    PLAYER {
        string playerId PK
    }
    STREAM {
        string streamKey PK
        string type  "Daily|Persistent"
        string es3Key
    }
    SEED {
        int seedInt PK
        string ymd  "yyyyMMdd"
    }
    ES3ENTRY {
        string key PK
        string state  "UnityEngine.Random.State(序列化)"
    }

    PLAYER ||--o{ STREAM : owns
    STREAM ||--|| SEED : derives
    STREAM }o--|| ES3ENTRY : persists_to
```

### 关键数据结构

- **种子 Seed**: `SeedFrom(playerId, date, streamKey)` 生成 `int`。
- **State 存储 Key**:
  - 每日流: `rng/daily/{playerId}/{yyyyMMdd}/{streamKey}`
  - 持久流: `rng/persistent/{playerId}/{streamKey}`
- **状态类型**: `UnityEngine.Random.State`（Unity 可序列化结构）。

### 接口与实现（代码引用）

- IRandomSource（统一 API）

```1:18:Scripts/0_General/0_4_RandomSystem/IRandomSource.cs
namespace TabernaNoctis.RandomSystem
{
    public interface IRandomSource
    {
        int Range(int minInclusive, int maxExclusive);
        float Range(float minInclusive, float maxInclusive);
        float Value01();
        void Shuffle<T>(IList<T> list);
        List<T> Deal<T>(IList<T> deck, int count, bool removeFromSource = false);
        T PickOne<T>(IList<T> list);
        T PickWeighted<T>(IList<T> items, IList<float> weights);
    }
}
```

- RandomService（获取每日/持久流并处理 ES3）

```17:33:Scripts/0_General/0_4_RandomSystem/RandomService.cs
public IRandomSource GetDailyStream(string streamKey, string playerId, DateTime? date = null, bool autoSave = true)
{
    DateTime d = date ?? DateTime.Now;
    string es3Key = "rng/daily/" + (playerId ?? string.Empty) + "/" + d.ToString("yyyyMMdd") + "/" + (streamKey ?? string.Empty);

    if (_store.Exists(es3Key))
    {
        var st = _store.Load(es3Key);
        return new UnityRandomStream(st, autoSave ? (s => _store.Save(es3Key, s)) : null);
    }
    else
    {
        var st = CreateStateFromSeed(HashUtil.SeedFrom(playerId, d, streamKey));
        _store.Save(es3Key, st);
        return new UnityRandomStream(st, autoSave ? (s => _store.Save(es3Key, s)) : null);
    }
}
```

- HashUtil（FNV-1a 32-bit 简化）

```7:21:Scripts/0_General/0_4_RandomSystem/HashUtil.cs
public static int SeedFrom(string playerId, DateTime date, string streamKey)
{
    string s = (playerId ?? string.Empty) + "|" + date.ToString("yyyyMMdd") + "|" + (streamKey ?? string.Empty);
    unchecked
    {
        const uint offset = 2166136261u;
        const uint prime = 16777619u;
        uint hash = offset;
        for (int i = 0; i < s.Length; i++)
        {
            hash ^= s[i];
            hash *= prime;
        }
        return (int)hash;
    }
}
```

- UnityRandomStream（无全局污染执行）

```151:165:Scripts/0_General/0_4_RandomSystem/UnityRandomStream.cs
private void Execute(Action action)
{
    var original = UnityEngine.Random.state;
    try
    {
        UnityEngine.Random.state = _state;
        action();
        _state = UnityEngine.Random.state;
        _autoSaver?.Invoke(_state);
    }
    finally
    {
        UnityEngine.Random.state = original;
    }
}
```

- ES3 存取 `Random.State`

```7:20:Scripts/0_General/0_4_RandomSystem/ES3RandomStateStore.cs
public bool Exists(string key) { return ES3.KeyExists(key); }
public void Save(string key, UnityEngine.Random.State state) { ES3.Save(key, state); }
public UnityEngine.Random.State Load(string key) { return ES3.Load<UnityEngine.Random.State>(key); }
```

### 典型用法

- 每日消息（每日稳定）

```csharp
var rng = RandomService.Instance.GetDailyStream("DailyMessage", playerId);
var msg = rng.PickOne(messagePool);
```

- 卡组（持久流，跨会话连续）

```csharp
var rng = RandomService.Instance.GetPersistentStream("CardDeck", playerId, autoSave:true);
rng.Shuffle(deck);
var hand = rng.Deal(deck, 5, removeFromSource:true);
```

### UI 工具（编辑器监控器）

- 路径：`自制工具/随机系统/随机数监控器`
- 功能：加载/初始化流、未消耗/消耗预览、直方图、ES3 状态清除、UTC 切换。
- 样式：依赖 `Scripts/0_Editor/UITK/EditorColors.uss`，窗口 `RandomMonitorWindow.cs`。

### 设计要点与边界

- `UnityEngine.Random` 为静态且主线程限定；并行/Job 下建议扩展自定义 PRNG（可实现另一个 `IRandomSource`）。
- 浮点上限为“包含”；与 `System.Random` 不同（参考 Unity 文档）。
- 时区：默认本地日期；如需跨区一致性可改 UTC。
- 性能：`autoSave` 可按需关闭，集中保存。

### 测试

- 同一 `playerId + yyyyMMdd + streamKey` 在同一天输出一致；跨天或换 ID 输出不同。
- 多业务流并行调用，结果独立且互不干扰。
- 重启进程后，ES3 正确恢复并延续流状态。
- 洗牌均匀性、权重抽取正确性（可统计直方图）。

### 近期更新（2025-10）

- **新增真随机模式（默认）**：当业务未显式要求“每日稳定”时，使用多重熵源生成一次性种子，避免 Editor 每次 Play 时 `UnityEngine.Random` 状态重复导致的固定序列问题。
  - 熵源组合：时间戳 `DateTime.Now.Ticks` + `Guid.NewGuid()` + `UnityEngine.Random.value`，三者异或为 `combinedSeed`，再以 `System.Random(combinedSeed)` 采样。
  - 优点：每次运行、同一存档同一日也会产生不同结果；不污染全局 `UnityEngine.Random.state`。
- **保留每日稳定（可选，用于可复现/QA）**：当 `useSeed=true` 时，按 `finalSeed = seed + slotHash + currentDay * 1000` 生成稳定结果；其中 `slotHash` 来源于 `SaveManager.CurrentSlotID`，确保不同存档不同序列。
- **可观测性**：在报纸控制器中增加了日志，区分“使用种子随机”与“使用真随机模式”。

#### 关键实现片段（代码引用）

- 真随机熵源与抽取（`DailyMessagesData.GetRandomEntry(seed:null)`）：

```121:133:Scripts/2_DayMessageScreen/DailyMessagesData.cs
// 使用System.Random基于多重熵源，确保每次调用都是真正动态随机
// 熵源：时间戳 + Guid + Unity随机值，避免Editor中状态重置问题
int timeSeed = (int)(System.DateTime.Now.Ticks & 0x7FFFFFFF);
int guidSeed = System.Guid.NewGuid().GetHashCode();
int unitySeed = (int)(UnityEngine.Random.value * int.MaxValue);
int combinedSeed = timeSeed ^ guidSeed ^ unitySeed;

var rng = new System.Random(combinedSeed);
int idx = rng.Next(0, source.Count);
lastSelectedIndex = Mathf.Clamp(idx, 0, source.Count - 1);

Debug.Log($"[DailyMessagesData] 真随机选择: 索引={lastSelectedIndex}, 熵源组合={combinedSeed}");
return source[lastSelectedIndex];
```

- 每日稳定/真随机模式切换与日志（`NewspaperAnimationController`）：

```106:121:Scripts/2_DayMessageScreen/NewspaperAnimationController.cs
// 基于当前天数+存档ID混合种子，确保每天不同且不同存档不同
int currentDay = TimeSystemManager.Instance != null ? TimeSystemManager.Instance.CurrentDay : 1;
string slotId = SaveManager.Instance != null ? SaveManager.Instance.CurrentSlotID : "1";
int slotHash = string.IsNullOrEmpty(slotId) ? 0 : slotId.GetHashCode();
int finalSeed = useSeed ? (seed + slotHash + currentDay * 1000) : 0;

if (useSeed)
{
    Debug.Log($"[Newspaper] 使用种子随机: 基数={seed}, 存档Hash={slotHash}, 天数={currentDay}, 最终种子={finalSeed}");
}
else
{
    Debug.Log($"[Newspaper] 使用真随机模式（基于时间戳+Guid+Unity.Random）");
}

var entry = dailyMessagesData.GetRandomEntry(useSeed ? (int?)finalSeed : null);
```

- 暴露当前存档 ID（`SaveManager`）：

```17:19:Scripts/0_General/0_6_SaveSystem/SaveManager.cs
// ====== 公开属性 ======
public string CurrentSlotID => _currentSlotID;
```

#### 使用建议

- **生产**：保持 `useSeed=false`（默认），获得最大“不可预测性”。
- **QA/复现**：将 `useSeed=true`，指定 `seed` 并固定 `slotId`、`day`，即可稳定复现。
- **监控**：通过控制台日志确认当前模式与关键参数（slotHash、day、finalSeed/combinedSeed）。

### 参考资料

- Unity 2022.3 Random API：`https://docs.unity3d.com/2022.3/Documentation/ScriptReference/Random.html`
