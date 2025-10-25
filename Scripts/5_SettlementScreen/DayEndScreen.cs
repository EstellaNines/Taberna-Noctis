using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;
using Sirenix.OdinInspector;
using DG.Tweening;

/// <summary>
/// 结算界面控制器：显示当日收入/支出/利润/评分/星级，并提供"前往新一天"按钮
/// </summary>
public class DayEndScreen : MonoBehaviour
{
	[BoxGroup("日期显示")]
	[LabelText("日期文本(Day X)")] public TMP_Text dayText;
	
	[BoxGroup("日期显示")]
	[LabelText("启用动态日期格式")] 
	[InfoBox("启用后将根据天数显示不同格式，如 Day 1、第10天、Day 100")]
	public bool enableDynamicDayFormat = true;
	
	[BoxGroup("日期显示")]
	[LabelText("日期格式配置")]
	[ShowIf("enableDynamicDayFormat")]
	public DayFormatConfig dayFormatConfig;

	[BoxGroup("左侧标签(LeftToolTip)")]
	[LabelText("收入标签")] public TMP_Text incomeLabel;
	[BoxGroup("左侧标签(LeftToolTip)")]
	[LabelText("支出标签")] public TMP_Text expenseLabel;
	[BoxGroup("左侧标签(LeftToolTip)")]
	[LabelText("利润标签")] public TMP_Text profitLabel;

	[BoxGroup("右侧数值(RightToolTip)")]
	[LabelText("收入数值")] public TMP_Text incomeValue;
	[BoxGroup("右侧数值(RightToolTip)")]
	[LabelText("支出数值")] public TMP_Text expenseValue;
	[BoxGroup("右侧数值(RightToolTip)")]
	[LabelText("利润数值")] public TMP_Text profitValue;

	[BoxGroup("星级评价(StarRating)")]
	[LabelText("星级标题")] public TMP_Text starRatingTitle;

	[BoxGroup("星级评价(StarRateCount)")]
	[LabelText("评分数值(+X.X)")] public TMP_Text starRateCountText;

	[BoxGroup("星星填充(StarLine)")]
	[LabelText("五个星星Image(从左到右)")]
	[InfoBox("星星需设置为 Image.Type=Filled, FillMethod=Horizontal, FillOrigin=Left")]
	[ListDrawerSettings(ShowIndexLabels = true, DraggableItems = false)]
	public Image[] stars = new Image[5];

	[BoxGroup("星星填充(StarLine)")]
	[LabelText("单星所需评分")] public float scorePerStar = 100f;
	
	[BoxGroup("星星填充(StarLine)")]
	[LabelText("星星填充动画时长")] 
	[Range(0.5f, 3f)]
	public float starFillDuration = 1.5f;
	
	[BoxGroup("星星填充(StarLine)")]
	[LabelText("星星填充动画曲线")] 
	public Ease starFillEase = Ease.OutCubic;

	[BoxGroup("按钮")]
	[LabelText("前往新一天按钮")] public Button continueButton;

	[BoxGroup("自动显示")]
	[LabelText("监听DAY_COMPLETED自动显示")] public bool autoShowOnDayCompleted = true;
	
	[BoxGroup("自动显示")]
	[LabelText("场景加载时自动刷新")] 
	[InfoBox("启用后，每次场景激活时自动从存档读取数据并刷新界面")]
	public bool autoRefreshOnEnable = true;

	private UnityAction<int> _onDayCompleted;

	private void Awake()
	{
		_onDayCompleted = OnDayCompleted;
		MessageManager.Register(MessageDefine.DAY_COMPLETED, _onDayCompleted);
		if (continueButton != null) continueButton.onClick.AddListener(OnContinueClicked);
		// 按需求：界面一开始需要显示，只有星星默认隐藏
		HideAllStars();
	}
	
	private void OnEnable()
	{
		// 场景激活时自动刷新数据
		if (autoRefreshOnEnable && Application.isPlaying)
		{
			// 清空当日菜单，准备新一天
			var sm = SaveManager.Instance;
			if (sm != null) sm.ClearTodayMenu();
			// 先结算利润到金钱
			SettleProfitToMoney();
			// 再刷新界面显示
			RefreshFromSystems();
		}
	}

	private void OnDestroy()
	{
		MessageManager.Remove(MessageDefine.DAY_COMPLETED, _onDayCompleted);
		if (continueButton != null) continueButton.onClick.RemoveListener(OnContinueClicked);
		
		// 清理所有星星的 Tween 动画
		if (stars != null)
		{
			foreach (var star in stars)
			{
				if (star != null) star.DOKill();
			}
		}
	}

