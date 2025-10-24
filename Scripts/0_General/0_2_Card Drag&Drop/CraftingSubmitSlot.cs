using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using TabernaNoctis.Cards;
using Sirenix.OdinInspector;

/// <summary>
/// 全新的“提交卡槽”（合成用空槽）：
/// - 支持两种录入方式：
///   1) 将材料卡牌拖放到本槽位
///   2) 先点击本槽位“选中/待填充”，再点击任意材料卡牌
/// - 仅记录材料ID与简单展示，不更改原卡牌位置/不销毁
/// - 记录后会广播 CRAFTING_SLOT_FILLED(slotIndex, materialId)，清空时广播 CRAFTING_SLOT_CLEARED(slotIndex)
/// </summary>
public class CraftingSubmitSlot : MonoBehaviour, IPointerClickHandler, IDropHandler
{
    [Header("槽位标识")]
    [LabelText("槽位索引")]
    [SerializeField, Tooltip("合成槽索引(0..2)")] private int slotIndex = 0;

    [Header("UI 引用(可选)")]
    [LabelText("材料图标")]
    [SerializeField, Tooltip("显示材料图标")] private Image materialIcon;
    [LabelText("材料名称文本")]
    [SerializeField, Tooltip("显示材料名称")] private TMP_Text materialNameText;
    [LabelText("高亮背景")]
    [SerializeField, Tooltip("选中/待填充时的高亮背景")] private Image highlightBackground;
    [LabelText("高亮颜色")]
    [SerializeField, Tooltip("高亮颜色")] private Color highlightColor = new Color(1f, 1f, 0.6f, 0.2f);
    [LabelText("普通颜色")]
    [SerializeField, Tooltip("普通颜色")] private Color normalColor = new Color(1f, 1f, 1f, 0.05f);

    [Header("交互设置")]
    [LabelText("允许点击填充")]
    [SerializeField, Tooltip("是否允许通过'选中槽位后点击卡牌'来填充")] private bool enableClickFill = true;
    [LabelText("右键清空")]
    [SerializeField, Tooltip("右键点击本槽位清空")] private bool rightClickToClear = true;

    [Header("易用性 - 投递命中区")]
    [LabelText("启用扩展命中区")]
    [SerializeField, Tooltip("通过 RaycastPadding 扩大可投递区域，解决影子较大而槽较小难以命中的问题")] private bool enableExpandedHitArea = true;
    [LabelText("命中区图形(可选)")]
    [SerializeField, Tooltip("用于承载 Raycast 的图形，留空则使用高亮背景")]
    private Graphic hitAreaGraphic;
    [LabelText("Raycast Padding(左,上,右,下)")]
    [SerializeField, Tooltip("扩大点击/投递命中区的像素边距")] private Vector4 hitAreaPadding = new Vector4(80f, 80f, 80f, 80f);

    [Header("当前状态(调试)")]
    [LabelText("当前材料ID")]
    [SerializeField, Tooltip("当前记录的材料ID(0=空)")] private int materialId = 0;
    private BaseCardSO materialCard;
    private bool armedForClick = false;

    [Header("可视辅助(Gizmos)")]
    [LabelText("在场景视图绘制范围")]
    [SerializeField] private bool drawGizmos = true;
    [LabelText("范围颜色")]
    [SerializeField] private Color gizmoColor = new Color(0.2f, 1f, 0.2f, 0.4f);
    [LabelText("命中区颜色")]
    [SerializeField] private Color gizmoHitColor = new Color(0.2f, 0.6f, 1f, 0.25f);

    private void OnEnable()
    {
        MessageManager.Register<BaseCardSO>(MessageDefine.CARD_CLICKED, OnCardClicked);
        MessageManager.Register<int>(MessageDefine.CRAFTING_SLOT_CLEARED, OnExternalClear);
        ApplyHitAreaPadding();
        RefreshUI();
    }

    private void OnDisable()
    {
        MessageManager.Remove<BaseCardSO>(MessageDefine.CARD_CLICKED, OnCardClicked);
        MessageManager.Remove<int>(MessageDefine.CRAFTING_SLOT_CLEARED, OnExternalClear);
    }

    // ============ 方式A：点击卡牌写入 ============
    private void OnCardClicked(BaseCardSO card)
    {
        if (!enableClickFill) return;
        if (!armedForClick) return;
        if (card == null) return;
        if (materialId != 0) return; // 已有材料则忽略
        SetMaterial(card);
        armedForClick = false;
    }

