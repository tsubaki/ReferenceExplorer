using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;
using terasurware;

public class ReferenceObjectWindow : EditorWindow
{

	[MenuItem("Window/Referenced/to object")]
	static void Init ()
	{
		var window = GetWindow (typeof(ReferenceObjectWindow));
		window.title = "to";
		window.Show ();
	}

	List<ReferenceObject> referenceObjectList = new List<ReferenceObject> ();
	
	void OnInspectorUpdate ()
	{
		Repaint ();
	}

	Vector2 current;

	void OnSelectionChange()
	//void Update ()
	{
		referenceObjectList.Clear ();
		SceneObjectUtility.Init ();

		SceneObjectUtility.GetReferenceObject (Selection.activeGameObject, referenceObjectList);
	}

	void OnGUI ()
	{
		GUIStyle styles = new GUIStyle ();
		styles.margin.left = 10;
		styles.margin.top = 5;

		current = EditorGUILayout.BeginScrollView (current);
		
		int preGameObjectID = 0;

		try {

			foreach (var referenceObject in referenceObjectList) {
				GameObject rootObject = referenceObject.rootComponent.gameObject;
				GameObject targetObject = null;

				if (referenceObject.value is Component)
					targetObject = ((Component)referenceObject.value).gameObject;
				if (referenceObject.value is GameObject)
					targetObject = (GameObject)referenceObject.value;

				if (preGameObjectID != rootObject.GetInstanceID ()) {
					preGameObjectID = rootObject.GetInstanceID ();
					EditorGUILayout.Space ();
					EditorGUILayout.ObjectField (rootObject, typeof(GameObject));
				}

				string msg = string.Format ("{2}.{1} -> ({0}) {3}", 
				                           referenceObject.value.GetType ().Name, 
				                           referenceObject.memberName, 
				                           referenceObject.rootComponent.GetType ().Name,
				                           targetObject.name);

	
				GUILayout.BeginHorizontal ();
				GUILayout.Label (EditorGUIUtility.ObjectContent (null, typeof(ReferenceObject)).image, GUILayout.Height (16), GUILayout.Width (16));
				GUILayout.Label (msg);
				GUILayout.EndHorizontal ();
			}
		} catch {
			referenceObjectList.Clear ();
		}

		
		EditorGUILayout.EndScrollView ();
	}

}
