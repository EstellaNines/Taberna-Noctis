using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TabernaNoctis.Cards;
using DG.Tweening;
using TabernaNoctis.QueueSystem;

namespace TabernaNoctis.CardSystem
{
	/// <summary>
	/// 卡牌队列派发器 - 逐张派发卡牌到卡槽，带移动动画
	/// </summary>
	public class CardQueueDispenser : MonoBehaviour
	{
		#region 序列化字段

		[Header("卡槽容器")]
		[SerializeField]
		[Tooltip("卡槽的父对象（通常在ScrollView的Content中）")]
		private Transform slotsContainer;

		[SerializeField]
		[Tooltip("卡槽预制件（包含CardDisplay组件）")]
		private GameObject cardSlotPrefab;

	[SerializeField]
	[Tooltip("最大卡槽数量（限制同时显示的卡槽数）")]
	private int maxSlots = 10;

		[Header("派发配置")]
		[SerializeField]
		[Tooltip("派发间隔（秒）")]
		private float dispenseInterval = 0.5f;

		[SerializeField]
		[Tooltip("是否自动开始派发")]
		private bool autoStart = false;

		[Header("动画配置")]
		[SerializeField]
		[Tooltip("卡牌移动动画时长")]
		private float moveAnimationDuration = 0.3f;

		[SerializeField]
		[Tooltip("移动动画曲线类型")]
		private Ease moveEaseType = Ease.OutCubic;

		[SerializeField]
		[Tooltip("新卡片淡入时长")]
		private float fadeInDuration = 0.2f;

		[Header("滚动视图")]
		[SerializeField]
		[Tooltip("ScrollRect组件（可选，用于自动滚动）")]
		private ScrollRect scrollRect;

		[SerializeField]
		[Tooltip("是否自动滚动到最新卡牌（横向时滚动到最左，纵向时滚动到顶部）")]
		private bool autoScrollToLatest = true;

		[SerializeField]
		[Tooltip("自动滚动时是否跟随填充末端（横向最右/纵向最下）。默认true，若为false则跟随最新插入位置（横向最左/纵向最上）。")]
		private bool autoScrollFollowTail = true;

		[Header("内容尺寸")]
		[SerializeField]
		[Tooltip("派发或创建卡槽后，是否自动调整Content宽度以适配横向滚动")]
		private bool autoResizeContent = true;

		[SerializeField]
		[Tooltip("使用Viewport宽度作为Content最小宽度；否则使用minContentWidth数值")]
		private bool useViewportAsMinWidth = true;

		[SerializeField]
		[Tooltip("Content最小宽度（仅在不使用Viewport时生效）")]
		private float minContentWidth = 1920f;

		[SerializeField]
		[Tooltip("额外的Content水平内边距（像素），用于在最右侧预留空白")]
		private float extraContentPadding = 0f;

		#endregion

		#region 私有字段

		private List<CardSlot> cardSlots = new List<CardSlot>();
		private PooledQueue<BaseCardSO> cardQueue = new PooledQueue<BaseCardSO>(16);
		private Coroutine dispenseCoroutine;
		private bool isDispensing = false;

		private RectTransform slotsContainerRect;
		private HorizontalLayoutGroup horizontalLayout;
		private ContentSizeFitter contentSizeFitter;

		private CardSlot currentHoverSlot;

		#endregion

		#region Unity生命周期

		private void Awake()
		{
			ValidateComponents();
			CacheLayoutComponents();
			InitializeSlots();
			UpdateContentSizeAndLayout();
		}

		private void Start()
		{
			if (autoStart && cardQueue.Count > 0)
			{
				StartDispensing();
			}
		}

		#endregion

		#region 公共方法 - 队列管理

		/// <summary>
		/// 添加卡牌到派发队列
		/// </summary>
		public void EnqueueCard(BaseCardSO cardData)
		{
			if (cardData == null)
			{
				Debug.LogWarning("[CardQueueDispenser] 尝试添加空卡牌数据");
				return;
			}

			cardQueue.Enqueue(cardData);
			Debug.Log($"[CardQueueDispenser] 添加卡牌到队列: {cardData.nameEN}，当前队列长度: {cardQueue.Count}");
		}

		/// <summary>
		/// 批量添加卡牌到队列
		/// </summary>
		public void EnqueueCards(List<BaseCardSO> cards)
		{
			if (cards == null || cards.Count == 0) return;

			foreach (var card in cards)
			{
				if (card != null)
				{
					cardQueue.Enqueue(card);
				}
			}

			Debug.Log($"[CardQueueDispenser] 批量添加 {cards.Count} 张卡牌，当前队列长度: {cardQueue.Count}");
		}

