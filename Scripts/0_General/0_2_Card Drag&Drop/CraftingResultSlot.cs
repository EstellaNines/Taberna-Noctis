using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TabernaNoctis.Cards;
using Sirenix.OdinInspector;
using DG.Tweening;

/// <summary>
/// 显示合成结果的空槽（仅展示，不参与拖拽/提交）。
/// </summary>
public class CraftingResultSlot : MonoBehaviour
{
    [Header("背景层次（按层从下到上）")]
    [LabelText("背景图")]
    [SerializeField] private Image backgroundImage;
    [LabelText("高亮背景图")]
    [SerializeField] private Image highlightBackground;

    [Header("卡牌显示区（按下按钮后才显示）")]
    [LabelText("卡牌挂载父物体")]
    [SerializeField, Tooltip("实例化Card预制体的父物体")] private RectTransform cardRoot;
    [LabelText("卡牌预制体(可选)")]
    [SerializeField, Tooltip("Card预制体引用（可空，留空则从Resources加载）")] private GameObject cardPrefab;
    [LabelText("Resources路径")]
    [SerializeField, Tooltip("当cardPrefab未指定时，从Resources加载此路径")] private string resourcesPath = "Prefabs/0_General/1_CardSystem/Card";

    [Header("外部名称显示(可选)")]
    [LabelText("结果名称文本")]
    [SerializeField, Tooltip("用于显示鸡尾酒名称的外部TMP文本(可选)")] private TMP_Text resultNameText;

	[Header("材质修复(可选)")]
	[LabelText("修复条形渐变材质")]
	[SerializeField, Tooltip("若实例化后条形渐变材质丢失，可启用并指定覆盖材质")] private bool applyBarMaterialOverride = false;
	[LabelText("覆盖用材质")]
	[SerializeField, Tooltip("用于覆盖条形/状态条 Image 的材质")] private Material barMaterialOverride;
	[LabelText("命名关键字(匹配条形Image)")]
	[SerializeField, Tooltip("名称包含任一关键字的 Image 将被认为是条形并应用覆盖材质")] private string[] barNameKeywords = new string[] { "Bar", "Status", "Gauge" };

	[Header("尺寸限制")]
	[LabelText("自动适配到背景尺寸内")]
	[SerializeField, Tooltip("防止卡牌超出背景大小：若过大则等比缩小，不放大")] private bool fitCardToBackground = true;
	[LabelText("边距(像素)")]
	[SerializeField, Tooltip("适配时预留的四周边距像素")]
	private float fitPadding = 8f;

    [Header("状态(调试)")]
    [LabelText("当前鸡尾酒")] 
    [SerializeField] private CocktailCardSO currentCocktail;

    private GameObject cardInstance;
    private Image cardUISprite;     // 子节点："UI Sprite" 的 Image
    private TMP_Text cardNameText;  // 子节点："Name" 的 TMP_Text
	private TMP_Text cardCategoryText; // 子节点："Category" 的 TMP_Text

    [Header("飞向菜单动效")]
    [LabelText("启用生成后飞向菜单按钮")]
    [SerializeField] private bool playFlyToMenu = true;
    [LabelText("菜单按钮(目标)RectTransform")]
    [SerializeField] private RectTransform menuButtonTarget;
    [LabelText("等待秒数")]
    [SerializeField] private float flyDelaySeconds = 2f;
    [LabelText("飞行时长(秒)")]
    [SerializeField] private float flyDurationSeconds = 0.6f;
    [LabelText("移动缓动")]
    [SerializeField] private Ease flyEase = Ease.InCubic;
    [LabelText("完成后隐藏卡牌")]
    [SerializeField] private bool hideCardAfterFly = true;

    private Sequence flySequence;
    private Vector2 initialAnchoredPos;
    private Vector3 initialLocalScale = Vector3.one;

    private void Awake()
    {
        // 初始仅显示背景；高亮与卡牌隐藏
        SetHighlight(false);
        // 确保预制体实例已创建，并且默认隐藏，防止场景中 Card 预制体意外被激活
        EnsureCardInstance();
        HideCard();
    }

    public void SetHighlight(bool on)
    {
        if (highlightBackground != null)
        {
            var c = highlightBackground.color;
            c.a = on ? Mathf.Max(c.a, 0.2f) : 0f;
            highlightBackground.color = c;
            highlightBackground.enabled = on;
        }
    }

