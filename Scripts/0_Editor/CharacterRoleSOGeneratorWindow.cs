using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 角色 SO 生成器（编辑器窗口）
/// 菜单：自制工具/人物设计/角色SO生成器
/// 功能：
/// 1) 从 Resources 载入 NPCInfo.json（聚合身份倍率与 NPC 列表）
/// 2) 为每个身份生成/更新 `CharacterRoleData` 资产（不复制台词，仅写入 JSON 引用与 tag）
/// 3) 生成/更新 `CharacterRolesIndex` 索引资产，统一管理所有身份 SO
/// 4) 软校验台词 JSON 是否存在对应身份标签，并输出 Warning（不中断）
/// 设计要点：
/// - 幂等：重复执行会更新已存在的 SO，便于数据维护
/// - 目录/路径可配置：Resources 相对路径 + Assets 输出路径
/// - 不改写台词 JSON；仅引用其 Resources 路径与 identityId 作为 tag
/// </summary>
public sealed class CharacterRoleSOGeneratorWindow : EditorWindow
{
    [Serializable]
    private class NpcInfoRoot
    {
        public int version;
        public int basePaymentDefault;
        public float tipMultiplier;
        public int totalNpcCount;
        public IdentitiesBlock identities;
    }

    [Serializable]
    private class IdentitiesBlock
    {
        public IdentityBlock CompanyEmployee;
        public IdentityBlock SmallLeader;
        public IdentityBlock Freelancer;
        public IdentityBlock Boss;
        public IdentityBlock Student;
    }

    [Serializable]
    private class IdentityBlock
    {
        public float identityMultiplier;
        public List<NpcEntryLite> Busy;
        public List<NpcEntryLite> Irritable;
        public List<NpcEntryLite> Melancholy;
        public List<NpcEntryLite> Picky;
        public List<NpcEntryLite> Friendly;
    }

    [Serializable]
    private class NpcEntryLite
    {
        public string id;
        public string gender;
        public string name;
        public int initialMood;
        public float visitPercent;
    }

    [Serializable]
    private class DialoguesRoot
    {
        // dynamic map: identity -> object block (contains roleId/tag + states)
    }

    private const string DefaultNpcInfoResPath = "Character/NPCInfo";
    private const string DefaultDialoguesCNResPath = "Character/Dialogues/NPCDialogues";
    private const string DefaultDialoguesENResPath = "Character/Dialogues/NPCDialogues_en";

    private string _npcInfoResPath = DefaultNpcInfoResPath;
    private string _dialoguesCNResPath = DefaultDialoguesCNResPath;
    private string _dialoguesENResPath = DefaultDialoguesENResPath;
    private string _saveFolder = "Assets/Scripts/0_ScriptableObject/Characters";
    private string _indexPath = "Assets/Scripts/0_ScriptableObject/CharacterRolesIndex.asset";

    [MenuItem("自制工具/人物设计/角色SO生成器")] private static void Open()
    {
        var w = GetWindow<CharacterRoleSOGeneratorWindow>(true, "角色SO生成器", true);
        w.minSize = new Vector2(560, 240);
        w.Show();
    }

