using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

namespace TN.UI
{
    /// <summary>
    /// DiscreteSlider 自定义Inspector编辑器
    /// </summary>
    [CustomEditor(typeof(DiscreteSlider))]
    public class DiscreteSliderEditor : UnityEditor.Editor
    {
        private SerializedProperty sliderBackground;
        private SerializedProperty sliderHandle;
        private SerializedProperty minValue;
        private SerializedProperty maxValue;
        private SerializedProperty currentValue;
        private SerializedProperty showTicks;
        private SerializedProperty tickColor;
        private SerializedProperty tickWidth;
        private SerializedProperty tickHeight;
        private SerializedProperty isDraggable;
        private SerializedProperty isClickable;
        private SerializedProperty useSnapAnimation;
        private SerializedProperty snapSpeed;
        private SerializedProperty onValueChanged;

        private void OnEnable()
        {
            sliderBackground = serializedObject.FindProperty("sliderBackground");
            sliderHandle = serializedObject.FindProperty("sliderHandle");
            minValue = serializedObject.FindProperty("minValue");
            maxValue = serializedObject.FindProperty("maxValue");
            currentValue = serializedObject.FindProperty("currentValue");
            showTicks = serializedObject.FindProperty("showTicks");
            tickColor = serializedObject.FindProperty("tickColor");
            tickWidth = serializedObject.FindProperty("tickWidth");
            tickHeight = serializedObject.FindProperty("tickHeight");
            isDraggable = serializedObject.FindProperty("isDraggable");
            isClickable = serializedObject.FindProperty("isClickable");
            useSnapAnimation = serializedObject.FindProperty("useSnapAnimation");
            snapSpeed = serializedObject.FindProperty("snapSpeed");
            onValueChanged = serializedObject.FindProperty("onValueChanged");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var comp = (DiscreteSlider)target;

            DrawComponentHeader();
            
            // 组件引用
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("滑动条组件", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(sliderBackground, new GUIContent("滑动条背景"));
            EditorGUILayout.PropertyField(sliderHandle, new GUIContent("滑动点"));
            
            // 快速查找按钮
            if (GUILayout.Button("🔍 自动查找组件", GUILayout.Height(25)))
            {
                FindComponents(comp);
                serializedObject.ApplyModifiedProperties();
            }

            // 检查组件
            if (sliderBackground.objectReferenceValue == null || sliderHandle.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox(
                    "⚠️ 请设置滑动条背景和滑动点引用！\n点击上方的'自动查找组件'按钮尝试自动查找。",
                    MessageType.Warning);
            }

            // 数值范围
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("数值范围", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical("box");
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(minValue, new GUIContent("最小值"));
            EditorGUILayout.PropertyField(maxValue, new GUIContent("最大值"));
            
            if (EditorGUI.EndChangeCheck())
            {
                // 确保最大值大于最小值
                if (maxValue.intValue <= minValue.intValue)
                {
                    maxValue.intValue = minValue.intValue + 1;
                }
                
                // 确保当前值在范围内
                currentValue.intValue = Mathf.Clamp(currentValue.intValue, minValue.intValue, maxValue.intValue);
            }
            
            int stepCount = maxValue.intValue - minValue.intValue;
            EditorGUILayout.HelpBox(
                $"共 {stepCount + 1} 个位置：{minValue.intValue} 到 {maxValue.intValue}",
                MessageType.Info);
            EditorGUILayout.EndVertical();

            // 当前值
            EditorGUILayout.Space(5);
            EditorGUILayout.BeginVertical("box");
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.IntSlider(currentValue, minValue.intValue, maxValue.intValue, new GUIContent("当前值"));
            
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                comp.SetValue(currentValue.intValue, false);
            }
            
            // 快捷值按钮
            EditorGUILayout.Space(3);
            EditorGUILayout.LabelField("快捷值", EditorStyles.miniBoldLabel);
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button($"最小({minValue.intValue})", GUILayout.Height(22)))
            {
                currentValue.intValue = minValue.intValue;
                serializedObject.ApplyModifiedProperties();
                comp.SetValue(currentValue.intValue, false);
            }
            if (GUILayout.Button("0", GUILayout.Height(22)))
            {
                if (minValue.intValue <= 0 && maxValue.intValue >= 0)
                {
                    currentValue.intValue = 0;
                    serializedObject.ApplyModifiedProperties();
                    comp.SetValue(0, false);
                }
            }
            if (GUILayout.Button($"最大({maxValue.intValue})", GUILayout.Height(22)))
            {
                currentValue.intValue = maxValue.intValue;
                serializedObject.ApplyModifiedProperties();
                comp.SetValue(currentValue.intValue, false);
            }
            
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            // 视觉设置
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("视觉设置", EditorStyles.boldLabel);
            
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(showTicks, new GUIContent("显示刻度标记"));
            
            if (showTicks.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(tickColor, new GUIContent("刻度线颜色"));
                EditorGUILayout.PropertyField(tickWidth, new GUIContent("刻度线宽度"));
                EditorGUILayout.PropertyField(tickHeight, new GUIContent("刻度线高度"));
                EditorGUI.indentLevel--;
            }
            
            if (EditorGUI.EndChangeCheck() && showTicks.boolValue)
            {
                serializedObject.ApplyModifiedProperties();
                // 重新创建刻度线
                if (Application.isPlaying)
                {
                    comp.SetRange(minValue.intValue, maxValue.intValue);
                }
            }

            // 交互设置
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("交互设置", EditorStyles.boldLabel);
            
            EditorGUILayout.PropertyField(isDraggable, new GUIContent("可拖拽"));
            EditorGUILayout.PropertyField(isClickable, new GUIContent("可点击跳转"));
            EditorGUILayout.PropertyField(useSnapAnimation, new GUIContent("启用吸附动画"));
            
            if (useSnapAnimation.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(snapSpeed, new GUIContent("吸附速度"));
                EditorGUI.indentLevel--;
            }

            // 事件
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("事件", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(onValueChanged, new GUIContent("值改变时"));

            serializedObject.ApplyModifiedProperties();

            // 操作按钮
            EditorGUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("⬅️ -1", GUILayout.Height(30)))
            {
                comp.Decrement();
            }
            if (GUILayout.Button("🔄 重置到0", GUILayout.Height(30)))
            {
                comp.ResetToZero();
            }
            if (GUILayout.Button("➡️ +1", GUILayout.Height(30)))
            {
                comp.Increment();
            }
            
            EditorGUILayout.EndHorizontal();

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
            EditorGUILayout.LabelField("📊 离散滑动条组件", titleStyle);
            EditorGUILayout.HelpBox(
                "固定值滑动条：滑动点只能停留在整数位置上。",
                MessageType.Info);
        }

