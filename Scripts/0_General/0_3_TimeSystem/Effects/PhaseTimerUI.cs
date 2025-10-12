using UnityEngine;
using TMPro;
using UnityEngine.UI;
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
    [SerializeField] private RawImage backgroundRawImage; // 背景（ShaderGraph 材质方式）

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
    [SerializeField] private float tweenDuration = 0.0f; // 进度与颜色过渡时间

    [Header("ShaderGraph 材质控制（RawImage）")]
#if ODIN_INSPECTOR
    [LabelText("使用 ShaderGraph 背景")]
#endif
    [SerializeField] private bool useShaderGraphBackground = false;
#if ODIN_INSPECTOR
    [LabelText("覆写材质(可空，默认克隆运行时材质)")]
#endif
    [SerializeField] private Material overrideBackgroundMaterial;
#if ODIN_INSPECTOR
    [LabelText("Influence1 属性名（淡黄）")]
#endif
    [SerializeField] private string matInfluence1Name = "_ColorInfluence_1";
#if ODIN_INSPECTOR
    [LabelText("Influence2 属性名（天蓝）")]
#endif
    [SerializeField] private string matInfluence2Name = "_ColorInfluence_2";
    // 不再直接修改材质颜色属性，仅控制 Influence 与材质切换

    [Header("阶段材质（RawImage 将在阶段切换时替换）")]
#if ODIN_INSPECTOR
    [LabelText("早晨材质")]
#endif
    [SerializeField] private Material morningMat;
#if ODIN_INSPECTOR
    [LabelText("下午材质")]
#endif
    [SerializeField] private Material afternoonMat;
#if ODIN_INSPECTOR
    [LabelText("夜晚材质")]
#endif
    [SerializeField] private Material nightMat;

    private Material runtimeMat; // 运行时实例，避免修改资源本体
    private TimePhase lastAppliedPhase; // 记录已应用材质的阶段

    [Header("文字颜色联动（可选）")]
#if ODIN_INSPECTOR
    [LabelText("当下午且 Color2>0.5 时，以下文字置为白色")]
#endif
    [SerializeField] private TextMeshProUGUI[] textsToWhiten;
	[SerializeField, Range(0f,1f)] private float influenceSwitchThreshold = 0.5f; // 下午文字切换阈值

    [Header("显示规则")]
#if ODIN_INSPECTOR
    [LabelText("最后警告秒数")]
#endif
    [SerializeField] private float finalWarningSeconds = 30f;
#if ODIN_INSPECTOR
    [LabelText("警告颜色")]
