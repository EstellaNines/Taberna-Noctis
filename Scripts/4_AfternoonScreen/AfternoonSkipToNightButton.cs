using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;

/// <summary>
/// Afternoon 跳过按钮控制器：
/// - 可选：启动时隐藏按钮；
/// - 点击后保存、跳转时间到 Night，并切换到夜晚场景（经全局场景管理器）。
/// </summary>
public class AfternoonSkipToNightButton : MonoBehaviour
{
    [Header("引用")]
    [LabelText("跳过按钮")]
    [SerializeField] private Button skipButton;

    [Header("启动行为")]
    [LabelText("启动时隐藏按钮")]
    [SerializeField] private bool hideOnStart = false;

    [Header("跳转时间设置")]
    [LabelText("夜晚起始小时(24h)")]
    [SerializeField, Range(0, 23)] private int nightStartHour = 23;
    [LabelText("夜晚起始分钟")]
    [SerializeField, Range(0, 59)] private int nightStartMinute = 0;

    private CanvasGroup skipCanvasGroup;

    private void Awake()
    {
        // 自动获取引用
        if (skipButton == null) skipButton = GetComponent<Button>();
        if (skipButton == null) skipButton = GetComponentInChildren<Button>(true);

        // 确保有 CanvasGroup 以便显隐
        if (skipButton != null)
        {
            skipCanvasGroup = skipButton.GetComponent<CanvasGroup>();
            if (skipCanvasGroup == null) skipCanvasGroup = skipButton.gameObject.AddComponent<CanvasGroup>();
            skipButton.onClick.RemoveListener(OnSkipClicked);
            skipButton.onClick.AddListener(OnSkipClicked);
        }

        if (hideOnStart) HideVisual();
        else ShowVisual();
    }

    public void OnSkipClicked()
    {
        // 先做收尾保存
        if (SaveManager.Instance != null)
        {
            try { SaveManager.Instance.SaveCheckpoint(); } catch { }
        }

        // 设置时间到 Night 指定起点，并广播阶段变更
        if (TimeSystemManager.Instance != null)
        {
            try
            {
                TimeSystemManager.Instance.JumpToPhaseTime(TimePhase.Night, nightStartHour, nightStartMinute);
                MessageManager.Send(MessageDefine.PHASE_CHANGED, TimePhase.Night);
            }
            catch { }
        }

        // 切换到夜晚场景（通过加载界面）
        try
        {
            GlobalSceneManager.GoToName("5_NightScreen");
        }
        catch
        {
            Debug.LogWarning("[AfternoonSkipToNightButton] 切换到夜晚失败，请检查场景名或时间系统");
        }
    }

    private void HideVisual()
    {
        if (skipButton == null) return;
        if (skipCanvasGroup == null) skipCanvasGroup = skipButton.GetComponent<CanvasGroup>();
        if (skipCanvasGroup == null) skipCanvasGroup = skipButton.gameObject.AddComponent<CanvasGroup>();
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


