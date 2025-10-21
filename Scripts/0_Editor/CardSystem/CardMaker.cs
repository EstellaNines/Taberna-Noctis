using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using TabernaNoctis.Cards;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using Sirenix.Utilities;

namespace TabernaNoctis.Editor
{
    /// <summary>
    /// 卡牌制作器 - 完整的卡牌数据管理工具
    /// </summary>
    public class CardMaker : EditorWindow
    {
        #region 枚举定义

        private enum ImportResult
        {
            Created,   // 新建
            Updated,   // 更新
            Skipped,   // 跳过（未修改）
            Failed     // 失败
        }

        private enum CardType
        {
            Cocktail,  // 鸡尾酒
            Material   // 材料
        }

        private enum LeftPanelTab
        {
            JSONImport,      // JSON导入
            ManualCreate,    // 手动创建
            ForceUpdate      // 强制更新
        }

        private enum CenterPanelTab 
        { 
            Cocktails,  // 鸡尾酒
            Materials   // 材料
        }

        #endregion

        #region JSON数据结构

        [System.Serializable]
        public class JsonCardEffects
        {
            public int busy;
            public int impatient;
            public int bored;
            public int picky;
            public int friendly;
        }

        [System.Serializable]
        public class JsonCocktailData
        {
            public string name_en;
            public string name_cn;
            public string category;
            public string[] tags;            // 新增：标签数组
            public JsonCardEffects effects;
            public int cost;
            public int price;
            public int profit;
            public int reputation_change;
            public string feature;
            public string ui_path;
        }

        [System.Serializable]
        public class JsonMaterialData
        {
            public int id;
            public string name_cn;
            public string name_en;
            public string category;
            public string[] tags;            // 新增：标签数组
            public JsonCardEffects effects;
            public int price;
            public string feature;
            public string ui_path;
        }

        [System.Serializable]
        public class CocktailsWrapper
        {
            public List<JsonCocktailData> cocktails;
        }

        [System.Serializable]
        public class MaterialsWrapper
        {
            public List<JsonMaterialData> materials;
        }

        #endregion

        #region 路径配置

        private const string COCKTAILS_JSON_PATH = "Assets/Resources/Cards/Cocktails.json";
        private const string MATERIALS_JSON_PATH = "Assets/Resources/Cards/Materials.json";
        private const string OUTPUT_FOLDER = "Assets/Scripts/0_ScriptableObject/Cards";
        private const string COCKTAILS_FOLDER = "Assets/Scripts/0_ScriptableObject/Cards/Cocktails";
        private const string MATERIALS_FOLDER = "Assets/Scripts/0_ScriptableObject/Cards/Materials";

		// 预制件模板与输出目录
		private const string CARD_TEMPLATE_PREFAB_PATH = "Assets/Resources/Prefabs/0_General/1_CardSystem/Card.prefab";
		private const string CARD_PREFAB_OUTPUT_FOLDER = "Assets/Resources/Prefabs/0_General/1_CardSystem";

        #endregion

        #region 窗口变量

        [MenuItem("自制工具/卡牌系统/卡牌制作器")]
        private static void OpenCardMaker()
        {
            var window = GetWindow<CardMaker>();
            window.titleContent = new GUIContent("卡牌制作器", EditorGUIUtility.IconContent("d_ScriptableObject Icon").image);
            window.minSize = new Vector2(1200, 700);
            window.position = new Rect(100, 100, 1400, 800);
            window.LoadAllCards();
            window.Show();
        }

        private void OnEnable()
        {
            LoadAllCards();
        }

        // 布局
        private float leftPanelWidth = 420f;
        private float centerPanelWidth = 380f;
        private bool isDraggingLeftSplitter = false;
        private bool isDraggingCenterSplitter = false;

        // 左侧板块
        private LeftPanelTab leftTab = LeftPanelTab.JSONImport;
        private Vector2 leftScrollPos;
        private string logOutput = "";
        private bool incrementalUpdate = true;

        // 手动创建表单
        private CardType createCardType = CardType.Cocktail;
        private JsonCocktailData cocktailFormData = new JsonCocktailData { effects = new JsonCardEffects() };
        private JsonMaterialData materialFormData = new JsonMaterialData { effects = new JsonCardEffects() };

        // 中间板块
        private Vector2 centerScrollPos;
        private CenterPanelTab centerTab = CenterPanelTab.Cocktails;
        private List<CocktailCardSO> loadedCocktails = new List<CocktailCardSO>();
        private List<MaterialCardSO> loadedMaterials = new List<MaterialCardSO>();
        private string searchFilter = "";
        
        // 右侧板块
        private Vector2 rightScrollPos;
        private BaseCardSO selectedCard = null;

        #endregion

        #region GUI绘制

        private void OnGUI()
        {
            // 绘制顶部工具栏
            DrawTopToolbar();

            EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            // 左侧板块
            DrawLeftPanel();

            // 左侧分隔条
            DrawVerticalSplitter(ref isDraggingLeftSplitter, ref leftPanelWidth, 350f, 600f);

            // 中间板块
            DrawCenterPanel();

            // 中间分隔条
            DrawVerticalSplitter(ref isDraggingCenterSplitter, ref centerPanelWidth, 300f, 550f);

            // 右侧板块
            DrawRightPanel();

            EditorGUILayout.EndHorizontal();

            // 处理拖拽
            HandleSplitterDrag();
        }

        private void DrawTopToolbar()
        {
            SirenixEditorGUI.BeginBox();
            EditorGUILayout.BeginHorizontal(GUILayout.Height(30));
            
            GUILayout.FlexibleSpace();
            
            var titleStyle = new GUIStyle(EditorStyles.largeLabel);
            titleStyle.fontSize = 16;
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.normal.textColor = EditorGUIUtility.isProSkin ? new Color(0.8f, 0.9f, 1f) : new Color(0.15f, 0.2f, 0.3f);
            GUILayout.Label("卡牌制作器", titleStyle);
            
            GUILayout.FlexibleSpace();
            
			if (GUILayout.Button(new GUIContent("刷新全部", EditorGUIUtility.IconContent("d_Refresh").image), GUILayout.Width(90), GUILayout.Height(25)))
            {
                LoadAllCards();
                logOutput = "已刷新所有数据\n";
            }

			GUILayout.Space(6);

			if (GUILayout.Button(new GUIContent("生成全部预制件", EditorGUIUtility.IconContent("d_Prefab Icon").image), GUILayout.Width(120), GUILayout.Height(25)))
			{
				GenerateAllCardPrefabs();
			}
            
            EditorGUILayout.EndHorizontal();
            SirenixEditorGUI.EndBox();
        }

        #endregion

		#region 预制件生成

		private void GenerateAllCardPrefabs()
		{
			LoadAllCards();
			EnsureFolderExists(CARD_PREFAB_OUTPUT_FOLDER);

			var template = AssetDatabase.LoadAssetAtPath<GameObject>(CARD_TEMPLATE_PREFAB_PATH);
			if (template == null)
			{
				EditorUtility.DisplayDialog("错误", $"找不到模板预制件: {CARD_TEMPLATE_PREFAB_PATH}", "确定");
				LogError($"找不到模板预制件: {CARD_TEMPLATE_PREFAB_PATH}");
				return;
			}

			int created = 0;
			foreach (var card in loadedMaterials)
			{
				if (card == null) continue;
				if (CreateCardPrefab(template, card)) created++;
			}
			foreach (var card in loadedCocktails)
			{
				if (card == null) continue;
				if (CreateCardPrefab(template, card)) created++;
			}

			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
			EditorUtility.DisplayDialog("完成", $"已生成 {created} 个卡牌预制件", "确定");
			Log($"生成预制件完成: {created} 个");
		}

