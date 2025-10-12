using UnityEngine;
using UnityEngine.EventSystems;

public class CardSubmissionArea : MonoBehaviour, ICardDragArea, IDropHandler
{
    public void OnCardDropped(Card card)
    {
        string msg = card != null ? card.GetSubmitLogMessage() : "<null card>";
        Debug.Log($"提交区收到卡牌消息: {msg}");
        MessageManager.Send<string>(MessageDefine.TEST_MESSAGE, msg);
        Destroy(card.gameObject);
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData == null || eventData.pointerDrag == null) return;
        Card card = eventData.pointerDrag.GetComponent<Card>();
        if (card == null) return;
        OnCardDropped(card);
        card.ConfirmUIDropHandled();
    }
}


