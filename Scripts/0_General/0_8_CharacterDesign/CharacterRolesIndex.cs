using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 角色 SO 总索引：集中管理所有身份对应的 SO。
/// - version：与 NPCInfo.json 的版本号保持一致，便于数据演进
/// - tipMultiplier：全局小费倍率（来自 NPCInfo.json）
/// - basePaymentDefault：全局基础付费默认值（来自 NPCInfo.json）
/// - roles：项目内所有身份的 `CharacterRoleData` 列表
/// 用途：运行时 Loader 可通过该索引快速定位身份 SO；编辑器生成器负责维护该资产内容。
/// </summary>
[CreateAssetMenu(menuName = "TabernaNoctis/Characters/CharacterRolesIndex", fileName = "CharacterRolesIndex")]
public sealed class CharacterRolesIndex : ScriptableObject
{
    /// <summary>
    /// 数据版本（与 NPCInfo.json 的 version 对齐）
    /// </summary>
    public int version = 1;
    /// <summary>
    /// 全局小费倍率（用于结算系数）
    /// </summary>
    public float tipMultiplier = 1f;
    /// <summary>
    /// 默认基础付费（用于缺省基线）
    /// </summary>
    public int basePaymentDefault = 0;
    /// <summary>
    /// 全部身份的 SO 引用集合
    /// </summary>
    public List<CharacterRoleData> roles = new List<CharacterRoleData>();
}


