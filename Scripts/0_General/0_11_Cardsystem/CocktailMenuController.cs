using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TabernaNoctis.Cards;
using Sirenix.OdinInspector;

/// <summary>
/// 鸡尾酒菜单：通过按钮控制开关；监听合成事件，按首次合成的顺序在 Area 中生成 Cocktail 预制体。
/// 预制体要求包含："Cocktail Image"(Image)、"Name"(TMP_Text)、"State"(TMP_Text)。
/// </summary>
public class CocktailMenuController : MonoBehaviour
{
	[LabelText("根可视区域(可选)")]
	[SerializeField] private RectTransform rootVisual; // 用于 CanvasGroup
	[LabelText("初始隐藏")]
	[SerializeField] private bool hideOnStart = true;

	[LabelText("生成区域 Area 容器")]
	[SerializeField] private RectTransform areaRoot;
	[LabelText("鸡尾酒条目预制体")]
	[SerializeField] private GameObject cocktailItemPrefab; // 结构如 Resources/Prefabs/0_General/3_Menu/Cocktail.prefab

	[LabelText("起始序号(从1开始)")]
	[SerializeField] private int startIndex = 1;

	[Header("过滤/忽略")]
	[LabelText("忽略的鸡尾酒英文名(含保底)")]
	[SerializeField] private string[] ignoreCocktailNamesEN = new string[] { "Unspeakable" };

	private CanvasGroup canvasGroup;
	private readonly System.Collections.Generic.Dictionary<int, int> cocktailIdToIndex = new System.Collections.Generic.Dictionary<int, int>();
	private int nextIndexAssigned = 0;

	private void Awake()
	{
		var target = rootVisual != null ? rootVisual : (RectTransform)transform;
		canvasGroup = target.GetComponent<CanvasGroup>();
		if (canvasGroup == null) canvasGroup = target.gameObject.AddComponent<CanvasGroup>();
		nextIndexAssigned = startIndex;
		if (hideOnStart) SetVisible(false);
	}

	private void OnEnable()
	{
		MessageManager.Register<(CocktailCardSO cocktail, MaterialCardSO a, MaterialCardSO b, MaterialCardSO c)>(
			"COCKTAIL_CRAFTED_DETAIL", OnCraftedDetail);
		MessageManager.Register<CocktailCardSO>(
			"COCKTAIL_CRAFTED", OnCraftedCocktailOnly);
	}

	private void OnDisable()
	{
		MessageManager.Remove<(CocktailCardSO cocktail, MaterialCardSO a, MaterialCardSO b, MaterialCardSO c)>(
			"COCKTAIL_CRAFTED_DETAIL", OnCraftedDetail);
		MessageManager.Remove<CocktailCardSO>(
			"COCKTAIL_CRAFTED", OnCraftedCocktailOnly);
	}

	[Button("打开菜单", ButtonSizes.Medium)]
	public void OpenMenu()
	{
		SetVisible(true);
	}

	[Button("关闭菜单", ButtonSizes.Medium)]
	public void CloseMenu()
	{
		SetVisible(false);
	}

	[Button("开关切换", ButtonSizes.Medium)]
	public void ToggleMenu()
	{
		SetVisible(!(canvasGroup != null && canvasGroup.alpha > 0.99f));
	}

	private void SetVisible(bool visible)
	{
		if (canvasGroup == null) return;
		canvasGroup.alpha = visible ? 1f : 0f;
		canvasGroup.interactable = visible;
		canvasGroup.blocksRaycasts = visible;
	}

	private void OnCraftedDetail((CocktailCardSO cocktail, MaterialCardSO a, MaterialCardSO b, MaterialCardSO c) payload)
	{
		HandleCrafted(payload.cocktail);
	}

	private void OnCraftedCocktailOnly(CocktailCardSO cocktail)
	{
		HandleCrafted(cocktail);
	}

	private void HandleCrafted(CocktailCardSO cocktail)
	{
		if (cocktail == null) return;
		if (IsIgnorable(cocktail)) return;
		if (areaRoot == null || cocktailItemPrefab == null)
		{
			Debug.LogWarning("[CocktailMenu] 未配置 Area 或 预制体");
			return;
		}

		// 去重：只在首次出现时分配序号并生成
		if (cocktailIdToIndex.ContainsKey(cocktail.id))
		{
			return;
		}

		int index = nextIndexAssigned++;
		cocktailIdToIndex[cocktail.id] = index;

		var item = Instantiate(cocktailItemPrefab, areaRoot);
		item.name = $"MenuCocktail_{index}_{cocktail.nameEN}";

		// 填充 UI
		var img = FindByName<Image>(item.transform, "Cocktail Image");
		if (img != null)
		{
			img.sprite = cocktail.uiSpritePreview;
			img.preserveAspect = true;
		}
		var nameText = FindByName<TMP_Text>(item.transform, "Name");
		if (nameText != null)
		{
			nameText.text = cocktail.nameEN;
		}

		// 仅设置数值到 Value（每行一个，带颜色和正负号）；State 名称由预制件自身提供
		var valueText = FindByName<TMP_Text>(item.transform, "Value");
		if (valueText != null)
		{
			valueText.text = BuildValuesMultiline(cocktail.effects);
		}

		// 不自动打开菜单：由外部菜单按钮控制
	}

	private bool IsIgnorable(CocktailCardSO cocktail)
	{
		if (cocktail == null) return true;
		if (ignoreCocktailNamesEN == null) return false;
		string name = string.IsNullOrEmpty(cocktail.nameEN) ? string.Empty : cocktail.nameEN.Trim();
		for (int i = 0; i < ignoreCocktailNamesEN.Length; i++)
		{
			var n = ignoreCocktailNamesEN[i];
			if (!string.IsNullOrEmpty(n) && string.Equals(name, n.Trim(), System.StringComparison.OrdinalIgnoreCase))
				return true;
		}
		return false;
	}

	private string BuildValuesMultiline(CardEffects effects)
	{
		// 文本格式：仅数值，每行一个，颜色区分正负零
		System.Text.StringBuilder sb = new System.Text.StringBuilder(64);
		AppendValueLine(sb, effects.busy);
		AppendValueLine(sb, effects.impatient);
		AppendValueLine(sb, effects.bored);
		AppendValueLine(sb, effects.picky);
		AppendValueLine(sb, effects.friendly, false);
		return sb.ToString();
	}

	private void AppendValueLine(System.Text.StringBuilder sb, int value, bool addNewLine = true)
	{
		string color = value > 0 ? "#24A148" : (value < 0 ? "#DA1E28" : "#000000");
		string sign = value > 0 ? "+" : string.Empty;
		sb.Append("<color=");
		sb.Append(color);
		sb.Append(">");
		sb.Append(sign);
		sb.Append(value);
		sb.Append("</color>");
		if (addNewLine) sb.Append('\n');
	}

	private T FindByName<T>(Transform parent, string childName) where T : Component
	{
		var comps = parent.GetComponentsInChildren<T>(true);
		for (int i = 0; i < comps.Length; i++)
		{
			if (comps[i] != null && comps[i].name == childName) return comps[i];
		}
		return null;
	}
}


