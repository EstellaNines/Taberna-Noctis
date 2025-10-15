using UnityEngine;
using UnityEngine.UI;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif
using UnityEngine.SceneManagement;

/// <summary>
/// 控制 UI RawImage 在「早上」阶段使用 M_GradientSky_Morning 材质，
/// 并在整段早上期间将材质参数从起始值平滑过渡到目标值：
///  - _ColorInfluence_1: 0.25 → 0.5
///  - _ColorInfluence_2: 0.75 → 0.5
/// 只在早上阶段生效，避免修改资源本体（运行时克隆材质实例）。
/// </summary>
public class MorningSkyMaterialController : MonoBehaviour
{
    [Header("目标 RawImage 与材质")]
#if ODIN_INSPECTOR
    [BoxGroup("引用"), LabelText("目标 RawImage")]
#endif
    [SerializeField] private RawImage targetRawImage;                  // 要控制的 UI RawImage
#if ODIN_INSPECTOR
    [BoxGroup("阶段材质"), LabelText("早上材质")]
#endif
    [SerializeField] private Material morningMaterial;                 // 指向 M_GradientSky_Morning
#if ODIN_INSPECTOR
    [BoxGroup("阶段材质"), LabelText("下午材质")]
#endif
    [SerializeField] private Material afternoonMaterial;               // 指向下午材质
#if ODIN_INSPECTOR
    [BoxGroup("阶段材质"), LabelText("夜晚材质")]
#endif
    [SerializeField] private Material nightMaterial;                   // 指向夜晚材质

    [Header("场景规则")]
#if ODIN_INSPECTOR
    [BoxGroup("场景规则"), LabelText("DayMessage 场景名")]
#endif
    [SerializeField] private string dayMessageSceneName = "2_DayMessageScreen";
#if ODIN_INSPECTOR
    [BoxGroup("场景规则"), LabelText("Day 场景名")]
#endif
    [SerializeField] private string dayScreenSceneName = "3_DayScreen";
#if ODIN_INSPECTOR
    [BoxGroup("场景规则"), LabelText("Night 场景名")]
#endif
    [SerializeField] private string nightScreenSceneName = "4_NightScreen";

    [Header("材质属性名（Shader 参数）")]
#if ODIN_INSPECTOR
    [BoxGroup("Shader 参数名"), LabelText("影响1参数_ColorInfluence_1")]
#endif
    [SerializeField] private string colorInfluence1Name = "_ColorInfluence_1";
#if ODIN_INSPECTOR
    [BoxGroup("Shader 参数名"), LabelText("影响2参数_ColorInfluence_2")]
#endif
    [SerializeField] private string colorInfluence2Name = "_ColorInfluence_2";

    [Header("分阶段过渡设置")]
#if ODIN_INSPECTOR
    [FoldoutGroup("分阶段设置"), FoldoutGroup("分阶段设置/早上"), LabelText("起始 Color1（0.25）")]
#endif
    [SerializeField] private float startInfluence1 = 0.25f;            // 早上开始时 Color1 位置
#if ODIN_INSPECTOR
    [FoldoutGroup("分阶段设置/早上"), LabelText("起始 Color2（0.75）")]
#endif
    [SerializeField] private float startInfluence2 = 0.75f;            // 早上开始时 Color2 位置
#if ODIN_INSPECTOR
    [FoldoutGroup("分阶段设置/早上"), LabelText("结束位置（0.5）")]
#endif
    [SerializeField] private float endInfluence = 0.5f;                // 早上结束时二者都为 0.5

#if ODIN_INSPECTOR
    [FoldoutGroup("分阶段设置"), FoldoutGroup("分阶段设置/下午"), LabelText("起始 Color1（1.0）")]
#endif
    [SerializeField] private float afternoonStartInfluence1 = 1.0f;
#if ODIN_INSPECTOR
    [FoldoutGroup("分阶段设置/下午"), LabelText("起始 Color2（0.0）")]
#endif
    [SerializeField] private float afternoonStartInfluence2 = 0.0f;
#if ODIN_INSPECTOR
    [FoldoutGroup("分阶段设置/下午"), LabelText("结束 Color1（0.25）")]
#endif
    [SerializeField] private float afternoonEndInfluence1 = 0.25f;
#if ODIN_INSPECTOR
    [FoldoutGroup("分阶段设置/下午"), LabelText("结束 Color2（0.75）")]
#endif
    [SerializeField] private float afternoonEndInfluence2 = 0.75f;

#if ODIN_INSPECTOR
    [FoldoutGroup("分阶段设置"), FoldoutGroup("分阶段设置/夜晚"), LabelText("起始 Color1（1.0）")]
#endif
    [SerializeField] private float nightStartInfluence1 = 1.0f;
#if ODIN_INSPECTOR
    [FoldoutGroup("分阶段设置/夜晚"), LabelText("起始 Color2（0.0）")]
#endif
    [SerializeField] private float nightStartInfluence2 = 0.0f;
#if ODIN_INSPECTOR
    [FoldoutGroup("分阶段设置/夜晚"), LabelText("结束 Color1（0.25）")]
#endif
    [SerializeField] private float nightEndInfluence1 = 0.25f;
#if ODIN_INSPECTOR
    [FoldoutGroup("分阶段设置/夜晚"), LabelText("结束 Color2（0.75）")]
#endif
    [SerializeField] private float nightEndInfluence2 = 0.75f;

