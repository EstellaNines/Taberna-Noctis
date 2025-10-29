using UnityEditor;
using UnityEngine;
using TabernaNoctis.Cards;

/// <summary>
/// 配方说明书（编辑器窗口）：以图文表格展示 CocktailRecipeDatabase 中每条配方，
/// 显示三张材料卡 UI 图片 + 结果鸡尾酒卡 UI 图片与英文名称。
/// </summary>
public class CocktailRecipeManualWindow : EditorWindow
{
	private CocktailRecipeDatabase database;
	private Vector2 scroll;
	private float iconSize = 72f;
	private bool showIds = false;
	private string search = string.Empty;

	[MenuItem("自制工具/卡牌系统/配方说明书", priority = 2001)]
	public static void Open()
	{
		var win = GetWindow<CocktailRecipeManualWindow>(false, "配方说明书", true);
		win.minSize = new Vector2(720, 420);
	}

	private void OnGUI()
	{
		DrawToolbar();
		EditorGUILayout.Space(6);

		if (database == null)
		{
			EditorGUILayout.HelpBox("请指定鸡尾酒配方数据库(CocktailRecipeDatabase)，或点击自动查找。", MessageType.Info);
			return;
		}

		if (database.recipes == null || database.recipes.Count == 0)
		{
			EditorGUILayout.HelpBox("当前数据库中没有任何配方。", MessageType.Warning);
			return;
		}

		// 表头
		EditorGUILayout.BeginHorizontal();
		GUILayout.Label("材料 A", EditorStyles.boldLabel, GUILayout.Width(160));
		GUILayout.Label("材料 B", EditorStyles.boldLabel, GUILayout.Width(160));
		GUILayout.Label("材料 C", EditorStyles.boldLabel, GUILayout.Width(160));
		GUILayout.Label("→ 结果", EditorStyles.boldLabel, GUILayout.ExpandWidth(true));
		EditorGUILayout.EndHorizontal();

		// 列表
		scroll = EditorGUILayout.BeginScrollView(scroll);
		for (int i = 0; i < database.recipes.Count; i++)
		{
			var r = database.recipes[i];
			if (r == null)
			{
				continue;
			}

			// 过滤（按英文名模糊），确保字符串非空
			if (!string.IsNullOrEmpty(search))
			{
				string a = r.materialA != null && !string.IsNullOrEmpty(r.materialA.nameEN) ? r.materialA.nameEN : string.Empty;
				string b = r.materialB != null && !string.IsNullOrEmpty(r.materialB.nameEN) ? r.materialB.nameEN : string.Empty;
				string c = r.materialC != null && !string.IsNullOrEmpty(r.materialC.nameEN) ? r.materialC.nameEN : string.Empty;
				string res = r.result != null && !string.IsNullOrEmpty(r.result.nameEN) ? r.result.nameEN : string.Empty;
				string s = search.ToLowerInvariant();
				if (!(a.ToLowerInvariant().Contains(s) || b.ToLowerInvariant().Contains(s) || c.ToLowerInvariant().Contains(s) || res.ToLowerInvariant().Contains(s)))
					continue;
			}

			EditorGUILayout.BeginHorizontal("box");
			DrawMaterialCell(r.materialA, 160);
			DrawMaterialCell(r.materialB, 160);
			DrawMaterialCell(r.materialC, 160);
			DrawResultCell(r.result);
			EditorGUILayout.EndHorizontal();
		}
		EditorGUILayout.EndScrollView();
	}

	private void DrawToolbar()
	{
		EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
		database = (CocktailRecipeDatabase)EditorGUILayout.ObjectField(database, typeof(CocktailRecipeDatabase), false, GUILayout.Width(300));
		if (GUILayout.Button("自动查找", EditorStyles.toolbarButton, GUILayout.Width(80)))
		{
			TryFindDatabaseAsset();
		}
		GUILayout.FlexibleSpace();
		GUILayout.Label("图标尺寸", GUILayout.Width(60));
		iconSize = GUILayout.HorizontalSlider(iconSize, 48f, 128f, GUILayout.Width(120));
		showIds = GUILayout.Toggle(showIds, "显示ID", EditorStyles.toolbarButton, GUILayout.Width(70));
		GUILayout.Space(8);
		// 使用稳定样式，避免某些版本下 GUI.skin.FindStyle("ToolbarSeachTextField") 返回 null 导致布局异常
		search = GUILayout.TextField(search, EditorStyles.toolbarSearchField, GUILayout.Width(200));
		if (GUILayout.Button("X", EditorStyles.toolbarButton, GUILayout.Width(22))) search = string.Empty;
		EditorGUILayout.EndHorizontal();
	}

	private void DrawMaterialCell(MaterialCardSO mat, float width)
	{
		EditorGUILayout.BeginVertical(GUILayout.Width(width));
		DrawSpriteWithName(mat != null ? mat.uiSpritePreview : null, mat != null ? mat.nameEN : "<None>");
		if (showIds && mat != null) EditorGUILayout.LabelField($"ID: {mat.id}", GUILayout.Height(16));
		EditorGUILayout.EndVertical();
	}

	private void DrawResultCell(CocktailCardSO cocktail)
	{
		EditorGUILayout.BeginVertical();
		DrawSpriteWithName(cocktail != null ? cocktail.uiSpritePreview : null, cocktail != null ? cocktail.nameEN : "<None>");
		if (showIds && cocktail != null) EditorGUILayout.LabelField($"ID: {cocktail.id}", GUILayout.Height(16));
		EditorGUILayout.EndVertical();
	}

	private void DrawSpriteWithName(Sprite sprite, string name)
	{
		Rect r = GUILayoutUtility.GetRect(iconSize, iconSize, GUILayout.ExpandWidth(false));
		if (sprite != null)
		{
			DrawSprite(r, sprite);
		}
		else
		{
			EditorGUI.HelpBox(r, "No Sprite", MessageType.None);
		}
		EditorGUILayout.LabelField(name ?? string.Empty, EditorStyles.wordWrappedLabel, GUILayout.Height(18));
	}

	private void DrawSprite(Rect r, Sprite sprite)
	{
		if (sprite == null) return;
		Texture2D tex = sprite.texture;
		if (tex == null)
		{
			EditorGUI.HelpBox(r, "No Texture", MessageType.None);
			return;
		}
		Rect tr = sprite.textureRect;
		Rect uv = new Rect(tr.x / tex.width, tr.y / tex.height, tr.width / tex.width, tr.height / tex.height);
		GUI.DrawTextureWithTexCoords(r, tex, uv, true);
		GUI.Label(new Rect(r.x, r.y, r.width, 16), string.Empty); // 兼容深色皮肤的可点击区域
	}

	private void TryFindDatabaseAsset()
	{
		string[] guids = AssetDatabase.FindAssets("t:CocktailRecipeDatabase");
		if (guids != null && guids.Length > 0)
		{
			string path = AssetDatabase.GUIDToAssetPath(guids[0]);
			database = AssetDatabase.LoadAssetAtPath<CocktailRecipeDatabase>(path);
		}
	}
}


