using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;

#pragma warning disable 0618

namespace ReferenceExplorer
{
	public class CameraOrderWindow : EditorWindow {

		public enum OrderType
		{
			CameraOrder,
			SpriteOrder
		}

		public OrderType orderType;

		[MenuItem("Window/ReferenceExplorer/Renderer Order")]
		static void Init()
		{
			var window = CameraOrderWindow.GetWindow<CameraOrderWindow>("order");
			window.Show();
		}

		void OnFocus()
		{
			UpdateCameras();
			SortLayers();
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


		void SortLayers()
		{
			layers.Clear();
			SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);

			var sortinglayer = tagManager.FindProperty("m_SortingLayers");

			for(int i=0; i<sortinglayer.arraySize; i++)
			{
				var tag = sortinglayer.GetArrayElementAtIndex(i);
				layers.Add( new SortingLayerWithObject(){ tagName = tag.displayName });
			}
			SceneObjectUtility.CollectionAllComponent();
			foreach( var renderer in FindObjectsOfType<SpriteRenderer>())
			{
				if( renderer.gameObject.name.IndexOf(saerchText) == -1 )
					continue;

				var sortingLayerName =  string.IsNullOrEmpty( renderer.sortingLayerName ) ? "Default" : renderer.sortingLayerName ;
				var tag = layers.Find( t => t.tagName == sortingLayerName);

				Debug.Log(renderer.gameObject.name + "/" + renderer.sortingLayerName );

				tag.rendererList.Add( renderer);
			}

			foreach( var layer in layers )
			{
				layer.rendererList.Sort( (x,y) =>  x.sortingOrder - y.sortingOrder);
			}
		}

		List<SortingLayerWithObject> layers = new List<SortingLayerWithObject>();
		List<Camera> allCameras = new List<Camera>();

		string saerchText = string.Empty;
		Vector2 current;

		void OnGUISpriteOrder()
		{
			EditorGUILayout.BeginHorizontal("box");
			
			EditorGUI.BeginChangeCheck();
			EditorGUILayout.LabelField("Search", GUILayout.Width(60));
			saerchText = EditorGUILayout.TextField(saerchText);

			EditorGUILayout.EndHorizontal();
			
			current = EditorGUILayout.BeginScrollView(current);
			
			
			
			foreach( var layer in layers )
			{
				EditorGUI.indentLevel = 0;
				layer.isOpen = EditorGUILayout.Foldout (layer.isOpen, layer.tagName); 
				if( layer.isOpen )
				{
					EditorGUI.indentLevel = 1;
					foreach( var obj in layer.rendererList )
					{
						if( obj == null )
							continue;
						EditorGUILayout.BeginHorizontal();
						obj.sortingOrder = EditorGUILayout.IntField(obj.sortingOrder, GUILayout.Width(40));
						EditorGUILayout.ObjectField(obj, typeof(SpriteRenderer), true);
						EditorGUILayout.EndHorizontal();
					}
				}
				EditorGUI.indentLevel = 0;
			}

			if( EditorGUI.EndChangeCheck())
			{
				SortLayers();
				Repaint();
			}

			EditorGUILayout.EndScrollView();
		}

		void OnGUICameraOrder()
		{
			current = EditorGUILayout.BeginScrollView(current);

			EditorGUILayout.BeginVertical("box");
			foreach( var camera in allCameras )
			{
				EditorGUILayout.BeginHorizontal();
				camera.depth = EditorGUILayout.FloatField(camera.depth, GUILayout.Width(40));
				EditorGUILayout.ObjectField(camera.gameObject, typeof(GameObject) );
				EditorGUILayout.EndHorizontal();
			}
			EditorGUILayout.EndVertical();

			EditorGUILayout.EndScrollView();

		}

		void OnGUI()
		{
		 	orderType = (OrderType) EditorGUILayout.EnumPopup(orderType, EditorStyles.toolbarPopup);

			switch( orderType )
			{
			case OrderType.SpriteOrder:
				OnGUISpriteOrder();
				break;
			case OrderType.CameraOrder:
				OnGUICameraOrder();
				break;
			}
		}



		class SortingLayerWithObject
		{
			public List<SpriteRenderer> rendererList = new List<SpriteRenderer>();
			public string tagName;
			public bool isOpen = true;
		}
	}
}
