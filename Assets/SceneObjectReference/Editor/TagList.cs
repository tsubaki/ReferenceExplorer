using UnityEngine;
using System.Collections;
using UnityEditor;
using terasurware;
using System.Collections.Generic;

public class TagList : EditorWindow {
	
	Vector2 current;
	List<TagWithObjects> tagWithObjectList = new List<TagWithObjects>();

	
	[MenuItem("Window/Referenced/tag list")]
	static void Init ()
	{
		var window = GetWindow (typeof(TagList)) as TagList;
		window.title = "tags";
		window.Find();
		window.Show ();
	}
	
	void Find()
	{
		var allObjects = SceneObjectUtility.GetAllObjectsInScene(false);
		tagWithObjectList.Clear();
		
		foreach( var obj in allObjects )
		{
			if( obj.CompareTag("Untagged") )
				continue;
			
			var tagWithObj = tagWithObjectList.Find( (item) => item.tag == obj.tag) ;
			
			if( tagWithObj == null )
			{
				tagWithObj = new TagWithObjects(){ tag = obj.tag };
				tagWithObjectList.Add(tagWithObj);
			}
			
			tagWithObj.objectList.Add( obj );
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
	
	void OnGUI()
	{
		current = EditorGUILayout.BeginScrollView (current);
		
		GUIStyle buttonStyle = new GUIStyle();
		buttonStyle.margin.left = 10;
		buttonStyle.margin.top = 5;
		
		GUIStyle labelStyle = new GUIStyle(buttonStyle);
		labelStyle.fontSize = 24;
		
		
		foreach( var tagWithObject in tagWithObjectList )
		{
			tagWithObject.isOpen = GUILayout.Toggle (tagWithObject.isOpen, tagWithObject.tag, labelStyle); 
			
			if (tagWithObject.isOpen) {			
			
				foreach( var obj in tagWithObject.objectList )
				{
					if(  GUILayout.Button(obj.name, buttonStyle) )
					{
						UnityEditor.EditorGUIUtility.PingObject(obj );
					}
				}
			}
			GUILayout.Space(16);
		}
		
		EditorGUILayout.EndScrollView();
	}
	
	class TagWithObjects
	{
		public string tag;
		public List<GameObject> objectList = new List<GameObject>();
		public bool isOpen = true;
	}
}
