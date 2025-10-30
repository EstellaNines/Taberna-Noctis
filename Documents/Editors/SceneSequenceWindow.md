## SceneSequenceWindow（场景顺序管理器）

### 概述

- 用途：可视化 `Build Settings` 场景顺序，校验必需场景与命名规范，一键跳转。
- 菜单：自制工具/场景/场景顺序管理器

### 主要功能

- 构建列表可视化、启用状态检查；
- 必需场景缺失告警（如 0_Start、1_SaveFiles、2_DayMessage、…）；
- 选中并快速 `OpenScene`。

### 使用说明

1. 打开窗口，点击“刷新”；
2. 修复红色告警项（缺失/未启用/命名不一致）；
3. 一键打开场景定位问题对象。

### 依赖

- `EditorBuildSettings`、`EditorSceneManager`。

### 常见陷阱

- 场景未启用导致流程中断或卡 Loading；
- 命名改动未同步到跳转逻辑常量。
