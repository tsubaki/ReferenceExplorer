using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;
using System.Reflection;

namespace ReferenceExplorer
{
	public class SceneObjectUtility
	{

		static readonly System.Type[] ignoreTypes =
		{
			typeof(Mesh), typeof(Material), typeof(MeshFilter), typeof(MeshRenderer),
			typeof(string), typeof(SpriteRenderer), typeof(ParticleSystem), typeof(Renderer),
			typeof(ParticleSystemRenderer), typeof(Animator), typeof(SkinnedMeshRenderer), typeof(NavMesh),
			typeof(Shader), typeof(AnimationCurve), typeof(Color)
		};
		static readonly string[] ignoreMember =
		{
			"root", "parent", "particleEmitter", "rigidbody", "canvas",
			"rigidbody2D", "camera", "light", "animation", "parentInternal",
			"constantForce", "gameObject", "guiText", "guiTexture",
			"hingeJoint", "networkView", "particleSystem", "renderer",
			"tag", "transform", "hideFlags", "name", "audio", "collider2D", "collider", "material", "mesh",
			"Material", "material", "Color", "maxVolume", "minVolume", "rolloffFactor", "GetRemainingDistance",
			"guiElement",
		};
		static readonly string[] ignoreEvents =
		{
			"onRequestRebuild",
		};
		static List<Component> allComponents = new List<Component>();
		static List<ReferenceObject> glovalReferenceList = new List<ReferenceObject> ();

		public static Component[] SceneComponents{
			get{ return allComponents.ToArray();}
		}

		public static ReferenceObject[] SceneReferenceObjects{
			get{ return glovalReferenceList.ToArray(); }
		}


		public static void UpdateReferenceList ()
		{
			CollectionAllComponent ();
		}

		public static void UpdateGlovalReferenceList ()
		{
			glovalReferenceList.Clear ();
			foreach (var item in GetAllObjectsInScene (false)) {
				GetReferenceObject (item, glovalReferenceList);
			}
			
		}

		public static bool IsIgnoreType (System.Type type)
		{
			return System.Array.Exists<System.Type> (ignoreTypes, (item) => item == type);
		}

		public static bool IsIgnoreMember (System.Type type, string parameter)
		{
			string checkParameterName = parameter;
			return System.Array.Exists<string> (ignoreMember, (item) => item == checkParameterName);
		}

		public static void CollectionAllComponent ()
		{
			allComponents.Clear();
			var allObject = GetAllObjectsInScene (false);

			foreach (var obj in allObject) {
				allComponents.AddRange (obj.GetComponents<Component> ());
			}
		}

		public static void GetReferenceObject (GameObject activeGameObject, List<ReferenceObject> objectList)
		{
			if (activeGameObject == null)
				return;

			foreach (var component in activeGameObject.GetComponents<Component>()) {
				CollectComponentParameter (component, objectList);
			}
		}

		public static void FindReferenceObject (GameObject activeGameObject, List<ReferenceObject> objectList)
		{
			if (activeGameObject == null)
				return;


			List<object> refList = new List<object> ();


			foreach (var item in activeGameObject.GetComponents<Component>()) {
				refList.Add (item);
			}
			refList.Add (activeGameObject);



			foreach (var item in glovalReferenceList) {
				if ( refList.Exists ( r => r == item.value)) {
					objectList.Add (item);
				}
			}
		}
		
		public static GameObject GetGameObject (object target)
		{
			try {
				if (target is GameObject) {
					return (GameObject)target;
				}
				if (target is Component) {
					return ((Component)target).gameObject;
				}
			} catch (UnassignedReferenceException) {
			}
			return null;
		}
		
		static System.Action act2;



