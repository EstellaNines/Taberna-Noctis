using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace TabernaNoctis.CardSystem
{
    /// <summary>
    /// 卡槽组件 - 管理单个卡槽的卡牌显示
    /// </summary>
    public class CardSlot : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        #region 序列化字段

        [Header("卡槽配置")]
        [SerializeField]
        [Tooltip("卡槽编号（从1开始）")]
        private int slotIndex = 1;

        [SerializeField]
        [Tooltip("卡槽内的CardDisplay组件引用")]
        private CardDisplay cardDisplay;

        [Header("视觉反馈")]
        [SerializeField]
        [Tooltip("是否高亮显示（点击选择时使用）")]
        private bool isHighlighted = false;

        [SerializeField]
        [Tooltip("卡槽背景（可选）")]
        private Image slotBackground;

        [SerializeField]
        [Tooltip("高亮颜色")]
        private Color highlightColor = new Color(1f, 1f, 0.5f, 0.3f);

        [SerializeField]
        [Tooltip("正常颜色")]
        private Color normalColor = new Color(1f, 1f, 1f, 0.1f);

        [Header("点击/悬停描边")]
        [SerializeField]
        [Tooltip("点击/悬停高亮的描边Image（应位于层级最上方）")]
        private Image clickOutline;

        [SerializeField]
        [Tooltip("描边颜色")]
        private Color outlineColor = new Color(1f, 0.92f, 0.3f, 1f);

        [SerializeField]
        [Tooltip("是否仅由悬停控制描边（勾选后点击不再切换描边）")]
        private bool hoverDrivesOutline = true;

        #endregion

        #region 属性

        public int SlotIndex 
        { 
            get => slotIndex; 
            set => slotIndex = value; 
        }

        public CardDisplay CardDisplay => cardDisplay;

        public bool HasCard => cardDisplay != null && cardDisplay.GetCurrentCardData() != null;

        public RectTransform RectTransform => transform as RectTransform;

        #endregion

        #region 状态

        private bool isHoverActive = false;

        #endregion

        #region 公共方法

        /// <summary>
        /// 设置卡牌数据到卡槽
        /// </summary>
        public void SetCard(TabernaNoctis.Cards.BaseCardSO cardData)
        {
            if (cardDisplay == null)
            {
                Debug.LogWarning($"[CardSlot] 卡槽 {slotIndex} 没有CardDisplay组件", this);
                return;
            }

            // 确保显示并设置数据
            if (!cardDisplay.gameObject.activeSelf)
            {
                cardDisplay.gameObject.SetActive(true);
            }
            cardDisplay.SetCardData(cardData);
        }

        /// <summary>
        /// 清空卡槽
        /// </summary>
        public void ClearCard()
        {
            if (cardDisplay != null)
            {
                cardDisplay.ClearDisplay();
                // 清空后隐藏
                if (Application.isPlaying)
                {
                    cardDisplay.gameObject.SetActive(false);
                }
            }
            // 清空时也取消高亮与悬停
            isHighlighted = false;
            isHoverActive = false;
            UpdateVisuals();
        }

        /// <summary>
        /// 设置点击高亮状态（仅在 hoverDrivesOutline=false 时用于描边）
        /// </summary>
        public void SetHighlight(bool highlight)
        {
            isHighlighted = highlight;
            UpdateVisuals();
        }

        /// <summary>
        /// 设置悬停状态
        /// </summary>
        public void SetHover(bool hover)
        {
            isHoverActive = hover;
            UpdateVisuals();
        }

        /// <summary>
        /// 获取当前卡牌数据
        /// </summary>
        public TabernaNoctis.Cards.BaseCardSO GetCardData()
        {
            return cardDisplay?.GetCurrentCardData();
        }

        #endregion

        #region 事件

        public void OnPointerClick(PointerEventData eventData)
        {
            // 无卡时，不响应
            if (!HasCard) return;
			// 左键点击：始终广播点击事件；描边是否切换取决于 hoverDrivesOutline
			if (eventData.button == PointerEventData.InputButton.Left)
			{
				// 广播点击（供提交槽/合成使用）
				var data = GetCardData();
				if (data != null)
				{
					MessageManager.Send<TabernaNoctis.Cards.BaseCardSO>(MessageDefine.CARD_CLICKED, data);
				}

				// 若描边不是由悬停控制，则点击切换描边
				if (!hoverDrivesOutline)
				{
					isHighlighted = !isHighlighted;
					UpdateVisuals();
				}
			}
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!HasCard) return;
            var dispenser = GetComponentInParent<CardQueueDispenser>();
            if (dispenser != null)
                dispenser.HandleSlotHover(this);
            else
                SetHover(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            var dispenser = GetComponentInParent<CardQueueDispenser>();
            if (dispenser != null)
                dispenser.HandleSlotExit(this);
            else
                SetHover(false);
        }

        #endregion

        #region 私有方法

        private void Awake()
        {
            if (cardDisplay == null)
            {
                cardDisplay = GetComponentInChildren<CardDisplay>();
            }

            // 自动查找名为"CardOutline"的子对象作为描边（若未手动拖拽）
            if (clickOutline == null)
            {
                Transform outlineTr = transform.Find("CardOutline");
                if (outlineTr != null)
                {
                    clickOutline = outlineTr.GetComponent<Image>();
                }
            }

            // 运行时默认隐藏卡牌，等待派发时再显示
            if (Application.isPlaying && cardDisplay != null)
            {
                cardDisplay.gameObject.SetActive(false);
            }

            // 初始化描边
            if (clickOutline != null)
            {
                clickOutline.color = outlineColor;
                clickOutline.raycastTarget = false; // 避免遮挡点击与悬停事件
                clickOutline.enabled = false;
            }
            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            if (slotBackground != null)
            {
                slotBackground.color = isHighlighted ? highlightColor : normalColor;
            }

            if (clickOutline != null)
            {
                clickOutline.color = outlineColor;
                bool showByHover = HasCard && isHoverActive;
                bool showByClick = !hoverDrivesOutline && isHighlighted;
                clickOutline.enabled = showByHover || showByClick;
            }
        }

        #endregion

        #region 编辑器辅助

#if UNITY_EDITOR
        [ContextMenu("自动查找CardDisplay")]
        private void AutoFindCardDisplay()
        {
            cardDisplay = GetComponentInChildren<CardDisplay>();
            if (cardDisplay != null)
            {
                Debug.Log($"[CardSlot] 找到CardDisplay组件: {cardDisplay.name}");
            }
            else
            {
                Debug.LogWarning("[CardSlot] 未找到CardDisplay组件");
            }
        }
#endif

        #endregion
    }
}

