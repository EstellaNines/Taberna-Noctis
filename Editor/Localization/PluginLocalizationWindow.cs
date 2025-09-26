using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

// 插件汉化工具窗口
public class PluginLocalizationWindow : EditorWindow
{
	// 选择的字典资源
	public LocalizationMappingData mapping;
	// 目标根目录(Assets相对路径)
	public string targetAssetsPath = "";
	// 预览结果
	private List<PreviewChange> previewChanges = new List<PreviewChange>();
	// 是否只分析 Editor 下的脚本
	public bool onlyEditorFolder = true;
	// 文件过滤(逗号分隔扩展名)
	public string fileExtensions = ".cs";
	// 是否创建 .bak 备份
	public bool createBackup = true;
	// UTF8 带BOM写入，防止中文乱码
	public bool useUtf8Bom = true;
	// 是否使用 JSON 词库
	public bool useJsonMapping = true;
	// JSON 词库(TextAsset)
	public TextAsset jsonTextAsset;
	// JSON 文件路径(可选)
	public string jsonFilePath = "";
	// 是否仅整词匹配，避免 Play 命中 Playing
	public bool wholeWordOnly = true;
	// 是否区分大小写
	public bool caseSensitive = false;
	// 预览滚动位置
	private Vector2 scrollPos = Vector2.zero;

	// 正则：匹配普通字符串字面量，不包含@逐字串
	private static readonly Regex kStringLiteralRegex = new Regex("(?<!@)\"(.*?)\"", RegexOptions.Compiled);

	[MenuItem("自制工具/插件汉化工具/汉化工具")]
	public static void Open()
	{
		var win = GetWindow<PluginLocalizationWindow>(true, "插件汉化工具", true);
		win.minSize = new Vector2(720, 560);
		win.Show();
	}

	// 构建词库：优先 JSON，其次 ScriptableObject
	private Dictionary<string, string> BuildDictionary()
	{
		var comparer = caseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
		var map = new Dictionary<string, string>(comparer);
		if (useJsonMapping)
		{
			string json = null;
			if (jsonTextAsset != null) json = jsonTextAsset.text;
			else if (!string.IsNullOrEmpty(jsonFilePath) && File.Exists(jsonFilePath)) json = File.ReadAllText(jsonFilePath);
			if (!string.IsNullOrEmpty(json))
			{
				try
				{
					var root = JsonUtility.FromJson<JsonMappingRoot>(json);
					if (root != null && root.items != null)
					{
						for (int i = 0; i < root.items.Count; i++)
						{
							var it = root.items[i];
							if (string.IsNullOrEmpty(it.english)) continue;
							if (!map.ContainsKey(it.english)) map.Add(it.english, it.chinese ?? string.Empty);
						}
					}
				}
				catch (Exception e)
				{
					Debug.LogWarning("JSON 词库解析失败: " + e.Message);
				}
			}
		}
		if (!useJsonMapping && mapping != null)
		{
			var soMap = mapping.ToDictionary();
			foreach (var kv in soMap)
			{
				if (!map.ContainsKey(kv.Key)) map.Add(kv.Key, kv.Value);
			}
		}
		return map;
	}

	// 使用词库替换字符串内容，支持整词匹配与大小写设置
	private string ReplaceWithDictionary(string content, Dictionary<string,string> dict)
	{
		if (dict == null || dict.Count == 0) return content;
		var keys = new List<string>(dict.Keys);
		keys.Sort((a,b) => b.Length.CompareTo(a.Length));
		var result = content;
		for (int i = 0; i < keys.Count; i++)
		{
			var key = keys[i];
			var value = dict[key] ?? string.Empty;
			if (string.IsNullOrEmpty(key)) continue;
			var pattern = wholeWordOnly ? ("\\b" + Regex.Escape(key) + "\\b") : Regex.Escape(key);
			var opts = caseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;
			var replaced = Regex.Replace(result, pattern, value, opts);
			result = replaced;
		}
		return result;
	}

	private void OnEnable()
	{
		// 改为支持 JSON 词库：不再强制生成默认 ScriptableObject
	}

