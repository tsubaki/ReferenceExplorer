using UnityEngine;
using System.Collections;
using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor;
using System.Text.RegularExpressions;
using System.Linq;

public class ReferenceExplorerUtility
{
	/// <summary>
	///  Get Class Instance.
	/// </summary>
	/// <returns>The object.</returns>
	/// <param name="target">Target.</param>
	public static System.Object GetObject (object target)
	{
		if (target == null)
			return null;

		try {

			if (target is GameObject) {
				return (GameObject)target;
			} else  if (target is Component) {
				return (Component)target;
			} else if (target.GetType ().IsClass) {
				return (System.Object)target;
			}
		} catch (UnassignedReferenceException) {
		}
		return null;
	}

	/// <summary>
	/// Gets the game object.
	/// </summary>
	/// <returns>The game object.</returns>
	/// <param name="target">Target.</param>
	public static GameObject GetGameObject (System.Object target)
	{
		if (target == null)
			return null;

		try {
			if (target is GameObject) {
				return (GameObject)target;
			} else  if (target is Component) {
				if ((Component)target == null)
					return null;

				return ((Component)target).gameObject;
			}
		} catch (UnassignedReferenceException) {
		}
		return null;
	}

	public static bool IsFamilly (object obj, GameObject target)
	{
		return IsFamilly (obj, target, true) || IsFamilly (obj, target, false);
	}

	public static void WriteLine (IEnumerable<object> referenceObjectList, GameObject selection, Color mainColor, Color shaodowColor, float curve)
	{
		var cameraTransform = SceneView.currentDrawingSceneView.camera.transform;
		var rotate = cameraTransform.rotation;
		var cameraPos = cameraTransform.position;

		var refObjects = referenceObjectList.Select (item => ReferenceExplorerUtility.GetGameObject (item));

		foreach (var targetObject in refObjects) {

			try {
				if (targetObject == null || PrefabUtility.GetPrefabType (targetObject) == PrefabType.Prefab)
					continue;
				
				var startPosition = selection.transform.position;
				var endPosition = targetObject.transform.position;
				
				var size = Vector3.Distance (endPosition, cameraPos) * 1f;
				
				if (startPosition == endPosition)
					continue;
				
				Handles.color = shaodowColor;
				var diffPos = startPosition - endPosition;
				var tan = new Vector3 (diffPos.y, diffPos.x, diffPos.z);
				
				var startTan = startPosition;
				var endTan = endPosition + tan * curve;
				
				Handles.CircleCap (0, endPosition, rotate, size);
				
				for (int i=0; i<3; i++)
					Handles.DrawBezier (startPosition, endPosition, startTan, endTan, shaodowColor, null, (i + 1) * 5);
				Handles.DrawBezier (startPosition, endPosition, startTan, endTan, mainColor, null, 1);

				Handles.Label (endPosition, targetObject.name);
			} catch (MissingReferenceException) {
				Debug.LogWarningFormat (selection, "{0} is missing! check it!", targetObject.name);
			}
		}	
	}
	
	public static bool IsFamilly (object obj, GameObject target, bool isParent)
	{
		
		var gameObj = ReferenceExplorerUtility.GetGameObject (obj);
		
		if (gameObj == null)
			return false;
		
		if (isParent) {
			if (gameObj == target)
				return true;
			
			return IsFamilly (gameObj.transform.parent, target, true);
		} else {
			if (gameObj == target)
				return true;
			
			var childCount = gameObj.transform.childCount;
			for (int i=0; i<childCount; i++) {
				bool result = IsFamilly (gameObj.transform.GetChild (i), target, false);
				if (result == true) {
					return true;
				}
			}
		}
		return false;
	}

	public static Dictionary<string, string> GetTExtCommand (string text)
	{

		var dic = new Dictionary<string, string> ();

		var searchText = text.ToLower ();
		
		var typeMatch = Regex.Match (searchText, "t:(?<typeName>\\w*)");
		if (typeMatch.Success) {
			string typeText = typeMatch.Groups ["typeName"].ToString ();

			dic.Add ("type", typeText);

		}
		
		var objMatch = Regex.Match (searchText, "o:(?<objName>\\w*)");
		if (objMatch.Success) {
			string objname = objMatch.Groups ["objName"].ToString ();
		
			dic.Add ("obj", objname);
		}

		var paramMatch = Regex.Match (searchText, "p:(?<param>\\w*)");
		if (paramMatch.Success) {
			string paramName = paramMatch.Groups ["param"].ToString ();
			dic.Add ("param", paramName);
		}
		return dic;
	}

}