        private void FindComponents(DiscreteSlider comp)
        {
            Transform bgTransform = comp.transform.Find("Background");
            if (bgTransform == null)
            {
                bgTransform = comp.transform.Find("Slider Background");
            }
            if (bgTransform == null)
            {
                bgTransform = comp.transform.Find("SliderBackground");
            }
            
            if (bgTransform != null)
            {
                sliderBackground.objectReferenceValue = bgTransform.GetComponent<RectTransform>();
                Debug.Log($"✓ 找到滑动条背景: {bgTransform.name}");
            }

            Transform handleTransform = comp.transform.Find("Handle");
            if (handleTransform == null)
            {
                handleTransform = comp.transform.Find("Slider Handle");
            }
            if (handleTransform == null)
            {
                handleTransform = comp.transform.Find("SliderHandle");
            }
            if (handleTransform == null && bgTransform != null)
            {
                handleTransform = bgTransform.Find("Handle");
            }
            
            if (handleTransform != null)
            {
                sliderHandle.objectReferenceValue = handleTransform.GetComponent<RectTransform>();
                Debug.Log($"✓ 找到滑动点: {handleTransform.name}");
            }

            if (bgTransform == null && handleTransform == null)
            {
                EditorUtility.DisplayDialog("查找失败", 
                    "未找到滑动条组件！\n请确保子对象命名为 'Background' 和 'Handle'", 
                    "确定");
            }
        }
    }
}

