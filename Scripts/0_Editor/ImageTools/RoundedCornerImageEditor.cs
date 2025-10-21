using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

namespace TN.UI
{
    /// <summary>
    /// RoundedCornerImage 自定义Inspector编辑器
    /// </summary>
    [CustomEditor(typeof(RoundedCornerImage))]
    public class RoundedCornerImageEditor : UnityEditor.Editor
    {
        private SerializedProperty useIndividualCorners;
        private SerializedProperty cornerRadius;
        private SerializedProperty topLeftRadius;
        private SerializedProperty topRightRadius;
        private SerializedProperty bottomLeftRadius;
        private SerializedProperty bottomRightRadius;
        private SerializedProperty edgeSmoothing;
        private SerializedProperty ignoreImageColor;
        private SerializedProperty autoSetWhiteColor;
        private SerializedProperty useRectSize;
        private SerializedProperty manualResolution;

        private void OnEnable()
        {
            useIndividualCorners = serializedObject.FindProperty("useIndividualCorners");
            cornerRadius = serializedObject.FindProperty("cornerRadius");
            topLeftRadius = serializedObject.FindProperty("topLeftRadius");
            topRightRadius = serializedObject.FindProperty("topRightRadius");
            bottomLeftRadius = serializedObject.FindProperty("bottomLeftRadius");
            bottomRightRadius = serializedObject.FindProperty("bottomRightRadius");
            edgeSmoothing = serializedObject.FindProperty("edgeSmoothing");
            ignoreImageColor = serializedObject.FindProperty("ignoreImageColor");
            autoSetWhiteColor = serializedObject.FindProperty("autoSetWhiteColor");
            useRectSize = serializedObject.FindProperty("useRectSize");
            manualResolution = serializedObject.FindProperty("manualResolution");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // 圆角设置
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("圆角设置", EditorStyles.boldLabel);
            
            EditorGUILayout.PropertyField(useIndividualCorners, new GUIContent("启用四角独立控制"));
            
            EditorGUILayout.Space(3);

            if (!useIndividualCorners.boolValue)
            {
                // 统一圆角模式
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.Slider(cornerRadius, 0f, 500f, new GUIContent("圆角半径"));
                
                // 当统一圆角改变时，同步更新四个独立角的值
                if (EditorGUI.EndChangeCheck())
                {
                    float newRadius = cornerRadius.floatValue;
                    topLeftRadius.floatValue = newRadius;
                    topRightRadius.floatValue = newRadius;
                    bottomLeftRadius.floatValue = newRadius;
                    bottomRightRadius.floatValue = newRadius;
                }
            }
            else
            {
                // 四角独立模式
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField("各角半径", EditorStyles.miniBoldLabel);
                
                EditorGUILayout.Slider(topLeftRadius, 0f, 500f, new GUIContent("↖ 左上角"));
                EditorGUILayout.Slider(topRightRadius, 0f, 500f, new GUIContent("↗ 右上角"));
                EditorGUILayout.Slider(bottomLeftRadius, 0f, 500f, new GUIContent("↙ 左下角"));
                EditorGUILayout.Slider(bottomRightRadius, 0f, 500f, new GUIContent("↘ 右下角"));
                
                // 快捷按钮
                EditorGUILayout.Space(3);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("全部圆角", GUILayout.Height(22)))
                {
                    topLeftRadius.floatValue = 50f;
                    topRightRadius.floatValue = 50f;
                    bottomLeftRadius.floatValue = 50f;
                    bottomRightRadius.floatValue = 50f;
                }
                if (GUILayout.Button("仅上方", GUILayout.Height(22)))
                {
                    topLeftRadius.floatValue = 50f;
                    topRightRadius.floatValue = 50f;
                    bottomLeftRadius.floatValue = 0f;
                    bottomRightRadius.floatValue = 0f;
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("仅下方", GUILayout.Height(22)))
                {
                    topLeftRadius.floatValue = 0f;
                    topRightRadius.floatValue = 0f;
                    bottomLeftRadius.floatValue = 50f;
                    bottomRightRadius.floatValue = 50f;
                }
                if (GUILayout.Button("仅左侧", GUILayout.Height(22)))
                {
                    topLeftRadius.floatValue = 50f;
                    topRightRadius.floatValue = 0f;
                    bottomLeftRadius.floatValue = 50f;
                    bottomRightRadius.floatValue = 0f;
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("仅右侧", GUILayout.Height(22)))
                {
                    topLeftRadius.floatValue = 0f;
                    topRightRadius.floatValue = 50f;
                    bottomLeftRadius.floatValue = 0f;
                    bottomRightRadius.floatValue = 50f;
                }
                if (GUILayout.Button("全部直角", GUILayout.Height(22)))
                {
                    topLeftRadius.floatValue = 0f;
                    topRightRadius.floatValue = 0f;
                    bottomLeftRadius.floatValue = 0f;
                    bottomRightRadius.floatValue = 0f;
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.EndVertical();
            }

            // 其他设置
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("其他设置", EditorStyles.boldLabel);
            EditorGUILayout.Slider(edgeSmoothing, 0f, 10f, new GUIContent("边缘平滑度"));

            // 高级设置
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("高级设置", EditorStyles.boldLabel);
            
            var comp = (RoundedCornerImage)target;
            
            // 忽略Image颜色选项
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(ignoreImageColor, new GUIContent("忽略Image颜色", "让Shader忽略Image组件的颜色设置，只使用纹理原始颜色（推荐）"));
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                comp.RefreshMaterial();
            }
            
