## MissingScriptsScanner (Scan & Clean Missing Scripts)

### Overview

- Purpose: Scan all scenes and prefabs for missing scripts and provide one-click cleaning for the current scene.
- Menu:
  - 自制工具/诊断/扫描缺失脚本
  - 自制工具/诊断/清理当前场景缺失脚本
- When to use: Pre-build health check; quickly locating issues when stuck at loading.

### Key Implementation (Excerpt)

```1:17:Editor/MissingScriptsScanner.cs
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Scan missing scripts in all scenes & prefabs and clean them in the current scene.
/// Menus: 自制工具/诊断/扫描缺失脚本, 自制工具/诊断/清理当前场景缺失脚本
/// </summary>
public static class MissingScriptsScanner
{
    [MenuItem("自制工具/诊断/扫描缺失脚本")]
    public static void ScanAll()
```

```19:43:Editor/MissingScriptsScanner.cs
        // 1) Scan all enabled scenes in BuildSettings
        var buildScenes = EditorBuildSettings.scenes;
        for (int i = 0; i < buildScenes.Length; i++)
        {
            var s = buildScenes[i];
            if (!s.enabled) continue;
            var scene = EditorSceneManager.OpenScene(s.path, OpenSceneMode.Single);
            totalMissing += ScanScene(scene);
        }

        // 2) Scan all Prefabs
        var prefabGuids = AssetDatabase.FindAssets("t:Prefab");
        for (int i = 0; i < prefabGuids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (go == null) continue;
            totalMissing += ScanGameObject(go, $"[Prefab] {path}");
        }

        if (totalMissing == 0)
            Debug.Log("[MissingScriptsScanner] No missing scripts found.");
        else
            Debug.LogWarning($"[MissingScriptsScanner] Missing component count: {totalMissing}. Fix by logs or use Clean.");
```

```106:119:Editor/MissingScriptsScanner.cs
    private static int CleanGameObject(GameObject go)
    {
        int before = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
        if (before > 0)
        {
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
        }
        int cleaned = before;
        for (int i = 0; i < go.transform.childCount; i++)
        {
            cleaned += CleanGameObject(go.transform.GetChild(i).gameObject);
        }
        return cleaned;
    }
```

### How to Use

1. Run “自制工具/诊断/扫描缺失脚本”.
   - Console prints counts and object paths for each scene/prefab.
2. Fix manually or use “自制工具/诊断/清理当前场景缺失脚本” to quickly remove invalid components.
3. Save the scene after cleaning and rescan to ensure 0 remains.

### Implementation Notes

- Iterate enabled Build Settings scenes by `OpenScene` and traverse root objects.
- Prefabs scanned by `AssetDatabase.FindAssets("t:Prefab")`.
- Cleaning uses `GameObjectUtility.RemoveMonoBehavioursWithMissingScript` (safe).

### Pitfalls & Tips

- Moving scripts into `Editor/` breaks runtime component references; reattach runtime scripts.
- Scanning opens scenes; use VCS to avoid accidental saves.
- Fix prefab-level Missing at the asset, not only in scene instances.

### Changelog (Excerpt)

- Initial: pre-build health check and "stuck loading" diagnostics.
