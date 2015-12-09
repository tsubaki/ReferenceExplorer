using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;

public class SceneClassViewer : EditorWindow
{

	CodeSearch codeSearch;

	enum CurrentWindowType
	{
		Class,
		Callback,
		Export,
		CodeSearch,
	}

	[MenuItem("Window/ReferenceExploer/Types")]
	static void Initialize ()
	{
		var window = SceneClassViewer.GetWindow<SceneClassViewer> ();
		window.Show ();
	}

	void OnEnable ()
	{
		codeSearch = new CodeSearch();

		ReferenceExplorerData.RestoreAllData ();

		CallbackData.UpdateSenderRecieverlist();
		CallbackData.UpdateCallbacklist(isSelectedObject, searchText);
	}

	void OnHierarchyChange ()
	{
		ReferenceExplorerData.RestoreAllData ();
		
		CallbackData.UpdateSenderRecieverlist();
		CallbackData.UpdateCallbacklist(isSelectedObject, searchText);
	}
	
	void OnSelectionChange ()
	{
		ReferenceExplorerData.RestoreComponentReferenceData ();
		ReferenceExplorerData.UpdateSelectedComponentList();
		Repaint ();
	}

	private OrderType orderType = OrderType.Names;
	private System.Type currentType;
	private Vector2 classSroll, callbackScroll;
	private CurrentWindowType currentWindowType = CurrentWindowType.Class;

	private string searchText = string.Empty;
	private bool isSelectedObject;

	private string opendCallbackList = string.Empty;


	void OnGUIClass ()
	{
		using( var header = new GUILayout.HorizontalScope("box")){
			
			EditorGUI.BeginChangeCheck();
			
			isSelectedObject = GUILayout.Toggle(isSelectedObject, "selected",EditorStyles.toolbarButton, GUILayout.Width(70));
			searchText = EditorGUILayout.TextField( searchText);
			
			if( EditorGUI.EndChangeCheck()){
				CallbackData.UpdateCallbacklist(isSelectedObject, searchText);
			}
			orderType = (OrderType)EditorGUILayout.EnumPopup (orderType, EditorStyles.toolbarPopup, GUILayout.Width(95));
		}

		using (var scrollRectLayout = new GUILayout.ScrollViewScope(classSroll)) {
			classSroll = scrollRectLayout.scrollPosition;


			var componentTypeList = ReferenceExplorerData.allComponentTypes.AsEnumerable();
			var animatorBehaviourList = ReferenceExplorerData.animatorBehaviourList.AsEnumerable();

			var doc = ReferenceExplorerUtility.GetTExtCommand(searchText);
			if( doc.ContainsKey("type")){
				var typeText = doc["type"];
				componentTypeList = componentTypeList.
					Where(item => item.FullName.ToLower().IndexOf(typeText) != -1 ||
					      		  item.GetInterfaces()
					      				.Any( interfaces=> interfaces.FullName.ToLower().IndexOf(typeText) != -1));

				animatorBehaviourList = animatorBehaviourList
					.Where(item => item.behaviour.GetType().FullName.ToLower().IndexOf(typeText) != -1);
			}

			if (orderType == OrderType.Names) {
				componentTypeList = componentTypeList
					.OrderBy (t1 => t1.FullName).ToList ();

			} else {
				componentTypeList = componentTypeList
					.OrderBy (t1 => ReferenceExplorerData.allComponents.Count (t2 => t1 == t2.GetType ()) * -1).ToList ();
			}


			foreach (var type in componentTypeList) {

				Component component;
				int count;
				
				if( isSelectedObject ){
					component = ReferenceExplorerData.allSelectedComponent.Find(item => item.GetType() == type);
					count = ReferenceExplorerData.allSelectedComponent.Count(item => item.GetType() == type);
				}else{
					component = ReferenceExplorerData.allComponents.Find (item => item.GetType () == type);
					count = ReferenceExplorerData.allComponents.Count (item => item.GetType () == type);
				}

				if( count == 0) 
					continue;

				var color = GUI.backgroundColor;
				if( type == currentType ){
					GUI.backgroundColor = new Color (1f, 0.8f, 0.8f);
				}

				using (var classViewLayout = new GUILayout.VerticalScope("box")) {
					using (var itemLayout = new GUILayout.HorizontalScope()) {

						string buttontext = (currentType == type) ? "▼" :  "▶";
						if (GUILayout.Button (buttontext, EditorStyles.miniLabel, GUILayout.Width (12))) {
							if (currentType == type)
								currentType = null;
							else
								currentType = type;
						}

						if (component is MonoBehaviour) {
							var monoscript = MonoScript.FromMonoBehaviour((MonoBehaviour)component);
							EditorGUILayout.ObjectField (monoscript, type, false);
						} else {
							GUILayout.Label (type.ToString ());
						}
						
						EditorGUILayout.LabelField (count.ToString (), GUILayout.ExpandWidth (false), GUILayout.Width (40));
						if (GUILayout.Button ("F", EditorStyles.toolbarButton, GUILayout.Width (20))) {
							var components = ReferenceExplorerData.allComponents.FindAll (item => item.GetType () == type).Select (item => item.gameObject).ToArray ();
							Selection.objects = components;
						}
					}
					
					GUILayout.Space (5);
					EditorGUI.indentLevel = 1;
					
					if (type == currentType) {

						var components = isSelectedObject ? ReferenceExplorerData.allSelectedComponent : ReferenceExplorerData.allComponents;

						foreach (var obj in components
						         .FindAll(item => item.GetType() == type)
						         .OrderBy(item => item.name)
						         .AsEnumerable()) {
							EditorGUILayout.ObjectField (obj, type, true);
						}
					}
					
					EditorGUI.indentLevel = 0;
				}
				GUI.backgroundColor= color;
			}

			foreach( var type in animatorBehaviourList.Select( item => item.behaviour.GetType() ).Distinct()){
				
				if( type == null )
					continue;
				
				var animators = animatorBehaviourList
					.Where( item => item.behaviour.GetType() == type)
					.Distinct();

				if( animators.Count() == 0 )
					continue;


				var color = GUI.backgroundColor;
				if( type == currentType ){
					GUI.backgroundColor = new Color (1f, 0.8f, 0.8f);
				}

				using (var classViewLayout = new GUILayout.VerticalScope("box")) {

					using( var componentHeader = new GUILayout.HorizontalScope())
					{
						string buttontext = (currentType == type) ? "▼" :  "▶";
						if (GUILayout.Button (buttontext, EditorStyles.miniLabel, GUILayout.Width (12))) {
							if (currentType == type) { currentType = null; }
							else{		currentType = type;	}
						}
						
						var code = MonoScript.FromScriptableObject(animators.First().behaviour);
						EditorGUILayout.ObjectField(code, typeof(MonoScript), false);
					}

					if( type != currentType )
						continue;
					
					EditorGUI.indentLevel = 1;
					using( var animatotrLayouts = new GUILayout.VerticalScope()){
						foreach( var anim in animators ){
							EditorGUILayout.ObjectField( anim.animator, typeof(Animator), true);
						}
					}
					EditorGUI.indentLevel = 0;
				}
				GUI.backgroundColor= color;
			}

		}	
	}


