using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

public class CallbackData  {

	public static List<CallMethodWithText> allMethodCallWithText = new List<CallMethodWithText> ();
	public static List<CallMethodWithText> allMethodRecieveWithText = new List<CallMethodWithText> ();

	public static List<AnimatorSender> allAnimatorSender = new List<AnimatorSender>();

	public static List<CallbackViewerInfo> callbackList = new List<CallbackViewerInfo>();

	public static readonly UnityengineCallback[] buidinCallback = new UnityengineCallback[]
	{
		new UnityengineCallback(){
			senderComponent = typeof(Application),
			callbacks = new string[]{
				"OnApplicationFocus", "OnApplicationPause", "OnApplicationQuit",
				"OnLevelWasLoaded", 			}
		},
		new UnityengineCallback(){
			senderComponent = typeof(Collider),
			callbacks = new string[]{
				"OnCollisionEnter", "OnCollisionExit", "OnCollisionStay",
				"OnTriggerEnter", "OnTriggerExit", "OnTriggerStay",
				"OnParticleCollision", 
			}
		},
		new UnityengineCallback(){
			senderComponent = typeof(Collider2D),
			callbacks = new string[]{
				"OnCollisionExit2D", "OnCollisionEnter2D", "OnCollisionStay2D", 
				"OnTriggerEnter2D","OnTriggerExit2D",  "OnTriggerStay2D", 
			}
		},
		new UnityengineCallback(){
			senderComponent = typeof(Transform),
			callbacks = new string[]{
				"OnTransformChildrenChanged", "OnTransformParentChanged",
			}
		},
		new UnityengineCallback(){
			senderComponent = typeof(AudioSource),
			callbacks = new string[]{
				"OnAudioFilterRead",
			}
		},
		new UnityengineCallback(){
			senderComponent = typeof(Animator),
			callbacks = new string[]{
				"OnAnimatorIK", "OnAnimatorMove", 
			}
		},

		new UnityengineCallback(){
			senderComponent = typeof(Camera),
			callbacks = new string[]{
				"OnBecameInvisible", "OnBecameVisible", "OnWillRenderObject",
				"OnPreCull", "OnPreRender", "OnRenderImage", "OnRenderObject", 
				"OnPostRender",
			}
		},
		new UnityengineCallback(){
			senderComponent = typeof(Joint),
			callbacks = new string[]{
				"OnJointBreak",
			}
		},
		new UnityengineCallback(){
			callbacks = new string[]{
				// monobehaviour
				"OnGUI", "Update", "Start", "Awake", "FixedUpdate", "LateUpdate", 
				"OnDisable", "OnEnable", "OnDestroy",

				// input
				"OnMouseDown", "OnMouseDrag", "OnMouseEnter",
				"OnMouseExit", "OnMouseOver", "OnMouseUp", "OnMouseUpAsButton", 

				// network
				"OnConnectedToServer", "OnControllerColliderHit", "OnDisconnectedFromServer", 
				"OnFailedToConnect", "OnFailedToConnectToMasterServer", 
				"OnSerializeNetworkView", "OnServerInitialized", "OnNetworkInstantiate",
				"OnPlayerConnected", "OnPlayerDisconnected", "OnMasterServerEvent", 

				// gizmo
				"OnDrawGizmos", "OnDrawGizmosSelected",

				// ugui callbacks
				"OnDeselect", "OnMove", "OnPointerClick", "OnPointerDown", "OnPointerEnter", "OnPointerExit",
				"OnPointerUp", "OnSelect", "OnSubmit", "OnInitializePotentialDrag", "OnBeginDrag", "OnPointerClick", 

				// editor
				"OnValidate", "Reset", 
			}
		},
	};


	static void AllSendmessageInScene ()
	{
		allMethodCallWithText.Clear ();
		allAnimatorSender.Clear();

		foreach (var monoscript in  ReferenceExplorerData.allMonoscript) {
			foreach (var text in monoscript.text.Split(';')) {
				
				if (RegisterSendmessage (monoscript, text, "SendMessage\\((?<method>.*?),.*\\)", ref allMethodCallWithText)) {			continue; 	}
				if (RegisterSendmessage (monoscript, text, "SendMessage\\((?<method>.*?)\\)", ref allMethodCallWithText)) {				continue;	}
				if (RegisterSendmessage (monoscript, text, "SendMessageUpwards\\((?<method>.*?),.*\\)", ref allMethodCallWithText)) {	continue; 	}
				if (RegisterSendmessage (monoscript, text, "SendMessageUpwards\\((?<method>.*?),.*\\)", ref allMethodCallWithText)) {	continue; 	}
				if (RegisterSendmessage (monoscript, text, "BroadcastMessage\\((?<method>.*?)\\)", ref allMethodCallWithText)) {		continue;	}
				if (RegisterSendmessage (monoscript, text, "BroadcastMessage\\((?<method>.*?)\\)", ref allMethodCallWithText)) {		continue;	}
			}
		}

		foreach( var animator in ReferenceExplorerData.allComponents.Where( item => item.GetType() == typeof(Animator) ).Select(item => item as Animator) ){
			GetAnimationEvents(animator);
		}
		allAnimatorSender = allAnimatorSender.Distinct().ToList();
	}
	
	private static bool RegisterSendmessage (MonoScript monoscript, string line, string pattern, ref List<CallMethodWithText> methodCallWithTextList)
	{
		var match = Regex.Match (line, pattern);
		if (match.Success) {
			
			string methodName = match.Groups ["method"].ToString ().Replace ("\"", "");
			methodCallWithTextList.Add (new CallMethodWithText (){ callback = methodName, monoScript = monoscript, type = monoscript.GetClass() });
			return true;
		}
		return false;
	}
	
