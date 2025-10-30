## RandomMonitorWindow (RNG Monitor)

### Overview

- Purpose: Visualize daily seeds, runtime PRNG sequences, and histograms to validate "stable within day, varying across days".
- Menu: 自制工具/随机系统/随机数监控器

### Key Features

- Show current saved seed and sequence progress
- Sampling histogram / mean & variance stats
- View ES3-persisted seed state (if used)

### Usage

1. Open the window and run the target scene.
2. Observe sampling distributions across nodes, ensure no skew/outliers.

### Dependencies

- Central RNG entry (e.g., `RandomSystemManager` or helper wrapper)
- Optional: `Easy Save 3` persistence

### Pitfalls

- Editor vs runtime PRNG mismatch; fix the seed and distribute RNG from a single entry.
