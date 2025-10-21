using UnityEngine;
using UnityEngine.EventSystems;
using TabernaNoctis.Cards;

namespace TabernaNoctis.CardSystem
{
	/// <summary>
	/// 卡槽提交区域 - 接收拖拽的卡牌并处理提交逻辑
	/// </summary>
	public class CardSlotSubmissionArea : MonoBehaviour, ICardSlotDropArea, IDropHandler
	{
		public void OnCardSlotDropped(CardSlot cardSlot, BaseCardSO cardData)
		{
			if (cardData == null)
			{
				Debug.LogWarning("[CardSlotSubmissionArea] 接收到空卡牌数据");
				return;
			}

			Debug.Log($"[CardSlotSubmissionArea] 提交卡牌: {cardData.nameEN} (ID:{cardData.id})");
			
			// 这里可以添加游戏逻辑
			// 例如：消耗材料、触发事件等
			
			// 示例：发送消息
			if (cardData is MaterialCardSO material)
			{
				Debug.Log($"  - 材料: {material.nameEN}, 价格: ${material.price}");
				// MessageManager.Send<MaterialCardSO>(MessageDefine.MATERIAL_SUBMITTED, material);
			}

			// 清空卡槽并销毁（或回收）
			cardSlot.ClearCard();
			Destroy(cardSlot.gameObject);
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
				// 立即清理拖拽影子并标记处理完成
				draggable.CleanupAfterSuccessfulDrop();
			}
		}
	}
}