		private bool CreateCardPrefab(GameObject template, BaseCardSO card)
		{
			try
			{
				// 实例化临时对象
				var instance = (GameObject)PrefabUtility.InstantiatePrefab(template);
				if (instance == null) return false;

				// 命名
				string safeName = SanitizeFileName(string.IsNullOrEmpty(card.nameEN) ? card.nameCN : card.nameEN);
				instance.name = $"Card_{card.id:D3}_{safeName}";

				// 绑定数据到CardDisplay（使用隐藏的editorPreviewCard以便编辑器预览）
				var display = instance.GetComponent<TabernaNoctis.CardSystem.CardDisplay>();
				if (display != null)
				{
					var so = new SerializedObject(display);
					var prop = so.FindProperty("editorPreviewCard");
					if (prop != null)
					{
						prop.objectReferenceValue = card;
						so.ApplyModifiedPropertiesWithoutUndo();
					}
				}

				// 保存为预制件
				string assetPath = System.IO.Path.Combine(CARD_PREFAB_OUTPUT_FOLDER, instance.name + ".prefab").Replace("\\", "/");
				PrefabUtility.SaveAsPrefabAsset(instance, assetPath, out bool success);
				Object.DestroyImmediate(instance);
				if (success)
				{
					Log($"  生成预制件: {assetPath}");
					return true;
				}
				LogError($"  生成失败: {assetPath}");
				return false;
			}
			catch (System.Exception e)
			{
				LogError($"生成预制件失败 ({card.nameEN}): {e.Message}");
				Debug.LogException(e);
				return false;
			}
		}

		private string SanitizeFileName(string name)
		{
			if (string.IsNullOrEmpty(name)) return "Card";
			foreach (char c in System.IO.Path.GetInvalidFileNameChars())
			{
				name = name.Replace(c.ToString(), "_");
			}
			// 额外替换空白
			name = name.Replace(' ', '_');
			return name;
		}

		#endregion

        #region 左侧板块

        private void DrawLeftPanel()
        {
            SirenixEditorGUI.BeginBox();
            EditorGUILayout.BeginVertical(GUILayout.Width(leftPanelWidth), GUILayout.ExpandHeight(true));
            
            SirenixEditorGUI.Title("创建工具", "", TextAlignment.Left, true);
            
            // Tab切换
            EditorGUILayout.BeginHorizontal();
            
            // 背景高亮绘制需要在有内容之后获取Rect
            Color originalColor = GUI.backgroundColor;
            
            GUI.backgroundColor = leftTab == LeftPanelTab.JSONImport ? (EditorGUIUtility.isProSkin ? new Color(0.5f, 0.8f, 1f) : new Color(0.75f, 0.9f, 1f)) : (EditorGUIUtility.isProSkin ? new Color(0.2f,0.2f,0.2f,1f) : Color.white);
            if (GUILayout.Button("JSON导入", GUILayout.Height(30)))
                leftTab = LeftPanelTab.JSONImport;
                
            GUI.backgroundColor = leftTab == LeftPanelTab.ManualCreate ? (EditorGUIUtility.isProSkin ? new Color(0.5f, 1f, 0.7f) : new Color(0.7f, 1f, 0.85f)) : (EditorGUIUtility.isProSkin ? new Color(0.2f,0.2f,0.2f,1f) : Color.white);
            if (GUILayout.Button("手动创建", GUILayout.Height(30)))
                leftTab = LeftPanelTab.ManualCreate;
                
            GUI.backgroundColor = leftTab == LeftPanelTab.ForceUpdate ? (EditorGUIUtility.isProSkin ? new Color(1f, 0.8f, 0.5f) : new Color(1f, 0.9f, 0.7f)) : (EditorGUIUtility.isProSkin ? new Color(0.2f,0.2f,0.2f,1f) : Color.white);
            if (GUILayout.Button("强制更新", GUILayout.Height(30)))
                leftTab = LeftPanelTab.ForceUpdate;
                
            GUI.backgroundColor = originalColor;
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(10);

            leftScrollPos = EditorGUILayout.BeginScrollView(leftScrollPos);

            switch (leftTab)
            {
                case LeftPanelTab.JSONImport:
                    DrawJSONImportTab();
                    break;
                case LeftPanelTab.ManualCreate:
                    DrawManualCreateTab();
                    break;
                case LeftPanelTab.ForceUpdate:
                    DrawForceUpdateTab();
                    break;
            }

            EditorGUILayout.EndScrollView();
            
            EditorGUILayout.EndVertical();
            SirenixEditorGUI.EndBox();
        }

        private void DrawJSONImportTab()
        {
            EditorGUILayout.HelpBox(
                "从JSON文件导入卡牌数据并生成ScriptableObject资源\n" +
                "源文件：\n" +
                "- " + COCKTAILS_JSON_PATH + "\n" +
                "- " + MATERIALS_JSON_PATH,
                MessageType.Info);

            GUILayout.Space(10);

            // 增量更新选项
            SirenixEditorGUI.BeginBox("导入选项");
            incrementalUpdate = EditorGUILayout.Toggle("增量更新模式", incrementalUpdate);
            EditorGUILayout.HelpBox(
                incrementalUpdate ? "只更新有变化的资源，跳过未修改的文件" : "强制覆盖所有现有资源",
                incrementalUpdate ? MessageType.Info : MessageType.Warning);
            SirenixEditorGUI.EndBox();

            GUILayout.Space(10);

            // 导入按钮
            Color originalColor = GUI.backgroundColor;
            
            GUI.backgroundColor = new Color(0.4f, 0.8f, 1f);
            if (GUILayout.Button("导入所有卡牌数据", GUILayout.Height(45)))
            {
                ImportAllCards();
            }

            GUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = new Color(0.7f, 0.9f, 1f);
            if (GUILayout.Button("仅导入鸡尾酒", GUILayout.Height(35)))
            {
                ImportCocktails();
            }
            if (GUILayout.Button("仅导入材料", GUILayout.Height(35)))
            {
                ImportMaterials();
            }
            EditorGUILayout.EndHorizontal();
            
            GUI.backgroundColor = originalColor;

            GUILayout.Space(15);
            
            SirenixEditorGUI.Title("导入日志", "", TextAlignment.Left, false);
            DrawStyledTextArea(logOutput);
        }

        private void DrawManualCreateTab()
        {
            SirenixEditorGUI.Title("手动创建卡牌", "", TextAlignment.Left, true);
            
            EditorGUILayout.HelpBox(
                "手动输入数据创建卡牌并自动写入JSON文件", 
                MessageType.Info);

            GUILayout.Space(10);

            // 卡牌类型选择
            SirenixEditorGUI.BeginBox("卡牌类型");
            createCardType = (CardType)EditorGUILayout.EnumPopup("选择类型", createCardType);
            SirenixEditorGUI.EndBox();

            GUILayout.Space(10);

            if (createCardType == CardType.Cocktail)
            {
                DrawCocktailForm();
            }
            else
            {
                DrawMaterialForm();
            }

            GUILayout.Space(20);
            
            Color originalColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.3f, 1f, 0.5f);
            if (GUILayout.Button("创建卡牌并写入JSON", GUILayout.Height(45)))
            {
                CreateCardManually();
            }
            GUI.backgroundColor = originalColor;

            GUILayout.Space(10);
            SirenixEditorGUI.Title("操作日志", "", TextAlignment.Left, false);
            DrawStyledTextArea(logOutput, 120);
        }

        private void DrawCocktailForm()
        {
            SirenixEditorGUI.BeginBox("鸡尾酒基础信息");
            cocktailFormData.name_cn = EditorGUILayout.TextField("中文名", cocktailFormData.name_cn);
            cocktailFormData.name_en = EditorGUILayout.TextField("英文名", cocktailFormData.name_en);
            cocktailFormData.category = EditorGUILayout.TextField("分类", cocktailFormData.category);
            cocktailFormData.feature = EditorGUILayout.TextField("特点", cocktailFormData.feature);
            cocktailFormData.ui_path = EditorGUILayout.TextField("UI路径", cocktailFormData.ui_path);
            SirenixEditorGUI.EndBox();

            GUILayout.Space(5);
            
            SirenixEditorGUI.BeginBox("效果数值");
            cocktailFormData.effects.busy = EditorGUILayout.IntField("忙碌", cocktailFormData.effects.busy);
            cocktailFormData.effects.impatient = EditorGUILayout.IntField("急躁", cocktailFormData.effects.impatient);
            cocktailFormData.effects.bored = EditorGUILayout.IntField("烦闷", cocktailFormData.effects.bored);
            cocktailFormData.effects.picky = EditorGUILayout.IntField("挑剔", cocktailFormData.effects.picky);
            cocktailFormData.effects.friendly = EditorGUILayout.IntField("友好", cocktailFormData.effects.friendly);
            SirenixEditorGUI.EndBox();

            GUILayout.Space(5);
            
            SirenixEditorGUI.BeginBox("经济数据");
            cocktailFormData.cost = EditorGUILayout.IntField("成本", cocktailFormData.cost);
            cocktailFormData.price = EditorGUILayout.IntField("售价", cocktailFormData.price);
            cocktailFormData.profit = EditorGUILayout.IntField("利润", cocktailFormData.profit);
            cocktailFormData.reputation_change = EditorGUILayout.IntField("评价变化", cocktailFormData.reputation_change);
            SirenixEditorGUI.EndBox();
        }

