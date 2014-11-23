using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;

namespace ReferenceExplorer
{
	public class SceneObjectUtility
	{

		static readonly System.Type[] ignoreTypes =
		{
			typeof(Mesh), typeof(Material), typeof(MeshFilter), typeof(MeshRenderer),
			typeof(string), typeof(SpriteRenderer), typeof(ParticleSystem), typeof(Renderer),
			typeof(ParticleSystemRenderer), typeof(Animator), typeof(SkinnedMeshRenderer), typeof(NavMesh),
			typeof(Shader), typeof(AnimationCurve), typeof(Color), typeof(System.Collections.Hashtable)
		};
		static readonly string[] ignoreMember =
		{
			"root", "parent", "particleEmitter", "rigidbody", "canvas",
			"rigidbody2D", "camera", "light", "animation", "parentInternal",
			"constantForce", "gameObject", "guiText", "guiTexture", "attachedRigidbody",
			"hingeJoint", "networkView", "particleSystem", "renderer",
			"tag", "transform", "hideFlags", "name", "audio", "collider2D", "collider", "material", "mesh",
			"Material", "material", "Color", "maxVolume", "minVolume", "rolloffFactor", "GetRemainingDistance",
			"guiElement",
		};

		static readonly System.Type[] ignoreClassTypes =
		{
			typeof(Behaviour), typeof(Component), typeof(MonoBehaviour), typeof(Transform)
		};

		static readonly System.Type[] primitive =
		{
			typeof(int), typeof(float), typeof(short), typeof(double), typeof(long), typeof(bool)
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

		public static List<System.Type> SceneUniqueComponentName()
		{
			List<System.Type> uniqueTypeList = new List<System.Type>();
			foreach( var component in allComponents )
			{
				if( !uniqueTypeList.Contains( component.GetType() ) )
					uniqueTypeList.Add(component.GetType()) ;
			}

			foreach( var removeClass in ignoreClassTypes )
			{
				uniqueTypeList.Remove( removeClass );
			}

			return uniqueTypeList;
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



		static void CollectObjectParameter (object obj, Component component, List<ReferenceObject>objectList, int hierarchy)
		{
			try {
				hierarchy ++;
				if( hierarchy > 3 )
					return;

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

					if( value is GameObject || value is Component )
					{
						var item = GetGameObject(value);
						if( item == null )
							continue;
					}

					if( System.Array.Exists<System.Type>( primitive, item => item == value.GetType() ) )
						continue;



					if (field.FieldType.GetCustomAttributes (typeof(System.SerializableAttribute), false).Length != 0) {
						CollectObjectParameter (value, component, objectList, hierarchy);
					
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
			CollectObjectParameter (component, component, objectList, 0);
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



		public static bool AddMatchMethod (string source, Component calledComponent, string patterm, List<CallbackCallObject> callbackCallObjectList)
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

		public static void GetAnimationEvents (Animator animator, List<ANimationCallbackObject> callbackCallObjectList)
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
						ANimationCallbackObject item = callbackCallObjectList.Find ((name) => {
							return name.method.Equals (ev.functionName); });
						if (item == null) {
							item = new ANimationCallbackObject ();
							item.method = ev.functionName;
							item.clip = clip;
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
	}


}
