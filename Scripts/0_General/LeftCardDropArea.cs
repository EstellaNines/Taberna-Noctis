using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LeftCardDropArea : MonoBehaviour, ICardDragArea
{
    public void OnCardDropped(Card card)
    {
        card.transform.position = transform.position;
        Debug.Log("卡放这");
    }

}
