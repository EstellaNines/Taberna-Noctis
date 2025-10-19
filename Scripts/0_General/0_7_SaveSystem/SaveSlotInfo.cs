using System;

[Serializable]
public class SaveSlotInfo
{
	public string slotId;
	public string slotName;
	public int day;
	public TimePhase phase;
	public int money;
	public int star;               // 星级整数部分（0-5）
	public float cumulativeScore;  // 累计评分（用于精确显示）
	public string lastSaveTime;
	public double playSeconds;
}


