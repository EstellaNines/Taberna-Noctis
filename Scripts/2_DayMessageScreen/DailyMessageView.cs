using UnityEngine;
using UnityEngine.UI;

public class DailyMessageView : MonoBehaviour
{
    [Header("Target UI")]
    [SerializeField] private Image targetImage;

    public void SetSprite(Sprite sprite, bool setNativeSize = false)
    {
        if (targetImage == null) return;
        targetImage.sprite = sprite;
        if (setNativeSize && sprite != null) targetImage.SetNativeSize();
        if (!targetImage.gameObject.activeSelf) targetImage.gameObject.SetActive(true);
    }

    public Image GetImage() => targetImage;
}


