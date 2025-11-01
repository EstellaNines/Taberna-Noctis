# Card System Parameters

## Overview

This document defines the complete parameter data for all cards in Taberna Noctis, including attributes, effects, and economic values for both Material cards and Cocktail cards.

## Feature Summary

This parameter document systematically enumerates all values and relationships of materials and cocktails. It covers the impacts on the five customer states, cost/selling price/profit and reputation, and presents formulas and tables in a structured way. It provides actionable references for design balancing and gameplay decisions.

## Core Design Principles

- **State mapping**: Each material affects the five customer states (Busy, Irritable, Melancholy, Picky, Friendly) differently
- **Value balance**: Positive and negative values are balanced so there is no omnipotent material
- **Economy**: Materials have purchase costs; cocktails have craft costs and selling prices
- **Strategic depth**: Players should choose optimal material combinations based on customer states

## Material Cards (16)

### Base Spirits (6)

| #   | Material Name | English Name | Busy | Irritable | Melancholy | Picky | Friendly | Unit Cost | Notes                                   |
| --- | ------------- | ------------ | ---- | --------- | ---------- | ----- | -------- | --------- | --------------------------------------- |
| 1   | 金酒          | Gin          | 0    | -1        | 0          | +2    | +1       | 25        | Picky/Friendly leaning, classic base    |
| 2   | 伏特加        | Vodka        | -1   | -1        | +1         | 0     | 0        | 20        | Melancholy relief, high value           |
| 3   | 白朗姆        | White Rum    | -1   | 0         | 0          | -1    | +2       | 22        | Friendly specialist, tropical profile   |
| 4   | 龙舌兰        | Tequila      | -1   | -2        | -1         | +1    | 0        | 24        | Picky leaning, bold character           |
| 5   | 波本威士忌    | Bourbon      | +2   | 0         | -1         | -1    | +1       | 30        | Busy/Friendly leaning, American classic |
| 6   | 黑麦威士忌    | Rye Whiskey  | +2   | -1        | 0          | +1    | 0        | 32        | Busy/Picky leaning, premium choice      |

### Flavoring Liqueurs (4)

| #   | Material Name | English Name   | Busy | Irritable | Melancholy | Picky | Friendly | Unit Cost | Notes                                   |
| --- | ------------- | -------------- | ---- | --------- | ---------- | ----- | -------- | --------- | --------------------------------------- |
| 7   | 干味美思      | Dry Vermouth   | 0    | 0         | -1         | +2    | 0        | 18        | Picky specialist, classic modifier      |
| 8   | 甜味美思      | Sweet Vermouth | 0    | +1        | 0          | +2    | +1       | 18        | Versatile, sweet balancing              |
| 9   | 橙皮利口酒    | Orange Liqueur | 0    | -1        | 0          | +1    | +2       | 15        | Friendly leaning, citrus aroma          |
| 10  | 金巴利        | Campari        | 0    | +2        | -1         | +2    | -1       | 20        | Irritable/Picky leaning, bitter profile |

### Bitters & Sweeteners (2)

| #   | Material Name | English Name      | Busy | Irritable | Melancholy | Picky | Friendly | Unit Cost | Notes                                |
| --- | ------------- | ----------------- | ---- | --------- | ---------- | ----- | -------- | --------- | ------------------------------------ |
| 11  | 安高天娜苦精  | Angostura Bitters | +1   | +1        | -1         | +1    | 0        | 5         | Multi-state booster, classic bitters |
| 12  | 单糖浆        | Simple Syrup      | -1   | +1        | +1         | 0     | +1       | 3         | Sweetness control, low cost          |

### Fresh Ingredients (4)

| #   | Material Name | English Name | Busy | Irritable | Melancholy | Picky | Friendly | Unit Cost | Notes                                   |
| --- | ------------- | ------------ | ---- | --------- | ---------- | ----- | -------- | --------- | --------------------------------------- |
| 13  | 青柠          | Lime         | -2   | -1        | +1         | +1    | 0        | 4         | Refreshing relief, sour balance         |
| 14  | 柠檬          | Lemon        | -2   | -1        | 0          | 0     | +1       | 4         | Gentle aroma, friendly boost            |
| 15  | 苏打水        | Soda Water   | -2   | +1        | +2         | 0     | +1       | 2         | Melancholy killer, crisp bubbles        |
| 16  | 可乐          | Cola         | -1   | +1        | +2         | -1    | +2       | 3         | Melancholy/Friendly leaning, sweet soda |

## Cocktail Cards (8)

### Classic Cocktails

