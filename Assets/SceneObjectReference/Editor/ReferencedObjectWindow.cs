using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;
using terasurware;

public class ReferencedObjectWindow :  EditorWindow 
{
	Vector2 current;

	List<ReferenceObject> referenceObjectList = new List<ReferenceObject>();


	[MenuItem("Window/Referenced/for object")]
	static void Init () {
		var window = GetWindow( typeof(ReferencedObjectWindow));
		window.title = "from";
		window.Show();
	}

	void OnEnable()
	{
		SceneObjectUtility.UpdateGlovalReferenceList();
	}
	
	void OnInspectorUpdate () {
		Repaint ();
	}

	void OnHierarchyChange()
	{
		SceneObjectUtility.UpdateGlovalReferenceList();
	}

	void OnSelectionChange()
	//void Update()
	{
		referenceObjectList.Clear();
		SceneObjectUtility.UpdateGlovalReferenceList();
		SceneObjectUtility.FindReferenceObject( Selection.activeGameObject, referenceObjectList) ;
	}
	
	void OnGUI () {	
		GUIStyle styles = new GUIStyle();
		styles.margin.left = 10;
		styles.margin.top = 5;

		current = EditorGUILayout.BeginScrollView(current);

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
					EditorGUILayout.ObjectField (referenceObject.rootComponent.gameObject , referenceObject.GetType());
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


		EditorGUILayout.EndScrollView();
	}

}
