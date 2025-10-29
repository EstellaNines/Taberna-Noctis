using System.Linq;
using UnityEditor;
using UnityEngine;
using TabernaNoctis.Cards;

public static class CocktailRecipeDatabaseCreator
{
    [MenuItem("自制工具/卡牌系统/配方生成器")]
    public static void BuildDatabaseFromAssets()
    {
        // 选择保存路径
        string path = EditorUtility.SaveFilePanelInProject(
            "Create Cocktail Recipe Database",
            "CocktailRecipes",
            "asset",
            "Choose a location to save the recipe database asset.");
        if (string.IsNullOrEmpty(path)) return;

        var db = ScriptableObject.CreateInstance<CocktailRecipeDatabase>();

        // 尝试加载所有 CocktailCardSO 与 MaterialCardSO（仅用于帮助查找）
        var allCocktails = AssetDatabase.FindAssets("t:CocktailCardSO")
            .Select(g => AssetDatabase.LoadAssetAtPath<CocktailCardSO>(AssetDatabase.GUIDToAssetPath(g)))
            .Where(a => a != null)
            .ToList();
        var allMaterials = AssetDatabase.FindAssets("t:MaterialCardSO")
            .Select(g => AssetDatabase.LoadAssetAtPath<MaterialCardSO>(AssetDatabase.GUIDToAssetPath(g)))
            .Where(a => a != null)
            .ToList();

        // 内置 8 个经典配方（按我们之前实现的逻辑，基于材料英文名快速匹配）
        // 名称到 Material 映射（英文名）
        MaterialCardSO FindMat(string en)
        {
            var m = allMaterials.FirstOrDefault(x => x != null && x.nameEN == en);
            if (m == null) Debug.LogWarning($"[RecipeBuilder] 未找到材料: {en}");
            return m;
        }
        CocktailCardSO FindCkt(string en)
        {
            var c = allCocktails.FirstOrDefault(x => x != null && x.nameEN == en);
            if (c == null) Debug.LogWarning($"[RecipeBuilder] 未找到鸡尾酒: {en}");
            return c;
        }

        void Add(string a, string b, string c, string cocktail)
        {
            var r = new CocktailRecipeDatabase.Recipe
            {
                materialA = FindMat(a),
                materialB = FindMat(b),
                materialC = FindMat(c),
                result = FindCkt(cocktail)
            };
            r.Normalize();
            db.recipes.Add(r);
        }

        Add("Gin", "Dry Vermouth", "Lemon", "Martini");
        Add("Rye Whiskey", "Sweet Vermouth", "Angostura Bitters", "Manhattan");
        Add("Bourbon", "Simple Syrup", "Angostura Bitters", "Old Fashioned");
        Add("Gin", "Campari", "Sweet Vermouth", "Negroni");
        Add("Tequila", "Orange Liqueur", "Lime", "Margarita");
        Add("White Rum", "Simple Syrup", "Lime", "Daiquiri");
        Add("Vodka", "Soda Water", "Lime", "Moscow Mule");
        Add("White Rum", "Cola", "Lime", "Cuba Libre");

        // 尝试设置保底
        db.fallbackUnspeakable = FindCkt("Unspeakable");

        db.NormalizeAndDeduplicate();
        AssetDatabase.CreateAsset(db, path);
        AssetDatabase.SaveAssets();
        EditorGUIUtility.PingObject(db);
        Debug.Log($"[RecipeBuilder] 已创建配方数据库: {path}, 配方数={db.recipes.Count}");
    }
}


