using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

namespace TN.UI
{
    /// <summary>
    /// RoundedCornerGradient 自定义Inspector编辑器
    /// </summary>
    [CustomEditor(typeof(RoundedCornerGradient))]
    public class RoundedCornerGradientEditor : UnityEditor.Editor
    {
        // 圆角属性
        private SerializedProperty useIndividualCorners;
        private SerializedProperty cornerRadius;
        private SerializedProperty topLeftRadius;
        private SerializedProperty topRightRadius;
        private SerializedProperty bottomLeftRadius;
        private SerializedProperty bottomRightRadius;
        private SerializedProperty edgeSmoothing;

        // 渐变属性
        private SerializedProperty useGradient;
        private SerializedProperty gradientType;
        private SerializedProperty startColor;
        private SerializedProperty endColor;
        private SerializedProperty gradientAngle;
        private SerializedProperty gradientOffset;
        private SerializedProperty gradientCenter;
        private SerializedProperty gradientRadius;
        private SerializedProperty blendMode;

        // 高级属性
        private SerializedProperty ignoreImageColor;
        private SerializedProperty useRectSize;
        private SerializedProperty manualResolution;

        private void OnEnable()
        {
            // 圆角
            useIndividualCorners = serializedObject.FindProperty("useIndividualCorners");
            cornerRadius = serializedObject.FindProperty("cornerRadius");
            topLeftRadius = serializedObject.FindProperty("topLeftRadius");
            topRightRadius = serializedObject.FindProperty("topRightRadius");
            bottomLeftRadius = serializedObject.FindProperty("bottomLeftRadius");
            bottomRightRadius = serializedObject.FindProperty("bottomRightRadius");
            edgeSmoothing = serializedObject.FindProperty("edgeSmoothing");

            // 渐变
            useGradient = serializedObject.FindProperty("useGradient");
            gradientType = serializedObject.FindProperty("gradientType");
            startColor = serializedObject.FindProperty("startColor");
            endColor = serializedObject.FindProperty("endColor");
            gradientAngle = serializedObject.FindProperty("gradientAngle");
            gradientOffset = serializedObject.FindProperty("gradientOffset");
            gradientCenter = serializedObject.FindProperty("gradientCenter");
            gradientRadius = serializedObject.FindProperty("gradientRadius");
            blendMode = serializedObject.FindProperty("blendMode");

            // 高级
            ignoreImageColor = serializedObject.FindProperty("ignoreImageColor");
            useRectSize = serializedObject.FindProperty("useRectSize");
            manualResolution = serializedObject.FindProperty("manualResolution");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var comp = (RoundedCornerGradient)target;

            DrawComponentHeader();

            // === 圆角设置 ===
            DrawRoundedCornerSettings();

            EditorGUILayout.Space(10);

            // === 渐变设置 ===
            DrawGradientSettings();

            EditorGUILayout.Space(10);

            // === 高级设置 ===
            DrawAdvancedSettings();

            // === 操作按钮 ===
            EditorGUILayout.Space(10);
            if (GUILayout.Button("🔄 强制刷新效果", GUILayout.Height(30)))
            {
                comp.RefreshMaterial();
                EditorUtility.SetDirty(comp);
                Debug.Log("[RoundedCornerGradient] 已刷新效果");
            }

            serializedObject.ApplyModifiedProperties();

            if (!Application.isPlaying && GUI.changed)
            {
                EditorUtility.SetDirty(comp);
            }
        }

        private void DrawComponentHeader()
        {
            EditorGUILayout.Space(5);
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13,
                alignment = TextAnchor.MiddleLeft
            };
            EditorGUILayout.LabelField("🎨 圆角渐变组合组件", titleStyle);
            EditorGUILayout.HelpBox(
                "此组件同时支持圆角和渐变功能，避免Material冲突。",
                MessageType.Info);
        }

