using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;
using System;
using System.Text.RegularExpressions;

public class CodeSearch
{
	string searchText = string.Empty;
	bool isSelectedObject = false;
	IEnumerable<MonoScript> monoscripts = null;

	public CodeSearch(){
		UpdateMonoscript();
	}

	void UpdateMonoscript ()
	{
		var components = (isSelectedObject ? ReferenceExplorerData.allSelectedComponent : ReferenceExplorerData.allComponents)
				.Where (item => item is MonoBehaviour) 
				.Select (item => (MonoBehaviour)item);
		var animatorBehaviourList = ReferenceExplorerData.animatorBehaviourList.AsEnumerable ();

		var allMonoscript = new List<MonoScript> ();
		allMonoscript.AddRange (components
		                       .Where (item => item != null && item is MonoBehaviour)
		                       .Select (item => MonoScript.FromMonoBehaviour ((MonoBehaviour)item)));
		allMonoscript.AddRange (animatorBehaviourList
		                       .Where (item => item != null && item.behaviour is ScriptableObject)
		                       .Select (item => MonoScript.FromScriptableObject ((ScriptableObject)item.behaviour)));

		monoscripts = allMonoscript
			.Distinct()
			.Where( item => item.text.IndexOf(searchText) != -1 );
	}

	public void OnGUI ()
	{
		using (var header = new EditorGUILayout.HorizontalScope()) {
			EditorGUI.BeginChangeCheck ();
			isSelectedObject = GUILayout.Toggle (isSelectedObject, "selected", EditorStyles.toolbarButton, GUILayout.Width (70));
			searchText = EditorGUILayout.TextField (searchText);

			if (EditorGUI.EndChangeCheck ()) {
				UpdateMonoscript();
			}
		}

		if(  monoscripts == null)
			return;

		foreach (var monoscript in monoscripts) {
			EditorGUILayout.ObjectField(monoscript, typeof(MonoScript), false);
		}
	}
}
