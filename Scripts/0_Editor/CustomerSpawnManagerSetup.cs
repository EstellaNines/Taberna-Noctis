using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// 编辑器工具：帮助设置 CustomerSpawnManager 的引用
/// </summary>
public class CustomerSpawnManagerSetup
{
    [MenuItem("Tools/TN/Setup CustomerSpawnManager References")]
    public static void SetupReferences()
    {
        Debug.Log("=== CustomerSpawnManager 引用设置工具 ===");

        // 1. 查找场景中的 CustomerSpawnManager
        var spawnManager = Object.FindObjectOfType<CustomerSpawnManager>();
        if (spawnManager == null)
        {
            Debug.LogError("场景中未找到 CustomerSpawnManager 组件！");
            Debug.LogError("请先在 NightScreen 场景中创建一个 GameObject 并添加 CustomerSpawnManager 组件");
            return;
        }

        Debug.Log($"找到 CustomerSpawnManager: {spawnManager.gameObject.name}");

        // 2. 查找 CharacterRolesIndex
        string[] rolesIndexGuids = AssetDatabase.FindAssets("t:CharacterRolesIndex");
        CharacterRolesIndex rolesIndex = null;
        
        if (rolesIndexGuids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(rolesIndexGuids[0]);
            rolesIndex = AssetDatabase.LoadAssetAtPath<CharacterRolesIndex>(path);
            Debug.Log($"找到 CharacterRolesIndex: {path}");
        }

        // 3. 查找 NpcDatabase
        string[] databaseGuids = AssetDatabase.FindAssets("t:NpcDatabase");
        NpcDatabase npcDatabase = null;
        
        if (databaseGuids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(databaseGuids[0]);
            npcDatabase = AssetDatabase.LoadAssetAtPath<NpcDatabase>(path);
            Debug.Log($"找到 NpcDatabase: {path}");
        }

        // 4. 设置引用
        bool hasChanges = false;

        // 使用反射设置私有字段（仅在编辑器中）
        var characterRolesIndexField = typeof(CustomerSpawnManager).GetField("characterRolesIndex", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var npcDatabaseField = typeof(CustomerSpawnManager).GetField("npcDatabase", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (characterRolesIndexField != null && rolesIndex != null)
        {
            characterRolesIndexField.SetValue(spawnManager, rolesIndex);
            hasChanges = true;
            Debug.Log("�7�3 已设置 CharacterRolesIndex 引用");
        }

        if (npcDatabaseField != null && npcDatabase != null)
        {
            npcDatabaseField.SetValue(spawnManager, npcDatabase);
            hasChanges = true;
            Debug.Log("�7�3 已设置 NpcDatabase 引用");
        }

        if (hasChanges)
        {
            EditorUtility.SetDirty(spawnManager);
            Debug.Log("�7�3 引用设置完成！请保存场景");
        }
        else
        {
            Debug.LogWarning("�7�4 未找到可设置的引用");
        }

        // 5. 验证设置
        VerifySetup(spawnManager);
    }

    [MenuItem("Tools/TN/Verify CustomerSpawnManager Setup")]
    public static void VerifySetup()
    {
        var spawnManager = Object.FindObjectOfType<CustomerSpawnManager>();
        if (spawnManager == null)
        {
            Debug.LogError("场景中未找到 CustomerSpawnManager！");
            return;
        }

        VerifySetup(spawnManager);
    }

    private static void VerifySetup(CustomerSpawnManager spawnManager)
    {
        Debug.Log("=== CustomerSpawnManager 设置验证 ===");

        // 使用反射检查私有字段
        var characterRolesIndexField = typeof(CustomerSpawnManager).GetField("characterRolesIndex", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var npcDatabaseField = typeof(CustomerSpawnManager).GetField("npcDatabase", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var rolesIndex = characterRolesIndexField?.GetValue(spawnManager) as CharacterRolesIndex;
        var database = npcDatabaseField?.GetValue(spawnManager) as NpcDatabase;

        if (rolesIndex != null)
        {
            Debug.Log($"�7�3 CharacterRolesIndex: {rolesIndex.name}");
            Debug.Log($"   - 版本: {rolesIndex.version}");
            Debug.Log($"   - 身份数量: {rolesIndex.roles.Count}");
            
            int totalNpcs = 0;
            foreach (var role in rolesIndex.roles)
            {
                if (role != null)
                {
                    totalNpcs += role.npcAssets.Count;
                    Debug.Log($"   - {role.identityId}: {role.npcAssets.Count} 个NPC");
                }
            }
            Debug.Log($"   - 总NPC数量: {totalNpcs}");
        }
        else
        {
            Debug.LogWarning("�7�4 CharacterRolesIndex 未设置");
        }

        if (database != null)
        {
            Debug.Log($"�7�3 NpcDatabase: {database.name}");
            Debug.Log($"   - NPC数量: {database.allNpcs.Count}");
        }
        else
        {
            Debug.LogWarning("�7�4 NpcDatabase 未设置");
        }

        if (rolesIndex == null && database == null)
        {
            Debug.LogError("�7�4 至少需要设置 CharacterRolesIndex 或 NpcDatabase 中的一个！");
            Debug.LogError("推荐使用 CharacterRolesIndex，因为它利用了现有的层次化架构");
        }

        Debug.Log("=== 验证完成 ===");
    }

    [MenuItem("Tools/TN/Create CustomerSpawnManager GameObject")]
    public static void CreateCustomerSpawnManagerGameObject()
    {
        // 检查是否已存在
        var existing = Object.FindObjectOfType<CustomerSpawnManager>();
        if (existing != null)
        {
            Debug.LogWarning($"场景中已存在 CustomerSpawnManager: {existing.gameObject.name}");
            Selection.activeGameObject = existing.gameObject;
            return;
        }

        // 创建新的 GameObject
        var go = new GameObject("CustomerSpawnManager");
        var component = go.AddComponent<CustomerSpawnManager>();
        
        // 设置位置
        go.transform.position = Vector3.zero;
        
        // 选中新创建的对象
        Selection.activeGameObject = go;
        
        Debug.Log("�7�3 已创建 CustomerSpawnManager GameObject");
        Debug.Log("请运行 'Tools → TN → Setup CustomerSpawnManager References' 来设置引用");
    }
}
