using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;

namespace ReferenceExplorer
{
	public class SceneReferenceViewer : EditorWindow
	{
		// cache
		
		private bool isOpenReferenceList, isOpenReferencedByList;
		private ReferenceIgnoreType ignoreReferenceType = ReferenceIgnoreType.IgnoreSelf;
		private IEnumerable<ReferenceViewerClassbase> referenceList;
		private IEnumerable<ReferenceViewerClassbase> referencedByList;
		
		// viewer
		
		private Vector2 scrollViewPosition;
		private bool isLocked = false;
		private bool isHideNoReferenceObjects = false;
		
		private GameObject[] selectedObjects = null;
		
		private string search = string.Empty;
		
		[MenuItem("Window/ReferenceExploer/references")]
		static void Initialize ()
		{
			var window = EditorWindow.GetWindow<SceneReferenceViewer> ();
			window.Show ();
		}
		
		void OnEnable ()
		{
			SceneView.onSceneGUIDelegate += OnSceneGUI;
			ReferenceExplorerData.RestoreAllData ();
			UpdateReferenceInfomation ();
		}
		
		void OnDisable ()
		{
			SceneView.onSceneGUIDelegate -= OnSceneGUI;
		}
		
		public void OnSceneGUI (SceneView sceneView)
		{
			if( selectedObjects == null )
				return;
			
			foreach (var selection in selectedObjects) {
				if (isOpenReferenceList) {
					ReferenceLineWrite (selection);
				}
				if (isOpenReferencedByList) {
					ReferencedLineWrite (selection);
				}
			}
		}
		
		void OnForcus ()
		{
			UpdateReferenceInfomation ();
		}
		
		void OnHierarchyChange ()
		{
			
			ReferenceExplorerData.RestoreAllData ();
			UpdateReferenceInfomation ();
			
			Repaint ();
		}
		
		void OnSelectionChange ()
		{
			ReferenceExplorerData.RestoreComponentReferenceData ();
			UpdateReferenceInfomation ();
			SceneView.RepaintAll();
			Repaint ();
		}
		
		void ReferenceLineWrite (GameObject selection)
		{
			List<ReferenceInfo> list = new List<ReferenceInfo>();
			foreach( var refs in referenceList ){
				if( ReferenceExplorerData.IsOpenComponentList.Contains(refs.type ) == false )
					list.AddRange( refs.referenceInfoList );
			}
			
			var collection = list
				.Where(item => ReferenceExplorerUtility.GetGameObject(item.fromObject) == selection)
					.Select( item => item.referenceTarget);
			
			ReferenceExplorerUtility.WriteLine (collection, selection, Color.red, new Color (0.5f, 0, 0, 0.06f), -0.04f);
		}
		
		void ReferencedLineWrite (GameObject selection)
		{
			List<ReferenceInfo> list = new List<ReferenceInfo>();
			foreach( var refs in referencedByList ){
				if( ReferenceExplorerData.IsOpenComponentList.Contains(refs.type )  == false )
					list.AddRange( refs.referenceInfoList );
			}
			
			var collection = list
				.Where(item => ReferenceExplorerUtility.GetGameObject(item.referenceTarget) == selection)
					.Select( item => item.fromObject);
			
			ReferenceExplorerUtility.WriteLine (collection, selection, Color.blue, new Color (0, 0, 0.5f, 0.06f), 0.04f);
		}
		
		void UpdateReferenceInfomation ()
		{
			if(! isLocked ){
				var currentObject = Selection.gameObjects;
				selectedObjects = currentObject;
			}
			
			if( selectedObjects == null )
				return;
			
			referenceList = ReferenceExplorerData.AllReferenceComponent (selectedObjects, ignoreReferenceType, search);
			referencedByList = ReferenceExplorerData.AllReferencedByComponent (selectedObjects, ignoreReferenceType, search);
		}
		
		void OnGUIReference (IEnumerable<ReferenceViewerClassbase> referenceList)
		{
			foreach (var type in referenceList.Select(item=>item.type).Distinct()) {
				
				var referenceItems = referenceList.Where(item=>item.type == type);
				
				EditorGUI.indentLevel = 1;
				using (var itemLayout = new GUILayout.VerticalScope("box")) {
					
					var isShow = !ReferenceExplorerData.IsOpenComponentList.Contains (type);
					if (EditorGUILayout.Foldout (isShow, type.FullName) != isShow) {
						if (isShow) {
							ReferenceExplorerData.IsOpenComponentList.Add (type);
						} else {
							ReferenceExplorerData.IsOpenComponentList.Remove (type);
						}
					}
					
					if (isShow) {
						EditorGUI.indentLevel = 2;
						var monoscript = ReferenceExplorerData.allMonoscript.Find (item => item.GetClass () == type);
						EditorGUILayout.ObjectField ("script", monoscript, typeof(MonoScript), true);
						
						foreach( var referencedItem in referenceItems){
							foreach (var component in referencedItem.referenceInfoList) {
								
								if ((component.referenceTarget is GameObject) && (null != component.referenceTarget as GameObject)) {
									EditorGUILayout.ObjectField (component.referenceName, (GameObject)component.referenceTarget, typeof(GameObject), true);
								} else if ((component.referenceTarget is Component) && (null != component.referenceTarget as Component)) {
									EditorGUILayout.ObjectField (component.referenceName, (Component)component.referenceTarget, typeof(Component), true);
								}
							}
						}
					}
				}
			}
		}
		
