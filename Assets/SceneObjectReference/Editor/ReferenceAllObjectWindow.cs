using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;
using terasurware;

public class ReferenceAllObjectWindow : EditorWindow
{

	[MenuItem("Window/Referenced/All")]
	static void Init ()
	{
		var window = GetWindow (typeof(ReferenceAllObjectWindow));
		window.Show ();
	}

	List<ReferenceObject> refObjectList = new List<ReferenceObject> ();
	Vector2 current = new Vector2 ();

	void OnInspectorUpdate ()
	{
		Repaint ();
	}

	List<GameObject> allObject;
	bool isHiding = false;

	void ParentShow (Transform parent)
	{
		if (parent != null) {
			parent.gameObject.hideFlags = HideFlags.None;
			ParentShow (parent.parent);
		}
	}

	void OnDestroy ()
	{
		ShowAllObject ();
	}

	void HideNoCommunication ()
	{
		isHiding = true;


		UpdateAllObject ();
		UpdateList ();
		foreach (var obj in allObject) {
			obj.hideFlags = HideFlags.HideInHierarchy;
		}
		
		foreach (var item in refObjectList) {
			ParentShow (item.rootObject.transform);
			ParentShow (item.thisObject.transform);
		}
	}
	
	void ShowAllObject ()
	{
		isHiding = false;

		UpdateAllObject ();

		foreach (var obj in allObject) {
			obj.hideFlags = HideFlags.None;
		}
	}

	void Update ()
	{

		if (EditorApplication.isPaused || !EditorApplication.isPlaying) {
			UpdateAllObject ();
			UpdateList ();
		}
	}

	void UpdateAllObject ()
	{
		allObject = SceneObjectUtility.GetAllObjectsInScene (false);
	}

	void UpdateList ()
	{
		refObjectList.Clear ();

		
		foreach (var obj in allObject) {
			SceneObjectUtility.GetReferenceObject (obj, refObjectList);
		}
		refObjectList.Sort ((x,y) => {
			return x.thisObject.GetInstanceID () - y.thisObject.GetInstanceID (); });
	}
	
	void OnGUI ()
	{	

		if (EditorApplication.isPlaying && !EditorApplication.isPaused) {
			if (GUILayout.Button ("pause")) {
				EditorApplication.isPaused = true;
			}
			return;
		}

		if (isHiding == false && GUILayout.Button ("hide")) {
			HideNoCommunication ();
		}

		if (isHiding == true && GUILayout.Button ("show")) {
			ShowAllObject ();
		}

		GUIStyle styles = new GUIStyle ();
		styles.margin.left = 10;
		styles.margin.top = 5;
		
		current = EditorGUILayout.BeginScrollView (current);
		
		int preGameObjectID = 0;
		
		foreach (var refObject in refObjectList) {
			try {

				if (preGameObjectID != refObject.thisObject.GetInstanceID ()) {
					preGameObjectID = refObject.thisObject.GetInstanceID ();
					EditorGUILayout.Space ();
					EditorGUILayout.ObjectField (refObject.thisObject, refObject.thisObject.GetType ());
				}
			
				string msg = string.Format ("{2}.{1} -> ({0}){4}", 
			                           refObject.value.GetType ().Name, 
			                           refObject.fieldName, 
			                           refObject.valueType.Name, 
			                           refObject.thisObject.name, 
			                           refObject.value.name);
			
				GUILayout.BeginHorizontal ();
				GUILayout.Label (EditorGUIUtility.ObjectContent (null, refObject.valueType).image, GUILayout.Height (16), GUILayout.Width (16));
				if (GUILayout.Button (msg, styles)) {
					EditorGUIUtility.PingObject (refObject.value);

					Selection.activeGameObject = refObject.thisObject;
				}
				GUILayout.EndHorizontal ();
			} catch (MissingReferenceException) {
			} catch (MissingComponentException) {
			} catch (UnassignedReferenceException){
			}
		}
		
		EditorGUILayout.EndScrollView ();
	}
}
