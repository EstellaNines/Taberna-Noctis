## SaveViewerWindow (Save Inspector)

### Overview

- Purpose: Browse/edit save slots in the editor, view JSON/key fields, perform clean/delete operations.
- Menu: 自制工具/保存系统/存档监控器

### Key Features

- Slot list, details view, raw JSON view
- One-click clean today stats, delete slot, import/export
- Quick locate key fields: `unlockedRecipes`, `currentMenuRecipeIDs`, money, reputation

### Usage

1. Open the window and select a slot.
2. Use the Details/JSON tabs to cross-check runtime behaviors.
3. Clean or export backups when needed.

### Dependencies

- `SaveManager`, `SaveData`, `RecipeData`.
- Optional: ES3 read/write helpers.

### Pitfalls

- Editing saves while running is risky; export backups first.
- Not available in player builds.
