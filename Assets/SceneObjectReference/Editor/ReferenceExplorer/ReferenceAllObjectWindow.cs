using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;

namespace ReferenceExplorer
{
	public class ReferenceAllObjectWindow : EditorWindow
	{
		List<ReferenceObject> refObjectList = new List<ReferenceObject> ();
		Vector2 current = new Vector2 ();
		
		[MenuItem("Window/Referenced/All Reference Objects")]
		static void Init ()
		{
			var window = GetWindow (typeof(ReferenceAllObjectWindow));
			window.title = "all";
			window.Show ();
		}
		
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
				
				ParentShow (item.referenceComponent.transform);
				if (item.value == null)
					continue;
				
				var obj = SceneObjectUtility.GetGameObject (item.value);
				
				if (obj != null)
					ParentShow (obj.transform);
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
				return x.referenceComponent.GetInstanceID () - y.referenceComponent.GetInstanceID (); });
		}
		
		void OnGUI ()
		{	
			EditorGUILayout.BeginHorizontal ();
			
			if (GUILayout.Button ("update", EditorStyles.toolbarButton, GUILayout.Width (Screen.width / 2))) {
				UpdateAllObject ();
				UpdateList ();
				
				if (EditorApplication.isPlaying == true)
					EditorApplication.isPaused = true;
			}
			
			if (EditorApplication.isPlaying && !EditorApplication.isPaused) {
				if (GUILayout.Button ("pause", EditorStyles.toolbarButton, GUILayout.Width (Screen.width / 2))) {
					EditorApplication.isPaused = true;
					UpdateList ();
				}
				return;
			}
			
			
			if (isHiding == false && GUILayout.Button ("hide", EditorStyles.toolbarButton, GUILayout.Width (Screen.width / 2))) {
				HideNoCommunication ();
			}
			
			if (isHiding == true && GUILayout.Button ("show", EditorStyles.toolbarButton, GUILayout.Width (Screen.width / 2))) {
				ShowAllObject ();
			}
			
			EditorGUILayout.EndHorizontal ();
			
			GUIStyle styles = new GUIStyle ();
			styles.margin.left = 10;
			styles.margin.top = 5;
			
			current = EditorGUILayout.BeginScrollView (current);
			
			int preGameObjectID = 0;
			
			
			try {
				
				List<Component> comps = new List<Component> ();
				foreach (var referenceObject in refObjectList) {
					if (! comps.Contains (referenceObject.referenceComponent)) {
						comps.Add (referenceObject.referenceComponent);
					}
				}

				for (int i=0; i<refObjectList.Count; i++) {
					
					var referenceObject = refObjectList [i];
					GameObject targetObject = null;
					
					if (referenceObject.value is Component)
						targetObject = ((Component)referenceObject.value).gameObject;
					if (referenceObject.value is GameObject)
						targetObject = (GameObject)referenceObject.value;
					
					EditorGUILayout.BeginHorizontal ("box", GUILayout.Width (Screen.width - 12));

					EditorGUILayout.BeginVertical();

					EditorGUILayout.ObjectField (
						referenceObject.referenceComponent.gameObject, 
						referenceObject.GetType (),
						GUILayout.Width (100));
					preGameObjectID = refObjectList [i].referenceComponent.GetInstanceID ();

					EditorGUILayout.BeginHorizontal();

					if( referenceObject.referenceComponent is MonoBehaviour )
						{
							var comp = MonoScript.FromMonoBehaviour((MonoBehaviour) referenceObject.referenceComponent  );
							EditorGUILayout.ObjectField(comp, typeof(MonoScript));
						}else{
							EditorGUILayout.LabelField( referenceObject.referenceComponent.GetType().Name );
						}



					EditorGUILayout.BeginVertical ();
					
					for (; i< refObjectList.Count && preGameObjectID == refObjectList[i].referenceComponent.GetInstanceID(); i++) {
						preGameObjectID = refObjectList [i].referenceComponent.GetInstanceID ();
						
						referenceObject = refObjectList [i];
						GameObject rootObject = referenceObject.referenceComponent.gameObject;
						targetObject = null;
						
						if (referenceObject.value is Component)
							targetObject = ((Component)referenceObject.value).gameObject;
						if (referenceObject.value is GameObject)
							targetObject = (GameObject)referenceObject.value;
						
	
						EditorGUILayout.BeginHorizontal();

						EditorGUILayout.LabelField(referenceObject.referenceMemberName, GUILayout.ExpandWidth(false));

						EditorGUILayout.LabelField("->", GUILayout.Width(16));

						EditorGUILayout.ObjectField(referenceObject.value as Object, referenceObject.value.GetType());
						EditorGUILayout.EndHorizontal();

					}
					
					GUILayout.Space (5);
					
					EditorGUILayout.EndVertical ();

					EditorGUILayout.EndHorizontal();

					EditorGUILayout.EndVertical();

					EditorGUILayout.EndHorizontal ();
				}
			} catch (UnityEngine.ExitGUIException e) {
				Debug.LogWarning (e.ToString ());
			} catch (System.Exception e) {
				Debug.LogWarning (e.ToString ());
				refObjectList.Clear ();
			}
			
			EditorGUILayout.EndScrollView ();
		}
	}
}


