using System.Collections.Generic;
using UnityEngine;

// 存储英文到中文的映射数据
[CreateAssetMenu(fileName = "LocalizationMappingData", menuName = "Localization/映射数据")]
public class LocalizationMappingData : ScriptableObject
{
	// 具体的映射条目列表
	public List<MappingItem> items = new List<MappingItem>();

	// 将列表转换为字典，便于快速检索
	public Dictionary<string, string> ToDictionary()
	{
		var map = new Dictionary<string, string>();
		for (int i = 0; i < items.Count; i++)
		{
			var it = items[i];
			if (string.IsNullOrEmpty(it.english)) continue;

			if (!map.ContainsKey(it.english))
			{
				map.Add(it.english, it.chinese ?? string.Empty);
			}
		}
		return map;
	}

	[System.Serializable]
	public class MappingItem
	{
		// 英文原文
		public string english;
		// 中文翻译
		public string chinese;
	}
}


