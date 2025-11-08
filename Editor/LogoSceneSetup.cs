using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;
using TabernaNoctis.UI;

namespace TabernaNoctis.Editor
{
    /// <summary>
    /// Logo 场景设置工具
    /// 自动配置 Logo 视频播放场景
    /// </summary>
    public class LogoSceneSetup : EditorWindow
    {
        private VideoClip videoClip;
        private string nextSceneName = "0_StartScreen";
        private bool allowSkip = true;
        private float minPlayTime = 0.5f;
        private float fadeInDuration = 0.5f;
        private float fadeOutDuration = 0.5f;

        [MenuItem("自制工具/Logo视频快速设置/Logo Scene Setup")]
        public static void ShowWindow()
        {
            var window = GetWindow<LogoSceneSetup>("Logo视频快速设置");
            window.minSize = new Vector2(400, 300);
            window.Show();
        }

        private void OnGUI()
        {
            GUILayout.Label("Logo 视频场景设置工具", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "此工具将自动在 L_LogoScreen 场景中设置视频播放器。\n" +
                "确保你已经将视频文件放在 Resources/Video 文件夹中。",
                MessageType.Info);

            EditorGUILayout.Space(10);

            // 视频设置
            GUILayout.Label("视频设置", EditorStyles.boldLabel);
            videoClip = (VideoClip)EditorGUILayout.ObjectField("视频片段", videoClip, typeof(VideoClip), false);
            nextSceneName = EditorGUILayout.TextField("下一个场景名称", nextSceneName);
            
            EditorGUILayout.Space(5);

            // 交互设置
            GUILayout.Label("交互设置", EditorStyles.boldLabel);
            allowSkip = EditorGUILayout.Toggle("允许跳过", allowSkip);
            if (allowSkip)
            {
                minPlayTime = EditorGUILayout.FloatField("最小播放时间（秒）", minPlayTime);
            }

            EditorGUILayout.Space(5);

            // 淡入淡出设置
            GUILayout.Label("淡入淡出设置", EditorStyles.boldLabel);
            fadeInDuration = EditorGUILayout.FloatField("淡入时长（秒）", fadeInDuration);
            fadeOutDuration = EditorGUILayout.FloatField("淡出时长（秒）", fadeOutDuration);

            EditorGUILayout.Space(20);

            // 按钮区域
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("自动设置 Logo 场景", GUILayout.Height(30), GUILayout.Width(200)))
            {
                SetupLogoScene();
            }
            
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            if (GUILayout.Button("打开 L_LogoScreen 场景"))
            {
                OpenLogoScene();
            }

            if (GUILayout.Button("配置 Build Settings"))
            {
                ConfigureBuildSettings();
            }
        }

