using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 一键从 Resources/Character/NPCInfo.json 生成 50 位顾客的独立 SO（NpcCharacterData），并维护 NpcDatabase。
/// 菜单：自制工具/角色系统/生成50位顾客SO
/// </summary>
public static class NpcCharacterSoGenerator
{
    private const string JsonAssetPath = "Assets/Resources/Character/NPCInfo.json"; // 直接读文本，避免Resources导入依赖
    private const string OutputFolder = "Assets/Scripts/0_ScriptableObject/NPCs";
    private const string DatabasePath = "Assets/Scripts/0_ScriptableObject/NpcDatabase.asset";

    [MenuItem("自制工具/人物设计/角色系统/生成50位顾客SO")] 
    public static void GenerateAll()
    {
        try
        {
            // 1) 读取 JSON
            if (!File.Exists(JsonAssetPath))
            {
                EditorUtility.DisplayDialog("生成失败", "未找到 NPCInfo.json: " + JsonAssetPath, "OK");
                return;
            }

            string json = File.ReadAllText(JsonAssetPath);
            var root = JsonUtility.FromJson<NPCInfoRoot>(json);
            if (root == null || root.identities == null)
            {
                EditorUtility.DisplayDialog("生成失败", "解析 NPCInfo.json 失败，请检查文件格式。", "OK");
                return;
            }

            // 2) 准备输出目录
            EnsureFolder(OutputFolder);

            // 3) 遍历身份与状态，生成/更新 SO
            var createdOrUpdated = new List<NpcCharacterData>();

            CreateOrUpdateForIdentity(root.identities.CompanyEmployee, "CompanyEmployee", createdOrUpdated);
            CreateOrUpdateForIdentity(root.identities.SmallLeader, "SmallLeader", createdOrUpdated);
            CreateOrUpdateForIdentity(root.identities.Freelancer, "Freelancer", createdOrUpdated);
            CreateOrUpdateForIdentity(root.identities.Boss, "Boss", createdOrUpdated);
            CreateOrUpdateForIdentity(root.identities.Student, "Student", createdOrUpdated);

            // 3.1) 将生成的 NPC 绑定到对应身份 SO 的 npcAssets 列表
            BindNpcAssetsToRoles(createdOrUpdated, new Dictionary<string, float>
            {
                {"CompanyEmployee", root.identities?.CompanyEmployee?.identityMultiplier ?? 1f},
                {"SmallLeader",    root.identities?.SmallLeader?.identityMultiplier    ?? 1f},
                {"Freelancer",     root.identities?.Freelancer?.identityMultiplier     ?? 1f},
                {"Boss",           root.identities?.Boss?.identityMultiplier           ?? 1f},
                {"Student",        root.identities?.Student?.identityMultiplier        ?? 1f},
            });

            // 4) 维护 NpcDatabase
            var db = AssetDatabase.LoadAssetAtPath<NpcDatabase>(DatabasePath);
            if (db == null)
            {
                EnsureFolder("Assets/Scripts/0_ScriptableObject");
                db = ScriptableObject.CreateInstance<NpcDatabase>();
                db.version = root.version;
                AssetDatabase.CreateAsset(db, DatabasePath);
            }
            db.version = root.version;
            db.allNpcs = createdOrUpdated;
            EditorUtility.SetDirty(db);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("生成完成", $"已生成/更新 {createdOrUpdated.Count} 位顾客SO，并更新 NpcDatabase。", "OK");
        }
        catch (System.SystemException e)
        {
            Debug.LogError("[NpcCharacterSoGenerator] 生成失败: " + e.Message);
            EditorUtility.DisplayDialog("生成失败", e.Message, "OK");
        }
    }

    private static void CreateOrUpdateForIdentity(Identity identity, string identityId, List<NpcCharacterData> sink)
    {
        if (identity == null) return;

        CreateOrUpdateState(identity.Busy, identityId, identity.identityMultiplier, "Busy", sink);
        CreateOrUpdateState(identity.Irritable, identityId, identity.identityMultiplier, "Irritable", sink);
        CreateOrUpdateState(identity.Melancholy, identityId, identity.identityMultiplier, "Melancholy", sink);
        CreateOrUpdateState(identity.Picky, identityId, identity.identityMultiplier, "Picky", sink);
        CreateOrUpdateState(identity.Friendly, identityId, identity.identityMultiplier, "Friendly", sink);
    }