	private void OnGUI()
	{
		GUILayout.Space(6);
		EditorGUILayout.LabelField("词库来源", EditorStyles.boldLabel);
		useJsonMapping = EditorGUILayout.ToggleLeft("使用 JSON 词库(优先)", useJsonMapping);
		if (useJsonMapping)
		{
			jsonTextAsset = (TextAsset)EditorGUILayout.ObjectField("JSON TextAsset", jsonTextAsset, typeof(TextAsset), false);
			EditorGUILayout.BeginHorizontal();
			jsonFilePath = EditorGUILayout.TextField("JSON 文件路径", jsonFilePath);
			if (GUILayout.Button("选择文件", GUILayout.Width(100)))
			{
				var fp = EditorUtility.OpenFilePanel("选择 JSON 词库", Application.dataPath, "json");
				if (!string.IsNullOrEmpty(fp)) jsonFilePath = fp;
			}
			EditorGUILayout.EndHorizontal();
		}
		else
		{
			mapping = (LocalizationMappingData)EditorGUILayout.ObjectField("映射数据", mapping, typeof(LocalizationMappingData), false);
			if (mapping == null)
			{
				EditorGUILayout.HelpBox("未指定 ScriptableObject 词库，将无法替换。可切换到 JSON 词库模式。", MessageType.Info);
			}
		}

		GUILayout.Space(10);
		EditorGUILayout.LabelField("目标", EditorStyles.boldLabel);
		EditorGUILayout.BeginHorizontal();
		targetAssetsPath = EditorGUILayout.TextField("Assets相对路径", targetAssetsPath);
		if (GUILayout.Button("选择目录", GUILayout.Width(100)))
		{
			string abs = EditorUtility.OpenFolderPanel("选择需要汉化的目录", Application.dataPath, "");
			if (!string.IsNullOrEmpty(abs))
			{
				if (abs.Replace('\\','/').StartsWith(Application.dataPath))
				{
					targetAssetsPath = "Assets" + abs.Replace('\\','/').Substring(Application.dataPath.Length);
				}
				else
				{
					EditorUtility.DisplayDialog("错误", "请选择项目Assets目录内的路径", "确定");
				}
			}
		}
		EditorGUILayout.EndHorizontal();

		onlyEditorFolder = EditorGUILayout.ToggleLeft("仅处理 Editor 目录", onlyEditorFolder);
		fileExtensions = EditorGUILayout.TextField("文件扩展名(逗号分隔)", fileExtensions);
		createBackup = EditorGUILayout.ToggleLeft("替换前创建 .bak 备份", createBackup);
		useUtf8Bom = EditorGUILayout.ToggleLeft("使用 UTF8 BOM 写入", useUtf8Bom);
		wholeWordOnly = EditorGUILayout.ToggleLeft("整词匹配(避免 Play 命中 Playing)", wholeWordOnly);
		caseSensitive = EditorGUILayout.ToggleLeft("大小写敏感", caseSensitive);

		GUILayout.Space(10);
		EditorGUILayout.BeginHorizontal();
		if (GUILayout.Button("扫描并预览"))
		{
			DoScan();
		}
		GUI.enabled = previewChanges.Count > 0;
		if (GUILayout.Button("执行替换"))
		{
			DoApply();
		}
		if (GUILayout.Button("清空预览"))
		{
			previewChanges.Clear();
		}
		GUI.enabled = true;
		EditorGUILayout.EndHorizontal();

		GUILayout.Space(8);
		EditorGUILayout.LabelField($"预览项: {previewChanges.Count}");
		scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.Height(position.height - 240));
		for (int i = 0; i < previewChanges.Count; i++)
		{
			var pc = previewChanges[i];
			EditorGUILayout.LabelField(pc.assetPath, EditorStyles.miniBoldLabel);
			EditorGUILayout.LabelField($"行 {pc.lineNumber+1}");
			EditorGUILayout.LabelField("原:");
			EditorGUILayout.TextArea(pc.originalLine);
			EditorGUILayout.LabelField("新:");
			EditorGUILayout.TextArea(pc.replacedLine);
			GUILayout.Space(8);
		}
		GUILayout.EndScrollView();
	}

	private void DoScan()
	{
		previewChanges.Clear();
		if (!useJsonMapping && mapping == null)
		{
			EditorUtility.DisplayDialog("错误", "未指定映射资源", "确定");
			return;
		}
		if (string.IsNullOrEmpty(targetAssetsPath) || !Directory.Exists(ToAbsolute(targetAssetsPath)))
		{
			EditorUtility.DisplayDialog("错误", "目标路径无效", "确定");
			return;
		}
		var exts = ParseExtensions(fileExtensions);
		var files = CollectFiles(ToAbsolute(targetAssetsPath), exts, onlyEditorFolder);
		var dict = BuildDictionary();
		for (int f = 0; f < files.Count; f++)
		{
			var file = files[f];
			var lines = File.ReadAllLines(file);
			for (int i = 0; i < lines.Length; i++)
			{
				var line = lines[i];
				var replaced = ReplaceInsideStringLiterals(line, dict);
				if (!string.Equals(line, replaced, StringComparison.Ordinal))
				{
					previewChanges.Add(new PreviewChange
					{
						assetPath = ToAssets(file),
						lineNumber = i,
						originalLine = line,
						replacedLine = replaced
					});
				}
			}
		}
	}

	private void DoApply()
	{
		if (previewChanges.Count == 0) return;
		var grouped = new Dictionary<string, List<PreviewChange>>();
		for (int i = 0; i < previewChanges.Count; i++)
		{
			var pc = previewChanges[i];
			if (!grouped.TryGetValue(pc.assetPath, out var list))
			{
				list = new List<PreviewChange>();
				grouped.Add(pc.assetPath, list);
			}
			list.Add(pc);
		}
		foreach (var kv in grouped)
		{
			var abs = ToAbsolute(kv.Key);
			var lines = File.ReadAllLines(abs);
			var dict = BuildDictionary();
			for (int i = 0; i < kv.Value.Count; i++)
			{
				var c = kv.Value[i];
				var old = lines[c.lineNumber];
				var newer = ReplaceInsideStringLiterals(old, dict);
				lines[c.lineNumber] = newer;
			}
			if (createBackup)
			{
				var bak = GetBackupPathOutsideAssets(abs);
				var bakDir = Path.GetDirectoryName(bak);
				if (!Directory.Exists(bakDir)) Directory.CreateDirectory(bakDir);
				if (!File.Exists(bak)) File.Copy(abs, bak, true);
			}
			var enc = useUtf8Bom ? new UTF8Encoding(true) : new UTF8Encoding(false);
			File.WriteAllLines(abs, lines, enc);
		}
		AssetDatabase.Refresh();
		EditorUtility.DisplayDialog("完成", "替换已完成", "确定");
	}

	private static List<string> CollectFiles(string absoluteRoot, HashSet<string> exts, bool onlyEditor)
	{
		var list = new List<string>();
		var all = Directory.GetFiles(absoluteRoot, "*", SearchOption.AllDirectories);
		for (int i = 0; i < all.Length; i++)
		{
			var p = all[i].Replace('\\','/');
			if (p.EndsWith(".meta")) continue;
			if (exts.Count > 0)
			{
				var ext = Path.GetExtension(p).ToLowerInvariant();
				if (!exts.Contains(ext)) continue;
			}
			if (onlyEditor)
			{
				if (!p.Contains("/Editor/")) continue;
			}
			list.Add(p);
		}
		return list;
	}

	private static HashSet<string> ParseExtensions(string csv)
	{
		var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		if (string.IsNullOrEmpty(csv)) return set;
		var parts = csv.Split(',');
		for (int i = 0; i < parts.Length; i++)
		{
			var e = parts[i].Trim();
			if (string.IsNullOrEmpty(e)) continue;
			if (!e.StartsWith(".")) e = "." + e;
			set.Add(e);
		}
		return set;
	}

