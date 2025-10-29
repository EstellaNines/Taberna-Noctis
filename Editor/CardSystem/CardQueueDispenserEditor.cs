using UnityEngine;
using UnityEditor;
using TabernaNoctis.CardSystem;
using TabernaNoctis.Cards;

namespace TabernaNoctis.Editor
{
    /// <summary>
    /// CardQueueDispenser自定义编辑器
    /// </summary>
    [CustomEditor(typeof(CardQueueDispenser))]
    public class CardQueueDispenserEditor : UnityEditor.Editor
    {
        private SerializedProperty slotsContainer;
        private SerializedProperty cardSlotPrefab;
        private SerializedProperty maxSlots;
        private SerializedProperty dispenseInterval;
        private SerializedProperty autoStart;
        private SerializedProperty moveAnimationDuration;
        private SerializedProperty moveEaseType;
        private SerializedProperty fadeInDuration;
        private SerializedProperty scrollRect;
        private SerializedProperty autoScrollToLatest;
        private SerializedProperty playPlaceBottleSfx;
        private SerializedProperty placeBottleSfxPath;
        private SerializedProperty placeBottleSfxVolume;

        private BaseCardSO testCard;

        private void OnEnable()
        {
            slotsContainer = serializedObject.FindProperty("slotsContainer");
            cardSlotPrefab = serializedObject.FindProperty("cardSlotPrefab");
            maxSlots = serializedObject.FindProperty("maxSlots");
            dispenseInterval = serializedObject.FindProperty("dispenseInterval");
            autoStart = serializedObject.FindProperty("autoStart");
            moveAnimationDuration = serializedObject.FindProperty("moveAnimationDuration");
            moveEaseType = serializedObject.FindProperty("moveEaseType");
            fadeInDuration = serializedObject.FindProperty("fadeInDuration");
            scrollRect = serializedObject.FindProperty("scrollRect");
            autoScrollToLatest = serializedObject.FindProperty("autoScrollToLatest");

            // 音效设置
            playPlaceBottleSfx = serializedObject.FindProperty("playPlaceBottleSfx");
            placeBottleSfxPath = serializedObject.FindProperty("placeBottleSfxPath");
            placeBottleSfxVolume = serializedObject.FindProperty("placeBottleSfxVolume");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var dispenser = (CardQueueDispenser)target;

            DrawComponentHeader();

            // 卡槽容器配置
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("卡槽容器配置", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.PropertyField(slotsContainer, new GUIContent("卡槽父对象"));
            EditorGUILayout.PropertyField(cardSlotPrefab, new GUIContent("卡槽预制件"));
            EditorGUILayout.PropertyField(maxSlots, new GUIContent("最大卡槽数量"));
            EditorGUILayout.EndVertical();

            // 派发配置
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("派发配置", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.PropertyField(dispenseInterval, new GUIContent("派发间隔（秒）"));
            EditorGUILayout.PropertyField(autoStart, new GUIContent("自动开始派发"));
            EditorGUILayout.EndVertical();

            // 动画配置
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("动画配置", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.PropertyField(moveAnimationDuration, new GUIContent("移动动画时长"));
            EditorGUILayout.PropertyField(moveEaseType, new GUIContent("移动缓动类型"));
            EditorGUILayout.PropertyField(fadeInDuration, new GUIContent("淡入时长"));
            EditorGUILayout.EndVertical();

            // 滚动视图
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("滚动视图配置", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.PropertyField(scrollRect, new GUIContent("ScrollRect组件"));
            EditorGUILayout.PropertyField(autoScrollToLatest, new GUIContent("自动滚动到最新"));
            EditorGUILayout.EndVertical();

            // 音效设置
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("音效设置", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.PropertyField(playPlaceBottleSfx, new GUIContent("播放摆放瓶子音效"));
            using (new EditorGUI.DisabledScope(!playPlaceBottleSfx.boolValue))
            {
                EditorGUILayout.PropertyField(placeBottleSfxPath, new GUIContent("音效路径(Resources)"));
                EditorGUILayout.Slider(placeBottleSfxVolume, 0f, 1f, new GUIContent("音量"));
                EditorGUILayout.HelpBox("发牌开始自动播放：若窗口>音效→循环；若音效>窗口→自动加速在窗口内播完；发牌结束或停止立即停止音效。", MessageType.Info);
            }
            EditorGUILayout.EndVertical();

            serializedObject.ApplyModifiedProperties();

            // 运行时信息
            if (Application.isPlaying)
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.LabelField("运行时信息", EditorStyles.boldLabel);
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField("队列中卡牌数量", dispenser.GetQueueCount().ToString());
                EditorGUILayout.LabelField("已显示卡牌数量", dispenser.GetAllDisplayedCards().Count.ToString());
                
                var allSlots = dispenser.GetAllSlots();
                int filledSlots = 0;
                foreach (var slot in allSlots)
                {
                    if (slot != null && slot.HasCard) filledSlots++;
                }
                EditorGUILayout.LabelField("已填充卡槽", $"{filledSlots}/{allSlots.Count}");
                EditorGUILayout.EndVertical();
            }

            // 测试功能
            EditorGUILayout.Space(10);
            DrawTestSection(dispenser);
        }

        private void DrawComponentHeader()
        {
            EditorGUILayout.Space(5);
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13,
                alignment = TextAnchor.MiddleLeft
            };
            EditorGUILayout.LabelField("卡牌队列派发器", titleStyle);
            EditorGUILayout.HelpBox(
                "逐张派发卡牌到卡槽系统\n" +
                "新卡显示在1号槽，旧卡依次右移\n" +
                "支持动画效果和自动滚动",
                MessageType.Info);
        }

        private void DrawTestSection(CardQueueDispenser dispenser)
        {
            EditorGUILayout.LabelField("测试功能", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");

            // 单张测试
            testCard = (BaseCardSO)EditorGUILayout.ObjectField(
                "测试卡牌",
                testCard,
                typeof(BaseCardSO),
                false
            );

            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("添加到队列", GUILayout.Height(30)))
            {
                if (testCard != null)
                {
                    dispenser.EnqueueCard(testCard);
                    EditorUtility.SetDirty(dispenser);
                    Debug.Log($"[Editor] 已添加测试卡牌: {testCard.nameEN}");
                }
                else
                {
                    EditorUtility.DisplayDialog("提示", "请先选择一张测试卡牌", "确定");
                }
            }

            GUI.enabled = Application.isPlaying;
            if (GUILayout.Button("派发下一张", GUILayout.Height(30)))
            {
                dispenser.DispenseNextCard();
            }
            GUI.enabled = true;

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // 批量测试
            EditorGUILayout.LabelField("批量测试", EditorStyles.miniBoldLabel);
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("5张材料卡", GUILayout.Height(25)))
            {
                LoadRandomCards<MaterialCardSO>(dispenser, 5);
            }

            if (GUILayout.Button("5张鸡尾酒", GUILayout.Height(25)))
            {
                LoadRandomCards<CocktailCardSO>(dispenser, 5);
            }

            if (GUILayout.Button("10张混合", GUILayout.Height(25)))
            {
                LoadRandomCards<MaterialCardSO>(dispenser, 5);
                LoadRandomCards<CocktailCardSO>(dispenser, 5);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // 控制按钮
            EditorGUILayout.BeginHorizontal();

            GUI.enabled = Application.isPlaying && dispenser.GetQueueCount() > 0;
            Color originalBG = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.4f, 1f, 0.5f);
            if (GUILayout.Button("开始派发", GUILayout.Height(35)))
            {
                dispenser.StartDispensing();
            }
            GUI.backgroundColor = originalBG;

            GUI.backgroundColor = new Color(1f, 0.5f, 0.4f);
            GUI.enabled = Application.isPlaying;
            if (GUILayout.Button("停止派发", GUILayout.Height(35)))
            {
                dispenser.StopDispensing();
            }
            GUI.backgroundColor = originalBG;
            GUI.enabled = true;

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("清空队列", GUILayout.Height(30)))
            {
                dispenser.ClearQueue();
            }

            if (GUILayout.Button("清空所有卡槽", GUILayout.Height(30)))
            {
                dispenser.ClearAllSlots();
            }
            EditorGUILayout.EndHorizontal();

            if (!Application.isPlaying)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.HelpBox("运行时功能需要在Play模式下测试", MessageType.Warning);
            }

            EditorGUILayout.EndVertical();
        }

        private void LoadRandomCards<T>(CardQueueDispenser dispenser, int count) where T : BaseCardSO
        {
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            System.Collections.Generic.List<BaseCardSO> cards = new System.Collections.Generic.List<BaseCardSO>();

            int actualCount = Mathf.Min(count, guids.Length);
            for (int i = 0; i < actualCount; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                T card = AssetDatabase.LoadAssetAtPath<T>(path);
                if (card != null)
                {
                    cards.Add(card);
                }
            }

            if (cards.Count > 0)
            {
                dispenser.EnqueueCards(cards);
                Debug.Log($"[Editor] 已添加 {cards.Count} 张 {typeof(T).Name} 到队列");
                
                if (Application.isPlaying)
                {
                    EditorUtility.DisplayDialog("提示", 
                        $"已添加 {cards.Count} 张卡牌到队列\n当前队列总数: {dispenser.GetQueueCount()}", 
                        "确定");
                }
            }
            else
            {
                EditorUtility.DisplayDialog("提示", $"未找到 {typeof(T).Name} 卡牌", "确定");
            }
        }
    }
}
