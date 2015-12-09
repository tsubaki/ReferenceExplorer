using UnityEngine;
using System.Collections;
using System;
using UnityEditor;

public class Layout : IDisposable
{

	public enum LayoutType
	{
		Horizontal,
		Vertical
	}

	private LayoutType type;

	public Layout (LayoutType layoutType, params GUILayoutOption[] options)
	{
		if (layoutType == LayoutType.Vertical)
			EditorGUILayout.BeginHorizontal (options);
		else
			EditorGUILayout.BeginVertical (options);

		type = layoutType;
	}

	public void Dispose ()
	{
		if (type == LayoutType.Vertical)
			EditorGUILayout.EndHorizontal ();
		else
			EditorGUILayout.EndVertical ();
	}
}
