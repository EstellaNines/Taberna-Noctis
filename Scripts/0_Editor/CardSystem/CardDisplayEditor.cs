using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;
using TabernaNoctis.Cards;
using TabernaNoctis.CardSystem;

namespace TabernaNoctis.Editor
{
    /// <summary>
    /// CardDisplay自定义编辑器
    /// </summary>
    [CustomEditor(typeof(CardDisplay))]
    public class CardDisplayEditor : UnityEditor.Editor
    {
        private SerializedProperty outlineBackground;
        private SerializedProperty mainBackground;
        private SerializedProperty textBackground;
        private SerializedProperty textBackgrounds;
        
        private SerializedProperty tagsText;
        private SerializedProperty nameText;
        private SerializedProperty priceText;
        private SerializedProperty cardImage;

        private SerializedProperty busyBar;
        private SerializedProperty impatientBar;
        private SerializedProperty boredBar;
        private SerializedProperty pickyBar;
        private SerializedProperty friendlyBar;
        
        private SerializedProperty editorPreviewCard;

        // 测试用
        private BaseCardSO testCardData;

        private void OnEnable()
        {
            outlineBackground = serializedObject.FindProperty("outlineBackground");
            mainBackground = serializedObject.FindProperty("mainBackground");
            textBackground = serializedObject.FindProperty("textBackground");
            textBackgrounds = serializedObject.FindProperty("textBackgrounds");
            
            tagsText = serializedObject.FindProperty("tagsText");
            nameText = serializedObject.FindProperty("nameText");
            priceText = serializedObject.FindProperty("priceText");
            cardImage = serializedObject.FindProperty("cardImage");

            busyBar = serializedObject.FindProperty("busyBar");
            impatientBar = serializedObject.FindProperty("impatientBar");
            boredBar = serializedObject.FindProperty("boredBar");
            pickyBar = serializedObject.FindProperty("pickyBar");
            friendlyBar = serializedObject.FindProperty("friendlyBar");
            
            editorPreviewCard = serializedObject.FindProperty("editorPreviewCard");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var comp = (CardDisplay)target;

            DrawComponentHeader();
            
            // 编辑器预览
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Editor Preview", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(editorPreviewCard, new GUIContent("Preview Card", "Drag CardSO here for real-time preview in Edit Mode"));
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                if (editorPreviewCard.objectReferenceValue != null)
                {
                    comp.SetCardData((BaseCardSO)editorPreviewCard.objectReferenceValue);
                    EditorUtility.SetDirty(comp);
                }
            }
            
            if (editorPreviewCard.objectReferenceValue != null)
            {
                var previewCard = editorPreviewCard.objectReferenceValue as BaseCardSO;
                string tagsDisplay = previewCard.tags != null && previewCard.tags.Length > 0 
                    ? string.Join(", ", previewCard.tags) 
                    : "None";
                EditorGUILayout.HelpBox(
                    $"Preview: {previewCard.nameEN}\n" +
                    $"Tags: {tagsDisplay}\n" +
                    $"Theme Color: RGB({previewCard.themeColor.r:F2}, {previewCard.themeColor.g:F2}, {previewCard.themeColor.b:F2})",
                    MessageType.Info);
            }
            EditorGUILayout.EndVertical();

