using UnityEngine;
using System.Collections;
using System;
using System.Linq;
using System.Linq.Expressions;
using UnityEditor;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

/// <summary>
/// Reference explorer data.
/// </summary>
public class ReferenceExplorerData
{


	public static List<GameObject> allObjects = new List<GameObject> ();
	public static List<Component> allComponents = new List<Component> ();
	public static List<ReferenceInfo> allReferenceInfo = new List<ReferenceInfo> ();
	public static List<MonoScript> allMonoscript = new List<MonoScript> ();
	public static List<Type> allComponentTypes = new List<Type> ();
	public static List<Component> allSelectedComponent = new List<Component> ();
	public static List<AnimatorBehaviourInfo> animatorBehaviourList = new List<AnimatorBehaviourInfo> ();
	static double lastCollectionTime = 0;
	static double localSelectedCollectionTime = 0;
	public static List<Type> IsOpenComponentList = new List<Type> ();

	/// <summary>
	/// Restores all data.
	/// </summary>
	public static void RestoreAllData ()
	{
		if (lastCollectionTime > UnityEditor.EditorApplication.timeSinceStartup)
			return;

		CollectAllObject ();
		CollectAllComponent ();
		CollectAllReference ();
		CollectAllType ();
		UpdateSelectedComponentList ();
		AllMonobehaviour ();

		CollectAllAnimatorBehaviour ();

		lastCollectionTime = UnityEditor.EditorApplication.timeSinceStartup + 0.1f;
	}

	public static void UpdateSelectedComponentList ()
	{
		if (localSelectedCollectionTime == UnityEditor.EditorApplication.timeSinceStartup)
			return;

		GameObject[] objects = Selection.gameObjects;

		allSelectedComponent.Clear ();

		if (objects == null)
			allSelectedComponent.AddRange (allComponents);

		allSelectedComponent = allComponents
			.Where (item => objects.Contains (ReferenceExplorerUtility.GetGameObject (item)))
			.ToList ();


		localSelectedCollectionTime = UnityEditor.EditorApplication.timeSinceStartup;
	}

	/// <summary>
	/// Restores the component reference data.
	/// </summary>
	public static void RestoreComponentReferenceData ()
	{
		if (lastCollectionTime == UnityEditor.EditorApplication.timeSinceStartup)
			return;

		CollectAllComponent ();
		CollectAllReference ();
		CollectAllType ();
		AllMonobehaviour ();
		CollectAllAnimatorBehaviour ();

		lastCollectionTime = UnityEditor.EditorApplication.timeSinceStartup;
	}

	/// <summary>
	/// Collects all object.
	/// </summary>
	static void CollectAllObject ()
	{
		// collect all object.
		allObjects.Clear ();
		var allSceneObjectsWithEditor = (GameObject[])Resources.FindObjectsOfTypeAll (typeof(GameObject));

		foreach (GameObject pObject in allSceneObjectsWithEditor) {

			if (pObject.hideFlags == HideFlags.NotEditable || pObject.hideFlags == HideFlags.HideAndDontSave) {
				continue;
			}
			
			if (Application.isEditor) {
				string sAssetPath = AssetDatabase.GetAssetPath (pObject.transform.root.gameObject);
				if (!string.IsNullOrEmpty (sAssetPath)) {
					continue;
				}
			}
			
			allObjects.Add (pObject);
		}	
	}

	static void CollectAllAnimatorBehaviour ()
	{
		animatorBehaviourList.Clear ();
		var animators = allComponents.Where (item => item is Animator).Select (item => item as Animator);

		foreach (var animator in animators) {
			if (animator == null)
				continue;

			foreach (var behaviour in animator.GetBehaviours<StateMachineBehaviour>()) {

				animatorBehaviourList.Add (new AnimatorBehaviourInfo (){
					animator = animator,
					behaviour = behaviour,
				});
			}

		}

	}

	/// <summary>
	/// Collects all component.
	/// </summary>
	static void CollectAllComponent ()
	{
		// collect all component.
		List<Component> allComponentOverlap = new List<Component> ();
		foreach (var sceneObject in allObjects) {
			if (sceneObject == null)
				continue;
			allComponentOverlap.AddRange (sceneObject.GetComponents<Component> ());
		}
		allComponents = allComponentOverlap
				.Where( item => item != null)
				.Where (item => !IgnoreComponents.IsNotAnalyticsTypes (item))
				.Distinct ()
				.ToList ();
	}

	/// <summary>
	/// Collects all reference.
	/// </summary>
	static void CollectAllReference ()
	{
		allReferenceInfo.Clear ();
		foreach (var component in allComponents) {
			allReferenceInfo.AddRange (ObjectCollector.FindReferenceInfo (component));
		}
	}

	/// <summary>
	/// Collects the type of the all.
	/// </summary>
	static void CollectAllType ()
	{
		allComponentTypes = allComponents
				.Where ( item => item != null)
				.Select (item => item.GetType ())
				.Distinct ()
				.OrderBy (item => item.FullName)
         		.ToList ();
	}

	/// <summary>
	/// Alls the monobehaviour.
	/// </summary>
	static void AllMonobehaviour ()
	{
		allMonoscript = allComponents
			.Where (item => item is MonoBehaviour)
			.Select<Component, MonoScript> (item => MonoScript.FromMonoBehaviour ((MonoBehaviour)item))
			.Distinct ()
			.ToList ();
	}

