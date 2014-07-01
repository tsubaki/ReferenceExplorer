using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using System.Reflection;

public class GetComponentsP : MonoBehaviour {

	[MenuItem("Component/Hoge")]
	public static void GetComponents()
	{
		var obj = Selection.activeGameObject;
		if( obj == null ){
			return;
		}

		Queue<RefObject> objects = new Queue<RefObject>();

		foreach( var component in obj.GetComponents<Component>())
		{

			var type = component.GetType();

			foreach( var field in type.GetFields(
				BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance |
				BindingFlags.Static | BindingFlags.DeclaredOnly))
			{
				var item = new RefObject(){
					rootObject = obj,
					rootComponent = component,
					value = field.GetValue(component),
					memberName = field.Name,
				};
				AddObject(item, objects);
			}

			foreach( var property in type.GetProperties(
				BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
			{
				var item = new RefObject(){
					rootObject = obj,
					rootComponent = component,
					value = property.GetValue(component, null),
					memberName = property.Name,
				};
				AddObjectWithoutSelfObject(item, objects);
			}
		}

		foreach( var collectedObj in objects)
		{
			Debug.Log(collectedObj.rootComponent.name + "." + collectedObj.memberName + "/" + collectedObj.value);
		}
	}

	private static void AddObjectWithoutSelfObject(RefObject refObject, Queue<RefObject> objects)
	{
		var value = refObject.value as Object;
		if (value == null || value.GetType ().IsPrimitive)
			return;

		if(value is GameObject)
		{
			var obj = value as GameObject;
			if( obj != refObject.rootObject )
				objects.Enqueue(refObject);
		}else if(value is Component){
			var component = value as Component;
			if(component.gameObject != refObject.rootObject)
				objects.Enqueue(refObject);
		}
	}

	private static void AddObject(RefObject refObject, Queue<RefObject> objects)
	{
		var value = refObject.value as Object;
		if (value == null || value.GetType ().IsPrimitive)
			return;
		
		if(value is GameObject)
		{
			objects.Enqueue(refObject);
		}else if(value is Component){
			objects.Enqueue(refObject);
		}
	}

	struct RefObject
	{
		public object value;
		public GameObject rootObject;
		public Component rootComponent;
		public string memberName;
	}
}
