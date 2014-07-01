using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;
using terasurware;

public class ReferencedObjectWindow :  EditorWindow 
{
	Vector2 current;

	List<ReferenceObject> refObjectList = new List<ReferenceObject>();


	[MenuItem("Window/Referenced/for object")]
	static void Init () {
		var window = GetWindow( typeof(ReferencedObjectWindow));
		window.Show();
	}
	
	void OnInspectorUpdate () {
		Repaint ();
	}


	void OnSelectionChange()
	//void Update()
	{
		refObjectList.Clear();
		SceneObjectUtility.FindReferencedObject( Selection.activeGameObject, refObjectList) ;
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

			string msg = string.Format("{2} <- {0}.{1}", refObject.value.GetType().Name, refObject.fieldName, refObject.valueType.Name);

			GUILayout.BeginHorizontal();
			GUILayout.Label(EditorGUIUtility.ObjectContent(null,refObject.valueType).image, GUILayout.Height(16), GUILayout.Width(16));
			GUILayout.Label(msg);
			GUILayout.EndHorizontal();
		}

		EditorGUILayout.EndScrollView();
	}
}
