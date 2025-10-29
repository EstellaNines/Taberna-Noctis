using System.Collections.Generic;
using UnityEngine;
using TMPro;
using TabernaNoctis.Cards;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

/// <summary>
/// NightScreen 当晚菜单展示（基于 NightMenu.prefab）：
/// - 自动在预制件子物体中寻址 Area 作为列表容器
/// - 从 SaveData.currentMenuRecipeIDs 读取菜单
/// - 优先解析为鸡尾酒SO以显示名称；解析失败则显示原ID
/// - 支持行预制；若未提供，则动态创建TMP文本作为行
/// </summary>
public class NightMenuViewer : MonoBehaviour
{
    [Header("UI引用")]
#if ODIN_INSPECTOR
    [BoxGroup("UI 引用"), LabelText("列表容器(Area)")]
#endif
    [SerializeField] private Transform listContainer;   // NightMenu/Area
#if ODIN_INSPECTOR
    [BoxGroup("UI 引用"), LabelText("卡牌条目预制(NightCocktail)")]
#endif
    [SerializeField] private GameObject cardItemPrefab; // 可选：卡牌风格条目预制（需含 NightMenuItemView）
#if ODIN_INSPECTOR
    [BoxGroup("UI 引用"), LabelText("文本行预制(可选)")]
#endif
    [SerializeField] private GameObject linePrefab;     // 备选：简单文本行预制（内含TMP_Text）

    [Header("资源设置")]
#if ODIN_INSPECTOR
    [BoxGroup("资源设置"), LabelText("鸡尾酒SO资源目录(可选)"), InfoBox("留空=在Resources全局扫描；若你的SO集中放在某个目录(例如 'Cards/Cocktails')，可填该相对路径以减少扫描量。", InfoMessageType.Info)]
#endif
    [SerializeField] private string cocktailsResourcesFolder = ""; // 可留空→全局扫描（编辑器/打包需放入Resources）

#if ODIN_INSPECTOR
    [BoxGroup("资源设置"), LabelText("鸡尾酒SO列表(可直接指定)")]
#endif
    [SerializeField] private List<CocktailCardSO> cocktailCatalog = new List<CocktailCardSO>();

    [Header("行为")]
#if ODIN_INSPECTOR
    [BoxGroup("行为"), LabelText("启用时自动刷新")]
#endif
    [SerializeField] private bool refreshOnEnable = true;

#if ODIN_INSPECTOR
    [BoxGroup("行为"), LabelText("显示时自动刷新")]
#endif
    [SerializeField] private bool refreshOnShow = true;

#if ODIN_INSPECTOR
    [BoxGroup("UI 引用"), LabelText("显隐根物体(默认本物体)")]
#endif
    [SerializeField] private GameObject rootToToggle;

    private Dictionary<int, CocktailCardSO> _idToCocktail;

    private void Awake()
    {
        if (listContainer == null)
        {
            var t = transform.Find("Area");
            if (t != null) listContainer = t;
        }
        if (rootToToggle == null) rootToToggle = gameObject;
        BuildCocktailIndex();
    }

    private void OnEnable()
    {
        MessageManager.Register<string>(MessageDefine.SAVE_LOADED, OnAnyRefreshEvent);
        MessageManager.Register<string>(MessageDefine.SAVE_COMPLETED, OnAnyRefreshEvent);
        MessageManager.Register<string>(MessageDefine.MENU_REFRESH_REQUEST, OnAnyRefreshEvent);
        if (refreshOnEnable) Refresh();
    }

    private void OnDisable()
    {
        MessageManager.Remove<string>(MessageDefine.SAVE_LOADED, OnAnyRefreshEvent);
        MessageManager.Remove<string>(MessageDefine.SAVE_COMPLETED, OnAnyRefreshEvent);
        MessageManager.Remove<string>(MessageDefine.MENU_REFRESH_REQUEST, OnAnyRefreshEvent);
    }

    private void OnAnyRefreshEvent(string _)
    {
        Refresh();
    }

