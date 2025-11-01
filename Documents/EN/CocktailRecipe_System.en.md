# Cocktail Recipe Crafting System Design Document

## Feature Summary

The cocktail recipe system is built on the core matching rule of "3 materials → 1 cocktail", with a guaranteed fallback "Unspeakable" so every combination yields a result. It establishes an economic gradient via cost/price/profit and expresses strategy through the five customer states. This supports player discovery and memorization of recipes and also makes categorized content expansion straightforward. The document provides a full recipe overview and examples to guide implementation and balancing.

## System Overview

### Core Mechanics

- **Synthesis Rule**: 3 material cards → 1 cocktail card
- **Material Consumption**: Materials are consumed; the corresponding cocktail is generated
- **Recipe Unlocking**: Players must discover the correct material combinations
- **Quality System**: Different material combinations affect cocktail quality

### Material Card Categories

#### Base Spirits

- **Gin** (琴酒) - ID: 16
- **Vodka** (伏特加) - ID: 15
- **White Rum** (白朗姆) - ID: 14
- **Tequila** (龙舌兰) - ID: 13
- **Bourbon** (波本威士忌) - ID: 12
- **Rye Whiskey** (黑麦威士忌) - ID: 11

#### Liqueurs

- **Dry Vermouth** (干味美思) - ID: 10
- **Sweet Vermouth** (甜味美思) - ID: 9
- **Orange Liqueur** (橙皮利口酒) - ID: 8
- **Campari** (金巴利) - ID: 7

#### Bitters & Syrups

- **Angostura Bitters** (安格斯图拉苦精) - ID: 6
- **Simple Syrup** (糖浆) - ID: 5

#### Fresh Ingredients

- **Lime** (青柠) - ID: 4
- **Lemon** (柠檬) - ID: 3
- **Soda Water** (苏打水) - ID: 2
- **Cola** (可乐) - ID: 1

---

## Cocktail Recipe Design

### 1. Martini (马天尼)

**Recipe Combination**:

- Gin + Dry Vermouth + Lemon

**Cost**: $48 | **Price**: $72 | **Profit**: $24

**Traits**: Picky specialist, crisp and pure classic

**Customer Effects**: Busy+1, Irritable-2, Melancholy-2, Picky+4, Friendly+1

**Difficulty**: ★★★★☆

---

### 2. Manhattan (曼哈顿)

**Recipe Combination**:

- Rye Whiskey + Sweet Vermouth + Angostura Bitters

**Cost**: $55 | **Price**: $82 | **Profit**: $27

**Traits**: Busy/Picky leaning, urban whiskey character

**Customer Effects**: Busy+3, Irritable+0, Melancholy-1, Picky+4, Friendly+1

**Difficulty**: ★★★★☆

---

### 3. Old Fashioned (古典)

**Recipe Combination**:

- Bourbon + Simple Syrup + Angostura Bitters

**Cost**: $38 | **Price**: $57 | **Profit**: $19

**Traits**: Busy/Irritable/Friendly leaning, balanced evergreen

**Customer Effects**: Busy+2, Irritable+2, Melancholy-1, Picky+0, Friendly+2

**Difficulty**: ★★★☆☆

---

### 4. Negroni (尼格罗尼)

**Recipe Combination**:

- Gin + Campari + Sweet Vermouth

**Cost**: $63 | **Price**: $94 | **Profit**: $31

**Traits**: Irritable/Picky leaning, bitter-sweet benchmark

**Customer Effects**: Busy+1, Irritable+3, Melancholy-2, Picky+5, Friendly+0

**Difficulty**: ★★★★★

---

### 5. Margarita (玛格丽特)

**Recipe Combination**:

- Tequila + Orange Liqueur + Lime

**Cost**: $46 | **Price**: $69 | **Profit**: $23

**Traits**: Picky/Friendly leaning, tart-salty tropical edge

**Customer Effects**: Busy-4, Irritable-4, Melancholy+0, Picky+3, Friendly+2

**Difficulty**: ★★★☆☆

---

### 6. Daiquiri (戴基里)

**Recipe Combination**:

- White Rum + Simple Syrup + Lime

**Cost**: $29 | **Price**: $43 | **Profit**: $14

**Traits**: Friendly leaning, bright straight-up rum sour

**Customer Effects**: Busy-4, Irritable-2, Melancholy+1, Picky+1, Friendly+3

**Difficulty**: ★★☆☆☆

---

### 7. Moscow Mule (莫斯科骡子)

**Recipe Combination**:

- Vodka + Soda Water + Lime

**Cost**: $31 | **Price**: $46 | **Profit**: $15

**Traits**: Melancholy relief, spicy ginger fizz

**Customer Effects**: Busy-5, Irritable-1, Melancholy+3, Picky+1, Friendly+1

**Difficulty**: ★★☆☆☆

---

### 8. Cuba Libre (自由古巴)

**Recipe Combination**:

- White Rum + Cola + Lime

**Cost**: $32 | **Price**: $48 | **Profit**: $16

**Traits**: Melancholy/Friendly leaning, sweet cola refresh

**Customer Effects**: Busy-4, Irritable+1, Melancholy+2, Picky-2, Friendly+4

**Difficulty**: ★☆☆☆☆

---

### 9. Unspeakable (不可描述之物) - Fallback Recipe

**Recipe Combination**:

- Any 3-material incorrect combination (all combinations not in the 8 correct recipes)

