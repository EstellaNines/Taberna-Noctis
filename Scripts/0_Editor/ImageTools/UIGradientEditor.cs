using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

namespace TN.UI
{
    /// <summary>
    /// UIGradient 自定义Inspector编辑器
    /// </summary>
    [CustomEditor(typeof(UIGradient))]
    public class UIGradientEditor : UnityEditor.Editor
    {
        private SerializedProperty gradientType;
        private SerializedProperty startColor;
        private SerializedProperty endColor;
        private SerializedProperty angle;
        private SerializedProperty offset;
        private SerializedProperty center;
        private SerializedProperty radius;
        private SerializedProperty blendMode;
        private SerializedProperty useShader;
        private SerializedProperty realtimeUpdate;

        private void OnEnable()
        {
            gradientType = serializedObject.FindProperty("gradientType");
            startColor = serializedObject.FindProperty("startColor");
            endColor = serializedObject.FindProperty("endColor");
            angle = serializedObject.FindProperty("angle");
            offset = serializedObject.FindProperty("offset");
            center = serializedObject.FindProperty("center");
            radius = serializedObject.FindProperty("radius");
            blendMode = serializedObject.FindProperty("blendMode");
            useShader = serializedObject.FindProperty("useShader");
            realtimeUpdate = serializedObject.FindProperty("realtimeUpdate");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var comp = (UIGradient)target;

            // 渐变类型
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("渐变类型", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(gradientType, new GUIContent("渐变模式"));

            // 渐变颜色
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("渐变颜色", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.PropertyField(startColor, new GUIContent("起始颜色"));
            EditorGUILayout.PropertyField(endColor, new GUIContent("结束颜色"));
            
            // 颜色预设按钮
            EditorGUILayout.Space(3);
            EditorGUILayout.LabelField("颜色预设", EditorStyles.miniBoldLabel);
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("白→黑", GUILayout.Height(22)))
            {
                startColor.colorValue = Color.white;
                endColor.colorValue = Color.black;
            }
            if (GUILayout.Button("黑→白", GUILayout.Height(22)))
            {
                startColor.colorValue = Color.black;
                endColor.colorValue = Color.white;
            }
            if (GUILayout.Button("红→蓝", GUILayout.Height(22)))
            {
                startColor.colorValue = Color.red;
                endColor.colorValue = Color.blue;
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("黄→橙", GUILayout.Height(22)))
            {
                startColor.colorValue = Color.yellow;
                endColor.colorValue = new Color(1f, 0.5f, 0f);
            }
            if (GUILayout.Button("青→绿", GUILayout.Height(22)))
            {
                startColor.colorValue = Color.cyan;
                endColor.colorValue = Color.green;
            }
            if (GUILayout.Button("紫→粉", GUILayout.Height(22)))
            {
                startColor.colorValue = new Color(0.5f, 0f, 1f);
                endColor.colorValue = new Color(1f, 0f, 0.5f);
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();

            // 根据渐变类型显示不同的参数
            EditorGUILayout.Space(5);
            
            UIGradient.GradientType currentType = (UIGradient.GradientType)gradientType.enumValueIndex;

            if (currentType == UIGradient.GradientType.Linear)
            {
                EditorGUILayout.LabelField("线性渐变设置", EditorStyles.boldLabel);
                EditorGUILayout.BeginVertical("box");
                
                EditorGUILayout.Slider(angle, 0f, 360f, new GUIContent("渐变角度"));
                EditorGUILayout.Slider(offset, -1f, 1f, new GUIContent("渐变偏移"));
                
                // 角度快捷按钮
                EditorGUILayout.Space(3);
                EditorGUILayout.LabelField("快捷角度", EditorStyles.miniBoldLabel);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("→ 0°", GUILayout.Height(22)))
                {
                    angle.floatValue = 0f;
                }
                if (GUILayout.Button("↑ 90°", GUILayout.Height(22)))
                {
                    angle.floatValue = 90f;
                }
                if (GUILayout.Button("← 180°", GUILayout.Height(22)))
                {
                    angle.floatValue = 180f;
                }
                if (GUILayout.Button("↓ 270°", GUILayout.Height(22)))
                {
                    angle.floatValue = 270f;
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.EndVertical();
            }
            else if (currentType == UIGradient.GradientType.Radial)
            {
                EditorGUILayout.LabelField("径向渐变设置", EditorStyles.boldLabel);
                EditorGUILayout.BeginVertical("box");
                
                EditorGUILayout.PropertyField(center, new GUIContent("中心点"));
                EditorGUILayout.Slider(radius, 0f, 2f, new GUIContent("半径"));
                
                // 中心点快捷按钮
                EditorGUILayout.Space(3);
                EditorGUILayout.LabelField("快捷中心点", EditorStyles.miniBoldLabel);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("居中", GUILayout.Height(22)))
                {
                    center.vector2Value = new Vector2(0.5f, 0.5f);
                }
                if (GUILayout.Button("左上", GUILayout.Height(22)))
                {
                    center.vector2Value = new Vector2(0f, 1f);
                }
                if (GUILayout.Button("右上", GUILayout.Height(22)))
                {
                    center.vector2Value = new Vector2(1f, 1f);
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("左下", GUILayout.Height(22)))
                {
                    center.vector2Value = new Vector2(0f, 0f);
                }
                if (GUILayout.Button("右下", GUILayout.Height(22)))
                {
                    center.vector2Value = new Vector2(1f, 0f);
                }
                if (GUILayout.Button("重置", GUILayout.Height(22)))
                {
                    center.vector2Value = new Vector2(0.5f, 0.5f);
                    radius.floatValue = 1f;
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.EndVertical();
            }
            else if (currentType == UIGradient.GradientType.Corner)
            {
                EditorGUILayout.LabelField("四角渐变设置", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(
                    "四角渐变从四个角向中心渐变，无需额外参数设置。",
                    MessageType.Info);
            }

            // 混合模式
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("混合模式", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(blendMode, new GUIContent("渐变混合"));
            
            // 混合模式说明
            UIGradient.BlendMode currentBlendMode = (UIGradient.BlendMode)blendMode.enumValueIndex;
            string blendModeDesc = "";
            switch (currentBlendMode)
            {
                case UIGradient.BlendMode.Replace:
                    blendModeDesc = "替换：渐变色替换纹理颜色";
                    break;
                case UIGradient.BlendMode.Multiply:
                    blendModeDesc = "相乘：渐变色与纹理颜色相乘";
                    break;
                case UIGradient.BlendMode.Overlay:
                    blendModeDesc = "叠加：渐变色叠加到纹理上（保持纹理细节）";
                    break;
            }
            EditorGUILayout.HelpBox(blendModeDesc, MessageType.None);

            // 高级设置
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("高级设置", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(useShader, new GUIContent("使用Shader渲染", "启用可获得更好的性能和效果"));
            EditorGUILayout.PropertyField(realtimeUpdate, new GUIContent("实时更新", "在编辑器中实时更新渐变效果"));

            serializedObject.ApplyModifiedProperties();

            // 手动刷新按钮
            EditorGUILayout.Space(10);
            if (GUILayout.Button("�9�4 刷新渐变效果", GUILayout.Height(30)))
            {
                comp.RefreshGradient();
                EditorUtility.SetDirty(comp);
                Debug.Log("[UIGradient] 已刷新渐变效果");
            }

            // 在编辑器模式下实时更新
            if (!Application.isPlaying && GUI.changed)
            {
                EditorUtility.SetDirty(comp);
            }
        }
    }
}