        private void DrawMaterialForm()
        {
            SirenixEditorGUI.BeginBox("材料基础信息");
            materialFormData.id = EditorGUILayout.IntField("ID", materialFormData.id);
            materialFormData.name_cn = EditorGUILayout.TextField("中文名", materialFormData.name_cn);
            materialFormData.name_en = EditorGUILayout.TextField("英文名", materialFormData.name_en);
            materialFormData.category = EditorGUILayout.TextField("分类", materialFormData.category);
            materialFormData.feature = EditorGUILayout.TextField("特点", materialFormData.feature);
            materialFormData.ui_path = EditorGUILayout.TextField("UI路径", materialFormData.ui_path);
            SirenixEditorGUI.EndBox();

            GUILayout.Space(5);
            
            SirenixEditorGUI.BeginBox("效果数值");
            materialFormData.effects.busy = EditorGUILayout.IntField("忙碌", materialFormData.effects.busy);
            materialFormData.effects.impatient = EditorGUILayout.IntField("急躁", materialFormData.effects.impatient);
            materialFormData.effects.bored = EditorGUILayout.IntField("烦闷", materialFormData.effects.bored);
            materialFormData.effects.picky = EditorGUILayout.IntField("挑剔", materialFormData.effects.picky);
            materialFormData.effects.friendly = EditorGUILayout.IntField("友好", materialFormData.effects.friendly);
            SirenixEditorGUI.EndBox();

            GUILayout.Space(5);
            
            SirenixEditorGUI.BeginBox("经济数据");
            materialFormData.price = EditorGUILayout.IntField("价格", materialFormData.price);
            SirenixEditorGUI.EndBox();
        }

        private void DrawForceUpdateTab()
        {
            SirenixEditorGUI.Title("强制更新模式", "", TextAlignment.Left, true);
            
            EditorGUILayout.HelpBox(
                "遍历JSON文件中的所有数据，强制替换现有的ScriptableObject\n" +
                "即使数据相同也会重新创建。用于修复数据不一致问题。",
                MessageType.Warning);

            GUILayout.Space(15);

            Color originalColor = GUI.backgroundColor;
            
            GUI.backgroundColor = new Color(1f, 0.6f, 0.4f);
            if (GUILayout.Button("强制更新所有鸡尾酒", GUILayout.Height(40)))
            {
                if (EditorUtility.DisplayDialog("确认操作", "此操作会删除并重建所有鸡尾酒SO，是否继续？", "确定", "取消"))
                {
                    ForceUpdateCocktails();
                }
            }

            GUILayout.Space(5);

            if (GUILayout.Button("强制更新所有材料", GUILayout.Height(40)))
            {
                if (EditorUtility.DisplayDialog("确认操作", "此操作会删除并重建所有材料SO，是否继续？", "确定", "取消"))
                {
                    ForceUpdateMaterials();
                }
            }

            GUILayout.Space(5);

            GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
            if (GUILayout.Button("强制更新全部卡牌", GUILayout.Height(40)))
            {
                if (EditorUtility.DisplayDialog("确认操作", "此操作会删除并重建所有卡牌SO，是否继续？", "确定", "取消"))
                {
                    ForceUpdateAll();
                }
            }
            
            GUI.backgroundColor = originalColor;

            GUILayout.Space(20);
            SirenixEditorGUI.Title("更新日志", "", TextAlignment.Left, false);
            DrawStyledTextArea(logOutput);
        }

        private void DrawStyledTextArea(string text, float height = -1)
        {
            var style = new GUIStyle(EditorStyles.textArea);
            style.wordWrap = true;
            style.fontSize = 11;
            style.padding = new RectOffset(8, 8, 8, 8);
            
            if (height > 0)
            {
                EditorGUILayout.TextArea(text, style, GUILayout.ExpandHeight(true), GUILayout.Height(height));
            }
            else
            {
                EditorGUILayout.TextArea(text, style, GUILayout.ExpandHeight(true));
            }
        }

        #endregion

        #region 中间板块

