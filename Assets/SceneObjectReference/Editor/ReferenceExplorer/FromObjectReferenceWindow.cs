using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;

namespace ReferenceExplorer
{
	public class FromObjectReferenceWindow
	{
		Vector2 current;
		List<ReferenceObject> referenceObjectList = new List<ReferenceObject> ();
		List<ReferenceObjectItem> refCompItems = new List<ReferenceObjectItem>();

		Texture fromRefImage;

		public bool ignoreSelfReference = false;

		public FromObjectReferenceWindow()
		{
			fromRefImage = AssetDatabase.LoadAssetAtPath ("Assets/SceneObjectReference/Editor/fromRef.png", typeof(Texture2D)) as Texture2D;
		}

		void OnEnable ()
		{
			SceneObjectUtility.UpdateGlovalReferenceList ();

			SceneView.onSceneGUIDelegate -= OnSceneGUI;
			SceneView.onSceneGUIDelegate += OnSceneGUI;
		}
	
		void OnDisable ()
		{
			SceneView.onSceneGUIDelegate -= OnSceneGUI;
		}


		void OnHierarchyChange ()
		{
			SceneObjectUtility.UpdateGlovalReferenceList ();
		}
	
		public void OnSelectionChange ()
		{
			ReferenceUpdate();
			UpdateCache();
		}
	
		public void OnSceneGUI (SceneView sceneView)
		{
			if( Selection.activeGameObject == null )
				return;

			foreach( var selection in Selection.gameObjects)
			{
				SceneGuiLineWriter(selection);
			}
		}

		public void SceneGuiLineWriter(GameObject selection)
		{
			var cameraTransform = SceneView.currentDrawingSceneView.camera.transform;
			var rotate = cameraTransform.rotation;
			var cameraPos = cameraTransform.position;
			
			Color shadowCol = new Color (0, 0, 0.5f, 0.06f);
			
			var enableTypeList = refCompItems.FindAll( item => item.isDisplay == true );

			var referenceList = referenceObjectList.FindAll(item => SceneObjectUtility.GetGameObject(item.value) == selection );

			foreach (var refs in referenceList) {

				if(! enableTypeList.Exists( item => item.componentType == refs.referenceComponent.GetType()))
					continue;

				var obj = SceneObjectUtility.GetGameObject (refs.referenceComponent);
				
				var startPosition = selection.transform.position;
				var endPosition = obj.transform.position;
				
				var size = Vector3.Distance (endPosition, cameraPos) * 0.02f;
				
				if (startPosition == endPosition)
					continue;
				
				Handles.color = Color.blue;
				
				var diffPos = startPosition - endPosition;
				var tan = new Vector3 (diffPos.y, diffPos.x, diffPos.z);
				
				
				var startTan = startPosition;
				var endTan = endPosition + tan * 0.4f;
				
				Handles.CircleCap (1, endPosition, rotate, size);
				
				for (int i=0; i<3; i++)
					Handles.DrawBezier (startPosition, endPosition, startTan, endTan, shadowCol, null, (i + 1) * 5);
				Handles.DrawBezier (startPosition, endPosition, startTan, endTan, Color.blue, null, 1);
				Handles.Label (endPosition, obj.name);
			}	
		}

		public void ReferenceUpdate()
		{
			referenceObjectList.Clear ();
			
			foreach( var selection in Selection.gameObjects)
			{
				SceneObjectUtility.FindReferenceObject (selection, referenceObjectList);
			}
			
			
			if( ignoreSelfReference )
			{
				foreach( var selection in Selection.gameObjects )
				{
					referenceObjectList.RemoveAll( item => item.referenceComponent.gameObject == selection );
				}
			}
			
			referenceObjectList.RemoveAll( item => (item.value is Component || item.value is GameObject) == false);
			
			referenceObjectList.Sort ((x, y) => GetObjectID (x.referenceComponent) - GetObjectID (y.referenceComponent));
			
			refCompItems.Clear();
			foreach (var referenceObject in referenceObjectList) {
				if (! refCompItems.Exists( item => item.componentType == referenceObject.referenceComponent.GetType())) {
					refCompItems.Add (new ReferenceObjectItem(){ 
						componentType = referenceObject.referenceComponent.GetType(),
						isDisplay = true,
					});
				}
			}
		}

		int GetObjectID (object obj)
		{
			if (obj is Component)
				return ((Component)obj).GetInstanceID ();
			if (obj is GameObject)
				return ((GameObject)obj).GetInstanceID ();

			return -1;
		}
	
		List<Cache> cachees = new List<Cache>();

		void UpdateCache()
		{
			cachees.Clear();
			foreach (var type in refCompItems) {

				var c = new Cache();
				c.type = type;
				cachees.Add( c );
				c.components = referenceObjectList.FindAll (item => item.referenceComponent.GetType() == type.componentType);

				if( c.components[0].referenceComponent is MonoBehaviour )
				{
					c.monoscript = MonoScript.FromMonoBehaviour((MonoBehaviour)c.components[0].referenceComponent);
				}
			}
		}

		public void OnGUI ()
		{	
			GUIStyle styles = new GUIStyle ();
			styles.margin.left = 10;
			styles.margin.top = 5;

			EditorGUILayout.BeginHorizontal("box");
			
			var iconSize = EditorGUIUtility.GetIconSize();
			EditorGUIUtility.SetIconSize(new Vector2(16, 16));
			GUILayout.Label(fromRefImage);
			EditorGUIUtility.SetIconSize(iconSize);
			
			EditorGUILayout.LabelField("Reference from any objects");
			
			EditorGUILayout.EndHorizontal();

			current = EditorGUILayout.BeginScrollView (current);



			try {

				foreach (var cache in cachees) {

					var components = cache.components;

					EditorGUILayout.BeginVertical ("box");

					EditorGUILayout.BeginHorizontal();
					EditorGUI.BeginChangeCheck();
					cache.type.isDisplay = EditorGUILayout.Foldout (cache.type.isDisplay, cache.type.componentType.Name);
					if( EditorGUI.EndChangeCheck() )
						SceneView.RepaintAll();

					EditorGUILayout.EndHorizontal();

					if( cache.type.isDisplay == false ){
						EditorGUILayout.EndVertical();
						continue;
					}

					EditorGUI.indentLevel = 1;

					if( cache.monoscript != null )
					{
						EditorGUILayout.ObjectField("script",  cache.monoscript , typeof(MonoScript), true);
					}


					foreach (var toComp in components) {
						EditorGUILayout.BeginHorizontal ();
						EditorGUILayout.ObjectField(toComp.referenceMemberName,  toComp.referenceComponent.gameObject , typeof(GameObject), true);
						EditorGUILayout.EndHorizontal ();
					}
					EditorGUILayout.EndVertical ();

					EditorGUI.indentLevel = 0;

				}

			} catch {
				referenceObjectList.Clear ();
			}


			EditorGUILayout.EndScrollView ();
		}

	}

	class Cache
	{
		public ReferenceObjectItem type;
		public MonoScript monoscript;
		public List<ReferenceObject> components = new List<ReferenceObject>();
	}
}
