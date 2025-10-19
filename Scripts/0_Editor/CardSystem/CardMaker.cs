using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using TabernaNoctis.Cards;

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

        #endregion

        #region 窗口变量

        [MenuItem("自制工具/卡牌系统/卡牌制作器")]
        public static void OpenCardMaker()
        {
            var window = GetWindow<CardMaker>("卡牌制作器");
            window.minSize = new Vector2(1200, 600);
            window.Show();
        }

        // 布局
        private float leftPanelWidth = 400f;
        private float centerPanelWidth = 400f;
        private bool isDraggingLeftSplitter = false;
        private bool isDraggingCenterSplitter = false;

        // 左侧板块
        private LeftPanelTab leftTab = LeftPanelTab.JSONImport;
        private Vector2 leftScrollPos;
        private string logOutput = "准备开始...\n";
        private bool incrementalUpdate = true;

        // 手动创建表单
        private CardType createCardType = CardType.Cocktail;
        private JsonCocktailData cocktailFormData = new JsonCocktailData { effects = new JsonCardEffects() };
        private JsonMaterialData materialFormData = new JsonMaterialData { effects = new JsonCardEffects() };

        // 中间板块
        private Vector2 centerScrollPos;
        
        // 右侧板块
        private Vector2 rightScrollPos;

        #endregion

        #region GUI绘制

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            // 左侧板块
            DrawLeftPanel();

            // 左侧分隔条
            DrawVerticalSplitter(ref isDraggingLeftSplitter, ref leftPanelWidth, 300f, 600f);

            // 中间板块
            DrawCenterPanel();

            // 中间分隔条
            DrawVerticalSplitter(ref isDraggingCenterSplitter, ref centerPanelWidth, 300f, 600f);

            // 右侧板块
            DrawRightPanel();

            EditorGUILayout.EndHorizontal();

            // 处理拖拽
            HandleSplitterDrag();
        }

        #endregion

        #region 左侧板块

        private void DrawLeftPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(leftPanelWidth), GUILayout.ExpandHeight(true));
            
            GUILayout.Label("创建工具", EditorStyles.boldLabel);
            
            // Tab切换
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Toggle(leftTab == LeftPanelTab.JSONImport, "JSON导入", "Button"))
                leftTab = LeftPanelTab.JSONImport;
            if (GUILayout.Toggle(leftTab == LeftPanelTab.ManualCreate, "手动创建", "Button"))
                leftTab = LeftPanelTab.ManualCreate;
            if (GUILayout.Toggle(leftTab == LeftPanelTab.ForceUpdate, "强制更新", "Button"))
                leftTab = LeftPanelTab.ForceUpdate;
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
        }

        private void DrawJSONImportTab()
        {
            EditorGUILayout.HelpBox(
                "从JSON文件导入卡牌数据并生成ScriptableObject资源。\n" +
                "源文件：\n" +
                "- " + COCKTAILS_JSON_PATH + "\n" +
                "- " + MATERIALS_JSON_PATH,
                MessageType.Info);

            GUILayout.Space(10);

            // 增量更新选项
            EditorGUILayout.BeginHorizontal();
            incrementalUpdate = EditorGUILayout.Toggle("增量更新", incrementalUpdate, GUILayout.Width(200));
            EditorGUILayout.LabelField(incrementalUpdate ? "只更新有变化的资源" : "覆盖所有现有资源", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(10);

            if (GUILayout.Button("导入所有卡牌数据", GUILayout.Height(40)))
            {
                ImportAllCards();
            }

            GUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("仅导入鸡尾酒", GUILayout.Height(30)))
            {
                ImportCocktails();
            }
            if (GUILayout.Button("仅导入材料", GUILayout.Height(30)))
            {
                ImportMaterials();
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(20);
            GUILayout.Label("导入日志:", EditorStyles.boldLabel);
            EditorGUILayout.TextArea(logOutput, GUILayout.ExpandHeight(true));
        }

        private void DrawManualCreateTab()
        {
            GUILayout.Label("手动创建卡牌", EditorStyles.boldLabel);
            
            EditorGUILayout.HelpBox(
                "手动输入数据创建卡牌并自动写入JSON文件", 
                MessageType.Info);

            GUILayout.Space(10);

            // 卡牌类型选择
            createCardType = (CardType)EditorGUILayout.EnumPopup("卡牌类型", createCardType);

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
            
            if (GUILayout.Button("创建卡牌并写入JSON", GUILayout.Height(40)))
            {
                CreateCardManually();
            }

            GUILayout.Space(10);
            GUILayout.Label("日志:", EditorStyles.boldLabel);
            EditorGUILayout.TextArea(logOutput, GUILayout.Height(150));
        }

        private void DrawCocktailForm()
        {
            GUILayout.Label("鸡尾酒信息", EditorStyles.boldLabel);
            
            cocktailFormData.name_cn = EditorGUILayout.TextField("中文名", cocktailFormData.name_cn);
            cocktailFormData.name_en = EditorGUILayout.TextField("英文名", cocktailFormData.name_en);
            cocktailFormData.category = EditorGUILayout.TextField("分类", cocktailFormData.category);
            cocktailFormData.feature = EditorGUILayout.TextField("特点", cocktailFormData.feature);
            cocktailFormData.ui_path = EditorGUILayout.TextField("UI路径", cocktailFormData.ui_path);

            GUILayout.Space(5);
            GUILayout.Label("效果数值", EditorStyles.boldLabel);
            cocktailFormData.effects.busy = EditorGUILayout.IntField("忙碌", cocktailFormData.effects.busy);
            cocktailFormData.effects.impatient = EditorGUILayout.IntField("急躁", cocktailFormData.effects.impatient);
            cocktailFormData.effects.bored = EditorGUILayout.IntField("烦闷", cocktailFormData.effects.bored);
            cocktailFormData.effects.picky = EditorGUILayout.IntField("挑剔", cocktailFormData.effects.picky);
            cocktailFormData.effects.friendly = EditorGUILayout.IntField("友好", cocktailFormData.effects.friendly);

            GUILayout.Space(5);
            GUILayout.Label("经济数据", EditorStyles.boldLabel);
            cocktailFormData.cost = EditorGUILayout.IntField("成本", cocktailFormData.cost);
            cocktailFormData.price = EditorGUILayout.IntField("售价", cocktailFormData.price);
            cocktailFormData.profit = EditorGUILayout.IntField("利润", cocktailFormData.profit);
            cocktailFormData.reputation_change = EditorGUILayout.IntField("评价变化", cocktailFormData.reputation_change);
        }

        private void DrawMaterialForm()
        {
            GUILayout.Label("材料信息", EditorStyles.boldLabel);
            
            materialFormData.id = EditorGUILayout.IntField("ID", materialFormData.id);
            materialFormData.name_cn = EditorGUILayout.TextField("中文名", materialFormData.name_cn);
            materialFormData.name_en = EditorGUILayout.TextField("英文名", materialFormData.name_en);
            materialFormData.category = EditorGUILayout.TextField("分类", materialFormData.category);
            materialFormData.feature = EditorGUILayout.TextField("特点", materialFormData.feature);
            materialFormData.ui_path = EditorGUILayout.TextField("UI路径", materialFormData.ui_path);

            GUILayout.Space(5);
            GUILayout.Label("效果数值", EditorStyles.boldLabel);
            materialFormData.effects.busy = EditorGUILayout.IntField("忙碌", materialFormData.effects.busy);
            materialFormData.effects.impatient = EditorGUILayout.IntField("急躁", materialFormData.effects.impatient);
            materialFormData.effects.bored = EditorGUILayout.IntField("烦闷", materialFormData.effects.bored);
            materialFormData.effects.picky = EditorGUILayout.IntField("挑剔", materialFormData.effects.picky);
            materialFormData.effects.friendly = EditorGUILayout.IntField("友好", materialFormData.effects.friendly);

            GUILayout.Space(5);
            GUILayout.Label("经济数据", EditorStyles.boldLabel);
            materialFormData.price = EditorGUILayout.IntField("价格", materialFormData.price);
        }

        private void DrawForceUpdateTab()
        {
            GUILayout.Label("强制更新模式", EditorStyles.boldLabel);
            
            EditorGUILayout.HelpBox(
                "遍历JSON文件中的所有数据，强制替换现有的ScriptableObject，\n" +
                "即使数据相同也会重新创建。用于修复数据不一致问题。",
                MessageType.Warning);

            GUILayout.Space(10);

            if (GUILayout.Button("强制更新所有鸡尾酒", GUILayout.Height(40)))
            {
                ForceUpdateCocktails();
            }

            GUILayout.Space(5);

            if (GUILayout.Button("强制更新所有材料", GUILayout.Height(40)))
            {
                ForceUpdateMaterials();
            }

            GUILayout.Space(5);

            if (GUILayout.Button("强制更新全部卡牌", GUILayout.Height(40)))
            {
                ForceUpdateAll();
            }

            GUILayout.Space(20);
            GUILayout.Label("更新日志:", EditorStyles.boldLabel);
            EditorGUILayout.TextArea(logOutput, GUILayout.ExpandHeight(true));
        }

        #endregion

        #region 中间板块

        private void DrawCenterPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(centerPanelWidth), GUILayout.ExpandHeight(true));
            
            GUILayout.Label("中间板块", EditorStyles.boldLabel);
            
            centerScrollPos = EditorGUILayout.BeginScrollView(centerScrollPos);
            
            EditorGUILayout.HelpBox("此区域预留用于未来功能", MessageType.Info);
            
            EditorGUILayout.EndScrollView();
            
            EditorGUILayout.EndVertical();
        }

        #endregion

        #region 右侧板块

        private void DrawRightPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            
            GUILayout.Label("右侧板块", EditorStyles.boldLabel);
            
            rightScrollPos = EditorGUILayout.BeginScrollView(rightScrollPos);
            
            EditorGUILayout.HelpBox("此区域预留用于未来功能", MessageType.Info);
            
            EditorGUILayout.EndScrollView();
            
            EditorGUILayout.EndVertical();
        }

        #endregion

        #region 分隔条

        private void DrawVerticalSplitter(ref bool isDragging, ref float width, float minWidth, float maxWidth)
        {
            Rect splitterRect = EditorGUILayout.BeginVertical(GUILayout.Width(5), GUILayout.ExpandHeight(true));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();

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

        #region JSON导入功能（保留原有）

        private void ImportAllCards()
        {
            logOutput = "=== 开始导入所有卡牌数据 ===\n";
            Repaint();

            bool success = true;
            success &= ImportCocktails();
            success &= ImportMaterials();

            if (success)
            {
                Log("=== 所有卡牌数据导入完成！ ===");
                EditorUtility.DisplayDialog("导入成功", "所有卡牌数据已成功导入并生成ScriptableObject资源！", "确定");
            }
            else
            {
                EditorUtility.DisplayDialog("导入失败", "部分数据导入失败，请查看日志。", "确定");
            }
        }

        private bool ImportCocktails()
        {
            try
            {
                Log("\n--- 开始导入鸡尾酒数据 ---");

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

                Log($"成功解析JSON，找到 {cocktailsData.cocktails.Count} 个鸡尾酒");

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

                Log($"✓ 鸡尾酒导入完成: {successCount}/{cocktailsData.cocktails.Count} 成功");
                if (incrementalUpdate)
                {
                    Log($"  ├─ 新建: {successCount - updatedCount} 个");
                    Log($"  ├─ 更新: {updatedCount} 个");
                    Log($"  └─ 跳过: {skippedCount} 个（未修改）");
                }
                Log($"  └─ 精灵加载: {spriteLoadedCount}/{successCount} 成功");
                
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
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
                Log("\n--- 开始导入材料数据 ---");

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

                Log($"成功解析JSON，找到 {materialsData.materials.Count} 个材料");

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

                Log($"✓ 材料导入完成: {successCount}/{materialsData.materials.Count} 成功");
                if (incrementalUpdate)
                {
                    Log($"  ├─ 新建: {successCount - updatedCount} 个");
                    Log($"  ├─ 更新: {updatedCount} 个");
                    Log($"  └─ 跳过: {skippedCount} 个（未修改）");
                }
                Log($"  └─ 精灵加载: {spriteLoadedCount}/{successCount} 成功");
                
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
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

        #region 手动创建功能（新增）

        private void CreateCardManually()
        {
            logOutput = "=== 开始手动创建卡牌 ===\n";
            
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
                
                EditorUtility.DisplayDialog("创建成功", "卡牌已成功创建并写入JSON！", "确定");
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
            // 验证数据
            if (string.IsNullOrEmpty(cocktailFormData.name_cn) || string.IsNullOrEmpty(cocktailFormData.name_en))
            {
                LogError("名称不能为空！");
                return;
            }

            // 读取现有JSON
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

            // 添加到列表
            wrapper.cocktails.Add(cocktailFormData);
            
            // 写入JSON
            string newJson = JsonUtility.ToJson(wrapper, true);
            File.WriteAllText(COCKTAILS_JSON_PATH, newJson);
            AssetDatabase.Refresh();

            Log($"✓ 已写入JSON: {cocktailFormData.name_cn}");

            // 创建SO
            int id = wrapper.cocktails.Count;
            CreateCocktailSO(cocktailFormData, id, out bool spriteLoaded);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Log($"✓ 已创建SO: Cocktail_{id:D3}_{cocktailFormData.name_cn}");
            Log("=== 创建完成 ===");

            // 重置表单
            cocktailFormData = new JsonCocktailData { effects = new JsonCardEffects() };
        }

        private void CreateMaterialManually()
        {
            // 验证数据
            if (string.IsNullOrEmpty(materialFormData.name_cn) || string.IsNullOrEmpty(materialFormData.name_en))
            {
                LogError("名称不能为空！");
                return;
            }

            if (materialFormData.id <= 0)
            {
                LogError("ID必须大于0！");
                return;
            }

            // 读取现有JSON
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

            // 检查ID是否已存在
            if (wrapper.materials.Exists(m => m.id == materialFormData.id))
            {
                LogError($"ID {materialFormData.id} 已存在！");
                return;
            }

            // 添加到列表
            wrapper.materials.Add(materialFormData);
            
            // 写入JSON
            string newJson = JsonUtility.ToJson(wrapper, true);
            File.WriteAllText(MATERIALS_JSON_PATH, newJson);
            AssetDatabase.Refresh();

            Log($"✓ 已写入JSON: {materialFormData.name_cn}");

            // 创建SO
            CreateMaterialSO(materialFormData, out bool spriteLoaded);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Log($"✓ 已创建SO: Material_{materialFormData.id:D3}_{materialFormData.name_cn}");
            Log("=== 创建完成 ===");

            // 重置表单
            materialFormData = new JsonMaterialData { effects = new JsonCardEffects() };
        }

        #endregion

        #region 强制更新功能（新增）

        private void ForceUpdateAll()
        {
            logOutput = "=== 开始强制更新所有卡牌 ===\n";
            
            ForceUpdateCocktails();
            ForceUpdateMaterials();
            
            Log("=== 强制更新完成 ===");
            EditorUtility.DisplayDialog("更新完成", "所有卡牌已强制更新！", "确定");
        }

        private void ForceUpdateCocktails()
        {
            try
            {
                Log("\n--- 强制更新鸡尾酒 ---");

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

                // 临时关闭增量更新
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

                    // 删除现有资源
                    if (File.Exists(assetPath))
                    {
                        AssetDatabase.DeleteAsset(assetPath);
                    }

                    // 重新创建
                    CreateCocktailSO(jsonData, i + 1, out bool spriteLoaded);
                    successCount++;
                    Log($"  ↻ 强制更新: {fileName}");
                }

                EditorUtility.ClearProgressBar();
                
                // 恢复设置
                incrementalUpdate = oldIncrementalSetting;

                Log($"✓ 鸡尾酒强制更新完成: {successCount}/{cocktailsData.cocktails.Count}");
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
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
                Log("\n--- 强制更新材料 ---");

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

                // 临时关闭增量更新
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

                    // 删除现有资源
                    if (File.Exists(assetPath))
                    {
                        AssetDatabase.DeleteAsset(assetPath);
                    }

                    // 重新创建
                    CreateMaterialSO(jsonData, out bool spriteLoaded);
                    successCount++;
                    Log($"  ↻ 强制更新: {fileName}");
                }

                EditorUtility.ClearProgressBar();
                
                // 恢复设置
                incrementalUpdate = oldIncrementalSetting;

                Log($"✓ 材料强制更新完成: {successCount}/{materialsData.materials.Count}");
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            catch (System.Exception e)
            {
                EditorUtility.ClearProgressBar();
                LogError($"强制更新材料时发生错误: {e.Message}");
                Debug.LogException(e);
            }
        }

        #endregion

        #region SO创建方法（保留原有）

        private ImportResult CreateCocktailSO(JsonCocktailData jsonData, int id, out bool spriteLoaded)
        {
            spriteLoaded = false;
            try
            {
                string fileName = $"Cocktail_{id:D3}_{jsonData.name_cn}.asset";
                string assetPath = Path.Combine(COCKTAILS_FOLDER, fileName);

                // 增量更新：检查现有资源
                CocktailCardSO existingCocktail = null;
                bool isUpdate = false;
                if (incrementalUpdate && File.Exists(assetPath))
                {
                    existingCocktail = AssetDatabase.LoadAssetAtPath<CocktailCardSO>(assetPath);
                    if (existingCocktail != null)
                    {
                        // 比较数据是否相同
                        if (IsCocktailDataSame(existingCocktail, jsonData, id))
                        {
                            Log($"  ⊙ 跳过: {fileName} (未修改)");
                            return ImportResult.Skipped;
                        }
                        isUpdate = true;
                    }
                }

                // 创建或使用现有实例
                CocktailCardSO cocktail = existingCocktail ?? ScriptableObject.CreateInstance<CocktailCardSO>();

                // 映射数据
                cocktail.id = id;
                cocktail.nameCN = jsonData.name_cn;
                cocktail.nameEN = jsonData.name_en;
                cocktail.category = jsonData.category;
                cocktail.feature = jsonData.feature;
                cocktail.uiPath = jsonData.ui_path;

                // 尝试加载UI精灵
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
                        else
                        {
                            Log($"    ⚠ 未找到精灵: {jsonData.ui_path}");
                        }
                    }
                }

                // 映射效果
                cocktail.effects = new CardEffects
                {
                    busy = jsonData.effects.busy,
                    impatient = jsonData.effects.impatient,
                    bored = jsonData.effects.bored,
                    picky = jsonData.effects.picky,
                    friendly = jsonData.effects.friendly
                };

                // 映射经济数据
                cocktail.cost = jsonData.cost;
                cocktail.price = jsonData.price;
                cocktail.profit = jsonData.profit;
                cocktail.reputationChange = jsonData.reputation_change;

                // 保存资源
                if (!isUpdate)
                {
                    AssetDatabase.CreateAsset(cocktail, assetPath);
                    Log($"  → 创建: {fileName}");
                }
                else
                {
                    EditorUtility.SetDirty(cocktail);
                    Log($"  ↻ 更新: {fileName}");
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

                // 增量更新：检查现有资源
                MaterialCardSO existingMaterial = null;
                bool isUpdate = false;
                if (incrementalUpdate && File.Exists(assetPath))
                {
                    existingMaterial = AssetDatabase.LoadAssetAtPath<MaterialCardSO>(assetPath);
                    if (existingMaterial != null)
                    {
                        // 比较数据是否相同
                        if (IsMaterialDataSame(existingMaterial, jsonData))
                        {
                            Log($"  ⊙ 跳过: {fileName} (未修改)");
                            return ImportResult.Skipped;
                        }
                        isUpdate = true;
                    }
                }

                // 创建或使用现有实例
                MaterialCardSO material = existingMaterial ?? ScriptableObject.CreateInstance<MaterialCardSO>();

                // 映射数据
                material.id = jsonData.id;
                material.nameCN = jsonData.name_cn;
                material.nameEN = jsonData.name_en;
                material.category = jsonData.category;
                material.feature = jsonData.feature;
                material.uiPath = jsonData.ui_path;

                // 尝试加载UI精灵
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
                        else
                        {
                            Log($"    ⚠ 未找到精灵: {jsonData.ui_path}");
                        }
                    }
                }

                // 映射效果
                material.effects = new CardEffects
                {
                    busy = jsonData.effects.busy,
                    impatient = jsonData.effects.impatient,
                    bored = jsonData.effects.bored,
                    picky = jsonData.effects.picky,
                    friendly = jsonData.effects.friendly
                };

                // 映射经济数据
                material.price = jsonData.price;

                // 保存资源
                if (!isUpdate)
                {
                    AssetDatabase.CreateAsset(material, assetPath);
                    Log($"  → 创建: {fileName}");
                }
                else
                {
                    EditorUtility.SetDirty(material);
                    Log($"  ↻ 更新: {fileName}");
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
            Debug.Log(message);
            Repaint();
        }

        private void LogError(string message)
        {
            logOutput += "✗ 错误: " + message + "\n";
            Debug.LogError(message);
            Repaint();
        }

        #endregion
    }
}