    private static void CreateOrUpdateState(List<NpcJson> list, string identityId, float identityMultiplier, string state, List<NpcCharacterData> sink)
    {
        if (list == null) return;
        foreach (var n in list)
        {
            // 新命名规则：以状态为前缀，形如 Busy_CompanyEmployee_001_M.asset
            var newAssetPath = $"{OutputFolder}/{state}_{n.id}.asset";
            var oldAssetPath = $"{OutputFolder}/Npc_{n.id}.asset"; // 兼容旧命名，便于迁移

            // 若存在旧文件且新文件不存在，则迁移重命名
            if (!File.Exists(newAssetPath) && File.Exists(oldAssetPath))
            {
                var moveResult = AssetDatabase.MoveAsset(oldAssetPath, newAssetPath);
                if (!string.IsNullOrEmpty(moveResult))
                {
                    Debug.LogWarning($"[NpcCharacterSoGenerator] 重命名失败: {oldAssetPath} -> {newAssetPath}, {moveResult}");
                }
            }

            var npc = AssetDatabase.LoadAssetAtPath<NpcCharacterData>(newAssetPath);
            bool created = false;
            if (npc == null)
            {
                npc = ScriptableObject.CreateInstance<NpcCharacterData>();
                created = true;
            }

            npc.id = n.id;
            npc.identityId = identityId;
            npc.identityMultiplier = identityMultiplier;
            npc.gender = n.gender;
            npc.state = state;
            npc.displayName = string.IsNullOrEmpty(n.displayName) ? GetDefaultDisplayName(identityId) : n.displayName;
            npc.stateColor = ParseColor(n.stateColor, state);
            npc.initialMood = n.initialMood;
            npc.visitPercent = n.visitPercent;
            npc.portraitPath = NormalizePortraitPath(n.portraitPath, identityId, n.id);
            // dialoguesRefCN/EN 使用 NpcCharacterData 默认值，无需额外设置

            // 确保SO显示名称与文件名一致（状态_编号）
            npc.name = $"{state}_{n.id}";

            if (created)
            {
                AssetDatabase.CreateAsset(npc, newAssetPath);
            }
            else
            {
                EditorUtility.SetDirty(npc);
            }
            sink.Add(npc);
        }
    }

    /// <summary>
    /// 尝试将生成的 NPC SO 绑定到各身份的 CharacterRoleData（*.asset）
    /// </summary>
    private static void BindNpcAssetsToRoles(List<NpcCharacterData> allNpcs, Dictionary<string, float> identityMultiplierMap)
    {
        if (allNpcs == null || allNpcs.Count == 0) return;

        // 收集全部角色SO
        var roleGuids = AssetDatabase.FindAssets("t:CharacterRoleData");
        var identityToRole = new Dictionary<string, CharacterRoleData>();
        foreach (var guid in roleGuids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var role = AssetDatabase.LoadAssetAtPath<CharacterRoleData>(path);
            if (role == null || string.IsNullOrEmpty(role.identityId)) continue;
            identityToRole[role.identityId] = role;
        }

        // 逐身份写入
        var grouped = new Dictionary<string, List<NpcCharacterData>>();
        foreach (var npc in allNpcs)
        {
            if (!grouped.TryGetValue(npc.identityId, out var list))
            {
                list = new List<NpcCharacterData>();
                grouped[npc.identityId] = list;
            }
            list.Add(npc);
        }

        foreach (var kv in grouped)
        {
            var identity = kv.Key;
            if (!identityToRole.TryGetValue(identity, out var role))
            {
                Debug.LogWarning($"[NpcCharacterSoGenerator] 未找到身份SO: {identity}，请确认*.asset是否存在。");
                continue;
            }

            // 按名称排序，便于浏览
            kv.Value.Sort((a,b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));
            role.npcAssets.Clear();
            role.npcAssets.AddRange(kv.Value);

            if (identityMultiplierMap != null && identityMultiplierMap.TryGetValue(identity, out float mul))
            {
                role.identityMultiplier = mul;
            }

            EditorUtility.SetDirty(role);
            Debug.Log($"[NpcCharacterSoGenerator] 绑定 {identity} → {kv.Value.Count} 个NPC");
        }
    }

