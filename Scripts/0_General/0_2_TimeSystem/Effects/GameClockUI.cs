using UnityEngine;
using TMPro;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

/// <summary>
/// 基础时钟显示组件：将标签(第X天 时段)与时间(HH:MM)分别输出到两个 TMP 文本。
/// 将本脚本挂到任意物体，在 Inspector 指定 labelText 与 timeText 引用即可。
/// </summary>
public class GameClockUI : MonoBehaviour
{
#if ODIN_INSPECTOR
    [LabelText("标题文本(TMP)")]
#endif
    [SerializeField] private TextMeshProUGUI labelText; // Day X + Phase
#if ODIN_INSPECTOR
    [LabelText("时间文本(TMP)")]
#endif
    [SerializeField] private TextMeshProUGUI timeText;  // HH:MM

    private void Reset()
    {
        // 优先尝试获取当前节点上的两个 TMP（若只有一个，则作为 timeText）
        var tmps = GetComponentsInChildren<TextMeshProUGUI>();
        if (tmps != null && tmps.Length > 0)
        {
            if (timeText == null) timeText = tmps[0];
            if (tmps.Length > 1 && labelText == null) labelText = tmps[1];
        }
    }

    private void Update()
    {
        if (TimeSystemManager.Instance == null) return;

        var mgr = TimeSystemManager.Instance;
        string phaseText = GetPhaseText(mgr.CurrentPhase);
        string time = mgr.GameClock != null ? mgr.GameClock.GetTimeString() : "--:--";

        if (labelText != null)
        {
            labelText.text = $"Day {mgr.CurrentDay} {phaseText}";
        }
        if (timeText != null)
        {
            timeText.text = time;
        }
    }

    private string GetPhaseText(TimePhase phase)
    {
        switch (phase)
        {
            case TimePhase.Morning: return "Morning";
            case TimePhase.Afternoon: return "Afternoon";
            case TimePhase.Night: return "Night";
            default: return string.Empty;
        }
    }
}


