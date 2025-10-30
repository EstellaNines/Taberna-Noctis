using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 下午场景的配方书加载器：
/// - 从 SaveManager 读取 unlockedRecipes 中 recorded=true 的条目
/// - 按 orderIndex 排序并实例化到指定容器
/// - 支持消息触发刷新（RECIPE_BOOK_REFRESH_REQUEST / SAVE_LOADED / SAVE_COMPLETED）
/// </summary>
public class AfternoonFormulaBookLoader : MonoBehaviour
{
    [Header("UI引用")]
    [SerializeField] private Transform leftPage;           // 左页容器
    [SerializeField] private Transform rightPage;          // 右页容器
    [SerializeField] private GameObject formulaItemPrefab; // 单个配方条目预制（Formula.prefab）

    [Header("自动刷新")]
    [SerializeField] private bool refreshOnEnable = true;

    private void OnEnable()
    {
        MessageManager.Register<string>(MessageDefine.SAVE_LOADED, _ => Refresh());
        MessageManager.Register<string>(MessageDefine.SAVE_COMPLETED, _ => Refresh());
        MessageManager.Register<string>(MessageDefine.RECIPE_BOOK_REFRESH_REQUEST, _ => Refresh());
        if (refreshOnEnable) Refresh();
    }

    private void OnDisable()
    {
        MessageManager.Remove<string>(MessageDefine.SAVE_LOADED, _ => Refresh());
        MessageManager.Remove<string>(MessageDefine.SAVE_COMPLETED, _ => Refresh());
        MessageManager.Remove<string>(MessageDefine.RECIPE_BOOK_REFRESH_REQUEST, _ => Refresh());
    }

    [ContextMenu("刷新配方书")]
    public void Refresh()
    {
        // 清空左右页
        ClearContainer(leftPage);
        ClearContainer(rightPage);

        var sm = SaveManager.Instance;
        if (sm == null || formulaItemPrefab == null) return;
        var snap = sm.GenerateSaveData();
        if (snap == null || snap.unlockedRecipes == null) { Debug.Log("[FormulaBook] 无存档或无unlockedRecipes"); return; }

        // 过滤recorded=true并按orderIndex排序
        var list = new List<RecipeData>();
        foreach (var r in snap.unlockedRecipes)
        {
            if (r != null && r.recorded) list.Add(r);
        }
        Debug.Log($"[FormulaBook] 已记录配方数量:{list.Count}");
        list.Sort((a, b) => a.orderIndex.CompareTo(b.orderIndex));

        // 实例化，按顺序分页：偶数索引进左页，奇数右页（或你想要的规则）
        for (int i = 0; i < list.Count; i++)
        {
            var r = list[i];
            var parent = (i % 2 == 0) ? leftPage : rightPage;
            if (parent == null) parent = leftPage != null ? leftPage : rightPage;
            if (parent == null) break;
            var go = Instantiate(formulaItemPrefab, parent);
            var view = go.GetComponent<FormulaItemView>();
            if (view != null) view.Apply(r);
            else
            {
                var text = go.GetComponentInChildren<TMP_Text>();
                if (text != null) text.text = r.displayName;
            }
            go.name = $"Recipe_{r.recipeId}";
        }
    }

    private static void ClearContainer(Transform t)
    {
        if (t == null) return;
        for (int i = t.childCount - 1; i >= 0; i--) Destroy(t.GetChild(i).gameObject);
    }
}


