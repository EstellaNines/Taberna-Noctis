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
			[Header("作为合成槽使用（可选）")]
			[SerializeField] private bool useAsCraftingSlot = false;
			[SerializeField] private int craftingSlotIndex = 0; // 0..2
			
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

				// 如果作为“合成槽”使用：只记录材料，不消费和销毁卡槽
				if (useAsCraftingSlot)
				{
					MessageManager.Send<(int slotIndex, int materialId)>(MessageDefine.CRAFTING_SLOT_FILLED, (craftingSlotIndex, cardData.id));
					// 不清卡、不销毁，留给玩家继续操作
					return;
				}

				// 否则：正常提交（购买/结算逻辑）
				if (SaveManager.Instance != null)
				{
					string itemKey = cardData.id.ToString();
					int price = 0;
					if (cardData is MaterialCardSO mat) price = Mathf.RoundToInt(mat.price);
					SaveManager.Instance.ApplyPurchase(itemKey, price);
					MessageManager.Send<(string itemKey, int price)>(MessageDefine.MATERIAL_PURCHASED, (itemKey, price));
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


