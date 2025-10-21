using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.IO;

namespace TN.Editor.ImageTools
{
    /// <summary>
    /// 圆角工具 - 用于快速为图片添加圆角效果
    /// </summary>
    public class RoundedCornerTool : EditorWindow
    {
        #region 字段

        private Texture2D selectedTexture;
        private float cornerRadius = 50f;
        private float edgeSmoothing = 1f;
        private bool autoDetectResolution = true;
        private Vector2 manualResolution = new Vector2(512, 512);
        
        // 四角独立控制
        private bool useIndividualCorners = false;
        private float topLeftRadius = 50f;
        private float topRightRadius = 50f;
        private float bottomLeftRadius = 50f;
        private float bottomRightRadius = 50f;
        
        private Material previewMaterial;
        private Shader roundedCornerShader;
        
        private enum ProcessMode
        {
            ApplyMaterial,      // 应用Material（实时、可调整）
            GenerateNewTexture  // 生成新纹理（永久修改）
        }
        
        private ProcessMode processMode = ProcessMode.ApplyMaterial;
        
        private Vector2 scrollPos;

        #endregion

        #region Unity生命周期

        [MenuItem("自制工具/图片效果/圆角工具")]
        public static void ShowWindow()
        {
            var window = GetWindow<RoundedCornerTool>("圆角工具");
            window.minSize = new Vector2(400, 500);
            window.Show();
        }

        private void OnEnable()
        {
            LoadShader();
        }

        private void OnDisable()
        {
            if (previewMaterial != null)
            {
                DestroyImmediate(previewMaterial);
            }
        }

        #endregion

        #region GUI绘制

        private void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            
            DrawHeader();
            EditorGUILayout.Space(10);
            
            DrawTextureSelection();
            EditorGUILayout.Space(10);
            
            DrawSettings();
            EditorGUILayout.Space(10);
            
            DrawPreview();
            EditorGUILayout.Space(10);
            
            DrawActions();
            
            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter
            };
            
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Unity 圆角工具", titleStyle);
            EditorGUILayout.Space(3);
            
