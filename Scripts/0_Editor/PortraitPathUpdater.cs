using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

/// <summary>
/// 立绘路径更新工具：将Portrait文件夹中的立绘路径写入NPCInfo.json
/// </summary>
public class PortraitPathUpdater : EditorWindow
{
    [MenuItem("自制工具/人物设计/角色系统/更新立绘路径到NPCInfo")]
    public static void ShowWindow()
    {
        GetWindow<PortraitPathUpdater>("立绘路径更新器");
    }
    
    private void OnGUI()
    {
        GUILayout.Label("立绘路径更新工具", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        GUILayout.Label("此工具将扫描Resources/Character/Portrait文件夹中的所有立绘文件，");
        GUILayout.Label("并根据文件名自动匹配到NPCInfo.json中对应的NPC，添加portraitPath字段。");
        GUILayout.Space(10);
        
        if (GUILayout.Button("扫描并更新立绘路径", GUILayout.Height(30)))
        {
            UpdatePortraitPaths();
        }
        
        GUILayout.Space(10);
        if (GUILayout.Button("预览映射关系（不修改文件）", GUILayout.Height(25)))
        {
            PreviewPortraitMapping();
        }
    }
    
    /// <summary>
    /// 更新立绘路径到NPCInfo.json
    /// </summary>
    private void UpdatePortraitPaths()
    {
        try
        {
            // 1. 扫描Portrait文件夹
            var portraitMappings = ScanPortraitFiles();
            
            // 2. 读取NPCInfo.json
            string npcInfoPath = "Assets/Resources/Character/NPCInfo.json";
            if (!File.Exists(npcInfoPath))
            {
                Debug.LogError($"NPCInfo.json文件不存在: {npcInfoPath}");
                return;
            }
            
            string jsonContent = File.ReadAllText(npcInfoPath);
            var npcData = JsonUtility.FromJson<NPCInfoRoot>(jsonContent);
            
            // 3. 更新立绘路径
            int updatedCount = ApplyPortraitPaths(npcData, portraitMappings);
            
            // 4. 保存文件
            string updatedJson = JsonUtility.ToJson(npcData, true);
            File.WriteAllText(npcInfoPath, updatedJson);
            
            Debug.Log($"立绘路径更新完成！共更新了 {updatedCount} 个NPC的立绘路径");
            Debug.Log($"文件已保存: {npcInfoPath}");
            
            // 刷新AssetDatabase
            AssetDatabase.Refresh();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"更新立绘路径时发生错误: {e.Message}");
        }
    }
    
    /// <summary>
    /// 预览映射关系
    /// </summary>
    private void PreviewPortraitMapping()
    {
        var portraitMappings = ScanPortraitFiles();
        
        Debug.Log("=== 立绘文件映射预览 ===");
        foreach (var mapping in portraitMappings)
        {
            Debug.Log($"{mapping.Key} -> {mapping.Value}");
        }
        Debug.Log($"共找到 {portraitMappings.Count} 个立绘文件");
    }
    
    /// <summary>
    /// 扫描Portrait文件夹中的立绘文件
    /// </summary>
    private Dictionary<string, string> ScanPortraitFiles()
    {
        var mappings = new Dictionary<string, string>();
        string portraitFolder = "Assets/Resources/Character/Portrait";
        
        if (!Directory.Exists(portraitFolder))
        {
            Debug.LogWarning($"Portrait文件夹不存在: {portraitFolder}");
            return mappings;
        }
        
        string[] pngFiles = Directory.GetFiles(portraitFolder, "*.png");
        
        foreach (string filePath in pngFiles)
        {
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            string resourcePath = $"Character/Portrait/{fileName}";
            
            // 根据文件名分析对应的NPC
            var npcKey = AnalyzePortraitFileName(fileName);
            if (!string.IsNullOrEmpty(npcKey))
            {
                mappings[npcKey] = resourcePath;
            }
        }
        
        return mappings;
    }
    
