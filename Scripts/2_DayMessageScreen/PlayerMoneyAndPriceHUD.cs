using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TabernaNoctis.Cards;
using Sirenix.OdinInspector;

/// <summary>
/// 显示玩家当前金钱与当前拖拽物品价格：
/// - 作为一个 RawImage 父物体上挂载的脚本，两个子 TMP：
///   moneyText 显示 "$XXX"（来自存档/SaveManager）
///   priceText 显示 "-$XX"（红色），在拖拽材料卡时显示，否则隐藏
/// - 监听 SAVE_LOADED 刷新金钱；监听 CARD_DRAG_STARTED/CARD_DRAG_ENDED 刷新价格
/// </summary>
public class PlayerMoneyAndPriceHUD : MonoBehaviour
{
    [Header("UI 引用（在 RawImage 下）")]
    [LabelText("金钱文本")]
    [SerializeField] private TMP_Text moneyText;
    [LabelText("价格文本")]
    [SerializeField] private TMP_Text priceText;

    [Header("样式")]
    [LabelText("价格颜色")]
    [SerializeField] private Color priceColor = new Color(1f, 0.2f, 0.2f, 1f);
    [LabelText("无价格时隐藏")]
    [SerializeField] private bool hidePriceWhenNone = true;

    private void Reset()
    {
        // 自动在子物体中查找
        var tmps = GetComponentsInChildren<TMP_Text>(true);
        if (tmps != null)
        {
            if (tmps.Length > 0) moneyText = tmps[0];
            if (tmps.Length > 1) priceText = tmps[1];
        }
    }

    private void Awake()
    {
        if (hidePriceWhenNone && priceText != null) priceText.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        MessageManager.Register<string>(MessageDefine.SAVE_LOADED, OnSaveLoaded);
        MessageManager.Register<BaseCardSO>(MessageDefine.CARD_DRAG_STARTED, OnCardDragStarted);
        MessageManager.Register<bool>(MessageDefine.CARD_DRAG_ENDED, OnCardDragEnded);
        MessageManager.Register<(string itemKey, int price)>(MessageDefine.MATERIAL_PURCHASED, OnMaterialPurchased);
        RefreshMoneyFromSave();
        // 初始进入时隐藏价格显示，直到开始拖拽
        HidePriceImmediate();
    }

    private void OnDisable()
    {
        MessageManager.Remove<string>(MessageDefine.SAVE_LOADED, OnSaveLoaded);
        MessageManager.Remove<BaseCardSO>(MessageDefine.CARD_DRAG_STARTED, OnCardDragStarted);
        MessageManager.Remove<bool>(MessageDefine.CARD_DRAG_ENDED, OnCardDragEnded);
        MessageManager.Remove<(string itemKey, int price)>(MessageDefine.MATERIAL_PURCHASED, OnMaterialPurchased);
    }

    private void OnSaveLoaded(string _)
    {
        RefreshMoneyFromSave();
    }

    private void RefreshMoneyFromSave()
    {
        if (moneyText == null) return;
        var data = GetSaveDataSafe();
        int money = data != null ? Mathf.RoundToInt(data.currentMoney) : 0;
        moneyText.text = "$" + money.ToString();
    }

    private SaveData GetSaveDataSafe()
    {
        // 通过 SaveManager 的内存快照生成实时数据
        if (SaveManager.Instance == null) return null;
        try
        {
            return SaveManager.Instance.GenerateSaveData();
        }
        catch { return null; }
    }

    private void OnCardDragStarted(BaseCardSO card)
    {
        if (priceText == null)
            return;

        float price = 0f;
        if (card is MaterialCardSO m)
        {
            price = m.price;
        }
        // 其他卡种可扩展

        priceText.color = priceColor;
        priceText.text = "-$" + Mathf.RoundToInt(price);
        if (hidePriceWhenNone) priceText.gameObject.SetActive(true);
    }

    private void OnCardDragEnded(bool _)
    {
        HidePriceImmediate();
    }

    private void OnMaterialPurchased((string itemKey, int price) payload)
    {
        // 采购完成后刷新显示的现金
        RefreshMoneyFromSave();
    }

    private void HidePriceImmediate()
    {
        if (priceText == null) return;
        priceText.text = string.Empty;
        if (hidePriceWhenNone) priceText.gameObject.SetActive(false);
    }
}


