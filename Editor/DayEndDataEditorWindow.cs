using UnityEngine;
using UnityEditor;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using Sirenix.Utilities;

/// <summary>
/// 结算界面数据编辑器：用于快速修改存档中的当日收入/支出/评价数值，测试结算界面显示
/// </summary>
public class DayEndDataEditorWindow : OdinEditorWindow
{
	[MenuItem("自制工具/测试工具/结算数据编辑器 ⚙")]
	private static void OpenWindow()
	{
		var window = GetWindow<DayEndDataEditorWindow>();
		window.titleContent = new GUIContent("结算数据编辑器", EditorGUIUtility.IconContent("d_Settings").image);
		window.minSize = new Vector2(400, 600);
		window.Show();
	}

	[Title("当前存档信息", bold: true)]
	[InfoBox("选择一个存档槽位后，可以编辑当日数值并实时预览结算界面", InfoMessageType.Info)]
	[ValueDropdown("GetSaveSlots")]
	[LabelText("选择存档槽位")]
	[OnValueChanged("OnSlotChanged")]
	public string selectedSlot = "1";

	[ShowInInspector, ReadOnly, LabelText("当前天数")]
	[PropertySpace(SpaceBefore = 10)]
	private int currentDay => GetCurrentDay();

	[ShowInInspector, ReadOnly, LabelText("当前阶段")]
	private string currentPhase => GetCurrentPhase();

	[ShowInInspector, ReadOnly, LabelText("当前金钱")]
	private int currentMoney => GetCurrentMoney();

	[Title("今日数值编辑", "修改当日增量数值（用于测试结算界面）", bold: true)]
	[BoxGroup("收支数据")]
	[LabelText("今日收入")]
	[PropertyTooltip("今日收入增量（≥0）")]
	[MinValue(0)]
	public int todayIncome = 0;

	[BoxGroup("收支数据")]
	[LabelText("今日支出")]
	[PropertyTooltip("今日支出增量（≥0）")]
	[MinValue(0)]
	public int todayExpense = 0;

	[BoxGroup("收支数据")]
	[ShowInInspector, ReadOnly]
	[LabelText("今日利润")]
	[PropertyTooltip("自动计算：收入 - 支出")]
	private int todayProfit => todayIncome - todayExpense;

	[BoxGroup("评价数据")]
	[PropertySpace(SpaceBefore = 5)]
	[LabelText("今日评价增量")]
	[PropertyTooltip("今日评价变化值（可正可负）")]
	[Range(-100, 200)]
	public float todayReputationChange = 0f;

	[BoxGroup("评价数据")]
	[ShowInInspector, ReadOnly]
	[LabelText("累计评分")]
	[PropertyTooltip("当前累计评价总分")]
	private float cumulativeScore => GetCumulativeScore();

	[BoxGroup("评价数据")]
	[ShowInInspector, ReadOnly]
	[LabelText("预计星级")]
	[PropertyTooltip("基于累计评分的星级（100分/星）")]
	private string predictedStars => $"{(cumulativeScore / 100f):F2} ⭐";

	[Title("操作按钮", bold: true)]
	[HorizontalGroup("Buttons")]
	[Button(ButtonSizes.Large), GUIColor(0.4f, 0.8f, 1f)]
	[PropertyTooltip("从存档加载当前的今日数值")]
	private void 加载当日数值()
	{
		LoadCurrentDayData();
	}

	[HorizontalGroup("Buttons")]
	[Button(ButtonSizes.Large), GUIColor(0.3f, 1f, 0.4f)]
	[PropertyTooltip("将编辑的数值保存到存档")]
	private void 保存到存档()
	{
		SaveToSlot();
	}

	[HorizontalGroup("Buttons2")]
	[Button(ButtonSizes.Large), GUIColor(1f, 0.8f, 0.3f)]
	[PropertyTooltip("打开结算界面预览当前数值")]
	private void 预览结算界面()
	{
		PreviewDayEndScreen();
	}

	[HorizontalGroup("Buttons2")]
	[Button(ButtonSizes.Large), GUIColor(1f, 0.4f, 0.4f)]
	[PropertyTooltip("将所有今日数值重置为0")]
	private void 重置今日数值()
	{
		ResetTodayData();
	}
	
	[HorizontalGroup("Buttons3")]
	[Button(ButtonSizes.Large), GUIColor(0.6f, 0.9f, 1f)]
	[PropertyTooltip("强制刷新已打开的结算界面")]
	private void 刷新结算界面()
	{
		ForceRefreshDayEndScreen();
	}

