using System;
using System.Collections.Generic;

[Serializable]
public class RecipeData
{
	public string recipeId;           // 唯一ID
	public string displayName;        // 展示名
	public List<string> materials = new List<string>(); // 材料ID列表
	public int baseCost;              // 成本（用于经济统计）
	public List<string> tags = new List<string>(); // 风味/标签
}


