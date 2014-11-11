using UnityEngine;
using System.Collections;
using UnityEditor;
using UnityEditorInternal;
using System.Collections.Generic;
using System.Text.RegularExpressions;


[InitializeOnLoad]
public class SceneObjectsCallMethods : EditorWindow 
{

	Vector2 current;
	int currentItem;

	static Texture2D texture;

	List<MethodWithObject> methdoList = new List<MethodWithObject>();
	static List<int> componentHaveCallbackList = new List<int>();

	string findMethodName = string.Empty;

	static void HierarchyItemCB (int instanceID, Rect selectionRect)
	{
		// place the icoon to the right of the list:
		try{
			Rect r = new Rect (selectionRect);
			r.x += r.width - 20;

			if ( componentHaveCallbackList.Contains(instanceID) )
			{
				GUI.Label (r, texture);
			}
		}catch(System.Exception e){
			Debug.LogWarning(e.Message);
		}
	}

	static SceneObjectsCallMethods ()
	{
		// Init
		texture = AssetDatabase.LoadAssetAtPath ("Assets/SceneObjectReference/Editor/icon.png", typeof(Texture2D)) as Texture2D;
		EditorApplication.hierarchyWindowItemOnGUI += HierarchyItemCB;
	}


	void GetAnimationEvents(Animator animator)
	{
		if( animator == null )
			return;

#if UNITY_5_0
		var anim = (UnityEditor.Animations.AnimatorController)animator.runtimeAnimatorController;

		foreach( var clip in anim.animationClips )
		{
			foreach( var ev in AnimationUtility.GetAnimationEvents(clip) )
			{
				MethodWithObject item = methdoList.Find( (name) =>{ return name.method.Equals(ev.functionName) ; } );
				if( item == null ){
					item = new MethodWithObject();
					item.method = ev.functionName;
					methdoList.Add(item);
				}

				if( !item.objectList.Exists( (obj)=>{ return animator.Equals(obj); } ) )
				{
					item.objectList.Add(animator );
				}
			}
		}
#else
		var anim = (UnityEditorInternal.AnimatorController)animator.runtimeAnimatorController;

		if( anim == null)
			return;

		for(int i=0; i<anim.layerCount; i++)
		{
			var layer = anim.GetLayer(i);
			for( int r=0; r< layer.stateMachine.stateCount; r++)
			{
				var state = layer.stateMachine.GetState(r);
				var clip = state.GetMotion() as AnimationClip;

				if( clip == null )
					continue;
				
				foreach( var ev in AnimationUtility.GetAnimationEvents(clip) )
				{
					MethodWithObject item = methdoList.Find( (name) =>{ return name.method.Equals(ev.functionName) ; } );
					if( item == null ){
						item = new MethodWithObject();
						item.method = ev.functionName;
						methdoList.Add(item);
					}
					
					if( !item.objectList.Exists( (obj)=>{ return animator.Equals(obj); } ) )
					{
						item.objectList.Add(animator );
					}
				}
			}
		}
#endif

	}

	void UpdateComponentHaveCallbackList()
	{
		componentHaveCallbackList.Clear();

		if( methdoList.Count <= currentItem || methdoList.Count == 0 )
			return;


		var methodName = methdoList[currentItem].method;

		componentHaveCallbackList.AddRange(HaveMethodComponentCount(methodName));
	}

	List<int> HaveMethodComponentCount(string methodName )
	{
		List<int> list = new List<int>();
		foreach( var item in GameObject.FindObjectsOfType<MonoBehaviour>())
		{
			var method = item.GetType().GetMethod(methodName, 
			                                      System.Reflection.BindingFlags.NonPublic | 
			                                      System.Reflection.BindingFlags.Public |
			                                      System.Reflection.BindingFlags.Instance);
			if( method != null ){
				list.Add(item.gameObject.GetInstanceID());
			}
		}
		return list;
	}

	void GetSendMessageMethods(GameObject obj)
	{
		foreach( var component in  GameObject.FindObjectsOfType<MonoBehaviour>())
		{
			foreach( var text in MonoScript.FromMonoBehaviour(component).text.Split(';') )
			{
				if( AddMatchMethod(text, component, "SendMessage\\((?<call>.*?),.*\\)") )
					continue;
				if( AddMatchMethod(text, component, "SendMessage\\((?<call>.*?)\\)") )
					continue;
				if( AddMatchMethod(text, component, "BroadcastMessage\\((?<call>.*?)\\)") )
					continue;
				if( AddMatchMethod(text, component, "BroadcastMessage\\((?<call>.*?)\\)") )
					continue;
			}
		}
	}