		/// <summary>
		/// 清空队列
		/// </summary>
		public void ClearQueue()
		{
			cardQueue.Clear();
			Debug.Log("[CardQueueDispenser] 队列已清空");
		}

		/// <summary>
		/// 获取当前队列长度
		/// </summary>
		public int GetQueueCount()
		{
			return cardQueue.Count;
		}

		#endregion

		#region 公共方法 - 派发控制

		/// <summary>
		/// 开始派发卡牌
		/// </summary>
		public void StartDispensing()
		{
			if (isDispensing)
			{
				Debug.LogWarning("[CardQueueDispenser] 已在派发中");
				return;
			}

            if (cardQueue.Count == 0)
			{
				Debug.LogWarning("[CardQueueDispenser] 队列为空，无法开始派发");
				return;
			}

			dispenseCoroutine = StartCoroutine(DispenseCardsCoroutine());
		}

		/// <summary>
		/// 停止派发
		/// </summary>
		public void StopDispensing()
		{
			if (dispenseCoroutine != null)
			{
				StopCoroutine(dispenseCoroutine);
				dispenseCoroutine = null;
			}

			isDispensing = false;
			Debug.Log("[CardQueueDispenser] 停止派发");
		}

		/// <summary>
		/// 立即派发下一张卡牌（手动触发）
		/// </summary>
		public void DispenseNextCard()
		{
            if (cardQueue.Count == 0)
			{
				Debug.LogWarning("[CardQueueDispenser] 队列为空");
				return;
			}

            if (cardQueue.TryDequeue(out var nextCard))
            {
                DispenseCard(nextCard);
            }
		}

		/// <summary>
		/// 清空所有卡槽
		/// </summary>
		public void ClearAllSlots()
		{
			foreach (var slot in cardSlots)
			{
				if (slot != null)
				{
					slot.ClearCard();
				}
			}

			Debug.Log("[CardQueueDispenser] 所有卡槽已清空");
		}

		#endregion

		#region 私有方法 - 初始化

		private void ValidateComponents()
		{
			if (slotsContainer == null)
			{
				Debug.LogError("[CardQueueDispenser] 未设置卡槽容器！", this);
			}

			if (cardSlotPrefab == null)
			{
				Debug.LogWarning("[CardQueueDispenser] 未设置卡槽预制件", this);
			}
		}

		private void CacheLayoutComponents()
		{
			slotsContainerRect = slotsContainer as RectTransform;
			if (slotsContainerRect != null)
			{
				horizontalLayout = slotsContainerRect.GetComponent<HorizontalLayoutGroup>();
				contentSizeFitter = slotsContainerRect.GetComponent<ContentSizeFitter>();
			}
		}

	private void InitializeSlots()
	{
		// 仅查找现有卡槽，不自动创建
		CardSlot[] existingSlots = slotsContainer.GetComponentsInChildren<CardSlot>(true);
		
		if (existingSlots.Length > 0)
		{
			cardSlots.AddRange(existingSlots);
			// 为现有卡槽编号
			for (int i = 0; i < cardSlots.Count; i++)
			{
				if (cardSlots[i] != null)
				{
					cardSlots[i].SlotIndex = i + 1;
				}
			}
			Debug.Log($"[CardQueueDispenser] 找到 {cardSlots.Count} 个现有卡槽");
		}
		else
		{
			Debug.Log("[CardQueueDispenser] 未找到现有卡槽，将按需创建");
		}
	}

	private CardSlot CreateSlotOnDemand()
	{
		if (cardSlotPrefab == null)
		{
			Debug.LogError("[CardQueueDispenser] 无法创建卡槽：未设置卡槽预制件");
			return null;
		}

		int newIndex = cardSlots.Count + 1;
		GameObject slotObj = Instantiate(cardSlotPrefab, slotsContainer);
		slotObj.name = $"CardSlot_{newIndex:D2}";

		CardSlot slot = slotObj.GetComponent<CardSlot>();
		if (slot == null)
		{
			slot = slotObj.AddComponent<CardSlot>();
		}

		slot.SlotIndex = newIndex;
		cardSlots.Add(slot);

		// 新卡槽插入到最前面（索引0）
		slot.transform.SetSiblingIndex(0);

		return slot;
	}

		#endregion

		#region 私有方法 - 派发逻辑