    public void SetResult(CocktailCardSO cocktail)
    {
        currentCocktail = cocktail;
        if (cocktail == null)
        {
            HideCard();
            return;
        }

        EnsureCardInstance();
        ResetCardTransform();
        UpdateCardVisual(cocktail);
        ShowCard();
        FitCardWithinBackground();
        TryApplyBarMaterialOverride();

        // 外部名称文本（可选）
		if (resultNameText != null)
		{
			// 始终显示英文名
			resultNameText.text = cocktail.nameEN;
		}

        // 动效
        PlayFlyToMenuAnim();
    }

    public void Clear()
    {
        currentCocktail = null;
        HideCard();
        SetHighlight(false);
        if (resultNameText != null) resultNameText.text = string.Empty;
    }

    private void EnsureCardInstance()
    {
        if (cardInstance != null) return;
        if (cardRoot == null) cardRoot = (RectTransform)transform;

        var prefab = cardPrefab;
        if (prefab == null)
        {
            prefab = Resources.Load<GameObject>(resourcesPath);
            if (prefab == null)
            {
                Debug.LogError($"[CraftingResultSlot] 无法从 Resources 加载卡牌预制体: {resourcesPath}");
                return;
            }
        }
        cardInstance = Instantiate(prefab, cardRoot);
        cardInstance.SetActive(false);

        // 取常用UI引用（按名称查找，避免依赖具体脚本）
		cardUISprite = FindChildByName<Image>(cardInstance.transform, "UI Sprite");
		cardNameText = FindChildByName<TMP_Text>(cardInstance.transform, "Name");
		cardCategoryText = FindChildByName<TMP_Text>(cardInstance.transform, "Category");

        var rt = cardInstance.transform as RectTransform;
        if (rt != null)
        {
            initialAnchoredPos = rt.anchoredPosition;
            initialLocalScale = rt.localScale;
        }
    }

    private void UpdateCardVisual(CocktailCardSO cocktail)
    {
        if (cardInstance == null) return;
        if (cardUISprite != null)
        {
            cardUISprite.enabled = (cocktail.uiSpritePreview != null);
            if (cardUISprite.enabled) cardUISprite.sprite = cocktail.uiSpritePreview;
        }
		if (cardNameText != null)
		{
			// 始终显示英文名
			cardNameText.text = cocktail.nameEN;
		}
		if (cardCategoryText != null)
		{
			// 类别统一显示英文
			cardCategoryText.text = ResolveEnglishCategory(cocktail);
		}
    }

    private void ShowCard()
    {
        if (cardInstance != null) cardInstance.SetActive(true);
    }

    private void HideCard()
    {
        if (cardInstance != null) cardInstance.SetActive(false);
    }

    private void ResetCardTransform()
    {
        if (cardInstance == null) return;
        var rt = cardInstance.transform as RectTransform;
        if (rt == null) return;
        // 终止上一次动画并复位
        if (flySequence != null && flySequence.IsActive())
        {
            flySequence.Kill();
            flySequence = null;
        }
        rt.anchoredPosition = initialAnchoredPos;
        rt.localScale = initialLocalScale;
    }

    private T FindChildByName<T>(Transform parent, string targetName) where T : Component
    {
        var comps = parent.GetComponentsInChildren<T>(true);
        for (int i = 0; i < comps.Length; i++)
        {
            if (comps[i] != null && comps[i].name == targetName) return comps[i];
        }
        return null;
    }

	private void FitCardWithinBackground()
	{
		if (!fitCardToBackground) return;
		if (cardInstance == null) return;
		if (backgroundImage == null) return;

		var bgRt = backgroundImage.rectTransform;
		var cardRt = cardInstance.transform as RectTransform;
		if (bgRt == null || cardRt == null) return;

		float bgW = Mathf.Max(1f, bgRt.rect.width - fitPadding * 2f);
		float bgH = Mathf.Max(1f, bgRt.rect.height - fitPadding * 2f);
		float cardW = Mathf.Max(1f, cardRt.rect.width);
		float cardH = Mathf.Max(1f, cardRt.rect.height);

		// 计算需要的等比缩放因子（仅缩小，不放大）
		float scaleW = bgW / cardW;
		float scaleH = bgH / cardH;
		float scale = Mathf.Min(1f, Mathf.Min(scaleW, scaleH));

		// 应用到本地缩放，仅影响卡牌实例
		cardRt.localScale = new Vector3(scale, scale, 1f);
	}


