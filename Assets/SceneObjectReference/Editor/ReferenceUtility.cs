using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using System.Reflection;
using terasurware;

public class ReferenceUtility {

	[MenuItem("Component/Hoge")]
	public static void GetComponents()
	{
		var obj = Selection.activeGameObject;
		if( obj == null ){
			return;
		}

		Queue<ReferenceObject> objects = new Queue<ReferenceObject>();

		foreach( var component in obj.GetComponents<Component>())
		{

			var type = component.GetType();

			foreach( var field in type.GetFields(
				BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance |
				BindingFlags.Static | BindingFlags.DeclaredOnly))
			{
				var item = new ReferenceObject(){
					rootComponent = component,
					value = field.GetValue(component),
					memberName = field.Name,
				};
				AddObject(item, objects);
			}

			foreach( var property in type.GetProperties(
				BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
			{
				var item = new ReferenceObject(){
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

	private static void AddObjectWithoutSelfObject(ReferenceObject refObject, Queue<ReferenceObject> objects)
	{
		var value = refObject.value as Object;
		if (value == null || value.GetType ().IsPrimitive)
			return;

		if(value is GameObject)
		{
			var obj = value as GameObject;
			if( obj != refObject.rootComponent.gameObject )
				objects.Enqueue(refObject);
		}else if(value is Component){
			var component = value as Component;
			if(component.gameObject != refObject.rootComponent.gameObject)
				objects.Enqueue(refObject);
		}
	}

	private static void AddObject(ReferenceObject refObject, Queue<ReferenceObject> objects)
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
}
