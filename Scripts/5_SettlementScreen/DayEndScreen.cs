using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;
using Sirenix.OdinInspector;

/// <summary>
/// 结算界面控制器：显示当日收入/支出/利润/评分/星级，并提供"前往新一天"按钮
/// </summary>
public class DayEndScreen : MonoBehaviour
{
	[BoxGroup("日期显示")]
	[LabelText("日期文本(Day X)")] public TMP_Text dayText;

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

	[BoxGroup("按钮")]
	[LabelText("前往新一天按钮")] public Button continueButton;

	[BoxGroup("自动显示")]
	[LabelText("监听DAY_COMPLETED自动显示")] public bool autoShowOnDayCompleted = true;

	private UnityAction<int> _onDayCompleted;

	private void Awake()
	{
		_onDayCompleted = OnDayCompleted;
		MessageManager.Register(MessageDefine.DAY_COMPLETED, _onDayCompleted);
		if (continueButton != null) continueButton.onClick.AddListener(OnContinueClicked);
		// 按需求：界面一开始需要显示，只有星星默认隐藏
		HideAllStars();
	}

	private void OnDestroy()
	{
		MessageManager.Remove(MessageDefine.DAY_COMPLETED, _onDayCompleted);
		if (continueButton != null) continueButton.onClick.RemoveListener(OnContinueClicked);
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
	/// 设置结算数据并刷新界面
	/// </summary>
	/// <param name="income">今日收入</param>
	/// <param name="expense">今日支出</param>
	/// <param name="profit">今日利润</param>
	/// <param name="score">今日增加评分</param>
	/// <param name="starRating">当前星级评价（可带小数，如1.5）</param>
	public void SetDayEndData(int income, int expense, int profit, float score, float starRating)
	{
		// 日期
		var tsm = TimeSystemManager.Instance;
		int day = tsm != null ? tsm.CurrentDay : 1;
		SetTextIfNotNull(dayText, $"Day {day}");

		// 左侧标签（可选，若需本地化可改为 Key）
		SetTextIfNotNull(incomeLabel, "Income");
		SetTextIfNotNull(expenseLabel, "Expense");
		SetTextIfNotNull(profitLabel, "Profit");

		// 右侧数值
		SetTextIfNotNull(incomeValue, income.ToString());
		SetTextIfNotNull(expenseValue, expense.ToString());
		SetTextIfNotNull(profitValue, profit.ToString());

		// 星级标题与评分数值
		SetTextIfNotNull(starRatingTitle, "Star Rating");
		SetTextIfNotNull(starRateCountText, $"+{score:F1}");

		// 星星填充
		UpdateStars(starRating);
	}

	/// <summary>
	/// 从各系统聚合数据并刷新界面（当前为占位，待经济/评分系统实现后替换）
	/// </summary>
	private void RefreshFromSystems()
	{
		// TODO: 从经济管理器、评分管理器获取实际数据
		// 当前占位：从 SaveManager 读取今日临时数据
		var sm = SaveManager.Instance;
		if (sm != null)
		{
			var snap = sm.GenerateSaveData();
			int income = snap.todayIncome;
			int expense = snap.todayExpense;
			int profit = income - expense;
			float score = snap.todayAverageScore; // 占位：实际应从评分系统获取今日增量
			float starRating = snap.starRating;
			SetDayEndData(income, expense, profit, score, starRating);
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
	/// 根据“当前评分 / 单星所需评分”的百分比来填充星星（连续填充，非离散）
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
		// 1. 自动保存（新一天）
		if (SaveManager.Instance != null)
		{
			try { SaveManager.Instance.SaveNewDay(); } catch { }
		}
		// 2. 时间系统推进到新一天
		if (TimeSystemManager.Instance != null)
		{
			TimeSystemManager.Instance.StartNewDay();
		}
		// 3. 关闭结算界面（或通过场景切换回 DayScene）
		gameObject.SetActive(false);
		// 可选：通过 GlobalSceneManager 切换到 DayScene
		// GlobalSceneManager.LoadWithLoadingScreen("DayScene", UnityEngine.SceneManagement.LoadSceneMode.Single);
	}

	private static void SetTextIfNotNull(TMP_Text t, string v)
	{
		if (t != null) t.text = v;
	}
}
