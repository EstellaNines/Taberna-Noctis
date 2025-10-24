using System;
using System.Collections.Generic;
using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif
using TabernaNoctis.Cards;

/// <summary>
/// 鸡尾酒配方数据库（ScriptableObject）：
/// - 三个材料 → 一张鸡尾酒卡牌
/// - 提供查找与规范化（排序+去重）
/// - 可设置“不可描述之物”作为兜底配方
/// </summary>
[CreateAssetMenu(fileName = "CocktailRecipes", menuName = "TabernaNoctis/Cards/Cocktail Recipe Database", order = 200)]
public class CocktailRecipeDatabase : ScriptableObject
{
    [Serializable]
    public class Recipe
    {
#if ODIN_INSPECTOR
        [LabelText("材料 A")]
        [AssetsOnly]
        [Required]
        [TableColumnWidth(180, Resizable = true)]
#endif
        public MaterialCardSO materialA;
#if ODIN_INSPECTOR
        [LabelText("材料 B")]
        [AssetsOnly]
        [Required]
        [TableColumnWidth(180, Resizable = true)]
#endif
        public MaterialCardSO materialB;
#if ODIN_INSPECTOR
        [LabelText("材料 C")]
        [AssetsOnly]
        [Required]
        [TableColumnWidth(180, Resizable = true)]
#endif
        public MaterialCardSO materialC;
#if ODIN_INSPECTOR
        [LabelText("结果鸡尾酒")]
        [AssetsOnly]
        [Required]
        [TableColumnWidth(200, Resizable = true)]
#endif
        public CocktailCardSO result;

        public void Normalize()
        {
            // 将ABC按 id 升序排序，保证组合唯一性
            int a = materialA != null ? materialA.id : int.MaxValue;
            int b = materialB != null ? materialB.id : int.MaxValue;
            int c = materialC != null ? materialC.id : int.MaxValue;
            if (a > b) { (materialA, materialB) = (materialB, materialA); (a, b) = (b, a); }
            if (b > c) { (materialB, materialC) = (materialC, materialB); (b, c) = (c, b); }
            if (a > b) { (materialA, materialB) = (materialB, materialA); }
        }

        public bool SameTriple(Recipe other)
        {
            if (other == null) return false;
            int a1 = materialA != null ? materialA.id : -1;
            int b1 = materialB != null ? materialB.id : -1;
            int c1 = materialC != null ? materialC.id : -1;
            int a2 = other.materialA != null ? other.materialA.id : -1;
            int b2 = other.materialB != null ? other.materialB.id : -1;
            int c2 = other.materialC != null ? other.materialC.id : -1;
            return a1 == a2 && b1 == b2 && c1 == c2;
        }
    }

#if ODIN_INSPECTOR
    [InfoBox("在此编辑三材料组合 → 结果鸡尾酒的配方。建议不时点击下方'规范化去重'按钮保持唯一性。", InfoMessageType = InfoMessageType.None)]
    [LabelText("配方列表")]
    [TableList(AlwaysExpanded = true, NumberOfItemsPerPage = 8)]
#endif
    public List<Recipe> recipes = new List<Recipe>();

#if ODIN_INSPECTOR
    [BoxGroup("全局设置")]
    [LabelText("保底鸡尾酒（不可描述之物）")]
    [AssetsOnly]
    [Required]
    [PreviewField(Alignment = ObjectFieldAlignment.Center)]
#endif
    public CocktailCardSO fallbackUnspeakable;

    /// <summary>
    /// 依据3个材料SO查找鸡尾酒，找不到则返回fallback（可空）。
    /// </summary>
    public CocktailCardSO ResolveCocktailByMaterials(MaterialCardSO a, MaterialCardSO b, MaterialCardSO c)
    {
        int idA = a != null ? a.id : int.MaxValue;
        int idB = b != null ? b.id : int.MaxValue;
        int idC = c != null ? c.id : int.MaxValue;
        if (idA > idB) { (a, b) = (b, a); (idA, idB) = (idB, idA); }
        if (idB > idC) { (b, c) = (c, b); (idB, idC) = (idC, idB); }
        if (idA > idB) { (a, b) = (b, a); }

        for (int i = 0; i < recipes.Count; i++)
        {
            var r = recipes[i];
            if (r == null || r.result == null) continue;
            int ra = r.materialA != null ? r.materialA.id : -1;
            int rb = r.materialB != null ? r.materialB.id : -1;
            int rc = r.materialC != null ? r.materialC.id : -1;
            if (ra == idA && rb == idB && rc == idC)
            {
                return r.result;
            }
        }
        return fallbackUnspeakable;
    }

    /// <summary>
    /// 兼容旧接口：使用三个ID进行解析。
    /// </summary>
    public CocktailCardSO ResolveCocktailByMaterials(int idA, int idB, int idC)
    {
        MaterialCardSO a = FindMaterialById(idA);
        MaterialCardSO b = FindMaterialById(idB);
        MaterialCardSO c = FindMaterialById(idC);
        return ResolveCocktailByMaterials(a, b, c);
    }

    private MaterialCardSO FindMaterialById(int id)
    {
        for (int i = 0; i < recipes.Count; i++)
        {
            var r = recipes[i];
            if (r?.materialA != null && r.materialA.id == id) return r.materialA;
            if (r?.materialB != null && r.materialB.id == id) return r.materialB;
            if (r?.materialC != null && r.materialC.id == id) return r.materialC;
        }
        return null;
    }

    /// <summary>
    /// 去重并排序所有配方（OnValidate时自动调用）。
    /// </summary>
    public void NormalizeAndDeduplicate()
    {
        for (int i = 0; i < recipes.Count; i++)
        {
            recipes[i]?.Normalize();
        }
        // 去重
        for (int i = 0; i < recipes.Count; i++)
        {
            var a = recipes[i];
            if (a == null) continue;
            for (int j = recipes.Count - 1; j > i; j--)
            {
                var b = recipes[j];
                if (b == null) continue;
                if (a.SameTriple(b))
                {
                    recipes.RemoveAt(j);
                }
            }
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        NormalizeAndDeduplicate();
    }
#endif

#if ODIN_INSPECTOR
    [Button("规范化去重"), GUIColor(0.4f, 0.8f, 1f)]
    private void OdinNormalize()
    {
        NormalizeAndDeduplicate();
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }
#endif
}