    private void OnGUI()
    {
        GUILayout.Label("输入资源路径(基于 Resources)", EditorStyles.boldLabel);
        _npcInfoResPath = EditorGUILayout.TextField("NPCInfo.json", _npcInfoResPath);
        _dialoguesCNResPath = EditorGUILayout.TextField("台词(CN)", _dialoguesCNResPath);
        _dialoguesENResPath = EditorGUILayout.TextField("台词(EN)", _dialoguesENResPath);

        GUILayout.Space(6);
        GUILayout.Label("输出(Assets 路径)", EditorStyles.boldLabel);
        _saveFolder = EditorGUILayout.TextField("角色SO输出目录", _saveFolder);
        _indexPath = EditorGUILayout.TextField("索引SO路径", _indexPath);

        GUILayout.Space(8);
        using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(_npcInfoResPath)))
        {
            if (GUILayout.Button("生成/更新 角色SO 与 索引SO"))
            {
                Generate();
            }
        }
    }

    /// <summary>
    /// 主流程：解析 NPCInfo → 生成/更新 角色 SO → 更新索引 SO → 软校验台词 JSON 标签
    /// </summary>
    private void Generate()
    {
        var npcInfoTa = Resources.Load<TextAsset>(_npcInfoResPath);
        if (npcInfoTa == null)
        {
            EditorUtility.DisplayDialog("失败", "未在 Resources 找到 NPCInfo.json: " + _npcInfoResPath, "好的");
            return;
        }
        NpcInfoRoot npcInfo;
        try { npcInfo = JsonUtility.FromJson<NpcInfoRoot>(npcInfoTa.text); }
        catch (Exception e)
        {
            EditorUtility.DisplayDialog("解析失败", "NPCInfo 解析错误: " + e.Message, "好的");
            return;
        }
        if (npcInfo == null || npcInfo.identities == null)
        {
            EditorUtility.DisplayDialog("失败", "NPCInfo.identities 为空", "好的");
            return;
        }

        // 确保输出目录存在
        if (!AssetDatabase.IsValidFolder(_saveFolder))
        {
            Directory.CreateDirectory(_saveFolder);
        }

        // 载入或创建索引 SO
        CharacterRolesIndex index = AssetDatabase.LoadAssetAtPath<CharacterRolesIndex>(_indexPath);
        if (index == null)
        {
            index = ScriptableObject.CreateInstance<CharacterRolesIndex>();
            index.version = npcInfo.version;
            index.basePaymentDefault = npcInfo.basePaymentDefault;
            index.tipMultiplier = npcInfo.tipMultiplier;
            AssetDatabase.CreateAsset(index, _indexPath);
        }
        else
        {
            index.version = npcInfo.version;
            index.basePaymentDefault = npcInfo.basePaymentDefault;
            index.tipMultiplier = npcInfo.tipMultiplier;
        }

        // 遍历每个身份，生成/更新对应角色 SO
        int created = 0, updated = 0, warned = 0;
        created += ProcessIdentity(index, "CompanyEmployee", npcInfo.identities.CompanyEmployee, ref updated, ref warned);
        created += ProcessIdentity(index, "SmallLeader", npcInfo.identities.SmallLeader, ref updated, ref warned);
        created += ProcessIdentity(index, "Freelancer", npcInfo.identities.Freelancer, ref updated, ref warned);
        created += ProcessIdentity(index, "Boss", npcInfo.identities.Boss, ref updated, ref warned);
        created += ProcessIdentity(index, "Student", npcInfo.identities.Student, ref updated, ref warned);

        EditorUtility.SetDirty(index);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorGUIUtility.PingObject(index);
        EditorUtility.DisplayDialog("完成", $"角色SO生成完成/更新：新建{created}，更新{updated}，台词警告{warned}", "好的");
    }

    /// <summary>
    /// 依据 NPCInfo（精简项）在项目中查找/绑定对应的 NpcCharacterData 资产到角色 SO 的 npcAssets。
    /// 规则：按 identityId + 数字 + 性别 与状态拼出我们既定命名（例如 Busy_CompanyEmployee_001_M）。
    /// 若找不到则跳过。
    /// </summary>
    private static void AppendAssets(CharacterRoleData role, string identityId, string state, List<NpcEntryLite> list)
    {
        if (list == null) return;
#if UNITY_EDITOR
        for (int i = 0; i < list.Count; i++)
        {
            var src = list[i];
            // 资产命名：<State>_<IdentityId>_<NNN>_<M/F>
            string assetName = $"{state}_{identityId}_{src.id.Split('_')[1]}_{(src.gender.StartsWith("m", StringComparison.OrdinalIgnoreCase) ? "M" : "F")}";
            string[] guids = AssetDatabase.FindAssets(assetName + " t:NpcCharacterData");
            if (guids != null && guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                var npc = AssetDatabase.LoadAssetAtPath<NpcCharacterData>(path);
                if (npc != null && !role.npcAssets.Contains(npc)) role.npcAssets.Add(npc);
            }
        }
#endif
    }

    private int ProcessIdentity(CharacterRolesIndex index, string identityId, IdentityBlock block, ref int updated, ref int warned)
    {
        if (block == null) return 0;
        string soPath = Path.Combine(_saveFolder, identityId + ".asset").Replace("\\", "/");
        CharacterRoleData role = AssetDatabase.LoadAssetAtPath<CharacterRoleData>(soPath);
        bool isNew = false;
        if (role == null)
        {
            role = ScriptableObject.CreateInstance<CharacterRoleData>();
            AssetDatabase.CreateAsset(role, soPath);
            isNew = true;
        }

        role.identityId = identityId;
        role.identityMultiplier = block.identityMultiplier;
        role.dialoguesRefCN = _dialoguesCNResPath;
        role.dialoguesRefEN = _dialoguesENResPath;
        role.dialogueTag = identityId;

        role.npcAssets.Clear();
        AppendAssets(role, identityId, "Busy", block.Busy);
        AppendAssets(role, identityId, "Irritable", block.Irritable);
        AppendAssets(role, identityId, "Melancholy", block.Melancholy);
        AppendAssets(role, identityId, "Picky", block.Picky);
        AppendAssets(role, identityId, "Friendly", block.Friendly);

        EditorUtility.SetDirty(role);
        if (!index.roles.Contains(role)) index.roles.Add(role);
        if (!isNew) updated++;

        int tagFound = 0;
        var cn = Resources.Load<TextAsset>(_dialoguesCNResPath);
        var en = Resources.Load<TextAsset>(_dialoguesENResPath);
        if (cn != null && cn.text.Contains("\"" + identityId + "\"")) tagFound++;
        if (en != null && en.text.Contains("\"" + identityId + "\"")) tagFound++;
        if (tagFound < 2) { warned++; Debug.LogWarning($"[角色SO生成器] 台词JSON中未完全找到标签: {identityId} ({tagFound}/2)"); }

        return isNew ? 1 : 0;
    }
}