	public static IEnumerable<ReferenceViewerClassbase> AllReferenceComponent (GameObject[] currentObjects, ReferenceIgnoreType ignoreType, string search = null)
	{
		var list = new List<ReferenceViewerClassbase> ();

		foreach (var currentObject in currentObjects) {
			var referenceList = allReferenceInfo
				.Where (item => currentObject == ReferenceExplorerUtility.GetGameObject (item.fromObject))
				.Where (item => item.referenceTarget != null && item.referenceName != null && ReferenceExplorerUtility.GetGameObject (item.referenceTarget) != null);

			if (string.IsNullOrEmpty (search) == false) {

				var dic = ReferenceExplorerUtility.GetTExtCommand (search);

				if (dic.ContainsKey ("type")) {
					var typeText = dic ["type"];
					referenceList = referenceList.Where (item => 
					                                     item.referenceName.ToLower ().IndexOf (typeText) != -1 ||
						item.referenceTarget.GetType ().FullName.ToLower ().IndexOf (typeText) != -1 ||
						item.fromObject.GetType ().FullName.ToLower ().IndexOf (typeText) != -1);
				}
				if (dic.ContainsKey ("obj")) {
					var objName = dic ["obj"];
					referenceList = referenceList.Where (item => ReferenceExplorerUtility.GetGameObject (item.referenceTarget).name.ToLower ().IndexOf (objName) != -1);
				}
				if (dic.ContainsKey ("param")) {
					var param = dic ["param"];
					referenceList = referenceList
						.Where (item => item.referenceName.IndexOf (param) != -1);
				}
			}

			if (ignoreType == ReferenceIgnoreType.IgnoreSelf) {
				referenceList = referenceList
					.Where (item => currentObject != ReferenceExplorerUtility.GetGameObject (item.referenceTarget))
					.Where (item => currentObjects.Contains (ReferenceExplorerUtility.GetGameObject (item.referenceTarget)) == false);
			} else if (ignoreType == ReferenceIgnoreType.IgnoreFamilly) {
				referenceList = referenceList
					.Where (item => ReferenceExplorerUtility.IsFamilly (item.referenceTarget, currentObject) == false)
					.Where (item => currentObjects.Contains (ReferenceExplorerUtility.GetGameObject (item.referenceTarget)) == false);
			}
			
			var allComponentType = referenceList
				.Select (item => item.fromObject.GetType ())
					.Distinct ().OrderBy (item => item.FullName);

			foreach (var uniqueComponentType in allComponentType) {

				var componentItme = new ReferenceViewerClassbase ();
				componentItme.type = uniqueComponentType;
				componentItme.referenceInfoList = referenceList
				         .Where (item => item.fromObject.GetType () == uniqueComponentType)
				         .OrderBy (item => ReferenceExplorerUtility.GetGameObject (item.fromObject).name)
				         .ToList ();
				list.Add (componentItme);
			}
		}

		return list.Where (item => item.referenceInfoList.Count > 0);
	}

	public static IEnumerable<ReferenceViewerClassbase> AllReferencedByComponent (GameObject[] currentObjects, ReferenceIgnoreType ignoreType, string search = null)
	{
		var list = new List<ReferenceViewerClassbase> ();
		
		foreach (var currentObject in currentObjects) {
			var referencedByList = allReferenceInfo
				.Where (item => currentObject == ReferenceExplorerUtility.GetGameObject (item.referenceTarget));

			if (string.IsNullOrEmpty (search) == false) {

				var dic = ReferenceExplorerUtility.GetTExtCommand (search);

				if (dic.ContainsKey ("type")) {
					var typeText = dic ["type"];
					referencedByList = referencedByList
						.Where (item => 
                               item.referenceName.ToLower ().IndexOf (typeText) != -1 ||
						item.referenceTarget.GetType ().FullName.ToLower ().IndexOf (typeText) != -1 ||
						item.fromObject.GetType ().FullName.ToLower ().IndexOf (typeText) != -1 ||
						ReferenceExplorerUtility.GetGameObject (item.fromObject).name.ToLower ().IndexOf (typeText) != -1);
				}
				if (dic.ContainsKey ("obj")) {
					var objName = dic ["obj"];
					referencedByList = referencedByList
						.Where (item => 
						       ReferenceExplorerUtility.GetGameObject (item.fromObject).name.ToLower ().IndexOf (objName) != -1);
				}

				if (dic.ContainsKey ("param")) {
					var param = dic ["param"];
					referencedByList = referencedByList
						.Where (item => item.referenceName.IndexOf (param) != -1);
				}
			}

			if (ignoreType == ReferenceIgnoreType.IgnoreSelf) {
				referencedByList = referencedByList.Where (item => currentObject != ReferenceExplorerUtility.GetGameObject (item.fromObject))
					.Where (item => currentObjects.Contains (ReferenceExplorerUtility.GetGameObject (item.fromObject)) == false);
			} else if (ignoreType == ReferenceIgnoreType.IgnoreFamilly) {
				referencedByList = referencedByList.Where (item => ReferenceExplorerUtility.IsFamilly (item.fromObject, currentObject) == false)
					.Where (item => currentObjects.Contains (ReferenceExplorerUtility.GetGameObject (item.fromObject)) == false);
			}
			
			var allComponentType = referencedByList
				.Select (item => item.fromObject.GetType ())
					.Distinct ().OrderBy (item => item.FullName);

			foreach (var uniqueComponentType in allComponentType) {
				
				var componentItme = new ReferenceViewerClassbase ();
				componentItme.type = uniqueComponentType;
				componentItme.referenceInfoList = referencedByList
					.Where (item => item.fromObject.GetType () == uniqueComponentType)
						.OrderBy (item => ReferenceExplorerUtility.GetGameObject (item.fromObject).name)
						.ToList ();
				list.Add (componentItme);
			}
		}

		return list.Where (item => item.referenceInfoList.Count > 0);
	}
}