	bool exportCallbacks = false;
	bool exportClassReference = true;
	bool exportIsComponentOnly = false;

	bool isContainFamilly = true;

	void OnGUIAnimator()
	{
		using( var title = new GUILayout.VerticalScope("box") ){
			EditorGUILayout.LabelField("export component graph", EditorStyles.boldLabel);
			EditorGUILayout.Space();
			exportClassReference = EditorGUILayout.Toggle("reference class", exportClassReference );
			exportCallbacks = EditorGUILayout.Toggle("callback", exportCallbacks );
			exportIsComponentOnly = EditorGUILayout.Toggle("component only", exportIsComponentOnly );

			if( GUILayout.Button("export")){
				ExplortReferenceMap.Export(exportClassReference, exportCallbacks, exportIsComponentOnly);
			}
		}

		using( var title = new GUILayout.VerticalScope("box") ){
			EditorGUILayout.LabelField("export object graph", EditorStyles.boldLabel);
			EditorGUILayout.Space();
			isContainFamilly = EditorGUILayout.Toggle("contain familly", isContainFamilly );
			exportCallbacks = EditorGUILayout.Toggle("callback", exportCallbacks );
			if( GUILayout.Button("export")){
				ExplortReferenceMap.ExportObjectReference(isContainFamilly, exportCallbacks);
			}
		}
	}

