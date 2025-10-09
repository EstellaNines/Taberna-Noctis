using System;
using System.Collections.Generic;
using UnityEngine;


public class SaveManager : MonoBehaviour
{
	public static SaveManager Instance;

	[SerializeField] private int maxSaveSlots = 3;
	private const string SAVE_KEY_PREFIX = "save_slot_";
	private const int CURRENT_SAVE_VERSION = SaveDataFactory.CURRENT_SAVE_VERSION;

	private SaveData _current;
	private string _currentSlotID;

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
		MessageManager.Send(MessageDefine.SAVE_LOADED, slotID);
	}

	public void DeleteSaveSlot(string slotID)
	{
		string key = SAVE_KEY_PREFIX + slotID;
		if (ES3.KeyExists(key)) ES3.DeleteKey(key);
	}

	public List<SaveSlotInfo> GetAllSaveSlots()
	{
        EnsureInstance();
		var list = new List<SaveSlotInfo>();
		for (int i = 1; i <= maxSaveSlots; i++)
		{
			string slot = i.ToString();
			var data = LoadFromFile(slot);
			if (data == null) { list.Add(new SaveSlotInfo { slotId = slot, slotName = $"存档{slot}", day = 0, lastSaveTime = "--" }); continue; }
			list.Add(new SaveSlotInfo
			{
				slotId = slot,
				slotName = data.saveSlotName,
				day = data.currentDay,
				phase = data.currentPhase,
				money = data.currentMoney,
				star = data.starRating,
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
		// 新一天初始化
		snap.currentDay = Math.Max(1, snap.currentDay + 1);
		snap.totalDaysCompleted = Math.Max(0, snap.currentDay - 1);
		snap.currentPhase = TimePhase.Morning;
		snap.daySubPhase = DaySubPhase.MorningStocking;
		snap.clockHour = 8; snap.clockMinute = 0; snap.phaseRemainingTime = 180f;
		snap.todayIncome = 0; snap.todayExpense = 0; snap.todayCustomersServed = 0; snap.todayAverageScore = 0f;
		snap.todayPurchasedItems.Clear(); snap.todayRecipesCreated = 0;
		snap.todayStockingCompleted = false; snap.todayMenuSelected = false; snap.currentMenuRecipeIDs.Clear();
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

        return data;
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


