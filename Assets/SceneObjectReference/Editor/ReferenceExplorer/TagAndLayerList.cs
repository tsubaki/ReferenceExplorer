using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;

namespace ReferenceExplorer
{
	public class TagAndLayerList : EditorWindow
	{
	
		Vector2 current;
		List<TagWithObjects> tagWithObjectList = new List<TagWithObjects> ();
		List<LayerWithObject> layerWithObjectList = new List<LayerWithObject> ();
		bool isOpenTagList = false, isOpenLayerList = false;
	
		[MenuItem("Window/Referenced/Tag and Layer")]
		static void Init ()
		{
			var window = GetWindow (typeof(TagAndLayerList)) as TagAndLayerList;
			window.title = "tags";
			window.FindTags ();
			window.Show ();
		}
	
		void FindTags ()
		{
			var allObjects = SceneObjectUtility.GetAllObjectsInScene (false);
			tagWithObjectList.Clear ();
		
			foreach (var obj in allObjects) {
				if (obj.CompareTag ("Untagged"))
					continue;
			
				var tagWithObj = tagWithObjectList.Find ((item) => item.tag == obj.tag);
			
				if (tagWithObj == null) {
					tagWithObj = new TagWithObjects (){ tag = obj.tag };
					tagWithObjectList.Add (tagWithObj);
				}
			
				tagWithObj.objectList.Add (obj);
			}
		
		}

		void FindLayers ()
		{
			var allObjects = SceneObjectUtility.GetAllObjectsInScene (false);
			layerWithObjectList.Clear ();
		
			int defaultLayer = LayerMask.NameToLayer ("Default");
		
			foreach (var obj in allObjects) {
				if (obj.layer == defaultLayer)
					continue;
			
				var layerWithObj = layerWithObjectList.Find ((item) => item.layer == obj.layer);
			
				if (layerWithObj == null) {
					layerWithObj = new LayerWithObject (){ layer = obj.layer };
					layerWithObjectList.Add (layerWithObj);
				}
			
				layerWithObj.objectList.Add (obj);
			}
		
		}

		void OnFocus ()
		{
			FindTags ();
			FindLayers ();
		}
	
		void OnHierarchyChange ()
		{
			FindTags ();
			FindLayers ();
		}
	
		void OnGUI ()
		{
			current = EditorGUILayout.BeginScrollView (current);
		
			GUIStyle buttonStyle = new GUIStyle ();
			buttonStyle.margin.left = 50;
			buttonStyle.margin.top = 5;
		
			EditorGUILayout.BeginVertical("box");
			isOpenTagList = EditorGUILayout.Foldout (isOpenTagList, "Tags");
			if (isOpenTagList) {
				foreach (var tagWithObject in tagWithObjectList) {
					EditorGUI.indentLevel = 1;
					EditorGUILayout.BeginVertical ();

					EditorGUILayout.BeginHorizontal();
					tagWithObject.isOpen = EditorGUILayout.Foldout (tagWithObject.isOpen, tagWithObject.tag); 

					if( GUILayout.Button("all", EditorStyles.toolbarButton, GUILayout.Width(40)) )
					{
						Selection.objects = tagWithObject.objectList.ToArray();
					}
					
					EditorGUILayout.EndHorizontal();

					if (tagWithObject.isOpen) {			
					
						EditorGUI.indentLevel = 2;
						foreach (var obj in tagWithObject.objectList) {
							EditorGUILayout.ObjectField(obj, obj.GetType() );
						}
					}

					EditorGUILayout.EndVertical ();

				}
			}
			EditorGUILayout.EndVertical();
			EditorGUI.indentLevel = 0;


			EditorGUILayout.BeginVertical("box");
			isOpenLayerList = EditorGUILayout.Foldout (isOpenLayerList, "Layers");

			if (isOpenLayerList) {
				foreach (var layerWithObject in layerWithObjectList) {
					EditorGUI.indentLevel = 1;
					string layerName = LayerMask.LayerToName (layerWithObject.layer);
				
				
					EditorGUILayout.BeginHorizontal();
					
					layerWithObject.isOpen = EditorGUILayout.Foldout (layerWithObject.isOpen, layerName); 
					if( GUILayout.Button("all", EditorStyles.toolbarButton, GUILayout.Width(40)) )
					{
						Selection.objects = layerWithObject.objectList.ToArray();
					}
					
					EditorGUILayout.EndHorizontal();

					EditorGUI.indentLevel = 2;

					if (layerWithObject.isOpen) {
						foreach (var obj in layerWithObject.objectList) {
							EditorGUILayout.ObjectField(obj, obj.GetType() );
						}
					}
				}
			}
			EditorGUI.indentLevel = 0;
			EditorGUILayout.EndVertical();

			EditorGUILayout.EndScrollView ();
		}
	
		class TagWithObjects
		{
			public string tag;
			public List<GameObject> objectList = new List<GameObject> ();
			public bool isOpen = false;
		}

		class LayerWithObject
		{
			public int layer;
			public List<GameObject> objectList = new List<GameObject> ();
			public bool isOpen = false;
		}
	}
}