    private IEnumerator DispenseCardsCoroutine()
	{
		isDispensing = true;

        while (cardQueue.Count > 0)
		{
            if (cardQueue.TryDequeue(out var nextCard))
            {
                DispenseCard(nextCard);
            }

			// 等待指定间隔
			yield return new WaitForSeconds(dispenseInterval);
		}

		isDispensing = false;
		// 派发完成后输出当前队列顺序
		LogCurrentCardOrder();
	}
	
	/// <summary>
	/// 输出当前卡槽中的卡牌队列顺序
	/// </summary>
	private void LogCurrentCardOrder()
	{
		List<string> cardNames = new List<string>();
		for (int i = 0; i < cardSlots.Count; i++)
		{
			var slot = cardSlots[i];
			if (slot != null && slot.HasCard)
			{
				var cardData = slot.GetCardData();
				if (cardData != null)
				{
					cardNames.Add(cardData.nameEN);
				}
			}
		}
		
		if (cardNames.Count > 0)
		{
			Debug.Log($"[CardQueue] 当前卡牌队列（从左到右）: {string.Join(" → ", cardNames)}");
		}
		else
		{
			Debug.Log("[CardQueue] 当前队列为空");
		}
	}

	private void DispenseCard(BaseCardSO cardData)
	{
		if (cardData == null) return;

		// 1. 统计当前有卡的卡槽数，决定是否需要新卡槽
		int usedSlotsCount = 0;
		for (int i = 0; i < cardSlots.Count; i++)
		{
			if (cardSlots[i] != null && cardSlots[i].HasCard)
			{
				usedSlotsCount++;
			}
		}

		// 2. 如果未达上限且有卡需要右移，创建新卡槽接收
		if (usedSlotsCount > 0 && usedSlotsCount < maxSlots)
		{
			CreateSlotOnDemand();
		}

		// 3. 将所有已有卡牌向后移动一个卡槽
		ShiftCardsToRight();

		// 4. 确保至少有一个卡槽用于显示新卡
		if (cardSlots.Count == 0)
		{
			CreateSlotOnDemand();
		}

		// 5. 在第一个卡槽显示新卡牌
		if (cardSlots.Count > 0 && cardSlots[0] != null)
		{
			cardSlots[0].SetCard(cardData);
			
			// 淡入动画
			PlayFadeInAnimation(cardSlots[0]);

			// 在更新布局前记录“最新卡”的目标可见区域（用于更精准的滚动）
			if (autoScrollToLatest && scrollRect != null)
			{
				StartCoroutine(ScrollAfterLayout());
			}
		}

		// 6. 刷新Content尺寸；滚动动作放到布局重建后一帧
		UpdateContentSizeAndLayout();
	}

	private void ShiftCardsToRight()
	{
		// 从后往前遍历，将每个卡槽的卡牌移动到下一个卡槽
		for (int i = cardSlots.Count - 1; i > 0; i--)
		{
			CardSlot currentSlot = cardSlots[i];
			CardSlot previousSlot = cardSlots[i - 1];

			if (currentSlot == null || previousSlot == null) continue;

			// 获取前一个卡槽的卡牌数据
			BaseCardSO cardData = previousSlot.GetCardData();
			
			if (cardData != null)
			{
				// 移动卡牌到当前卡槽
				currentSlot.SetCard(cardData);
				
				// 播放移动动画
				PlayMoveAnimation(currentSlot);
			}
			else
			{
				// 前一个卡槽为空，清空当前卡槽
				currentSlot.ClearCard();
			}
		}
	}

		#endregion

		#region 私有方法 - 布局与滚动

		private float GetBaselineWidth()
		{
			if (!useViewportAsMinWidth) return minContentWidth;
			if (scrollRect == null) return minContentWidth;
			var viewport = scrollRect.viewport != null ? scrollRect.viewport : (RectTransform)scrollRect.transform;
			return viewport.rect.width;
		}

