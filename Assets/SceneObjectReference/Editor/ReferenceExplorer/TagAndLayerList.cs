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
		
			isOpenTagList = EditorGUILayout.Foldout (isOpenTagList, "Tags");
			if (isOpenTagList) {
				foreach (var tagWithObject in tagWithObjectList) {
					EditorGUI.indentLevel = 1;
					EditorGUILayout.BeginVertical ();
					tagWithObject.isOpen = EditorGUILayout.Foldout (tagWithObject.isOpen, tagWithObject.tag); 

					if (tagWithObject.isOpen) {			
					
						EditorGUI.indentLevel = 2;
						foreach (var obj in tagWithObject.objectList) {
							if (GUILayout.Button (obj.name, buttonStyle)) {
								UnityEditor.EditorGUIUtility.PingObject (obj);
							}
						}
					}

					EditorGUILayout.EndVertical ();

				}
			}
			EditorGUI.indentLevel = 0;

			isOpenLayerList = EditorGUILayout.Foldout (isOpenLayerList, "Layers");
			if (isOpenLayerList) {
				foreach (var layerWithObject in layerWithObjectList) {
					EditorGUI.indentLevel = 1;
					string layerName = LayerMask.LayerToName (layerWithObject.layer);
				
					layerWithObject.isOpen = EditorGUILayout.Foldout (layerWithObject.isOpen, layerName); 
				
					EditorGUI.indentLevel = 2;
					if (layerWithObject.isOpen) {
						foreach (var obj in layerWithObject.objectList) {
							if (GUILayout.Button (obj.name, buttonStyle)) {
								UnityEditor.EditorGUIUtility.PingObject (obj);
							}
						}
					}
				}
			}
			EditorGUI.indentLevel = 0;

			EditorGUILayout.EndScrollView ();
		}
	
		class TagWithObjects
		{
			public string tag;
			public List<GameObject> objectList = new List<GameObject> ();
			public bool isOpen = true;
		}

		class LayerWithObject
		{
			public int layer;
			public List<GameObject> objectList = new List<GameObject> ();
			public bool isOpen = true;
		}
	}
}
