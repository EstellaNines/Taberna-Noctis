using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 角色（身份）数据 ScriptableObject：每个身份一份 SO。
/// 注意：不在 SO 内复制台词文本，仅保存台词 JSON 的 Resources 路径与定位用的 tag。
/// - identityId：身份唯一键（如 CompanyEmployee）
/// - identityMultiplier：付费/结算等用到的身份倍率
/// - dialoguesRefCN/EN：台词 JSON 的 Resources 路径（中/英）
/// - dialogueTag：与 identityId 相同，用于从台词 JSON 根节点精准定位该身份段落
/// - npcEntries：从 NPCInfo.json 聚合出的同身份下全部 NPC 条目（含状态/性别/心情/到访占比）
/// </summary>
[CreateAssetMenu(menuName = "TabernaNoctis/Characters/CharacterRoleData", fileName = "CharacterRoleData")]
public sealed class CharacterRoleData : ScriptableObject
{
    [Serializable]
    public sealed class NpcEntry
    {
        /// <summary>
        /// NPC 全局唯一 id（如 CompanyEmployee_001_M）
        /// </summary>
        public string id;
        /// <summary>
        /// 性别："male" 或 "female"
        /// </summary>
        public string gender; // "male" | "female"
        /// <summary>
        /// NPC 展示名（用于调试或本地化前的占位）
        /// </summary>
        public string name;
        /// <summary>
        /// 初始心情值，用于进入场景时的基础心情
        /// </summary>
        public int initialMood;
        /// <summary>
        /// 到访占比（同状态 20% 基线内的个体分配值）；上层会结合每日消息 delta 做二次归一
        /// </summary>
        public float visitPercent;
        /// <summary>
        /// 所属状态：Busy / Irritable / Melancholy / Picky / Friendly
        /// </summary>
        public string state; // Busy / Irritable / Melancholy / Picky / Friendly
    }

    [Header("Identity")]
    /// <summary>
    /// 身份唯一键（如 CompanyEmployee）
    /// </summary>
    public string identityId; // e.g., CompanyEmployee
    /// <summary>
    /// 身份倍率（用于付费/结算等计算）
    /// </summary>
    public float identityMultiplier = 1f;

    [Header("Dialogues Reference (Resources)")]
    /// <summary>
    /// 中文台词 JSON 的 Resources 路径（不含扩展名）
    /// </summary>
    public string dialoguesRefCN = "Character/Dialogues/NPCDialogues";
    /// <summary>
    /// 英文台词 JSON 的 Resources 路径（不含扩展名）
    /// </summary>
    public string dialoguesRefEN = "Character/Dialogues/NPCDialogues_en";
    /// <summary>
    /// 台词定位用标签（等于 identityId），用于 JSON 根节点快速定位到该身份
    /// </summary>
    public string dialogueTag; // equals identityId

    [Header("NPC Entries (Aggregated from NPCInfo.json)")]
    /// <summary>
    /// 该身份下的全部 NPC 条目集合（由生成器从 NPCInfo.json 聚合填充）
    /// </summary>
    public List<NpcEntry> npcEntries = new List<NpcEntry>();
}


