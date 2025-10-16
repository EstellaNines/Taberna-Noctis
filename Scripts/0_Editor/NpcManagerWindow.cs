using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

/// <summary>
/// 顾客 NPC 角色管理器（编辑器窗口）
/// 功能：
/// - 左侧：NPC 列表（按名字显示，每行有下拉按钮，点击弹出详细菜单项）
/// - 中间/左侧：显示所选 NPC 的详细信息
/// - 右侧：显示立绘（来自 NpcCharacterData.portraitPath 的 Resources）
/// 依赖：NpcCharacterData 单体 SO；CharacterRoleData 的 npcAssets 仅用于定位资产集合（可选）。
/// </summary>
public sealed class NpcManagerWindow : EditorWindow
{
    private List<NpcCharacterData> _allNpcs = new List<NpcCharacterData>();
    private NpcCharacterData _selected;
    private Texture2D _portraitTex;

    // UI Toolkit elements
    private ListView _listView;
    private VisualElement _detailPanel;
    private VisualElement _portraitPanel;
    private Image _portraitImage;

    [MenuItem("自制工具/人物设计/NPC 管理器")] private static void Open()
    {
        var w = GetWindow<NpcManagerWindow>(true, "NPC 管理器", true);
        w.minSize = new Vector2(900, 540);
        w.Show();
    }

    private void OnEnable()
    {
        RefreshNpcList();
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    private void OnDisable()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        _portraitTex = null;
    }

    private void OnPlayModeChanged(PlayModeStateChange state)
    {
        // 保持资源引用在切换时仍可刷新
        if (state == PlayModeStateChange.EnteredEditMode || state == PlayModeStateChange.EnteredPlayMode)
        {
            RefreshNpcList();
            LoadPortrait(_selected);
            Repaint();
        }
    }

