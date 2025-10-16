using UnityEngine;

/// <summary>
/// 单个顾客的 ScriptableObject（独立SO）。
/// 目的：用独立SO承载每位顾客的静态数值/画像信息，而不再依赖“类别SO数组”。
/// </summary>
[CreateAssetMenu(menuName = "TN/Characters/NpcCharacterData", fileName = "Npc_XXXXXX")]
public sealed class NpcCharacterData : ScriptableObject
{
    [Header("Identity")]
    public string id;                 // 如 CompanyEmployee_001_M
    public string identityId;         // 如 CompanyEmployee / SmallLeader / Freelancer / Boss / Student
    public float identityMultiplier;  // 来自身份SO的倍率

    [Header("Basic")]
    public string gender;             // "male" | "female"
    public string state;              // Busy / Irritable / Melancholy / Picky / Friendly
    public string displayName;        // 展示名（本地化前可作占位）
    public int initialMood;           // 初始心情
    public float visitPercent;        // 基线占比（来自数据表/JSON），后续会被每日消息进行二次归一

    [Header("Portrait (Resources)")]
    public string portraitPath;       // 如 Character/Portrait/YH2-男大学生（Resources路径，无扩展名）

    [Header("Dialogues Ref (Resources)")]
    public string dialoguesRefCN = "Character/Dialogues/NPCDialogues";
    public string dialoguesRefEN = "Character/Dialogues/NPCDialogues_en";
}


