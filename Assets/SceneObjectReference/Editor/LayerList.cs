using UnityEngine;
using System.Collections;
using UnityEditor;
using terasurware;
using System.Collections.Generic;

public class LayerList : EditorWindow
{
	
	Vector2 current;
	List<LayerWithObject> layerWithObjectList = new List<LayerWithObject> ();
	
	[MenuItem("Window/Referenced/layer list")]
	static void Init ()
	{
		var window = GetWindow (typeof(LayerList)) as LayerList;
		window.title = "layers";
		window.Find();
		window.Show ();
	}
	
	void Find ()
	{
		var allObjects = SceneObjectUtility.GetAllObjectsInScene (false);
		layerWithObjectList.Clear ();
		
		int defaultLayer = LayerMask.NameToLayer ("Default");
		Debug.Log (defaultLayer);
		
		foreach (var obj in allObjects) {
			Debug.Log (obj.layer);
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
	
	void OnFocus()
	{
		Find ();
	}

	
	void OnHierarchyChange()
	{
		Find();
	}


	void OnGUI ()
	{
		current = EditorGUILayout.BeginScrollView (current);
		
		GUIStyle buttonStyle = new GUIStyle ();
		buttonStyle.margin.left = 10;
		buttonStyle.margin.top = 5;
		
		GUIStyle labelStyle = new GUIStyle (buttonStyle);
		labelStyle.fontSize = 24;
		
		
		foreach (var layerWithObject in layerWithObjectList) {
			string layerName = LayerMask.LayerToName (layerWithObject.layer);
			
			layerWithObject.isOpen = GUILayout.Toggle (layerWithObject.isOpen, layerName, labelStyle); 
			
			if (layerWithObject.isOpen) {
				foreach (var obj in layerWithObject.objectList) {
					if (GUILayout.Button (obj.name, buttonStyle)) {
						UnityEditor.EditorGUIUtility.PingObject (obj);
					}
				}
			}
			
			GUILayout.Space (16);
		}
		
		EditorGUILayout.EndScrollView ();
	}
	
	class LayerWithObject
	{
		public int layer;
		public List<GameObject> objectList = new List<GameObject> ();
		public bool isOpen = true;
	}
}

