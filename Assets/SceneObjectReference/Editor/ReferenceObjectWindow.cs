using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;
using terasurware;


public class ReferenceObjectWindow : EditorWindow {

	[MenuItem("Window/Referenced/to object")]
	static void Init () {
		var window = GetWindow( typeof(ReferenceObjectWindow));
		window.Show();
	}

	List<ReferenceObject> refObjectList = new List<ReferenceObject>();
	
	void OnInspectorUpdate () {
		Repaint ();
	}

	Vector2 current;


	//void OnSelectionChange()
	void Update()
	{
		refObjectList.Clear();
		SceneObjectUtility.GetReferenceObject( Selection.activeGameObject, refObjectList) ;
	}

	void OnGUI () {
		GUIStyle styles = new GUIStyle();
		styles.margin.left = 10;
		styles.margin.top = 5;

		current = EditorGUILayout.BeginScrollView(current);
		
		int preGameObjectID = 0;
		
		foreach( var refObject in refObjectList)
		{
			if( preGameObjectID != refObject.rootObject.GetInstanceID())
			{
				preGameObjectID = refObject.rootObject.GetInstanceID();
				EditorGUILayout.Space();
				EditorGUILayout.ObjectField( refObject.value, refObject.valueType);
			}
			
			string msg = string.Format("{2}.{1} -> {0}", refObject.value.GetType().Name, refObject.fieldName, refObject.valueType.Name);
			
			GUILayout.BeginHorizontal();
			GUILayout.Label(EditorGUIUtility.ObjectContent(null, typeof(ReferenceObject )).image, GUILayout.Height(16), GUILayout.Width(16));
			GUILayout.Label(msg);
			GUILayout.EndHorizontal();
		}
		
		EditorGUILayout.EndScrollView();
	}
}
