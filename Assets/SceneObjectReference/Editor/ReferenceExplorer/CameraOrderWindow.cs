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
			CameraOrderInScene,
#if UNITY_4_6 || UNITY_5
			SpriteOrderInScene
#endif
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

			backgroundTexture = new Texture2D(1,1);
		}

		void OnHierarchyChange()
		{
			UpdateCameras();
			Repaint();
		}

		void OnInspectorUpdate ()
		{
			SortCamera();
			SortLayers();
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
			SortLayers();
		}

		void SortCamera()
		{
			allCameras.Sort( (x, y) => {	return (int)(1000 * x.depth) - (int)(1000 * y.depth); });
		}


		void SortLayers()
		{
#if UNITY_4_6 || UNITY_5
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

				var sortingLayerName =  string.IsNullOrEmpty( renderer.sortingLayerName ) ? "Default" : renderer.sortingLayerName ;
				var tag = layers.Find( t => t.tagName == sortingLayerName);

				tag.rendererList.Add( renderer);
			}

			foreach( var layer in layers )
			{
				layer.rendererList.Sort( (x,y) =>  x.sortingOrder - y.sortingOrder);
			}
#endif
		}

		List<SortingLayerWithObject> layers = new List<SortingLayerWithObject>();
		List<Camera> allCameras = new List<Camera>();

		Vector2 current;

		Texture2D backgroundTexture = null;


#if UNITY_4_6 || UNITY_5
		void OnGUISpriteOrder()
		{
			EditorGUI.BeginChangeCheck();

			current = EditorGUILayout.BeginScrollView(current);
			var selectedList = new List<GameObject>( Selection.gameObjects );

			GUIStyle style = new GUIStyle();
			style.normal.background = backgroundTexture;

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

						if( selectedList.Contains( obj.gameObject ))
							EditorGUILayout.BeginHorizontal(style);
						else
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
#endif

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

#if UNITY_4_6 || UNITY_5
			switch( orderType )
			{
			case OrderType.SpriteOrderInScene:
				OnGUISpriteOrder();
				break;
			case OrderType.CameraOrderInScene:
				OnGUICameraOrder();
				break;
			}
#else
			OnGUICameraOrder();
#endif

		}



		class SortingLayerWithObject
		{
			public List<SpriteRenderer> rendererList = new List<SpriteRenderer>();
			public string tagName;
			public bool isOpen = true;
		}
	}
}
