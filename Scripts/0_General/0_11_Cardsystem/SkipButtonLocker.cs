using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

/// <summary>
/// 在发牌未完成前隐藏“跳过/开始/继续”等按钮，待发牌完成再显示。
/// 监听 MessageDefine.QUEUE_DISPENSE_STARTED / QUEUE_DISPENSE_FINISHED。
/// 将本脚本挂在按钮物体或其父物体上，配置 targetRoot（默认就是本物体）。
/// </summary>
public class SkipButtonLocker : MonoBehaviour
{
#if ODIN_INSPECTOR
    [BoxGroup("引用"), LabelText("按钮根物体(默认本物体)")]
#endif
    [SerializeField] private GameObject targetRoot;

#if ODIN_INSPECTOR
    [BoxGroup("行为"), LabelText("启用时即隐藏")]
#endif
    [SerializeField] private bool hideOnEnable = true;

    private void Awake()
    {
        if (targetRoot == null) targetRoot = gameObject;
    }

    private void OnEnable()
    {
        MessageManager.Register<string>(MessageDefine.QUEUE_DISPENSE_STARTED, OnQueueStarted);
        MessageManager.Register<string>(MessageDefine.QUEUE_DISPENSE_FINISHED, OnQueueFinished);
        // 初始状态与当前“是否正在发牌”同步；若未在发牌则立即显示
        bool locked = CardDispenseRuntime.IsLocked;
        SafeSetActive(!locked);
    }

    private void OnDisable()
    {
        MessageManager.Remove<string>(MessageDefine.QUEUE_DISPENSE_STARTED, OnQueueStarted);
        MessageManager.Remove<string>(MessageDefine.QUEUE_DISPENSE_FINISHED, OnQueueFinished);
    }

    private void OnQueueStarted(string _)
    {
        SafeSetActive(false);
    }

    private void OnQueueFinished(string _)
    {
        SafeSetActive(true);
    }

    private void SafeSetActive(bool on)
    {
        if (targetRoot != null && targetRoot.activeSelf != on)
            targetRoot.SetActive(on);
    }
}


