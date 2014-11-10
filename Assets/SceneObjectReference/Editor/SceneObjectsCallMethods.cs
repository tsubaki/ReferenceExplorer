using UnityEngine;
using System.Collections;
using UnityEditor;
using UnityEditorInternal;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class SceneObjectsCallMethods : EditorWindow 
{
	string[] GetAnimationEvents(Animator animator)
	{
		List<string> callMethodNameList = new List<string>();

		if( animator == null )
			return callMethodNameList.ToArray();

		var anim = (UnityEditorInternal.AnimatorController)animator.runtimeAnimatorController;

		for(int i=0; i<anim.layerCount; i++)
		{
			var layer = anim.GetLayer(i);
			for( int r=0; r< layer.stateMachine.stateCount; r++)
			{
				var state = layer.stateMachine.GetState(r);
				var clip = state.GetMotion() as AnimationClip;

				foreach( var ev in AnimationUtility.GetAnimationEvents(clip) )
				{

					if(! callMethodNameList.Exists( (name)=>{ return name.Equals(ev.functionName ); }) )
					{
						callMethodNameList.Add(ev.functionName);
					}
				}
			}
		}

		return callMethodNameList.ToArray();
	}

	string[] GetAnimationEvents( Animation anim )
	{
		List<string> callMethodNameList = new List<string>();

		if( anim == null )
			return callMethodNameList.ToArray();

		foreach( var clip in AnimationUtility.GetAnimationClips(anim) )
		{
			foreach( var ev in AnimationUtility.GetAnimationEvents(clip) )
			{
				if(! callMethodNameList.Exists( (name)=>{ return name.Equals(ev.functionName ); }) )
				{
					callMethodNameList.Add(ev.functionName);
				}
			}
		}

		return callMethodNameList.ToArray();
	}

	string[] GetSendMessageMethods(GameObject obj)
	{
		List<string> callBackList = new List<string>();

		foreach( var component in  obj.GetComponents<MonoBehaviour>())
		{
			foreach( var text in MonoScript.FromMonoBehaviour(component).text.Split(';') )
			{
				var match = Regex.Matches(text, "SendMessage\\((?<call>.*?),.*\\)");
				if(  AddMatchMethod(component.name, match, callBackList)) 
					continue;

				match = Regex.Matches(text, "SendMessage\\((?<call>.*?)\\)");
				if(  AddMatchMethod(component.name, match, callBackList)) 
					continue;


				match = Regex.Matches(text, "BroadcastMessage\\((?<call>.*?)\\)");
				if(  AddMatchMethod(component.name, match, callBackList)) 
					continue;

				match = Regex.Matches(text, "BroadcastMessage\\((?<call>.*?)\\)");
				if(  AddMatchMethod(component.name, match, callBackList)) 
					continue;
			}
		}
		return callBackList.ToArray();
	}

	bool AddMatchMethod(string componentname ,MatchCollection match, List<string> callBackList)
	{
		if( match.Count != 0)
		{
			var method = componentname + "." + match[0].Groups["call"];
			
			if(! callBackList.Exists( (m) =>{ return m.Equals( method); } ) )
			{
				callBackList.Add(method);
				return true;
			}
		}
		return false;
	}

	Vector2 current;
	List<string> animationMethodList = new List<string>();
	List<string> sendMessageMethodList = new List<string>();

	
	[MenuItem("Window/Referenced/animation events")]
	static void Init ()
	{
		var window = GetWindow (typeof(SceneObjectsCallMethods)) as SceneObjectsCallMethods;
		window.title = "animation call mehod";
		window.Show ();
	}
	
	void Find()
	{
		var obj = Selection.activeGameObject;
		if( obj == null )
			return;

		var animator = obj.GetComponent<Animator>();
		var animation = obj.GetComponent<Animation>();

		animationMethodList.Clear();
		animationMethodList.AddRange( GetAnimationEvents(animation ) );
		animationMethodList.AddRange( GetAnimationEvents(animator ) );

		sendMessageMethodList.Clear();
		sendMessageMethodList.AddRange( GetSendMessageMethods(obj) );
	}

	void OnInspectorUpdate ()
	{
		Find ();
		Repaint ();
	}


	void OnGUI()
	{
		current = EditorGUILayout.BeginScrollView (current);
		
		GUIStyle buttonStyle = new GUIStyle();
		buttonStyle.margin.left = 10;
		buttonStyle.margin.top = 5;
		
		GUIStyle labelStyle = new GUIStyle(buttonStyle);
		labelStyle.fontSize = 24;

		if( animationMethodList.Count != 0 )
		{
			EditorGUILayout.LabelField("Animation", labelStyle );
			EditorGUILayout.Space();

			foreach( var methodName in animationMethodList )
			{
				EditorGUILayout.LabelField(methodName);
			}
		}

		if( sendMessageMethodList.Count != 0 )
		{
			EditorGUILayout.LabelField("SendMessage", labelStyle );
			EditorGUILayout.Space();

			foreach( var methodName in sendMessageMethodList )
			{
				EditorGUILayout.LabelField(methodName);
			}
		}

		EditorGUILayout.EndScrollView();
	}
}
