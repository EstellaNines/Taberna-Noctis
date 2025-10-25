using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TabernaNoctis.Cards;
using Sirenix.OdinInspector;

/// <summary>
/// 配方书控制器：监听合成广播，按奇偶序号将配方页实例化到 LeftPage / RightPage。
/// 需要在 Book.prefab 根物体上挂载，并把左右页与配方页预制体引用好。
/// </summary>
public class FormulaBookController : MonoBehaviour
{
	[LabelText("左页容器")]
	[SerializeField] private RectTransform leftPage;
	[LabelText("右页容器")]
	[SerializeField] private RectTransform rightPage;
	[LabelText("配方页预制体")]
	[SerializeField] private GameObject formulaPagePrefab; // Resources/Prefabs/0_General/2_FormulaBook/Formula.prefab 也可直接拖引用
	[LabelText("根可视区域(可选)")]
	[SerializeField] private RectTransform rootVisual; // 可选；不指定则使用本对象
	[LabelText("初始隐藏")]
	[SerializeField] private bool hideOnStart = true;   // 初始隐藏

	[LabelText("起始序号(从1开始)")]
	[SerializeField] private int startIndex = 1; // 从1开始计数
	private int currentCount = 0; // 保留但不再直接用于编号分配
	private CanvasGroup canvasGroup;

	[Header("编号与过滤")]
	[LabelText("忽略的鸡尾酒英文名(含保底)")]
	[SerializeField] private string[] ignoreCocktailNamesEN = new string[] { "Unspeakable" };

	// 为每款鸡尾酒分配唯一编号（首次出现时决定），用于奇偶放置
	private readonly System.Collections.Generic.Dictionary<int, int> cocktailIdToIndex = new System.Collections.Generic.Dictionary<int, int>();
	private int nextIndexAssigned = 0; // 在 Awake 时设置为 startIndex

	[Header("测试数据（按钮调用）")]
	[LabelText("测试鸡尾酒")]
	[SerializeField] private CocktailCardSO testCocktail;
	[LabelText("测试材料A")]
	[SerializeField] private MaterialCardSO testMaterialA;
	[LabelText("测试材料B")]
	[SerializeField] private MaterialCardSO testMaterialB;
	[LabelText("测试材料C")]
	[SerializeField] private MaterialCardSO testMaterialC;

	[Header("配方SO测试")]
	[LabelText("配方数据库")]
	[SerializeField] private CocktailRecipeDatabase recipeDatabase;
	[LabelText("测试配方索引"), MinValue(0)]
	[SerializeField] private int testRecipeIndex = 0;

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

	private void Awake()
	{
		var target = rootVisual != null ? rootVisual : (RectTransform)transform;
		canvasGroup = target.GetComponent<CanvasGroup>();
		if (canvasGroup == null) canvasGroup = target.gameObject.AddComponent<CanvasGroup>();
		if (hideOnStart) HideBookImmediate();
		nextIndexAssigned = startIndex;
	}

	// 公开给按钮调用
	[Button("打开配方书", ButtonSizes.Medium)]
	public void OpenBook()
	{
		SetVisible(true);
	}

	[Button("关闭配方书", ButtonSizes.Medium)]
	public void CloseBook()
	{
		SetVisible(false);
	}

	[Button("打开/关闭 配方书", ButtonSizes.Medium)]
	public void ToggleBook()
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

	private void HideBookImmediate()
	{
		SetVisible(false);
	}

	// 供测试按钮调用：使用 Inspector 配置的测试数据生成一页配方
	[Button("测试：生成一页配方(手动指定)", ButtonSizes.Medium), GUIColor(0.4f, 1f, 0.6f)]
	public void TestSpawnFormulaPage()
	{
		if (testCocktail == null)
		{
			Debug.LogWarning("[FormulaBook] 未设置测试鸡尾酒数据");
			return;
		}
		SetVisible(true);
		OnCraftedDetail((testCocktail, testMaterialA, testMaterialB, testMaterialC));
	}

	[Button("测试：按配方索引生成", ButtonSizes.Medium), GUIColor(0.5f, 0.85f, 1f)]
	public void TestSpawnFromRecipe()
	{
		if (recipeDatabase == null || recipeDatabase.recipes == null || recipeDatabase.recipes.Count == 0)
		{
			Debug.LogWarning("[FormulaBook] 未配置配方数据库或配方为空");
			return;
		}
		int idx = Mathf.Clamp(testRecipeIndex, 0, recipeDatabase.recipes.Count - 1);
		var r = recipeDatabase.recipes[idx];
		if (r == null || r.result == null)
		{
			Debug.LogWarning("[FormulaBook] 该索引配方为空或未设置结果");
			return;
		}
		SetVisible(true);
		OnCraftedDetail((r.result, r.materialA, r.materialB, r.materialC));
	}

