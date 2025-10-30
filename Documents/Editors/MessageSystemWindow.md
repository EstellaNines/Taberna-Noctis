## MessageSystemWindow（消息系统监控台）

### 概述

- 用途：在编辑器下订阅/发送项目内的消息事件（`MessageManager/MessageDefine`），便于联调与回归。
- 菜单：自制工具/消息系统/消息系统
- 特色：
  - 实时查看消息流水与参数；
  - 一键发送测试消息（含常用模板）。

### 主要功能

- 订阅/取消订阅指定 Key；
- 控制台式列表展示（时间、Key、参数摘要）；
- 快捷发送（常用如 `SERVICE_PAYMENT_COMPLETE`、`CRAFTING_RESULT` 等）。

### 使用说明

1. 打开“自制工具/消息系统/消息系统”。
2. 在输入框填入 `MessageDefine` 中的常量 Key 并订阅。
3. 在运行态观察消息是否按预期触发；必要时用“发送”模拟触发。

### 依赖

- `MessageManager`、`MessageDefine`；
- Editor UI: UITK（已适配 `Assets/Editor/UITK/EditorColors.uss` 及回退路径）。

### 常见陷阱

- 构建版本中无 EditorWindow；请仅在编辑器下使用。
- Key 拼写错误导致无事件；统一从 `MessageDefine` 复制常量名。
