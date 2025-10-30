## SceneSequenceWindow (Build Scene Order Manager)

### Overview

- Purpose: Visualize `Build Settings` scene order, validate required scenes and naming conventions, and open scenes quickly.
- Menu: 自制工具/场景/场景顺序管理器

### Key Features

- Visualize build list and enabled status
- Missing required scenes warnings (e.g., 0_Start, 1_SaveFiles, 2_DayMessage, …)
- Select and `OpenScene` with one click

### Usage

1. Open the window and click “Refresh”.
2. Fix red warnings (missing/disabled/naming mismatch).
3. Open scenes with one click to locate problematic objects.

### Dependencies

- `EditorBuildSettings`, `EditorSceneManager`.

### Pitfalls

- Disabled scenes cause flow breaks or loading stall.
- Naming changes not synced with constants used by navigation logic.
