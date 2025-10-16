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
            npc.displayName = n.name;
            npc.initialMood = n.initialMood;
            npc.visitPercent = n.visitPercent;
            npc.portraitPath = n.portraitPath;
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
        public int initialMood;
        public float visitPercent;
        public string portraitPath; // 可能为空
    }
}