	private void OnDayCompleted(int day)
	{
		if (!autoShowOnDayCompleted) return;
		// 收到 DAY_COMPLETED 消息后显示界面并刷新数据
		gameObject.SetActive(true);
		RefreshFromSystems();
	}

	// ========== 预留接口：外部系统注入结算数据 ==========
	/// <summary>
	/// 设置结算数据并刷新界面（显示当日增量）
	/// </summary>
	/// <param name="todayIncome">今日收入增量</param>
	/// <param name="todayExpense">今日支出增量</param>
	/// <param name="todayProfit">今日利润增量</param>
	/// <param name="todayReputationChange">今日评价增量（可正可负）</param>
	/// <param name="totalScore">累计评价值（用于计算星星填充）</param>
	public void SetDayEndData(int todayIncome, int todayExpense, int todayProfit, float todayReputationChange, float totalScore)
	{
		Debug.Log($"[DayEndScreen] SetDayEndData 被调用 - 收入:{todayIncome} 支出:{todayExpense} 利润:{todayProfit} 评价:{todayReputationChange:F1} 总分:{totalScore:F1}");
		
		// 日期 - 动态格式化
		var tsm = TimeSystemManager.Instance;
		int day = tsm != null ? tsm.CurrentDay : 1;
		string dayString = GetFormattedDayString(day);
		SetTextIfNotNull(dayText, dayString);

		// 左侧标签（可选，若需本地化可改为 Key）
		SetTextIfNotNull(incomeLabel, "Income");
		SetTextIfNotNull(expenseLabel, "Expense");
		SetTextIfNotNull(profitLabel, "Profit");

		// 右侧数值 - 显示增量（带符号）
		string incomeStr = FormatIncremental(todayIncome);
		string expenseStr = FormatIncremental(todayExpense);
		string profitStr = FormatIncremental(todayProfit);
		
		SetTextIfNotNull(incomeValue, incomeStr);
		SetTextIfNotNull(expenseValue, expenseStr);
		SetTextIfNotNull(profitValue, profitStr);
		
		Debug.Log($"[DayEndScreen] 数值文本设置 - Income:{incomeStr} Expense:{expenseStr} Profit:{profitStr}");

		// 星级标题与评分数值 - 显示今日增量
		SetTextIfNotNull(starRatingTitle, "Star Rating");
		string reputationStr = FormatIncremental(todayReputationChange);
		SetTextIfNotNull(starRateCountText, reputationStr);
		
		Debug.Log($"[DayEndScreen] 评价文本设置 - {reputationStr}");

		// 星星填充动画 - 基于累计评分
		UpdateStarsAnimated(totalScore);
	}
	
	/// <summary>
	/// 格式化增量数值（带正负号）
	/// </summary>
	private string FormatIncremental(float value)
	{
		if (value > 0) return $"+{value:F0}";
		if (value < 0) return $"{value:F0}";
		return "0";
	}

	/// <summary>
	/// 结算当日利润到持有金钱，并将评价增量累加到总评分
	/// </summary>
	private void SettleProfitToMoney()
	{
		var sm = SaveManager.Instance;
		if (sm == null) return;
		
		var snap = sm.GenerateSaveData();
		
		// 结算到金钱：材料在购买时已即时扣除（ApplyPurchase），
		// 因此此处只需把“今日收入”加回，避免对“今日支出”二次扣减。
		snap.currentMoney += snap.todayIncome;
		
		// 结算评价增量到累计评分
		snap.cumulativeScore += snap.todayReputationChange;
		
		// 计算星级（100分/星）
		snap.starRating = Mathf.FloorToInt(snap.cumulativeScore / 100f);
		
		int profit = snap.todayIncome - snap.todayExpense;
		Debug.Log($"[DayEndScreen] 结算完成 - 利润:{profit} 新金钱:{snap.currentMoney} 累计评分:{snap.cumulativeScore:F1} 星级:{snap.starRating}");
		
		// 保存到存档
		sm.SaveCheckpoint();
	}
	
