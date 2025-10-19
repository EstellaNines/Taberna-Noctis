using UnityEngine;

namespace TabernaNoctis.Cards
{
    [System.Serializable]
    public struct CardEffects
    {
        public int busy;         // 忙碌
        public int impatient;    // 急躁
        public int bored;        // 烦闷
        public int picky;        // 挑剔
        public int friendly;     // 友好
    }

    public abstract class BaseCardSO : ScriptableObject
    {
        [Header("基础信息")]
        public int id;                   // 物品编号（001起）
        public string nameCN;            // 中文名
        public string nameEN;            // 英文名
        public string category;          // 分类
        [TextArea]
        public string feature;           // 特点/描述

        [Header("数值效果(五向)")]
        public CardEffects effects;      // 五向效果

        [Header("UI")]
        public string uiPath;            // Resources 路径（Cards/...）
        public Sprite uiSpritePreview;   // 预览用（可选缓存）
    }
}


