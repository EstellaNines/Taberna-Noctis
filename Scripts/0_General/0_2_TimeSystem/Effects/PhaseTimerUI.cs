using UnityEngine;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

/// <summary>
/// 阶段倒计时 UI：显示剩余真实时间 mm:ss，并用环形进度条表示进度。
/// </summary>
public class PhaseTimerUI : MonoBehaviour
{
    [Header("引用")]
#if ODIN_INSPECTOR
    [LabelText("时间文本(TMP)")]
#endif
    [SerializeField] private TextMeshProUGUI timerText;   // 数字文本 mm:ss
#if ODIN_INSPECTOR
    [LabelText("圆环图像(Image)")]
#endif
    [SerializeField] private Image progressRing;          // 环形（Filled/Radial360）

    [Header("渐变（圆环）")]
#if ODIN_INSPECTOR
    [LabelText("白天→下午 渐变")]
#endif
    [SerializeField] private Gradient dayToAfternoonGradient;      // 天蓝 → 淡橙
#if ODIN_INSPECTOR
    [LabelText("下午→夜晚 渐变")]
#endif
    [SerializeField] private Gradient afternoonToNightGradient;    // 淡橙 → 星空色

    [Header("渐变（文本颜色）")]
#if ODIN_INSPECTOR
    [LabelText("文本颜色 渐变")]
#endif
    [SerializeField] private Gradient textColorGradient; // 白 → 绿 → 红

    [Header("补间设置")]
#if ODIN_INSPECTOR
    [LabelText("补间时长(秒)")]
#endif
    [SerializeField] private float tweenDuration = 0.2f; // 进度与颜色过渡时间

    private Gradient activeRingGradient;
    private float shownProgress = 1f; // 0..1 (remaining/total)
    private Tweener progressTweener;

    private void Reset()
    {
        if (timerText == null) timerText = GetComponentInChildren<TextMeshProUGUI>();
        if (progressRing == null) progressRing = GetComponentInChildren<Image>();

        // 合理的默认渐变
        if (dayToAfternoonGradient == null)
        {
            dayToAfternoonGradient = new Gradient();
            dayToAfternoonGradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(new Color(0.53f, 0.81f, 0.94f), 0f), new GradientColorKey(new Color(1f, 0.85f, 0.72f), 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
            );
        }
        if (afternoonToNightGradient == null)
        {
            afternoonToNightGradient = new Gradient();
            afternoonToNightGradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(new Color(1f, 0.85f, 0.72f), 0f), new GradientColorKey(new Color(0.105f, 0.141f, 0.349f), 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
            );
        }
        if (textColorGradient == null)
        {
            textColorGradient = new Gradient();
            textColorGradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(Color.green, 0.5f),
                    new GradientColorKey(Color.red, 1f)
                },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
            );
        }
    }

    private void OnEnable()
    {
        MessageManager.Register<TimePhase>(MessageDefine.PHASE_CHANGED, OnPhaseChanged);
        // 初始化一次
        var mgr = TimeSystemManager.Instance;
        if (mgr != null) SetActiveGradientByPhase(mgr.CurrentPhase);
    }

    private void OnDisable()
    {
        MessageManager.Remove<TimePhase>(MessageDefine.PHASE_CHANGED, OnPhaseChanged);
        progressTweener?.Kill();
    }

    private void OnPhaseChanged(TimePhase phase)
    {
        SetActiveGradientByPhase(phase);
        // 重置显示进度（从1渐变至目标）
        shownProgress = 1f;
        if (progressRing != null) progressRing.fillAmount = shownProgress;
    }

    private void SetActiveGradientByPhase(TimePhase phase)
    {
        switch (phase)
        {
            case TimePhase.Morning:
            case TimePhase.Afternoon:
                activeRingGradient = dayToAfternoonGradient;
                break;
            case TimePhase.Night:
                activeRingGradient = afternoonToNightGradient;
                break;
            default:
                activeRingGradient = dayToAfternoonGradient;
                break;
        }
    }

    private void Update()
    {
        var mgr = TimeSystemManager.Instance;
        if (mgr == null) return;

        float remaining = Mathf.Max(0f, mgr.PhaseRemainingTime);
        int minutes = Mathf.FloorToInt(remaining / 60f);
        int seconds = Mathf.FloorToInt(remaining % 60f);

        if (timerText != null)
        {
            timerText.text = $"{minutes:D2}:{seconds:D2}";
        }

        float targetProgress = mgr.PhaseDuration <= 0f ? 0f : remaining / mgr.PhaseDuration; // 1→0
        if (Mathf.Abs(targetProgress - shownProgress) > 0.001f)
        {
            progressTweener?.Kill();
            progressTweener = DOVirtual.Float(shownProgress, targetProgress, tweenDuration, v =>
            {
                shownProgress = v;
                ApplyVisuals(v);
            });
        }
        else
        {
            ApplyVisuals(shownProgress);
        }
    }

    private void ApplyVisuals(float progress)
    {
        if (progressRing != null)
        {
            progressRing.fillAmount = progress;
            if (activeRingGradient != null)
            {
                var ringCol = activeRingGradient.Evaluate(1f - progress);
                progressRing.DOColor(ringCol, tweenDuration).SetUpdate(true);
            }
        }

        if (timerText != null && textColorGradient != null)
        {
            var txtCol = textColorGradient.Evaluate(1f - progress);
            timerText.DOColor(txtCol, tweenDuration).SetUpdate(true);
        }
    }
}