	private void TryApplyBarMaterialOverride()
	{
		if (!applyBarMaterialOverride) return;
		if (cardInstance == null) return;
		if (barMaterialOverride == null) return;

		var images = cardInstance.GetComponentsInChildren<Image>(true);
		for (int i = 0; i < images.Length; i++)
		{
			var img = images[i];
			if (img == null || string.IsNullOrEmpty(img.name)) continue;
			if (!NameMatchesKeywords(img.name)) continue;
			// 仅当原材质缺失或使用默认UI材质时覆盖，避免误改其它图片
			var mat = img.material;
			if (mat == null || (mat.shader != null && mat.shader.name == "UI/Default"))
			{
				img.material = barMaterialOverride;
			}
		}
	}

	private bool NameMatchesKeywords(string name)
	{
		if (barNameKeywords == null || barNameKeywords.Length == 0) return false;
		for (int k = 0; k < barNameKeywords.Length; k++)
		{
			var key = barNameKeywords[k];
			if (string.IsNullOrEmpty(key)) continue;
			if (name.IndexOf(key, System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
		}
		return false;
	}

    private void PlayFlyToMenuAnim()
    {
        if (!playFlyToMenu) return;
        if (cardInstance == null) return;
        if (menuButtonTarget == null) return;

        var rt = cardInstance.transform as RectTransform;
        if (rt == null) return;

        // 若有未完成序列，先终止
        if (flySequence != null && flySequence.IsActive())
        {
            flySequence.Kill();
            flySequence = null;
        }

        // 以世界坐标飞向按钮位置，同时缩放至0
        Vector3 targetWorldPos = menuButtonTarget.position;
        flySequence = DOTween.Sequence();
        flySequence.AppendInterval(Mathf.Max(0f, flyDelaySeconds));
        flySequence.Append(rt.DOMove(targetWorldPos, Mathf.Max(0.01f, flyDurationSeconds)).SetEase(flyEase));
        flySequence.Join(rt.DOScale(0f, Mathf.Max(0.01f, flyDurationSeconds)).SetEase(Ease.InQuad));
        flySequence.OnComplete(() =>
        {
            if (hideCardAfterFly) HideCard();
        });
    }

	private string ResolveEnglishCategory(BaseCardSO card)
	{
		if (card == null) return string.Empty;
		// 1) 优先使用第一个包含英文字母的标签
		if (card.tags != null)
		{
			for (int i = 0; i < card.tags.Length; i++)
			{
				string t = card.tags[i];
				if (!string.IsNullOrEmpty(t) && ContainsAsciiLetter(t)) return t;
			}
		}
		// 2) 尝试将 category 中文映射为英文
		string mapped = MapCnCategoryToEn(card.category);
		if (!string.IsNullOrEmpty(mapped)) return mapped;
		// 3) 如果 category 本身含英文，直接返回
		if (!string.IsNullOrEmpty(card.category) && ContainsAsciiLetter(card.category)) return card.category;
		// 4) 都没有则返回空
		return string.Empty;
	}

	private bool ContainsAsciiLetter(string s)
	{
		for (int i = 0; i < s.Length; i++)
		{
			char c = s[i];
			if ((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z')) return true;
		}
		return false;
	}

	private string MapCnCategoryToEn(string cn)
	{
		if (string.IsNullOrEmpty(cn)) return string.Empty;
		// 常见映射（可按需扩展）
		switch (cn)
		{
			// 材料侧分类
			case "基础酒类": return "Base Spirit";
			case "基酒": return "Base Spirit";
			case "调味酒类": return "Flavored Wine";
			case "调味酒": return "Flavored Wine";
			case "调味品": return "Seasoning";
			case "新鲜配料": return "Fresh Ingredient";
			// 鸡尾酒侧分类
			case "经典鸡尾酒": return "Classic";
			case "经典酒": return "Classic";
			case "热带风味": return "Tropical";
			case "热带酒": return "Tropical";
			case "特制鸡尾酒": return "Signature";
			case "特调酒": return "Signature";
			case "鸡尾酒": return "Cocktail";
			default: return string.Empty;
		}
	}
}


