using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;
using terasurware;

public class ToReferenceWindow : EditorWindow
{

	[MenuItem("Window/Referenced/to object")]
	static void Init ()
	{
		var window = GetWindow (typeof(ToReferenceWindow));
		window.title = "to";
		window.Show ();
	}

	List<ReferenceObject> referenceObjectList = new List<ReferenceObject> ();
	
	void OnInspectorUpdate ()
	{
		Repaint ();
	}
	
	
	void OnEnable()
	{
		SceneView.onSceneGUIDelegate -= OnSceneGUI;
		SceneView.onSceneGUIDelegate += OnSceneGUI;
	}
	
	void OnDisable()
	{
		SceneView.onSceneGUIDelegate -= OnSceneGUI;
	}
		
	Vector2 current;

	void OnSelectionChange()
	//void Update ()
	{
		referenceObjectList.Clear ();
		SceneObjectUtility.Init ();

		SceneObjectUtility.GetReferenceObject (Selection.activeGameObject, referenceObjectList);
	}
	
	void OnSceneGUI(SceneView sceneView)
	{
		var selection = Selection.activeGameObject as GameObject;
		if( selection == null)
			return;

		foreach( var target in referenceObjectList )
		{
			var obj = SceneObjectUtility.GetGameObject(target.value);
			if( obj == null)
			{
				Debug.Log("null");
				continue;
			}
			
			

			
			var startPosition = selection.transform.position;
			var endPosition = obj.transform.position;
			
			if( startPosition == endPosition )
				continue;
			
			Handles.color = Color.red;
			Handles.DrawLine(startPosition, endPosition);
		}
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

				if (preGameObjectID != referenceObject.rootComponent.GetInstanceID ()) {
					preGameObjectID = referenceObject.rootComponent.GetInstanceID ();
					EditorGUILayout.Space ();
					GUILayout.Label(referenceObject.rootComponent.GetType().Name);
				}

				string msg = string.Format ("( {1} ) {0} ",referenceObject.memberName, referenceObject.value.GetType().Name);

				GUILayout.BeginHorizontal ();
					EditorGUILayout.LabelField(msg);
					EditorGUILayout.ObjectField((Object)referenceObject.value, referenceObject.value.GetType(), true);
				GUILayout.EndHorizontal ();
			}
		} catch {
			referenceObjectList.Clear ();
		}

		
		EditorGUILayout.EndScrollView ();
	}

}
