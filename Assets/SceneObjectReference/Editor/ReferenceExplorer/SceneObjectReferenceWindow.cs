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
		bool isLock = false;

		Texture icon;

		GameObject selectionObject;

		[MenuItem("Window/Referenced/Scene Object Reference")]
		static void Iint()
		{
			var tfWindow = EditorWindow.GetWindow<SceneObjectReferenceWindow>();
			tfWindow.Show();
		}

		void OnEnable()
		{
			SceneView.onSceneGUIDelegate -= to.OnSceneGUI;
			SceneView.onSceneGUIDelegate += to.OnSceneGUI;

			SceneView.onSceneGUIDelegate -= from.OnSceneGUI;
			SceneView.onSceneGUIDelegate += from.OnSceneGUI;

		}

		void OnDisable()
		{
			SceneView.onSceneGUIDelegate -= from.OnSceneGUI;
			SceneView.onSceneGUIDelegate -= from.OnSceneGUI;
		}

		void OnSelectionChange ()
		{
			if( isLock )
				return;

			SceneObjectUtility.UpdateGlovalReferenceList ();


			from.OnSelectionChange();
			to.OnSelectionChange();

			selectionObject = Selection.activeGameObject;
			icon = EditorGUIUtility.Load("Icons/Generated/PrefabNormal Icon.asset") as Texture2D;
		}

		void OnInspectorUpdate ()
		{
			SceneObjectUtility.UpdateReferenceList();
			Repaint ();
		}

		SceneObjectReferenceWindow()
		{
			from = new FromObjectReferenceWindow();
			to = new ToReferenceWindow();
		}
		
		void OnGUI()
		{
			try{
				EditorGUI.BeginChangeCheck();
				
				EditorGUILayout.BeginHorizontal();
				{
					ignoreSelfReference = GUILayout.Toggle(ignoreSelfReference, "ignore self", EditorStyles.toolbarButton, GUILayout.Width(70));
					isLock =  GUILayout.Toggle(isLock, "Lock", EditorStyles.toolbarButton, GUILayout.Width(45));

					if( GUILayout.Button("select child", EditorStyles.toolbarButton, GUILayout.Width(70) ) )
					{
						var objs = Selection.activeGameObject.transform.GetComponentsInChildren<Transform>();
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
				isLock = false;
			}
		}
	}
}

