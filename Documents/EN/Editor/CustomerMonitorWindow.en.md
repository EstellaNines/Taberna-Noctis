## CustomerMonitorWindow (Customer Arrival/Service State Monitor)

### Overview

- Purpose: Visualize real-time customer generation/queue/service/settlement in Night scenes; support force-enqueue and cooldown reset.
- Menu: 自制工具/人物设计/顾客到来监控器

### Key Features

- Lists: current queue / in-service / departed.
- One-click: force spawn, clear cooldowns, reset flow.
- Quick overview of key variables (mood, identity multiplier, waiting time).

### Usage

1. Enter the Night scene and run.
2. Open the window to observe service states and event order.
3. Use force operations to reproduce edge cases (wait timeout / drink only once, etc.).

### Dependencies

- `CustomerServiceManager`, `CustomerSpawnManager`, `CustomerNpcView`.
- May use reflection to access private fields (mind IL2CPP stripping).

### Pitfalls

- EditorWindow is not included in player builds.
- Reflection field names must be kept in sync and preserved by link.xml for IL2CPP.
