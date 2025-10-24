# 鸡尾酒配方合成系统设计文档

## 系统概述

### 核心机制

- **合成规则**：3 张材料卡牌 → 1 张鸡尾酒卡牌
- **材料消耗**：合成后材料卡牌消失，生成对应鸡尾酒
- **配方解锁**：玩家需要发现正确的材料组合
- **品质系统**：不同材料组合影响鸡尾酒品质

### 材料卡牌分类

#### 基酒类 (Base Spirits)

- **Gin** (琴酒) - ID: 16
- **Vodka** (伏特加) - ID: 15
- **White Rum** (白朗姆) - ID: 14
- **Tequila** (龙舌兰) - ID: 13
- **Bourbon** (波本威士忌) - ID: 12
- **Rye Whiskey** (黑麦威士忌) - ID: 11

#### 利口酒类 (Liqueurs)

- **Dry Vermouth** (干味美思) - ID: 10
- **Sweet Vermouth** (甜味美思) - ID: 9
- **Orange Liqueur** (橙皮利口酒) - ID: 8
- **Campari** (金巴利) - ID: 7

#### 调味料类 (Bitters & Syrups)

- **Angostura Bitters** (安格斯图拉苦精) - ID: 6
- **Simple Syrup** (糖浆) - ID: 5

#### 新鲜配料类 (Fresh Ingredients)

- **Lime** (青柠) - ID: 4
- **Lemon** (柠檬) - ID: 3
- **Soda Water** (苏打水) - ID: 2
- **Cola** (可乐) - ID: 1

---

## 鸡尾酒配方设计

### 1. Martini (马天尼)

**配方组合**：

- Gin (琴酒) + Dry Vermouth (干味美思) + Lemon (柠檬)

**成本**: $48 | **售价**: $72 | **利润**: $24
**特点**：挑剔专精，干练纯粹的经典之王
**顾客效果**：busy+1, impatient-2, bored-2, picky+4, friendly+1
**难度**：★★★★☆

---

### 2. Manhattan (曼哈顿)

**配方组合**：

- Rye Whiskey (黑麦威士忌) + Sweet Vermouth (甜味美思) + Angostura Bitters (安格斯图拉苦精)

**成本**: $55 | **售价**: $82 | **利润**: $27
**特点**：忙碌挑剔型，厚重威士忌的都会风
**顾客效果**：busy+3, impatient+0, bored-1, picky+4, friendly+1
**难度**：★★★★☆

---

### 3. Old Fashioned (古典)

**配方组合**：

- Bourbon (波本威士忌) + Simple Syrup (糖浆) + Angostura Bitters (安格斯图拉苦精)

**成本**: $38 | **售价**: $57 | **利润**: $19
**特点**：忙碌急躁友好型，平衡稳健的常青款
**顾客效果**：busy+2, impatient+2, bored-1, picky+0, friendly+2
**难度**：★★★☆☆

---

### 4. Negroni (尼格罗尼)

**配方组合**：

- Gin (琴酒) + Campari (金巴利) + Sweet Vermouth (甜味美思)

**成本**: $63 | **售价**: $94 | **利润**: $31
**特点**：急躁挑剔型，苦甜并陈的风味标杆
**顾客效果**：busy+1, impatient+3, bored-2, picky+5, friendly+0
**难度**：★★★★★

---

### 5. Margarita (玛格丽特)

**配方组合**：

- Tequila (龙舌兰) + Orange Liqueur (橙皮利口酒) + Lime (青柠)

**成本**: $46 | **售价**: $69 | **利润**: $23
**特点**：挑剔友好型，酸咸相拥的热带清锋
**顾客效果**：busy-4, impatient-4, bored+0, picky+3, friendly+2
**难度**：★★★☆☆

---

### 6. Daiquiri (戴基里)

**配方组合**：

- White Rum (白朗姆) + Simple Syrup (糖浆) + Lime (青柠)

**成本**: $29 | **售价**: $43 | **利润**: $14
**特点**：友好型，清爽直给的朗姆酸
**顾客效果**：busy-4, impatient-2, bored+1, picky+1, friendly+3
**难度**：★★☆☆☆

---

### 7. Moscow Mule (莫斯科骡子)

**配方组合**：

- Vodka (伏特加) + Soda Water (苏打水) + Lime (青柠)

**成本**: $31 | **售价**: $46 | **利润**: $15
**特点**：烦闷缓解型，姜味辛口的清凉气泡
**顾客效果**：busy-5, impatient-1, bored+3, picky+1, friendly+1
**难度**：★★☆☆☆

---

### 8. Cuba Libre (自由古巴)

**配方组合**：

- White Rum (白朗姆) + Cola (可乐) + Lime (青柠)

**成本**: $32 | **售价**: $48 | **利润**: $16
**特点**：烦闷友好型，甜汽可乐的畅快配方
**顾客效果**：busy-4, impatient+1, bored+2, picky-2, friendly+4
**难度**：★☆☆☆☆

---

### 9. Unspeakable (不可描述之物) - 保底配方

**配方组合**：

- **任意 3 种材料的错误组合**（除了前 8 种正确配方外的所有组合）

**示例错误组合**：

- Gin + Campari + Angostura Bitters
- Vodka + Bourbon + Tequila
- Cola + Soda Water + Simple Syrup
- Lemon + Lime + Orange Liqueur
- 任何不在正确配方表中的 3 材料组合

