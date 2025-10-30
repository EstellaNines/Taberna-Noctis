## MissingScriptsScanner（缺失脚本扫描与清理）

### 概述

- 用途：扫描所有场景与 Prefab 的缺失脚本，并支持一键清理当前场景的缺失脚本组件。
- 菜单：
  - 自制工具/诊断/扫描缺失脚本
  - 自制工具/诊断/清理当前场景缺失脚本
- 适用阶段：构建前健康检查、卡在加载界面时的快速定位。

### 关键实现（节选）

```1:17:Editor/MissingScriptsScanner.cs
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 扫描所有场景与预制件中的丢失脚本，并支持一键清理。
/// 菜单：自制工具/诊断/扫描缺失脚本、自制工具/诊断/清理当前场景缺失脚本
/// </summary>
public static class MissingScriptsScanner
{
    [MenuItem("自制工具/诊断/扫描缺失脚本")]
    public static void ScanAll()
```

```19:43:Editor/MissingScriptsScanner.cs
        // 1) 扫描 BuildSettings 中的所有场景
        var buildScenes = EditorBuildSettings.scenes;
        for (int i = 0; i < buildScenes.Length; i++)
        {
            var s = buildScenes[i];
            if (!s.enabled) continue;
            var scene = EditorSceneManager.OpenScene(s.path, OpenSceneMode.Single);
            totalMissing += ScanScene(scene);
        }

        // 2) 扫描所有 Prefab
        var prefabGuids = AssetDatabase.FindAssets("t:Prefab");
        for (int i = 0; i < prefabGuids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (go == null) continue;
            totalMissing += ScanGameObject(go, $"[Prefab] {path}");
        }

        if (totalMissing == 0)
            Debug.Log("[MissingScriptsScanner] 未发现缺失脚本。");
        else
            Debug.LogWarning($"[MissingScriptsScanner] 共发现缺失脚本组件数量：{totalMissing}，请按日志逐一修复或使用清理功能移除无效组件。");
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

### 使用说明

1. 打开“自制工具/诊断/扫描缺失脚本”。
   - 控制台输出每个场景/Prefab 的缺失数量与对象路径。
2. 逐一修复或用“自制工具/诊断/清理当前场景缺失脚本”快速移除无效组件。
3. 清理后务必保存场景，并重新运行一次扫描确认为 0。

### 实现要点

- 扫描 Build Settings 中启用的场景，逐个 `OpenScene` 并遍历根对象。
- Prefab 扫描通过 `AssetDatabase.FindAssets("t:Prefab")` 全量遍历。
- 清理使用 Unity 提供的 `GameObjectUtility.RemoveMonoBehavioursWithMissingScript`，安全可靠。

### 常见陷阱与建议

- 脚本被移动到 `Editor/` 后，场景中挂载的运行时代码引用变为 Missing，需要重新挂载运行时版本。
- 扫描会打开场景，请在版本控制下操作，避免误保存。
- 对 Prefab 的 Missing 需要在资源层修复，而不是只清理场景里的实例。

### 变更记录（摘）

- 首次引入：用于打包前健康检查与“卡 Loading”排障。
