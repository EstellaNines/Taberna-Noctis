using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TabernaNoctis.Cards;
using TN.UI;
using System.Collections.Generic;

namespace TabernaNoctis.CardSystem
{
    /// <summary>
    /// 卡牌显示组件 - 负责将CardSO数据显示到UI�?
    /// </summary>
    [ExecuteAlways]  // �ڱ༭ģʽ��Ҳִ�У�����ʵʱԤ��
    public class CardDisplay : MonoBehaviour
    {
        #region 序列化字�? - 基�?�UI组件

        [Header("背景组件")]
        [SerializeField]
        [Tooltip("卡牌外边框背�?（Outline�?")]
        private Image outlineBackground;

        [SerializeField]
        [Tooltip("卡牌主背�?")]
        private Image mainBackground;

        [SerializeField]
		[Tooltip("文字区域背景（单个）")]
		private Image textBackground;

		[SerializeField]
		[Tooltip("文字区域背景（多个）")]
		private Image[] textBackgrounds;

        [Header("基�?�显示组件")]
        [SerializeField]
        [Tooltip("显示卡牌标�?�的文本")]
        private TextMeshProUGUI tagsText;

        [SerializeField]
        [Tooltip("显示卡牌名称的文�?")]
        private TextMeshProUGUI nameText;

        [SerializeField]
        [Tooltip("显示价格的文�?")]
        private TextMeshProUGUI priceText;

        [SerializeField]
        [Tooltip("显示卡牌图片的Image")]
        private Image cardImage;

        #endregion

        #region 序列化字�? - 状态滑�?

        [Header("状态效果显示（5�?�?")]
        [SerializeField]
        [Tooltip("忙�?�状�?")]
        private StateEffectBar busyBar;

        [SerializeField]
        [Tooltip("急躁状�?")]
        private StateEffectBar impatientBar;

        [SerializeField]
        [Tooltip("烦闷状�?")]
        private StateEffectBar boredBar;

        [SerializeField]
        [Tooltip("挑剔状�?")]
        private StateEffectBar pickyBar;

        [SerializeField]
        [Tooltip("友好状�?")]
        private StateEffectBar friendlyBar;

        #endregion

        #region 序列化字�? - 颜色配置

        [Header("显示设置")]
        [Tooltip("始终使用英文名称（已固定为true）")]
        private const bool useEnglishName = true;  // 固定为true，始终显示英文

        [Header("标�?��?�色配置")]
		[SerializeField]
		private Color baseSpiritsColor = new Color(0.70f, 0.85f, 1.00f);      // 基酒 - 浅蓝色

		[SerializeField]
		private Color flavoredWineColor = new Color(1.00f, 0.95f, 0.70f);    // 调味酒 - 浅黄色

		[SerializeField]
		private Color flavoringColor = new Color(1.00f, 0.70f, 0.70f);       // 调味品 - 浅红色

		[SerializeField]
		private Color freshIngredientColor = new Color(0.80f, 1.00f, 0.80f); // 新鲜配料 - 浅绿色

		[SerializeField]
		private Color classicColor = new Color(0.53f, 0.81f, 0.98f);         // 经典酒 - 天蓝色

		[SerializeField]
		private Color tropicalColor = new Color(1.00f, 0.80f, 0.60f);        // 热带酒 - 浅橙色

		[SerializeField]
		private Color specialtyColor = new Color(0.90f, 0.80f, 1.00f);       // 特调酒 - 浅紫色

        #endregion

        #region 私有字�??

        private BaseCardSO currentCardData;
        private bool isInitialized = false;
        
        // �༭��ģʽר��
        [SerializeField]
        [HideInInspector]
        private BaseCardSO editorPreviewCard;  // �ڱ༭����Ԥ���Ŀ���

        #endregion
        
        #region Unity��������
        
        private void OnEnable()
        {
            // �༭��ģʽ�£������Ԥ���������Զ���ʾ
            #if UNITY_EDITOR
            if (!Application.isPlaying && editorPreviewCard != null)
            {
                SetCardData(editorPreviewCard);
            }
            #endif
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // �༭��ģʽ���Զ�ˢ����ʾ
            if (!Application.isPlaying && currentCardData != null)
            {
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    if (this != null && currentCardData != null)
                    {
                        DisplayCardData();
                    }
                };
            }
        }
#endif
        
