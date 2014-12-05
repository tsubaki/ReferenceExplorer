using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ReferenceExplorer
{
	public class ToReferenceWindow
	{

		static readonly string[] getComponentFunctionPattern = new string[]
		{
			"GetComponent\\<(?<call>.*?)\\>",
			"GetComponents\\<(?<call>.*?)\\>",
			"GetComponentInChildren\\<(?<call>.*?)\\>",
			"GetComponentsInChildren\\<(?<call>.*?)\\>",
			"GetComponentInParent\\<(?<call>.*?)\\>",
			"GetComponentsInParent\\<(?<call>.*?)\\>",
		};

		public ToReferenceWindow()
		{
			toRefImage = AssetDatabase.LoadAssetAtPath ("Assets/SceneObjectReference/Editor/toRef.png", typeof(Texture2D)) as Texture2D;
		}

		List<ReferenceObject> referenceObjectList = new List<ReferenceObject> ();
		List<PerhapsReferenceObject> perhapsReferenceObjectList = new List<PerhapsReferenceObject> ();
		List<ReferenceObjectItem> refCompItems = new List<ReferenceObjectItem>();

		Texture toRefImage;
		public bool ignoreSelfReference = false;
		public bool findWillAccessComponent = false;
	
		void OnEnable ()
		{
			SceneView.onSceneGUIDelegate -= OnSceneGUI;
			SceneView.onSceneGUIDelegate += OnSceneGUI;
		}
	
		void OnDisable ()
		{
			SceneView.onSceneGUIDelegate -= OnSceneGUI;
		}
		
		Vector2 current;

		public void OnSelectionChange ()
		{
			referenceObjectList.Clear ();
			perhapsReferenceObjectList.Clear ();

			SceneObjectUtility.UpdateReferenceList ();
		
			foreach( var selection in Selection.gameObjects )
			{
				SceneObjectUtility.GetReferenceObject (selection, referenceObjectList);

				if( findWillAccessComponent )
				{
					foreach( var component in selection.GetComponents<MonoBehaviour>() )
						UpdatePerahpsReferenceObjectList (component, perhapsReferenceObjectList);
				}
			}


			if( ignoreSelfReference )
			{
				foreach( var selection in Selection.gameObjects )
				{
					referenceObjectList.RemoveAll( item => SceneObjectUtility.GetGameObject (item.value) == selection );
				}
			}

			refCompItems.Clear();

			if( findWillAccessComponent )
			{
				foreach( var selection in Selection.gameObjects )
				{
					foreach( var component in selection.GetComponents<MonoBehaviour>() ){

						if( !refCompItems.Exists(item => item.componentType == component.GetType() ))
						{
							refCompItems.Add(new ReferenceObjectItem() 
							{
								componentType = component.GetType(),
								isDisplay = true,
							});
						}
					}
				}

				foreach( var obj in referenceObjectList )
				{
					perhapsReferenceObjectList.RemoveAll( item => item.referenceMonobehaviourName == obj.value.GetType().Name );
				}


			}else{
				foreach (var referenceObject in referenceObjectList) {
					if (! refCompItems.Exists( item => item.componentType == referenceObject.referenceComponent.GetType())) {
						refCompItems.Add (new ReferenceObjectItem(){ 
							componentType = referenceObject.referenceComponent.GetType(),
							isDisplay = true,
						});
					}
				}
			}

		}

		public static void UpdatePerahpsReferenceObjectList (MonoBehaviour component, List<PerhapsReferenceObject> list)
		{
			if( component == null )
				return;
			// analytics  source code.
			var monoScript = MonoScript.FromMonoBehaviour(component);
			var uniqueClassList = SceneObjectUtility.SceneUniqueComponentName();

			foreach (var text in monoScript.text.Split(';')) {

				foreach( var methodPattern in getComponentFunctionPattern)
				{
					
					Match m = Regex.Match (text, methodPattern);

					if (m.Success) {
						var className = m.Groups ["call"].ToString ();

						if(! list.Exists (item =>  item.compType == component.GetType() && item.referenceMonobehaviourName == className) )
						{
							var method = new PerhapsReferenceObject ()
							{
								compType = component.GetType(),
								referenceMonobehaviourName = className,
								monoscript = monoScript,
							};
							list.Add (method);

							uniqueClassList.RemoveAll( item => item.Name == className );
						}
					}
				}

				foreach( var className in uniqueClassList)
				{
					if( component.GetType() == className )
						continue;
					var result = text.IndexOf(className.Name ) ;
					if(result != -1 && result != 0 )
					{
						if(! list.Exists (item =>  item.compType == component.GetType() && item.referenceMonobehaviourName == className.Name) )
						{
							var method = new PerhapsReferenceObject ()
							{
								compType = component.GetType(),
								referenceMonobehaviourName = className.Name,
								monoscript = monoScript,
							};
							list.Add (method);
							continue;
						}
					}
				}
			}
		}



		public void OnSceneGUI (SceneView sceneView)
		{
			if (Selection.activeGameObject == null)
				return;

			foreach( var selection in Selection.gameObjects )
			{
				SceneGuiLineWriter(selection);
			}
		}
	
		void SceneGuiLineWriter(GameObject selection )
		{
			var cameraTransform = SceneView.currentDrawingSceneView.camera.transform;
			var rotate = cameraTransform.rotation;
			var cameraPos = cameraTransform.position;
			
			Color shadowCol = new Color (0.5f, 0, 0, 0.06f);
			var enableTypeList = refCompItems.FindAll( item => item.isDisplay );
			
			foreach (var target in referenceObjectList.FindAll(item=> item.referenceComponent.gameObject == selection)) {
				var obj = SceneObjectUtility.GetGameObject (target.value);
				if (obj == null) {
					continue;
				}
				if (obj == Selection.activeGameObject) {
					continue;
				}

				
				if( PrefabUtility.GetPrefabType( obj ) == PrefabType.Prefab )
					continue;
				
				if( !enableTypeList.Exists( item => item.componentType == target.referenceComponent.GetType () ) )
					continue;
				
				var startPosition = selection.transform.position;
				var endPosition = obj.transform.position;
				
				var size = Vector3.Distance (endPosition, cameraPos) * 0.02f;
				
				if (startPosition == endPosition)
					continue;
				
				Handles.color = Color.red;
				
				var diffPos = startPosition - endPosition;
				var tan = new Vector3 (diffPos.y, diffPos.x, diffPos.z);
				
				
				var startTan = startPosition;
				var endTan = endPosition + tan * 0.4f;
				
				Handles.CircleCap (1, endPosition, rotate, size);
				
				for (int i=0; i<3; i++)
					Handles.DrawBezier (startPosition, endPosition, startTan, endTan, shadowCol, null, (i + 1) * 5);
				Handles.DrawBezier (startPosition, endPosition, startTan, endTan, Color.red, null, 1);
				
				Handles.Label (endPosition, obj.name);
			}		}

		public void OnGUI ()
		{
			GUIStyle styles = new GUIStyle ();
			styles.margin.left = 10;
			styles.margin.top = 5;

			EditorGUILayout.BeginHorizontal("box");

			var iconSize = EditorGUIUtility.GetIconSize();
			EditorGUIUtility.SetIconSize(new Vector2(20, 16));
			GUILayout.Label(toRefImage);
			EditorGUIUtility.SetIconSize(iconSize);

			EditorGUILayout.LabelField("Referencing");

			EditorGUI.BeginChangeCheck();
			findWillAccessComponent = EditorGUILayout.Toggle( findWillAccessComponent);
			if( EditorGUI.EndChangeCheck())
				OnSelectionChange();

			EditorGUILayout.EndHorizontal();

			current = EditorGUILayout.BeginScrollView (current);


			try {
				foreach (var type in refCompItems) {

					var components = referenceObjectList.FindAll (item => item.referenceComponent.GetType() == type.componentType);
					var willRefMonobehaviourList = perhapsReferenceObjectList.FindAll( item => item.compType == type.componentType);

					if( components.Count == 0 && willRefMonobehaviourList.Count == 0)
						continue;

					EditorGUILayout.BeginVertical ("box");

					EditorGUILayout.BeginHorizontal();
					EditorGUI.BeginChangeCheck();
					type.isDisplay = EditorGUILayout.Foldout (type.isDisplay, type.componentType.Name);
					if( EditorGUI.EndChangeCheck() )
						SceneView.RepaintAll();

					EditorGUILayout.EndHorizontal();

					if( type.isDisplay == false ){
						EditorGUILayout.EndVertical();
						continue;
					}

					EditorGUI.indentLevel = 1;

					if( components.Count != 0)
					{
						if( components[0].referenceComponent is MonoBehaviour )
						{
							var monoscript = MonoScript.FromMonoBehaviour((MonoBehaviour)components[0].referenceComponent);
							EditorGUILayout.ObjectField("script", monoscript, typeof(MonoScript), true);
						}
						foreach (var toComp in components) {
							EditorGUILayout.ObjectField (toComp.referenceMemberName, (Object)toComp.value, toComp.value.GetType (), true);
						}
					}else{
						if( willRefMonobehaviourList.Count != 0 )
						{
							var monoscript = willRefMonobehaviourList[0].monoscript;
							EditorGUILayout.ObjectField("script", monoscript, typeof(MonoScript), true);
						}
					}

					foreach (var compName in willRefMonobehaviourList) {
						EditorGUILayout.SelectableLabel (compName.referenceMonobehaviourName, GUILayout.Height(16));
					}

					EditorGUILayout.EndVertical ();

					EditorGUI.indentLevel = 0;
				}



			} catch {
				referenceObjectList.Clear ();
			}

		
			EditorGUILayout.EndScrollView ();
		}
		public class PerhapsReferenceObject
		{
			public System.Type compType;
			public string referenceMonobehaviourName;
			public MonoScript monoscript;
		}
	}
}