    private void RefreshNpcList()
    {
        _allNpcs.Clear();
        string[] guids = AssetDatabase.FindAssets("t:NpcCharacterData");
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            var npc = AssetDatabase.LoadAssetAtPath<NpcCharacterData>(path);
            if (npc != null) _allNpcs.Add(npc);
        }
        _allNpcs.Sort((a, b) => string.Compare(a.displayName, b.displayName, StringComparison.Ordinal));
    }

    public void CreateGUI()
    {
        // Root styling
        rootVisualElement.AddToClassList("app-root");
        TryAttachStyle(rootVisualElement);

        // Split view: left list, right panels
        var split = new TwoPaneSplitView(0, 320, TwoPaneSplitViewOrientation.Horizontal);
        rootVisualElement.Add(split);

        // Left: List
        var left = new VisualElement();
        left.style.flexGrow = 0;
        left.style.width = 320;
        left.AddToClassList("section");
        var leftTitle = new Label("NPC 列表");
        leftTitle.AddToClassList("section-title");
        var refreshBtn = new Button(() => { RefreshNpcList(); RebuildList(); }) { text = "刷新" };
        var leftHeader = new VisualElement(); leftHeader.style.flexDirection = FlexDirection.Row;
        leftHeader.Add(leftTitle);
        leftHeader.Add(new VisualElement() { style = { flexGrow = 1 } });
        leftHeader.Add(refreshBtn);
        left.Add(leftHeader);

        _listView = new ListView();
        _listView.itemsSource = _allNpcs;
        _listView.selectionType = SelectionType.Single;
        _listView.makeItem = MakeListItem;
        _listView.bindItem = BindListItem;
        _listView.fixedItemHeight = 24;
        _listView.onSelectionChange += OnListSelectionChanged;
        _listView.AddToClassList("list");
        left.Add(_listView);

        // Right: details + portrait
        var right = new VisualElement();
        right.style.flexGrow = 1;
        var rightSplit = new TwoPaneSplitView(0, (int)(position.width * 0.5f), TwoPaneSplitViewOrientation.Horizontal);
        right.Add(rightSplit);

        _detailPanel = new ScrollView();
        _detailPanel.AddToClassList("section");
        var detailTitle = new Label("详细信息");
        detailTitle.AddToClassList("section-title");
        _detailPanel.Add(detailTitle);
        rightSplit.Add(_detailPanel);

        _portraitPanel = new VisualElement();
        _portraitPanel.AddToClassList("section");
        var portraitTitle = new Label("立绘");
        portraitTitle.AddToClassList("section-title");
        _portraitPanel.Add(portraitTitle);
        _portraitImage = new Image();
        _portraitImage.scaleMode = ScaleMode.ScaleToFit;
        _portraitImage.style.flexGrow = 1;
        _portraitPanel.Add(_portraitImage);
        rightSplit.Add(_portraitPanel);

        split.Add(left);
        split.Add(right);

        // Build initial list
        RebuildList();
        UpdateDetails(null);
        UpdatePortrait(null);
    }

    private VisualElement MakeListItem()
    {
        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        var btn = new Button();
        btn.style.flexGrow = 1;
        var drop = new Button() { text = "▼" };
        drop.style.width = 28;
        row.Add(btn);
        row.Add(drop);
        return row;
    }

    private void BindListItem(VisualElement e, int index)
    {
        var npc = (index >= 0 && index < _allNpcs.Count) ? _allNpcs[index] : null;
        var btn = e.ElementAt(0) as Button;
        var drop = e.ElementAt(1) as Button;
        btn.text = npc != null ? npc.displayName : "<null>";
        btn.clicked -= null;
        btn.clicked += () =>
        {
            if (_selected != npc)
            {
                _selected = npc;
                UpdateDetails(npc);
                LoadPortrait(npc);
                UpdatePortrait(_portraitTex);
            }
        };
        drop.clicked -= null;
        drop.clicked += () => { ShowNpcContextMenu(npc); };
    }

    private void RebuildList()
    {
        _listView.itemsSource = null;
        _listView.itemsSource = _allNpcs;
        _listView.Rebuild();
    }

    

    private void ShowNpcContextMenu(NpcCharacterData npc)
    {
        GenericMenu m = new GenericMenu();
        m.AddItem(new GUIContent("在项目窗口中定位"), false, () =>
        {
            EditorGUIUtility.PingObject(npc);
            Selection.activeObject = npc;
        });
        m.AddItem(new GUIContent("复制立绘路径"), false, () =>
        {
            EditorGUIUtility.systemCopyBuffer = npc != null ? npc.portraitPath : string.Empty;
        });
        m.AddItem(new GUIContent("打开立绘资源(若可)"), false, () =>
        {
            TryPingPortraitAsset(npc);
        });
        m.ShowAsContext();
    }

    private void UpdateDetails(NpcCharacterData npc)
    {
        _detailPanel.Clear();
        var title = new Label("详细信息"); title.AddToClassList("section-title");
        _detailPanel.Add(title);
        if (npc == null)
        {
            _detailPanel.Add(new Label("请选择左侧列表中的一个 NPC。"));
            return;
        }

        _detailPanel.Add(MakeField("ID", npc.id));
        _detailPanel.Add(MakeField("显示名", npc.displayName));
        _detailPanel.Add(MakeField("身份", npc.identityId));
        _detailPanel.Add(MakeField("身份倍率", npc.identityMultiplier.ToString("0.###")));
        _detailPanel.Add(MakeField("性别", npc.gender));
        _detailPanel.Add(MakeField("状态", npc.state));
        _detailPanel.Add(MakeField("初始心情", npc.initialMood.ToString()));
        _detailPanel.Add(MakeField("到访占比(基线)", npc.visitPercent.ToString("0.###")));
        _detailPanel.Add(MakeField("立绘路径(Resources)", npc.portraitPath));
        _detailPanel.Add(MakeField("台词(CN)", npc.dialoguesRefCN));
        _detailPanel.Add(MakeField("台词(EN)", npc.dialoguesRefEN));
        _detailPanel.Add(MakeField("台词Tag(=身份ID)", npc.identityId));

        // Odin: 提供一个按钮打开 Odin 检视（若已安装）
        var odinBtn = new Button(() => TryOpenOdinInspector(npc)) { text = "用 Odin 查看" };
        odinBtn.AddToClassList("btn"); odinBtn.AddToClassList("btn-secondary");
        _detailPanel.Add(odinBtn);
    }

    private VisualElement MakeField(string label, string value)
    {
        var row = new VisualElement(); row.style.flexDirection = FlexDirection.Row; row.AddToClassList("section");
        var l = new Label(label); l.style.minWidth = 140; l.AddToClassList("unity-base-field__label");
        var t = new TextField(); t.value = value; t.isReadOnly = true; t.style.flexGrow = 1;
        row.Add(l); row.Add(t);
        return row;
    }

    private void UpdatePortrait(Texture2D tex)
    {
        _portraitImage.image = tex;
    }

    private void OnListSelectionChanged(IEnumerable<object> objs)
    {
        foreach (var o in objs)
        {
            var npc = o as NpcCharacterData;
            _selected = npc;
            UpdateDetails(npc);
            LoadPortrait(npc);
            UpdatePortrait(_portraitTex);
            break;
        }
    }

    private void LoadPortrait(NpcCharacterData npc)
    {
        _portraitTex = null;
        if (npc == null || string.IsNullOrEmpty(npc.portraitPath)) return;
        var spr = Resources.Load<Sprite>(npc.portraitPath);
        if (spr != null)
        {
            // 从 Sprite 提取 Texture2D（临时）
            try
            {
                var rt = RenderTexture.GetTemporary((int)spr.rect.width, (int)spr.rect.height, 0, RenderTextureFormat.ARGB32);
                var prev = RenderTexture.active;
                RenderTexture.active = rt;
                GL.Clear(true, true, new Color(0, 0, 0, 0));
                Graphics.Blit(spr.texture, rt);
                var tex = new Texture2D((int)spr.rect.width, (int)spr.rect.height, TextureFormat.RGBA32, false);
                tex.ReadPixels(new Rect((int)spr.rect.x, (int)spr.rect.y, (int)spr.rect.width, (int)spr.rect.height), 0, 0);
                tex.Apply();
                RenderTexture.active = prev;
                RenderTexture.ReleaseTemporary(rt);
                _portraitTex = tex;
            }
            catch
            {
                _portraitTex = spr.texture;
            }
        }
    }

    private void TryPingPortraitAsset(NpcCharacterData npc)
    {
        if (npc == null || string.IsNullOrEmpty(npc.portraitPath)) return;
        // 尝试定位导入的纹理资源
#if UNITY_EDITOR
        string[] guids = AssetDatabase.FindAssets(Path.GetFileName(npc.portraitPath));
        if (guids != null && guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
            if (obj != null)
            {
                EditorGUIUtility.PingObject(obj);
                Selection.activeObject = obj;
            }
        }
#endif
    }

    private void TryAttachStyle(VisualElement root)
    {
        string ussPath = "Assets/Scripts/0_Editor/UITK/EditorColors.uss";
        var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(ussPath);
        if (styleSheet != null) root.styleSheets.Add(styleSheet);
    }

    private void TryOpenOdinInspector(NpcCharacterData target)
    {
        if (target == null) return;
        // 通过反射调用 Sirenix.OdinInspector.Editor.InlineEditorAttribute 或 Odin 专用窗口
        var t = Type.GetType("Sirenix.OdinInspector.Editor.OdinEditorWindow, Sirenix.OdinInspector.Editor");
        if (t == null)
        {
            // 未安装 Odin：回退到 Project ping
            EditorGUIUtility.PingObject(target);
            Selection.activeObject = target;
            return;
        }
        try
        {
            // 创建一个临时 Odin 窗口展示该对象
            var win = ScriptableObject.CreateInstance(t) as EditorWindow;
            if (win != null)
            {
                win.titleContent = new GUIContent("Odin Inspector");
                win.Show();
                var mi = t.GetMethod("InspectObject", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (mi != null)
                {
                    mi.Invoke(win, new object[] { target });
                }
            }
        }
        catch
        {
            EditorGUIUtility.PingObject(target);
            Selection.activeObject = target;
        }
    }
}


