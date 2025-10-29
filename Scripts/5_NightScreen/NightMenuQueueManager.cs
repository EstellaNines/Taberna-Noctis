using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TabernaNoctis.Cards;
using Sirenix.OdinInspector;

/// <summary>
/// 夜晚场景 - 菜单队列派发器
/// 读取当日菜单（SaveData.currentMenuRecipeIDs）并将对应的 CocktailCardSO 入队到通用发牌器，按节奏派发到界面。
/// 用法与 AfternoonCardQueueManager 一致，只是数据来源改为晚间菜单。
/// </summary>
public class NightMenuQueueManager : MonoBehaviour
{
    [Header("派发器引用")]
    [LabelText("卡牌队列派发器")]
    [SerializeField]
    [Tooltip("卡牌队列派发器（与下午共用的通用发牌器）")]
    private TabernaNoctis.CardSystem.CardQueueDispenser dispenser;

    [Header("鸡尾酒数据")]
    [LabelText("全部鸡尾酒卡列表")]
    [SerializeField]
    [Tooltip("可用的鸡尾酒卡列表（用于根据ID/名称查找）")]
    private List<CocktailCardSO> allCocktailCards = new List<CocktailCardSO>();

    [Header("派发设置")]
    [LabelText("自动开始派发")]
    [SerializeField]
    [Tooltip("场景加载时自动开始派发")] private bool autoDispenseOnStart = true;

    [LabelText("开始延迟(秒)")]
    [SerializeField]
    [Tooltip("开始延迟（秒），等待场景完全加载")] private float startDelay = 0.5f;

    [LabelText("是否按ID排序(默认保持保存顺序)")]
    [SerializeField]
    [Tooltip("false=保持保存时的顺序；true=按ID升序")] private bool sortById = false;

    private void Start()
    {
        if (autoDispenseOnStart)
        {
            Invoke(nameof(LoadAndDispenseNightMenu), startDelay);
        }
    }

    /// <summary>
    /// 读取当日菜单（由 Day/Afternoon 确认并保存）并派发到队列
    /// </summary>
    [Button("加载并派发晚间菜单", ButtonSizes.Medium)]
    public void LoadAndDispenseNightMenu()
    {
        if (dispenser == null)
        {
            Debug.LogError("[NightMenuQueueManager] 未设置卡牌派发器！");
            return;
        }

        var menuIds = GetTodayMenuIds();
        if (menuIds == null || menuIds.Count == 0)
        {
            Debug.LogWarning("[NightMenuQueueManager] 当日菜单为空，跳过派发");
            return;
        }

        // 将ID解析为CocktailCardSO
        var cocktailCards = new List<CocktailCardSO>(menuIds.Count);
        for (int i = 0; i < menuIds.Count; i++)
        {
            var so = FindCocktailByKey(menuIds[i]);
            if (so != null)
            {
                cocktailCards.Add(so);
            }
            else
            {
                Debug.LogWarning($"[NightMenuQueueManager] 未找到菜单鸡尾酒: {menuIds[i]}");
            }
        }

        if (cocktailCards.Count == 0)
        {
            Debug.LogWarning("[NightMenuQueueManager] 菜单解析为空，跳过派发");
            return;
        }

        if (sortById)
        {
            cocktailCards = cocktailCards.OrderBy(c => c.id).ToList();
        }

        Debug.Log($"[NightMenuQueueManager] 将派发 {cocktailCards.Count} 张菜单鸡尾酒卡");

        // 入队并开始派发
        dispenser.EnqueueCards(cocktailCards.Cast<BaseCardSO>().ToList());
        dispenser.StartDispensing();
    }

    /// <summary>
    /// 从存档读取当日菜单ID列表
    /// </summary>
    private List<string> GetTodayMenuIds()
    {
        if (SaveManager.Instance == null)
        {
            Debug.LogWarning("[NightMenuQueueManager] SaveManager 未找到");
            return new List<string>();
        }

        try
        {
            var saveData = SaveManager.Instance.GenerateSaveData();
            return saveData?.currentMenuRecipeIDs ?? new List<string>();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[NightMenuQueueManager] 读取存档失败: {e.Message}");
            return new List<string>();
        }
    }

    /// <summary>
    /// 根据字符串键查找鸡尾酒：优先数字ID，其次英文/中文名
    /// </summary>
    private CocktailCardSO FindCocktailByKey(string key)
    {
        if (string.IsNullOrEmpty(key)) return null;
        if (int.TryParse(key, out int id))
        {
            return allCocktailCards.FirstOrDefault(c => c != null && c.id == id);
        }

        return allCocktailCards.FirstOrDefault(c =>
            c != null && (
                string.Equals(c.nameEN, key, System.StringComparison.OrdinalIgnoreCase) ||
                string.Equals(c.nameCN, key, System.StringComparison.OrdinalIgnoreCase)
            ));
    }

#if ODIN_INSPECTOR
    [Button("测试：随机挑选5个并保存+派发", ButtonSizes.Medium)]
#endif
    public void TestPickRandomMenuAndDispatch()
    {
        if (allCocktailCards == null || allCocktailCards.Count == 0)
        {
            Debug.LogWarning("[NightMenuQueueManager] allCocktailCards 为空，无法随机挑选");
            return;
        }

        // 复制并洗牌
        var pool = new List<CocktailCardSO>(allCocktailCards.Where(c => c != null));
        for (int i = pool.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            var tmp = pool[i];
            pool[i] = pool[j];
            pool[j] = tmp;
        }

        int take = Mathf.Min(5, pool.Count);
        var picked = pool.GetRange(0, take);

        // 保存为当日菜单（id 字符串）
        var keys = new List<string>(picked.Count);
        for (int i = 0; i < picked.Count; i++) keys.Add(picked[i].id.ToString());

        var sm = SaveManager.Instance;
        if (sm != null)
        {
            sm.UpdateDailyMenu(keys);
            MessageManager.Send(MessageDefine.COCKTAIL_MENU_SAVED, keys);
            Debug.Log("[NightMenuQueueManager] 测试菜单已保存: " + string.Join(", ", keys));
        }
        else
        {
            Debug.LogWarning("[NightMenuQueueManager] SaveManager 不存在，跳过保存");
        }

        // 入队并派发
        if (dispenser != null)
        {
            dispenser.EnqueueCards(picked.Cast<BaseCardSO>().ToList());
            dispenser.StartDispensing();
            Debug.Log($"[NightMenuQueueManager] 已派发测试菜单 {picked.Count} 张鸡尾酒卡");
        }
        else
        {
            Debug.LogWarning("[NightMenuQueueManager] 未设置发牌器，已保存菜单但未派发");
        }
    }

#if UNITY_EDITOR
    [ContextMenu("自动加载所有鸡尾酒卡（编辑器）")]
    private void AutoLoadAllCocktailCards()
    {
        allCocktailCards.Clear();
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:CocktailCardSO");
        foreach (string guid in guids)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            CocktailCardSO so = UnityEditor.AssetDatabase.LoadAssetAtPath<CocktailCardSO>(path);
            if (so != null) allCocktailCards.Add(so);
        }

        allCocktailCards = allCocktailCards.OrderBy(c => c.id).ToList();
        Debug.Log($"[NightMenuQueueManager] 自动加载了 {allCocktailCards.Count} 张鸡尾酒卡");
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}


