using TabernaNoctis.Cards;

namespace TabernaNoctis.CardSystem
{
	/// <summary>
	/// 卡槽放置区域接口
	/// </summary>
	public interface ICardSlotDropArea
	{
		/// <summary>
		/// 当卡槽被放置到此区域时调用
		/// </summary>
		/// <param name="cardSlot">被拖拽的卡槽</param>
		/// <param name="cardData">卡牌数据</param>
		void OnCardSlotDropped(CardSlot cardSlot, BaseCardSO cardData);
	}
}