        private void DrawCenterPanel()
        {
            SirenixEditorGUI.BeginBox();
            EditorGUILayout.BeginVertical(GUILayout.Width(centerPanelWidth), GUILayout.ExpandHeight(true));
            
            // 标题栏
            EditorGUILayout.BeginHorizontal();
            SirenixEditorGUI.Title("卡牌库", "", TextAlignment.Left, false, true);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(new GUIContent(EditorGUIUtility.IconContent("d_Refresh").image, "刷新列表"), GUILayout.Width(30), GUILayout.Height(25)))
            {
                LoadAllCards();
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(5);

            // Tab切换
            EditorGUILayout.BeginHorizontal();
            Color originalColor = GUI.backgroundColor;
            
            GUI.backgroundColor = centerTab == CenterPanelTab.Cocktails ? (EditorGUIUtility.isProSkin ? new Color(0.8f, 0.6f, 1f) : new Color(0.9f, 0.8f, 1f)) : (EditorGUIUtility.isProSkin ? new Color(0.2f,0.2f,0.2f,1f) : Color.white);
            if (GUILayout.Button($"鸡尾酒 [{loadedCocktails.Count}]", GUILayout.Height(28)))
                centerTab = CenterPanelTab.Cocktails;
                
            GUI.backgroundColor = centerTab == CenterPanelTab.Materials ? (EditorGUIUtility.isProSkin ? new Color(1f, 0.8f, 0.5f) : new Color(1f, 0.9f, 0.7f)) : (EditorGUIUtility.isProSkin ? new Color(0.2f,0.2f,0.2f,1f) : Color.white);
            if (GUILayout.Button($"材料 [{loadedMaterials.Count}]", GUILayout.Height(28)))
                centerTab = CenterPanelTab.Materials;
                
            GUI.backgroundColor = originalColor;
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(5);

            // 搜索框
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(EditorGUIUtility.IconContent("d_Search Icon"), GUILayout.Width(20), GUILayout.Height(20));
            searchFilter = EditorGUILayout.TextField(searchFilter, EditorStyles.toolbarSearchField);
            if (GUILayout.Button("×", GUILayout.Width(25), GUILayout.Height(20)))
            {
                searchFilter = "";
                GUI.FocusControl(null);
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(8);

            // 列表显示
            centerScrollPos = EditorGUILayout.BeginScrollView(centerScrollPos);

            if (centerTab == CenterPanelTab.Cocktails)
            {
                DrawCocktailsList();
            }
            else
            {
                DrawMaterialsList();
            }

            EditorGUILayout.EndScrollView();
            
            EditorGUILayout.EndVertical();
            SirenixEditorGUI.EndBox();
        }

        private void DrawCocktailsList()
        {
            if (loadedCocktails.Count == 0)
            {
                EditorGUILayout.HelpBox("没有找到鸡尾酒卡牌\n请先从JSON导入或手动创建", MessageType.Warning);
                return;
            }

            // 过滤
            var filteredList = loadedCocktails;
            if (!string.IsNullOrEmpty(searchFilter))
            {
                filteredList = loadedCocktails.FindAll(c => 
                    c.nameCN.Contains(searchFilter) || 
                    c.nameEN.ToLower().Contains(searchFilter.ToLower()) ||
                    c.category.Contains(searchFilter));
            }

            if (filteredList.Count == 0)
            {
                EditorGUILayout.HelpBox($"没有找到匹配 \"{searchFilter}\" 的鸡尾酒", MessageType.Info);
                return;
            }

            foreach (var cocktail in filteredList)
            {
                if (cocktail == null) continue;
                DrawCardListItem(cocktail, cocktail.price);
            }
        }

        private void DrawMaterialsList()
        {
            if (loadedMaterials.Count == 0)
            {
                EditorGUILayout.HelpBox("没有找到材料卡牌\n请先从JSON导入或手动创建", MessageType.Warning);
                return;
            }

            // 过滤
            var filteredList = loadedMaterials;
            if (!string.IsNullOrEmpty(searchFilter))
            {
                filteredList = loadedMaterials.FindAll(m => 
                    m.nameCN.Contains(searchFilter) || 
                    m.nameEN.ToLower().Contains(searchFilter.ToLower()) ||
                    m.category.Contains(searchFilter));
            }

            if (filteredList.Count == 0)
            {
                EditorGUILayout.HelpBox($"没有找到匹配 \"{searchFilter}\" 的材料", MessageType.Info);
                return;
            }

            foreach (var material in filteredList)
            {
                if (material == null) continue;
                DrawCardListItem(material, material.price);
            }
        }

        private void DrawCardListItem(BaseCardSO card, int price)
        {
            bool isSelected = selectedCard == card;
            
            SirenixEditorGUI.BeginBox();

            EditorGUILayout.BeginHorizontal();

            // 图标预览
            if (card.uiSpritePreview != null)
            {
                Rect iconRect = GUILayoutUtility.GetRect(50, 50, GUILayout.Width(50), GUILayout.Height(50));
                SirenixEditorGUI.DrawSolidRect(iconRect, new Color(0, 0, 0, 0.1f));
                GUI.DrawTexture(iconRect.Padding(2), card.uiSpritePreview.texture, ScaleMode.ScaleToFit);
            }
            else
            {
                Rect iconRect = GUILayoutUtility.GetRect(50, 50, GUILayout.Width(50), GUILayout.Height(50));
                SirenixEditorGUI.DrawSolidRect(iconRect, new Color(0.5f, 0.5f, 0.5f, 0.2f));
                GUI.Label(iconRect, "无图", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 9 });
            }

            GUILayout.Space(8);

            // 卡牌信息
            EditorGUILayout.BeginVertical();
            
            var nameStyle = new GUIStyle(EditorStyles.boldLabel);
            nameStyle.fontSize = 13;
            nameStyle.normal.textColor = isSelected
                ? (EditorGUIUtility.isProSkin ? new Color(0.4f, 0.7f, 1f) : new Color(0.1f, 0.25f, 0.5f))
                : (EditorGUIUtility.isProSkin ? Color.white : new Color(0.1f, 0.1f, 0.1f));
            EditorGUILayout.LabelField($"{card.id:D3}. {card.nameCN}", nameStyle);
            
            var subStyle = new GUIStyle(EditorStyles.miniLabel);
            subStyle.normal.textColor = EditorGUIUtility.isProSkin ? new Color(0.7f, 0.7f, 0.7f) : new Color(0.3f, 0.3f, 0.3f);
            EditorGUILayout.LabelField(card.nameEN, subStyle);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(card.category, subStyle, GUILayout.Width(120));
            GUILayout.FlexibleSpace();
            var priceStyle = new GUIStyle(EditorStyles.miniLabel);
            priceStyle.fontStyle = FontStyle.Bold;
            priceStyle.normal.textColor = EditorGUIUtility.isProSkin ? new Color(1f, 0.8f, 0.3f) : new Color(0.6f, 0.4f, 0.0f);
            EditorGUILayout.LabelField($"${price}", priceStyle);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();

            // 在行末尾绘制高亮背景（此时已有布局可供GetLastRect）
            if (isSelected)
            {
                var rowRect = GUILayoutUtility.GetLastRect();
                // 扩展到整块Box宽度（包含左右内边距），并限定高度
                rowRect.x = 0f;
                rowRect.width = GUILayoutUtility.GetLastRect().width + 4f;
                rowRect.height = Mathf.Max(rowRect.height, 60f);
                SirenixEditorGUI.DrawSolidRect(rowRect, new Color(0.3f, 0.5f, 0.8f, 0.15f));
            }

            EditorGUILayout.EndHorizontal();

            GUILayout.Space(3);

            // 查看详情按钮
            Color originalColor = GUI.backgroundColor;
            GUI.backgroundColor = isSelected ? new Color(0.5f, 0.8f, 1f) : new Color(0.7f, 0.7f, 0.7f);
            if (GUILayout.Button(isSelected ? "已选中" : "查看详情", GUILayout.Height(25)))
            {
                selectedCard = card;
                Repaint();
            }
            GUI.backgroundColor = originalColor;

            SirenixEditorGUI.EndBox();
            GUILayout.Space(4);
        }

        #endregion

        #region 右侧板块

        private void DrawRightPanel()
        {
            SirenixEditorGUI.BeginBox();
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            
            SirenixEditorGUI.Title("卡牌详情", "", TextAlignment.Left, true);

            if (selectedCard == null)
            {
                GUILayout.FlexibleSpace();
                EditorGUILayout.HelpBox("请在中间板块选择一张卡牌查看详细信息", MessageType.Info);
                GUILayout.FlexibleSpace();
            }
            else
            {
                rightScrollPos = EditorGUILayout.BeginScrollView(rightScrollPos);
                DrawCardDetails();
                EditorGUILayout.EndScrollView();
            }
            
            EditorGUILayout.EndVertical();
            SirenixEditorGUI.EndBox();
        }

        private void DrawCardDetails()
        {
            if (selectedCard == null) return;

            // 卡牌标题
            var titleStyle = new GUIStyle(EditorStyles.largeLabel);
            titleStyle.fontSize = 16;
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.alignment = TextAnchor.MiddleCenter;
            titleStyle.normal.textColor = new Color(0.6f, 0.8f, 1f);
            
            GUILayout.Space(10);
            EditorGUILayout.LabelField($"{selectedCard.nameCN} | {selectedCard.nameEN}", titleStyle);
            
            GUILayout.Space(15);

            // 精灵预览
            if (selectedCard.uiSpritePreview != null)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                
                EditorGUILayout.BeginVertical();
                SirenixEditorGUI.Title("精灵预览", "", TextAlignment.Center, false);
                
                Rect previewRect = GUILayoutUtility.GetRect(280, 280, GUILayout.Width(280), GUILayout.Height(280));
                
                // 绘制背景和边框
                SirenixEditorGUI.DrawSolidRect(previewRect.Padding(-4), new Color(0.2f, 0.2f, 0.2f, 0.3f));
                SirenixEditorGUI.DrawBorders(previewRect.Padding(-3), 2, new Color(0.4f, 0.6f, 0.8f));
                SirenixEditorGUI.DrawSolidRect(previewRect, Color.white);
                
                GUI.DrawTexture(previewRect.Padding(5), selectedCard.uiSpritePreview.texture, ScaleMode.ScaleToFit);
                
                EditorGUILayout.EndVertical();
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.HelpBox("未找到精灵预览图", MessageType.Warning);
            }

            GUILayout.Space(20);

            // 基础信息
            SirenixEditorGUI.BeginBox("基础信息");
            DrawDetailField("ID", selectedCard.id.ToString());
            DrawDetailField("中文名", selectedCard.nameCN);
            DrawDetailField("英文名", selectedCard.nameEN);
            DrawDetailField("分类", selectedCard.category);
            
            // 显示标签
            if (selectedCard.tags != null && selectedCard.tags.Length > 0)
            {
                DrawTagsField(selectedCard.tags);
            }
            
            DrawDetailField("特点", selectedCard.feature);
            DrawDetailField("UI路径", selectedCard.uiPath);
            SirenixEditorGUI.EndBox();

            GUILayout.Space(10);

