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
    public bool recorded;             // 是否已记录到配方书（合成过一次）
    public int orderIndex;            // 在配方书中的固定序号（用于次日还原位置）
    public string cocktailSpritePath; // 鸡尾酒图片的Resources路径（来自SO.uiPath）
    public List<string> materialNames = new List<string>();      // 材料展示名
    public List<string> materialSpritePaths = new List<string>(); // 材料图片Resources路径
}


