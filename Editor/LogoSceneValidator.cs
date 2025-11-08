using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Video;
using UnityEngine.UI;
using TabernaNoctis.UI;
using System.Collections.Generic;

namespace TabernaNoctis.Editor
{
    /// <summary>
    /// Logo 场景验证工具
    /// 检查 Logo 视频播放场景的配置是否正确
    /// </summary>
    public class LogoSceneValidator : EditorWindow
    {
        private Vector2 scrollPosition;
        private List<ValidationResult> validationResults = new List<ValidationResult>();

        private class ValidationResult
        {
            public string category;
            public string message;
            public MessageType type; // Info, Warning, Error
            public bool passed;

            public ValidationResult(string category, string message, MessageType type, bool passed = true)
            {
                this.category = category;
                this.message = message;
                this.type = type;
                this.passed = passed;
            }
        }

        [MenuItem("Taberna Noctis/Setup/Validate Logo Scene")]
        public static void ShowWindow()
        {
            var window = GetWindow<LogoSceneValidator>("Logo 场景验证");
            window.minSize = new Vector2(500, 400);
            window.Show();
        }

        private void OnGUI()
        {
            GUILayout.Label("Logo 场景配置验证工具", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "此工具会检查 Logo 视频播放场景的配置是否正确。\n" +
                "点击下方按钮开始验证。",
                MessageType.Info);

            EditorGUILayout.Space(10);

            if (GUILayout.Button("开始验证", GUILayout.Height(35)))
            {
                ValidateScene();
            }

            EditorGUILayout.Space(10);

            // 显示验证结果
            if (validationResults.Count > 0)
            {
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

                string currentCategory = "";
                foreach (var result in validationResults)
                {
                    // 显示分类标题
                    if (result.category != currentCategory)
                    {
                        currentCategory = result.category;
                        EditorGUILayout.Space(5);
                        GUILayout.Label(currentCategory, EditorStyles.boldLabel);
                    }

                    // 显示验证结果
                    EditorGUILayout.BeginHorizontal();
                    
                    string icon = result.passed ? "✅" : (result.type == MessageType.Error ? "❌" : "⚠️");
                    GUILayout.Label(icon, GUILayout.Width(30));
                    
                    EditorGUILayout.HelpBox(result.message, result.type);
                    
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndScrollView();

                EditorGUILayout.Space(10);

                // 统计
                int passed = validationResults.FindAll(r => r.passed).Count;
                int warnings = validationResults.FindAll(r => !r.passed && r.type == MessageType.Warning).Count;
                int errors = validationResults.FindAll(r => !r.passed && r.type == MessageType.Error).Count;

                string summary = $"验证完成：{passed} 项通过，{warnings} 项警告，{errors} 项错误";
                MessageType summaryType = errors > 0 ? MessageType.Error : 
                                         warnings > 0 ? MessageType.Warning : 
                                         MessageType.Info;
                EditorGUILayout.HelpBox(summary, summaryType);
            }
        }

        /// <summary>
        /// 验证场景配置
        /// </summary>
        private void ValidateScene()
        {
            validationResults.Clear();

            // 1. 检查场景文件
            ValidateSceneFile();

            // 2. 检查视频资源
            ValidateVideoFile();

            // 3. 检查 Build Settings
            ValidateBuildSettings();

            // 4. 如果场景已打开，检查场景内容
            if (EditorSceneManager.GetActiveScene().name == "L_LogoScreen")
            {
                ValidateSceneContent();
            }
            else
            {
                validationResults.Add(new ValidationResult(
                    "场景检查",
                    "L_LogoScreen 场景未打开。打开场景以进行详细验证。",
                    MessageType.Info,
                    true
                ));
            }

            Repaint();
        }

        /// <summary>
        /// 检查场景文件是否存在
        /// </summary>
        private void ValidateSceneFile()
        {
            string scenePath = "Assets/Scenes/L_LogoScreen.unity";
            bool exists = System.IO.File.Exists(scenePath);

            validationResults.Add(new ValidationResult(
                "场景文件",
                exists ? "L_LogoScreen.unity 场景文件存在。" : "未找到 L_LogoScreen.unity 场景文件！",
                exists ? MessageType.Info : MessageType.Error,
                exists
            ));
        }

        /// <summary>
        /// 检查视频文件
        /// </summary>
        private void ValidateVideoFile()
        {
            // 检查 Resources 文件夹
            string videoPath = "Assets/Resources/Video/签名.avi";
            bool aviExists = System.IO.File.Exists(videoPath);

            if (aviExists)
            {
                validationResults.Add(new ValidationResult(
                    "视频资源",
                    "找到视频文件：签名.avi",
                    MessageType.Info,
                    true
                ));

                // 检查视频是否可以加载
                VideoClip clip = Resources.Load<VideoClip>("Video/签名");
                if (clip != null)
                {
                    validationResults.Add(new ValidationResult(
                        "视频资源",
                        $"视频资源加载成功（时长：{clip.length:F2}秒）",
                        MessageType.Info,
                        true
                    ));
                }
                else
                {
                    validationResults.Add(new ValidationResult(
                        "视频资源",
                        "视频文件存在但无法加载为 VideoClip。可能需要重新导入。",
                        MessageType.Warning,
                        false
                    ));
                }
            }
            else
            {
                validationResults.Add(new ValidationResult(
                    "视频资源",
                    "未找到视频文件 签名.avi！请将视频放在 Resources/Video/ 文件夹中。",
                    MessageType.Error,
                    false
                ));
            }
        }

        /// <summary>
        /// 检查 Build Settings
        /// </summary>
        private void ValidateBuildSettings()
        {
            var scenes = EditorBuildSettings.scenes;

            // 检查是否包含 Logo 场景
            string logoScenePath = "Assets/Scenes/L_LogoScreen.unity";
            bool logoSceneFound = false;
            int logoSceneIndex = -1;

            for (int i = 0; i < scenes.Length; i++)
            {
                if (scenes[i].path == logoScenePath)
                {
                    logoSceneFound = true;
                    logoSceneIndex = i;
                    break;
                }
            }

            if (logoSceneFound)
            {
                if (logoSceneIndex == 0)
                {
                    validationResults.Add(new ValidationResult(
                        "Build Settings",
                        "L_LogoScreen 是 Build Settings 中的第一个场景。✓",
                        MessageType.Info,
                        true
                    ));
                }
                else
                {
                    validationResults.Add(new ValidationResult(
                        "Build Settings",
                        $"L_LogoScreen 在 Build Settings 中的位置是 [{logoSceneIndex}]，但应该是 [0]（第一个）！",
                        MessageType.Warning,
                        false
                    ));
                }
            }
            else
            {
                validationResults.Add(new ValidationResult(
                    "Build Settings",
                    "L_LogoScreen 未添加到 Build Settings！",
                    MessageType.Error,
                    false
                ));
            }

            // 检查是否包含 StartScreen
            string startScenePath = "Assets/Scenes/0_StartScreen.unity";
            bool startSceneFound = false;

            foreach (var scene in scenes)
            {
                if (scene.path == startScenePath)
                {
                    startSceneFound = true;
                    break;
                }
            }

            if (startSceneFound)
            {
                validationResults.Add(new ValidationResult(
                    "Build Settings",
                    "0_StartScreen 已添加到 Build Settings。",
                    MessageType.Info,
                    true
                ));
            }
            else
            {
                validationResults.Add(new ValidationResult(
                    "Build Settings",
                    "0_StartScreen 未添加到 Build Settings！",
                    MessageType.Error,
                    false
                ));
            }
        }

        /// <summary>
        /// 检查场景内容
        /// </summary>
        private void ValidateSceneContent()
        {
            // 检查摄像机
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                validationResults.Add(new ValidationResult(
                    "场景对象",
                    "找到主摄像机。",
                    MessageType.Info,
                    true
                ));

                // 检查 Audio Listener
                if (mainCamera.GetComponent<AudioListener>() != null)
                {
                    validationResults.Add(new ValidationResult(
                        "场景对象",
                        "主摄像机有 Audio Listener 组件。",
                        MessageType.Info,
                        true
                    ));
                }
                else
                {
                    validationResults.Add(new ValidationResult(
                        "场景对象",
                        "主摄像机缺少 Audio Listener 组件！视频将没有声音。",
                        MessageType.Warning,
                        false
                    ));
                }
            }
            else
            {
                validationResults.Add(new ValidationResult(
                    "场景对象",
                    "未找到主摄像机！",
                    MessageType.Error,
                    false
                ));
            }

            // 检查 Canvas
            Canvas canvas = Object.FindObjectOfType<Canvas>();
            if (canvas != null)
            {
                validationResults.Add(new ValidationResult(
                    "场景对象",
                    "找到 Canvas。",
                    MessageType.Info,
                    true
                ));

                // 检查 Canvas Group
                CanvasGroup canvasGroup = canvas.GetComponent<CanvasGroup>();
                if (canvasGroup != null)
                {
                    validationResults.Add(new ValidationResult(
                        "场景对象",
                        "Canvas 有 CanvasGroup 组件（用于淡入淡出）。",
                        MessageType.Info,
                        true
                    ));
                }
                else
                {
                    validationResults.Add(new ValidationResult(
                        "场景对象",
                        "Canvas 缺少 CanvasGroup 组件！淡入淡出效果将不工作。",
                        MessageType.Warning,
                        false
                    ));
                }
            }
            else
            {
                validationResults.Add(new ValidationResult(
                    "场景对象",
                    "未找到 Canvas！",
                    MessageType.Error,
                    false
                ));
            }

            // 检查 VideoPlayer 和脚本
            LogoVideoPlayer logoPlayer = Object.FindObjectOfType<LogoVideoPlayer>();
            if (logoPlayer != null)
            {
                validationResults.Add(new ValidationResult(
                    "脚本组件",
                    "找到 LogoVideoPlayer 脚本。",
                    MessageType.Info,
                    true
                ));

                // 检查 VideoPlayer 组件
                VideoPlayer videoPlayer = logoPlayer.GetComponent<VideoPlayer>();
                if (videoPlayer != null)
                {
                    validationResults.Add(new ValidationResult(
                        "脚本组件",
                        "找到 VideoPlayer 组件。",
                        MessageType.Info,
                        true
                    ));

                    // 检查视频源
                    if (videoPlayer.clip != null)
                    {
                        validationResults.Add(new ValidationResult(
                            "脚本组件",
                            $"VideoPlayer 已设置视频：{videoPlayer.clip.name}",
                            MessageType.Info,
                            true
                        ));
                    }
                    else
                    {
                        validationResults.Add(new ValidationResult(
                            "脚本组件",
                            "VideoPlayer 未设置视频片段！",
                            MessageType.Warning,
                            false
                        ));
                    }

                    // 检查 Render Mode
                    if (videoPlayer.renderMode == VideoRenderMode.RenderTexture)
                    {
                        if (videoPlayer.targetTexture != null)
                        {
                            validationResults.Add(new ValidationResult(
                                "脚本组件",
                                "VideoPlayer 已设置 Render Texture。",
                                MessageType.Info,
                                true
                            ));
                        }
                        else
                        {
                            validationResults.Add(new ValidationResult(
                                "脚本组件",
                                "VideoPlayer Render Mode 是 RenderTexture，但未设置 Target Texture！",
                                MessageType.Error,
                                false
                            ));
                        }
                    }
                    else if (videoPlayer.renderMode == VideoRenderMode.CameraNearPlane)
                    {
                        validationResults.Add(new ValidationResult(
                            "脚本组件",
                            "VideoPlayer 使用 Camera Near Plane 模式（可选方式）。",
                            MessageType.Info,
                            true
                        ));
                    }

                    // 检查设置
                    if (videoPlayer.playOnAwake)
                    {
                        validationResults.Add(new ValidationResult(
                            "脚本组件",
                            "VideoPlayer 的 Play On Awake 已启用，但应该禁用！脚本会控制播放。",
                            MessageType.Warning,
                            false
                        ));
                    }

                    if (videoPlayer.isLooping)
                    {
                        validationResults.Add(new ValidationResult(
                            "脚本组件",
                            "VideoPlayer 的 Loop 已启用，但应该禁用！视频会一直循环。",
                            MessageType.Warning,
                            false
                        ));
                    }
                }
                else
                {
                    validationResults.Add(new ValidationResult(
                        "脚本组件",
                        "LogoVideoPlayer 对象缺少 VideoPlayer 组件！",
                        MessageType.Error,
                        false
                    ));
                }

                // 使用反射检查脚本参数
                var so = new SerializedObject(logoPlayer);
                string nextSceneName = so.FindProperty("nextSceneName").stringValue;
                
                if (!string.IsNullOrEmpty(nextSceneName))
                {
                    validationResults.Add(new ValidationResult(
                        "脚本参数",
                        $"下一个场景名称已设置：{nextSceneName}",
                        MessageType.Info,
                        true
                    ));
                }
                else
                {
                    validationResults.Add(new ValidationResult(
                        "脚本参数",
                        "下一个场景名称未设置！",
                        MessageType.Error,
                        false
                    ));
                }

                var fadeCanvasGroup = so.FindProperty("fadeCanvasGroup").objectReferenceValue as CanvasGroup;
                if (fadeCanvasGroup != null)
                {
                    validationResults.Add(new ValidationResult(
                        "脚本参数",
                        "Fade Canvas Group 已设置。",
                        MessageType.Info,
                        true
                    ));
                }
                else
                {
                    validationResults.Add(new ValidationResult(
                        "脚本参数",
                        "Fade Canvas Group 未设置！淡入淡出效果将不工作。",
                        MessageType.Warning,
                        false
                    ));
                }
            }
            else
            {
                validationResults.Add(new ValidationResult(
                    "脚本组件",
                    "未找到 LogoVideoPlayer 脚本！",
                    MessageType.Error,
                    false
                ));
            }
        }
    }
}

