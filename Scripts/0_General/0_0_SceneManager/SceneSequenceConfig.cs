using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Sirenix.OdinInspector;
#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "SceneSequenceConfig", menuName = "Config/Scene Sequence", order = 10)]
public class SceneSequenceConfig : ScriptableObject
{
    [Serializable]
    public class SceneEntry
    {
        [LabelText("场景名")] public string sceneName; // 必须与 Build Settings 中的场景名一致
        [LabelText("加载模式")] public LoadSceneMode loadMode = LoadSceneMode.Single;
        [LabelText("标签")] public string tag; // 可选：阶段标签（如 Start/Day/Night 等）

#if UNITY_EDITOR
        [LabelText("场景引用"), AssetsOnly]
        public SceneAsset sceneAsset;

        private void SyncFromSceneAsset()
        {
            sceneName = sceneAsset != null ? sceneAsset.name : sceneName;
        }

        [OnValueChanged("SyncFromSceneAsset"), ShowInInspector, LabelText("同步场景名"), ReadOnly]
        private string _editorSceneName => sceneAsset != null ? sceneAsset.name : sceneName;
#endif
    }

    [BoxGroup("基础设置")]
    [LabelText("Loading 场景名")] public string loadingScreenSceneName = "LoadingScreen";

#if UNITY_EDITOR
    [BoxGroup("基础设置"), LabelText("Loading 场景引用"), AssetsOnly]
    public SceneAsset loadingScreenScene;

    private void SyncLoadingNameFromAsset()
    {
        loadingScreenSceneName = loadingScreenScene != null ? loadingScreenScene.name : loadingScreenSceneName;
    }

    [OnValueChanged("SyncLoadingNameFromAsset"), ShowInInspector, LabelText("同步 Loading 名"), ReadOnly]
    private string _editorLoadingName => loadingScreenScene != null ? loadingScreenScene.name : loadingScreenSceneName;
#endif

    [BoxGroup("顺序配置")]
    [LabelText("顺序场景列表")]
    [ListDrawerSettings(ShowIndexLabels = true, DraggableItems = true, ShowFoldout = true, DefaultExpandedState = true)]
    public List<SceneEntry> orderedScenes = new List<SceneEntry>();

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