		static void CollectObjectParameter (object obj, Component component, List<ReferenceObject>objectList)
		{
			try {


				if (obj == null)
					return;
				var type = obj.GetType ();

				if (IsIgnoreType (type))
					return;
				if (obj == null)
					return;
				if (type.IsPrimitive || type.IsEnum)
					return;


				foreach (var field in type.GetFields(
					BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance |
					BindingFlags.Static | BindingFlags.DeclaredOnly)) {
				
					if (IsIgnoreMember (field.FieldType, field.Name))
						continue;

					var value = field.GetValue (obj);
					if (value == null)
						continue;

					if (field.FieldType.GetCustomAttributes (typeof(System.SerializableAttribute), false).Length != 0) {
						CollectObjectParameter (value, component, objectList);
					
					} else {
						var item = new ReferenceObject (){
						referenceComponent = component,
						value = value,
						referenceMemberName = field.Name,
					};
						AddObject (item, objectList, true);
					}
					continue;
				}

				foreach (var ev in type.GetEvents()) {
				
					if (System.Array.AsReadOnly<string> (ignoreEvents).Contains (ev.Name)) {
						continue;
					}
				
					var fi = type.GetField (ev.Name, 
						BindingFlags.Static | 
						BindingFlags.NonPublic | 
						BindingFlags.Instance | 
						BindingFlags.Public | 
						BindingFlags.FlattenHierarchy);
				
					var del = (System.Delegate)fi.GetValue (obj);
				
				
					if (del == null) {
						continue;
					}

					var list = del.GetInvocationList ();
				
					foreach (var item in list) {
						if (item.Target is Component) {
							var c = item.Target as Component;
							if (c == null)
								continue;

							var refObject = new ReferenceObject (){
							referenceComponent = component,
							value = c,
							referenceMemberName = ev.Name + "(" + item.Method.Name + ")",
						};						
							AddObject (refObject, objectList, true);				
						}
						if (item.Target is GameObject) {
							var go = item.Target as GameObject;
							if (go == null)
								continue;

							var refObject = new ReferenceObject (){
							referenceComponent = component,
								value = go,
								referenceMemberName = ev.Name + "(" + item.Method.Name + ")",
							};
							AddObject (refObject, objectList, true);		
							continue;
						}
					}
				
				}
			

				// property Instability

				foreach (var property in type.GetProperties(
					BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance )) {
					if (IsIgnoreMember (property.PropertyType, property.Name))
						continue;

					if (IsIgnoreType (property.PropertyType))
						continue;

					var value = property.GetValue (obj, null);
					if (value == null)
						continue;

					var item = new ReferenceObject (){
						referenceComponent = component,
						value = value,
						referenceMemberName = property.Name,
					};
						
					AddObject (item, objectList, false);
					continue;
				}

			}catch (System.ArgumentException){
			} catch (System.Exception e) {
				Debug.LogWarning (e.ToString ());

			}
		}

		static void CollectComponentParameter (Component component, List<ReferenceObject>objectList)
		{
			CollectObjectParameter (component, component, objectList);
		}

		private static void AddObject (ReferenceObject refObject, List<ReferenceObject> objectList, bool isAllowSameObject)
		{

			var value = refObject.value;
			if (value == null)
				return;
			
			if (value is GameObject) {


				var obj = value as GameObject;
				if (obj != refObject.referenceComponent.gameObject || isAllowSameObject == true)
					objectList.Add (refObject);
			
			} else if (value is Component) {

				Component component = (Component)value;

				if (component == null)
					return;

				if (component.gameObject != refObject.referenceComponent.gameObject || isAllowSameObject == true)
					objectList.Add (refObject);

			} else if (value is ICollection) {

				foreach (var item in (ICollection)value) {
					var nestItem = new ReferenceObject (){
						referenceComponent = refObject.referenceComponent,
						value = item,
						referenceMemberName = refObject.referenceMemberName,
					};
					AddObject (nestItem, objectList, isAllowSameObject);
				}
			}
		}

		public static List<GameObject> GetAllObjectsInScene (bool bOnlyRoot)
		{
			GameObject[] pAllObjects = null;

			try{
				pAllObjects = (GameObject[])Resources.FindObjectsOfTypeAll (typeof(GameObject));
			}catch{
				Debug.LogWarning("get all object faild, try again");
				return new List<GameObject>();
			}
		
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
