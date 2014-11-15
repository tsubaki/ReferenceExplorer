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
					UpdatePerahpsReferenceObjectList (selection);
			}


			if( ignoreSelfReference )
			{
				foreach( var selection in Selection.gameObjects )
				{
					referenceObjectList.RemoveAll( item => SceneObjectUtility.GetGameObject (item.value) == selection );
				}
			}

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

		void UpdatePerahpsReferenceObjectList (GameObject obj)
		{
			if (obj == null)
				return;

			// analytics  source code.

			foreach (var component in  obj.GetComponents<MonoBehaviour>()) {
				foreach (var text in MonoScript.FromMonoBehaviour(component).text.Split(';')) {

					foreach( var methodPattern in getComponentFunctionPattern)
					{
						
						Match m = Regex.Match (text, methodPattern);

						if (m.Success) {
							var methodName = m.Groups ["call"].ToString ();
							if(! perhapsReferenceObjectList.Exists (item =>  item.comp == component && item.typeName == methodName) )
							{
								var method = new PerhapsReferenceObject ()
								{
									comp = component,
									typeName = methodName
								};
								perhapsReferenceObjectList.Add (method);
							}
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

			EditorGUILayout.LabelField("reference to any objects");

			EditorGUI.BeginChangeCheck();
			findWillAccessComponent = EditorGUILayout.Toggle( findWillAccessComponent);
			if( EditorGUI.EndChangeCheck())
				OnSelectionChange();

			EditorGUILayout.EndHorizontal();

			current = EditorGUILayout.BeginScrollView (current);


			try {
				foreach (var type in refCompItems) {

					var components = referenceObjectList.FindAll (item => item.referenceComponent.GetType() == type.componentType);

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

					if( components[0].referenceComponent is MonoBehaviour )
					{
						var monoscript = MonoScript.FromMonoBehaviour((MonoBehaviour)components[0].referenceComponent);
						EditorGUILayout.ObjectField("script", monoscript, typeof(MonoScript), true);
					}

					foreach (var toComp in components) {
						EditorGUILayout.ObjectField (toComp.referenceMemberName, (Object)toComp.value, toComp.value.GetType (), true);
					}

					foreach (var compName in perhapsReferenceObjectList.FindAll( item => item.comp.GetType() == type.componentType)) {
						EditorGUILayout.LabelField (compName.typeName);
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
			public Component comp;
			public string typeName;
		}
	}
}