	/// <summary>
	/// Raises the GUI callback event.
	/// </summary>
	void OnGUICallback ()
	{
		
		using( var header = new GUILayout.HorizontalScope("box")){
			
			EditorGUI.BeginChangeCheck();
			
			isSelectedObject = GUILayout.Toggle(isSelectedObject, "selected",EditorStyles.toolbarButton, GUILayout.Width(70));
			searchText = EditorGUILayout.TextField( searchText);
			
			if( EditorGUI.EndChangeCheck()){
				CallbackData.UpdateCallbacklist(isSelectedObject, searchText);
			}
		}

		EditorGUI.BeginChangeCheck();

		callbackScroll = EditorGUILayout.BeginScrollView(callbackScroll);

		var width = position.width * 0.48f;

		foreach (var callback in CallbackData.callbackList ){


			if( callback.recieverList.Count == 0 && callback.senderList.Count == 0)
				continue;
			
			if( GUILayout.Button(callback.callback, EditorStyles.toolbarButton )){
				opendCallbackList = callback.callback;
			}

			if( opendCallbackList != callback.callback ){
				continue;
			}

			if( callback.senderList.Where(item => ReferenceExplorerUtility.GetObject(item) != null ).Count() != 0){
				EditorGUILayout.LabelField("Sender", EditorStyles.boldLabel) ;

				foreach( var type in callback.senderTypeList ){
					using ( var componentLayout = new EditorGUILayout.HorizontalScope())
					{
						var senders = callback.senderList.Where( item => item.GetType() == type);

						if( type.IsSubclassOf(typeof(MonoBehaviour))){
							var monoscript = MonoScript.FromMonoBehaviour((MonoBehaviour)callback.senderList.First());
							EditorGUILayout.ObjectField(monoscript, typeof(MonoScript), true, GUILayout.Width(width));

							using ( var referencecomponentLayout = new EditorGUILayout.VerticalScope()){
								foreach( var sender in senders ){
									EditorGUILayout.ObjectField(sender, typeof(Component), true, GUILayout.Width(width) );
								}
							}
						}
					}

					var animationSender = CallbackData.allAnimatorSender.Where(item => item.callback == callback.callback );
					foreach( var sender in animationSender ){
						using (var animatorLayout = new GUILayout.HorizontalScope())
						{
							EditorGUILayout.ObjectField(sender.clip, typeof(AnimationClip), true, GUILayout.Width(width) );
							EditorGUILayout.ObjectField(sender.sender, typeof(Animator), true, GUILayout.Width(width) );
						}
					}
				}
			}

			if( callback.recieverList.Count != 0 ){
				EditorGUILayout.LabelField("Reciever", EditorStyles.boldLabel);

				var types = callback.recieverTypeList;

				foreach( var type in types )
				{
					using ( var componentLayout = new EditorGUILayout.HorizontalScope())
					{
						var recievers = callback.recieverList.Where(item => item.GetType() == type );
						if( recievers.Count() == 0)
							continue;
						var monoscript = MonoScript.FromMonoBehaviour((MonoBehaviour) recievers.First());
						EditorGUILayout.ObjectField(monoscript, typeof(MonoScript), true, GUILayout.Width(width));
						
						using( var referenceComponentLayout = new EditorGUILayout.VerticalScope())
						{
							foreach( var reciever in recievers )
							{
								EditorGUILayout.ObjectField(reciever, typeof(MonoScript), true, GUILayout.Width(width));
							}
						}
					}
					
					GUILayout.Space(5);
				}
			}
		}

		EditorGUILayout.EndScrollView();

		EditorGUI.EndChangeCheck();
	}

	/// <summary>
	/// Raises the GU event.
	/// </summary>
	void OnGUI ()
	{
		using (var outline = new GUILayout.VerticalScope()) {

			using (var header = new GUILayout.HorizontalScope("box")) {
				if (GUILayout.Toggle (currentWindowType == CurrentWindowType.Callback, "Callbacks", EditorStyles.toolbarButton)) {
					currentWindowType = CurrentWindowType.Callback;
				}
				if (GUILayout.Toggle (currentWindowType == CurrentWindowType.Class, "Components", EditorStyles.toolbarButton)) {
					currentWindowType = CurrentWindowType.Class;
				}
				if (GUILayout.Toggle (currentWindowType == CurrentWindowType.CodeSearch, "Code", EditorStyles.toolbarButton)) {
					currentWindowType = CurrentWindowType.CodeSearch;
				}
				if (GUILayout.Toggle (currentWindowType == CurrentWindowType.Export, "Export", EditorStyles.toolbarButton)) {
					currentWindowType = CurrentWindowType.Export;
				}
			}

			switch (currentWindowType) {
			case CurrentWindowType.Callback:
				OnGUICallback ();
				break;
			case CurrentWindowType.Class:
				OnGUIClass ();
				break;
			case CurrentWindowType.Export:
				OnGUIAnimator();
				break;
			case CurrentWindowType.CodeSearch:
				codeSearch.OnGUI();
				break;
			}
		}
	}
}
