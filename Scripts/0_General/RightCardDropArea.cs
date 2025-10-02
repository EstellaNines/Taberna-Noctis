using UnityEngine;

public class RightCardDropArea : MonoBehaviour, ICardDragArea
{
    [SerializeField] private GameObject objectToSpawn;
    public void OnCardDropped(Card card)
    {
        Destroy(card.gameObject);
        Instantiate(objectToSpawn, transform.position, transform.rotation);
    }
}