	[Title("快速测试数据", bold: true)]
	[HorizontalGroup("Quick1")]
	[Button("盈利日 (+350/-120)"), GUIColor(0.5f, 1f, 0.5f)]
	private void QuickProfit()
	{
		todayIncome = 350;
		todayExpense = 120;
		todayReputationChange = 89;
	}

	[HorizontalGroup("Quick1")]
	[Button("亏损日 (+50/-200)"), GUIColor(1f, 0.5f, 0.5f)]
	private void QuickLoss()
	{
		todayIncome = 50;
		todayExpense = 200;
		todayReputationChange = -20;
	}

	[HorizontalGroup("Quick2")]
	[Button("大丰收 (+1000/0)"), GUIColor(1f, 1f, 0.3f)]
	private void QuickBigProfit()
	{
		todayIncome = 1000;
		todayExpense = 0;
		todayReputationChange = 150;
	}

	[HorizontalGroup("Quick2")]
	[Button("糟糕的一天 (+10/-500)"), GUIColor(0.8f, 0.3f, 0.3f)]
	private void QuickBadDay()
	{
		todayIncome = 10;
		todayExpense = 500;
		todayReputationChange = -50;
	}

	[Title("高级功能", bold: true)]
	[Button(ButtonSizes.Medium), GUIColor(0.7f, 0.7f, 1f)]
	[PropertyTooltip("跳过一天并应用当前数值（用于测试多日累计）")]
	private void 模拟跳过一天()
	{
		SimulateNextDay();
	}

	// ========== 数据读取方法 ==========
	private ValueDropdownList<string> GetSaveSlots()
	{
		var list = new ValueDropdownList<string>();
		if (SaveManager.Instance != null)
		{
			var slots = SaveManager.Instance.GetAllSaveSlots();
			foreach (var slot in slots)
			{
				string display = $"存档 {slot.slotId} - Day {slot.day} ({slot.lastSaveTime})";
				list.Add(display, slot.slotId);
			}
		}
		else
		{
			list.Add("存档 1", "1");
			list.Add("存档 2", "2");
			list.Add("存档 3", "3");
		}
		return list;
	}

	private void OnSlotChanged()
	{
		LoadCurrentDayData();
	}

	private int GetCurrentDay()
	{
		if (SaveManager.Instance == null) return 1;
		var data = SaveManager.Instance.GenerateSaveData();
		return data?.currentDay ?? 1;
	}

	private string GetCurrentPhase()
	{
		if (SaveManager.Instance == null) return "N/A";
		var data = SaveManager.Instance.GenerateSaveData();
		return data?.currentPhase.ToString() ?? "N/A";
	}

	private int GetCurrentMoney()
	{
		if (SaveManager.Instance == null) return 0;
		var data = SaveManager.Instance.GenerateSaveData();
		return data?.currentMoney ?? 0;
	}

	private float GetCumulativeScore()
	{
		if (SaveManager.Instance == null) return 0f;
		var data = SaveManager.Instance.GenerateSaveData();
		return (data?.cumulativeScore ?? 0f) + todayReputationChange;
	}

	// ========== 操作方法 ==========
	private void LoadCurrentDayData()
	{
		if (SaveManager.Instance == null)
		{
			EditorUtility.DisplayDialog("错误", "SaveManager 未初始化，请在运行时使用此工具", "确定");
			return;
		}

		SaveManager.Instance.LoadSaveSlot(selectedSlot);
		var data = SaveManager.Instance.GenerateSaveData();

		if (data != null)
		{
			todayIncome = data.todayIncome;
			todayExpense = data.todayExpense;
			todayReputationChange = data.todayReputationChange;
			Debug.Log($"[DayEndDataEditor] 已加载存档 {selectedSlot} 的当日数值");
		}
	}

	private void SaveToSlot()
	{
		if (SaveManager.Instance == null)
		{
			EditorUtility.DisplayDialog("错误", "SaveManager 未初始化，请在运行时使用此工具", "确定");
			return;
		}

		if (!Application.isPlaying)
		{
			EditorUtility.DisplayDialog("提示", "请在播放模式下保存数据", "确定");
			return;
		}

		var data = SaveManager.Instance.GenerateSaveData();
		data.todayIncome = todayIncome;
		data.todayExpense = todayExpense;
		data.todayReputationChange = todayReputationChange;

		SaveManager.Instance.SaveCheckpoint();
		Debug.Log($"[DayEndDataEditor] 已保存今日数值到存档 {selectedSlot}");
		EditorUtility.DisplayDialog("成功", $"今日数值已保存到存档 {selectedSlot}", "确定");
	}