            // 背景组件
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("背景组件（主题色）", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("这些背景会根据卡牌标签自动着色，不影响文字与数值颜色", MessageType.Info);
            EditorGUILayout.PropertyField(outlineBackground, new GUIContent("外边框背景", "外框，颜色更深"));
            EditorGUILayout.PropertyField(mainBackground, new GUIContent("主背景", "卡牌主体背景"));
            EditorGUILayout.PropertyField(textBackground, new GUIContent("文字背景（单个）", "单个文字区域背景，颜色更浅"));
            EditorGUILayout.PropertyField(textBackgrounds, new GUIContent("文字背景（多个）", "多个文字区域背景引用"), true);

            // 基础UI组件
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("基础显示组件", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(tagsText, new GUIContent("标签文本"));
            EditorGUILayout.PropertyField(nameText, new GUIContent("名称文本（始终英文）"));
            EditorGUILayout.PropertyField(priceText, new GUIContent("价格文本（$）"));
            EditorGUILayout.PropertyField(cardImage, new GUIContent("卡牌图片"));

            // 状态效果条
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("状态效果显示（5条）", EditorStyles.boldLabel);
            
            DrawStateBar(busyBar, "忙碌");
            DrawStateBar(impatientBar, "急躁");
            DrawStateBar(boredBar, "烦闷");
            DrawStateBar(pickyBar, "挑剔");
            DrawStateBar(friendlyBar, "友好");

            serializedObject.ApplyModifiedProperties();

            // 测试功能
            EditorGUILayout.Space(15);
            DrawTestSection(comp);
            
            // 底部信息
            EditorGUILayout.Space(10);
            EditorGUILayout.HelpBox("Tip: Drag a CardSO to 'Preview Card' field to see real-time preview in Edit Mode.", MessageType.Info);
        }

        private void DrawComponentHeader()
        {
            EditorGUILayout.Space(5);
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13,
                alignment = TextAnchor.MiddleLeft
            };
            EditorGUILayout.LabelField("Card Display Component", titleStyle);
            EditorGUILayout.HelpBox(
                "This component displays CardSO data on UI.\nMake sure all component references are set correctly.",
                MessageType.Info);
        }

        private void DrawStateBar(SerializedProperty barProperty, string label)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField(label, EditorStyles.miniBoldLabel);

            SerializedProperty stateName = barProperty.FindPropertyRelative("stateName");
            SerializedProperty parentObject = barProperty.FindPropertyRelative("parentObject");
            SerializedProperty iconImage = barProperty.FindPropertyRelative("iconImage");
            SerializedProperty slider = barProperty.FindPropertyRelative("slider");
            SerializedProperty valueText = barProperty.FindPropertyRelative("valueText");

            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(parentObject, new GUIContent("父对象"));
            EditorGUILayout.PropertyField(iconImage, new GUIContent("状态图标"));
            EditorGUILayout.PropertyField(slider, new GUIContent("离散滑动条"));
            EditorGUILayout.PropertyField(valueText, new GUIContent("数值文本"));
            EditorGUI.indentLevel--;

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(3);
        }

        private void DrawTestSection(CardDisplay comp)
        {
            EditorGUILayout.LabelField("Test Functions", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical("box");
            
            // 测试卡牌选择
            testCardData = (BaseCardSO)EditorGUILayout.ObjectField(
                "Test Card Data",
                testCardData,
                typeof(BaseCardSO),
                false
            );

            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Load Test Data", GUILayout.Height(30)))
            {
                if (testCardData != null)
                {
                    comp.SetCardData(testCardData);
                    EditorUtility.SetDirty(comp);
                }
                else
                {
                    EditorUtility.DisplayDialog("Error", "Please select a test card data first", "OK");
                }
            }

            if (GUILayout.Button("Clear Display", GUILayout.Height(30)))
            {
                comp.ClearDisplay();
                EditorUtility.SetDirty(comp);
            }

            EditorGUILayout.EndHorizontal();

            // 快速测试按钮
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Quick Test", EditorStyles.miniBoldLabel);
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Material Card", GUILayout.Height(25)))
            {
                LoadFirstCard<MaterialCardSO>(comp);
            }

            if (GUILayout.Button("Cocktail Card", GUILayout.Height(25)))
            {
                LoadFirstCard<CocktailCardSO>(comp);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void LoadFirstCard<T>(CardDisplay comp) where T : BaseCardSO
        {
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                T card = AssetDatabase.LoadAssetAtPath<T>(path);
                if (card != null)
                {
                    comp.SetCardData(card);
                    testCardData = card;
                    EditorUtility.SetDirty(comp);
                    Debug.Log($"[CardDisplay] 已加载测试卡牌: {card.nameCN}");
                }
            }
            else
            {
                EditorUtility.DisplayDialog("Info", $"Cannot find {typeof(T).Name} card data", "OK");
            }
        }
    }
}