            // 效果数值
            SirenixEditorGUI.BeginBox("效果数值");
            DrawEffectBar("忙碌", selectedCard.effects.busy);
            DrawEffectBar("急躁", selectedCard.effects.impatient);
            DrawEffectBar("烦闷", selectedCard.effects.bored);
            DrawEffectBar("挑剔", selectedCard.effects.picky);
            DrawEffectBar("友好", selectedCard.effects.friendly);
            SirenixEditorGUI.EndBox();

            GUILayout.Space(10);

            // 经济数据
            SirenixEditorGUI.BeginBox("经济数据");
            if (selectedCard is CocktailCardSO cocktail)
            {
                DrawDetailField("成本", $"{cocktail.cost}");
                DrawDetailField("售价", $"{cocktail.price}");
                DrawDetailField("利润", $"{cocktail.profit}");
                DrawDetailField("评价变化", cocktail.reputationChange.ToString());
            }
            else if (selectedCard is MaterialCardSO material)
            {
                DrawDetailField("价格", $"{material.price}");
            }
            SirenixEditorGUI.EndBox();

            GUILayout.Space(10);

            // 资源引用
            SirenixEditorGUI.BeginBox("资源引用");
            EditorGUILayout.ObjectField("ScriptableObject", selectedCard, typeof(BaseCardSO), false);
            SirenixEditorGUI.EndBox();

            GUILayout.Space(15);

            // 操作按钮
            EditorGUILayout.BeginHorizontal();
            Color originalBG = GUI.backgroundColor;
            
            GUI.backgroundColor = new Color(0.5f, 0.8f, 1f);
            if (GUILayout.Button(new GUIContent("在Project中定位", EditorGUIUtility.IconContent("d_Project").image), GUILayout.Height(35)))
            {
                EditorGUIUtility.PingObject(selectedCard);
                Selection.activeObject = selectedCard;
            }
            
            GUI.backgroundColor = new Color(0.6f, 1f, 0.6f);
            if (GUILayout.Button(new GUIContent("在Inspector中打开", EditorGUIUtility.IconContent("d_UnityEditor.InspectorWindow").image), GUILayout.Height(35)))
            {
                Selection.activeObject = selectedCard;
                EditorGUIUtility.PingObject(selectedCard);
            }
            
