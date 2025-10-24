using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;

/// <summary>
/// DayScreen 跳过按钮控制器：
/// - 启动时隐藏按钮；
/// - 监听 MATERIAL_PURCHASED，累计今日购买数量；
/// - 达到阈值后显示按钮；
/// - 点击后保存并切换到下午（依赖时间系统，否则直接切场景）。
/// </summary>
public class DaySkipToAfternoonButton : MonoBehaviour
{
    [Header("引用")]
    [LabelText("跳过按钮")]
    [SerializeField] private Button skipButton;

    [Header("条件")]
    [LabelText("显示所需购买数")]
    [SerializeField, Min(1)] private int purchasesRequired = 3;

    [Header("启动行为")]
    [LabelText("启动时隐藏按钮")]
    [SerializeField] private bool hideOnStart = true;

    private int currentPurchases;
    private CanvasGroup skipCanvasGroup;

    private void Awake()
    {
        // 自动尝试获取引用，避免未手动拖拽
        if (skipButton == null) skipButton = GetComponent<Button>();
        if (skipButton == null) skipButton = GetComponentInChildren<Button>(true);

        // 确保有 CanvasGroup 来实现“隐藏而不禁用脚本”
        if (skipButton != null)
        {
            skipCanvasGroup = skipButton.GetComponent<CanvasGroup>();
            if (skipCanvasGroup == null) skipCanvasGroup = skipButton.gameObject.AddComponent<CanvasGroup>();
            // 绑定点击事件（若未在 Inspector 配置）
            skipButton.onClick.RemoveListener(OnSkipClicked);
            skipButton.onClick.AddListener(OnSkipClicked);
        }

        if (hideOnStart)
        {
            HideVisual();
        }
    }

    private void OnEnable()
    {
        MessageManager.Register<(string itemKey, int price)>(MessageDefine.MATERIAL_PURCHASED, OnMaterialPurchased);
        MessageManager.Register<string>(MessageDefine.SAVE_COMPLETED, OnSaveCompleted);
        MessageManager.Register<string>(MessageDefine.SAVE_LOADED, OnSaveLoaded);
        RefreshFromSaveAndMaybeShow();
    }

    private void OnDisable()
    {
        MessageManager.Remove<(string itemKey, int price)>(MessageDefine.MATERIAL_PURCHASED, OnMaterialPurchased);
        MessageManager.Remove<string>(MessageDefine.SAVE_COMPLETED, OnSaveCompleted);
        MessageManager.Remove<string>(MessageDefine.SAVE_LOADED, OnSaveLoaded);
    }

    private void RefreshFromSaveAndMaybeShow()
    {
        var save = GetSaveDataSafe();
        currentPurchases = save != null && save.todayPurchasedItems != null ? save.todayPurchasedItems.Count : 0;
        TryShowButton();
    }

    private void OnMaterialPurchased((string itemKey, int price) payload)
    {
        // 为避免事件丢失或重复计数，这里以存档真实数据为准
        var save = GetSaveDataSafe();
        currentPurchases = save != null && save.todayPurchasedItems != null ? save.todayPurchasedItems.Count : currentPurchases + 1;
        TryShowButton();
    }

    private void OnSaveCompleted(string slotId)
    {
        // 读存档快照，避免遗漏事件导致计数不同步
        RefreshFromSaveAndMaybeShow();
    }

    private void OnSaveLoaded(string slotId)
    {
        RefreshFromSaveAndMaybeShow();
    }

    private void TryShowButton()
    {
        if (skipButton == null) return;
        if (currentPurchases >= purchasesRequired) ShowVisual();
        else HideVisual();
    }

    public void OnSkipClicked()
    {
        // 先做晨间收尾保存
        if (SaveManager.Instance != null)
        {
            try { SaveManager.Instance.SaveCheckpoint(); } catch { }
        }

        // 设置时间系统为下午起点（不依赖内部私有推进）
        if (TimeSystemManager.Instance != null)
        {
            try
            {
                TimeSystemManager.Instance.JumpToPhaseTime(TimePhase.Afternoon, 14, 0);
                // 通知其他系统阶段已变化
                MessageManager.Send(MessageDefine.PHASE_CHANGED, TimePhase.Afternoon);
            }
            catch { }
        }

        // 通过全局场景管理器切换（经 LoadingScreen）
        try
        {
            GlobalSceneManager.GoToName("4_AfternoonScreen");
        }
        catch
        {
            Debug.LogWarning("[DaySkipToAfternoonButton] 切换到下午失败，请检查场景名或时间系统");
        }
    }

    private SaveData GetSaveDataSafe()
    {
        if (SaveManager.Instance == null) return null;
        try { return SaveManager.Instance.GenerateSaveData(); } catch { return null; }
    }

    private void HideVisual()
    {
        if (skipButton == null) return;
        if (skipCanvasGroup == null) skipCanvasGroup = skipButton.GetComponent<CanvasGroup>();
        if (skipCanvasGroup == null) skipCanvasGroup = skipButton.gameObject.AddComponent<CanvasGroup>();
        // 仅隐藏与禁用交互，不关闭GameObject，确保本脚本继续监听事件
        skipCanvasGroup.alpha = 0f;
        skipCanvasGroup.blocksRaycasts = false;
        skipCanvasGroup.interactable = false;
        if (!skipButton.gameObject.activeSelf) skipButton.gameObject.SetActive(true);
    }

    private void ShowVisual()
    {
        if (skipButton == null) return;
        if (skipCanvasGroup == null) skipCanvasGroup = skipButton.GetComponent<CanvasGroup>();
        if (skipCanvasGroup == null) skipCanvasGroup = skipButton.gameObject.AddComponent<CanvasGroup>();
        skipCanvasGroup.alpha = 1f;
        skipCanvasGroup.blocksRaycasts = true;
        skipCanvasGroup.interactable = true;
        if (!skipButton.gameObject.activeSelf) skipButton.gameObject.SetActive(true);
    }
}


