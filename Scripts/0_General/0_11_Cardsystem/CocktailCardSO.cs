using UnityEngine;

namespace TabernaNoctis.Cards
{
    [CreateAssetMenu(fileName = "CocktailCard_", menuName = "TabernaNoctis/Cards/Cocktail Card", order = 11)]
    public class CocktailCardSO : BaseCardSO
    {
        [Header("鸡尾酒经济")]
        public int cost;                 // 成本
        public int price;                // 售价
        public int profit;               // 利润

        [Header("评价")]
        public int reputationChange;     // 评价变化
    }
}