	private void OnCraftedDetail((CocktailCardSO cocktail, MaterialCardSO a, MaterialCardSO b, MaterialCardSO c) payload)
	{
		HandleCrafted(payload.cocktail, payload.a, payload.b, payload.c);
	}

	// 当只收到鸡尾酒SO时：根据配方数据库查找材料并生成
	private void OnCraftedCocktailOnly(CocktailCardSO cocktail)
	{
		if (cocktail == null)
		{
			Debug.LogWarning("[FormulaBook] 收到空的鸡尾酒SO");
			return;
		}
		if (recipeDatabase == null || recipeDatabase.recipes == null)
		{
			Debug.LogWarning("[FormulaBook] 未配置配方数据库，无法从鸡尾酒反查材料");
			return;
		}
		for (int i = 0; i < recipeDatabase.recipes.Count; i++)
		{
			var r = recipeDatabase.recipes[i];
			if (r != null && r.result == cocktail)
			{
				HandleCrafted(cocktail, r.materialA, r.materialB, r.materialC);
				return;
			}
		}
		Debug.LogWarning($"[FormulaBook] 在配方库中未找到 {cocktail.nameEN} 的配方");
	}

	private void HandleCrafted(CocktailCardSO cocktail, MaterialCardSO a, MaterialCardSO b, MaterialCardSO c)
	{
		if (cocktail == null) return;
		if (IsIgnorableCocktail(cocktail)) return; // 跳过保底等不需要入册的鸡尾酒

		// 已经入册过则不重复生成
		if (cocktailIdToIndex.ContainsKey(cocktail.id))
		{
			return;
		}

		int index = nextIndexAssigned++;
		cocktailIdToIndex[cocktail.id] = index;
		currentCount = index; // 仅用于命名展示

		bool isOdd = (index % 2 == 1);
		RectTransform parent = isOdd ? leftPage : rightPage;
		if (parent == null || formulaPagePrefab == null)
		{
			Debug.LogWarning("[FormulaBook] 未配置 Left/RightPage 或 formulaPagePrefab");
			return;
		}

		var page = Instantiate(formulaPagePrefab, parent);
		page.name = $"Formula_{index}";

		// Cocktail 显示
		var cocktailImg = FindByName<Image>(page.transform, "Cocktail Image");
		if (cocktailImg != null)
		{
			cocktailImg.sprite = cocktail.uiSpritePreview;
			cocktailImg.preserveAspect = true;
		}
		var cocktailName = FindByName<TMP_Text>(page.transform, "Name");
		if (cocktailName != null)
		{
			cocktailName.text = cocktail.nameEN;
		}

		// Materials 显示：预制件中按 MaterialUI_1/2/3 分组（每组下有 "Material Image" 与 "Name"）
		SetMaterialBlock(page.transform, "MaterialUI_1", a);
		SetMaterialBlock(page.transform, "MaterialUI_2", b);
		SetMaterialBlock(page.transform, "MaterialUI_3", c);
	}

	private bool IsIgnorableCocktail(CocktailCardSO cocktail)
	{
		if (cocktail == null) return true;
		if (ignoreCocktailNamesEN != null)
		{
			string name = string.IsNullOrEmpty(cocktail.nameEN) ? string.Empty : cocktail.nameEN.Trim();
			for (int i = 0; i < ignoreCocktailNamesEN.Length; i++)
			{
				var n = ignoreCocktailNamesEN[i];
				if (!string.IsNullOrEmpty(n) && string.Equals(name, n.Trim(), System.StringComparison.OrdinalIgnoreCase))
					return true;
			}
		}
		return false;
	}

	private void SetMaterialBlock(Transform root, string groupName, MaterialCardSO mat)
	{
		var group = root.Find(groupName);
		if (group == null) return;
		var img = FindByName<Image>(group, "Material Image");
		if (img != null)
		{
			img.sprite = mat != null ? mat.uiSpritePreview : null;
			img.preserveAspect = true;
		}
		var name = FindByName<TMP_Text>(group, "Name");
		if (name != null)
		{
			name.text = mat != null ? mat.nameEN : string.Empty;
		}
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