private string ReplaceInsideStringLiterals(string line, Dictionary<string,string> dict)
	{
    return kStringLiteralRegex.Replace(line, m =>
    {
        var content = m.Groups[1].Value;
        var replaced = ReplaceWithDictionary(content, dict);
        if (ReferenceEquals(content, replaced)) return m.Value;
        var escaped = replaced.Replace("\\", "\\\\").Replace("\"", "\\\"");
        return "\"" + escaped + "\"";
    });
	}

	private static string ToAbsolute(string assetsPath)
	{
		if (assetsPath.Replace('\\','/').StartsWith("Assets"))
		{
			return Application.dataPath + assetsPath.Substring("Assets".Length);
		}
		return assetsPath;
	}

	private static string ToAssets(string absolutePath)
	{
		var norm = absolutePath.Replace('\\','/');
		var data = Application.dataPath.Replace('\\','/');
		if (norm.StartsWith(data))
		{
			return "Assets" + norm.Substring(data.Length);
		}
		return absolutePath;
	}

	// 生成备份文件路径，放到项目根目录的 LocalizationBackups 下，避免 Unity 把备份当作脚本导入
	private static string GetBackupPathOutsideAssets(string absolutePath)
	{
		var projectRoot = Directory.GetParent(Application.dataPath).FullName;
		var rel = absolutePath.Replace('/', '\\');
		var assetsRoot = Application.dataPath.Replace('/', '\\');
		if (rel.StartsWith(assetsRoot)) rel = rel.Substring(assetsRoot.Length).TrimStart('\\');
		return Path.Combine(projectRoot, "LocalizationBackups", rel + ".bak");
	}

	[Serializable]
	private class PreviewChange
	{
		public string assetPath;
		public int lineNumber;
		public string originalLine;
		public string replacedLine;
	}

	[Serializable]
	private class JsonMappingRoot
	{
		public List<JsonItem> items;
	}

	[Serializable]
	private class JsonItem
	{
		public string english;
		public string chinese;
	}
}


