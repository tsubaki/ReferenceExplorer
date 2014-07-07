using UnityEngine;
using System.Collections;

public class ComponentAccessG2 : MonoBehaviour
{
	public delegate void Test();
	
	
	public event Test act;
	
	public void Start ()
	{
		act += Callback;
		
		/*
		foreach (var item in act.GetInvocationList()) {
			
			if( act.Target is Component )
			{
				var comp = act.Target as Component;
				if( comp != null)
					Debug.Log (comp.name);
			}
			if( act.Target is GameObject )
			{
				var obj = act.Target as GameObject;
				if( obj != null)
					Debug.Log (obj.name);
			}
		}
		*/
	}
	
	public void Callback ()
	{
	}
}
