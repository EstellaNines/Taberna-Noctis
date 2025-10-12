using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CardStorageArea : MonoBehaviour, ICardDragArea, IDropHandler
{
    public void OnCardDropped(Card card)
    {
        RectTransform cardRect = card.GetComponent<RectTransform>();
        RectTransform selfRect = GetComponent<RectTransform>();
        if (cardRect != null && selfRect != null)
        {
            cardRect.SetParent(selfRect, true);
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.anchoredPosition = Vector2.zero;
            cardRect.localRotation = Quaternion.identity;
            cardRect.localScale = Vector3.one;
        }
        else
        {
            card.transform.position = transform.position;
        }
        Debug.Log("Card stored in CardStorageArea");
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


