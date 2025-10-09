using System;

public static class SaveDataFactory
{
	public const int CURRENT_SAVE_VERSION = 2;

	public static SaveData CreateDefault(string slotId, string slotName)
	{
		var now = DateTime.Now;
		var data = new SaveData
		{
			saveSlotID = string.IsNullOrEmpty(slotId) ? ($"save_slot_{Guid.NewGuid():N}") : slotId,
			saveSlotName = string.IsNullOrEmpty(slotName) ? "Save" : slotName,
			lastSaveDateTime = now.ToString("yyyy-MM-dd HH:mm:ss"),
			totalPlayTimeSeconds = 0,
			saveVersion = CURRENT_SAVE_VERSION,

			currentDay = 1,
			currentPhase = TimePhase.Morning,
			daySubPhase = DaySubPhase.MorningStocking,
			clockHour = 8,
			clockMinute = 0,
			phaseRemainingTime = 180f,

			currentMoney = 500,
			totalEarnedMoney = 0,
			totalSpentMoney = 0,
			todayIncome = 0,
			todayExpense = 0,

			starRating = 0,
			cumulativeScore = 0f,
			totalCustomersServed = 0,
			todayCustomersServed = 0,
			todayAverageScore = 0f,

			totalRecipesCreated = 0,
			todayRecipesCreated = 0,
			todayStockingCompleted = false,
			todayMenuSelected = false,

			tutorialCompleted = false,
			totalDaysCompleted = 0,
			highestStarRatingAchieved = 0,
			consecutivePerfectDays = 0,
			maxConsecutivePerfectDays = 0
		};

		EnsureDefaults(data);
		return data;
	}

	public static void EnsureDefaults(SaveData data)
	{
		if (data == null) return;
		if (data.inventory == null) data.inventory = new System.Collections.Generic.Dictionary<string, int>();
		if (data.unlockedRecipes == null) data.unlockedRecipes = new System.Collections.Generic.List<RecipeData>();
		if (data.currentMenuRecipeIDs == null) data.currentMenuRecipeIDs = new System.Collections.Generic.List<string>();
		if (data.todayPurchasedItems == null) data.todayPurchasedItems = new System.Collections.Generic.List<string>();
		if (data.unlockedAchievements == null) data.unlockedAchievements = new System.Collections.Generic.List<string>();
		if (data.specialRecipes == null) data.specialRecipes = new System.Collections.Generic.List<string>();
		if (data.customerTypeCount == null) data.customerTypeCount = new System.Collections.Generic.Dictionary<string, int>();
	}
}