        /// <summary>
        /// 设置 Logo 场景
        /// </summary>
        private void SetupLogoScene()
        {
            // 打开场景
            string scenePath = "Assets/Scenes/L_LogoScreen.unity";
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            // 创建 Canvas
            GameObject canvasObj = new GameObject("Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            // 创建黑色背景
            GameObject backgroundObj = new GameObject("Background");
            backgroundObj.transform.SetParent(canvasObj.transform, false);
            
            RectTransform bgRect = backgroundObj.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            bgRect.anchoredPosition = Vector2.zero;
            
            Image bgImage = backgroundObj.AddComponent<Image>();
            bgImage.color = Color.black;

            // 创建视频播放器对象
            GameObject videoPlayerObj = new GameObject("VideoPlayer");
            videoPlayerObj.transform.SetParent(canvasObj.transform, false);
            
            // 添加 RectTransform 使其填满整个屏幕
            RectTransform videoRect = videoPlayerObj.AddComponent<RectTransform>();
            videoRect.anchorMin = Vector2.zero;
            videoRect.anchorMax = Vector2.one;
            videoRect.sizeDelta = Vector2.zero;
            videoRect.anchoredPosition = Vector2.zero;

            // 添加 RawImage 用于显示视频
            RawImage rawImage = videoPlayerObj.AddComponent<RawImage>();
            rawImage.color = Color.white;

            // 添加 VideoPlayer 组件
            VideoPlayer videoPlayer = videoPlayerObj.AddComponent<VideoPlayer>();
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            
            // 创建 RenderTexture
            RenderTexture renderTexture = new RenderTexture(1920, 1080, 0);
            renderTexture.name = "VideoRenderTexture";
            videoPlayer.targetTexture = renderTexture;
            rawImage.texture = renderTexture;

            // 设置视频源
            if (videoClip != null)
            {
                videoPlayer.clip = videoClip;
                videoPlayer.source = VideoSource.VideoClip;
            }
            else
            {
                // 尝试加载 Resources 中的视频
                VideoClip clip = Resources.Load<VideoClip>("Video/签名");
                if (clip != null)
                {
                    videoPlayer.clip = clip;
                    videoPlayer.source = VideoSource.VideoClip;
                }
            }

            videoPlayer.playOnAwake = false;
            videoPlayer.isLooping = false;
            videoPlayer.skipOnDrop = true;
            videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;

            // 添加 CanvasGroup 用于淡入淡出
            CanvasGroup canvasGroup = canvasObj.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;

            // 添加 LogoVideoPlayer 脚本
            LogoVideoPlayer logoPlayer = videoPlayerObj.AddComponent<LogoVideoPlayer>();
            
            // 使用反射设置私有字段
            var so = new SerializedObject(logoPlayer);
            so.FindProperty("videoClip").objectReferenceValue = videoPlayer.clip;
            so.FindProperty("nextSceneName").stringValue = nextSceneName;
            so.FindProperty("allowSkip").boolValue = allowSkip;
            so.FindProperty("minPlayTime").floatValue = minPlayTime;
            so.FindProperty("fadeInDuration").floatValue = fadeInDuration;
            so.FindProperty("fadeOutDuration").floatValue = fadeOutDuration;
            so.FindProperty("fadeCanvasGroup").objectReferenceValue = canvasGroup;
            so.ApplyModifiedProperties();

            // 创建摄像机
            GameObject cameraObj = GameObject.Find("Main Camera");
            if (cameraObj == null)
            {
                cameraObj = new GameObject("Main Camera");
                Camera cam = cameraObj.AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = Color.black;
                cam.orthographic = true;
                cam.nearClipPlane = 0.3f;
                cam.farClipPlane = 1000f;
                cameraObj.tag = "MainCamera";
                
                cameraObj.AddComponent<AudioListener>();
            }

            // 保存场景
            EditorSceneManager.SaveScene(scene);

            Debug.Log("[LogoSceneSetup] Logo 场景设置完成！");
            EditorUtility.DisplayDialog("设置完成", 
                "Logo 场景已成功设置！\n\n" +
                "下一步：\n" +
                "1. 检查场景中的设置\n" +
                "2. 配置 Build Settings（使用下面的按钮）\n" +
                "3. 测试播放效果", 
                "确定");
        }

        /// <summary>
        /// 打开 Logo 场景
        /// </summary>
        private void OpenLogoScene()
        {
            string scenePath = "Assets/Scenes/L_LogoScreen.unity";
            if (System.IO.File.Exists(scenePath))
            {
                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            }
            else
            {
                EditorUtility.DisplayDialog("错误", "未找到 L_LogoScreen.unity 场景文件！", "确定");
            }
        }

        /// <summary>
        /// 配置 Build Settings
        /// </summary>
        private void ConfigureBuildSettings()
        {
            // 获取当前的场景列表
            var scenes = EditorBuildSettings.scenes;
            var sceneList = new System.Collections.Generic.List<EditorBuildSettingsScene>(scenes);

            // Logo 场景路径
            string logoScenePath = "Assets/Scenes/L_LogoScreen.unity";
            string startScenePath = "Assets/Scenes/0_StartScreen.unity";

            // 移除已存在的 Logo 场景
            sceneList.RemoveAll(s => s.path == logoScenePath);

            // 在开头添加 Logo 场景
            sceneList.Insert(0, new EditorBuildSettingsScene(logoScenePath, true));

            // 确保 StartScreen 在第二个位置
            int startSceneIndex = sceneList.FindIndex(s => s.path == startScenePath);
            if (startSceneIndex > 1)
            {
                var startScene = sceneList[startSceneIndex];
                sceneList.RemoveAt(startSceneIndex);
                sceneList.Insert(1, startScene);
            }

            // 应用更改
            EditorBuildSettings.scenes = sceneList.ToArray();

            Debug.Log("[LogoSceneSetup] Build Settings 已配置！Logo 场景现在是第一个加载的场景。");
            EditorUtility.DisplayDialog("配置完成", 
                "Build Settings 已更新！\n\n" +
                "场景顺序：\n" +
                "1. L_LogoScreen\n" +
                "2. 0_StartScreen\n" +
                "3. 其他场景...", 
                "确定");

            // 打开 Build Settings 窗口供确认
            EditorWindow.GetWindow(System.Type.GetType("UnityEditor.BuildPlayerWindow,UnityEditor"));
        }
    }
}