        #endregion

        #region �?共方�?

        /// <summary>
        /// 设置并显示卡牌数�?
        /// </summary>
        public void SetCardData(BaseCardSO cardData)
        {
            if (cardData == null)
            {
                Debug.LogError("[CardDisplay] 卡牌数据为空�?", this);
                return;
            }

            currentCardData = cardData;
            
            #if UNITY_EDITOR
            // �ڱ༭��ģʽ�±���Ԥ����������
            if (!Application.isPlaying)
            {
                editorPreviewCard = cardData;
            }
            #endif
            
            if (!isInitialized)
            {
                Initialize();
            }

            DisplayCardData();
        }

        /// <summary>
        /// 获取当前显示的卡牌数�?
        /// </summary>
        public BaseCardSO GetCurrentCardData()
        {
            return currentCardData;
        }

        /// <summary>
        /// 清空显示
        /// </summary>
        public void ClearDisplay()
        {
            if (nameText != null)
                nameText.text = "";
            
            if (tagsText != null)
                tagsText.text = "";
            
            if (priceText != null)
                priceText.text = "";
            
            if (cardImage != null)
                cardImage.sprite = null;

            ClearAllBars();
        }

        /// <summary>
        /// 刷新显示（当数据�?外部�?改后调用�?
        /// </summary>
        public void RefreshDisplay()
        {
            if (currentCardData != null)
            {
                DisplayCardData();
            }
        }

        #endregion

        #region 私有方法 - 初�?�化

        private void Initialize()
        {
            ValidateComponents();
            isInitialized = true;
        }

        private void ValidateComponents()
        {
            if (nameText == null)
                Debug.LogWarning("[CardDisplay] �?设置nameText引用", this);
            
            if (tagsText == null)
                Debug.LogWarning("[CardDisplay] �?设置tagsText引用", this);
            
            if (priceText == null)
                Debug.LogWarning("[CardDisplay] �?设置priceText引用", this);
            
            if (cardImage == null)
                Debug.LogWarning("[CardDisplay] �?设置cardImage引用", this);

            // 验证状态条
            if (busyBar == null || busyBar.slider == null)
                Debug.LogWarning("[CardDisplay] 忙�?�状态条�?正确设置", this);
            
            if (impatientBar == null || impatientBar.slider == null)
                Debug.LogWarning("[CardDisplay] 急躁状态条�?正确设置", this);
            
            if (boredBar == null || boredBar.slider == null)
                Debug.LogWarning("[CardDisplay] 烦闷状态条�?正确设置", this);
            
            if (pickyBar == null || pickyBar.slider == null)
                Debug.LogWarning("[CardDisplay] 挑剔状态条�?正确设置", this);
            
            if (friendlyBar == null || friendlyBar.slider == null)
                Debug.LogWarning("[CardDisplay] 友好状态条�?正确设置", this);
        }

        #endregion

        #region 私有方法 - 显示逻辑

        private void DisplayCardData()
        {
            // 应用颜色基调
            ApplyThemeColor();

            // 显示名称
            DisplayName();

            // 显示标�??
            DisplayTags();

            // 显示价格
            DisplayPrice();

            // 显示图片
            DisplayImage();

            // 显示状态效�?
            DisplayEffects();
        }

        private void ApplyThemeColor()
        {
            if (currentCardData == null) return;

            Color themeColor = currentCardData.themeColor;

            // 应用到�?�边框（颜色稍深�?
            if (outlineBackground != null)
            {
                Color darkerTheme = new Color(
                    themeColor.r * 0.8f,
                    themeColor.g * 0.8f,
                    themeColor.b * 0.8f,
                    1f
                );
                outlineBackground.color = darkerTheme;
            }

            // 应用到主背景
            if (mainBackground != null)
            {
                mainBackground.color = themeColor;
            }

			// 应用到文字背景（可多个，颜色更浅，带透明度）
			Color lighterTheme = new Color(
				themeColor.r * 1.05f,
				themeColor.g * 1.05f,
				themeColor.b * 1.05f,
				0.9f
			);
			if (textBackground != null)
			{
				textBackground.color = lighterTheme;
			}
			if (textBackgrounds != null)
			{
				for (int i = 0; i < textBackgrounds.Length; i++)
				{
					var img = textBackgrounds[i];
					if (img != null) img.color = lighterTheme;
				}
			}
        }

