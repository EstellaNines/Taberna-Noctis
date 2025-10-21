using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TabernaNoctis.Cards;

namespace TabernaNoctis.CardSystem
{
	/// <summary>
	/// 卡槽拖拽组件 - 让卡槽中的卡牌可以拖拽
	/// 适配自原有的Card.cs拖拽系统
	/// </summary>
	[RequireComponent(typeof(CardSlot))]
	public class CardSlotDraggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
	{
		#region 序列化字段

		[Header("拖拽配置")]
		[SerializeField]
		[Tooltip("是否启用拖拽")]
		private bool draggingEnabled = true;

		[SerializeField]
		[Tooltip("拖拽时的透明度")]
		private float dragAlpha = 0.6f;

		[SerializeField]
		[Tooltip("拖拽时临时提升Canvas排序，避免被上层UI遮挡")]
		private bool elevateSortingOnDrag = true;

		[SerializeField]
		[Tooltip("拖拽时使用的排序Order（当elevateSortingOnDrag=true时生效）")]
		private int dragSortingOrder = 5000;

		[SerializeField]
		[Tooltip("可选：拖拽时暂时重挂到此容器（通常为顶层Canvas下的一个专用Drag层）以避免被RectMask2D裁剪")]
		private Transform dragRootOverride;

		#endregion

		#region 私有字段

		private CardSlot cardSlot;
		private RectTransform rectTransform;
		private Canvas rootCanvas;
		private CanvasGroup canvasGroup;
		private Vector2 originalAnchoredPosition;
		private Transform originalParent;
		private int originalSiblingIndex;
		private bool isDragging = false;
		private bool dropHandled = false;

		// 提升排序相关（非Ghost模式使用）
		private Canvas tempOrSelfCanvas;
		private bool tempCanvasAdded;
		private bool prevOverrideSorting;
		private int prevSortingOrder;

		// 拖拽影子（仅显示精灵）
		[SerializeField]
		[Tooltip("启用拖拽影子，仅显示卡牌精灵，不移动原卡槽")]
		private bool useDragGhost = true;

		[SerializeField]
		[Range(0f, 1f)]
		[Tooltip("拖拽影子透明度（0~1）")]
		private float ghostAlpha = 0.8f;

		[SerializeField]
		[Tooltip("拖拽影子缩放倍数")]
		private float ghostScale = 1f;

		[SerializeField]
		[Tooltip("拖拽影子固定尺寸（像素）")]
		private Vector2 ghostSize = new Vector2(360f, 360f);

		private RectTransform ghostRect;
		private Image ghostImage;
		private Transform ghostParent;
		private Canvas ghostCanvas;

		// 原卡槽可视隐藏还原
		private CanvasGroup originalVisualCanvasGroup;
		private bool originalVisualCanvasGroupAdded;
		private float originalVisualAlpha;

		#endregion

		#region Unity生命周期

		private void Awake()
		{
			cardSlot = GetComponent<CardSlot>();
			rectTransform = GetComponent<RectTransform>();
			rootCanvas = GetComponentInParent<Canvas>();
			
			// 确保有CanvasGroup
			canvasGroup = GetComponent<CanvasGroup>();
			if (canvasGroup == null)
			{
				canvasGroup = gameObject.AddComponent<CanvasGroup>();
			}
		}

		#endregion

		#region 拖拽事件