	void RegisterDefaultCallback()
	{
		if( findMethodName != "" )
		{
			methdoList.Add(new MethodWithObject(){ method = findMethodName});
		}

		methdoList.Add(new MethodWithObject(){ method = "Awake" });
		methdoList.Add(new MethodWithObject(){ method = "Start" });
		methdoList.Add(new MethodWithObject(){ method = "Update" });
		methdoList.Add(new MethodWithObject(){ method = "FixedUpdate" });
		methdoList.Add(new MethodWithObject(){ method = "LateUpdate" });
		methdoList.Add(new MethodWithObject(){ method = "OnApplicationPause" });
		methdoList.Add(new MethodWithObject(){ method = "OnCollisionEnter" });
		methdoList.Add(new MethodWithObject(){ method = "OnCollisionEnter2D" });
		methdoList.Add(new MethodWithObject(){ method = "OnCollisionExit" });
		methdoList.Add(new MethodWithObject(){ method = "OnCollisionExit2D" });
		methdoList.Add(new MethodWithObject(){ method = "OnCollisionStay" });
		methdoList.Add(new MethodWithObject(){ method = "OnCollisionStay2D" });
		methdoList.Add(new MethodWithObject(){ method = "OnApplicationQuit" });
		methdoList.Add(new MethodWithObject(){ method = "OnControllerColliderHit" });

	}

	bool AddMatchMethod(string source, Component component, string patterm )
	{
		var match = Regex.Match(source, patterm);
		if(! match.Success )
			return false;

		var method = match.Groups["call"].ToString().Replace("\"", "");
		MethodWithObject item = methdoList.Find( (name) =>{ return name.method.Equals(method) ; } );
		
		if( item == null ){
			item = new MethodWithObject();
			item.method = method;
			methdoList.Add(item);
		}
		
		if( !item.objectList.Exists( (comp)=>{ return component.Equals(comp); } ) )
		{
			item.objectList.Add(component );
		}
		return true;
	}


	
	[MenuItem("Window/Referenced/animation events")]
	static void Init ()
	{
		var window = GetWindow (typeof(SceneObjectsCallMethods)) as SceneObjectsCallMethods;
		window.title = "call mehod";
		window.Show ();
	}
	
	void Find()
	{
		var obj = Selection.activeGameObject;
		if( obj == null )
			return;
		methdoList.Clear();

		RegisterDefaultCallback();

		foreach( var anim in FindObjectsOfType<Animator>())
		{
			GetAnimationEvents(anim);
		}
		GetSendMessageMethods(obj);


		foreach( var item in methdoList.ToArray() )
		{
			var count = HaveMethodComponentCount(item.method ).Count;

			if( count == 0)
				methdoList.Remove(item);
		}


	}

	/*
	void OnInspectorUpdate ()
	{
		Find ();
		Repaint ();
	}
	*/
	
	void OnFocus()
	{
		Find ();
		UpdateComponentHaveCallbackList();
	}

	void OnHierarchyChange()
	{
		Find ();
		UpdateComponentHaveCallbackList();
	}



	void OnGUI()
	{
		GUIStyle buttonStyle = new GUIStyle();
		buttonStyle.margin.left = 10;
		buttonStyle.margin.top = 5;
		
		GUIStyle labelStyle = new GUIStyle(buttonStyle);
		labelStyle.fontSize = 24;

		findMethodName = EditorGUILayout.TextField(findMethodName, GUILayout.Width(Screen.width - 8) );

		if( methdoList.Count != 0 )
		{

			current = EditorGUILayout.BeginScrollView(current);

			for(int i=0; i<methdoList.Count; i++)
			{
				EditorGUILayout.BeginHorizontal("box", GUILayout.Width(Screen.width  - 8));
				var methodName = methdoList[i];
				var isEnable = EditorGUILayout.ToggleLeft(methodName.method, i == currentItem);

				if( isEnable && currentItem != i){
					currentItem = i;
					UpdateComponentHaveCallbackList();

					foreach( var id in componentHaveCallbackList )
					{
						UnityEditor.EditorGUIUtility.PingObject(id);
					}

				}

				EditorGUILayout.BeginVertical();

				if( i == currentItem )
				{
					foreach( var obj in methodName.objectList ){
						if( obj == null )
							break;
						EditorGUILayout.ObjectField( obj.GetType().ToString(), obj, obj.GetType() );
					}
				}
				EditorGUILayout.EndVertical();

				EditorGUILayout.EndHorizontal();
			}

			EditorGUILayout.EndScrollView();
		}
	}

	class MethodWithObject
	{
		public string method;
		public List<Component> objectList = new List<Component>();
		public bool isOpen = true;
	}
}
