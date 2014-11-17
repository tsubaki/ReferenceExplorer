using UnityEngine;
using System.Collections;
using UnityEditor;
using UnityEditorInternal;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ReferenceExplorer
{
	[InitializeOnLoad]
	public class SceneObjectsCallMethods : EditorWindow
	{

		Vector2 current;
		int currentItem;
		static Texture2D texture;
		List<MethodWithObject> methdoList = new List<MethodWithObject> ();
		static List<HaveCallbackObject> componentHaveCallbackList = new List<HaveCallbackObject> ();
		string findMethodName = string.Empty;

		SearchSceneComponentCode search = new SearchSceneComponentCode();

		bool isSelected = false;

		enum CallType{
			facility_callback_components,
			facility_callback_object,
			call_callback_components,
			Search
		};

		CallType callType = CallType.call_callback_components;

		static void HierarchyItemCB (int instanceID, Rect selectionRect)
		{
			// place the icoon to the right of the list:
			try {
				Rect r = new Rect (selectionRect);
				r.x += r.width - 20;

				if (componentHaveCallbackList.Exists( (item)=>{ return item.instanceID == instanceID;})) {
					GUI.Label (r, texture);
				}
			} catch (System.Exception e) {
				Debug.LogWarning (e.Message);
			}
		}

		static SceneObjectsCallMethods ()
		{
			// Init
			texture = AssetDatabase.LoadAssetAtPath ("Assets/SceneObjectReference/Editor/icon.png", typeof(Texture2D)) as Texture2D;
			EditorApplication.hierarchyWindowItemOnGUI += HierarchyItemCB;
		}

		void GetAnimationEvents (Animator animator)
		{
			if (animator == null)
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

			if (anim == null)
				return;

			for (int i=0; i<anim.layerCount; i++) {
				var layer = anim.GetLayer (i);
				for (int r=0; r< layer.stateMachine.stateCount; r++) {
					var state = layer.stateMachine.GetState (r);
					var clip = state.GetMotion () as AnimationClip;

					if (clip == null)
						continue;
				
					foreach (var ev in AnimationUtility.GetAnimationEvents(clip)) {
						MethodWithObject item = methdoList.Find ((name) => {
							return name.method.Equals (ev.functionName); });
						if (item == null) {
							item = new MethodWithObject ();
							item.method = ev.functionName;
							methdoList.Add (item);
						}
					
						if (!item.callComponent.Exists ((obj) => {
							return animator.Equals (obj); })) {
							item.callComponent.Add (animator);
						}
					}
				}
			}
#endif

		}

		void UpdateComponentHaveCallbackList ()
		{
			componentHaveCallbackList.Clear ();

			if (methdoList.Count <= currentItem || methdoList.Count == 0)
				return;

			var methodName = methdoList [currentItem].method;
			componentHaveCallbackList.AddRange (HaveMethodComponentCount (methodName));
		}

		List<HaveCallbackObject> HaveMethodComponentCount (string methodName)
		{
			List<HaveCallbackObject> list = new List<HaveCallbackObject> ();

			var monobehaviourList = new List<MonoBehaviour>();

			if( isSelected )
			{
				foreach( var obj in Selection.gameObjects )
				{
					monobehaviourList.AddRange( obj.GetComponents<MonoBehaviour>());
				}
			}else{
				monobehaviourList.AddRange(GameObject.FindObjectsOfType<MonoBehaviour>());
			}
			

			foreach (var item in monobehaviourList) {
				var method = item.GetType ().GetMethod (methodName, 
			                                      System.Reflection.BindingFlags.NonPublic | 
					System.Reflection.BindingFlags.Public |
					System.Reflection.BindingFlags.Instance);
				if (method != null) {
					list.Add (new HaveCallbackObject()
					{
						instanceID = item.gameObject.GetInstanceID (),
						component = item,
					});
				}
			}
			return list;
		}

		void GetSendMessageMethods ()
		{
			foreach (var component in  GameObject.FindObjectsOfType<MonoBehaviour>()) {
				foreach (var text in MonoScript.FromMonoBehaviour(component).text.Split(';')) {
					if (AddMatchMethod (text, component, "SendMessage\\((?<call>.*?),.*\\)"))
						continue;
					if (AddMatchMethod (text, component, "SendMessage\\((?<call>.*?)\\)"))
						continue;
					if (AddMatchMethod (text, component, "BroadcastMessage\\((?<call>.*?)\\)"))
						continue;
					if (AddMatchMethod (text, component, "BroadcastMessage\\((?<call>.*?)\\)"))
						continue;
				}
			}
		}

		void RegisterDefaultCallback ()
		{
			if (findMethodName != "") {
				methdoList.Add (new MethodWithObject (){ method = findMethodName});
			}

			methdoList.Add (new MethodWithObject (){ method = "Awake" });
			methdoList.Add (new MethodWithObject (){ method = "Start" });
			methdoList.Add (new MethodWithObject (){ method = "Update" });
			methdoList.Add (new MethodWithObject (){ method = "FixedUpdate" });
			methdoList.Add (new MethodWithObject (){ method = "LateUpdate" });
			methdoList.Add (new MethodWithObject (){ method = "OnApplicationPause" });
			methdoList.Add (new MethodWithObject (){ method = "OnTriggerStay" });
			methdoList.Add (new MethodWithObject (){ method = "OnTriggerEnter" });
			methdoList.Add (new MethodWithObject (){ method = "OnTriggerExit" });
			methdoList.Add (new MethodWithObject (){ method = "OnCollisionEnter" });
			methdoList.Add (new MethodWithObject (){ method = "OnCollisionEnter2D" });
			methdoList.Add (new MethodWithObject (){ method = "OnCollisionExit" });
			methdoList.Add (new MethodWithObject (){ method = "OnCollisionExit2D" });
			methdoList.Add (new MethodWithObject (){ method = "OnCollisionStay" });
			methdoList.Add (new MethodWithObject (){ method = "OnCollisionStay2D" });
			methdoList.Add (new MethodWithObject (){ method = "OnTriggerEnter2D" });
			methdoList.Add (new MethodWithObject (){ method = "OnTriggerStay2D" });
			methdoList.Add (new MethodWithObject (){ method = "OnTriggerExit2D" });
			methdoList.Add (new MethodWithObject (){ method = "OnApplicationQuit" });
			methdoList.Add (new MethodWithObject (){ method = "OnControllerColliderHit" });
			methdoList.Add (new MethodWithObject (){ method = "OnGUI" });
			methdoList.Add (new MethodWithObject (){ method = "OnDrawGizmos" });
			methdoList.Add (new MethodWithObject (){ method = "OnDrawGizmosSelected" });

		}

		bool AddMatchMethod (string source, Component calledComponent, string patterm)
		{
			var match = Regex.Match (source, patterm);
			if (! match.Success)
				return false;

			var method = match.Groups ["call"].ToString ().Replace ("\"", "");
			MethodWithObject item = methdoList.Find ((name) => { return name.method.Equals (method); });
		
			if (item == null) {
				item = new MethodWithObject ();
				item.method = method;
				methdoList.Add (item);
			}
		
			if (!item.callComponent.Exists ((comp) => { return calledComponent.Equals (comp); })) {
				item.callComponent.Add (calledComponent);
			}
			return true;
		}
	
		[MenuItem("Window/Referenced/Callbacks")]
		static void Init ()
		{
			var window = GetWindow (typeof(SceneObjectsCallMethods)) as SceneObjectsCallMethods;
			window.title = "callback";
			window.Show ();
		}
	
		void Find ()
		{
			methdoList.Clear ();

			RegisterDefaultCallback ();

			foreach (var anim in FindObjectsOfType<Animator>()) {
				GetAnimationEvents (anim);
			}
			GetSendMessageMethods ();


			foreach (var item in methdoList.ToArray()) {
				var count = HaveMethodComponentCount (item.method).Count;

				if (count == 0)
					methdoList.Remove (item);
			}
		}

		/*
	void OnInspectorUpdate ()
	{
		Find ();
		Repaint ();
	}
	*/
	
		void OnFocus ()
		{
			Find ();
			UpdateComponentHaveCallbackList ();
			search.OnFocus();
		}


		void OnHierarchyChange ()
		{
			Find ();
			UpdateComponentHaveCallbackList ();
			search.UpdateSearchComponent();
			Repaint();
		}

		void OnGUI ()
		{
			GUIStyle buttonStyle = new GUIStyle ();
			buttonStyle.margin.left = 10;
			buttonStyle.margin.top = 5;
		
			GUIStyle labelStyle = new GUIStyle (buttonStyle);
			labelStyle.fontSize = 24;

			EditorGUILayout.BeginHorizontal();

			EditorGUI.BeginChangeCheck();

			isSelected = GUILayout.Toggle(isSelected, "Select", EditorStyles.toolbarButton );

			callType = (CallType)EditorGUILayout.EnumPopup( callType, EditorStyles.toolbarPopup);

			if( EditorGUI.EndChangeCheck() )
			{
				Find ();
				UpdateComponentHaveCallbackList ();

				if( callType == CallType.Search )
				{
					search.selected = isSelected;
					search.UpdateCalledObjectList();
					search.UpdateSearchComponent();
				}
				Repaint();
			}


			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginVertical();
			if( callType == CallType.Search )
			{
				search.OnGUI();
			}else{
				OnGUICallBack();
			}
			EditorGUILayout.EndVertical();
		}

		void OnGUICallBack()
		{
			
			
			findMethodName = EditorGUILayout.TextField ("extra callback", findMethodName, GUILayout.Width (Screen.width - 8));
			
			if (methdoList.Count != 0) {
				
				current = EditorGUILayout.BeginScrollView (current);
				
				for (int i=0; i<methdoList.Count; i++) {
					EditorGUILayout.BeginHorizontal ("box", GUILayout.Width (Screen.width - 8));
					var methodName = methdoList [i];
					
					EditorGUI.BeginChangeCheck();
					var isEnable = EditorGUILayout.ToggleLeft (methodName.method, i == currentItem);
					if( EditorGUI.EndChangeCheck() )
					{
						UpdateComponentHaveCallbackList ();
					}
					
					if (isEnable && currentItem != i) {
						currentItem = i;
						
						//						foreach (var id in componentHaveCallbackList) {
						//							UnityEditor.EditorGUIUtility.PingObject (id.instanceID);
						//						}
					}
					
					EditorGUILayout.BeginVertical ();
					
					switch( callType )
					{
					case CallType.call_callback_components:
						if (i == currentItem) {
							foreach (var obj in methodName.callComponent) {
								EditorGUILayout.ObjectField (obj.GetType ().ToString (), obj, obj.GetType ());
							}
							if( methodName.callComponent.Count == 0 )
								EditorGUILayout.LabelField("Call from UnityEngine");
						}
						break;
					case CallType.facility_callback_components:
						
						if (i == currentItem) {
							
							var monoscriptList = new List<MonoScript>();
							
							foreach( var item in componentHaveCallbackList )
							{
								var code = MonoScript.FromMonoBehaviour(item.component) ;
								if(! monoscriptList.Contains( code ) )
								{
									monoscriptList.Add(code);
								}
							}
							
							foreach (var obj in monoscriptList) {
								if (obj == null)
									break;
								EditorGUILayout.ObjectField(obj , typeof(MonoScript), true);
							}
						}
						
						break;
					case CallType.facility_callback_object:
						if (i == currentItem) {
							
							var objList = new List<GameObject>();
							foreach( var item in componentHaveCallbackList )
							{
								if(! objList.Contains( item.component.gameObject ) )
								{
									objList.Add(item.component.gameObject);
								}
							}
							
							
							foreach (var obj in objList) {
								if (obj == null)
									break;
								
								EditorGUILayout.ObjectField ( obj, obj.GetType());
							}
						}
						break;
					}
					
					EditorGUILayout.EndVertical ();
					
					EditorGUILayout.EndHorizontal ();
				}
				
				EditorGUILayout.EndScrollView ();
			}		
		}

		class HaveCallbackObject
		{
			public int instanceID;
			public MonoBehaviour component;
		}

		class MethodWithObject
		{
			public string method;
			public List<Component> callComponent = new List<Component> ();
			public bool isOpen = true;
		}
	}
}