	/// <summary>
	/// 从保存系统聚合当日增量数据并刷新界面
	/// </summary>
	private void RefreshFromSystems()
	{
		var sm = SaveManager.Instance;
		if (sm != null)
		{
			var snap = sm.GenerateSaveData();
			
			// 读取当日增量（而非累计值）
			int todayIncome = snap.todayIncome;         // 今日收入增量
			int todayExpense = snap.todayExpense;       // 今日支出增量
			int todayProfit = todayIncome - todayExpense;
			float todayReputation = snap.todayReputationChange;  // 今日评价增量
			
			// 累计评价值 = 历史累计 + 今日增量（用于计算星星填充）
			float totalScore = snap.cumulativeScore + snap.todayReputationChange;
			
			Debug.Log($"[DayEndScreen] RefreshFromSystems - 历史累计:{snap.cumulativeScore:F1} 今日增量:{snap.todayReputationChange:F1} 总分:{totalScore:F1}");
			
			SetDayEndData(todayIncome, todayExpense, todayProfit, todayReputation, totalScore);
		}
		else
		{
			// 兜底：无管理器时显示占位
			SetDayEndData(0, 0, 0, 0f, 0f);
		}
	}

	/// <summary>
	/// 星星填充逻辑：整数部分激活星星，小数部分控制最后一颗的 fillAmount
	/// 例如 1.5 星：第1颗 fillAmount=1 显示，第2颗 fillAmount=0.5 显示，其余隐藏
	/// </summary>
	private void UpdateStars(float rating)
	{
		if (stars == null || stars.Length == 0) return;
		int full = Mathf.FloorToInt(rating);
		float frac = rating - full;
		for (int i = 0; i < stars.Length; i++)
		{
			if (stars[i] == null) continue;
			if (rating <= 0f)
			{
				stars[i].fillAmount = 0f;
				stars[i].enabled = false;
			}
			else if (i < full)
			{
				// 完全显示
				stars[i].fillAmount = 1f;
				stars[i].enabled = true;
			}
			else if (i == full)
			{
				// 部分填充
				stars[i].fillAmount = frac;
				stars[i].enabled = frac > 0f;
			}
			else
			{
				// 隐藏
				stars[i].fillAmount = 0f;
				stars[i].enabled = false;
			}
		}
	}

	/// <summary>
	/// 带动画的星星填充：根据累计评分计算星级并平滑填充
	/// 例如：totalScore=189, scorePerStar=100 -> 1.89 星 -> 第1颗满，第2颗填充89%
	/// </summary>
	public void UpdateStarsAnimated(float totalScore)
	{
		if (scorePerStar <= 0f) 
		{ 
			Debug.LogWarning("[DayEndScreen] scorePerStar <= 0, 无法计算星级");
			HideAllStars(); 
			return; 
		}
		
		if (stars == null || stars.Length == 0)
		{
			Debug.LogWarning("[DayEndScreen] 星星数组未配置");
			return;
		}
		
		// 计算目标星级
		float targetRating = Mathf.Clamp(totalScore / scorePerStar, 0f, stars.Length);
		
		Debug.Log($"[DayEndScreen] 星星填充动画 - 总分:{totalScore:F1} 单星分数:{scorePerStar} 目标星级:{targetRating:F2}");
		
		// 先隐藏所有星星，准备动画
		HideAllStars();
		
		// 计算整数星和小数星
		int fullStars = Mathf.FloorToInt(targetRating);
		float partialFill = targetRating - fullStars;
		
		Debug.Log($"[DayEndScreen] 完整星星:{fullStars} 部分填充:{partialFill:F2}");
		
		// 依次动画显示每颗星星
		for (int i = 0; i < stars.Length; i++)
		{
			if (stars[i] == null) 
			{
				Debug.LogWarning($"[DayEndScreen] 星星[{i}]未配置");
				continue;
			}
			
			if (i < fullStars)
			{
				// 完整星星：从0到1的填充动画
				AnimateStar(stars[i], 1f, i * 0.2f);
			}
			else if (i == fullStars && partialFill > 0f)
			{
				// 部分星星：从0到目标百分比的填充动画
				AnimateStar(stars[i], partialFill, i * 0.2f);
			}
		}
	}
	
	/// <summary>
	/// 单颗星星的填充动画
	/// </summary>
	private void AnimateStar(Image star, float targetFill, float delay)
	{
		star.enabled = true;
		star.fillAmount = 0f;
		
		// 使用 DOTween 实现平滑填充动画
		star.DOFillAmount(targetFill, starFillDuration)
			.SetDelay(delay)
			.SetEase(starFillEase);
	}
	
	/// <summary>
	/// 根据"当前评分 / 单星所需评分"的百分比来填充星星（连续填充，非离散）
	/// 例如 scorePerStar=100，currentScore=150 -> 等价于 1.5 星
	/// </summary>
	public void UpdateStarsByScore(float currentScore)
	{
		if (scorePerStar <= 0f) { HideAllStars(); return; }
		float rating = Mathf.Clamp(currentScore / scorePerStar, 0f, stars != null ? stars.Length : 5);
		UpdateStars(rating);
	}