		private void UpdateContentSizeAndLayout()
		{
			if (!autoResizeContent || slotsContainerRect == null) return;

			// 先让布局系统计算好首选尺寸（如果存在）
			if (contentSizeFitter != null)
			{
				contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
				contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
				LayoutRebuilder.ForceRebuildLayoutImmediate(slotsContainerRect);
			}

			// 只根据“已派发的卡槽数量”计算所需宽度（空卡槽不计入）
			float spacing = horizontalLayout != null ? horizontalLayout.spacing : 0f;
			int padLeft = horizontalLayout != null ? horizontalLayout.padding.left : 0;
			int padRight = horizontalLayout != null ? horizontalLayout.padding.right : 0;

			float requiredWidth = 0f;
			int usedCount = 0;
			for (int i = 0; i < cardSlots.Count; i++)
			{
				var slot = cardSlots[i];
				if (slot == null || !slot.HasCard) continue;
				var rt = slot.RectTransform;
				if (rt != null) {
					requiredWidth += rt.rect.width;
					usedCount++;
				}
			}

			if (usedCount > 1)
			{
				requiredWidth += spacing * (usedCount - 1);
			}

			requiredWidth += padLeft + padRight + extraContentPadding;

			float baseline = GetBaselineWidth();
			float finalWidth = Mathf.Max(baseline, requiredWidth);

			slotsContainerRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, finalWidth);
			LayoutRebuilder.ForceRebuildLayoutImmediate(slotsContainerRect);
		}

		private void ScrollToLatest()
		{
			if (scrollRect == null) return;

			// 横向：autoScrollFollowTail=true => 滚到最右；false => 最左（最新插入）
			// 纵向：autoScrollFollowTail=true => 滚到最下；false => 最上
			if (scrollRect.horizontal)
			{
				float target = autoScrollFollowTail ? 1f : 0f;
				DOTween.To(() => scrollRect.horizontalNormalizedPosition,
					x => scrollRect.horizontalNormalizedPosition = x,
					target,
					0.25f).SetEase(Ease.OutCubic);
			}
			else
			{
				float target = autoScrollFollowTail ? 0f : 1f;
				DOTween.To(() => scrollRect.verticalNormalizedPosition,
					x => scrollRect.verticalNormalizedPosition = x,
					target,
					0.25f).SetEase(Ease.OutCubic);
			}
		}

		private IEnumerator ScrollAfterLayout()
		{
			// 等待一帧，确保布局及Content宽度已更新
			yield return null;
			ScrollToLatest();
		}

		#endregion

		#region 私有方法 - 动画

		private void PlayFadeInAnimation(CardSlot slot)
		{
			if (slot == null || slot.CardDisplay == null) return;

			CanvasGroup canvasGroup = slot.CardDisplay.GetComponent<CanvasGroup>();
			if (canvasGroup == null)
			{
				canvasGroup = slot.CardDisplay.gameObject.AddComponent<CanvasGroup>();
			}

			// 淡入动画
			canvasGroup.alpha = 0f;
			canvasGroup.DOFade(1f, fadeInDuration).SetEase(Ease.OutQuad);
		}

		private void PlayMoveAnimation(CardSlot slot)
		{
			if (slot == null || slot.RectTransform == null) return;

			// 使用DoTween的Punch动画表示移动
			slot.RectTransform
				.DOPunchScale(new Vector3(0.1f, 0.1f, 0f), moveAnimationDuration, 1, 0.5f)
				.SetEase(moveEaseType);
		}

		#endregion

		#region 公共方法 - 工具方法

		/// <summary>
		/// 获取指定索引的卡槽
		/// </summary>
		public CardSlot GetSlot(int index)
		{
			if (index < 0 || index >= cardSlots.Count)
			{
				Debug.LogWarning($"[CardQueueDispenser] 卡槽索引 {index} 超出范围");
				return null;
			}

			return cardSlots[index];
		}

		/// <summary>
		/// 获取所有卡槽
		/// </summary>
		public List<CardSlot> GetAllSlots()
		{
			return new List<CardSlot>(cardSlots);
		}

		/// <summary>
		/// 获取所有已显示的卡牌数据
		/// </summary>
		public List<BaseCardSO> GetAllDisplayedCards()
		{
			List<BaseCardSO> cards = new List<BaseCardSO>();
			foreach (var slot in cardSlots)
			{
				if (slot != null && slot.HasCard)
				{
					cards.Add(slot.GetCardData());
				}
			}
			return cards;
		}

		#endregion

		#region 悬停互斥管理

		internal void HandleSlotHover(CardSlot hovered)
		{
			if (currentHoverSlot == hovered) { hovered.SetHover(true); return; }
			// 取消其它槽的悬停
			if (currentHoverSlot != null)
			{
				currentHoverSlot.SetHover(false);
			}
			currentHoverSlot = hovered;
			if (currentHoverSlot != null)
			{
				currentHoverSlot.SetHover(true);
			}
		}

		internal void HandleSlotExit(CardSlot slot)
		{
			if (currentHoverSlot == slot)
			{
				currentHoverSlot.SetHover(false);
				currentHoverSlot = null;
			}
		}

		#endregion
	}
}

