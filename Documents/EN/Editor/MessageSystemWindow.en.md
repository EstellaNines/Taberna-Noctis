## MessageSystemWindow (Message Bus Monitor)

### Overview

- Purpose: Subscribe/send in-project message events (`MessageManager/MessageDefine`) within the editor for integration and regression.
- Menu: 自制工具/消息系统/消息系统
- Highlights:
  - Live stream of messages and parameters
  - One-click test messages (with common templates)

### Key Features

- Subscribe/Unsubscribe specified keys
- Console-like list (time, key, param summary)
- Quick send (e.g., `SERVICE_PAYMENT_COMPLETE`, `CRAFTING_RESULT`)

### Usage

1. Open “自制工具/消息系统/消息系统”.
2. Input constants from `MessageDefine` and subscribe.
3. While running, verify expected messages; use “Send” to simulate when needed.

### Dependencies

- `MessageManager`, `MessageDefine`.
- Editor UI: UITK (supports `Assets/Editor/UITK/EditorColors.uss` with fallbacks).

### Pitfalls

- Not available in player builds; editor only.
- Key typos lead to no events; always copy constants from `MessageDefine`.
