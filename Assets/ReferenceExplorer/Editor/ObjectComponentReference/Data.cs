using UnityEngine;
using System.Collections;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;

public class ReferenceInfo
{
	public string referenceName = string.Empty;
	public System.Object fromObject = null;
	public System.Object referenceTarget = null;
}

public class ReferenceViewerClassbase
{
	public System.Type type;
	public List<ReferenceInfo> referenceInfoList;
}

public class CallbackViewerInfo
{
	public string callback;
	public List<Component> senderList = new List<Component> ();
	public List<Component> recieverList = new List<Component> ();
	public List<Type> recieverTypeList = new List<Type> ();
	public List<Type> senderTypeList = new List<Type> ();
}

public class UnityengineCallback
{
	public string[] callbacks;
	public Type senderComponent;
}

public class CallMethodWithText
{
	public string callback;
	public MonoScript monoScript;
	public Type type;

	public override int GetHashCode ()
	{
		return (callback + monoScript).GetHashCode ();
	}

	public override bool Equals (object obj)
	{
		var item = (CallMethodWithText)obj;
		return callback == item.callback && monoScript == item.monoScript;
	}
}

public class AnimatorSender
{
	public Animator sender;
	public AnimationClip clip;
	public string callback;

	public override int GetHashCode ()
	{
		return sender.GetInstanceID () + clip.GetInstanceID () + callback.GetHashCode ();
	}

	public override bool Equals (object obj)
	{
		var item = (AnimatorSender)obj;
		return 	sender.GetInstanceID () == item.sender.GetInstanceID () && 
			clip.GetInstanceID () == item.clip.GetInstanceID () &&
			callback == item.callback;
	}
}

public class AnimatorBehaviourInfo
{
	public Animator animator;
	public StateMachineBehaviour behaviour;

	public override int GetHashCode ()
	{
		return animator.GetInstanceID ();
	}

	public override bool Equals (object obj)
	{
		var item = (AnimatorBehaviourInfo)obj;
		return 	animator.GetInstanceID () == item.animator.GetInstanceID ();
	}
}

public class IgnoreComponents
{
	private static Type[] ignoreReferenceComponentType = new Type[]
	{
		typeof(UnityEngine.EventSystems.StandaloneInputModule),
		typeof(Mesh), typeof(Material), typeof(MeshFilter), typeof(MeshRenderer),
		typeof(string), typeof(UnityEngine.AI.NavMesh), typeof(Shader), typeof(AnimationCurve), typeof(Color), typeof(System.Collections.Hashtable)
	};
	
	public static bool IsNotAnalyticsTypes (System.Object obj)
	{
		if (obj == null)
			return false;
		var type = obj.GetType ();
		return ignoreReferenceComponentType.Any (item => item.IsAssignableFrom (type)) == true;
	}
}

public class ComponentWithEnable
{
	public Type componentType;
	public bool isShow;
}

public enum OrderType
{
	Names,
	ReferenceCount
}

public enum ReferenceIgnoreType
{
	IgnoreSelf,
	IgnoreFamilly,
	None
}