**成本**: $120 | **售价**: $1 | **利润**: $-119
**特点**：高成本，降评的禁忌之杯 - 乱合成的惩罚机制
**顾客效果**：全属性-5 (busy-5, impatient-5, bored-5, picky-5, friendly-5)
**难度**：★★★★★ (失败保底)

**设计理念**：

- 鼓励玩家学习正确配方
- 对随意实验的风险警告
- 材料浪费的经济惩罚
- 保证任何 3 材料组合都有结果

---

## 配方总览表

| 鸡尾酒        | 材料 1           | 材料 2             | 材料 3                | 成本 | 售价 | 利润  | 难度  |
| ------------- | ---------------- | ------------------ | --------------------- | ---- | ---- | ----- | ----- |
| Martini       | Gin (16)         | Dry Vermouth (10)  | Lemon (3)             | $48  | $72  | $24   | ★★★★☆ |
| Manhattan     | Rye Whiskey (11) | Sweet Vermouth (9) | Angostura Bitters (6) | $55  | $82  | $27   | ★★★★☆ |
| Old Fashioned | Bourbon (12)     | Simple Syrup (5)   | Angostura Bitters (6) | $38  | $57  | $19   | ★★★☆☆ |
| Negroni       | Gin (16)         | Campari (7)        | Sweet Vermouth (9)    | $63  | $94  | $31   | ★★★★★ |
| Margarita     | Tequila (13)     | Orange Liqueur (8) | Lime (4)              | $46  | $69  | $23   | ★★★☆☆ |
| Daiquiri      | White Rum (14)   | Simple Syrup (5)   | Lime (4)              | $29  | $43  | $14   | ★★☆☆☆ |
| Moscow Mule   | Vodka (15)       | Soda Water (2)     | Lime (4)              | $31  | $46  | $15   | ★★☆☆☆ |
| Cuba Libre    | White Rum (14)   | Cola (1)           | Lime (4)              | $32  | $48  | $16   | ★☆☆☆☆ |
| Unspeakable   | **任意错误组合** | **任意错误组合**   | **任意错误组合**      | $120 | $1   | $-119 | ★★★★★ |

---

## 系统实现建议

### 配方数据结构

```csharp
[System.Serializable]
public class CocktailRecipe
{
    public int recipeId;
    public string cocktailName;
    public List<int> requiredMaterialIds; // 3个材料ID
    public int resultCocktailId;
    public int difficulty; // 1-5星难度
    public string description;
    public bool isUnlocked;
}
```

### 合成验证逻辑

```csharp
public CocktailCardSO CraftCocktail(List<int> selectedMaterials)
{
    // 确保恰好3种材料
    if (selectedMaterials.Count != 3) return null;

    // 检查是否匹配任何正确配方
    foreach(var recipe in validRecipes)
    {
        if (IsExactMatch(selectedMaterials, recipe.requiredMaterialIds))
        {
            return recipe.resultCocktail; // 返回对应鸡尾酒
        }
    }

    // 没有匹配的正确配方，返回"不可描述之物"
    return unspeakableCocktail;
}

private bool IsExactMatch(List<int> materials, List<int> recipe)
{
    if (materials.Count != recipe.Count) return false;

    // 排序后比较，确保材料组合完全匹配
    var sortedMaterials = new List<int>(materials);
    var sortedRecipe = new List<int>(recipe);
    sortedMaterials.Sort();
    sortedRecipe.Sort();

    for (int i = 0; i < sortedMaterials.Count; i++)
    {
        if (sortedMaterials[i] != sortedRecipe[i]) return false;
    }
    return true;
}
```

### 配方发现系统

- **试验模式**：玩家可以尝试任意 3 种材料组合
- **成功提示**：正确组合时显示配方解锁动画
- **失败处理**：错误组合消耗材料但产生"不可描述之物"（巨额亏损）
- **配方书**：记录已解锁的配方供查阅
- **保底机制**：任何 3 材料组合都有结果，避免卡死

### 品质评级

- **完美搭配**：使用推荐材料组合 → 高品质鸡尾酒
- **替代材料**：某些材料可互相替代，但影响品质
- **创新组合**：玩家发现的新组合可能产生特殊效果

---

## 游戏平衡考虑

### 材料稀有度

- **常见**：Soda Water, Cola, Simple Syrup
- **普通**：Lemon, Lime, Angostura Bitters
- **稀有**：各种基酒和利口酒
- **珍贵**：特殊年份或品牌的基酒

### 经济平衡

- 材料成本 vs 鸡尾酒售价
- 合成失败的风险与收益
- 稀有材料的获取难度

### 进度设计

- 简单配方（1-2 星）：新手友好
- 中等配方（3 星）：需要一定经验
- 复杂配方（4-5 星）：高级玩家挑战

---

## 扩展可能性

### 季节性材料

- 春季：樱花糖浆、新鲜薄荷
- 夏季：西瓜汁、椰子水
- 秋季：肉桂糖浆、苹果汁
- 冬季：蜂蜜、热水

### 特殊效果

- **完美调制**：所有材料品质最佳时触发
- **创新奖励**：发现隐藏配方的额外奖励
- **顾客偏好**：不同顾客对特定鸡尾酒有偏好加成

### 设备升级

- **调酒器具**：影响合成成功率
- **冰块质量**：影响鸡尾酒品质
- **装饰材料**：提升视觉效果和售价
