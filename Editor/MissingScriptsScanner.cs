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
    {
        int totalMissing = 0;

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
    }

    [MenuItem("自制工具/诊断/清理当前场景缺失脚本")] 
    public static void CleanCurrentScene()
    {
        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid())
        {
            Debug.LogWarning("[MissingScriptsScanner] 当前无有效场景。");
            return;
        }

        int cleaned = 0;
        foreach (var root in scene.GetRootGameObjects())
        {
            cleaned += CleanGameObject(root);
        }
        if (cleaned > 0)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.LogWarning($"[MissingScriptsScanner] 已清理当前场景缺失脚本组件数量：{cleaned}，请记得保存场景。");
        }
        else
        {
            Debug.Log("[MissingScriptsScanner] 当前场景未发现缺失脚本。");
        }
    }

    private static int ScanScene(Scene scene)
    {
        if (!scene.IsValid()) return 0;
        int count = 0;
        foreach (var root in scene.GetRootGameObjects())
        {
            count += ScanGameObject(root, $"[Scene] {scene.name}");
        }
        if (count > 0)
        {
            Debug.LogWarning($"[MissingScriptsScanner] 场景 {scene.path} 中发现缺失脚本组件数量：{count}");
        }
        return count;
    }

    private static int ScanGameObject(GameObject go, string context)
    {
        int missing = 0;
        var comps = go.GetComponents<Component>();
        for (int i = 0; i < comps.Length; i++)
        {
            if (comps[i] == null)
            {
                missing++;
                Debug.LogWarning($"[MissingScript] 对象: {GetHierarchyPath(go)} | 位置: {context}");
            }
        }
        // 递归子节点
        for (int i = 0; i < go.transform.childCount; i++)
        {
            missing += ScanGameObject(go.transform.GetChild(i).gameObject, context);
        }
        return missing;
    }

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

    private static string GetHierarchyPath(GameObject go)
    {
        var names = new List<string>();
        var t = go.transform;
        while (t != null)
        {
            names.Add(t.name);
            t = t.parent;
        }
        names.Reverse();
        return string.Join("/", names);
    }
}