| Cocktail      | Chinese Name | Busy | Irritable | Melancholy | Picky | Friendly | Cost | Price | Profit | Reputation | Notes                                           |
| ------------- | ------------ | ---- | --------- | ---------- | ----- | -------- | ---- | ----- | ------ | ---------- | ----------------------------------------------- |
| Martini       | 马天尼       | +1   | -2        | -2         | +4    | +1       | 48   | 72    | 24     | +5         | Picky specialist, clean and timeless classic    |
| Manhattan     | 曼哈顿       | +3   | 0         | -1         | +4    | +1       | 55   | 82    | 27     | +4         | Busy/Picky leaning, urban whiskey heft          |
| Old Fashioned | 古典         | +2   | +2        | -1         | 0     | +2       | 38   | 57    | 19     | +3         | Busy/Irritable/Friendly balance, evergreen      |
| Negroni       | 尼格罗尼     | +1   | +3        | -2         | +5    | 0        | 63   | 94    | 31     | +5         | Irritable/Picky leaning, bitter-sweet benchmark |

### Tropical Styles

| Cocktail    | Chinese Name | Busy | Irritable | Melancholy | Picky | Friendly | Cost | Price | Profit | Reputation | Notes                                            |
| ----------- | ------------ | ---- | --------- | ---------- | ----- | -------- | ---- | ----- | ------ | ---------- | ------------------------------------------------ |
| Margarita   | 玛格丽特     | -4   | -4        | 0          | +3    | +2       | 46   | 69    | 23     | +3         | Picky/Friendly leaning, tart-salty tropical edge |
| Daiquiri    | 戴基里       | -4   | -2        | +1         | +1    | +3       | 29   | 43    | 14     | +2         | Friendly leaning, bright straight-up rum sour    |
| Moscow Mule | 莫斯科骡子   | -5   | -1        | +3         | +1    | +1       | 31   | 46    | 15     | +2         | Melancholy relief, spicy ginger fizz             |
| Cuba Libre  | 自由古巴     | -4   | +1        | +2         | -2    | +4       | 32   | 48    | 16     | +2         | Melancholy/Friendly leaning, sweet cola refresh  |

### Signature Cocktail

| Cocktail    | Chinese Name | Busy | Irritable | Melancholy | Picky | Friendly | Cost | Price | Profit | Reputation | Notes                               |
| ----------- | ------------ | ---- | --------- | ---------- | ----- | -------- | ---- | ----- | ------ | ---------- | ----------------------------------- |
| Unspeakable | 不可描述之物 | -5   | -5        | -5         | -5    | -5       | 120  | 1     | -119   | -10        | High cost, harsh reputation penalty |

## Economy Parameters

### Base Values

| Parameter         | Value | Description                 |
| ----------------- | ----- | --------------------------- |
| Starting Money    | 500   | Funds at game start         |
| Base Customer Fee | 30    | Fixed payment per customer  |
| Tip Multiplier    | 1.2   | Applied when mood delta ≥ 1 |

### Role Multipliers

| Role            | Multiplier | Notes                    |
| --------------- | ---------- | ------------------------ |
| Office Worker   | 1.0        | Baseline                 |
| Team Lead       | 1.2        | Mid purchasing power     |
| Freelancer      | 1.1        | Slightly above baseline  |
| Boss            | 1.5        | Highest purchasing power |
| College Student | 0.9        | Student discount         |

### Settlement Formulas

```
Final Mood = Initial Mood + ΔM (sum of material effects)
Payment = Drink Price + max(0, ΔM × 1.2 × Role Multiplier)
```

Note: If ΔM is negative, total payment is not reduced; there is simply no tip.

### Reputation System

Each cocktail affects the bar's Reputation in addition to affecting customer mood. These are two parallel systems:

**Dual-system synergy:**

1. **Mood System** (impacts current revenue):

   - Material effects change the customer's mood → mood delta ΔM → tip amount
   - Formula: `Tip = max(0, ΔM × 1.2 × Role Multiplier)`
   - Scope: payment for a single customer visit

2. **Reputation System** (impacts long-term growth):
   - Each cocktail has a fixed reputation delta (see the "Reputation" column above)
   - After tasting, the bar's reputation value adds this change
   - Formula: `New Reputation = Current Reputation + Cocktail Reputation Delta`
   - Scope: affects bar rating, recipe unlocks, and customer quality

**Effects of Reputation:**

- Reaching thresholds increases bar rating (1★ → 5★)
- Higher reputation attracts higher-role multiplier customers (Boss, Team Lead)
- Special recipes unlock at specific reputation thresholds
- Very low reputation may cause downgrades or customer loss

**Strategy notes:**

- Classic (Martini +5, Negroni +5): high reputation but higher cost
- Balanced (Old Fashioned +3): steady reputation growth
- Basic (Daiquiri +2): good for daily operations
- Forbidden (Unspeakable -10): severe penalty; use cautiously (easter egg/challenge)

**Settlement flow:**

```
Customer arrives → Select cocktail → Taste
↓
Two systems trigger simultaneously:
1. Mood System: ΔM affects tip → instant revenue
2. Reputation System: reputation delta → accumulates in bar reputation
↓
Customer leaves → Next customer
```

## Initial Mood by Role

| State / Role   | Office Worker | Team Lead | Freelancer | Boss | College Student |
| -------------- | ------------- | --------- | ---------- | ---- | --------------- |
| **Busy**       | 3             | 4         | 5          | 6    | 6               |
| **Irritable**  | 2             | 3         | 4          | 5    | 5               |
| **Melancholy** | 4             | 3         | 3          | 4    | 4               |
| **Picky**      | 4             | 5         | 4          | 6    | 3               |
| **Friendly**   | 7             | 8         | 8          | 8    | 9               |

