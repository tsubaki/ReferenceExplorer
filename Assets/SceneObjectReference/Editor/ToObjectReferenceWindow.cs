using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;
using terasurware;

public class ToObjectReferenceWindow :  EditorWindow 
{
	Vector2 current;

	List<ReferenceObject> referenceObjectList = new List<ReferenceObject>();


	[MenuItem("Window/Referenced/for object")]
	static void Init () {
		var window = GetWindow( typeof(ToObjectReferenceWindow));
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

		referenceObjectList.Sort( (x, y) => GetObjectID(x.rootComponent) - GetObjectID(y.rootComponent) );

	}


	int GetObjectID(object obj)
	{
		if (obj is Component)
			return ((Component)obj).GetInstanceID();
		if (obj is GameObject)
			return ((GameObject)obj).GetInstanceID();

		return -1;
	}
	
	void OnGUI () {	
		GUIStyle styles = new GUIStyle();
		styles.margin.left = 10;
		styles.margin.top = 5;

		current = EditorGUILayout.BeginScrollView(current);

		int preGameObjectID = 0;

		try {
			
			foreach (var referenceObject in referenceObjectList) {

				int currentObjectID  = GetObjectID(referenceObject.rootComponent.gameObject);
				if (preGameObjectID != currentObjectID) {
					preGameObjectID = currentObjectID;
					EditorGUILayout.Space ();
					EditorGUILayout.ObjectField(referenceObject.rootComponent.gameObject, 
					                            typeof(GameObject), false);
				}

				string msg = string.Format("  ({2}) {0} . {1}", 
				                referenceObject.rootComponent.GetType ().Name,
				                referenceObject.memberName,
				                referenceObject.value.GetType().Name);

				EditorGUILayout.LabelField(msg);
			}
		} catch {
			referenceObjectList.Clear ();
		}


		EditorGUILayout.EndScrollView();
	}

}
