using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 角色数据只读加载器：
/// - 负责读取 `CharacterRolesIndex`（编辑器下从 Assets 路径载入，运行时可改为 Addressables/预引用）
/// - 提供按身份获取 SO、遍历 NPC 条目、获取台词引用信息、计算到访占比等只读接口
/// 说明：
/// - 台词数据仍存放在 JSON 文件中，本类不复制文本，仅返回引用信息与定位用的 tag
/// - 概率相关：每日“状态基线20% + deltaPercent”，本类仅做简单计算与裁剪，上层做归一化与保存
/// </summary>
public static class CharacterRoleLoader
{
    private static CharacterRolesIndex _index;
    private static readonly Dictionary<string, CharacterRoleData> _idToRole = new Dictionary<string, CharacterRoleData>(StringComparer.Ordinal);

    /// <summary>
    /// 延迟载入索引 SO。编辑器内从 Assets 路径直接加载；运行时可按需切换为 Resources/Addressables。
    /// </summary>
    public static void LoadIndexIfNeeded(string indexAssetPath = "Assets/Scripts/0_ScriptableObject/CharacterRolesIndex.asset")
    {
        if (_index != null) return;
#if UNITY_EDITOR
        _index = UnityEditor.AssetDatabase.LoadAssetAtPath<CharacterRolesIndex>(indexAssetPath);
#else
        // In player, recommend addressables or pre-reference via bootstrapper
        _index = Resources.Load<CharacterRolesIndex>("CharacterRolesIndex");
#endif
        if (_index == null) { Debug.LogWarning("[CharacterRoleLoader] Index not found."); return; }
        _idToRole.Clear();
        for (int i = 0; i < _index.roles.Count; i++)
        {
            var r = _index.roles[i];
            if (r == null || string.IsNullOrEmpty(r.identityId)) continue;
            _idToRole[r.identityId] = r;
        }
    }

    /// <summary>
    /// 获取指定身份的角色 SO（只读引用）
    /// </summary>
    public static CharacterRoleData GetRole(string identityId)
    {
        if (string.IsNullOrEmpty(identityId)) return null;
        LoadIndexIfNeeded();
        _idToRole.TryGetValue(identityId, out var role);
        return role;
    }

    /// <summary>
    /// 遍历 NPC 条目。
    /// - 当 identityId 为空时：遍历所有身份
    /// - 当 state 不为空时：仅返回该状态下的条目
    /// </summary>
    public static IEnumerable<CharacterRoleData.NpcEntry> EnumerateNPCs(string identityId = null, string state = null)
    {
        LoadIndexIfNeeded();
        if (_index == null) yield break;
        if (!string.IsNullOrEmpty(identityId))
        {
            var r = GetRole(identityId);
            if (r == null) yield break;
            for (int i = 0; i < r.npcEntries.Count; i++)
            {
                var e = r.npcEntries[i];
                if (string.IsNullOrEmpty(state) || string.Equals(e.state, state, StringComparison.Ordinal)) yield return e;
            }
            yield break;
        }
        for (int i = 0; i < _index.roles.Count; i++)
        {
            var r = _index.roles[i];
            if (r == null) continue;
            for (int j = 0; j < r.npcEntries.Count; j++)
            {
                var e = r.npcEntries[j];
                if (string.IsNullOrEmpty(state) || string.Equals(e.state, state, StringComparison.Ordinal)) yield return e;
            }
        }
    }

    [Serializable]
    public sealed class DialoguesBlock
    {
        public string roleId;
        public string tag;
        public Dictionary<string, StateBlock> states; // Busy/Irritable/... -> gender arrays
    }

    [Serializable]
    public sealed class StateBlock
    {
        public List<string> Male;
        public List<string> Female;
    }

    /// <summary>
    /// 返回台词引用信息：根据系统语言选择 CN/EN 路径，并校验该身份在台词 JSON 中是否存在。
    /// 注意：此处不解析/返回具体台词数组，保持“台词留在 JSON 中”的设计。
    /// </summary>
    public static DialoguesBlock GetDialogues(string identityId, SystemLanguage locale)
    {
        var role = GetRole(identityId);
        if (role == null) return null;
        string res = (locale == SystemLanguage.ChineseSimplified || locale == SystemLanguage.ChineseTraditional)
            ? role.dialoguesRefCN : role.dialoguesRefEN;
        var ta = Resources.Load<TextAsset>(res);
        if (ta == null) { Debug.LogWarning($"[CharacterRoleLoader] Dialogues not found at {res}"); return null; }

        // naive partial parse: rely on roleId/tag and per-state blocks; for stability use JSON parser into surrogate types
        // Here we use JsonUtility; structure is dynamic so we fallback to a minimal surrogate that carries only roleId/tag.
        // In practice, locate by identityId string and then extract blocks via a lightweight parser.
        try
        {
            // Simple scan to ensure tag exists
            if (!ta.text.Contains("\"" + identityId + "\""))
            {
                Debug.LogWarning($"[CharacterRoleLoader] Tag not found in dialogues JSON: {identityId}");
            }
        }
        catch { }

        // 上层系统通常只需引用信息；具体台词抽取可在更靠近对话播放层实现。
        return new DialoguesBlock { roleId = identityId, tag = role.dialogueTag, states = null };
    }

    /// <summary>
    /// 计算“状态”层级的日到访占比：基线 20% + delta，裁剪到 [0,100]。
    /// 归一化与多身份叠加由上层负责。
    /// </summary>
    public static float ComputeDailyVisitPercent(string roleId, string state, float deltaPercent)
    {
        // base 20% per state + delta, clamp at [0, 100] per identity-state bucket
        float basePercent = 20f;
        float v = basePercent + deltaPercent;
        if (v < 0f) v = 0f; if (v > 100f) v = 100f;
        return v;
    }
}