            // 提示信息
            if (ignoreImageColor.boolValue)
            {
                EditorGUILayout.HelpBox(
                    "? 已启用：圆角效果将忽略Image颜色，确保显示一致",
                    MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "? 已禁用：Image颜色会影响整个图片（包括圆角区域）",
                    MessageType.Warning);
            }
            
            EditorGUILayout.Space(3);
            
            EditorGUILayout.PropertyField(autoSetWhiteColor, new GUIContent("自动设置白色", "自动将Image颜色设置为白色（仅在未忽略颜色时有用）"));
            
            // 显示当前Image颜色警告
            var img = comp.GetComponent<Image>();
            if (img != null && img.color != Color.white && !autoSetWhiteColor.boolValue && !ignoreImageColor.boolValue)
            {
                EditorGUILayout.HelpBox(
                    $"警告：Image颜色不是白色 ({img.color})，这会影响图片显示。\n建议启用'忽略Image颜色'或'自动设置白色'。",
                    MessageType.Warning);
            }
            
            EditorGUILayout.PropertyField(useRectSize, new GUIContent("自动使用RectTransform尺寸"));
            
            EditorGUI.BeginDisabledGroup(useRectSize.boolValue);
            EditorGUILayout.PropertyField(manualResolution, new GUIContent("手动分辨率"));
            EditorGUI.EndDisabledGroup();

            // 显示当前尺寸信息
            var rt = comp.transform as RectTransform;
            if (rt != null)
            {
                if (useRectSize.boolValue)
                {
                    EditorGUILayout.HelpBox(
                        $"当前RectTransform尺寸: {rt.rect.width:F0} x {rt.rect.height:F0}",
                        MessageType.Info);
                }
                else
                {
                    EditorGUILayout.HelpBox(
                        $"使用手动分辨率: {manualResolution.vector2Value.x:F0} x {manualResolution.vector2Value.y:F0}",
                        MessageType.Info);
                }
            }

            serializedObject.ApplyModifiedProperties();

            // 手动刷新按钮
            EditorGUILayout.Space(5);
            if (GUILayout.Button("? 强制刷新材质", GUILayout.Height(30)))
            {
                comp.RefreshMaterial();
                EditorUtility.SetDirty(comp);
                Debug.Log("[RoundedCornerImage] 已强制刷新材质");
            }

            // 在编辑器模式下实时更新
            if (!Application.isPlaying && GUI.changed)
            {
                EditorUtility.SetDirty(comp);
            }
        }
    }
}

