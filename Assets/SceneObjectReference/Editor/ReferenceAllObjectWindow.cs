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
		window.title = "all";
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

				ParentShow (item.rootComponent.transform);
				if (item.value == null)
					continue;
				
				var obj = SceneObjectUtility.GetGameObject(item.value);
				
				if( obj != null )
					ParentShow(obj.transform);
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

	/*
	 * void OnSelectionChange ()
	// void Update()
	{

		if (EditorApplication.isPaused || !EditorApplication.isPlaying) {
			UpdateAllObject ();
			UpdateList ();
		}
	}
	*/

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
			return x.rootComponent.GetInstanceID () - y.rootComponent.GetInstanceID (); });
	}
	
	void OnGUI ()
	{	
		EditorGUILayout.BeginHorizontal();

		if (GUILayout.Button ("update", EditorStyles.toolbarButton, GUILayout.Width(Screen.width / 2))) {
			UpdateAllObject ();
			UpdateList ();

			if( EditorApplication.isPlaying == true )
				EditorApplication.isPaused = true;
		}

		if (EditorApplication.isPlaying && !EditorApplication.isPaused) {
			if (GUILayout.Button ("pause",EditorStyles.toolbarButton, GUILayout.Width(Screen.width / 2))) {
				EditorApplication.isPaused = true;
				UpdateList ();
			}
			return;
		}


		if (isHiding == false && GUILayout.Button ("hide", EditorStyles.toolbarButton, GUILayout.Width(Screen.width / 2))) {
			HideNoCommunication ();
		}

		if (isHiding == true && GUILayout.Button ("show", EditorStyles.toolbarButton, GUILayout.Width(Screen.width / 2))) {
			ShowAllObject ();
		}

		EditorGUILayout.EndHorizontal();

		GUIStyle styles = new GUIStyle ();
		styles.margin.left = 10;
		styles.margin.top = 5;
		
		current = EditorGUILayout.BeginScrollView (current);
		
		int preGameObjectID = 0;
		
		
		try {


			bool isHolizon = false;

			for( int i=0; i<refObjectList.Count; i++)
			{

				var referenceObject = refObjectList[i];
				GameObject rootObject = referenceObject.rootComponent.gameObject;
				GameObject targetObject = null;
				
				if (referenceObject.value is Component)
					targetObject = ((Component)referenceObject.value).gameObject;
				if (referenceObject.value is GameObject)
					targetObject = (GameObject)referenceObject.value;

				EditorGUILayout.BeginHorizontal("box", GUILayout.Width(Screen.width  - 12));

				EditorGUILayout.ObjectField (
					referenceObject.rootComponent.gameObject, 
					referenceObject.GetType (),
					GUILayout.Width(100));
				preGameObjectID = refObjectList[i].rootComponent.GetInstanceID();

				EditorGUILayout.BeginVertical();

				for(; i< refObjectList.Count && preGameObjectID == refObjectList[i].rootComponent.GetInstanceID(); i++)
				{
					preGameObjectID = refObjectList[i].rootComponent.GetInstanceID();

					referenceObject = refObjectList[i];
					rootObject = referenceObject.rootComponent.gameObject;
					targetObject = null;
					
					if (referenceObject.value is Component)
						targetObject = ((Component)referenceObject.value).gameObject;
					if (referenceObject.value is GameObject)
						targetObject = (GameObject)referenceObject.value;

					string msg  = string.Empty;

					try{
						msg = string.Format ("{2}.{1} -> ({0}) {3}", 
					                            referenceObject.value.GetType ().Name, 
					                            referenceObject.memberName, 
					                            referenceObject.rootComponent.GetType ().Name,
					                            targetObject.name);
					}catch{
						msg = "";
					}
					if (GUILayout.Button (msg, styles)) {
						EditorGUIUtility.PingObject (targetObject);
					}

				}

				GUILayout.Space(5);

				EditorGUILayout.EndVertical();

				EditorGUILayout.EndHorizontal();
			}

			/*
			foreach (var referenceObject in refObjectList) {

				try {

					GameObject rootObject = referenceObject.rootComponent.gameObject;
					GameObject targetObject = null;
				
					if (referenceObject.value is Component)
						targetObject = ((Component)referenceObject.value).gameObject;
					if (referenceObject.value is GameObject)
						targetObject = (GameObject)referenceObject.value;


					if (preGameObjectID != rootObject.GetInstanceID ()) {
						preGameObjectID = rootObject.GetInstanceID ();
						EditorGUILayout.Space ();
						EditorGUILayout.ObjectField (referenceObject.rootComponent.gameObject, referenceObject.GetType ());
					}



				
					string msg = string.Format ("{2}.{1} -> ({0}) {3}", 
				                            referenceObject.value.GetType ().Name, 
				                            referenceObject.memberName, 
				                            referenceObject.rootComponent.GetType ().Name,
				                            targetObject.name);
				
					GUILayout.BeginHorizontal ();

					GUILayout.Label (EditorGUIUtility.ObjectContent (null, typeof(ReferenceObject)).image, GUILayout.Height (16), GUILayout.Width (16));
					if (GUILayout.Button (msg, styles)) {
						EditorGUIUtility.PingObject (targetObject);
					}
					GUILayout.EndHorizontal ();
				} catch (UnassignedReferenceException) {
				} catch (MissingReferenceException){}
			}
			*/
		}catch (UnityEngine.ExitGUIException e){
			Debug.LogWarning (e.ToString ());
		} catch (System.Exception e) {
			Debug.LogWarning (e.ToString ());
			refObjectList.Clear ();
		}

		EditorGUILayout.EndScrollView ();
	}
}