    [ContextMenu("刷新菜单")]
    public void Refresh()
    {
        if (listContainer == null)
        {
            Debug.LogWarning("[NightMenu] 未绑定列表容器(Area)");
            return;
        }
        for (int i = listContainer.childCount - 1; i >= 0; i--) Destroy(listContainer.GetChild(i).gameObject);

        var sm = SaveManager.Instance;
        if (sm == null) return;
        var snap = sm.GenerateSaveData();
        var ids = snap != null ? snap.currentMenuRecipeIDs : null;
        if (ids == null || ids.Count == 0)
        {
            AddLine("(No Menu)");
            return;
        }

        foreach (var key in ids)
        {
            string text = ResolveCocktailName(key);
            if (TryGetCocktail(key, out var so) && cardItemPrefab != null)
                AddCard(so);
            else
            {
                Debug.Log($"[NightMenu] 未能解析鸡尾酒SO或未设置cardItemPrefab，按文本显示: {text}");
                AddLine(text);
            }
        }
    }

#if ODIN_INSPECTOR
    [BoxGroup("操作"), Button("显示菜单")]
#endif
    public void ShowMenuPanel()
    {
        if (rootToToggle != null && !rootToToggle.activeSelf) rootToToggle.SetActive(true);
        if (refreshOnShow) Refresh();
    }

#if ODIN_INSPECTOR
    [BoxGroup("操作"), Button("隐藏菜单")]
#endif
    public void HideMenuPanel()
    {
        if (rootToToggle != null && rootToToggle.activeSelf) rootToToggle.SetActive(false);
    }

#if ODIN_INSPECTOR
    [BoxGroup("操作"), Button("切换显示/隐藏")]
#endif
    public void ToggleMenuPanel()
    {
        if (rootToToggle == null) rootToToggle = gameObject;
        bool next = !rootToToggle.activeSelf;
        rootToToggle.SetActive(next);
        if (next && refreshOnShow) Refresh();
    }

    private void AddCard(CocktailCardSO so)
    {
        var go = Instantiate(cardItemPrefab, listContainer);
        var view = go.GetComponent<NightMenuItemView>();
        if (view != null) view.Apply(so);
    }

    private void AddLine(string text)
    {
        if (linePrefab != null)
        {
            var go = Instantiate(linePrefab, listContainer);
            var tmp = go.GetComponentInChildren<TMP_Text>();
            if (tmp != null) tmp.text = text;
            return;
        }

        var goText = new GameObject("Item", typeof(RectTransform));
        goText.transform.SetParent(listContainer, false);
        var tmpText = goText.AddComponent<TextMeshProUGUI>();
        tmpText.text = text;
        tmpText.fontSize = 28;
        tmpText.alignment = TextAlignmentOptions.Center;
    }

    private string ResolveCocktailName(string key)
    {
        if (string.IsNullOrEmpty(key)) return "";
        if (int.TryParse(key, out int id))
        {
            if (_idToCocktail != null && _idToCocktail.TryGetValue(id, out var so) && so != null)
                return so.nameEN;
            return key;
        }

        // 不是数字ID，尝试按名称匹配
        if (_idToCocktail != null)
        {
            foreach (var kv in _idToCocktail)
            {
                var so = kv.Value;
                if (so == null) continue;
                if (string.Equals(so.nameEN, key, System.StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(so.nameCN, key, System.StringComparison.OrdinalIgnoreCase))
                    return so.nameEN;
            }
        }
        return key;
    }

    private bool TryGetCocktail(string key, out CocktailCardSO so)
    {
        so = null;
        if (string.IsNullOrEmpty(key)) return false;
        if (int.TryParse(key, out int id))
        {
            return _idToCocktail != null && _idToCocktail.TryGetValue(id, out so) && so != null;
        }
        if (_idToCocktail != null)
        {
            foreach (var kv in _idToCocktail)
            {
                var c = kv.Value;
                if (c == null) continue;
                if (string.Equals(c.nameEN, key, System.StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(c.nameCN, key, System.StringComparison.OrdinalIgnoreCase))
                { so = c; return true; }
            }
        }
        return false;
    }

    private void BuildCocktailIndex()
    {
        _idToCocktail = new Dictionary<int, CocktailCardSO>();
        // 1) 优先使用检查器手动指定的SO列表（无需放在Resources）
        if (cocktailCatalog != null && cocktailCatalog.Count > 0)
        {
            foreach (var so in cocktailCatalog)
            {
                if (so != null) _idToCocktail[so.id] = so;
            }
        }

        // 2) 如未提供列表，再通过Resources扫描
        if (_idToCocktail.Count == 0)
        {
            CocktailCardSO[] list;
            if (string.IsNullOrEmpty(cocktailsResourcesFolder))
                list = Resources.LoadAll<CocktailCardSO>(string.Empty);
            else
                list = Resources.LoadAll<CocktailCardSO>(cocktailsResourcesFolder);

            if (list != null && list.Length > 0)
            {
                for (int i = 0; i < list.Length; i++)
                {
                    var so = list[i];
                    if (so != null) _idToCocktail[so.id] = so;
                }
            }
        }

        Debug.Log($"[NightMenu] Cocktail 索引构建完成，数量: {_idToCocktail.Count}");
    }
}



