using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;
using terasurware;

public class FromObjectReferenceWindow :  EditorWindow 
{
	Vector2 current;

	List<ReferenceObject> referenceObjectList = new List<ReferenceObject>();


	[MenuItem("Window/Referenced/from object")]
	static void Init () {
		var window = GetWindow( typeof(FromObjectReferenceWindow));
		window.title = "from";
		window.Show();
	}

	void OnEnable()
	{
		SceneObjectUtility.UpdateGlovalReferenceList();

		SceneView.onSceneGUIDelegate -= OnSceneGUI;
		SceneView.onSceneGUIDelegate += OnSceneGUI;
	}
	
	void OnDisable()
	{
		SceneView.onSceneGUIDelegate -= OnSceneGUI;
	}
	
	void OnInspectorUpdate () {
		Repaint ();
	}

	void OnHierarchyChange()
	{
		SceneObjectUtility.UpdateGlovalReferenceList();
	}
	
	
	void OnSelectionChange()
	{
		referenceObjectList.Clear();
		SceneObjectUtility.UpdateGlovalReferenceList();
		SceneObjectUtility.FindReferenceObject( Selection.activeGameObject, referenceObjectList) ;

		referenceObjectList.Sort( (x, y) => GetObjectID(x.rootComponent) - GetObjectID(y.rootComponent) );

	}
	
	void OnSceneGUI(SceneView sceneView)
	{
		var selection = Selection.activeGameObject as GameObject;
		if( selection == null)
			return;
		
		var cameraTransform =  SceneView.currentDrawingSceneView.camera.transform;
		var rotate = cameraTransform.rotation;
		var cameraPos = cameraTransform.position;
		
		Color shadowCol = new Color(0, 0, 0.5f, 0.06f);
		
		foreach( var refs in referenceObjectList)
		{
			var obj = SceneObjectUtility.GetGameObject(refs.rootComponent);
			
			var startPosition = selection.transform.position;
			var endPosition = obj.transform.position;

			var size = Vector3.Distance(endPosition, cameraPos) * 0.02f;
			
			if( startPosition == endPosition )
				continue;
			
			Handles.color = Color.blue;
			
			var diffPos = startPosition - endPosition;
			var tan = new Vector3(diffPos.y, diffPos.x, diffPos.z);
			
			
			var startTan = startPosition ;
			 var endTan = endPosition + tan * 0.4f;
			
			Handles.CircleCap(1, endPosition, rotate, size);

			for(int i=0; i<3; i++)
				Handles.DrawBezier(startPosition, endPosition, startTan, endTan, shadowCol, null, (i + 1) * 5);
			Handles.DrawBezier(startPosition, endPosition, startTan, endTan, Color.blue, null, 1);
			Handles.Label(endPosition, obj.name);
		}
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
