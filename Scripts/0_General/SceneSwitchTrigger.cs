using UnityEngine;
using Sirenix.OdinInspector;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

// 通用触发组件：可挂在按钮或任意对象上，选择触发方式来切换场景
public class SceneSwitchTrigger : MonoBehaviour
{
    public enum TriggerMode
    {
        None,
        OnClick,         // 供 UI Button 调用（绑定 OnClick -> Trigger()）
        OnEnable,        // 对象启用时
        OnKeyDown,       // 指定按键
        ByMessage,       // 通过消息系统（可扩展）
    }

    public enum TargetType
    {
        Next,
        Prev,
        SceneName,
        SceneAsset,
        SequenceIndex,
        Selector
    }

    public enum NextTargetMode
    {
        SequenceNext,
        SceneName,
        SceneAsset,
        SequenceIndex
    }

    public enum PrevTargetMode
    {
        SequencePrev,
        SceneName,
        SceneAsset,
        SequenceIndex
    }

    [BoxGroup("触发设置")]
    [LabelText("触发方式")] public TriggerMode trigger = TriggerMode.OnClick;

    [BoxGroup("目标设置")]
    [EnumToggleButtons, LabelText("目标类型")] public TargetType targetType = TargetType.Next;

    [BoxGroup("目标设置"), LabelText("目标场景名"), ShowIf("@this.targetType == TargetType.SceneName")]
    public string targetSceneName;

    [BoxGroup("目标设置"), LabelText("目标场景引用"), ShowIf("@this.targetType == TargetType.SceneAsset"), AssetsOnly]
    public Object targetSceneAsset; // SceneAsset（Editor）

    [HideInInspector]
    public string cachedAssetSceneName; // 运行时使用的缓存名

    [BoxGroup("目标设置"), LabelText("索引"), ShowIf("@this.targetType == TargetType.SequenceIndex")]
    public int targetIndex;

    [BoxGroup("目标设置"), LabelText("策略对象"), ShowIf("@this.targetType == TargetType.Selector"), AssetsOnly]
    public SceneSelector selector;

    [BoxGroup("更多"), LabelText("加载模式")] public LoadSceneMode mode = LoadSceneMode.Single;
    [BoxGroup("更多"), LabelText("按键(可选)")] public KeyCode key = KeyCode.None;

    // ========== Next/Prev 独立配置（可选覆盖） ==========
    [BoxGroup("Next 目标")]
    [EnumToggleButtons, LabelText("Next 规则")] public NextTargetMode nextMode = NextTargetMode.SequenceNext;

    [BoxGroup("Next 目标"), LabelText("Next 场景名"), ShowIf("@this.nextMode == NextTargetMode.SceneName")]
    public string nextSceneName;

    [BoxGroup("Next 目标"), LabelText("Next 场景引用"), ShowIf("@this.nextMode == NextTargetMode.SceneAsset"), AssetsOnly]
    public Object nextSceneAsset; // SceneAsset (Editor)

    [HideInInspector]
    public string nextCachedAssetSceneName;

    [BoxGroup("Next 目标"), LabelText("Next 索引"), ShowIf("@this.nextMode == NextTargetMode.SequenceIndex")]
    public int nextIndex;

    [BoxGroup("Prev 目标")]
    [EnumToggleButtons, LabelText("Prev 规则")] public PrevTargetMode prevMode = PrevTargetMode.SequencePrev;

    [BoxGroup("Prev 目标"), LabelText("Prev 场景名"), ShowIf("@this.prevMode == PrevTargetMode.SceneName")]
    public string prevSceneName;

    [BoxGroup("Prev 目标"), LabelText("Prev 场景引用"), ShowIf("@this.prevMode == PrevTargetMode.SceneAsset"), AssetsOnly]
    public Object prevSceneAsset; // SceneAsset (Editor)

    [HideInInspector]
    public string prevCachedAssetSceneName;

    [BoxGroup("Prev 目标"), LabelText("Prev 索引"), ShowIf("@this.prevMode == PrevTargetMode.SequenceIndex")]
    public int prevIndex;

    // 兼容旧配置的开关已移除，统一由 TargetType/Next/Prev 配置控制显示

    private void OnEnable()
    {
        if (trigger == TriggerMode.OnEnable)
        {
            Trigger();
        }
    }

    private void Update()
    {
        if (trigger == TriggerMode.OnKeyDown && key != KeyCode.None)
        {
            if (Input.GetKeyDown(key)) Trigger();
        }
    }

