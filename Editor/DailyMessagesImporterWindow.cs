using System.IO;
using UnityEditor;
using UnityEngine;

public class DailyMessagesImporterWindow : EditorWindow
{
    private TextAsset _json;
    private string _assetName = "DailyMessagesData";
    private string _saveFolder = "Assets/Scripts/0_ScriptableObject";

    [MenuItem("自制工具/数据导入/JSON→每日消息SO 转换器")] 
    public static void Open()
    {
        var wnd = GetWindow<DailyMessagesImporterWindow>(true, "JSON→每日消息SO 转换器");
        wnd.minSize = new Vector2(520, 160);
        wnd.Show();
    }

    private void OnGUI()
    {
        GUILayout.Label("将 DailyMessages.json 转为 ScriptableObject", EditorStyles.boldLabel);
        _json = (TextAsset)EditorGUILayout.ObjectField("JSON 文件", _json, typeof(TextAsset), false);
        _assetName = EditorGUILayout.TextField("SO 名称", _assetName);
        EditorGUILayout.BeginHorizontal();
        _saveFolder = EditorGUILayout.TextField("保存目录", _saveFolder);
        if (GUILayout.Button("选择…", GUILayout.Width(72)))
        {
            var abs = EditorUtility.OpenFolderPanel("选择保存目录(Assets下)", Application.dataPath, "");
            if (!string.IsNullOrEmpty(abs))
            {
                var proj = Application.dataPath.Substring(0, Application.dataPath.Length - "Assets".Length);
                if (abs.StartsWith(proj))
                {
                    _saveFolder = abs.Substring(proj.Length).Replace("\\", "/");
                }
                else EditorUtility.DisplayDialog("路径错误", "请选择项目 Assets 目录内的路径", "好的");
            }
        }
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(8);
        using (new EditorGUI.DisabledScope(_json == null || string.IsNullOrEmpty(_assetName)))
        {
            if (GUILayout.Button("转换并保存 SO"))
            {
                ConvertAndSave();
            }
        }
    }

    private void ConvertAndSave()
    {
        if (_json == null)
        {
            EditorUtility.DisplayDialog("失败", "请先指定 JSON 文件", "好的");
            return;
        }
        DailyMessagesData.Root root = null;
        try
        {
            root = JsonUtility.FromJson<DailyMessagesData.Root>(_json.text);
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog("解析失败", "Json 解析错误: " + e.Message, "好的");
            return;
        }
        if (root == null || root.messages == null || root.messages.Count == 0)
        {
            EditorUtility.DisplayDialog("转换结果为空", "JSON 中未找到 messages，或数量为 0", "好的");
            return;
        }

        if (!AssetDatabase.IsValidFolder(_saveFolder))
        {
            Directory.CreateDirectory(_saveFolder);
            AssetDatabase.Refresh();
        }
        var so = ScriptableObject.CreateInstance<DailyMessagesData>();
        so.jsonResourcePath = string.Empty; // 独立于 Resources 路径
        var soPath = Path.Combine(_saveFolder, _assetName + ".asset").Replace("\\", "/");
        AssetDatabase.CreateAsset(so, soPath);

        // 通过 SerializedObject 写入列表内容
        var ser = new SerializedObject(so);
        var list = ser.FindProperty("messagesInSO");
        list.arraySize = 0;
        for (int i = 0; i < root.messages.Count; i++)
        {
            var e = root.messages[i];
            int idx = list.arraySize;
            list.InsertArrayElementAtIndex(idx);
            var ele = list.GetArrayElementAtIndex(idx);
            ele.FindPropertyRelative("id").stringValue = e.id;
            ele.FindPropertyRelative("title").stringValue = e.title;
            ele.FindPropertyRelative("imagePath").stringValue = e.imagePath;
            var adj = ele.FindPropertyRelative("adjustments");
            adj.arraySize = 0;
            if (e.adjustments != null)
            {
                for (int j = 0; j < e.adjustments.Count; j++)
                {
                    int aidx = adj.arraySize;
                    adj.InsertArrayElementAtIndex(aidx);
                    var aele = adj.GetArrayElementAtIndex(aidx);
                    aele.FindPropertyRelative("identity").stringValue = e.adjustments[j].identity;
                    aele.FindPropertyRelative("state").stringValue = e.adjustments[j].state;
                    aele.FindPropertyRelative("gender").stringValue = e.adjustments[j].gender;
                    aele.FindPropertyRelative("npcId").stringValue = e.adjustments[j].npcId;
                    aele.FindPropertyRelative("deltaPercent").floatValue = e.adjustments[j].deltaPercent;
                }
            }
        }
        ser.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(so);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorGUIUtility.PingObject(so);
        EditorUtility.DisplayDialog("成功", $"已创建 SO: {soPath}\n条目: {root.messages.Count}", "好的");
    }
}