    private Material runtimeMaterial;                                   // 运行时实例，避免改资源本体
    private TimePhase lastAppliedPhase = (TimePhase)(-1);

    private void Reset()
    {
        if (targetRawImage == null)
        {
            targetRawImage = GetComponent<RawImage>();
        }
    }

    private void OnEnable()
    {
        MessageManager.Register<TimePhase>(MessageDefine.PHASE_CHANGED, OnPhaseChanged);

        // 刚启用时，根据当前阶段初始化
        var tsm = TimeSystemManager.Instance;
        if (tsm != null)
        {
            ApplyPhase(tsm.CurrentPhase, true);
        }
    }

    private void OnDisable()
    {
        MessageManager.Remove<TimePhase>(MessageDefine.PHASE_CHANGED, OnPhaseChanged);
    }

    private void OnPhaseChanged(TimePhase newPhase)
    {
        ApplyPhase(newPhase, true);
    }

    private void Update()
    {
        var tsm = TimeSystemManager.Instance;
        if (tsm == null) return;

        // DayMessageScreen：不更新材质参数
        var activeSceneName = SceneManager.GetActiveScene().name;
        if (!string.IsNullOrEmpty(dayMessageSceneName) && activeSceneName == dayMessageSceneName)
        {
            return;
        }

        // 防丢：若外部切换阶段但未收到事件，这里兜底一次
        if (tsm.CurrentPhase != lastAppliedPhase)
        {
            ApplyPhase(tsm.CurrentPhase, false);
        }

        // 在当前阶段推进材质影响值
        if (runtimeMaterial == null || targetRawImage == null) return;

        float duration = Mathf.Max(0.0001f, tsm.PhaseDuration);
        float remaining = Mathf.Clamp(tsm.PhaseRemainingTime, 0f, duration);
        float progress = 1f - (remaining / duration); // 0 → 1 across current phase

        // 依据场景决定有效阶段：
        // - NightScreen：固定夜晚
        // - DayScreen：仅早上/下午
        // - 其他：按时间系统
        var effectivePhase = tsm.CurrentPhase;
        if (!string.IsNullOrEmpty(nightScreenSceneName) && activeSceneName == nightScreenSceneName)
        {
            effectivePhase = TimePhase.Night;
        }
        else if (!string.IsNullOrEmpty(dayScreenSceneName) && activeSceneName == dayScreenSceneName)
        {
            effectivePhase = (effectivePhase == TimePhase.Afternoon) ? TimePhase.Afternoon : TimePhase.Morning;
        }

        float inf1;
        float inf2;
        switch (effectivePhase)
        {
            case TimePhase.Morning:
                inf1 = Mathf.Lerp(startInfluence1, endInfluence, progress);
                inf2 = Mathf.Lerp(startInfluence2, endInfluence, progress);
                break;
            case TimePhase.Afternoon:
                inf1 = Mathf.Lerp(afternoonStartInfluence1, afternoonEndInfluence1, progress);
                inf2 = Mathf.Lerp(afternoonStartInfluence2, afternoonEndInfluence2, progress);
                break;
            case TimePhase.Night:
                inf1 = Mathf.Lerp(nightStartInfluence1, nightEndInfluence1, progress);
                inf2 = Mathf.Lerp(nightStartInfluence2, nightEndInfluence2, progress);
                break;
            default:
                inf1 = 0.5f; inf2 = 0.5f;
                break;
        }

        if (!string.IsNullOrEmpty(colorInfluence1Name)) runtimeMaterial.SetFloat(colorInfluence1Name, inf1);
        if (!string.IsNullOrEmpty(colorInfluence2Name)) runtimeMaterial.SetFloat(colorInfluence2Name, inf2);
    }

