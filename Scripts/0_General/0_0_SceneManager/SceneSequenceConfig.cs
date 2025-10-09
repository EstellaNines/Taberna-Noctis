using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif
#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "SceneSequenceConfig", menuName = "Config/Scene Sequence Config")]
public class SceneSequenceConfig : ScriptableObject
{
#if ODIN_INSPECTOR
    [BoxGroup("基础设置")]
    [LabelText("Loading 场景名")] public string loadingScreenSceneName = "S_LoadingScreen";
    [BoxGroup("基础设置")]
    [LabelText("Loading 场景引用")] public UnityEngine.Object loadingScreenScene;
#else
    public string loadingScreenSceneName = "S_LoadingScreen";
    public UnityEngine.Object loadingScreenScene;
#endif

    [System.Serializable]
    public class OrderedScene
    {
        public string sceneName;
        public UnityEngine.Object sceneAsset;
        public LoadSceneMode loadMode = LoadSceneMode.Single;
    }

#if ODIN_INSPECTOR
    [BoxGroup("顺序设置")]
    [TableList(AlwaysExpanded = true)]
#endif
    public List<OrderedScene> orderedScenes = new List<OrderedScene>();

    // 提供一个可枚举器用于调试/校验
    public IEnumerable<string> EnumerateAllSceneNames()
    {
        if (orderedScenes != null)
        {
            foreach (var s in orderedScenes)
            {
                if (!string.IsNullOrEmpty(s.sceneName)) yield return s.sceneName;
            }
        }
        yield return "0_StartScreen";
        yield return "S_LoadingScreen";
        yield return "1_SaveFilesScreen";
    }

    [BoxGroup("工具")]
    [Button("验证配置"), PropertySpace(8)]
    private void Validate()
    {
        if (string.IsNullOrEmpty(loadingScreenSceneName))
        {
            Debug.LogWarning("[SceneSequenceConfig] 未设置 Loading 场景名。");
        }
        for (int i = 0; i < orderedScenes.Count; i++)
        {
            var e = orderedScenes[i];
            if (e == null || string.IsNullOrEmpty(e.sceneName))
            {
                Debug.LogWarning($"[SceneSequenceConfig] 第 {i} 项场景名为空。");
                continue;
            }
#if UNITY_EDITOR
            if (!IsSceneInBuild(e.sceneName))
            {
                Debug.LogWarning($"[SceneSequenceConfig] 场景 '{e.sceneName}' 不在 Build Settings 中。");
            }
#endif
        }
        Debug.Log("[SceneSequenceConfig] 验证完成。");
    }

#if UNITY_EDITOR
    private static bool IsEditorSafe()
    {
        // 在域备份或编译中避免访问 Unity 对象/资产
        return !EditorApplication.isUpdating && !EditorApplication.isCompiling && !EditorApplication.isPlayingOrWillChangePlaymode;
    }

    private static bool IsSceneInBuild(string sceneName)
    {
        if (!IsEditorSafe()) return false;
        foreach (var s in EditorBuildSettings.scenes)
        {
            var name = System.IO.Path.GetFileNameWithoutExtension(s.path);
            if (string.Equals(name, sceneName, StringComparison.Ordinal)) return true;
        }
        return false;
    }
#endif

    // 仅供早前兼容：不再在下拉中使用，保留以便其它工具调用
    private static IEnumerable<string> GetAllScenesInBuild()
    {
#if UNITY_EDITOR
        if (IsEditorSafe())
        {
            foreach (var s in EditorBuildSettings.scenes)
            {
                yield return System.IO.Path.GetFileNameWithoutExtension(s.path);
            }
            yield break;
        }
        yield return "(编辑器忙碌)";
#else
        // 运行时环境无法读取 EditorBuildSettings，这里返回一个占位列表
        yield return "StartScreen";
        yield return "LoadingScreen";
        yield return "SaveFilesScreen";
#endif
    }
}


