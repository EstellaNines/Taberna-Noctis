using UnityEngine;

namespace TabernaNoctis.Cards
{
    [CreateAssetMenu(fileName = "MaterialCard_", menuName = "TabernaNoctis/Cards/Material Card", order = 10)]
    public class MaterialCardSO : BaseCardSO
    {
        [Header("材料经济")]
        public int price;    // 单价
    }
}