		void OnGUIReferencedBy (IEnumerable<ReferenceViewerClassbase> referencedByList)
		{
			
			foreach (var type in referencedByList.Select(item=>item.type).Distinct()) {
				
				var referendedByItems = referencedByList.Where(item=>item.type == type);
				
				EditorGUI.indentLevel = 1;
				
				using (var itemLayout = new GUILayout.VerticalScope("box")) {
					
					var isShow = !ReferenceExplorerData.IsOpenComponentList.Contains (type);
					if (EditorGUILayout.Foldout (isShow, type.FullName) != isShow) {
						if (isShow) {
							ReferenceExplorerData.IsOpenComponentList.Add (type);
						} else {
							ReferenceExplorerData.IsOpenComponentList.Remove (type);
						}
					}
					
					if (isShow) {
						EditorGUI.indentLevel = 2;
						var monoscript = ReferenceExplorerData.allMonoscript.Find (item => item.GetClass () == type);
						EditorGUILayout.ObjectField ("script", monoscript, typeof(MonoScript), true);
						
						foreach( var referenceItem in referendedByItems ){
							foreach (var component in referenceItem.referenceInfoList) {
								var fromObject = ReferenceExplorerUtility.GetGameObject (component.fromObject);
								EditorGUILayout.ObjectField (component.referenceName, fromObject, typeof(Object), true);
							}
						}
					}
				}
			}
		}
		
		void OnGUI ()
		{
			EditorGUI.BeginChangeCheck();
			
			using (var hedder = new GUILayout.HorizontalScope()) {
				
				EditorGUI.BeginChangeCheck();
				var isHide = GUILayout.Toggle(isHideNoReferenceObjects, "hide no reference object",EditorStyles.toolbarButton, GUILayout.Width(125));
				var ignoreType = (ReferenceIgnoreType)EditorGUILayout.EnumPopup (ignoreReferenceType, EditorStyles.toolbarPopup, GUILayout.Width (85));
				
				
				
				
				search = EditorGUILayout.TextField( search );
				
				isLocked = GUILayout.Toggle(isLocked, "Locked",EditorStyles.toolbarButton, GUILayout.Width(40));
				
				if( isHide != isHideNoReferenceObjects  || ignoreType != ignoreReferenceType){
					
					isHideNoReferenceObjects = isHide;
					ignoreReferenceType = ignoreType;
					
					if( isHideNoReferenceObjects ){
						switch( ignoreReferenceType ){
						case ReferenceIgnoreType.None:
							ObjectOrganize.AppearObjects();
							break;
						case ReferenceIgnoreType.IgnoreSelf:
							ObjectOrganize.DisappearObjects();
							break;
						case ReferenceIgnoreType.IgnoreFamilly:
							ObjectOrganize.DisappearObjectsWithFamillyReference();
							break;
						}
					}else{
						ObjectOrganize.AppearObjects();
					}
				}
			}
			
			using (var title = new GUILayout.HorizontalScope("box")) {
				GUILayout.Label ("Reference scene objecsts");
			}
			
			using (var scrollViewLayout = new GUILayout.ScrollViewScope(scrollViewPosition)) {
				scrollViewPosition = scrollViewLayout.scrollPosition;
				
				if (referenceList.Count () > 0 && referenceList.Sum( item => item.referenceInfoList.Count) > 0) {
					
					var color = GUI.backgroundColor;
					GUI.backgroundColor = new Color (1f, 0.8f, 0.8f);
					
					using (var referencedByBlockLayout = new GUILayout.VerticalScope("box")) {
						EditorGUI.indentLevel = 0;
						isOpenReferenceList = EditorGUILayout.Foldout (isOpenReferenceList, "reference");
						
						if (isOpenReferenceList) {
							OnGUIReference (referenceList);
						}
					}
					GUI.backgroundColor = color;
				}
				
				if (referencedByList.Count () > 0 && referencedByList.Sum( item => item.referenceInfoList.Count) > 0) {
					
					var color = GUI.backgroundColor;
					GUI.backgroundColor = new Color (0.8f, 0.8f, 1f);
					using (var referencedByBlockLayout = new GUILayout.VerticalScope("box")) {
						EditorGUI.indentLevel = 0;
						isOpenReferencedByList = EditorGUILayout.Foldout (isOpenReferencedByList, "referenced by");
						
						if (isOpenReferencedByList) {
							OnGUIReferencedBy (referencedByList);
						}
					}
					GUI.backgroundColor = color;
				}
				
			}
			
			if( EditorGUI.EndChangeCheck() ){
				UpdateReferenceInfomation ();
				SceneView.RepaintAll();
				Repaint ();
			}
			
		}
	}
}


