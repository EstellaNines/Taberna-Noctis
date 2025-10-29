using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 发牌期间禁止点击/拖拽：
/// - 监听 QUEUE_DISPENSE_STARTED/FINISHED
/// - 通过 CanvasGroup 或 GraphicRaycaster 统一屏蔽交互
/// 用法：将本脚本挂在卡区父物体，配置 blockerCanvasGroup 或 graphicRaycaster。
/// </summary>
public class CardInputLocker : MonoBehaviour
{
    [Header("交互阻断器(二选一)")]
    [SerializeField] private CanvasGroup blockerCanvasGroup;     // 推荐：挂在全屏透明Panel上
    [SerializeField] private GraphicRaycaster graphicRaycaster;  // 备选：直接关掉卡区的射线

    [Header("阻断时外观")]
    [SerializeField] private float blockerAlphaWhileLocked = 0f; // 0=全透明

    private void Awake()
    {
        if (blockerCanvasGroup == null)
        {
            // 尝试在子节点寻找一个CanvasGroup作为阻断层
            blockerCanvasGroup = GetComponentInChildren<CanvasGroup>(true);
        }
        if (graphicRaycaster == null)
        {
            graphicRaycaster = GetComponentInParent<GraphicRaycaster>();
        }
    }

    private void OnEnable()
    {
        MessageManager.Register<string>(MessageDefine.QUEUE_DISPENSE_STARTED, OnQueueStarted);
        MessageManager.Register<string>(MessageDefine.QUEUE_DISPENSE_FINISHED, OnQueueFinished);
    }

    private void OnDisable()
    {
        MessageManager.Remove<string>(MessageDefine.QUEUE_DISPENSE_STARTED, OnQueueStarted);
        MessageManager.Remove<string>(MessageDefine.QUEUE_DISPENSE_FINISHED, OnQueueFinished);
    }

    private void OnQueueStarted(string _)
    {
        SetLocked(true);
    }

    private void OnQueueFinished(string _)
    {
        SetLocked(false);
    }

    private void SetLocked(bool locked)
    {
        if (blockerCanvasGroup != null)
        {
            blockerCanvasGroup.gameObject.SetActive(locked);
            blockerCanvasGroup.blocksRaycasts = locked;
            blockerCanvasGroup.interactable = false;
            blockerCanvasGroup.alpha = locked ? blockerAlphaWhileLocked : 0f;
        }
        if (graphicRaycaster != null)
        {
            graphicRaycaster.enabled = !locked;
        }
    }
}


