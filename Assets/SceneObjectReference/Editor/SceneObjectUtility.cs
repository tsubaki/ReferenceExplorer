using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;
using System.Reflection;

namespace terasurware
{
	public class SceneObjectUtility
	{
		public static void FindReferencedObject (GameObject selectedObject, List<ReferenceObject> objList)
		{
			if (selectedObject == null)
				return;

			Component[] allComponents = null;
			{
				var allObject = GetAllObjectsInScene (false);
				List<Component> allComponentList = new List<Component> ();
				foreach (var obj in allObject) {
					allComponentList.AddRange (obj.GetComponents<Component> ());
				}
				allComponents = allComponentList.ToArray ();
			}
			var attachedComponents = selectedObject.GetComponents<Component> ();
		
			foreach (var obj in allComponents) {
				CheckReferencedObject (obj, attachedComponents, selectedObject, obj.gameObject, obj, objList);
			}
		}

		public static void GetReferenceObject (GameObject activeGameObject, List<ReferenceObject> objList)
		{
			if (activeGameObject == null)
				return;

			var components = activeGameObject.GetComponents<Component> ();
			Debug.Log(components.Length);
			foreach (var obj in components) {
				CheckReferenceObject (obj, obj, objList);
			}
		}

#region ToObject

		static void CheckReferenceObject (object obj, Component ownerComponent, List<ReferenceObject> objList)
		{
			var type = obj.GetType ();
			var fields = type.GetFields (BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
			foreach (var field in fields) {
				CheckReferenceField (obj, field.Name, field.GetValue (obj), ownerComponent, objList);
			}

			var propertys = type.GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
			foreach (var property in propertys) {
				try{
					var propertyValue = property.GetValue (obj, null);

					//if( propertyValue != null)
					//	CheckReferenceField (propertyValue, property.Name, propertyValue, ownerComponent, objList);

				}catch(TargetParameterCountException t)
				{
					Debug.Log(property.Name);
				}
			}
		}

		static void CheckReferenceField (object value, string fieldName, object fieldValue, Component ownerComponent, List<ReferenceObject> objList)
		{
			if (fieldValue == null || fieldValue.GetType ().IsPrimitive || fieldValue is string)
				return;

			if (fieldValue is GameObject) {
				try {
					objList.Add (new ReferenceObject ()
			    {
					rootObject = (GameObject)fieldValue,
					valueType = ownerComponent.GetType(),
						fieldName = fieldName,
					value = (GameObject)fieldValue,
					thisObject = ownerComponent.gameObject,
				});
				} catch (MissingReferenceException) {
				} catch (MissingComponentException) {
				}

			} else if (fieldValue is Component) {
				var component = (Component)fieldValue;

				try {
					var refObj = new ReferenceObject ();
					refObj.rootObject = component.gameObject;
					refObj.valueType = ownerComponent.GetType ();
					refObj.fieldName = fieldName;
					refObj.value = component;
					refObj.thisObject = ownerComponent.gameObject;
					objList.Add (refObj);



				} catch (MissingReferenceException) {
				} catch (MissingComponentException) {
				} catch (UnassignedReferenceException) {
				}

			} else if (fieldValue is ICollection) {
				foreach (var item in (ICollection)fieldValue) {
					CheckReferenceField (item, fieldName, item, ownerComponent, objList);
				}
			} else {
				CheckReferenceObject (fieldValue, ownerComponent, objList);
			}
		}
#endregion


#region forObject

		static void CheckReferencedObject (
				object obj, 
				Component[] attachedComponents, 
				GameObject selectedObject, 
				GameObject root, 
				Component ownerComponent, 
				List<ReferenceObject> objList)
		{
			var type = obj.GetType ();
			var fields = type.GetFields (BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);

			foreach (var field in fields) {
				CheckReferencedField (field.GetValue (obj), attachedComponents, selectedObject, root, field, ownerComponent, objList);
			}
		}

		static void CheckReferencedField (
				object value, 
				Component[] attachedComponents, 
				GameObject selectedObject, 
				GameObject root, FieldInfo field, 
				Component ownerComponent, 
				List<ReferenceObject> objList)
		{
			if (value == null || value.GetType ().IsPrimitive || value is string)
				return;

			if (value is GameObject) {
				GameObject v = value as GameObject;
				if (v == selectedObject) {
					objList.Add (new ReferenceObject ()
				{
					rootObject = root,
					valueType = v.GetType(),
					fieldName = field.Name,
					value = ownerComponent,
					thisObject = ownerComponent.gameObject,
				});
				}
			} else if (value is Component) {
				foreach (var component in attachedComponents) {
				
					Component c = value as Component;
					if (c == component) {

						objList.Add (new ReferenceObject ()
					{
						rootObject = root,
						valueType = c.GetType(),
						fieldName = field.Name,
						value = ownerComponent,
						thisObject = ownerComponent.gameObject,
					});
					}
				}
			} else if (value is ICollection) {
				foreach (var item in (ICollection)value) {
					CheckReferencedField (item, attachedComponents, selectedObject, root, field, ownerComponent, objList);
				}
			} else {
				//CheckReferencedObject (value, attachedComponents, selectedObject, root, ownerComponent, objList);
			}
		}
#endregion

		public static List<GameObject> GetAllObjectsInScene (bool bOnlyRoot)
		{
			GameObject[] pAllObjects = (GameObject[])Resources.FindObjectsOfTypeAll (typeof(GameObject));
		
			List<GameObject> pReturn = new List<GameObject> ();
		
			foreach (GameObject pObject in pAllObjects) {
				if (bOnlyRoot) {
					if (pObject.transform.parent != null) {
						continue;
					}
				}
			
				if (pObject.hideFlags == HideFlags.NotEditable || pObject.hideFlags == HideFlags.HideAndDontSave) {
					continue;
				}
			
				if (Application.isEditor) {
					string sAssetPath = AssetDatabase.GetAssetPath (pObject.transform.root.gameObject);
					if (!string.IsNullOrEmpty (sAssetPath)) {
						continue;
					}
				}
			
				pReturn.Add (pObject);
			}
		
			return pReturn;
		}
	}

}