**Examples of Incorrect Combinations**:

- Gin + Campari + Angostura Bitters
- Vodka + Bourbon + Tequila
- Cola + Soda Water + Simple Syrup
- Lemon + Lime + Orange Liqueur
- Any 3-material combo not listed as a correct recipe

**Cost**: $120 | **Price**: $1 | **Profit**: $-119

**Traits**: High cost, harsh reputation penalty — punishment for random mixing

**Customer Effects**: All -5 (Busy-5, Irritable-5, Melancholy-5, Picky-5, Friendly-5)

**Difficulty**: ★★★★★ (failure fallback)

**Design Intent**:

- Encourage players to learn correct recipes
- Warn of risks of arbitrary experimentation
- Economic penalty for wasting materials
- Ensure any 3-material combo yields a result

---

## Recipe Overview Table

| Cocktail      | Material 1          | Material 2          | Material 3            | Cost | Price | Profit | Difficulty |
| ------------- | ------------------- | ------------------- | --------------------- | ---- | ----- | ------ | ---------- |
| Martini       | Gin (16)            | Dry Vermouth (10)   | Lemon (3)             | $48  | $72   | $24    | ★★★★☆      |
| Manhattan     | Rye Whiskey (11)    | Sweet Vermouth (9)  | Angostura Bitters (6) | $55  | $82   | $27    | ★★★★☆      |
| Old Fashioned | Bourbon (12)        | Simple Syrup (5)    | Angostura Bitters (6) | $38  | $57   | $19    | ★★★☆☆      |
| Negroni       | Gin (16)            | Campari (7)         | Sweet Vermouth (9)    | $63  | $94   | $31    | ★★★★★      |
| Margarita     | Tequila (13)        | Orange Liqueur (8)  | Lime (4)              | $46  | $69   | $23    | ★★★☆☆      |
| Daiquiri      | White Rum (14)      | Simple Syrup (5)    | Lime (4)              | $29  | $43   | $14    | ★★☆☆☆      |
| Moscow Mule   | Vodka (15)          | Soda Water (2)      | Lime (4)              | $31  | $46   | $15    | ★★☆☆☆      |
| Cuba Libre    | White Rum (14)      | Cola (1)            | Lime (4)              | $32  | $48   | $16    | ★☆☆☆☆      |
| Unspeakable   | Any incorrect combo | Any incorrect combo | Any incorrect combo   | $120 | $1    | $-119  | ★★★★★      |

---

## Implementation Suggestions

### Recipe Data Structure

```csharp
[System.Serializable]
public class CocktailRecipe
{
    public int recipeId;
    public string cocktailName;
    public List<int> requiredMaterialIds; // 3 material IDs
    public int resultCocktailId;
    public int difficulty; // 1-5 stars difficulty
    public string description;
    public bool isUnlocked;
}
```

### Craft Validation Logic

```csharp
public CocktailCardSO CraftCocktail(List<int> selectedMaterials)
{
    // Ensure exactly 3 materials
    if (selectedMaterials.Count != 3) return null;

    // Check if matches any correct recipe
    foreach(var recipe in validRecipes)
    {
        if (IsExactMatch(selectedMaterials, recipe.requiredMaterialIds))
        {
            return recipe.resultCocktail; // Return corresponding cocktail
        }
    }

    // No correct match, return "Unspeakable"
    return unspeakableCocktail;
}

private bool IsExactMatch(List<int> materials, List<int> recipe)
{
    if (materials.Count != recipe.Count) return false;

    // Sort then compare to ensure exact match regardless of order
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

### Recipe Discovery System

- **Experiment Mode**: Players can try any 3-material combination
- **Success Prompt**: Show unlock animation on a correct combination
- **Failure Handling**: Incorrect combos consume materials and produce "Unspeakable" (huge loss)
- **Recipe Book**: Records unlocked recipes for reference
- **Fallback Mechanism**: Any 3-material combo yields a result to avoid dead-ends

### Quality Rating

- **Perfect Match**: Using recommended combos → high-quality cocktail
- **Substitutions**: Some materials can substitute others but affect quality
- **Innovations**: Newly discovered combos may produce special effects

---

## Game Balance Considerations

### Material Rarity

- **Common**: Soda Water, Cola, Simple Syrup
- **Uncommon**: Lemon, Lime, Angostura Bitters
- **Rare**: Various base spirits and liqueurs
- **Precious**: Special vintages or brands of base spirits

### Economic Balance

- Material cost vs cocktail price
- Risk vs reward of failed synthesis
- Acquisition difficulty of rare materials

### Progression Design

- Simple recipes (1–2 stars): Beginner-friendly
- Moderate recipes (3 stars): Require some experience
- Complex recipes (4–5 stars): Challenges for advanced players

---

## Expansion Possibilities

### Seasonal Ingredients

- Spring: Sakura syrup, fresh mint
- Summer: Watermelon juice, coconut water
- Autumn: Cinnamon syrup, apple juice
- Winter: Honey, hot water

### Special Effects

- **Perfect Mix**: Triggers when all materials are of best quality
- **Innovation Reward**: Extra rewards for discovering hidden recipes
- **Customer Preferences**: Different customers have preference bonuses for certain cocktails

### Equipment Upgrades

- **Bar Tools**: Affect synthesis success rate
- **Ice Quality**: Affects cocktail quality
- **Garnishes**: Improve visual appeal and selling price
