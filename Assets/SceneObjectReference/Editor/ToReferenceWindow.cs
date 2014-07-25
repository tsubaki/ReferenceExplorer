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
		
		
		var cameraTransform =  SceneView.currentDrawingSceneView.camera.transform;
		var rotate = cameraTransform.rotation;
		var cameraPos = cameraTransform.position;
		
		Color shadowCol = new Color(0.5f, 0, 0, 0.06f);
		
		foreach( var target in referenceObjectList )
		{
			var obj = SceneObjectUtility.GetGameObject(target.value);
			if( obj == null)
			{
				continue;
			}
			if( obj == Selection.activeGameObject){
				continue;
			}
			
			
			var startPosition = selection.transform.position;
			var endPosition = obj.transform.position;
			
			var size = Vector3.Distance(endPosition, cameraPos) * 0.02f;
			
			if( startPosition == endPosition )
				continue;
			
			Handles.color = Color.red;
			
			var diffPos = startPosition - endPosition;
			var tan = new Vector3(diffPos.y, diffPos.x, diffPos.z);
			
			
			var startTan = startPosition;
			 var endTan = endPosition  + tan * 0.4f;
			
			Handles.CircleCap(1, endPosition, rotate, size);

			for(int i=0; i<3; i++)
				Handles.DrawBezier(startPosition, endPosition, startTan, endTan, shadowCol, null, (i + 1) * 5);
			Handles.DrawBezier(startPosition, endPosition, startTan, endTan, Color.red, null, 1);
			
			Handles.Label(endPosition, obj.name);
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
