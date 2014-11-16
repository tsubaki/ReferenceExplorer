using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;

namespace ReferenceExplorer
{
	public class CameraOrderWindow : EditorWindow {

		[MenuItem("Window/Referenced/Camera order")]
		static void Init()
		{
			var window = CameraOrderWindow.GetWindow<CameraOrderWindow>("camera order");
			window.Show();
		}

		void OnFocus()
		{
			UpdateCameras();
		}

		void OnHierarchyChange()
		{
			UpdateCameras();
			Repaint();
		}

		void OnInspectorUpdate ()
		{
			SortCamera();
			Repaint();
		}

		void UpdateCameras()
		{
			allCameras.Clear();
		 	var allObject = SceneObjectUtility.GetAllObjectsInScene( false );
			foreach( var obj in allObject )
			{
				var camera = obj.GetComponent<Camera>();
				if( camera != null )
					allCameras.Add(camera);
			}

			SortCamera();
		}

		void SortCamera()
		{
			allCameras.Sort( (x, y) => {	return (int)(1000 * x.depth) - (int)(1000 * y.depth); });
		}

		List<Camera> allCameras = new List<Camera>();
		Vector2 current;

		void OnGUI()
		{
			current = EditorGUILayout.BeginScrollView(current);

			EditorGUILayout.BeginHorizontal("box");
			EditorGUILayout.LabelField("camera order");
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginVertical("box");
			foreach( var camera in allCameras )
			{
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.LabelField( camera.depth.ToString(), GUILayout.MaxWidth(50), GUILayout.MinWidth(20));
				EditorGUILayout.ObjectField(camera.gameObject, typeof(GameObject) );
				EditorGUILayout.EndHorizontal();
			}
			EditorGUILayout.EndVertical();

			EditorGUILayout.EndScrollView();
		}
	}
}
