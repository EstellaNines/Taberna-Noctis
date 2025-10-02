using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

// 用于在不改英文变量名的情况下，在Inspector中显示中文标签
public class InspectorCNAttribute : PropertyAttribute
{
	public readonly string label;

	public InspectorCNAttribute(string label)
	{
		this.label = label ?? string.Empty;
	}
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(InspectorCNAttribute))]
public class InspectorCNPropertyDrawer : PropertyDrawer
{
	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	{
		var cn = (InspectorCNAttribute)attribute;
		string text = string.IsNullOrEmpty(cn.label) ? label.text : cn.label;
		EditorGUI.PropertyField(position, property, new GUIContent(text), true);
	}

	public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
	{
		return EditorGUI.GetPropertyHeight(property, true);
	}
}
#endif


