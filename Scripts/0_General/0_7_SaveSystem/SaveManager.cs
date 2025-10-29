using System;
using System.Collections.Generic;
using UnityEngine;
using TabernaNoctis.Cards;


public class SaveManager : MonoBehaviour
{
	public static SaveManager Instance;

	[SerializeField] private int maxSaveSlots = 3;
	private const string SAVE_KEY_PREFIX = "save_slot_";
	private const int CURRENT_SAVE_VERSION = SaveDataFactory.CURRENT_SAVE_VERSION;

	private SaveData _current;
	private string _currentSlotID;
	
	// ====== 公开属性 ======
	public string CurrentSlotID => _currentSlotID;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        EnsureInstance();
    }

	private void Awake()
	{
		if (Instance != null && Instance != this) { Destroy(gameObject); return; }
		Instance = this;
		DontDestroyOnLoad(gameObject);
	}

    private static void EnsureInstance()
    {
        if (Instance == null)
        {
            var go = new GameObject("SaveManager");
            Instance = go.AddComponent<SaveManager>();
            DontDestroyOnLoad(go);
        }
    }

    private void OnEnable()
    {
        // 当有合成结果产出时，标记为已记录
        MessageManager.Register<CocktailCardSO>(MessageDefine.CRAFTING_RESULT, OnCraftingResult);
        // 详细：含三材料，便于写入展示所需的名称与图片路径
        MessageManager.Register<(CocktailCardSO cocktail, MaterialCardSO a, MaterialCardSO b, MaterialCardSO c)>(
            "COCKTAIL_CRAFTED_DETAIL", OnCraftedDetail);
    }

    private void OnDisable()
    {
        MessageManager.Remove<CocktailCardSO>(MessageDefine.CRAFTING_RESULT, OnCraftingResult);
        MessageManager.Remove<(CocktailCardSO cocktail, MaterialCardSO a, MaterialCardSO b, MaterialCardSO c)>(
            "COCKTAIL_CRAFTED_DETAIL", OnCraftedDetail);
    }

	// ====== 对外：槽位操作 ======
	public void LoadSaveSlot(string slotID)
	{
        // 先设置当前槽位，避免后续保存写入错误的 Key
        _currentSlotID = slotID;

        var data = LoadFromFile(slotID);
		if (data == null)
		{
			// 新建默认
            data = SaveDataFactory.CreateDefault(slotID, $"Save {slotID}");
            SaveToFile(data);
		}
		_current = data;
		
		// 广播顾客状态（如果存在）
		if (data.nightCustomerState != null)
		{
			MessageManager.Send(MessageDefine.CUSTOMER_STATE_LOADED, data.nightCustomerState);
		}
		
		MessageManager.Send(MessageDefine.SAVE_LOADED, slotID);
	}

	public void DeleteSaveSlot(string slotID)
	{
		string key = SAVE_KEY_PREFIX + slotID;
		string backupKey = key + "_backup";
		if (ES3.KeyExists(key)) { ES3.DeleteKey(key); }
		if (ES3.KeyExists(backupKey)) { ES3.DeleteKey(backupKey); }
		// 若删除的是当前已加载槽位，清空内存状态，避免后续写回
		if (_currentSlotID == slotID)
		{
			_current = null;
			_currentSlotID = null;
		}
	}

	public List<SaveSlotInfo> GetAllSaveSlots()
	{
        EnsureInstance();
		var list = new List<SaveSlotInfo>();
		for (int i = 1; i <= maxSaveSlots; i++)
		{
			string slot = i.ToString();
			var data = LoadFromFile(slot);
			if (data == null) 
			{ 
				list.Add(new SaveSlotInfo 
				{ 
					slotId = slot, 
					slotName = $"存档{slot}", 
					day = 0, 
					lastSaveTime = "--",
					cumulativeScore = 0f
				}); 
				continue; 
			}
			list.Add(new SaveSlotInfo
			{
				slotId = slot,
				slotName = data.saveSlotName,
				day = data.currentDay,
				phase = data.currentPhase,
				money = data.currentMoney,
				star = data.starRating,
				cumulativeScore = data.cumulativeScore,
				lastSaveTime = data.lastSaveDateTime,
				playSeconds = data.totalPlayTimeSeconds
			});
		}
		return list;
	}

	// ====== 三个保存点 ======
	public void SaveBeforeNight()
	{
		EnsureCurrentLoaded();
        var snap = GenerateSaveData();
		// 修正为夜晚前状态
		snap.currentPhase = TimePhase.Night;
		snap.clockHour = 19; snap.clockMinute = 0;
		// 校验
		if (!SaveDataValidator.Validate(snap, out var errors))
		{
			Debug.LogError("[SaveManager] SaveBeforeNight Validate failed:\n" + string.Join("\n", errors));
			return;
		}
		WriteWithMetadata(snap);
	}

	public void SaveAfterNight()
	{
		EnsureCurrentLoaded();
        var snap = GenerateSaveData();
		// 夜晚结束到结算
		snap.clockHour = 3; snap.clockMinute = 0;
		if (!SaveDataValidator.Validate(snap, out var errors))
		{
			Debug.LogError("[SaveManager] SaveAfterNight Validate failed:\n" + string.Join("\n", errors));
			return;
		}
		WriteWithMetadata(snap);
	}

	public void SaveNewDay()
	{
		EnsureCurrentLoaded();
		var snap = GenerateSaveData();
		// 不在存档层做“加一天”，避免与 TimeSystemManager.StartNewDay 的 currentDay++ 重复
		if (!SaveDataValidator.Validate(snap, out var errors))
		{
			Debug.LogError("[SaveManager] SaveNewDay Validate failed:\n" + string.Join("\n", errors));
			return;
		}
		WriteWithMetadata(snap);
	}

	// ====== 快照/写入 ======
	public SaveData GenerateSaveData()
	{
        // 基于当前数据，聚合各系统的实时状态
        var data = _current ?? SaveDataFactory.CreateDefault(_currentSlotID ?? "1", $"Save {_currentSlotID}");
        SaveDataFactory.EnsureDefaults(data);

        var tsm = TimeSystemManager.Instance;
        if (tsm != null)
        {
            data.currentDay = tsm.CurrentDay;
            data.currentPhase = tsm.CurrentPhase;
            data.daySubPhase = tsm.CurrentDaySubPhase;
            if (tsm.GameClock != null)
            {
                data.clockHour = tsm.GameClock.Hour;
                data.clockMinute = tsm.GameClock.Minute;
            }
            data.phaseRemainingTime = tsm.PhaseRemainingTime;
            data.totalPlayTimeSeconds = tsm.TotalPlayTime; // 使用绝对累计时长
            data.totalDaysCompleted = Math.Max(0, data.currentDay - 1);
        }

        // 收集 NightScreen 顾客状态
        if (CustomerSpawnManager.Instance != null)
        {
            data.nightCustomerState = CustomerSpawnManager.Instance.ExportState();
        }
        else if (data.nightCustomerState == null)
        {
            // 如果没有管理器实例且数据为空，创建默认状态
            data.nightCustomerState = TabernaNoctis.CharacterDesign.NightCustomerState.CreateDefault();
        }

        return data;
	}

	// ====== 菜单（当日） ======
	public void UpdateDailyMenu(System.Collections.Generic.List<string> cocktailIdStrings)
	{
		EnsureCurrentLoaded();
		if (_current == null)
		{
			_current = GenerateSaveData();
		}
		if (_current.currentMenuRecipeIDs == null)
		{
			_current.currentMenuRecipeIDs = new System.Collections.Generic.List<string>();
		}
		_current.currentMenuRecipeIDs.Clear();
		if (cocktailIdStrings != null && cocktailIdStrings.Count > 0)
		{
			_current.currentMenuRecipeIDs.AddRange(cocktailIdStrings);
		}
		_current.todayMenuSelected = _current.currentMenuRecipeIDs.Count > 0;
		WriteWithMetadata(_current);
	}

	public void ClearTodayMenu()
	{
		EnsureCurrentLoaded();
		if (_current == null)
		{
			_current = GenerateSaveData();
		}
		if (_current.currentMenuRecipeIDs == null)
		{
			_current.currentMenuRecipeIDs = new System.Collections.Generic.List<string>();
		}
		_current.currentMenuRecipeIDs.Clear();
		_current.todayMenuSelected = false;
		WriteWithMetadata(_current);
	}

	private void WriteWithMetadata(SaveData snap)
	{
		snap.lastSaveDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
		snap.saveVersion = CURRENT_SAVE_VERSION;
        var tsm = TimeSystemManager.Instance;
        if (tsm != null) snap.totalPlayTimeSeconds = tsm.TotalPlayTime;
        // 通知UI：开始保存
        MessageManager.Send(MessageDefine.SAVE_REQUESTED, _currentSlotID);
		SaveToFile(snap);
		_current = snap;
		MessageManager.Send(MessageDefine.SAVE_COMPLETED, _currentSlotID);
	}

    // 即时检查点保存：不更改阶段与时间，仅快照当前状态
    public void SaveCheckpoint()
    {
        EnsureCurrentLoaded();
        var snap = GenerateSaveData();
        if (!SaveDataValidator.Validate(snap, out var errors))
        {
            Debug.LogWarning("[SaveManager] SaveCheckpoint Validate failed:\n" + string.Join("\n", errors));
            // 尽量仍然写盘以保底
        }
        WriteWithMetadata(snap);
    }

    // ====== 经济与采购：应用一次材料购买并立即保存 ======
    public void ApplyPurchase(string itemKey, int price)
    {
        EnsureCurrentLoaded();
        if (_current == null)
        {
            _current = GenerateSaveData();
        }
        if (_current.todayPurchasedItems == null)
        {
            _current.todayPurchasedItems = new List<string>();
        }
        // 记账
        int cost = Mathf.Max(0, price);
        _current.todayPurchasedItems.Add(itemKey ?? string.Empty);
        _current.todayExpense += cost;
        _current.totalSpentMoney += cost;
        _current.currentMoney = Mathf.Max(0, _current.currentMoney - cost);

        // 序列化落盘
        if (!SaveDataValidator.Validate(_current, out var errors))
        {
            Debug.LogWarning("[SaveManager] ApplyPurchase Validate failed:\n" + string.Join("\n", errors));
        }
        WriteWithMetadata(_current);
    }

    // ====== 夜晚经营：应用一次服务结算（收入与评价）并立即保存 ======
    public void ApplyServiceGain(int income, int ratingDelta)
    {
        EnsureCurrentLoaded();
        if (_current == null)
        {
            _current = GenerateSaveData();
        }
        int inc = Mathf.Max(0, income);
        int rep = ratingDelta; // 评价可正可负

        // 金钱相关
        _current.currentMoney = Mathf.Max(0, _current.currentMoney + inc);
        _current.todayIncome = Mathf.Max(0, _current.todayIncome + inc);
        _current.totalEarnedMoney = Mathf.Max(0, _current.totalEarnedMoney + inc);

        // 评价与统计
        _current.cumulativeScore = Mathf.Max(0f, _current.cumulativeScore + rep);
        _current.todayReputationChange += rep;
        _current.totalCustomersServed = Mathf.Max(0, _current.totalCustomersServed + 1);
        _current.todayCustomersServed = Mathf.Max(0, _current.todayCustomersServed + 1);

        // 立即写盘，便于HUD与结算面板实时读取
        if (!SaveDataValidator.Validate(_current, out var errors))
        {
            Debug.LogWarning("[SaveManager] ApplyServiceGain Validate failed:\n" + string.Join("\n", errors));
        }
        WriteWithMetadata(_current);
    }

    // ====== 配方书：记录一次“发现/解锁”的配方（首次做出后才显示在配方书） ======
    public void DiscoverRecipe(string recipeId, string displayName = null)
    {
        if (string.IsNullOrEmpty(recipeId)) return;
        EnsureCurrentLoaded();
        if (_current == null) _current = GenerateSaveData();
        SaveDataFactory.EnsureDefaults(_current);
        // 已存在则忽略
        bool exists = _current.unlockedRecipes.Exists(r => r != null && r.recipeId == recipeId);
        if (!exists)
        {
            var entry = new RecipeData { recipeId = recipeId, displayName = displayName ?? recipeId };
            _current.unlockedRecipes.Add(entry);
            WriteWithMetadata(_current);
        }
    }

    // 使用配方SO作为权威ID（取 SO.id），避免名称变化导致ID漂移
    public void DiscoverRecipe(CocktailCardSO cocktail)
    {
        if (cocktail == null) return;
        var idStr = cocktail.id.ToString();
        EnsureCurrentLoaded();
        if (_current == null) _current = GenerateSaveData();
        SaveDataFactory.EnsureDefaults(_current);
        var list = _current.unlockedRecipes;
        var existing = list.Find(r => r != null && r.recipeId == idStr);
        if (existing != null)
        {
            existing.recorded = true;
            if (string.IsNullOrEmpty(existing.displayName)) existing.displayName = cocktail.nameEN;
            if (string.IsNullOrEmpty(existing.cocktailSpritePath)) existing.cocktailSpritePath = SafeUiPath(cocktail);
        }
        else
        {
            var entry = new RecipeData
            {
                recipeId = idStr,
                displayName = cocktail.nameEN,
                recorded = true,
                orderIndex = list != null ? list.Count : 0,
                cocktailSpritePath = SafeUiPath(cocktail)
            };
            list.Add(entry);
        }
        WriteWithMetadata(_current);
    }

    private void OnCraftedDetail((CocktailCardSO cocktail, MaterialCardSO a, MaterialCardSO b, MaterialCardSO c) payload)
    {
        try { DiscoverRecipeWithMaterials(payload.cocktail, payload.a, payload.b, payload.c); } catch { }
        MessageManager.Send(MessageDefine.RECIPE_BOOK_REFRESH_REQUEST, "");
    }

    private void DiscoverRecipeWithMaterials(CocktailCardSO cocktail, MaterialCardSO a, MaterialCardSO b, MaterialCardSO c)
    {
        if (cocktail == null) return;
        EnsureCurrentLoaded();
        if (_current == null) _current = GenerateSaveData();
        SaveDataFactory.EnsureDefaults(_current);
        var idStr = cocktail.id.ToString();
        var list = _current.unlockedRecipes;
        var entry = list.Find(r => r != null && r.recipeId == idStr);
        if (entry == null)
        {
            entry = new RecipeData
            {
                recipeId = idStr,
                displayName = cocktail.nameEN,
                recorded = true,
                orderIndex = list != null ? list.Count : 0
            };
            list.Add(entry);
        }
        entry.cocktailSpritePath = SafeUiPath(cocktail);
        entry.materials.Clear();
        entry.materialNames.Clear();
        entry.materialSpritePaths.Clear();
        AppendMat(entry, a);
        AppendMat(entry, b);
        AppendMat(entry, c);
        WriteWithMetadata(_current);
    }

    private void AppendMat(RecipeData entry, MaterialCardSO m)
    {
        if (entry == null || m == null) return;
        entry.materials.Add(m.id.ToString());
        entry.materialNames.Add(m.nameEN);
        entry.materialSpritePaths.Add(SafeUiPath(m));
    }

    private string SafeUiPath(BaseCardSO so)
    {
        if (so == null) return string.Empty;
        if (!string.IsNullOrEmpty(so.uiPath)) return so.uiPath;
        return string.Empty;
    }

    private void OnCraftingResult(CocktailCardSO cocktail)
    {
        try { DiscoverRecipe(cocktail); } catch { }
        // 通知UI或管理器刷新配方书
        MessageManager.Send(MessageDefine.RECIPE_BOOK_REFRESH_REQUEST, "");
    }

    // 应用层自动保存（暂停/退出）
    private void OnApplicationPause(bool pause)
    {
        if (pause)
        {
            try { var snap = GenerateSaveData(); WriteWithMetadata(snap); } catch { }
        }
    }

    private void OnApplicationQuit()
    {
        try { var snap = GenerateSaveData(); WriteWithMetadata(snap); } catch { }
    }

	// ====== ES3 读写/升级/备份 ======
	private void SaveToFile(SaveData data)
	{
		string key = SAVE_KEY_PREFIX + (_currentSlotID ?? "1");
		string backupKey = key + "_backup";
		try
		{
			if (ES3.KeyExists(key))
			{
				var old = ES3.Load<SaveData>(key);
				ES3.Save(backupKey, old);
			}
			ES3.Save(key, data);
		}
		catch (Exception e)
		{
			Debug.LogError($"[SaveManager] Save failed: {e.Message}, trying restore backup.");
			if (ES3.KeyExists(backupKey))
			{
				var bak = ES3.Load<SaveData>(backupKey);
				ES3.Save(key, bak);
			}
		}
	}

	private SaveData LoadFromFile(string slotID)
	{
		string key = SAVE_KEY_PREFIX + slotID;
		if (!ES3.KeyExists(key)) return null;
		try
		{
			var data = ES3.Load<SaveData>(key);
			if (data.saveVersion < CURRENT_SAVE_VERSION)
			{
				data = UpgradeSaveData(data);
				ES3.Save(key, data);
			}
			return data;
		}
		catch (Exception e)
		{
			Debug.LogError($"[SaveManager] Load failed: {e.Message}");
			return null;
		}
	}

	private SaveData UpgradeSaveData(SaveData oldData)
	{
		int oldVersion = oldData.saveVersion;
		if (oldVersion < 2)
		{
			// v1 -> v2 补全字段
			oldData.todayIncome = Math.Max(0, oldData.todayIncome);
			oldData.todayExpense = Math.Max(0, oldData.todayExpense);
			oldData.consecutivePerfectDays = Math.Max(0, oldData.consecutivePerfectDays);
			oldData.saveVersion = 2;
		}
		return oldData;
	}

	private void EnsureCurrentLoaded()
	{
		if (_current == null)
		{
			LoadSaveSlot(_currentSlotID ?? "1");
		}
	}
}