		public void OnBeginDrag(PointerEventData eventData)
		{
			// 只有有卡牌时才能拖拽
			if (!draggingEnabled || !cardSlot.HasCard)
			{
				return;
			}

			isDragging = true;
			dropHandled = false;

			// 记录原始位置和父对象
			originalAnchoredPosition = rectTransform.anchoredPosition;
			originalParent = rectTransform.parent;
			originalSiblingIndex = rectTransform.GetSiblingIndex();


			// GHOST 模式：仅显示精灵，不移动原卡槽
			if (useDragGhost)
			{
				CreateOrShowGhost();
				HideOriginalVisualKeepLayout();
			}
			else
			{
				// 非Ghost：移动原卡槽进行拖拽
				if (dragRootOverride != null)
				{
					rectTransform.SetParent(dragRootOverride, true);
				}
			}

			// 拖拽时禁用射线检测（让底层可以接收Drop事件）
			canvasGroup.blocksRaycasts = false;
			canvasGroup.alpha = useDragGhost ? 1f : dragAlpha;

			// 移动到最顶层显示（非Ghost）
			if (!useDragGhost)
			{
				rectTransform.SetAsLastSibling();
			}

			// 提升Canvas排序，确保渲染在上层（非Ghost方案提升自身；Ghost方案提升影子）
			if (elevateSortingOnDrag && !useDragGhost)
			{
				tempOrSelfCanvas = GetComponent<Canvas>();
				tempCanvasAdded = false;
				if (tempOrSelfCanvas == null)
				{
					tempOrSelfCanvas = gameObject.AddComponent<Canvas>();
					tempCanvasAdded = true;
				}
				prevOverrideSorting = tempOrSelfCanvas.overrideSorting;
				prevSortingOrder = tempOrSelfCanvas.sortingOrder;
				tempOrSelfCanvas.overrideSorting = true;
				tempOrSelfCanvas.sortingOrder = dragSortingOrder;
			}

			Debug.Log($"[CardSlotDraggable] 开始拖拽卡牌: {cardSlot.GetCardData()?.nameEN}");
		}