    // 供按钮 OnClick 或脚本调用
    public void Trigger()
    {
        switch (targetType)
        {
            case TargetType.Next:
                ExecuteNext();
                return;
            case TargetType.Prev:
                ExecutePrev();
                return;
            case TargetType.SceneName:
                if (!string.IsNullOrEmpty(targetSceneName))
                    GlobalSceneManager.LoadWithLoadingScreen(targetSceneName, mode);
                return;
            case TargetType.SceneAsset:
            {
                var name = GetSceneNameFromAsset();
                if (!string.IsNullOrEmpty(name))
                    GlobalSceneManager.LoadWithLoadingScreen(name, mode);
                return;
            }
            case TargetType.SequenceIndex:
                GlobalSceneManager.GoToIndex(targetIndex);
                return;
            case TargetType.Selector:
                if (selector != null)
                    GlobalSceneManager.LoadWithLoadingScreen(selector.GetTargetSceneName(), selector.GetMode());
                else
                    GlobalSceneManager.Next();
                return;
            default:
                GlobalSceneManager.Next();
                return;
        }
    }

	// 直接给按钮绑定：下一场景
	public void GoNext()
	{
        ExecuteNext();
	}

	// 直接给按钮绑定：上一场景
	public void GoPrev()
	{
        ExecutePrev();
	}

#if UNITY_EDITOR
    private void OnValidate()
    {
        // 在编辑器中把 SceneAsset 同步到字符串，以便运行时使用
        if (targetType == TargetType.SceneAsset && targetSceneAsset != null)
        {
            if (targetSceneAsset is SceneAsset sa)
            {
                cachedAssetSceneName = sa.name;
            }
            else
            {
                // 允许从 Object 拖入，但若不是 SceneAsset 则清空
                cachedAssetSceneName = string.Empty;
            }
        }

        if (nextMode == NextTargetMode.SceneAsset && nextSceneAsset != null)
        {
            if (nextSceneAsset is SceneAsset nsa) nextCachedAssetSceneName = nsa.name; else nextCachedAssetSceneName = string.Empty;
        }
        if (prevMode == PrevTargetMode.SceneAsset && prevSceneAsset != null)
        {
            if (prevSceneAsset is SceneAsset psa) prevCachedAssetSceneName = psa.name; else prevCachedAssetSceneName = string.Empty;
        }
    }
#endif

    private string GetSceneNameFromAsset()
    {
#if UNITY_EDITOR
        if (targetSceneAsset is SceneAsset sa)
            return sa.name;
#endif
        return cachedAssetSceneName;
    }

    private void ExecuteNext()
    {
        switch (nextMode)
        {
            case NextTargetMode.SceneName:
                if (!string.IsNullOrEmpty(nextSceneName)) { GlobalSceneManager.LoadWithLoadingScreen(nextSceneName, mode); return; }
                break;
            case NextTargetMode.SceneAsset:
                {
                    var name = nextCachedAssetSceneName;
                    if (string.IsNullOrEmpty(name))
                    {
#if UNITY_EDITOR
                        if (nextSceneAsset is SceneAsset nsa) name = nsa.name;
#endif
                    }
                    if (!string.IsNullOrEmpty(name)) { GlobalSceneManager.LoadWithLoadingScreen(name, mode); return; }
                }
                break;
            case NextTargetMode.SequenceIndex:
                GlobalSceneManager.GoToIndex(nextIndex); return;
            case NextTargetMode.SequenceNext:
            default:
                break;
        }
        GlobalSceneManager.Next();
    }

    private void ExecutePrev()
    {
        switch (prevMode)
        {
            case PrevTargetMode.SceneName:
                if (!string.IsNullOrEmpty(prevSceneName)) { GlobalSceneManager.LoadWithLoadingScreen(prevSceneName, mode); return; }
                break;
            case PrevTargetMode.SceneAsset:
                {
                    var name = prevCachedAssetSceneName;
                    if (string.IsNullOrEmpty(name))
                    {
#if UNITY_EDITOR
                        if (prevSceneAsset is SceneAsset psa) name = psa.name;
#endif
                    }
                    if (!string.IsNullOrEmpty(name)) { GlobalSceneManager.LoadWithLoadingScreen(name, mode); return; }
                }
                break;
            case PrevTargetMode.SequenceIndex:
                GlobalSceneManager.GoToIndex(prevIndex); return;
            case PrevTargetMode.SequencePrev:
            default:
                break;
        }
        GlobalSceneManager.Prev();
    }
}


