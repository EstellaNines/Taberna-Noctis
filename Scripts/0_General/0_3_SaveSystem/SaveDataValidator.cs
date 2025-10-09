using System;
using System.Collections.Generic;

public static class SaveDataValidator
{
	public static bool Validate(SaveData data, out List<string> errors)
	{
		errors = new List<string>();
		if (data == null) { errors.Add("SaveData is null"); return false; }

		// 容器非空
		SaveDataFactory.EnsureDefaults(data);

		// 时间
		if (data.clockHour < 0 || data.clockHour > 23) errors.Add($"clockHour out of range: {data.clockHour}");
		if (data.clockMinute < 0 || data.clockMinute > 59) errors.Add($"clockMinute out of range: {data.clockMinute}");
		if (data.phaseRemainingTime < 0) errors.Add($"phaseRemainingTime < 0: {data.phaseRemainingTime}");

		// 天数
		if (data.currentDay < 1) errors.Add($"currentDay < 1: {data.currentDay}");
		if (data.totalDaysCompleted < 0) errors.Add($"totalDaysCompleted < 0: {data.totalDaysCompleted}");
		if (data.totalDaysCompleted >= data.currentDay) errors.Add("totalDaysCompleted >= currentDay");

		// 经济
		if (data.currentMoney < 0) errors.Add("currentMoney < 0");
		if (data.todayIncome < 0) errors.Add("todayIncome < 0");
		if (data.todayExpense < 0) errors.Add("todayExpense < 0");

		// 星级/评分
		if (data.starRating < 0 || data.starRating > 5) errors.Add($"starRating out of range: {data.starRating}");
		if (data.cumulativeScore < 0) errors.Add("cumulativeScore < 0");
		if (data.todayAverageScore < 0) errors.Add("todayAverageScore < 0");

		// 计数
		if (data.totalCustomersServed < 0) errors.Add("totalCustomersServed < 0");
		if (data.todayCustomersServed < 0) errors.Add("todayCustomersServed < 0");
		if (data.totalRecipesCreated < 0) errors.Add("totalRecipesCreated < 0");
		if (data.todayRecipesCreated < 0) errors.Add("todayRecipesCreated < 0");
		if (data.consecutivePerfectDays < 0) errors.Add("consecutivePerfectDays < 0");
		if (data.maxConsecutivePerfectDays < 0) errors.Add("maxConsecutivePerfectDays < 0");

		// 列表/字典
		foreach (var kv in data.inventory)
			if (kv.Value < 0) errors.Add($"inventory negative: {kv.Key}={kv.Value}");
		if (data.todayMenuSelected && (data.currentMenuRecipeIDs == null || data.currentMenuRecipeIDs.Count == 0))
			errors.Add("todayMenuSelected=true but currentMenuRecipeIDs is empty");

		// 元信息
		if (string.IsNullOrEmpty(data.saveSlotID)) errors.Add("saveSlotID is empty");
		if (string.IsNullOrEmpty(data.saveSlotName)) errors.Add("saveSlotName is empty");
		if (data.saveVersion < 1) errors.Add("saveVersion < 1");

		return errors.Count == 0;
	}
}


