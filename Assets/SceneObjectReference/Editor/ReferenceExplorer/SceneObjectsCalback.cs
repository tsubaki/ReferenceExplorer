using UnityEngine;
using System.Collections;
using UnityEditor;
using UnityEditorInternal;
using System.Collections.Generic;
using System.Text.RegularExpressions;

#pragma warning disable 0618

namespace ReferenceExplorer
{
	[InitializeOnLoad]
	public class SceneObjectsCalback : EditorWindow
	{

		Vector2 current;
		int currentItem;
		static Texture2D texture;
		List<CallbackCallObject> callbackCallObjectList = new List<CallbackCallObject> ();
		static List<HaveCallbackObject> componentHaveCallbackList = new List<HaveCallbackObject> ();
		string findMethodName = string.Empty;

		SearchSceneComponentCode search = new SearchSceneComponentCode();

		bool isSelected = false;

		enum CallType{
			CalledComponents,
			CalledObjects,
			CallingComponents,
			SearchSourcecodes
		};

		CallType callType = CallType.CallingComponents;

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

		static SceneObjectsCalback ()
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
						CallbackCallObject item = callbackCallObjectList.Find ((name) => {
							return name.method.Equals (ev.functionName); });
						if (item == null) {
							item = new CallbackCallObject ();
							item.method = ev.functionName;
							callbackCallObjectList.Add (item);
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

			if (callbackCallObjectList.Count <= currentItem || callbackCallObjectList.Count == 0)
				return;

			var methodName = callbackCallObjectList [currentItem].method;
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
				callbackCallObjectList.Add (new CallbackCallObject (){ method = findMethodName});
			}

			callbackCallObjectList.Add (new CallbackCallObject (){ method = "Awake" });
			callbackCallObjectList.Add (new CallbackCallObject (){ method = "Start" });
			callbackCallObjectList.Add (new CallbackCallObject (){ method = "Update" });
			callbackCallObjectList.Add (new CallbackCallObject (){ method = "FixedUpdate" });
			callbackCallObjectList.Add (new CallbackCallObject (){ method = "LateUpdate" });
			callbackCallObjectList.Add (new CallbackCallObject (){ method = "OnApplicationPause" });
			callbackCallObjectList.Add (new CallbackCallObject (){ method = "OnTriggerStay" });
			callbackCallObjectList.Add (new CallbackCallObject (){ method = "OnTriggerEnter" });
			callbackCallObjectList.Add (new CallbackCallObject (){ method = "OnTriggerExit" });
			callbackCallObjectList.Add (new CallbackCallObject (){ method = "OnCollisionEnter" });
			callbackCallObjectList.Add (new CallbackCallObject (){ method = "OnCollisionEnter2D" });
			callbackCallObjectList.Add (new CallbackCallObject (){ method = "OnCollisionExit" });
			callbackCallObjectList.Add (new CallbackCallObject (){ method = "OnCollisionExit2D" });
			callbackCallObjectList.Add (new CallbackCallObject (){ method = "OnCollisionStay" });
			callbackCallObjectList.Add (new CallbackCallObject (){ method = "OnCollisionStay2D" });
			callbackCallObjectList.Add (new CallbackCallObject (){ method = "OnTriggerEnter2D" });
			callbackCallObjectList.Add (new CallbackCallObject (){ method = "OnTriggerStay2D" });
			callbackCallObjectList.Add (new CallbackCallObject (){ method = "OnTriggerExit2D" });
			callbackCallObjectList.Add (new CallbackCallObject (){ method = "OnApplicationQuit" });
			callbackCallObjectList.Add (new CallbackCallObject (){ method = "OnControllerColliderHit" });
			callbackCallObjectList.Add (new CallbackCallObject (){ method = "OnGUI" });
			callbackCallObjectList.Add (new CallbackCallObject (){ method = "OnDrawGizmos" });
			callbackCallObjectList.Add (new CallbackCallObject (){ method = "OnDrawGizmosSelected" });

		}

		bool AddMatchMethod (string source, Component calledComponent, string patterm)
		{
			var match = Regex.Match (source, patterm);
			if (! match.Success)
				return false;

			var method = match.Groups ["call"].ToString ().Replace ("\"", "");
			CallbackCallObject item = callbackCallObjectList.Find ((name) => { return name.method.Equals (method); });
		
			if (item == null) {
				item = new CallbackCallObject ();
				item.method = method;
				callbackCallObjectList.Add (item);
			}
		
			if (!item.callComponent.Exists ((comp) => { return calledComponent.Equals (comp); })) {
				item.callComponent.Add (calledComponent);
			}
			return true;
		}
	
		[MenuItem("Window/ReferenceExplorer/Callbacks")]
		static void Init ()
		{
			var window = GetWindow (typeof(SceneObjectsCalback)) as SceneObjectsCalback;
			window.title = "callback";
			window.Show ();
		}
	
		void SearchCallback ()
		{
			callbackCallObjectList.Clear ();

			RegisterDefaultCallback ();

			foreach (var anim in FindObjectsOfType<Animator>()) {
				GetAnimationEvents (anim);
			}
			GetSendMessageMethods ();


			foreach (var item in callbackCallObjectList.ToArray()) {
				var count = HaveMethodComponentCount (item.method).Count;

				if (count == 0)
					callbackCallObjectList.Remove (item);
			}
		}
	
		void OnFocus ()
		{
			SearchCallback ();
			UpdateComponentHaveCallbackList ();
			search.OnFocus();
		}


		void OnHierarchyChange ()
		{
			SearchCallback ();
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

			isSelected = GUILayout.Toggle(isSelected, "Search selected", EditorStyles.toolbarButton, GUILayout.Width(120) );

			callType = (CallType)EditorGUILayout.EnumPopup( callType, EditorStyles.toolbarPopup);

			if( EditorGUI.EndChangeCheck() )
			{
				SearchCallback ();
				UpdateComponentHaveCallbackList ();

				if( callType == CallType.SearchSourcecodes )
				{
					search.selected = isSelected;
					search.UpdateCalledObjectList();
					search.UpdateSearchComponent();
				}
				Repaint();
			}


			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginVertical();
			if( callType == CallType.SearchSourcecodes )
			{
				search.OnGUI();
			}else{
				OnGUICallBack();
			}
			EditorGUILayout.EndVertical();
		}

		void OnGUICallBack()
		{
			
			
			//findMethodName = EditorGUILayout.TextField ("extra callback", findMethodName, GUILayout.Width (Screen.width - 8));
			
			if (callbackCallObjectList.Count != 0) {
				
				current = EditorGUILayout.BeginScrollView (current);
				
				for (int i=0; i<callbackCallObjectList.Count; i++) {
					EditorGUILayout.BeginHorizontal ("box", GUILayout.Width (Screen.width - 8));
					var methodName = callbackCallObjectList [i];
					
					EditorGUI.BeginChangeCheck();
					if( EditorGUILayout.ToggleLeft (methodName.method, i == currentItem))
					{
						currentItem = i;
					}
					if( EditorGUI.EndChangeCheck() )
					{
						UpdateComponentHaveCallbackList ();
					}

					EditorGUILayout.BeginVertical ();
					
					switch( callType )
					{
					case CallType.CallingComponents:
						if (i == currentItem) {
							foreach (var obj in methodName.callComponent) {
								EditorGUILayout.ObjectField (obj.GetType ().ToString (), obj, obj.GetType ());
							}
							if( methodName.callComponent.Count == 0 )
								EditorGUILayout.LabelField("Call from UnityEngine");
						}
						break;
					case CallType.CalledComponents:
						
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
					case CallType.CalledObjects:
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
	}
}
