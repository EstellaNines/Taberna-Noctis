using System;
using System.Collections.Generic;
using TabernaNoctis.CharacterDesign;

[Serializable]
public class SaveData
{
	// ===== 元信息 =====
	public string saveSlotID;              // 存档槽唯一ID（如 "save_slot_1"）
	public string saveSlotName;            // 存档槽显示名称（如 "存档1"）
	public string lastSaveDateTime;        // 最后保存时间（"yyyy-MM-dd HH:mm:ss"）
	public double totalPlayTimeSeconds;    // 累计真实游玩时长（秒）
	public int saveVersion = 1;            // 存档版本号（兼容性检查）

	// ===== 时间线核心 =====
	public int currentDay = 1;             // 当前第几天（≥1）
	public TimePhase currentPhase;         // 当前时段
	public DaySubPhase daySubPhase;        // 白天子阶段
	public int clockHour;                  // 游戏时钟-时（0-23）
	public int clockMinute;                // 游戏时钟-分（0-59）
	public float phaseRemainingTime;       // 阶段剩余时间（秒）

	// ===== 经济数据 =====
	public int currentMoney;               // 当前持有金币（≥0）
	public int totalEarnedMoney;           // 累计赚取
	public int totalSpentMoney;            // 累计花费
	public int todayIncome;                // 今日收入
	public int todayExpense;               // 今日支出

	// ===== 评价系统 =====
	public int starRating;                 // 当前星级（0-5）
	public float cumulativeScore;          // 累计评分总和（≥0）
	public int totalCustomersServed;       // 累计服务顾客数（≥0）
	public int todayCustomersServed;       // 今日服务顾客数（≥0）
	public float todayAverageScore;        // 今日平均评分（≥0）
	public float todayReputationChange;    // 今日评价增量（可正可负）

	// ===== 库存/配方 =====
	public Dictionary<string, int> inventory = new Dictionary<string, int>();
	public List<RecipeData> unlockedRecipes = new List<RecipeData>();   // 已解锁配方
	public List<string> currentMenuRecipeIDs = new List<string>();      // 当前上架菜单（配方ID）
	public int totalRecipesCreated;                                     // 累计研发配方数（≥0）

	// ===== 当日临时数据（每天重置） =====
	public List<string> todayPurchasedItems = new List<string>();       // 今日购买材料
	public int todayRecipesCreated;                                     // 今日研发数量
	public bool todayStockingCompleted;                                 // 进货完成
	public bool todayMenuSelected;                                      // 菜单上架完成

	// ===== 进度/成就 =====
	public bool tutorialCompleted;             // 教学完成
	public int totalDaysCompleted;             // 已完成天数（< currentDay）
	public int highestStarRatingAchieved;      // 历史最高星级
	public List<string> unlockedAchievements = new List<string>();
	public List<string> specialRecipes = new List<string>();

	// ===== 统计数据 =====
	public Dictionary<string, int> customerTypeCount = new Dictionary<string, int>();
	public int consecutivePerfectDays;         // 连续满分天数（≥0）
	public int maxConsecutivePerfectDays;      // 历史最长连胜（≥0）
	
	// ===== 顾客到访系统 =====
	public NightCustomerState nightCustomerState;
}