	private void PreviewDayEndScreen()
	{
		if (!Application.isPlaying)
		{
			EditorUtility.DisplayDialog("提示", "请在播放模式下预览结算界面", "确定");
			return;
		}

		var dayEndScreen = FindObjectOfType<DayEndScreen>();
		if (dayEndScreen == null)
		{
			EditorUtility.DisplayDialog("错误", "场景中未找到 DayEndScreen 组件", "确定");
			return;
		}

		// 先保存数值到存档
		var data = SaveManager.Instance.GenerateSaveData();
		data.todayIncome = todayIncome;
		data.todayExpense = todayExpense;
		data.todayReputationChange = todayReputationChange;
		SaveManager.Instance.SaveCheckpoint();

		// 激活结算界面
		dayEndScreen.gameObject.SetActive(true);
		
		// 等待一帧后刷新（确保界面已激活）
		UnityEditor.EditorApplication.delayCall += () =>
		{
			if (dayEndScreen != null)
			{
				// 直接调用设置方法
				float totalScore = data.cumulativeScore + todayReputationChange;
				dayEndScreen.SetDayEndData(todayIncome, todayExpense, todayProfit, todayReputationChange, totalScore);
				Debug.Log($"[DayEndDataEditor] 预览结算界面 - 收入:{todayIncome} 支出:{todayExpense} 利润:{todayProfit} 评价:{todayReputationChange:F1} 总分:{totalScore:F1}");
			}
		};
	}

	private void ResetTodayData()
	{
		if (EditorUtility.DisplayDialog("确认重置", "确定要将所有今日数值重置为 0 吗？", "确定", "取消"))
		{
			todayIncome = 0;
			todayExpense = 0;
			todayReputationChange = 0f;
			Debug.Log("[DayEndDataEditor] 已重置今日数值");
		}
	}
	
	private void ForceRefreshDayEndScreen()
	{
		if (!Application.isPlaying)
		{
			EditorUtility.DisplayDialog("提示", "请在播放模式下使用此功能", "确定");
			return;
		}
		
		var dayEndScreen = FindObjectOfType<DayEndScreen>();
		if (dayEndScreen == null)
		{
			EditorUtility.DisplayDialog("错误", "场景中未找到 DayEndScreen 组件", "确定");
			return;
		}
		
		if (!dayEndScreen.gameObject.activeInHierarchy)
		{
			EditorUtility.DisplayDialog("提示", "结算界面未激活，请先点击\"预览结算界面\"", "确定");
			return;
		}
		
		// 读取当前存档数据
		var data = SaveManager.Instance.GenerateSaveData();
		float totalScore = data.cumulativeScore;
		
		Debug.Log($"[DayEndDataEditor] 强制刷新 - 从存档读取: 收入={data.todayIncome} 支出={data.todayExpense} 评价={data.todayReputationChange:F1} 总分={totalScore:F1}");
		
		// 直接调用设置方法
		int profit = data.todayIncome - data.todayExpense;
		dayEndScreen.SetDayEndData(data.todayIncome, data.todayExpense, profit, data.todayReputationChange, totalScore);
		
		EditorUtility.DisplayDialog("成功", $"已刷新结算界面\n收入: {data.todayIncome}\n支出: {data.todayExpense}\n评价: {data.todayReputationChange:F1}", "确定");
	}

	private void SimulateNextDay()
	{
		if (!Application.isPlaying)
		{
			EditorUtility.DisplayDialog("提示", "请在播放模式下使用此功能", "确定");
			return;
		}

		if (SaveManager.Instance == null) return;

		// 保存当前数值
		SaveToSlot();

		// 模拟推进到下一天
		var data = SaveManager.Instance.GenerateSaveData();
		data.cumulativeScore += todayReputationChange;
		data.totalEarnedMoney += todayIncome;
		data.totalSpentMoney += todayExpense;
		data.currentMoney += (todayIncome - todayExpense);

		// 重置今日数值
		data.todayIncome = 0;
		data.todayExpense = 0;
		data.todayReputationChange = 0f;
		data.currentDay++;

		SaveManager.Instance.SaveCheckpoint();

		// 刷新界面
		LoadCurrentDayData();

		Debug.Log($"[DayEndDataEditor] 已模拟跳到 Day {data.currentDay}");
		EditorUtility.DisplayDialog("成功", $"已模拟跳到 Day {data.currentDay}\n累计评分: {data.cumulativeScore:F1}", "确定");
	}
}