	private void HideAllStars()
	{
		if (stars == null) return;
		for (int i = 0; i < stars.Length; i++)
		{
			if (stars[i] == null) continue;
			stars[i].fillAmount = 0f;
			stars[i].enabled = false;
		}
	}

	public void OnContinueClicked()
	{
		// 1. 重置今日数值（准备新的一天）
		ResetTodayData();
		
		// 2. 时间系统推进到新一天（内部已包含自动保存流程）
		if (TimeSystemManager.Instance != null)
		{
			TimeSystemManager.Instance.StartNewDay();
		}
		
		// 3. 关闭结算界面（或通过场景切换回 DayScene）
		gameObject.SetActive(false);
		// 可选：通过 GlobalSceneManager 切换到 DayScene
		// GlobalSceneManager.LoadWithLoadingScreen("DayScene", UnityEngine.SceneManagement.LoadSceneMode.Single);
	}
	
	/// <summary>
	/// 重置今日数值（准备新的一天）
	/// </summary>
	private void ResetTodayData()
	{
		var sm = SaveManager.Instance;
		if (sm == null) return;
		
		var snap = sm.GenerateSaveData();
		snap.todayIncome = 0;
		snap.todayExpense = 0;
		snap.todayReputationChange = 0f;
		snap.todayCustomersServed = 0;
		
		Debug.Log("[DayEndScreen] 今日数值已重置，准备新的一天");
		
		sm.SaveCheckpoint();
	}

	private static void SetTextIfNotNull(TMP_Text t, string v)
	{
		if (t != null) t.text = v;
	}

	/// <summary>
	/// 根据天数获取格式化的日期字符串
	/// </summary>
	private string GetFormattedDayString(int day)
	{
		if (!enableDynamicDayFormat || dayFormatConfig == null)
		{
			return $"Day {day}";
		}

		return dayFormatConfig.GetFormattedDay(day);
	}
}

/// <summary>
/// 日期格式化配置类：定义不同天数范围的显示格式
/// </summary>
[System.Serializable]
public class DayFormatConfig
{
	[LabelText("格式规则列表")]
	[InfoBox("按天数范围定义显示格式，优先级从上到下")]
	[ListDrawerSettings(ShowIndexLabels = true, DraggableItems = true)]
	public DayFormatRule[] rules = new DayFormatRule[]
	{
		new DayFormatRule { dayMin = 1, dayMax = 999, format = "Day {0}", showMilestone = false },
	};

	[LabelText("默认格式（无匹配规则时使用）")]
	public string defaultFormat = "Day {0}";

	[LabelText("启用周末标识")]
	[InfoBox("每7天显示为周末")]
	public bool enableWeekendMarker = false;

	[LabelText("周末后缀")]
	[ShowIf("enableWeekendMarker")]
	public string weekendSuffix = " (Weekend)";

	/// <summary>
	/// 获取格式化后的日期字符串
	/// </summary>
	public string GetFormattedDay(int day)
	{
		string baseText = defaultFormat;
		bool isMilestone = false;
		string milestoneText = "";

		// 查找匹配的规则
		if (rules != null)
		{
			foreach (var rule in rules)
			{
				if (day >= rule.dayMin && day <= rule.dayMax)
				{
					baseText = rule.format;
					isMilestone = rule.showMilestone;
					milestoneText = rule.milestoneText;
					break;
				}
			}
		}

		// 格式化天数
		string result = string.Format(baseText, day);

		// 添加里程碑文本
		if (isMilestone && !string.IsNullOrEmpty(milestoneText))
		{
			result = $"{result}\n<size=70%>{milestoneText}</size>";
		}

		// 添加周末标识
		if (enableWeekendMarker && day % 7 == 0)
		{
			result += weekendSuffix;
		}

		return result;
	}
}

/// <summary>
/// 单条日期格式规则
/// </summary>
[System.Serializable]
public class DayFormatRule
{
	[LabelText("最小天数"), HorizontalGroup("Range")]
	public int dayMin = 1;

	[LabelText("最大天数"), HorizontalGroup("Range")]
	public int dayMax = 1;

	[LabelText("格式字符串")]
	[InfoBox("使用 {0} 作为天数占位符，例如：Day {0}、第 {0} 天")]
	public string format = "Day {0}";

	[LabelText("显示里程碑")]
	public bool showMilestone = false;

	[LabelText("里程碑文本")]
	[ShowIf("showMilestone")]
	[InfoBox("支持富文本标签，会显示在日期下方")]
	public string milestoneText = "";
}