            GUI.backgroundColor = originalBG;
            EditorGUILayout.EndHorizontal();
        }

        private void DrawDetailField(string label, string value)
        {
            EditorGUILayout.BeginHorizontal();
            
            var labelStyle = new GUIStyle(EditorStyles.label);
            labelStyle.fontStyle = FontStyle.Bold;
            labelStyle.normal.textColor = EditorGUIUtility.isProSkin ? new Color(0.7f, 0.8f, 0.9f) : new Color(0.15f, 0.2f, 0.3f);
            EditorGUILayout.LabelField(label, labelStyle, GUILayout.Width(80));
            
            var valueStyle = new GUIStyle(EditorStyles.label);
            valueStyle.wordWrap = true;
            valueStyle.normal.textColor = EditorGUIUtility.isProSkin ? Color.white : new Color(0.05f, 0.05f, 0.05f);
            EditorGUILayout.LabelField(value, valueStyle);
            
            EditorGUILayout.EndHorizontal();
        }

        private void DrawTagsField(string[] tags)
        {
            EditorGUILayout.BeginHorizontal();
            
            var labelStyle = new GUIStyle(EditorStyles.label);
            labelStyle.fontStyle = FontStyle.Bold;
            labelStyle.normal.textColor = EditorGUIUtility.isProSkin ? new Color(0.7f, 0.8f, 0.9f) : new Color(0.15f, 0.2f, 0.3f);
            EditorGUILayout.LabelField("标签", labelStyle, GUILayout.Width(80));
            
            EditorGUILayout.BeginHorizontal();
            foreach (string tag in tags)
            {
                // 根据标签类型选择颜色
                Color tagColor = GetTagColor(tag);
                
                var tagStyle = new GUIStyle(GUI.skin.box);
                tagStyle.normal.background = MakeTex(2, 2, tagColor);
                tagStyle.normal.textColor = Color.white;
                tagStyle.fontStyle = FontStyle.Bold;
                tagStyle.padding = new RectOffset(8, 8, 3, 3);
                tagStyle.margin = new RectOffset(2, 2, 2, 2);
                
                GUILayout.Label(tag, tagStyle);
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndHorizontal();
        }

        private Color GetTagColor(string tag)
        {
            // 为不同标签返回不同颜色
            switch (tag)
            {
                case "基酒": return new Color(0.70f, 0.85f, 1.00f);       // 浅蓝色（基酒）
                case "调味酒": return new Color(1.00f, 0.95f, 0.70f);     // 浅黄色（调味酒）
                case "调味品": return new Color(1.00f, 0.70f, 0.70f);     // 浅红色（调味品）
                case "新鲜配料": return new Color(0.80f, 1.00f, 0.80f);   // 浅绿色（新鲜配料）
                case "经典酒": return new Color(0.53f, 0.81f, 0.98f);     // 天蓝色（经典酒）
                case "热带酒": return new Color(1.00f, 0.80f, 0.60f);     // 浅橙色（热带酒）
                case "特调酒": return new Color(0.90f, 0.80f, 1.00f);     // 浅紫色（特调酒）
                default: return new Color(0.5f, 0.5f, 0.5f);           // 灰色
            }
        }

        private Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++)
                pix[i] = col;
            
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

        /// <summary>
        /// 根据标签获取颜色基调（用于卡牌背景）
        /// 颜色比标签颜色更柔和，适合作为背景
        /// </summary>
        private Color GetThemeColorFromTags(string[] tags)
        {
            if (tags == null || tags.Length == 0)
            {
                return new Color(0.9f, 0.9f, 0.9f);  // 默认浅灰色
            }

            // 使用第一个标签决定颜色基调
            string primaryTag = tags[0];
            
            switch (primaryTag)
            {
                case "基酒":
                    return new Color(0.70f, 0.85f, 1.00f);   // 浅蓝色
                case "调味酒":
                    return new Color(1.00f, 0.95f, 0.70f);   // 浅黄色
                case "调味品":
                    return new Color(1.00f, 0.70f, 0.70f);   // 浅红色
                case "新鲜配料":
                    return new Color(0.80f, 1.00f, 0.80f);   // 浅绿色
                case "经典酒":
                    return new Color(0.53f, 0.81f, 0.98f);   // 天蓝色
                case "热带酒":
                    return new Color(1.00f, 0.80f, 0.60f);   // 浅橙色
                case "特调酒":
                    return new Color(0.90f, 0.80f, 1.00f);   // 浅紫色
                default:
                    return new Color(0.9f, 0.9f, 0.9f);     // 默认浅灰色
            }
        }

        private void DrawEffectBar(string label, int value)
        {
            EditorGUILayout.BeginHorizontal();
            
            var labelStyle = new GUIStyle(EditorStyles.label);
            labelStyle.fontStyle = FontStyle.Bold;
            labelStyle.fixedWidth = 60;
            labelStyle.normal.textColor = EditorGUIUtility.isProSkin ? Color.white : new Color(0.1f, 0.1f, 0.1f);
            EditorGUILayout.LabelField(label, labelStyle);
            
            // 颜色根据数值
            Color barColor;
            if (value > 0)
                barColor = new Color(0.3f, 0.9f, 0.4f);
            else if (value < 0)
                barColor = new Color(0.9f, 0.3f, 0.3f);
            else
                barColor = new Color(0.6f, 0.6f, 0.6f);

            // 绘制背景和进度
            Rect rect = EditorGUILayout.GetControlRect(GUILayout.Height(22));
            
            // 背景
            SirenixEditorGUI.DrawSolidRect(rect, new Color(0.2f, 0.2f, 0.2f, 0.3f));
            
            // 进度条
            float normalizedValue = Mathf.Clamp01(Mathf.Abs(value) / 10f);
            Rect fillRect = new Rect(rect.x, rect.y, rect.width * normalizedValue, rect.height);
            SirenixEditorGUI.DrawSolidRect(fillRect, barColor * 0.8f);
            
            // 边框
            SirenixEditorGUI.DrawBorders(rect, 1, new Color(0.3f, 0.3f, 0.3f));
            
            // 文字
            var textStyle = new GUIStyle(GUI.skin.label);
            textStyle.alignment = TextAnchor.MiddleCenter;
            textStyle.fontStyle = FontStyle.Bold;
            textStyle.normal.textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;
            
            string valueText = value > 0 ? $"+{value}" : value.ToString();
            GUI.Label(rect, valueText, textStyle);

            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region 分隔条

        private void DrawVerticalSplitter(ref bool isDragging, ref float width, float minWidth, float maxWidth)
        {
            Rect splitterRect = EditorGUILayout.BeginVertical(GUILayout.Width(4), GUILayout.ExpandHeight(true));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();

            SirenixEditorGUI.DrawSolidRect(splitterRect, new Color(0.3f, 0.3f, 0.3f, 0.5f));
            EditorGUIUtility.AddCursorRect(splitterRect, MouseCursor.ResizeHorizontal);

            if (Event.current.type == EventType.MouseDown && splitterRect.Contains(Event.current.mousePosition))
            {
                isDragging = true;
            }

            if (isDragging && Event.current.type == EventType.MouseDrag)
            {
                width += Event.current.delta.x;
                width = Mathf.Clamp(width, minWidth, maxWidth);
                Repaint();
            }

            if (Event.current.type == EventType.MouseUp)
            {
                isDragging = false;
            }
        }

        private void HandleSplitterDrag()
        {
            if (isDraggingLeftSplitter || isDraggingCenterSplitter)
            {
                Repaint();
            }
        }

        #endregion

        #region JSON导入功能

        private void ImportAllCards()
        {
            logOutput = "开始导入所有卡牌数据\n";
            logOutput += "=====================================\n";
            Repaint();

            bool success = true;
            success &= ImportCocktails();
            success &= ImportMaterials();

            if (success)
            {
                Log("=====================================");
                Log("所有卡牌数据导入完成");
                EditorUtility.DisplayDialog("导入成功", "所有卡牌数据已成功导入并生成ScriptableObject资源", "确定");
            }
            else
            {
                EditorUtility.DisplayDialog("导入失败", "部分数据导入失败，请查看日志", "确定");
            }
        }

        private bool ImportCocktails()
        {
            try
            {
                Log("\n[导入鸡尾酒]");

                if (!File.Exists(COCKTAILS_JSON_PATH))
                {
                    LogError($"找不到JSON文件: {COCKTAILS_JSON_PATH}");
                    return false;
                }

                string jsonContent = File.ReadAllText(COCKTAILS_JSON_PATH);
                CocktailsWrapper cocktailsData = JsonUtility.FromJson<CocktailsWrapper>(jsonContent);

                if (cocktailsData == null || cocktailsData.cocktails == null)
                {
                    LogError("JSON解析失败或数据为空");
                    return false;
                }

                Log($"解析成功，找到 {cocktailsData.cocktails.Count} 个鸡尾酒");

                EnsureFolderExists(COCKTAILS_FOLDER);

                int successCount = 0;
                int spriteLoadedCount = 0;
                int skippedCount = 0;
                int updatedCount = 0;
                
                for (int i = 0; i < cocktailsData.cocktails.Count; i++)
                {
                    var jsonData = cocktailsData.cocktails[i];
                    EditorUtility.DisplayProgressBar("导入鸡尾酒", 
                        $"正在导入: {jsonData.name_cn}", 
                        (float)i / cocktailsData.cocktails.Count);

                    var result = CreateCocktailSO(jsonData, i + 1, out bool spriteLoaded);
                    if (result == ImportResult.Created || result == ImportResult.Updated)
                    {
                        successCount++;
                        if (spriteLoaded) spriteLoadedCount++;
                        if (result == ImportResult.Updated) updatedCount++;
                    }
                    else if (result == ImportResult.Skipped)
                    {
                        skippedCount++;
                    }
                }

                EditorUtility.ClearProgressBar();

                Log($"鸡尾酒导入完成: {successCount}/{cocktailsData.cocktails.Count} 成功");
                if (incrementalUpdate)
                {
                    Log($"  新建: {successCount - updatedCount} | 更新: {updatedCount} | 跳过: {skippedCount}");
                }
                Log($"  精灵加载: {spriteLoadedCount}/{successCount}");
                
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                LoadAllCards();
                return true;
            }
            catch (System.Exception e)
            {
                EditorUtility.ClearProgressBar();
                LogError($"导入鸡尾酒时发生错误: {e.Message}");
                Debug.LogException(e);
                return false;
            }
        }

        private bool ImportMaterials()
        {
            try
            {
                Log("\n[导入材料]");

                if (!File.Exists(MATERIALS_JSON_PATH))
                {
                    LogError($"找不到JSON文件: {MATERIALS_JSON_PATH}");
                    return false;
                }

                string jsonContent = File.ReadAllText(MATERIALS_JSON_PATH);
                MaterialsWrapper materialsData = JsonUtility.FromJson<MaterialsWrapper>(jsonContent);

                if (materialsData == null || materialsData.materials == null)
                {
                    LogError("JSON解析失败或数据为空");
                    return false;
                }

                Log($"解析成功，找到 {materialsData.materials.Count} 个材料");

                EnsureFolderExists(MATERIALS_FOLDER);

                int successCount = 0;
                int spriteLoadedCount = 0;
                int skippedCount = 0;
                int updatedCount = 0;
                
                for (int i = 0; i < materialsData.materials.Count; i++)
                {
                    var jsonData = materialsData.materials[i];
                    EditorUtility.DisplayProgressBar("导入材料", 
                        $"正在导入: {jsonData.name_cn}", 
                        (float)i / materialsData.materials.Count);

                    var result = CreateMaterialSO(jsonData, out bool spriteLoaded);
                    if (result == ImportResult.Created || result == ImportResult.Updated)
                    {
                        successCount++;
                        if (spriteLoaded) spriteLoadedCount++;
                        if (result == ImportResult.Updated) updatedCount++;
                    }
                    else if (result == ImportResult.Skipped)
                    {
                        skippedCount++;
                    }
                }

                EditorUtility.ClearProgressBar();

                Log($"材料导入完成: {successCount}/{materialsData.materials.Count} 成功");
                if (incrementalUpdate)
                {
                    Log($"  新建: {successCount - updatedCount} | 更新: {updatedCount} | 跳过: {skippedCount}");
                }
                Log($"  精灵加载: {spriteLoadedCount}/{successCount}");
                
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                LoadAllCards();
                return true;
            }
            catch (System.Exception e)
            {
                EditorUtility.ClearProgressBar();
                LogError($"导入材料时发生错误: {e.Message}");
                Debug.LogException(e);
                return false;
            }
        }

        #endregion

        #region 手动创建功能

        private void CreateCardManually()
        {
            logOutput = "开始手动创建卡牌\n";
            logOutput += "=====================================\n";
            
            try
            {
                if (createCardType == CardType.Cocktail)
                {
                    CreateCocktailManually();
                }
                else
                {
                    CreateMaterialManually();
                }
                
                EditorUtility.DisplayDialog("创建成功", "卡牌已成功创建并写入JSON", "确定");
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("创建失败", $"创建卡牌时发生错误：{e.Message}", "确定");
                LogError($"创建失败: {e.Message}");
                Debug.LogException(e);
            }
        }

        private void CreateCocktailManually()
        {
            if (string.IsNullOrEmpty(cocktailFormData.name_cn) || string.IsNullOrEmpty(cocktailFormData.name_en))
            {
                LogError("名称不能为空");
                return;
            }

            CocktailsWrapper wrapper;
            if (File.Exists(COCKTAILS_JSON_PATH))
            {
                string json = File.ReadAllText(COCKTAILS_JSON_PATH);
                wrapper = JsonUtility.FromJson<CocktailsWrapper>(json);
            }
            else
            {
                wrapper = new CocktailsWrapper { cocktails = new List<JsonCocktailData>() };
            }

            wrapper.cocktails.Add(cocktailFormData);
            
            string newJson = JsonUtility.ToJson(wrapper, true);
            File.WriteAllText(COCKTAILS_JSON_PATH, newJson);
            AssetDatabase.Refresh();

            Log($"已写入JSON: {cocktailFormData.name_cn}");

            int id = wrapper.cocktails.Count;
            CreateCocktailSO(cocktailFormData, id, out bool spriteLoaded);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Log($"已创建SO: Cocktail_{id:D3}_{cocktailFormData.name_cn}");
            Log("创建完成");

            LoadAllCards();
            cocktailFormData = new JsonCocktailData { effects = new JsonCardEffects() };
        }

        private void CreateMaterialManually()
        {
            if (string.IsNullOrEmpty(materialFormData.name_cn) || string.IsNullOrEmpty(materialFormData.name_en))
            {
                LogError("名称不能为空");
                return;
            }

            if (materialFormData.id <= 0)
            {
                LogError("ID必须大于0");
                return;
            }

            MaterialsWrapper wrapper;
            if (File.Exists(MATERIALS_JSON_PATH))
            {
                string json = File.ReadAllText(MATERIALS_JSON_PATH);
                wrapper = JsonUtility.FromJson<MaterialsWrapper>(json);
            }
            else
            {
                wrapper = new MaterialsWrapper { materials = new List<JsonMaterialData>() };
            }

            if (wrapper.materials.Exists(m => m.id == materialFormData.id))
            {
                LogError($"ID {materialFormData.id} 已存在");
                return;
            }

            wrapper.materials.Add(materialFormData);
            
            string newJson = JsonUtility.ToJson(wrapper, true);
            File.WriteAllText(MATERIALS_JSON_PATH, newJson);
            AssetDatabase.Refresh();

            Log($"已写入JSON: {materialFormData.name_cn}");

            CreateMaterialSO(materialFormData, out bool spriteLoaded);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Log($"已创建SO: Material_{materialFormData.id:D3}_{materialFormData.name_cn}");
            Log("创建完成");

            LoadAllCards();
            materialFormData = new JsonMaterialData { effects = new JsonCardEffects() };
        }

        #endregion

        #region 强制更新功能

        private void ForceUpdateAll()
        {
            logOutput = "开始强制更新所有卡牌\n";
            logOutput += "=====================================\n";
            
            ForceUpdateCocktails();
            ForceUpdateMaterials();
            
            Log("=====================================");
            Log("强制更新完成");
            EditorUtility.DisplayDialog("更新完成", "所有卡牌已强制更新", "确定");
        }

        private void ForceUpdateCocktails()
        {
            try
            {
                Log("\n[强制更新鸡尾酒]");

                if (!File.Exists(COCKTAILS_JSON_PATH))
                {
                    LogError($"找不到JSON文件: {COCKTAILS_JSON_PATH}");
                    return;
                }

                string jsonContent = File.ReadAllText(COCKTAILS_JSON_PATH);
                CocktailsWrapper cocktailsData = JsonUtility.FromJson<CocktailsWrapper>(jsonContent);

                if (cocktailsData == null || cocktailsData.cocktails == null)
                {
                    LogError("JSON解析失败或数据为空");
                    return;
                }

                EnsureFolderExists(COCKTAILS_FOLDER);

                bool oldIncrementalSetting = incrementalUpdate;
                incrementalUpdate = false;

                int successCount = 0;
                for (int i = 0; i < cocktailsData.cocktails.Count; i++)
                {
                    var jsonData = cocktailsData.cocktails[i];
                    EditorUtility.DisplayProgressBar("强制更新鸡尾酒", 
                        $"正在更新: {jsonData.name_cn}", 
                        (float)i / cocktailsData.cocktails.Count);

                    string fileName = $"Cocktail_{i + 1:D3}_{jsonData.name_cn}.asset";
                    string assetPath = Path.Combine(COCKTAILS_FOLDER, fileName);

                    if (File.Exists(assetPath))
                    {
                        AssetDatabase.DeleteAsset(assetPath);
                    }

                    CreateCocktailSO(jsonData, i + 1, out bool spriteLoaded);
                    successCount++;
                    Log($"  强制更新: {fileName}");
                }

                EditorUtility.ClearProgressBar();
                incrementalUpdate = oldIncrementalSetting;

                Log($"鸡尾酒强制更新完成: {successCount}/{cocktailsData.cocktails.Count}");
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                LoadAllCards();
            }
            catch (System.Exception e)
            {
                EditorUtility.ClearProgressBar();
                LogError($"强制更新鸡尾酒时发生错误: {e.Message}");
                Debug.LogException(e);
            }
        }

        private void ForceUpdateMaterials()
        {
            try
            {
                Log("\n[强制更新材料]");

                if (!File.Exists(MATERIALS_JSON_PATH))
                {
                    LogError($"找不到JSON文件: {MATERIALS_JSON_PATH}");
                    return;
                }

                string jsonContent = File.ReadAllText(MATERIALS_JSON_PATH);
                MaterialsWrapper materialsData = JsonUtility.FromJson<MaterialsWrapper>(jsonContent);

                if (materialsData == null || materialsData.materials == null)
                {
                    LogError("JSON解析失败或数据为空");
                    return;
                }

                EnsureFolderExists(MATERIALS_FOLDER);

                bool oldIncrementalSetting = incrementalUpdate;
                incrementalUpdate = false;

                int successCount = 0;
                for (int i = 0; i < materialsData.materials.Count; i++)
                {
                    var jsonData = materialsData.materials[i];
                    EditorUtility.DisplayProgressBar("强制更新材料", 
                        $"正在更新: {jsonData.name_cn}", 
                        (float)i / materialsData.materials.Count);

                    string fileName = $"Material_{jsonData.id:D3}_{jsonData.name_cn}.asset";
                    string assetPath = Path.Combine(MATERIALS_FOLDER, fileName);

                    if (File.Exists(assetPath))
                    {
                        AssetDatabase.DeleteAsset(assetPath);
                    }

                    CreateMaterialSO(jsonData, out bool spriteLoaded);
                    successCount++;
                    Log($"  强制更新: {fileName}");
                }

                EditorUtility.ClearProgressBar();
                incrementalUpdate = oldIncrementalSetting;

                Log($"材料强制更新完成: {successCount}/{materialsData.materials.Count}");
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                LoadAllCards();
            }
            catch (System.Exception e)
            {
                EditorUtility.ClearProgressBar();
                LogError($"强制更新材料时发生错误: {e.Message}");
                Debug.LogException(e);
            }
        }

        #endregion

        #region SO创建方法

        private ImportResult CreateCocktailSO(JsonCocktailData jsonData, int id, out bool spriteLoaded)
        {
            spriteLoaded = false;
            try
            {
                string fileName = $"Cocktail_{id:D3}_{jsonData.name_cn}.asset";
                string assetPath = Path.Combine(COCKTAILS_FOLDER, fileName);

                CocktailCardSO existingCocktail = null;
                bool isUpdate = false;
                if (incrementalUpdate && File.Exists(assetPath))
                {
                    existingCocktail = AssetDatabase.LoadAssetAtPath<CocktailCardSO>(assetPath);
                    if (existingCocktail != null)
                    {
                        if (IsCocktailDataSame(existingCocktail, jsonData, id))
                        {
                            Log($"  跳过: {fileName} (未修改)");
                            return ImportResult.Skipped;
                        }
                        isUpdate = true;
                    }
                }

                CocktailCardSO cocktail = existingCocktail ?? ScriptableObject.CreateInstance<CocktailCardSO>();

                cocktail.id = id;
                cocktail.nameCN = jsonData.name_cn;
                cocktail.nameEN = jsonData.name_en;
                cocktail.category = jsonData.category;
                cocktail.tags = jsonData.tags ?? new string[0];  // 标签处理
                cocktail.themeColor = GetThemeColorFromTags(cocktail.tags);  // 根据标签设置颜色基调
                cocktail.feature = jsonData.feature;
                cocktail.uiPath = jsonData.ui_path;

                if (!string.IsNullOrEmpty(jsonData.ui_path))
                {
                    Sprite sprite = Resources.Load<Sprite>(jsonData.ui_path);
                    if (sprite != null)
                    {
                        cocktail.uiSpritePreview = sprite;
                        spriteLoaded = true;
                    }
                    else
                    {
                        string spriteAssetPath = $"Assets/Resources/{jsonData.ui_path}.png";
                        sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spriteAssetPath);
                        if (sprite != null)
                        {
                            cocktail.uiSpritePreview = sprite;
                            spriteLoaded = true;
                        }
                    }
                }

                cocktail.effects = new CardEffects
                {
                    busy = jsonData.effects.busy,
                    impatient = jsonData.effects.impatient,
                    bored = jsonData.effects.bored,
                    picky = jsonData.effects.picky,
                    friendly = jsonData.effects.friendly
                };

                cocktail.cost = jsonData.cost;
                cocktail.price = jsonData.price;
                cocktail.profit = jsonData.profit;
                cocktail.reputationChange = jsonData.reputation_change;

                if (!isUpdate)
                {
                    AssetDatabase.CreateAsset(cocktail, assetPath);
                    Log($"  创建: {fileName}");
                }
                else
                {
                    EditorUtility.SetDirty(cocktail);
                    Log($"  更新: {fileName}");
                }
                
                return isUpdate ? ImportResult.Updated : ImportResult.Created;
            }
            catch (System.Exception e)
            {
                LogError($"创建鸡尾酒SO失败 ({jsonData.name_cn}): {e.Message}");
                return ImportResult.Failed;
            }
        }

        private ImportResult CreateMaterialSO(JsonMaterialData jsonData, out bool spriteLoaded)
        {
            spriteLoaded = false;
            try
            {
                string fileName = $"Material_{jsonData.id:D3}_{jsonData.name_cn}.asset";
                string assetPath = Path.Combine(MATERIALS_FOLDER, fileName);

                MaterialCardSO existingMaterial = null;
                bool isUpdate = false;
                if (incrementalUpdate && File.Exists(assetPath))
                {
                    existingMaterial = AssetDatabase.LoadAssetAtPath<MaterialCardSO>(assetPath);
                    if (existingMaterial != null)
                    {
                        if (IsMaterialDataSame(existingMaterial, jsonData))
                        {
                            Log($"  跳过: {fileName} (未修改)");
                            return ImportResult.Skipped;
                        }
                        isUpdate = true;
                    }
                }

                MaterialCardSO material = existingMaterial ?? ScriptableObject.CreateInstance<MaterialCardSO>();

                material.id = jsonData.id;
                material.nameCN = jsonData.name_cn;
                material.nameEN = jsonData.name_en;
                material.category = jsonData.category;
                material.tags = jsonData.tags ?? new string[0];  // 标签处理
                material.themeColor = GetThemeColorFromTags(material.tags);  // 根据标签设置颜色基调
                material.feature = jsonData.feature;
                material.uiPath = jsonData.ui_path;

                if (!string.IsNullOrEmpty(jsonData.ui_path))
                {
                    Sprite sprite = Resources.Load<Sprite>(jsonData.ui_path);
                    if (sprite != null)
                    {
                        material.uiSpritePreview = sprite;
                        spriteLoaded = true;
                    }
                    else
                    {
                        string spriteAssetPath = $"Assets/Resources/{jsonData.ui_path}.png";
                        sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spriteAssetPath);
                        if (sprite != null)
                        {
                            material.uiSpritePreview = sprite;
                            spriteLoaded = true;
                        }
                    }
                }

                material.effects = new CardEffects
                {
                    busy = jsonData.effects.busy,
                    impatient = jsonData.effects.impatient,
                    bored = jsonData.effects.bored,
                    picky = jsonData.effects.picky,
                    friendly = jsonData.effects.friendly
                };

                material.price = jsonData.price;

                if (!isUpdate)
                {
                    AssetDatabase.CreateAsset(material, assetPath);
                    Log($"  创建: {fileName}");
                }
                else
                {
                    EditorUtility.SetDirty(material);
                    Log($"  更新: {fileName}");
                }
                
                return isUpdate ? ImportResult.Updated : ImportResult.Created;
            }
            catch (System.Exception e)
            {
                LogError($"创建材料SO失败 ({jsonData.name_cn}): {e.Message}");
                return ImportResult.Failed;
            }
        }

        #endregion

        #region 数据比较方法

        private bool IsCocktailDataSame(CocktailCardSO existing, JsonCocktailData jsonData, int id)
        {
            return existing.id == id &&
                   existing.nameCN == jsonData.name_cn &&
                   existing.nameEN == jsonData.name_en &&
                   existing.category == jsonData.category &&
                   existing.feature == jsonData.feature &&
                   existing.uiPath == jsonData.ui_path &&
                   existing.effects.busy == jsonData.effects.busy &&
                   existing.effects.impatient == jsonData.effects.impatient &&
                   existing.effects.bored == jsonData.effects.bored &&
                   existing.effects.picky == jsonData.effects.picky &&
                   existing.effects.friendly == jsonData.effects.friendly &&
                   existing.cost == jsonData.cost &&
                   existing.price == jsonData.price &&
                   existing.profit == jsonData.profit &&
                   existing.reputationChange == jsonData.reputation_change;
        }

        private bool IsMaterialDataSame(MaterialCardSO existing, JsonMaterialData jsonData)
        {
            return existing.id == jsonData.id &&
                   existing.nameCN == jsonData.name_cn &&
                   existing.nameEN == jsonData.name_en &&
                   existing.category == jsonData.category &&
                   existing.feature == jsonData.feature &&
                   existing.uiPath == jsonData.ui_path &&
                   existing.effects.busy == jsonData.effects.busy &&
                   existing.effects.impatient == jsonData.effects.impatient &&
                   existing.effects.bored == jsonData.effects.bored &&
                   existing.effects.picky == jsonData.effects.picky &&
                   existing.effects.friendly == jsonData.effects.friendly &&
                   existing.price == jsonData.price;
        }

        #endregion

        #region 加载卡牌列表

        private void LoadAllCards()
        {
            loadedCocktails.Clear();
            loadedMaterials.Clear();

            string[] cocktailGuids = AssetDatabase.FindAssets("t:CocktailCardSO", new[] { COCKTAILS_FOLDER });
            foreach (string guid in cocktailGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                CocktailCardSO cocktail = AssetDatabase.LoadAssetAtPath<CocktailCardSO>(path);
                if (cocktail != null)
                {
                    loadedCocktails.Add(cocktail);
                }
            }

            loadedCocktails.Sort((a, b) => a.id.CompareTo(b.id));

            string[] materialGuids = AssetDatabase.FindAssets("t:MaterialCardSO", new[] { MATERIALS_FOLDER });
            foreach (string guid in materialGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                MaterialCardSO material = AssetDatabase.LoadAssetAtPath<MaterialCardSO>(path);
                if (material != null)
                {
                    loadedMaterials.Add(material);
                }
            }

            loadedMaterials.Sort((a, b) => a.id.CompareTo(b.id));

            Debug.Log($"[CardMaker] 已加载 {loadedCocktails.Count} 个鸡尾酒, {loadedMaterials.Count} 个材料");
        }

        #endregion

        #region 辅助方法

        private void EnsureFolderExists(string folderPath)
        {
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                string parentFolder = Path.GetDirectoryName(folderPath).Replace("\\", "/");
                string newFolderName = Path.GetFileName(folderPath);

                if (!AssetDatabase.IsValidFolder(parentFolder))
                {
                    EnsureFolderExists(parentFolder);
                }

                AssetDatabase.CreateFolder(parentFolder, newFolderName);
                Log($"创建文件夹: {folderPath}");
            }
        }

        private void Log(string message)
        {
            logOutput += message + "\n";
            Debug.Log("[CardMaker] " + message);
            Repaint();
        }

        private void LogError(string message)
        {
            logOutput += "[错误] " + message + "\n";
            Debug.LogError("[CardMaker] " + message);
            Repaint();
        }

        #endregion
    }
}
