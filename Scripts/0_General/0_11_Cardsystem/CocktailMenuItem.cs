using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TabernaNoctis.Cards;

public class CocktailMenuItem : MonoBehaviour, IPointerClickHandler
{
    public enum MarkState { None, Accepted, Rejected }

    [SerializeField] private Image checkImage;
    [SerializeField] private Image crossImage;

    private MarkState currentState = MarkState.None;
    private CocktailCardSO data;

    public void Setup(CocktailCardSO cocktail)
    {
        data = cocktail;
        if (checkImage == null) checkImage = FindByName<Image>(transform, "Check");
        if (crossImage == null) crossImage = FindByName<Image>(transform, "Cross");
        ApplyVisual();
    }

    public CocktailCardSO GetData() => data;
    public bool IsAccepted() => currentState == MarkState.Accepted;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData == null || eventData.button != PointerEventData.InputButton.Left) return;
        if (currentState == MarkState.None)
            currentState = MarkState.Accepted;
        else if (currentState == MarkState.Accepted)
            currentState = MarkState.Rejected;
        else
            currentState = MarkState.Accepted; // 二态循环：√ 与 ×
        ApplyVisual();
    }

    private void ApplyVisual()
    {
        if (checkImage != null) checkImage.gameObject.SetActive(currentState == MarkState.Accepted);
        if (crossImage != null) crossImage.gameObject.SetActive(currentState == MarkState.Rejected);
    }

    private T FindByName<T>(Transform parent, string childName) where T : Component
    {
        var comps = parent.GetComponentsInChildren<T>(true);
        for (int i = 0; i < comps.Length; i++)
        {
            if (comps[i] != null && comps[i].name == childName) return comps[i];
        }
        return null;
    }
}


