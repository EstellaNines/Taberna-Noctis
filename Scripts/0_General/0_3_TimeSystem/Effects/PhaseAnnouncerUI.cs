using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

/// <summary>
/// 进入场景或阶段切换时，使用 DOTween 播放当前时间段的提示动画（RawImage + TMP）。
/// 将本脚本挂到场景中的一个物体，并在 Inspector 绑定 RawImage 与 TMP 引用。
/// </summary>
public class PhaseAnnouncerUI : MonoBehaviour
{
#if ODIN_INSPECTOR
    [BoxGroup("引用"), LabelText("背景(RawImage)"), Required]
#endif
    [SerializeField] private RawImage bannerImage;

#if ODIN_INSPECTOR
    [BoxGroup("引用"), LabelText("文字(TMP)"), Required]
#endif
    [SerializeField] private TextMeshProUGUI phaseText;

#if ODIN_INSPECTOR
    [BoxGroup("文本"), LabelText("早上 文本")]
#endif
    [SerializeField] private string morningText = "Morning";
#if ODIN_INSPECTOR
    [BoxGroup("文本"), LabelText("下午 文本")]
#endif
    [SerializeField] private string afternoonText = "Afternoon";
#if ODIN_INSPECTOR
    [BoxGroup("文本"), LabelText("夜晚 文本")]
#endif
    [SerializeField] private string nightText = "Night";

#if ODIN_INSPECTOR
    [BoxGroup("配色"), LabelText("早上 颜色(起)")]
#endif
    [SerializeField] private Color morningFrom = new Color(0.53f, 0.81f, 0.94f);
#if ODIN_INSPECTOR
    [BoxGroup("配色"), LabelText("早上 颜色(止)")]
#endif
    [SerializeField] private Color morningTo = new Color(1f, 0.85f, 0.72f);

#if ODIN_INSPECTOR
    [BoxGroup("配色"), LabelText("下午 颜色(起)")]
#endif
    [SerializeField] private Color afternoonFrom = new Color(1f, 0.85f, 0.72f);
#if ODIN_INSPECTOR
    [BoxGroup("配色"), LabelText("下午 颜色(止)")]
#endif
    [SerializeField] private Color afternoonTo = new Color(0.99f, 0.73f, 0.45f);

#if ODIN_INSPECTOR
    [BoxGroup("配色"), LabelText("夜晚 颜色(起)")]
#endif
    [SerializeField] private Color nightFrom = new Color(0.10f, 0.14f, 0.35f);
#if ODIN_INSPECTOR
    [BoxGroup("配色"), LabelText("夜晚 颜色(止)")]
#endif
    [SerializeField] private Color nightTo = new Color(0.22f, 0.25f, 0.52f);

#if ODIN_INSPECTOR
    [BoxGroup("动画"), LabelText("淡入时长(s)")]
#endif
    [SerializeField] private float fadeIn = 0.35f;
#if ODIN_INSPECTOR
    [BoxGroup("动画"), LabelText("停留时长(s)")]
#endif
    [SerializeField] private float hold = 0.9f;
#if ODIN_INSPECTOR
    [BoxGroup("动画"), LabelText("淡出时长(s)")]
#endif
    [SerializeField] private float fadeOut = 0.4f;

    private Sequence _seq;

    private void Awake()
    {
        // 初始透明
        if (bannerImage != null)
        {
            var c = bannerImage.color; c.a = 0f; bannerImage.color = c;
        }
        if (phaseText != null)
        {
            var c = phaseText.color; c.a = 0f; phaseText.color = c;
        }
    }

    private void OnEnable()
    {
        MessageManager.Register<TimePhase>(MessageDefine.PHASE_CHANGED, OnPhaseChanged);
        // 场景进入时也播一次
        var mgr = TimeSystemManager.Instance;
        if (mgr != null)
        {
            PlayForPhase(mgr.CurrentPhase);
        }
    }

    private void OnDisable()
    {
        MessageManager.Remove<TimePhase>(MessageDefine.PHASE_CHANGED, OnPhaseChanged);
        _seq?.Kill();
    }

    private void OnPhaseChanged(TimePhase phase)
    {
        PlayForPhase(phase);
    }

    private void PlayForPhase(TimePhase phase)
    {
        if (bannerImage == null || phaseText == null) return;

        string text;
        Color from, to;
        switch (phase)
        {
            case TimePhase.Morning:
                text = morningText; from = morningFrom; to = morningTo; break;
            case TimePhase.Afternoon:
                text = afternoonText; from = afternoonFrom; to = afternoonTo; break;
            default:
                text = nightText; from = nightFrom; to = nightTo; break;
        }

        phaseText.text = text;

        // 终止之前的动画
        _seq?.Kill();
        _seq = DOTween.Sequence();

        // 设初始值
        bannerImage.color = new Color(from.r, from.g, from.b, 0f);
        phaseText.color = new Color(phaseText.color.r, phaseText.color.g, phaseText.color.b, 0f);
        phaseText.transform.localScale = Vector3.one * 0.9f;

        // 渐入
        _seq.Append(bannerImage.DOColor(new Color(from.r, from.g, from.b, 1f), fadeIn));
        _seq.Join(phaseText.DOFade(1f, fadeIn));
        _seq.Join(phaseText.transform.DOScale(1.05f, fadeIn));

        // 颜色由 from 渐变到 to
        _seq.Append(bannerImage.DOColor(new Color(to.r, to.g, to.b, 1f), hold));

        // 渐出
        _seq.Append(bannerImage.DOFade(0f, fadeOut));
        _seq.Join(phaseText.DOFade(0f, fadeOut));
        _seq.Join(phaseText.transform.DOScale(1f, fadeOut));
    }
}