    /// <summary>
    /// 分析立绘文件名，返回对应的NPC键值
    /// </summary>
    private string AnalyzePortraitFileName(string fileName)
    {
        // 移除可能的空格和特殊字符
        fileName = fileName.Trim();
        
        // 映射规则：
        // YH2-Busy女员工 -> CompanyEmployee_Busy_F
        // YH2-Busy男员工 -> CompanyEmployee_Busy_M
        // YH2-小领导(女) -> SmallLeader_F (通用)
        // YH2-自由职业者(男) -> Freelancer_M (通用)
        // YH2-老板(女) -> Boss_F (通用)
        // YH2-女大学生 -> Student_F (通用)
        
        // 员工类（有具体状态）
        if (fileName.Contains("Busy") && fileName.Contains("员工"))
        {
            return fileName.Contains("女") ? "CompanyEmployee_Busy_F" : "CompanyEmployee_Busy_M";
        }
        if (fileName.Contains("Impatient") && fileName.Contains("员工"))
        {
            return fileName.Contains("女") ? "CompanyEmployee_Irritable_F" : "CompanyEmployee_Irritable_M";
        }
        if (fileName.Contains("Boredom") && fileName.Contains("员工"))
        {
            return fileName.Contains("女") ? "CompanyEmployee_Melancholy_F" : "CompanyEmployee_Melancholy_M";
        }
        if (fileName.Contains("Picky") && fileName.Contains("员工"))
        {
            return fileName.Contains("女") ? "CompanyEmployee_Picky_F" : "CompanyEmployee_Picky_M";
        }
        if (fileName.Contains("Friendly") && fileName.Contains("员工"))
        {
            return fileName.Contains("女") ? "CompanyEmployee_Friendly_F" : "CompanyEmployee_Friendly_M";
        }
        
        // 其他身份（通用立绘）
        if (fileName.Contains("小领导"))
        {
            return fileName.Contains("女") ? "SmallLeader_F" : "SmallLeader_M";
        }
        if (fileName.Contains("自由职业者"))
        {
            return fileName.Contains("女") ? "Freelancer_F" : "Freelancer_M";
        }
        if (fileName.Contains("老板"))
        {
            return fileName.Contains("女") ? "Boss_F" : "Boss_M";
        }
        if (fileName.Contains("大学生"))
        {
            return fileName.Contains("女") ? "Student_F" : "Student_M";
        }
        
        // 特殊立绘
        if (fileName.Contains("侧身"))
        {
            return fileName.Contains("女") ? "Profile_F" : "Profile_M";
        }
        if (fileName.Contains("老店长"))
        {
            return "ShopKeeper";
        }
        
        Debug.LogWarning($"无法识别的立绘文件名: {fileName}");
        return null;
    }
    
    /// <summary>
    /// 将立绘路径应用到NPC数据
    /// </summary>
    private int ApplyPortraitPaths(NPCInfoRoot npcData, Dictionary<string, string> portraitMappings)
    {
        int updatedCount = 0;
        
        foreach (var identityKV in npcData.identities)
        {
            string identityName = identityKV.Key;
            var identity = identityKV.Value;
            
            // 遍历每个状态
            foreach (var stateField in identity.GetType().GetFields())
            {
                if (stateField.FieldType == typeof(List<NPCInfo>))
                {
                    string stateName = stateField.Name;
                    var npcList = (List<NPCInfo>)stateField.GetValue(identity);
                    
                    if (npcList != null)
                    {
                        foreach (var npc in npcList)
                        {
                            // 构建查找键
                            string genderSuffix = npc.gender == "male" ? "M" : "F";
                            
                            // 优先查找具体状态的立绘
                            string specificKey = $"{identityName}_{stateName}_{genderSuffix}";
                            // 备用通用立绘
                            string genericKey = $"{identityName}_{genderSuffix}";
                            
                            string portraitPath = null;
                            if (portraitMappings.ContainsKey(specificKey))
                            {
                                portraitPath = portraitMappings[specificKey];
                            }
                            else if (portraitMappings.ContainsKey(genericKey))
                            {
                                portraitPath = portraitMappings[genericKey];
                            }
                            
                            if (!string.IsNullOrEmpty(portraitPath))
                            {
                                npc.portraitPath = portraitPath;
                                updatedCount++;
                                Debug.Log($"更新立绘: {npc.id} -> {portraitPath}");
                            }
                            else
                            {
                                Debug.LogWarning($"未找到立绘: {npc.id} (查找键: {specificKey}, {genericKey})");
                            }
                        }
                    }
                }
            }
        }
        
        return updatedCount;
    }
}

// 数据结构定义（简化版，用于JSON解析）
[System.Serializable]
public class NPCInfoRoot
{
    public int version;
    public float basePaymentDefault;
    public float tipMultiplier;
    public int totalNpcCount;
    public string probabilityModel;
    public float baseProbabilityPercent;
    public float minClampPercent;
    public float maxClampPercent;
    public Dictionary<string, NPCIdentity> identities = new Dictionary<string, NPCIdentity>();
}

[System.Serializable]
public class NPCIdentity
{
    public float identityMultiplier;
    public List<NPCInfo> Busy = new List<NPCInfo>();
    public List<NPCInfo> Irritable = new List<NPCInfo>();
    public List<NPCInfo> Melancholy = new List<NPCInfo>();
    public List<NPCInfo> Picky = new List<NPCInfo>();
    public List<NPCInfo> Friendly = new List<NPCInfo>();
}

[System.Serializable]
public class NPCInfo
{
    public string id;
    public string gender;
    public string name;
    public int initialMood;
    public int visitPercent;
    public string portraitPath; // 新增字段
}
