using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sirenix.OdinInspector;

public class SaveSlotUI : MonoBehaviour
{
	[BoxGroup("基础设置")]
	[LabelText("槽位索引(1..3)")]
	public int slotIndex = 1;

	[BoxGroup("界面引用")]
	[LabelText("标题文本(可选)")]
	public TMP_Text titleText; // 可选：标题/索引，大号“01”
	[BoxGroup("界面引用")]
	[LabelText("详情文本(可选)")]
	public TMP_Text detailText; // 可选：旧版详情行
	[BoxGroup("界面引用")]
	[LabelText("加载按钮")]
	public Button loadButton;
	[BoxGroup("界面引用")]
	[LabelText("加载按钮文本")]
	public TMP_Text loadButtonText;
	[BoxGroup("界面引用")]
	[LabelText("删除按钮")]
	public Button deleteButton;

	[BoxGroup("数值文本(可选)")]
	[LabelText("索引大字")] public TMP_Text indexText;                 // 大号“01”可用此字段替代 titleText
	[BoxGroup("数值文本(可选)")]
	[LabelText("时长值")] public TMP_Text durationValueText;         // Duration 值
	[BoxGroup("数值文本(可选)")]
	[LabelText("天数/次数")] public TMP_Text inGameTimesValueText;      // In-Game Times（一般用 Day）
	[BoxGroup("数值文本(可选)")]
	[LabelText("持有金钱")] public TMP_Text holdingMoneyValueText;     // Holding Money
	[BoxGroup("数值文本(可选)")]
	[LabelText("星级评分")] public TMP_Text starRatingValueText;       // Star Rating

	private bool _isEmpty;

	public event Action<int> OnLoadClicked;
	public event Action<int> OnDeleteClicked;

	[FoldoutGroup("事件")]
	[InfoBox("未绑定引用时将自动跳过对应显示。", InfoMessageType.Info)]
	private void Awake()
	{
		if (loadButton != null) loadButton.onClick.AddListener(() => OnLoadClicked?.Invoke(slotIndex));
		if (deleteButton != null) deleteButton.onClick.AddListener(() => OnDeleteClicked?.Invoke(slotIndex));
	}

	public void Refresh(SaveSlotInfo info)
	{
		_isEmpty = info == null || info.day <= 0;

		// 索引显示（两位数）
		SetTextIfNotNull(indexText, slotIndex.ToString("D2"));
		SetTextIfNotNull(titleText, $"Save {slotIndex}");

		if (_isEmpty)
		{
			// 空槽：数值全部置零
			SetTextIfNotNull(detailText, "Empty");
			SetTextIfNotNull(durationValueText, "0");
			SetTextIfNotNull(inGameTimesValueText, "0");
			SetTextIfNotNull(holdingMoneyValueText, "0");
			SetTextIfNotNull(starRatingValueText, "0");
			SetLoadButtonLabel("Select");
			SetDeleteInteractable(false);
			return;
		}

		// 有档：按需要填充各字段
		var phaseEn = PhaseToEn(info.phase);
		float starRating = info.cumulativeScore / 100f; // 100分/星
		SetTextIfNotNull(detailText, $"Day {info.day} {phaseEn}  Money:{info.money}  Score:{info.cumulativeScore:F0}  Last:{info.lastSaveTime}");
		SetTextIfNotNull(durationValueText, FormatPlayTime(info.playSeconds));
		SetTextIfNotNull(inGameTimesValueText, info.day.ToString());
		SetTextIfNotNull(holdingMoneyValueText, info.money.ToString());
		SetTextIfNotNull(starRatingValueText, $"{starRating:F1}");
		SetLoadButtonLabel("Load");
		SetDeleteInteractable(true);
	}

	private void SetTexts(string title, string detail)
	{
		SetTextIfNotNull(titleText, title);
		SetTextIfNotNull(detailText, detail);
	}

	private static void SetTextIfNotNull(TMP_Text t, string v)
	{
		if (t != null) t.text = v;
	}

	private void SetLoadButtonLabel(string label)
	{
		if (loadButtonText != null) loadButtonText.text = label;
	}

	private void SetDeleteInteractable(bool on)
	{
		if (deleteButton != null) deleteButton.interactable = on;
	}

	private static string PhaseToEn(TimePhase p)
	{
		switch (p)
		{
			case TimePhase.Morning: return "Morning";
			case TimePhase.Afternoon: return "Afternoon";
			case TimePhase.Night: return "Night";
			default: return "";
		}
	}

	private static string FormatPlayTime(double seconds)
	{
		if (seconds < 0) seconds = 0;
		var ts = TimeSpan.FromSeconds(seconds);
		// 如 1h 23m 或 05m
		return ts.Hours > 0 ? $"{ts.Hours}h {ts.Minutes}m" : $"{ts.Minutes:D2}m";
	}
}