    private void ApplyPhase(TimePhase phase, bool resetInfluences)
    {
        lastAppliedPhase = phase;

        // DayMessageScreen：不切换材质、不重置参数
        var sceneName = SceneManager.GetActiveScene().name;
        if (!string.IsNullOrEmpty(dayMessageSceneName) && sceneName == dayMessageSceneName)
        {
            return;
        }

        // 计算用于选择材质与初始值的有效阶段
        var effectivePhase = phase;
        if (!string.IsNullOrEmpty(nightScreenSceneName) && sceneName == nightScreenSceneName)
        {
            effectivePhase = TimePhase.Night;
        }
        else if (!string.IsNullOrEmpty(dayScreenSceneName) && sceneName == dayScreenSceneName)
        {
            effectivePhase = (phase == TimePhase.Afternoon) ? TimePhase.Afternoon : TimePhase.Morning;
        }

        EnsureRuntimeMaterial(effectivePhase);
        if (targetRawImage != null && runtimeMaterial != null)
        {
            if (targetRawImage.material != runtimeMaterial)
            {
                targetRawImage.material = runtimeMaterial;
            }
            if (resetInfluences)
            {
                switch (effectivePhase)
                {
                    case TimePhase.Morning:
                        if (!string.IsNullOrEmpty(colorInfluence1Name)) runtimeMaterial.SetFloat(colorInfluence1Name, startInfluence1);
                        if (!string.IsNullOrEmpty(colorInfluence2Name)) runtimeMaterial.SetFloat(colorInfluence2Name, startInfluence2);
                        break;
                    case TimePhase.Afternoon:
                        if (!string.IsNullOrEmpty(colorInfluence1Name)) runtimeMaterial.SetFloat(colorInfluence1Name, afternoonStartInfluence1);
                        if (!string.IsNullOrEmpty(colorInfluence2Name)) runtimeMaterial.SetFloat(colorInfluence2Name, afternoonStartInfluence2);
                        break;
                    case TimePhase.Night:
                        if (!string.IsNullOrEmpty(colorInfluence1Name)) runtimeMaterial.SetFloat(colorInfluence1Name, nightStartInfluence1);
                        if (!string.IsNullOrEmpty(colorInfluence2Name)) runtimeMaterial.SetFloat(colorInfluence2Name, nightStartInfluence2);
                        break;
                }
            }
        }
    }

    private void EnsureRuntimeMaterial(TimePhase phase)
    {
        // 每次阶段切换时创建/替换运行时材质实例，避免累计修改同一实例带来的相互影响
        Material source = null;
        switch (phase)
        {
            case TimePhase.Morning:   source = morningMaterial;   break;
            case TimePhase.Afternoon: source = afternoonMaterial; break;
            case TimePhase.Night:     source = nightMaterial;     break;
        }

        if (source != null)
        {
            runtimeMaterial = new Material(source);
        }
        else if (targetRawImage != null && targetRawImage.material != null)
        {
            // 兜底：克隆当前材质，避免直接修改资源本体
            runtimeMaterial = new Material(targetRawImage.material);
        }
    }
}