        private void DisplayName()
        {
            if (nameText == null) return;

            // 始终显示英文名称
            nameText.text = currentCardData.nameEN;
        }

        private void DisplayTags()
        {
            if (tagsText == null) return;

            if (currentCardData.tags == null || currentCardData.tags.Length == 0)
            {
                tagsText.text = "";
                return;
            }

            // 将标签转换为英文
            string[] displayTags = new string[currentCardData.tags.Length];
            for (int i = 0; i < currentCardData.tags.Length; i++)
            {
                displayTags[i] = GetTagEnglishName(currentCardData.tags[i]);
            }

            // 将标签数组合并为字�?�串
            string tagsDisplay = string.Join(" | ", displayTags);
            tagsText.text = tagsDisplay;

            // 设置标�?�文字�?�色（使用�??一�?标�?�的颜色�?
            Color tagColor = GetTagColor(currentCardData.tags[0]);
            tagsText.color = tagColor;
        }

        private string GetTagEnglishName(string chineseTag)
        {
            // 使用if-else避免编码问题
            if (chineseTag == "基酒") return "Base Spirit";
            if (chineseTag == "调味酒") return "Flavored Wine";
            if (chineseTag == "调味品") return "Flavoring";
            if (chineseTag == "新鲜配料") return "Fresh Ingredient";
            if (chineseTag == "经典酒") return "Classic";
            if (chineseTag == "热带酒") return "Tropical";
            if (chineseTag == "特调酒") return "Specialty";
            
            return chineseTag;
        }

        private void DisplayPrice()
        {
            if (priceText == null) return;

            if (currentCardData is MaterialCardSO material)
            {
                // 材料：显示单�?
                priceText.text = $"${material.price}";
            }
            else if (currentCardData is CocktailCardSO cocktail)
            {
                // 鸡尾酒：显示�?�?
                priceText.text = $"${cocktail.price}";
            }
        }

        private void DisplayImage()
        {
            if (cardImage == null) return;

            // 从Resources加载Sprite
            if (!string.IsNullOrEmpty(currentCardData.uiPath))
            {
                Sprite sprite = Resources.Load<Sprite>(currentCardData.uiPath);
                if (sprite != null)
                {
                    cardImage.sprite = sprite;
                }
                else
                {
                    Debug.LogWarning($"[CardDisplay] 找不到精�?: {currentCardData.uiPath}", this);
                }
            }

            // 如果已经有�?��?�精灵，使用预�?�精�?
            if (currentCardData.uiSpritePreview != null)
            {
                cardImage.sprite = currentCardData.uiSpritePreview;
            }
        }

        private void DisplayEffects()
        {
            CardEffects effects = currentCardData.effects;

            // 设置各个状态条的�?
            SetEffectBar(busyBar, "忙�??", effects.busy);
            SetEffectBar(impatientBar, "急躁", effects.impatient);
            SetEffectBar(boredBar, "烦闷", effects.bored);
            SetEffectBar(pickyBar, "挑剔", effects.picky);
            SetEffectBar(friendlyBar, "友好", effects.friendly);
        }

