using UnityEngine;
using UnityEditor;
using TabernaNoctis.CardSystem;

namespace TabernaNoctis.Editor
{
    /// <summary>
    /// CardSlot自定义编辑器
    /// </summary>
    [CustomEditor(typeof(CardSlot))]
    public class CardSlotEditor : UnityEditor.Editor
    {
        private SerializedProperty slotIndex;
        private SerializedProperty cardDisplay;
        private SerializedProperty isHighlighted;
        private SerializedProperty slotBackground;
        private SerializedProperty highlightColor;
        private SerializedProperty normalColor;

        private void OnEnable()
        {
            slotIndex = serializedObject.FindProperty("slotIndex");
            cardDisplay = serializedObject.FindProperty("cardDisplay");
            isHighlighted = serializedObject.FindProperty("isHighlighted");
            slotBackground = serializedObject.FindProperty("slotBackground");
            highlightColor = serializedObject.FindProperty("highlightColor");
            normalColor = serializedObject.FindProperty("normalColor");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var slot = (CardSlot)target;

            DrawComponentHeader();

            // 卡槽配置
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("卡槽配置", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.PropertyField(slotIndex, new GUIContent("卡槽编号"));
            EditorGUILayout.PropertyField(cardDisplay, new GUIContent("CardDisplay组件"));
            EditorGUILayout.EndVertical();

            // 视觉反馈
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("视觉反馈", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.PropertyField(isHighlighted, new GUIContent("高亮显示"));
            EditorGUILayout.PropertyField(slotBackground, new GUIContent("卡槽背景"));
            EditorGUILayout.PropertyField(highlightColor, new GUIContent("高亮颜色"));
            EditorGUILayout.PropertyField(normalColor, new GUIContent("正常颜色"));
            EditorGUILayout.EndVertical();

            serializedObject.ApplyModifiedProperties();

            // 状态信息
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("状态信息", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("包含卡牌", slot.HasCard ? "是" : "否");
            if (slot.HasCard)
            {
                var cardData = slot.GetCardData();
                if (cardData != null)
                {
                    EditorGUILayout.LabelField("卡牌名称", cardData.nameEN);
                    EditorGUILayout.LabelField("卡牌ID", cardData.id.ToString());
                }
            }
            EditorGUILayout.EndVertical();

            // 测试按钮
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("测试功能", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("清空卡槽", GUILayout.Height(30)))
            {
                slot.ClearCard();
                EditorUtility.SetDirty(slot);
            }

            GUI.enabled = !slot.HasCard;
            if (GUILayout.Button("自动查找组件", GUILayout.Height(30)))
            {
                slot.SendMessage("AutoFindCardDisplay", SendMessageOptions.DontRequireReceiver);
                EditorUtility.SetDirty(slot);
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawComponentHeader()
        {
            EditorGUILayout.Space(5);
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13,
                alignment = TextAnchor.MiddleLeft
            };
            EditorGUILayout.LabelField("卡槽组件", titleStyle);
            EditorGUILayout.HelpBox("管理单个卡槽的卡牌显示，配合CardQueueDispenser使用", MessageType.Info);
        }
    }
}

