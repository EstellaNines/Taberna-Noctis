using UnityEditor;
using UnityEngine;
using DG.Tweening;
using System;
#if ODIN_INSPECTOR || SIRENIX_ODIN_INSPECTOR
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
#endif

// GlassingClinking 自定义检查器（优先使用 Odin 渲染，否则使用默认 Inspector）。
[CustomEditor(typeof(GlassingClinking))]
public class GlassClinkingEditor :
#if ODIN_INSPECTOR || SIRENIX_ODIN_INSPECTOR
	OdinEditor
#else
	Editor
#endif
{
	public override void OnInspectorGUI()
	{
		serializedObject.Update();

#if ODIN_INSPECTOR || SIRENIX_ODIN_INSPECTOR
		SirenixEditorGUI.Title("引用", null, TextAlignment.Left, true);
		DrawProperty("imageA", "图片_左");
		DrawProperty("imageB", "图片_右");
		GUILayout.Space(6);
		SirenixEditorGUI.Title("参数", null, TextAlignment.Left, true);
		DrawProperty("targetAnchoredPosA", "目标位置A");
		DrawProperty("targetAnchoredPosB", "目标位置B");
		DrawProperty("duration", "时长(秒)");
		DrawProperty("ease", "缓动");
		DrawProperty("playOnEnable", "启用时自动播放");
		GUILayout.Space(6);
		SirenixEditorGUI.Title("到达后倾斜", null, TextAlignment.Left, true);
		DrawProperty("tiltAngleLeft", "左图倾斜角度");
		DrawProperty("tiltAngleRight", "右图倾斜角度");
		DrawProperty("tiltDuration", "倾斜时长(秒)");
		DrawProperty("tiltEase", "倾斜缓动");
		DrawProperty("tiltSfx", "倾斜音效(MP3/WAV)");
		DrawProperty("tiltSfxVolume", "音量(0-1)");
		DrawProperty("audioTarget", "播放源(可选)");
#else
		DrawHeader("引用");
		DrawProperty("imageA", "ImageA");
		DrawProperty("imageB", "ImageB");
		GUILayout.Space(6);
		DrawHeader("参数");
		DrawProperty("targetAnchoredPosA", "目标位置A");
		DrawProperty("targetAnchoredPosB", "目标位置B");
		DrawProperty("duration", "时长(秒)");
		DrawProperty("ease", "缓动");
		DrawProperty("playOnEnable", "启用时自动播放");
		GUILayout.Space(6);
		DrawHeader("到达后倾斜");
		DrawProperty("tiltAngleLeft", "左图倾斜角度");
		DrawProperty("tiltAngleRight", "右图倾斜角度");
		DrawProperty("tiltDuration", "倾斜时长(秒)");
		DrawProperty("tiltEase", "倾斜缓动");
		DrawProperty("tiltSfx", "倾斜音效(MP3/WAV)");
		DrawProperty("tiltSfxVolume", "音量(0-1)");
		DrawProperty("audioTarget", "播放源(可选)");
#endif

		serializedObject.ApplyModifiedProperties();

		GUILayout.Space(8);
		DrawActions();
	}

	private void DrawActions()
	{
#if ODIN_INSPECTOR || SIRENIX_ODIN_INSPECTOR
		SirenixEditorGUI.Title("调试/操作", null, TextAlignment.Left, true);
		EditorGUILayout.BeginHorizontal();
		if (SirenixEditorGUI.Button("开始", ButtonSizes.Medium)) InvokeTarget(t => t.StartMove());
		if (SirenixEditorGUI.Button("重来", ButtonSizes.Medium)) InvokeTarget(t => t.RestartMove());
		EditorGUILayout.EndHorizontal();
#else
		DrawHeader("调试/操作");
		EditorGUILayout.BeginHorizontal();
		if (GUILayout.Button("开始", GUILayout.Height(24))) InvokeTarget(t => t.StartMove());
		if (GUILayout.Button("重来", GUILayout.Height(24))) InvokeTarget(t => t.RestartMove());
		EditorGUILayout.EndHorizontal();
#endif
	}

	private void DrawHeader(string title)
	{
		GUILayout.Label(title, EditorStyles.boldLabel);
	}

	private void DrawProperty(string propName, string label)
	{
		var prop = serializedObject.FindProperty(propName);
		if (prop != null)
		{
			EditorGUILayout.PropertyField(prop, new GUIContent(label), true);
		}
	}

	private void InvokeTarget(Action<GlassingClinking> call)
	{
		var mover = (GlassingClinking)target;
		if (mover == null) return;
		call(mover);
		if (!Application.isPlaying)
		{
			EditorApplication.QueuePlayerLoopUpdate();
			SceneView.RepaintAll();
		}
	}
}


