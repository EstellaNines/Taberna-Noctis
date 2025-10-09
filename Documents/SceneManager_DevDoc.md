# 场景管理器开发文档

项目：Taberna Noctis（夜之小酒馆）  
版本：v1.0  
最后更新：2025-10-05

---

## 场景顺序与加载流程

```mermaid
flowchart LR
  A[0_StartScreen] --> B[1_SaveFilesScreen]
  B --> C[2_DayScreen]
  C --> D[3_NightScreen]
  D --> E[4_SettlementScreen]
  classDef s fill:#eef,stroke:#88f,stroke-width:1px
  class A,B,C,D,E s
```

- Loading 场景：`S_LoadingScreen`
- 旧名兼容：由 `GlobalSceneManager.NormalizeSceneName()` 将旧名映射为带编号的新名。

---

## 时序图：时间系统驱动场景切换

```mermaid
sequenceDiagram
  participant T as TimeSystemManager
  participant C as SceneTimeCoordinator
  participant G as GlobalSceneManager
  participant L as S_LoadingScreen
  participant S as 目标场景

  Note over T: 发送 PHASE_CHANGED / DAY_COMPLETED
  T->>C: PHASE_CHANGED(Morning/Afternoon/Night)
  alt Morning/Afternoon
    C->>G: LoadWithLoadingScreen("2_DayScreen")
  else Night
    C->>G: LoadWithLoadingScreen("3_NightScreen")
  end
  T-->>C: DAY_COMPLETED(day)
  C->>G: LoadWithLoadingScreen("4_SettlementScreen")

  G->>L: 打开 Loading
  G->>G: NormalizeSceneName()
  L-->>G: 加载目标场景完成
  G-->>S: 激活并切换
```

---

## 结构图：核心类与职责

```mermaid
classDiagram
  class GlobalSceneManager {
    +LoadWithLoadingScreen(target, mode)
    +OnGoToName(name)
    -NormalizeSceneName(name) string
  }
  class SceneTimeCoordinator {
    -daySceneName : string
    -nightSceneName : string
    -settlementSceneName : string
    +OnPhaseChanged(TimePhase)
    +OnDayCompleted(int)
  }
  class TimeSystemManager {
    +CurrentPhase : TimePhase
    +StartNightPhase()
    +StartNewDay()
  }

  GlobalSceneManager <.. SceneTimeCoordinator : 使用
  SceneTimeCoordinator <.. TimeSystemManager : 订阅消息
```

---

## 加载期暂停/恢复协同

```mermaid
flowchart TD
  P[进入 Loading] -->|PauseForLoading| T1[TimeSystemManager 暂停计时]
  T1 --> L[执行异步加载]
  L --> E[离开 Loading]
  E -->|inSettlementFlow?| Q{处于结算流程?}
  Q -- 否 --> R[ResumeTimer]
  Q -- 是 --> K[保持暂停, 等待玩家点击新一天]
```

以上三类图表可直接嵌入到设计评审或 README。