        private void DrawRoundedCornerSettings()
        {
            EditorGUILayout.LabelField("圆角设置", EditorStyles.boldLabel);
            
            EditorGUILayout.PropertyField(useIndividualCorners, new GUIContent("启用四角独立控制"));
            
            EditorGUILayout.Space(3);

            if (!useIndividualCorners.boolValue)
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.Slider(cornerRadius, 0f, 500f, new GUIContent("圆角半径"));
                
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
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField("各角半径", EditorStyles.miniBoldLabel);
                
                EditorGUILayout.Slider(topLeftRadius, 0f, 500f, new GUIContent("↖ 左上角"));
                EditorGUILayout.Slider(topRightRadius, 0f, 500f, new GUIContent("↗ 右上角"));
                EditorGUILayout.Slider(bottomLeftRadius, 0f, 500f, new GUIContent("↙ 左下角"));
                EditorGUILayout.Slider(bottomRightRadius, 0f, 500f, new GUIContent("↘ 右下角"));
                
                EditorGUILayout.Space(3);
                EditorGUILayout.LabelField("快捷设置", EditorStyles.miniBoldLabel);
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

            EditorGUILayout.Slider(edgeSmoothing, 0f, 10f, new GUIContent("边缘平滑度"));
        }

        private void DrawGradientSettings()
        {
            EditorGUILayout.LabelField("渐变设置", EditorStyles.boldLabel);
            
            EditorGUILayout.PropertyField(useGradient, new GUIContent("启用渐变效果"));

            if (!useGradient.boolValue)
            {
                EditorGUILayout.HelpBox("渐变功能已禁用，只使用圆角效果。", MessageType.Info);
                return;
            }

            EditorGUILayout.Space(3);
            
            // 渐变类型
            EditorGUILayout.PropertyField(gradientType, new GUIContent("渐变类型"));
            
            // 渐变颜色
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.PropertyField(startColor, new GUIContent("起始颜色"));
            EditorGUILayout.PropertyField(endColor, new GUIContent("结束颜色"));
            
            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField("颜色预设", EditorStyles.miniBoldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("白→黑", GUILayout.Height(20)))
            {
                startColor.colorValue = Color.white;
                endColor.colorValue = Color.black;
            }
            if (GUILayout.Button("红→蓝", GUILayout.Height(20)))
            {
                startColor.colorValue = Color.red;
                endColor.colorValue = Color.blue;
            }
            if (GUILayout.Button("黄→橙", GUILayout.Height(20)))
            {
                startColor.colorValue = Color.yellow;
                endColor.colorValue = new Color(1f, 0.5f, 0f);
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            // 根据类型显示参数
            RoundedCornerGradient.GradientType currentType = 
                (RoundedCornerGradient.GradientType)gradientType.enumValueIndex;

            EditorGUILayout.Space(3);

            if (currentType == RoundedCornerGradient.GradientType.Linear)
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField("线性渐变参数", EditorStyles.miniBoldLabel);
                EditorGUILayout.Slider(gradientAngle, 0f, 360f, new GUIContent("渐变角度"));
                EditorGUILayout.Slider(gradientOffset, -1f, 1f, new GUIContent("渐变偏移"));
                
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("↑ 90°", GUILayout.Height(20)))
                    gradientAngle.floatValue = 90f;
                if (GUILayout.Button("→ 0°", GUILayout.Height(20)))
                    gradientAngle.floatValue = 0f;
                if (GUILayout.Button("↓ 270°", GUILayout.Height(20)))
                    gradientAngle.floatValue = 270f;
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
            }
            else if (currentType == RoundedCornerGradient.GradientType.Radial)
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField("径向渐变参数", EditorStyles.miniBoldLabel);
                EditorGUILayout.PropertyField(gradientCenter, new GUIContent("中心点"));
                EditorGUILayout.Slider(gradientRadius, 0f, 2f, new GUIContent("半径"));
                
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("居中", GUILayout.Height(20)))
                    gradientCenter.vector2Value = new Vector2(0.5f, 0.5f);
                if (GUILayout.Button("重置", GUILayout.Height(20)))
                {
                    gradientCenter.vector2Value = new Vector2(0.5f, 0.5f);
                    gradientRadius.floatValue = 1f;
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.PropertyField(blendMode, new GUIContent("混合模式"));
        }

        private void DrawAdvancedSettings()
        {
            EditorGUILayout.LabelField("高级设置", EditorStyles.boldLabel);
            
            EditorGUILayout.PropertyField(ignoreImageColor, 
                new GUIContent("忽略Image颜色", "让Shader忽略Image颜色，确保效果一致"));
            
            EditorGUILayout.PropertyField(useRectSize, 
                new GUIContent("自动使用RectTransform尺寸"));
            
            EditorGUI.BeginDisabledGroup(useRectSize.boolValue);
            EditorGUILayout.PropertyField(manualResolution, new GUIContent("手动分辨率"));
            EditorGUI.EndDisabledGroup();
        }
    }
}

