using UnityEngine;
using Sirenix.OdinInspector;
using TabernaNoctis.Cards;

/// <summary>
/// 下午场景：点击材料/鸡尾酒卡牌，将材料顺序填入三个提交槽（0→1→2）。
/// - 监听 CARD_CLICKED(BaseCardSO)
/// - 仅当点击的是 MaterialCardSO 时写入
/// - 优先选择第一个空槽位
/// </summary>
public class AfternoonCraftingClickFiller : MonoBehaviour
{
    [Header("提交槽引用（按顺序）")]
    [LabelText("槽0")] [SerializeField] private CraftingSubmitSlot slot0;
    [LabelText("槽1")] [SerializeField] private CraftingSubmitSlot slot1;
    [LabelText("槽2")] [SerializeField] private CraftingSubmitSlot slot2;

    private void OnEnable()
    {
        MessageManager.Register<BaseCardSO>(MessageDefine.CARD_CLICKED, OnCardClicked);
        AutoFindSlotsIfMissing();
    }

    private void OnDisable()
    {
        MessageManager.Remove<BaseCardSO>(MessageDefine.CARD_CLICKED, OnCardClicked);
    }

    private void AutoFindSlotsIfMissing()
    {
        if (slot0 != null && slot1 != null && slot2 != null) return;
        var slots = GetComponentsInChildren<CraftingSubmitSlot>(true);
        if (slots != null && slots.Length >= 3)
        {
            if (slot0 == null) slot0 = slots[0];
            if (slot1 == null) slot1 = slots[1];
            if (slot2 == null) slot2 = slots[2];
        }
    }

    private void OnCardClicked(BaseCardSO card)
    {
        if (card == null) return;
        if (!(card is MaterialCardSO)) return; // 仅材料卡可写入
        AutoFindSlotsIfMissing();

        // 依次填充到第一个空槽
        if (slot0 != null && slot0.GetMaterialId() == 0) { slot0.SetMaterial(card); return; }
        if (slot1 != null && slot1.GetMaterialId() == 0) { slot1.SetMaterial(card); return; }
        if (slot2 != null && slot2.GetMaterialId() == 0) { slot2.SetMaterial(card); return; }
        // 三槽已满：忽略或可在此提示
    }
}


