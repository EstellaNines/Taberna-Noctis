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
        public string nameCN;            // 本地化名（当前保留但建议与英文一致）
        public string nameEN;            // 英文名（游戏中统一显示）
        public string category;          // 分类
        [TextArea]
        public string feature;           // 特点/描述

        [Header("标签系统")]
        public string[] tags;            // 标签（基酒、调味酒、调味品、新鲜配料、经典酒、热带酒、特调酒等）

        [Header("颜色基调")]
		public Color themeColor = Color.white;  // 兼容旧版：主背景基色
		[Tooltip("主背景颜色（优先于 themeColor）")]
		public Color mainBackgroundColor = new Color(0,0,0,0);
		[Tooltip("外边框背景颜色")]
		public Color outlineBackgroundColor = new Color(0,0,0,0);
		[Tooltip("较浅的文字区域背景颜色")]
		public Color textBackgroundColor = new Color(0,0,0,0);

        [Header("数值效果(五向)")]
        public CardEffects effects;      // 五向效果

        [Header("UI")]
        public string uiPath;            // Resources 路径（Cards/...）
        public Sprite uiSpritePreview;   // 预览用（可选缓存）
    }
}