            EditorGUILayout.HelpBox(
                "此工具可以快速为图片素材添加圆角效果。\n" +
                "• 应用Material模式：不修改原始资源，实时可调\n" +
                "• 生成新纹理模式：创建新的圆角纹理资源",
                MessageType.Info);
        }

        private void DrawTextureSelection()
        {
            EditorGUILayout.LabelField("1. 选择图片", EditorStyles.boldLabel);
            
            var newTexture = (Texture2D)EditorGUILayout.ObjectField(
                "目标图片",
                selectedTexture,
                typeof(Texture2D),
                false);

            if (newTexture != selectedTexture)
            {
                selectedTexture = newTexture;
                if (autoDetectResolution && selectedTexture != null)
                {
                    manualResolution = new Vector2(selectedTexture.width, selectedTexture.height);
                }
            }

            // 快速选择当前选中的对象
            if (GUILayout.Button("使用当前选中的图片"))
            {
                if (Selection.activeObject is Texture2D tex)
                {
                    selectedTexture = tex;
                    if (autoDetectResolution)
                    {
                        manualResolution = new Vector2(tex.width, tex.height);
                    }
                }
                else
                {
                    EditorUtility.DisplayDialog("错误", "请在Project窗口中选择一个Texture2D资源", "确定");
                }
            }
        }

        private void DrawSettings()
        {
            EditorGUILayout.LabelField("2. 圆角设置", EditorStyles.boldLabel);
            
            // 四角独立控制开关
            useIndividualCorners = EditorGUILayout.Toggle("四角独立控制", useIndividualCorners);
            
            EditorGUILayout.Space(3);
            
            if (!useIndividualCorners)
            {
                // 统一圆角模式
                cornerRadius = EditorGUILayout.Slider("圆角半径", cornerRadius, 0f, 500f);
            }
            else
            {
                // 四角独立模式
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField("各角半径", EditorStyles.miniBoldLabel);
                
                topLeftRadius = EditorGUILayout.Slider("↖ 左上角", topLeftRadius, 0f, 500f);
                topRightRadius = EditorGUILayout.Slider("↗ 右上角", topRightRadius, 0f, 500f);
                bottomLeftRadius = EditorGUILayout.Slider("↙ 左下角", bottomLeftRadius, 0f, 500f);
                bottomRightRadius = EditorGUILayout.Slider("↘ 右下角", bottomRightRadius, 0f, 500f);
                
                EditorGUILayout.Space(5);
                
                // 快捷按钮
                EditorGUILayout.LabelField("快捷设置", EditorStyles.miniBoldLabel);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("🔘 全部圆角", GUILayout.Height(22)))
                {
                    topLeftRadius = topRightRadius = bottomLeftRadius = bottomRightRadius = 50f;
                }
                if (GUILayout.Button("⬆️ 仅上方", GUILayout.Height(22)))
                {
                    topLeftRadius = topRightRadius = 50f;
                    bottomLeftRadius = bottomRightRadius = 0f;
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("⬇️ 仅下方", GUILayout.Height(22)))
                {
                    topLeftRadius = topRightRadius = 0f;
                    bottomLeftRadius = bottomRightRadius = 50f;
                }
                if (GUILayout.Button("⬅️ 仅左侧", GUILayout.Height(22)))
                {
                    topLeftRadius = bottomLeftRadius = 50f;
                    topRightRadius = bottomRightRadius = 0f;
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("➡️ 仅右侧", GUILayout.Height(22)))
                {
                    topRightRadius = bottomRightRadius = 50f;
                    topLeftRadius = bottomLeftRadius = 0f;
                }
                if (GUILayout.Button("⭕ 全部直角", GUILayout.Height(22)))
                {
                    topLeftRadius = topRightRadius = bottomLeftRadius = bottomRightRadius = 0f;
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.EndVertical();
            }
            
            edgeSmoothing = EditorGUILayout.Slider("边缘平滑度", edgeSmoothing, 0f, 10f);
            
            EditorGUILayout.Space(5);
            autoDetectResolution = EditorGUILayout.Toggle("自动检测分辨率", autoDetectResolution);
            
            EditorGUI.BeginDisabledGroup(autoDetectResolution);
            manualResolution = EditorGUILayout.Vector2Field("手动分辨率", manualResolution);
            EditorGUI.EndDisabledGroup();
            
            if (selectedTexture != null && autoDetectResolution)
            {
                EditorGUILayout.HelpBox(
                    $"当前分辨率: {selectedTexture.width} x {selectedTexture.height}",
                    MessageType.None);
            }
        }

        private void DrawPreview()
        {
            EditorGUILayout.LabelField("3. 预览", EditorStyles.boldLabel);
            
            if (selectedTexture == null)
            {
                EditorGUILayout.HelpBox("请先选择一个图片", MessageType.Warning);
                return;
            }

            if (roundedCornerShader == null)
            {
                EditorGUILayout.HelpBox("找不到圆角Shader！", MessageType.Error);
                return;
            }

            // 创建预览Material
            if (previewMaterial == null)
            {
                previewMaterial = new Material(roundedCornerShader);
            }

            // 更新Material参数
            UpdateMaterialProperties(previewMaterial);

            // 绘制预览
            Rect previewRect = GUILayoutUtility.GetRect(300, 300, GUILayout.ExpandWidth(true));
            
            // 计算预览尺寸（保持宽高比）
            float aspectRatio = (float)selectedTexture.width / selectedTexture.height;
            float previewWidth = previewRect.width;
            float previewHeight = previewWidth / aspectRatio;
            
            if (previewHeight > previewRect.height)
            {
                previewHeight = previewRect.height;
                previewWidth = previewHeight * aspectRatio;
            }

            Rect imageRect = new Rect(
                previewRect.x + (previewRect.width - previewWidth) / 2,
                previewRect.y + (previewRect.height - previewHeight) / 2,
                previewWidth,
                previewHeight);

            EditorGUI.DrawPreviewTexture(imageRect, selectedTexture, previewMaterial);
        }

        private void DrawActions()
        {
            EditorGUILayout.LabelField("4. 处理模式", EditorStyles.boldLabel);
            
            processMode = (ProcessMode)EditorGUILayout.EnumPopup("模式", processMode);

            EditorGUILayout.Space(5);

            EditorGUI.BeginDisabledGroup(selectedTexture == null || roundedCornerShader == null);
            
            if (processMode == ProcessMode.ApplyMaterial)
            {
                if (GUILayout.Button("创建圆角Material", GUILayout.Height(30)))
                {
                    CreateRoundedMaterial();
                }
                
                EditorGUILayout.HelpBox(
                    "将创建一个Material资源，可以应用到Image或SpriteRenderer上",
                    MessageType.Info);
            }
            else
            {
                if (GUILayout.Button("生成圆角纹理", GUILayout.Height(30)))
                {
                    GenerateRoundedTexture();
                }
                
                EditorGUILayout.HelpBox(
                    "将生成一个新的PNG纹理文件（需要时间渲染）",
                    MessageType.Info);
            }
            
            EditorGUI.EndDisabledGroup();
        }

        #endregion

        #region 核心功能

        private void LoadShader()
        {
            roundedCornerShader = Shader.Find("Custom/RoundedCorner");
            
            if (roundedCornerShader == null)
            {
                Debug.LogError("找不到 Custom/RoundedCorner Shader！请确保已创建该Shader。");
            }
        }

        private void UpdateMaterialProperties(Material mat)
        {
            if (mat == null || selectedTexture == null) return;

            mat.mainTexture = selectedTexture;
            mat.SetFloat("_CornerSmoothing", edgeSmoothing);
            
            // 默认忽略Image颜色（推荐设置）
            mat.SetFloat("_IgnoreImageColor", 1f);
            
            // 设置圆角模式
            if (useIndividualCorners)
            {
                mat.SetFloat("_UseIndividualCorners", 1f);
                mat.SetVector("_CornerRadii", new Vector4(topLeftRadius, topRightRadius, bottomLeftRadius, bottomRightRadius));
            }
            else
            {
                mat.SetFloat("_UseIndividualCorners", 0f);
                mat.SetFloat("_CornerRadius", cornerRadius);
            }
            
            Vector2 resolution = autoDetectResolution 
                ? new Vector2(selectedTexture.width, selectedTexture.height)
                : manualResolution;
            
            mat.SetVector("_Resolution", new Vector4(resolution.x, resolution.y, 0, 0));
        }

        private void CreateRoundedMaterial()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "保存圆角Material",
                $"{selectedTexture.name}_Rounded",
                "mat",
                "选择保存位置");

            if (string.IsNullOrEmpty(path))
                return;

            Material newMaterial = new Material(roundedCornerShader);
            UpdateMaterialProperties(newMaterial);

            AssetDatabase.CreateAsset(newMaterial, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("成功", $"已创建圆角Material:\n{path}", "确定");
            EditorGUIUtility.PingObject(newMaterial);
            Selection.activeObject = newMaterial;
        }

        private void GenerateRoundedTexture()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "保存圆角纹理",
                $"{selectedTexture.name}_Rounded",
                "png",
                "选择保存位置");

            if (string.IsNullOrEmpty(path))
                return;

            // 创建临时Material
            Material tempMat = new Material(roundedCornerShader);
            UpdateMaterialProperties(tempMat);

            // 创建RenderTexture
            Vector2 resolution = autoDetectResolution 
                ? new Vector2(selectedTexture.width, selectedTexture.height)
                : manualResolution;

            RenderTexture rt = RenderTexture.GetTemporary(
                (int)resolution.x,
                (int)resolution.y,
                0,
                RenderTextureFormat.ARGB32);

            // 渲染到RenderTexture
            Graphics.Blit(selectedTexture, rt, tempMat);

            // 读取像素
            RenderTexture.active = rt;
            Texture2D newTexture = new Texture2D((int)resolution.x, (int)resolution.y, TextureFormat.ARGB32, false);
            newTexture.ReadPixels(new Rect(0, 0, resolution.x, resolution.y), 0, 0);
            newTexture.Apply();

            // 保存为PNG
            byte[] bytes = newTexture.EncodeToPNG();
            File.WriteAllBytes(path, bytes);

            // 清理
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(rt);
            DestroyImmediate(tempMat);
            DestroyImmediate(newTexture);

            AssetDatabase.Refresh();

            // 设置纹理导入设置
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }

            EditorUtility.DisplayDialog("成功", $"已生成圆角纹理:\n{path}", "确定");
            
            Texture2D savedTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            EditorGUIUtility.PingObject(savedTexture);
            Selection.activeObject = savedTexture;
        }

        #endregion
    }

    #region 快捷菜单扩展

    /// <summary>
    /// 为选中的纹理快速创建圆角Material
    /// </summary>
    public static class RoundedCornerContextMenu
    {
        [MenuItem("Assets/创建圆角Material", true)]
        private static bool ValidateCreateRoundedMaterial()
        {
            return Selection.activeObject is Texture2D;
        }

        [MenuItem("Assets/创建圆角Material")]
        private static void CreateRoundedMaterial()
        {
            Texture2D texture = Selection.activeObject as Texture2D;
            if (texture == null) return;

            Shader shader = Shader.Find("Custom/RoundedCorner");
            if (shader == null)
            {
                EditorUtility.DisplayDialog("错误", "找不到 Custom/RoundedCorner Shader", "确定");
                return;
            }

            string texturePath = AssetDatabase.GetAssetPath(texture);
            string directory = Path.GetDirectoryName(texturePath);
            string fileName = Path.GetFileNameWithoutExtension(texturePath);
            string savePath = $"{directory}/{fileName}_Rounded.mat";
            
            savePath = AssetDatabase.GenerateUniqueAssetPath(savePath);

            Material material = new Material(shader);
            material.mainTexture = texture;
            material.SetFloat("_IgnoreImageColor", 1f);  // 默认忽略Image颜色
            material.SetFloat("_UseIndividualCorners", 0f);
            material.SetFloat("_CornerRadius", 50f);
            material.SetFloat("_CornerSmoothing", 1f);
            material.SetVector("_Resolution", new Vector4(texture.width, texture.height, 0, 0));

            AssetDatabase.CreateAsset(material, savePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorGUIUtility.PingObject(material);
            Selection.activeObject = material;
        }
    }

    #endregion
}

