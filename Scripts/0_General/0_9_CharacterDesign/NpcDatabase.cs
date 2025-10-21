using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 全部顾客的索引SO：仅保存顾客SO的引用集合，便于运行时快速检索。
/// </summary>
[CreateAssetMenu(menuName = "TN/Characters/NpcDatabase", fileName = "NpcDatabase")]
public sealed class NpcDatabase : ScriptableObject
{
    [Header("Meta")] public int version = 1;
    [Header("All NPCs")] public List<NpcCharacterData> allNpcs = new List<NpcCharacterData>();
}


