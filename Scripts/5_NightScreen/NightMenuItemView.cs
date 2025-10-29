using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TabernaNoctis.Cards;

/// <summary>
/// 夜晚菜单的单项展示：鸡尾酒图片 + 名称 + 参数（可选）
/// 在预制上把各字段引用拖进来即可复用下午场景的视觉样式。
/// </summary>
public class NightMenuItemView : MonoBehaviour
{
    [Header("UI 引用")]
    [SerializeField] private Image cocktailImage;     // 绑定到 NightCocktail/Cocktail Image
    [SerializeField] private TMP_Text cocktailNameText; // 绑定到 NightCocktail/Name

    [Header("状态数值（分别对应 Valve 子节点）")]
    [SerializeField] private TMP_Text busyValueText;         // NightCocktail/Valve/Busy_valve
    [SerializeField] private TMP_Text irritableValueText;    // NightCocktail/Valve/Irritable_valve
    [SerializeField] private TMP_Text melancholyValueText;   // NightCocktail/Valve/Melancholy_valve
    [SerializeField] private TMP_Text pickyValueText;        // NightCocktail/Valve/Picky_valve
    [SerializeField] private TMP_Text friendlyValueText;     // NightCocktail/Valve/Friendly_valve

    [Header("可选：经济/评价文本（若预制包含）")]
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private TMP_Text profitText;
    [SerializeField] private TMP_Text reputationText;

    public void Apply(CocktailCardSO so)
    {
        if (so == null) return;
        if (cocktailNameText != null) cocktailNameText.text = so.nameEN;
        if (cocktailImage != null) cocktailImage.sprite = LoadSprite(so);

        // 预制的State顺序（根据你的NightCocktail）：
        // Busy / Irritable / Melancholy / Picky / Friendly
        var eff = so.effects;
        AssignValue(busyValueText, eff.busy);
        AssignValue(irritableValueText, eff.impatient);
        AssignValue(melancholyValueText, eff.bored);
        AssignValue(pickyValueText, eff.picky);
        AssignValue(friendlyValueText, eff.friendly);

        if (priceText != null) priceText.text = so.price.ToString();
        if (costText != null) costText.text = so.cost.ToString();
        if (profitText != null) profitText.text = so.profit.ToString();
        if (reputationText != null) reputationText.text = PrefixSigned(so.reputationChange);
    }

    private static string PrefixSigned(int v) => v > 0 ? "+" + v : v.ToString();

    private static Sprite LoadSprite(BaseCardSO so)
    {
        if (so == null) return null;
        if (so.uiSpritePreview != null) return so.uiSpritePreview;
        if (!string.IsNullOrEmpty(so.uiPath)) return Resources.Load<Sprite>(so.uiPath);
        return null;
    }

    private static void AssignValue(TMP_Text target, int value)
    {
        if (target == null) return;
        target.text = PrefixSigned(value);
    }
}