        private void SetEffectBar(StateEffectBar bar, string stateName, int value)
        {
            if (bar == null || bar.slider == null)
                return;

            // 设置滑动条�?
            bar.slider.SetValue(value, false);

            // 更新数值文�?
            if (bar.valueText != null)
            {
                if (value > 0)
                    bar.valueText.text = $"+{value}";
                else if (value < 0)
                    bar.valueText.text = value.ToString();
                else
                    bar.valueText.text = "0";

                // 设置颜色
                if (value > 0)
                    bar.valueText.color = new Color(0.3f, 0.9f, 0.4f);  // 绿色
                else if (value < 0)
                    bar.valueText.color = new Color(0.9f, 0.3f, 0.3f);  // 红色
                else
                    bar.valueText.color = new Color(0.6f, 0.6f, 0.6f);  // 灰色
            }

            // 更新图标（�?�果需要）
            if (bar.iconImage != null)
            {
                // �?以根�?状态类型�?�置不同的图�?
                // 这里�?以扩展加载�?�应的状态图�?
            }
        }

        private void ClearAllBars()
        {
            if (busyBar != null && busyBar.slider != null)
                busyBar.slider.SetValue(0, false);
            
            if (impatientBar != null && impatientBar.slider != null)
                impatientBar.slider.SetValue(0, false);
            
            if (boredBar != null && boredBar.slider != null)
                boredBar.slider.SetValue(0, false);
            
            if (pickyBar != null && pickyBar.slider != null)
                pickyBar.slider.SetValue(0, false);
            
            if (friendlyBar != null && friendlyBar.slider != null)
                friendlyBar.slider.SetValue(0, false);
        }

		private Color GetTagColor(string tag)
		{
			// 标签文字颜色用更深一些的色调，提升可读性
			if (tag == "基酒") return new Color(0.40f, 0.60f, 0.95f);
			if (tag == "调味酒") return new Color(0.95f, 0.80f, 0.30f);
			if (tag == "调味品") return new Color(0.95f, 0.40f, 0.40f);
			if (tag == "新鲜配料") return new Color(0.35f, 0.75f, 0.45f);
			if (tag == "经典酒") return new Color(0.25f, 0.55f, 0.95f);
			if (tag == "热带酒") return new Color(0.95f, 0.55f, 0.25f);
			if (tag == "特调酒") return new Color(0.65f, 0.45f, 0.95f);
			
			return Color.gray;
		}

        #endregion

        #region 编辑器辅�?

#if UNITY_EDITOR
        [ContextMenu("测试：加载�??一�?材料")]
        private void TestLoadFirstMaterial()
        {
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:MaterialCardSO");
            if (guids.Length > 0)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                MaterialCardSO material = UnityEditor.AssetDatabase.LoadAssetAtPath<MaterialCardSO>(path);
                SetCardData(material);
            }
        }

        [ContextMenu("测试：加载�??一�?鸡尾�?")]
        private void TestLoadFirstCocktail()
        {
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:CocktailCardSO");
            if (guids.Length > 0)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                CocktailCardSO cocktail = UnityEditor.AssetDatabase.LoadAssetAtPath<CocktailCardSO>(path);
                SetCardData(cocktail);
            }
        }

        [ContextMenu("清空显示")]
        private void TestClearDisplay()
        {
            ClearDisplay();
        }

        [ContextMenu("�?动查找组�?")]
        private void AutoFindComponents()
        {
            // 尝试�?动查找组�?
            if (nameText == null)
                nameText = transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
            
            if (tagsText == null)
                tagsText = transform.Find("TagsText")?.GetComponent<TextMeshProUGUI>();
            
            if (priceText == null)
                priceText = transform.Find("PriceText")?.GetComponent<TextMeshProUGUI>();
            
            if (cardImage == null)
                cardImage = transform.Find("CardImage")?.GetComponent<Image>();

            Debug.Log("[CardDisplay] �?动查找组件完�?");
        }
#endif

        #endregion

        #region 内部�? - 状态效果条

        /// <summary>
        /// 状态效果条数据结构
        /// </summary>
        [System.Serializable]
        public class StateEffectBar
        {
            [Tooltip("状态名称（用于调试�?")]
            public string stateName;

            [Tooltip("父�?�象")]
            public GameObject parentObject;

            [Tooltip("状态图标Image")]
            public Image iconImage;

            [Tooltip("离散滑动条组�?")]
            public DiscreteSlider slider;

            [Tooltip("数值显示文�?")]
            public TextMeshProUGUI valueText;
        }

        #endregion
    }
}