## Material Effect Analysis

### Best Materials by State

**Busy (need positive):**

- Best: Bourbon (+2), Rye Whiskey (+2)
- Good: Angostura Bitters (+1), Martini (+1), Negroni (+1)

**Irritable (need positive):**

- Best: Campari (+2), Old Fashioned (+2), Negroni (+3)
- Good: Sweet Vermouth (+1), Simple Syrup (+1), Soda Water (+1), Cola (+1)

**Melancholy (need positive):**

- Best: Soda Water (+2), Cola (+2), Cuba Libre (+2)
- Good: Vodka (+1), Simple Syrup (+1), Lime (+1), Daiquiri (+1)

**Picky (need positive):**

- Best: Martini (+4), Manhattan (+4), Negroni (+5)
- Good: Gin (+2), Dry Vermouth (+2), Sweet Vermouth (+2), Campari (+2)

**Friendly (need positive):**

- Best: Cuba Libre (+4)
- Good: White Rum (+2), Orange Liqueur (+2), Old Fashioned (+2), Margarita (+2), Cola (+2)

### Risky Materials (Negative Effects)

**Harmful to Busy customers:**

- Severe: Moscow Mule (-5), Margarita (-4), Daiquiri (-4), Cuba Libre (-4)
- Minor: Vodka (-1), White Rum (-1), Tequila (-1)

**Harmful to Irritable customers:**

- Severe: Margarita (-4), Tequila (-2), Martini (-2), Daiquiri (-2)
- Minor: Gin (-1), Vodka (-1), Rye Whiskey (-1)

## Game Mechanics Parameters

### Time System

| Parameter        | Value   | Description                       |
| ---------------- | ------- | --------------------------------- |
| Open Duration    | 180 s   | Night session (real 3 minutes)    |
| Arrival Interval | 25~35 s | Random interval (original design) |
| Tasting Time     | 10~15 s | Time for customer to taste        |

### Card Interactions

| Parameter        | Description                          |
| ---------------- | ------------------------------------ |
| Ingredient Count | 3 ingredients                        |
| Order Weight     | Adding order affects effect strength |
| Portion Weight   | Portion size affects final values    |

## Effect Formulas

### Mood Delta Calculation

```
Per-material sequence value = Intensity × Order Weight × Portion Weight
Final mood delta ΔM = Σ(all materials' sequence values)
Final Mood = Initial Mood + ΔM
```

### Payment Calculation

```
Base Fee = 30 (fixed)
Tip = max(0, ΔM × 1.2 × Role Multiplier)
Total Payment = Base Fee + Tip
```

Notes:

- Tip applies only when ΔM > 0
- If ΔM ≤ 0, the customer pays only the base fee of 30; no tip
- Role multiplier scales the tip amount

## Strategy Suggestions

### High Revenue Strategies

1. **For Boss (1.5×):**

   - Recommended: Negroni (Picky +5), Manhattan (Busy +3, Picky +4)
   - Expected: base 30 + large ΔM × 1.5 multiplier

2. **For Picky customers:**

   - Best materials: Gin, Dry Vermouth, Sweet Vermouth, Campari
   - Best cocktails: Negroni (+5), Martini (+4), Manhattan (+4)

3. **Safe choices (Friendly customers):**
   - Recommended: Cuba Libre (+4), Daiquiri (+3), Old Fashioned (+2)
   - Low risk, steady revenue

### Risk Management

**Avoid negative combos:**

- Busy + Moscow Mule (-5) = severe mood drop
- Irritable + Margarita (-4) = strong negative impact
- Picky + Cuba Libre (-2) = dissatisfaction

## Balance Analysis

### Material Value Analysis

**High value materials:**

- Soda Water (2): Melancholy +2, Irritable +1, Friendly +1
- Simple Syrup (3): Irritable +1, Melancholy +1, Friendly +1
- Cola (3): Irritable +1, Melancholy +2, Friendly +2

**Specialists:**

- Rye Whiskey (32): Busy +2, Picky +1
- Bourbon (30): Busy +2, Friendly +1
- Campari (20): Irritable +2, Picky +2

### Cocktail Profitability Analysis

**Highest profit**: Negroni (31)

**Best value**: Old Fashioned (19 profit, 38 cost)

**Lowest risk**: Daiquiri (Friendly +3, cost 29)

---

**Document Version**: v1.0  
**Last Updated**: Oct 17, 2025  
**Applicable Version**: Taberna Noctis Dev Build

---

## Development Notes

1. **Data structure**: Create ScriptableObjects for MaterialCard and CocktailCard
2. **Effect calculation**: Implement order and portion weight logic
3. **UI**: Cards should preview effects and show costs
4. **Balance testing**: Regularly verify revenue balance across combinations

This document provides complete numeric references for card system development.