		public void OnDrag(PointerEventData eventData)
		{
			if (!isDragging) return;

			if (useDragGhost)
			{
				MoveGhost(eventData);
			}
			else
			{
				// 在父容器的本地坐标系中移动
				RectTransform parentRect = rectTransform.parent as RectTransform;
				if (parentRect == null) return;

				Vector2 localPoint;
				if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
					parentRect,
					eventData.position,
					eventData.pressEventCamera,
					out localPoint))
				{
					rectTransform.anchoredPosition = localPoint;
				}
			}
		}

		public void OnEndDrag(PointerEventData eventData)
		{
			if (!isDragging) return;

			// 恢复射线检测和透明度
			canvasGroup.blocksRaycasts = true;
			canvasGroup.alpha = 1f;

			// 如果没有被任何区域处理，回到原位置（Ghost 则无需回位本体位置）
			if (!dropHandled && !useDragGhost)
			{
				ReturnToOriginalPosition();
			}

			// 还原排序/移除临时Canvas 或 销毁Ghost
			if (useDragGhost)
			{
				HideAndDisposeGhost();
				RestoreOriginalVisual();
			}
			else if (tempOrSelfCanvas != null)
			{
				if (tempCanvasAdded)
				{
					Destroy(tempOrSelfCanvas);
				}
				else
				{
					tempOrSelfCanvas.overrideSorting = prevOverrideSorting;
					tempOrSelfCanvas.sortingOrder = prevSortingOrder;
				}
				tempOrSelfCanvas = null;
			}

			isDragging = false;
			Debug.Log($"[CardSlotDraggable] 结束拖拽");
		}

		#endregion

		#region 公共方法

		/// <summary>
		/// 确认drop已被处理（由Drop区域调用）
		/// </summary>
		public void ConfirmDropHandled()
		{
			dropHandled = true;
		}

		/// <summary>
		/// 获取卡槽引用
		/// </summary>
		public CardSlot GetCardSlot()
		{
			return cardSlot;
		}

		/// <summary>
		/// 获取卡牌数据
		/// </summary>
		public BaseCardSO GetCardData()
		{
			return cardSlot?.GetCardData();
		}

		/// <summary>
		/// 启用/禁用拖拽
		/// </summary>
		public void SetDraggingEnabled(bool enabled)
		{
			draggingEnabled = enabled;
		}

		/// <summary>
		/// 获取开始拖拽前的原始父对象
		/// </summary>
		public Transform GetOriginalParentTransform()
		{
			return originalParent;
		}

		/// <summary>
		/// 获取开始拖拽前在原父对象下的序号
		/// </summary>
		public int GetOriginalSiblingIndex()
		{
			return originalSiblingIndex;
		}

		/// <summary>
		/// 获取开始拖拽前的锚定位置
		/// </summary>
		public Vector2 GetOriginalAnchoredPosition()
		{
			return originalAnchoredPosition;
		}

		#endregion

		#region 私有方法

		private void ReturnToOriginalPosition()
		{
			// 回到原始父对象和位置
			if (originalParent != null)
			{
				rectTransform.SetParent(originalParent);
				rectTransform.SetSiblingIndex(originalSiblingIndex);
			}
			
			rectTransform.anchoredPosition = originalAnchoredPosition;
		}

		private void CreateOrShowGhost()
		{
			var sprite = cardSlot?.GetCardData()?.uiSpritePreview;
			if (sprite == null)
			{
				// 兜底：不使用Ghost
				useDragGhost = false;
				return;
			}
			if (ghostRect == null)
			{
				GameObject go = new GameObject("DragGhost", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup), typeof(Image));
				ghostRect = go.GetComponent<RectTransform>();
				ghostCanvas = go.GetComponent<Canvas>();
				var cg = go.GetComponent<CanvasGroup>();
				ghostImage = go.GetComponent<Image>();
				ghostParent = dragRootOverride != null ? dragRootOverride : (rootCanvas != null ? rootCanvas.transform : transform.root);
				ghostRect.SetParent(ghostParent, false);
				ghostCanvas.overrideSorting = true;
				ghostCanvas.sortingOrder = dragSortingOrder;
				cg.blocksRaycasts = false;
				ghostImage.raycastTarget = false; // 不拦截投射，确保Drop区域能接收
			}
			ghostImage.sprite = sprite;
			ghostImage.color = new Color(1f, 1f, 1f, ghostAlpha);
			ghostImage.preserveAspect = true;
			ghostRect.localScale = Vector3.one * ghostScale;
			ghostRect.sizeDelta = ghostSize; // 固定480x480
			ghostRect.gameObject.SetActive(true);
		}

		private void MoveGhost(PointerEventData eventData)
		{
			if (ghostRect == null) return;
			RectTransform parentRect = ghostRect.parent as RectTransform;
			if (parentRect == null) return;
			Vector2 localPoint;
			if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
				parentRect,
				eventData.position,
				eventData.pressEventCamera,
				out localPoint))
			{
				ghostRect.anchoredPosition = localPoint;
			}
		}

		private void HideAndDisposeGhost(bool destroyObject = false)
		{
			if (ghostRect != null)
			{
				if (destroyObject)
				{
					Destroy(ghostRect.gameObject);
					ghostRect = null;
					ghostImage = null;
					ghostCanvas = null;
				}
				else
				{
					ghostRect.gameObject.SetActive(false);
				}
			}
		}

		private void HideOriginalVisualKeepLayout()
		{
			// 隐藏卡槽内的可视UI，但不禁用对象与布局（占位仍存在）
			if (originalVisualCanvasGroup == null)
			{
				originalVisualCanvasGroup = cardSlot.GetComponent<CanvasGroup>();
				originalVisualCanvasGroupAdded = false;
				if (originalVisualCanvasGroup == null)
				{
					originalVisualCanvasGroup = cardSlot.gameObject.AddComponent<CanvasGroup>();
					originalVisualCanvasGroupAdded = true;
				}
			}
			originalVisualAlpha = originalVisualCanvasGroup.alpha;
			originalVisualCanvasGroup.alpha = 0f; // 完全透明
			// 保持 blocksRaycasts=false 以允许下方Drop区域接收；不改active，不改Layout
		}

		private void RestoreOriginalVisual()
		{
			if (originalVisualCanvasGroup != null)
			{
				originalVisualCanvasGroup.alpha = originalVisualAlpha;
				if (originalVisualCanvasGroupAdded)
				{
					// 可选择保留该组件，避免重复添加销毁；这里保留
				}
			}
		}

		/// <summary>
		/// 被投递到有效区域后，由目标区域调用以立即清理影子/还原可视
		/// </summary>
		public void CleanupAfterSuccessfulDrop()
		{
			// 标记已处理，避免回退
			dropHandled = true;
			// 立即销毁影子，避免残留
			if (useDragGhost)
			{
				HideAndDisposeGhost(true);
			}
			// 还原本体可视（若本体稍后被销毁也无妨）
			if (canvasGroup != null)
			{
				canvasGroup.blocksRaycasts = true;
				canvasGroup.alpha = 1f;
			}
			RestoreOriginalVisual();
			isDragging = false;
		}

		#endregion
	}
}


