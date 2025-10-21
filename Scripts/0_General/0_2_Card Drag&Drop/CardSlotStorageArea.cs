using UnityEngine;
using UnityEngine.EventSystems;
using TabernaNoctis.Cards;

namespace TabernaNoctis.CardSystem
{
	/// <summary>
	/// 卡槽存储区域 - 接收拖拽的卡牌并放回
	/// </summary>
	public class CardSlotStorageArea : MonoBehaviour, ICardSlotDropArea, IDropHandler
	{
		public void OnCardSlotDropped(CardSlot cardSlot, BaseCardSO cardData)
		{
			RectTransform cardRect = cardSlot.GetComponent<RectTransform>();
			RectTransform selfRect = GetComponent<RectTransform>();
			
			if (cardRect != null && selfRect != null)
			{
				// 如果拖拽来源父级就是本存放区，则按原序号/位置放回
				var draggable = cardSlot.GetComponent<CardSlotDraggable>();
				if (draggable != null && draggable.GetOriginalParentTransform() == selfRect.transform)
				{
					cardRect.SetParent(selfRect, true);
					cardRect.SetSiblingIndex(Mathf.Clamp(draggable.GetOriginalSiblingIndex(), 0, selfRect.childCount));
					cardRect.anchoredPosition = draggable.GetOriginalAnchoredPosition();
				}
				else
				{
					// 否则作为默认居中放回（第一次进入存放区或跨区域移动）
					cardRect.SetParent(selfRect, true);
					cardRect.anchorMin = new Vector2(0.5f, 0.5f);
					cardRect.anchorMax = new Vector2(0.5f, 0.5f);
					cardRect.pivot = new Vector2(0.5f, 0.5f);
					cardRect.anchoredPosition = Vector2.zero;
				}
				cardRect.localRotation = Quaternion.identity;
				cardRect.localScale = Vector3.one;
			}
			
			Debug.Log($"[CardSlotStorageArea] 卡牌放回存储区: {cardData?.nameEN}");
		}

		public void OnDrop(PointerEventData eventData)
		{
			if (eventData == null || eventData.pointerDrag == null) return;

			CardSlotDraggable draggable = eventData.pointerDrag.GetComponent<CardSlotDraggable>();
			if (draggable == null) return;

			CardSlot cardSlot = draggable.GetCardSlot();
			BaseCardSO cardData = draggable.GetCardData();
			
			if (cardSlot != null && cardData != null)
			{
				OnCardSlotDropped(cardSlot, cardData);
				// 放回后清理影子/标记完成
				draggable.CleanupAfterSuccessfulDrop();
			}
		}
	}
}