	static void CollectionSendMessageReciever ()
	{
		allMethodRecieveWithText.Clear ();

		var monobehaviours = ReferenceExplorerData.allComponents
			.Where (item => item is MonoBehaviour)
				.Select<Component,MonoBehaviour> (item => (MonoBehaviour)item);
		
		foreach (var sender in allMethodCallWithText) {
			
			foreach (var monobehaviour in monobehaviours) {
				var method = monobehaviour.GetType ().GetMethod (sender.callback,
				                                                 System.Reflection.BindingFlags.NonPublic | 
				                                                 System.Reflection.BindingFlags.Public |
				                                                 System.Reflection.BindingFlags.Instance);
				if (method != null) {
					allMethodRecieveWithText.Add (new CallMethodWithText (){
						callback = sender.callback,
						monoScript = MonoScript.FromMonoBehaviour(monobehaviour)
					});
				}
			}
		}
		
		allMethodRecieveWithText = allMethodRecieveWithText
			.Distinct()
			.ToList ();

		var callbacks = buidinCallback.SelectMany( item => item.callbacks );

		foreach( var callback in callbacks )
		{
			foreach( var monobehaviour in monobehaviours )
			{
				var type = monobehaviour.GetType ();
				if( type.FullName.IndexOf("UnityEngine") != -1)
					continue;
				
				var method = type.GetMethod (callback,
				                             System.Reflection.BindingFlags.NonPublic | 
				                             System.Reflection.BindingFlags.Public |
				                             System.Reflection.BindingFlags.Instance);
				if (method != null) {
					allMethodRecieveWithText.Add( new CallMethodWithText(){
						callback = callback,
						monoScript = MonoScript.FromMonoBehaviour(monobehaviour),
					});
				}
			}
		}
	}

	public static void GetAnimationEvents (Animator animator)
	{
		if (animator == null)
			return;

		if( animator.runtimeAnimatorController == null )
			return;
		var anim = (UnityEditor.Animations.AnimatorController)animator.runtimeAnimatorController;

		if( anim == null )
			return;

		foreach( var clip in anim.animationClips )
		{
			foreach( var ev in AnimationUtility.GetAnimationEvents(clip) )
			{
				allAnimatorSender.Add( new AnimatorSender(){
					callback = ev.functionName,
					sender = animator,
					clip = clip
				});
				allMethodCallWithText.Add (new CallMethodWithText (){
					callback = ev.functionName, 
					type = typeof(Animator) 
				});
			}
		}
	}

	public static void UpdateSenderRecieverlist()
	{
		AllSendmessageInScene();
		CollectionSendMessageReciever();
	}

	public static void UpdateCallbacklist(bool selectedOBject, string search)
	{
		callbackList.Clear();

		var callbacks = allMethodRecieveWithText.Select(item=>item.callback).Distinct();
		var objects = Selection.gameObjects;

		foreach( var callback in callbacks ){

			var callbackviewinfo = new CallbackViewerInfo();
			callbackviewinfo.callback = callback;
			foreach( var monoScript in allMethodCallWithText.Where(item=>item.callback == callback).Select(item=>item.type).Distinct()){
				if( selectedOBject && objects != null){
					callbackviewinfo.senderList.AddRange( ReferenceExplorerData.allSelectedComponent
					                                     .Where(item=>item.GetType() == monoScript));
				}else{
					callbackviewinfo.senderList.AddRange( ReferenceExplorerData.allComponents
					                                     .Where(item=>item.GetType() == monoScript));
				}
			}

			foreach( var type in allMethodRecieveWithText
			        	.Where(item=>item.callback == callback)
			        	.Select(item=>item.monoScript.GetClass())
			        	.Distinct()){

				if( selectedOBject && objects != null){
					var collection = ReferenceExplorerData.allSelectedComponent
						.Where(item=>item.GetType() == type);
					callbackviewinfo.recieverList.AddRange(collection);
				}else{
					callbackviewinfo.recieverList.AddRange( ReferenceExplorerData.allComponents
				                                       .Where(item=>item.GetType() == type));
				}
			}
			callbackList.Add(callbackviewinfo );
		}
		if(! string.IsNullOrEmpty( search )){

			var dic = ReferenceExplorerUtility.GetTExtCommand(search);

			if( dic.ContainsKey("type") ){
				var typeText = dic["type"];
				foreach( var callback in callbackList ){
					callback.recieverList = callback.recieverList
						.Where(item => item.GetType().FullName.ToLower().IndexOf(typeText) != -1)
							.ToList();

					callback.senderList = callback.senderList
						.Where(item => item.GetType().FullName.ToLower().IndexOf(typeText) != -1)
							.ToList();
				}			
			}

			if( dic.ContainsKey("obj")){
				var objName = dic["obj"];

				foreach( var callback in callbackList ){
					callback.recieverList = callback.recieverList.Where(item=>item.name.ToLower().IndexOf(objName) != -1).ToList();
					callback.senderList = callback.senderList.Where( item => item.name.ToLower().IndexOf(objName) != -1).ToList();
				}
			}
		}

		foreach( var callback in callbackList ){

			callback.recieverTypeList = callback.recieverList.Select( item => item.GetType()).Distinct().ToList();
			callback.senderTypeList = callback.senderList.Select( item => item.GetType()).Distinct().ToList();

		}
	}
}
