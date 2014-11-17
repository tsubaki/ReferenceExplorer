using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;

namespace ReferenceExplorer
{
	public class SceneObjectReferenceWindow : EditorWindow{
		
		FromObjectReferenceWindow from;
		ToReferenceWindow to;

		bool ignoreSelfReference = true;

		Texture icon;
		bool isHiding = false;
		GameObject selectionObject;

		
		SceneObjectReferenceWindow()
		{
			from = new FromObjectReferenceWindow();
			to = new ToReferenceWindow();
			
			EditorApplication.playmodeStateChanged -= PlayModeChange;
			EditorApplication.playmodeStateChanged += PlayModeChange;
		}


		[MenuItem("Window/Referenced/Scene Object Reference")]
		static void Iint()
		{
			var tfWindow = EditorWindow.GetWindow<SceneObjectReferenceWindow>();
			tfWindow.Show();
		}

		void OnDestroy()
		{
			EditorApplication.playmodeStateChanged -= PlayModeChange;
			AppearUnreferenceObjects();
		}

		void OnEnable()
		{
			SceneView.onSceneGUIDelegate -= to.OnSceneGUI;
			SceneView.onSceneGUIDelegate += to.OnSceneGUI;

			SceneView.onSceneGUIDelegate -= from.OnSceneGUI;
			SceneView.onSceneGUIDelegate += from.OnSceneGUI;

		}

		void OnFocus()
		{
			SceneObjectUtility.UpdateGlovalReferenceList ();
		}

		void OnDisable()
		{
			SceneView.onSceneGUIDelegate -= from.OnSceneGUI;
			SceneView.onSceneGUIDelegate -= from.OnSceneGUI;
		}

		void OnHierarchyChange()
		{
			if( EditorApplication.isPlaying == false)
				SceneObjectUtility.UpdateGlovalReferenceList ();
		}

		void OnSelectionChange ()
		{
			from.OnSelectionChange();
			to.OnSelectionChange();

			selectionObject = Selection.activeGameObject;
			icon = EditorGUIUtility.Load("Icons/Generated/PrefabNormal Icon.asset") as Texture2D;
		}



		void OnInspectorUpdate ()
		{
			Repaint ();
		}

		void PlayModeChange()
		{
			if( EditorApplication.isPaused || EditorApplication.isPlaying == false)
					SceneObjectUtility.UpdateGlovalReferenceList ();
			if( isHiding == true )
			{
				AppearUnreferenceObjects();
				DisappearUnreferenceObjects();
			}else{
				DisappearUnreferenceObjects();
				AppearUnreferenceObjects();
			}
		}
		
		void OnGUI()
		{
			try{
				EditorGUI.BeginChangeCheck();
				
				EditorGUILayout.BeginHorizontal();
				{
					ignoreSelfReference = GUILayout.Toggle(ignoreSelfReference, "ignore self", EditorStyles.toolbarButton, GUILayout.Width(70));

					if (isHiding == false && GUILayout.Button ("Disappear", EditorStyles.toolbarButton, GUILayout.Width (80))) {
						DisappearUnreferenceObjects ();
					}
					
					if (isHiding == true && GUILayout.Button ("Appear", EditorStyles.toolbarButton, GUILayout.Width (80))) {
						AppearUnreferenceObjects ();
					}
					


					if( GUILayout.Button("select child", EditorStyles.toolbarButton, GUILayout.Width(70) ) )
					{
						var objs = Selection.activeGameObject.transform.GetComponentsInChildren<Transform>(true);
						List<GameObject> objList = new List<GameObject>();
						foreach( var comp in objs )
							objList.Add(comp.gameObject);

						Selection.objects = objList.ToArray();
					}


					var iconSize = EditorGUIUtility.GetIconSize();
					EditorGUIUtility.SetIconSize(Vector2.one * 16);

					if( selectionObject != null )
						if( GUILayout.Button(icon, GUIStyle.none, GUILayout.Width(18)) || GUILayout.Button(selectionObject.name, GUIStyle.none))
							Selection.activeGameObject = selectionObject;
					EditorGUIUtility.SetIconSize(iconSize);
				}
				EditorGUILayout.EndHorizontal();
				
				GUILayout.Space(5);
				
				
				if( EditorGUI.EndChangeCheck() )
				{
					from.ignoreSelfReference = ignoreSelfReference;
					to.ignoreSelfReference = ignoreSelfReference;
					
					OnSelectionChange();
				}
				
				EditorGUILayout.BeginHorizontal();
				
				EditorGUILayout.BeginVertical(GUILayout.Width( Screen.width * 0.5f));
				from.OnGUI();
				EditorGUILayout.EndVertical();
				
				EditorGUILayout.BeginVertical(GUILayout.Width( Screen.width * 0.5f));
				to.OnGUI();
				EditorGUILayout.EndVertical();
				
				EditorGUILayout.EndHorizontal();
			}catch{
			}
		}

		void DisappearUnreferenceObjects ()
		{
			isHiding = true;

			var refObjectList = new List<ReferenceObject>();
			var allObject = SceneObjectUtility.GetAllObjectsInScene (false);

			foreach (var obj in allObject) {
				obj.hideFlags = HideFlags.HideInHierarchy;
				SceneObjectUtility.GetReferenceObject (obj, refObjectList);
			}
			refObjectList.Sort ((x,y) => {	return x.referenceComponent.GetInstanceID () - y.referenceComponent.GetInstanceID (); });

			foreach (var item in refObjectList) {
				
				ParentShow (item.referenceComponent.transform);
				if (item.value == null)
					continue;
				
				var obj = SceneObjectUtility.GetGameObject (item.value);
				
				if (obj != null)
					ParentShow (obj.transform);
			}
		}

		void AppearUnreferenceObjects ()
		{
			isHiding = false;
			var allObject = SceneObjectUtility.GetAllObjectsInScene (false);
			
			foreach (var obj in allObject) {
				obj.hideFlags = HideFlags.None;
			}
		}

		void ParentShow (Transform parent)
		{
			if (parent != null) {
				parent.gameObject.hideFlags = HideFlags.None;
				ParentShow (parent.parent);
			}
		}
	}
}