    /// <summary>
    /// 规范化并验证立绘路径：
    /// - 若JSON路径可直接加载，原样返回
    /// - 否则在 Assets/Resources 下搜索与 id 匹配的Sprite并生成 Resources 相对路径
    /// - 否则返回空并告警
    /// </summary>
    private static string NormalizePortraitPath(string jsonPath, string identityId, string id)
    {
        bool IsValid(string p) => !string.IsNullOrEmpty(p) && Resources.Load<Sprite>(p) != null;

        if (IsValid(jsonPath)) return jsonPath;

        // 优先精确文件名搜索（不含扩展名）
        string[] exactGuids = AssetDatabase.FindAssets($"{id} t:Sprite");
        foreach (var g in exactGuids)
        {
            var ap = AssetDatabase.GUIDToAssetPath(g);
            var rp = ToResourcesPath(ap);
            if (IsValid(rp)) return rp;
        }

        // 模糊搜索：仅按文件名包含 id 片段
        string[] fuzzyGuids = AssetDatabase.FindAssets($"t:Sprite {id}");
        foreach (var g in fuzzyGuids)
        {
            var ap = AssetDatabase.GUIDToAssetPath(g);
            var rp = ToResourcesPath(ap);
            if (IsValid(rp)) return rp;
        }

        // 基于 identity + 编号 退化匹配
        string indexPart = id;
        int underscore = id.IndexOf('_');
        if (underscore >= 0) indexPart = id.Substring(underscore + 1); // 001_M
        string shortKey = identityId + "_" + indexPart;               // CompanyEmployee_001_M
        string[] shortGuids = AssetDatabase.FindAssets($"{shortKey} t:Sprite");
        foreach (var g in shortGuids)
        {
            var ap = AssetDatabase.GUIDToAssetPath(g);
            var rp = ToResourcesPath(ap);
            if (IsValid(rp)) return rp;
        }

        Debug.LogWarning($"[NpcCharacterSoGenerator] 立绘未找到，保持空: id={id}, JSON路径='{jsonPath}'");
        return string.IsNullOrEmpty(jsonPath) ? string.Empty : jsonPath; // 返回原值以便后续人工修复
    }

    private static string ToResourcesPath(string assetPath)
    {
        if (string.IsNullOrEmpty(assetPath)) return null;
        const string prefix = "Assets/Resources/";
        if (!assetPath.StartsWith(prefix)) return null;
        string withoutPrefix = assetPath.Substring(prefix.Length);
        string withoutExt = System.IO.Path.ChangeExtension(withoutPrefix, null);
        return withoutExt.Replace('\\','/');
    }

    private static void EnsureFolder(string folderPath)
    {
        var parts = folderPath.Replace('\\', '/').Split('/');
        string path = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string prev = path;
            path += "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(prev, parts[i]);
            }
        }
    }

    /// <summary>
    /// 根据身份ID返回默认的显示名称（用于向后兼容）
    /// </summary>
    private static string GetDefaultDisplayName(string identityId)
    {
        switch (identityId)
        {
            case "CompanyEmployee": return "Company Employee";
            case "SmallLeader": return "Small Leader";
            case "Freelancer": return "Freelancer";
            case "Boss": return "Boss";
            case "Student": return "College Student";
            default: return identityId;
        }
    }

    /// <summary>
    /// 解析颜色字符串（十六进制或状态默认值）
    /// </summary>
    private static Color ParseColor(string colorHex, string state)
    {
        // 如果提供了颜色字符串，尝试解析
        if (!string.IsNullOrEmpty(colorHex) && ColorUtility.TryParseHtmlString(colorHex, out Color color))
        {
            return color;
        }

        // 否则根据状态返回默认颜色
        switch (state)
        {
            case "Busy": return new Color(0f, 1f, 0f);          // 绿色 #00FF00
            case "Friendly": return new Color(0.54f, 0.17f, 0.89f); // 蓝紫色 #8A2BE2
            case "Irritable": return new Color(1f, 0f, 0f);     // 红色 #FF0000
            case "Melancholy": return new Color(0f, 1f, 1f);    // 青色 #00FFFF
            case "Picky": return new Color(1f, 1f, 0f);         // 黄色 #FFFF00
            default: return Color.white;
        }
    }

    // ===== JSON 数据结构（与 NPCInfo.json 对应，使用强类型避免字典反序列化问题） =====
    [System.Serializable]
    private sealed class NPCInfoRoot
    {
        public int version;
        public float basePaymentDefault;
        public float tipMultiplier;
        public int totalNpcCount;
        public string probabilityModel;
        public float baseProbabilityPercent;
        public float minClampPercent;
        public float maxClampPercent;
        public Identities identities;
    }

    [System.Serializable]
    private sealed class Identities
    {
        public Identity CompanyEmployee;
        public Identity SmallLeader;
        public Identity Freelancer;
        public Identity Boss;
        public Identity Student;
    }

    [System.Serializable]
    private sealed class Identity
    {
        public float identityMultiplier;
        public List<NpcJson> Busy;
        public List<NpcJson> Irritable;
        public List<NpcJson> Melancholy;
        public List<NpcJson> Picky;
        public List<NpcJson> Friendly;
    }

    [System.Serializable]
    private sealed class NpcJson
    {
        public string id;
        public string gender;
        public string name;
        public string displayName;      // 简短显示名称（如"Company Employee"）
        public string stateColor;       // 状态颜色（十六进制如"#00FF00"）
        public int initialMood;
        public float visitPercent;
        public string portraitPath; // 可能为空
    }
}


