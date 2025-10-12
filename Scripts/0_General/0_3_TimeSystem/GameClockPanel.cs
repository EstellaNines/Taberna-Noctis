using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

/// <summary>
/// Odin 检查器面板：用于在 Inspector 中直观查看与调试 GameClock/阶段信息
/// 可挂在与 TimeSystemManager 同一个物体上。
/// </summary>
public class GameClockPanel : MonoBehaviour
{
#if ODIN_INSPECTOR
    [FoldoutGroup("时间状态"), LabelText("当前天数"), ReadOnly]
#endif
    [SerializeField] private int day;

#if ODIN_INSPECTOR
    [FoldoutGroup("时间状态"), LabelText("当前阶段"), ReadOnly]
#endif
    [SerializeField] private string phase;

#if ODIN_INSPECTOR
    [FoldoutGroup("时间状态"), LabelText("时钟"), ReadOnly]
#endif
    [SerializeField] private string clock;

#if ODIN_INSPECTOR
    [FoldoutGroup("时间状态"), LabelText("剩余秒"), ReadOnly]
#endif
    [SerializeField] private float remainingSeconds;

#if ODIN_INSPECTOR
    [FoldoutGroup("调试快捷"), Button("跳到下午")]
#endif
    public void DebugJumpAfternoon()
    {
        if (TimeSystemManager.Instance == null) return;
#if UNITY_EDITOR
        var method = typeof(TimeSystemManager).GetMethod("DebugJumpToAfternoon", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method?.Invoke(TimeSystemManager.Instance, null);
#endif
    }

#if ODIN_INSPECTOR
    [FoldoutGroup("调试快捷"), Button("跳到夜晚")]
#endif
    public void DebugJumpNight()
    {
        if (TimeSystemManager.Instance == null) return;
#if UNITY_EDITOR
        var method = typeof(TimeSystemManager).GetMethod("DebugJumpToNight", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method?.Invoke(TimeSystemManager.Instance, null);
#endif
    }

#if ODIN_INSPECTOR
    [FoldoutGroup("时间编辑"), LabelText("目标阶段")]
    public TimePhase editPhase = TimePhase.Morning;

    [FoldoutGroup("时间编辑"), LabelText("小时"), Range(0, 23)]
    public int editHour = 8;

    [FoldoutGroup("时间编辑"), LabelText("分钟"), Range(0, 59)]
    public int editMinute = 0;

    [FoldoutGroup("时间编辑"), PropertySpace(6), Button("应用时间 (跳转)")]
    public void ApplyEditTime()
    {
        if (TimeSystemManager.Instance == null) return;
        TimeSystemManager.Instance.JumpToPhaseTime(editPhase, editHour, editMinute);
    }

    [FoldoutGroup("时间编辑"), PropertySpace(10), LabelText("滑动选择分钟"), OnValueChanged("OnSliderChanged")]
    [Range(0, 480)]
    public int minuteSlider = 0; // 根据阶段长度动态使用

    private void OnSliderChanged()
    {
        if (TimeSystemManager.Instance == null) return;
        // 根据当前目标阶段，把 slider(单位:分钟) 映射到 HH:MM
        if (TimeSystemManager.Instance.TryGetPhaseInfo(editPhase, out var info))
        {
            int totalMinutes = Mathf.Clamp(minuteSlider, 0, Mathf.FloorToInt((info.durationSeconds * info.timeScale) / 60f));
            int h = info.startHour + totalMinutes / 60;
            int m = info.startMinute + totalMinutes % 60;
            // 规范分钟到 0-59
            while (m >= 60) { m -= 60; h += 1; }
            editHour = Mathf.Clamp(h, 0, 23);
            editMinute = Mathf.Clamp(m, 0, 59);
        }
    }
#endif

    private void Update()
    {
        var mgr = TimeSystemManager.Instance;
        if (mgr == null) return;
        day = mgr.CurrentDay;
        phase = mgr.CurrentPhase.ToString();
        clock = mgr.GameClock != null ? mgr.GameClock.GetTimeString() : "--:--";
        remainingSeconds = mgr.PhaseRemainingTime;
    }

#if ODIN_INSPECTOR
    [FoldoutGroup("时间倍率"), LabelText("当前倍率"), ReadOnly]
#endif
    [SerializeField] private float currentTimeScale = 1f;

#if ODIN_INSPECTOR
    [FoldoutGroup("时间倍率"), Button("0.25x")]
#endif
    public void SetTimeScale025() => SetTimeScale(0.25f);

#if ODIN_INSPECTOR
    [FoldoutGroup("时间倍率"), Button("0.5x")]
#endif
    public void SetTimeScale05() => SetTimeScale(0.5f);

#if ODIN_INSPECTOR
    [FoldoutGroup("时间倍率"), Button("1x")]
#endif
    public void SetTimeScale1() => SetTimeScale(1f);

#if ODIN_INSPECTOR
    [FoldoutGroup("时间倍率"), Button("1.5x")]
#endif
    public void SetTimeScale15() => SetTimeScale(1.5f);

#if ODIN_INSPECTOR
    [FoldoutGroup("时间倍率"), Button("2x")]
#endif
    public void SetTimeScale2() => SetTimeScale(2f);

#if ODIN_INSPECTOR
    [FoldoutGroup("时间倍率"), Button("3x")]
#endif
    public void SetTimeScale3() => SetTimeScale(3f);

#if ODIN_INSPECTOR
    [FoldoutGroup("时间倍率"), Button("10x")]
#endif
    public void SetTimeScale10() => SetTimeScale(10f);

#if ODIN_INSPECTOR
    [FoldoutGroup("时间倍率"), Button("20x")]
#endif
    public void SetTimeScale20() => SetTimeScale(20f);

    private void SetTimeScale(float scale)
    {
        if (TimeSystemManager.Instance == null) return;
        TimeSystemManager.Instance.SetGlobalTimeScale(scale);
        currentTimeScale = scale;
    }
}