    // ============ 方式B：拖拽投递 ============
    public void OnDrop(PointerEventData eventData)
    {
        if (eventData == null || eventData.pointerDrag == null) return;
        var draggable = eventData.pointerDrag.GetComponent<TabernaNoctis.CardSystem.CardSlotDraggable>();
        if (draggable == null) return;
        var data = draggable.GetCardData();
        if (data == null) return;
        if (materialId != 0) return; // 已有材料则忽略
        SetMaterial(data);
        // 告知拖拽源：已处理
        draggable.CleanupAfterSuccessfulDrop();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // 左键：切换选中/待填充
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            if (enableClickFill)
            {
                armedForClick = !armedForClick;
                RefreshUI();
            }
        }
        // 右键：清空
        else if (eventData.button == PointerEventData.InputButton.Right && rightClickToClear)
        {
            ClearMaterial();
        }
    }

    // ============ 公开方法 ============
    public void SetMaterial(BaseCardSO card)
    {
        materialCard = card;
        materialId = card != null ? card.id : 0;
        MessageManager.Send<(int slotIndex, int materialId)>(MessageDefine.CRAFTING_SLOT_FILLED, (slotIndex, materialId));
        MessageManager.Send<(int slotIndex, BaseCardSO card)>(MessageDefine.CRAFTING_SLOT_CONTENT_UPDATED, (slotIndex, materialCard));
        Debug.Log($"[CraftingSubmitSlot] 槽{slotIndex} 已填入: {materialCard?.nameEN ?? "<空>"} (ID:{materialId})");
        RefreshUI();
    }

    public void ClearMaterial()
    {
        materialCard = null;
        materialId = 0;
        MessageManager.Send<int>(MessageDefine.CRAFTING_SLOT_CLEARED, slotIndex);
        MessageManager.Send<(int slotIndex, BaseCardSO card)>(MessageDefine.CRAFTING_SLOT_CONTENT_UPDATED, (slotIndex, null));
        Debug.Log($"[CraftingSubmitSlot] 槽{slotIndex} 已清空");
        RefreshUI();
    }

    public int GetMaterialId() => materialId;
    public BaseCardSO GetMaterialCard() => materialCard;

    private void RefreshUI()
    {
        if (highlightBackground != null)
        {
            highlightBackground.color = armedForClick ? highlightColor : normalColor;
        }
        if (materialIcon != null)
        {
            materialIcon.enabled = materialCard != null && materialCard.uiSpritePreview != null;
            if (materialIcon.enabled) materialIcon.sprite = materialCard.uiSpritePreview;
        }
        if (materialNameText != null)
        {
            // 需求：始终显示英文名
            materialNameText.text = materialCard != null ? materialCard.nameEN : string.Empty;
        }
    }

    private void OnExternalClear(int clearedIndex)
    {
        if (clearedIndex != slotIndex) return;
        materialCard = null;
        materialId = 0;
        armedForClick = false;
        RefreshUI();
    }

    private void ApplyHitAreaPadding()
    {
        if (!enableExpandedHitArea) return;
        var g = hitAreaGraphic != null ? hitAreaGraphic : (Graphic)highlightBackground;
        if (g == null) return;
        g.raycastTarget = true;
        g.raycastPadding = hitAreaPadding; // 扩大命中区，单位：像素（左、上、右、下）
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos) return;
        var rt = transform as RectTransform;
        if (rt == null) return;

        // 本体矩形
        Gizmos.matrix = transform.localToWorldMatrix;
        Vector3 size = new Vector3(rt.rect.width, rt.rect.height, 0f);
        Vector3 center = new Vector3(rt.rect.center.x, rt.rect.center.y, 0f);
        var prev = Gizmos.color;
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireCube(center, size);

        // 命中区（含 padding）
        float w = rt.rect.width + hitAreaPadding.x + hitAreaPadding.z; // left + right
        float h = rt.rect.height + hitAreaPadding.y + hitAreaPadding.w; // top + bottom
        Vector3 padSize = new Vector3(w, h, 0f);
        Gizmos.color = gizmoHitColor;
        Gizmos.DrawWireCube(center, padSize);
        Gizmos.color = prev;
        Gizmos.matrix = Matrix4x4.identity;
    }
}