#endif
    [SerializeField] private Color finalWarningColor = Color.red;

    private Gradient activeRingGradient;
    private float shownProgress = 1f; // 0..1 (remaining/total)
    // 移除逐帧创建的补间，改为插值/直接赋值
    private bool showFinalWarning = false;
    private bool lastShowFinalWarning = false;

    private void Reset()
    {
        if (timerText == null) timerText = GetComponentInChildren<TextMeshProUGUI>();
        if (progressRing == null) progressRing = GetComponentInChildren<Image>();
        if (backgroundRawImage == null)
        {
            var raws = GetComponentsInChildren<RawImage>();
            if (raws != null && raws.Length > 0) backgroundRawImage = raws[0];
        }

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

        // 无背景渐变（改用 ShaderGraph 颜色），无需初始化
    }

    private void OnEnable()
    {
        MessageManager.Register<TimePhase>(MessageDefine.PHASE_CHANGED, OnPhaseChanged);
        // 初始化一次
        var mgr = TimeSystemManager.Instance;
        if (mgr != null) SetActiveGradientByPhase(mgr.CurrentPhase);

        // 初始化材质：优先使用阶段指定材质，否则克隆现有材质，避免修改资源本体
        if (useShaderGraphBackground && backgroundRawImage != null)
        {
            var initPhase = TimeSystemManager.Instance != null ? TimeSystemManager.Instance.CurrentPhase : TimePhase.Morning;
            ApplyPhaseMaterial(initPhase);
        }
    }

    private void OnDisable()
    {
        MessageManager.Remove<TimePhase>(MessageDefine.PHASE_CHANGED, OnPhaseChanged);
    }

    private void OnPhaseChanged(TimePhase phase)
    {
        SetActiveGradientByPhase(phase);
        // 重置显示进度（从1渐变至目标）
        shownProgress = 1f;
        if (progressRing != null) progressRing.fillAmount = shownProgress;

        // 背景：按阶段替换材质，并设置 Influence 初始值
        if (useShaderGraphBackground && backgroundRawImage != null)
        {
            ApplyPhaseMaterial(phase);
        }
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
        // 防丢消息：若阶段变化但未收到事件，按当前阶段强制切换材质
        if (useShaderGraphBackground && backgroundRawImage != null)
        {
            var phaseNow = mgr.CurrentPhase;
            if (phaseNow != lastAppliedPhase)
            {
                ApplyPhaseMaterial(phaseNow);
            }
        }

        float remaining = Mathf.Max(0f, mgr.PhaseRemainingTime);
        int minutes = Mathf.FloorToInt(remaining / 60f);
        int seconds = Mathf.FloorToInt(remaining % 60f);

        showFinalWarning = remaining <= finalWarningSeconds + 0.0001f;

        if (timerText != null)
        {
            if (showFinalWarning)
            {
                timerText.text = $"{minutes:D2}:{seconds:D2}";
            }
            else
            {
                // 非最后30秒隐藏文本内容
                timerText.text = string.Empty;
            }
        }

        float targetProgress = mgr.PhaseDuration <= 0f ? 0f : remaining / mgr.PhaseDuration; // 1→0
        // 使用平滑插值避免每帧创建补间
        float step = (tweenDuration <= 0.0001f) ? 1f : Mathf.Clamp01(Time.unscaledDeltaTime / tweenDuration);
        shownProgress = Mathf.Lerp(shownProgress, targetProgress, step);
        ApplyVisuals(shownProgress);

        // 仅在状态切换时处理一次显隐与颜色
        if (timerText != null && lastShowFinalWarning != showFinalWarning)
        {
            if (showFinalWarning)
            {
                var c = finalWarningColor;
                c.a = 1f;
                timerText.color = c; // 纯红且可见
            }
            else
            {
                var c = timerText.color;
                c.a = 0f;
                timerText.color = c; // 隐藏
            }
        }
        lastShowFinalWarning = showFinalWarning;
    }

    private void ApplyVisuals(float progress)
    {
        if (progressRing != null)
        {
            progressRing.fillAmount = progress;
            if (activeRingGradient != null)
            {
                var ringCol = activeRingGradient.Evaluate(1f - progress);
                // 直接赋值颜色，避免每帧创建补间
                progressRing.color = ringCol;
            }
        }

        // 文本颜色/显隐已在 Update 的状态切换逻辑处理中，这里不再每帧创建补间

        // 背景：ShaderGraph(RawImage)
        if (useShaderGraphBackground && backgroundRawImage != null)
        {
			// 夜晚：严格按真实倒计时推进（剩余/总时长）
            float cover = 1f - Mathf.Clamp01(progress); // 默认值
            var mgr = TimeSystemManager.Instance;
            if (mgr != null)
            {
                if (mgr.CurrentPhase == TimePhase.Night)
                {
                    float remaining = Mathf.Max(0f, mgr.PhaseRemainingTime);
                    float duration = Mathf.Max(0.0001f, mgr.PhaseDuration);
                    cover = Mathf.Clamp01(1f - (remaining / duration));
                }
                else if (mgr.GameClock != null && mgr.TryGetPhaseInfo(mgr.CurrentPhase, out var info))
                {
                    // 其他阶段：按时钟范围（包含跨天处理）
                    int startMin = info.startHour * 60 + info.startMinute;
                    int phaseGameSeconds = Mathf.RoundToInt(info.durationSeconds * info.timeScale);
                    int endMin = startMin + Mathf.RoundToInt(phaseGameSeconds / 60f);
                    int curMin = mgr.GameClock.Hour * 60 + mgr.GameClock.Minute;
                    if (endMin <= startMin) endMin += 24 * 60;
                    if (curMin < startMin) curMin += 24 * 60;
                    cover = Mathf.InverseLerp(startMin, endMin, curMin);
                }
            }
            if (cover <= 0.001f) cover = 0f;
            if (cover >= 0.999f || (TimeSystemManager.Instance != null && TimeSystemManager.Instance.PhaseRemainingTime <= 0.01f)) cover = 1f;
            var mat = backgroundRawImage.material;
            if (mat != null)
            {
				// 两者总和恒为1；
				// 夜晚：Color1 最小值0.5，Color2 最大值0.5（结束时 0.5/0.5）
				float inf1;
				float inf2;
				if (TimeSystemManager.Instance != null && TimeSystemManager.Instance.CurrentPhase == TimePhase.Night)
				{
					inf2 = 0.5f * cover;            // 0 → 0.5
					inf1 = 1f - 0.5f * cover;        // 1 → 0.5
				}
				else
				{
					inf2 = cover;                   // 0 → 1
					inf1 = 1f - cover;              // 1 → 0
				}
                if (!string.IsNullOrEmpty(matInfluence1Name)) mat.SetFloat(matInfluence1Name, inf1);
                if (!string.IsNullOrEmpty(matInfluence2Name)) mat.SetFloat(matInfluence2Name, inf2);

                // 当处于下午阶段且 Color2>阈值 时，将指定文字改为白色
                if (mgr != null && mgr.CurrentPhase == TimePhase.Afternoon && textsToWhiten != null)
                {
                    if (inf2 > influenceSwitchThreshold)
                    {
                        for (int i = 0; i < textsToWhiten.Length; i++)
                        {
                            if (textsToWhiten[i] != null)
                            {
                                textsToWhiten[i].color = Color.white;
                            }
                        }
                    }
                }

				// 夜晚：不修改字体颜色（按需求禁用夜晚文字联动）
            }
        }
        // 无 RawImage 或未启用 SG 背景时不处理
    }

    private Material SelectPhaseMaterial(TimePhase phase)
    {
        switch (phase)
        {
            case TimePhase.Morning: return morningMat != null ? new Material(morningMat) : null;
            case TimePhase.Afternoon: return afternoonMat != null ? new Material(afternoonMat) : null;
            case TimePhase.Night: return nightMat != null ? new Material(nightMat) : null;
            default: return null;
        }
    }

    private void ApplyPhaseMaterial(TimePhase phase)
    {
        runtimeMat = SelectPhaseMaterial(phase);
        if (runtimeMat == null)
        {
            if (overrideBackgroundMaterial != null) runtimeMat = new Material(overrideBackgroundMaterial);
            else if (backgroundRawImage != null && backgroundRawImage.material != null) runtimeMat = new Material(backgroundRawImage.material);
        }
        if (backgroundRawImage != null && runtimeMat != null)
        {
            backgroundRawImage.material = runtimeMat;
            var mat = backgroundRawImage.material;
            if (mat != null)
            {
                if (!string.IsNullOrEmpty(matInfluence1Name)) mat.SetFloat(matInfluence1Name, 1f);
                if (!string.IsNullOrEmpty(matInfluence2Name)) mat.SetFloat(matInfluence2Name, 0f);
            }
        }
        lastAppliedPhase = phase;
    }

    // 颜色由各阶段材质自身决定，此处不再直接改材质颜色
}


