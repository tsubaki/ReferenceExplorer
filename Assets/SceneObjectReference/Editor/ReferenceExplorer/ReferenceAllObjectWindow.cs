using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace ReferenceExplorer
{
	public class ReferenceAllObjectWindow : EditorWindow
	{

		Vector2 current = new Vector2 ();
		static Texture2D objectIcon, toIcon, fromIcon;

		TagAndLayerList tagAndLayers = new TagAndLayerList();

		bool isVisible = false;

		public enum ReferenceType
		{
			To_Any_Objects,
			From_Any_Objects,
			Components,
			TagAndLayers,
		}

		ReferenceType refType = ReferenceType.Components;
		
		[MenuItem("Window/Referenced/Scene Inspector")]
		static void Init ()
		{
			var window = GetWindow (typeof(ReferenceAllObjectWindow));
			window.title = "all";
			window.Show ();
		}

		public ReferenceAllObjectWindow()
		{
			objectIcon = EditorGUIUtility.Load("Icons/Generated/PrefabNormal Icon.asset") as Texture2D;
			toIcon = AssetDatabase.LoadAssetAtPath ("Assets/SceneObjectReference/Editor/toRef.png", typeof(Texture2D)) as Texture2D;
			fromIcon =  AssetDatabase.LoadAssetAtPath ("Assets/SceneObjectReference/Editor/fromRef.png", typeof(Texture2D)) as Texture2D;
		}

		void OnInspectorUpdate ()
		{
			Repaint ();
		}
		
		List<GameObject> allObject;

		

		void OnHierarchyChange ()
		{
			if( refType == ReferenceType.TagAndLayers )
				tagAndLayers.UpdateTagAndLayers();
		}


		void OnFocus()
		{
			isVisible = true;
			UpdateAllObject();

			tagAndLayers.UpdateTagAndLayers();
		}

		void OnLostFocus()
		{
			isVisible = false;
		}

		void UpdateAllObject ()
		{
			allObject = SceneObjectUtility.GetAllObjectsInScene (false);
			allObject.Sort( (x, y) =>{ return System.String.Compare(x.name, y.name); });

			if( SceneObjectUtility.SceneReferenceObjects.Length == 0 )
				SceneObjectUtility.UpdateReferenceList();
		}


		void SelectTargetComponent(MonoScript target)
		{
			var allObject = SceneObjectUtility.GetAllObjectsInScene(false);

			var haveComponentObjects = allObject.FindAll( item => item.GetComponent(target.name) != null);

			Selection.objects = haveComponentObjects.ToArray();
		}

		void OnGUIIconLabel(Texture2D icon, Vector2 size, params GUILayoutOption[] options)
		{
			var iconSize = EditorGUIUtility.GetIconSize();
			EditorGUIUtility.SetIconSize(size);
			GUILayout.Label(icon, options);
			EditorGUIUtility.SetIconSize(iconSize);
		}

		void OnGUIAllObjectReferenceTo()
		{
			if(! isVisible ){
				EditorGUILayout.BeginHorizontal("box");
				OnGUIIconLabel(toIcon, new Vector2(16,16));
				EditorGUILayout.LabelField("lost focus");
				EditorGUILayout.EndHorizontal();
				return;
			}

			EditorGUILayout.BeginHorizontal("box");
			OnGUIIconLabel(toIcon, new Vector2(16,16));
			EditorGUILayout.LabelField("reference to any objects");
			EditorGUILayout.EndHorizontal();

			foreach( var obj in allObject )
			{
				if( obj == null)
					continue;
				
				var compList = System.Array.FindAll<ReferenceObject>(SceneObjectUtility.SceneReferenceObjects, item => item.referenceComponent.gameObject == obj );
				if( compList.Length == 0)
					continue;
				
				EditorGUILayout.BeginVertical("box");

				EditorGUILayout.BeginHorizontal();

				EditorGUI.indentLevel = 0;
				OnGUIIconLabel(objectIcon, new Vector2(16,16), GUILayout.Width(18) );
				EditorGUILayout.ObjectField( obj, typeof(GameObject) );
				EditorGUILayout.EndHorizontal();
				
				EditorGUI.indentLevel = 1;
				foreach( var comp in compList )
				{
					EditorGUILayout.BeginHorizontal();
					if( comp.referenceComponent is MonoBehaviour ){
						var monoscript = MonoScript.FromMonoBehaviour((MonoBehaviour) comp.referenceComponent);
						EditorGUILayout.ObjectField( monoscript, typeof(MonoScript));
					}else{
						EditorGUILayout.LabelField(comp.referenceComponent.GetType().Name);
					}
					EditorGUILayout.ObjectField( comp.referenceMemberName, comp.referenceComponent, comp.referenceComponent.GetType() );
					EditorGUILayout.EndHorizontal();
				}
				EditorGUI.indentLevel = 0;
				EditorGUILayout.EndVertical();
			}
		}

		void OnGUIAllComponent()
		{
			EditorGUILayout.BeginHorizontal("box");
			OnGUIIconLabel(objectIcon, new Vector2(16,16));
			EditorGUILayout.LabelField("all component on scene");
			EditorGUILayout.EndHorizontal();
			List<MonoScript> uniqueMonoscript =  new List<MonoScript>();
			foreach( var component in SceneObjectUtility.SceneComponents)
			{
				var monobehaviour = component as MonoBehaviour;
				if( monobehaviour == null)
					continue;

				var monoscript = MonoScript.FromMonoBehaviour((MonoBehaviour) monobehaviour);

				if(! uniqueMonoscript.Contains( monoscript ) )
					uniqueMonoscript.Add(monoscript);
			}

			foreach( var monoscript in uniqueMonoscript )
			{
				EditorGUILayout.BeginHorizontal();
				if( GUILayout.Button("F", GUILayout.Width(20)) )
					SelectTargetComponent(monoscript);
				EditorGUILayout.ObjectField( monoscript, typeof(MonoScript));
				EditorGUILayout.EndHorizontal();
			}
		}

		void OnGUIAllObjectReferenceFrom()
		{
			if(! isVisible ){
				EditorGUILayout.BeginHorizontal("box");
				OnGUIIconLabel(fromIcon, new Vector2(16,16));
				EditorGUILayout.LabelField("lost focus");
				EditorGUILayout.EndHorizontal();
				return;
			}

			EditorGUILayout.BeginHorizontal("box");
			OnGUIIconLabel(fromIcon, new Vector2(16,16));
			EditorGUILayout.LabelField("reference from any objects");
			EditorGUILayout.EndHorizontal();


			foreach( var obj in allObject )
			{
				var referenceFromTargetObjectList = System.Array.FindAll<ReferenceObject>( SceneObjectUtility.SceneReferenceObjects,
				                                                                          item => SceneObjectUtility.GetGameObject( item.value) == obj );
				
				if( referenceFromTargetObjectList.Length == 0)
					continue;
				
				EditorGUI.indentLevel = 0;
				EditorGUILayout.BeginVertical("box");
				EditorGUILayout.BeginHorizontal();
				OnGUIIconLabel(objectIcon, new Vector2(16,16), GUILayout.Width(18) );
				EditorGUILayout.ObjectField( obj, typeof(GameObject) );
				EditorGUILayout.EndHorizontal();


				EditorGUI.indentLevel = 1;
				foreach( var referenceObject in referenceFromTargetObjectList )
				{
					EditorGUILayout.BeginHorizontal();
					if( referenceObject.referenceComponent is MonoBehaviour ){
						var monoscript = MonoScript.FromMonoBehaviour((MonoBehaviour) referenceObject.referenceComponent);
						EditorGUILayout.ObjectField( monoscript, typeof(MonoScript) );
					}else{
						EditorGUILayout.LabelField(referenceObject.referenceComponent.GetType().Name);
					}
					
					EditorGUILayout.ObjectField(referenceObject.referenceMemberName, referenceObject.referenceComponent, referenceObject.referenceComponent.GetType());
					EditorGUILayout.EndHorizontal();
				}
				EditorGUILayout.EndVertical();
				EditorGUI.indentLevel = 0;
			}
		}

		void OnGUI ()
		{	


			EditorGUILayout.BeginHorizontal ();


			refType = (ReferenceType) EditorGUILayout.EnumPopup( refType , EditorStyles.toolbarPopup );
			
			EditorGUILayout.EndHorizontal ();



			GUIStyle styles = new GUIStyle ();
			styles.margin.left = 10;
			styles.margin.top = 5;
			
			current = EditorGUILayout.BeginScrollView (current);

			
			try {
				switch(refType)
				{
				case ReferenceType.From_Any_Objects:
					OnGUIAllObjectReferenceFrom();
					break;
				case ReferenceType.To_Any_Objects:
					OnGUIAllObjectReferenceTo();
					break;
				case ReferenceType.Components:
					OnGUIAllComponent();
					break;
				case ReferenceType.TagAndLayers:
					tagAndLayers.OnGUI();
					break;
				}


			} catch (UnityEngine.ExitGUIException e) {
				Debug.LogWarning (e.ToString ());
			} catch (System.Exception e) {
				Debug.LogWarning (e.ToString ());
			}
			
			EditorGUILayout.EndScrollView ();
		}

	}
}


