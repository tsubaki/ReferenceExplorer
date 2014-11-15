using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ReferenceExplorer
{
	public class SearchSceneComponentCode : EditorWindow
	{
		
		[MenuItem("Window/Referenced/Search Scene Code")]
		static void Init ()
		{
			var window = GetWindow<SearchSceneComponentCode> ("code ref");
			window.Show ();
		}
		
		List<CalledObject> calledObjectList = new List<CalledObject> ();
		bool isRegix = true;
		
		void UpdateCalledObjectList ()
		{
			calledObjectList.Clear ();
			
			foreach( var component in SceneObjectUtility.SceneComponents )
			{
				if( component is MonoBehaviour == false)
					continue;

				var monobehaviour = (MonoBehaviour) component;

				calledObjectList.Add( new CalledObject()
				{
					monoscript = MonoScript.FromMonoBehaviour(monobehaviour),
					component = monobehaviour,
				});
			}
		}

		void ComponentSearch( string text )
		{
			findedObjectList.Clear();

			foreach( var comp in calledObjectList )
			{
				var code = comp.monoscript.text;

				if( isRegix ){
					var match = Regex.Match(code, text );
					if( match.Success )
						findedObjectList.Add(comp);
				}else{
					int result = code.IndexOf( text );
					if( result != -1 && result != 0)
					{
						findedObjectList.Add(comp);
					}
				}
			}
		}

		string searchText;
		List<CalledObject> findedObjectList = new List<CalledObject>();
		List<MonoscriptType> findUniqueMonoscriptList = new List<MonoscriptType>();

		Vector2 currentScroll = Vector2.zero;

		void OnFocus()
		{
			SceneObjectUtility.UpdateReferenceList();
			UpdateCalledObjectList();
		}

		void OnGUI()
		{
			{
				EditorGUILayout.BeginHorizontal();



				EditorGUI.BeginChangeCheck();
				isRegix =  GUILayout.Toggle(isRegix, "Regix", EditorStyles.toolbarButton, GUILayout.Width(45));
				searchText = EditorGUILayout.TextField(searchText);

				if( EditorGUI.EndChangeCheck() )
				{
					if(! string.IsNullOrEmpty( searchText ) )
					{
						ComponentSearch(searchText);

						foreach( var obj in findedObjectList )
						{
							if( !findUniqueMonoscriptList.Exists( item => item.monoscript == obj.monoscript ) )
								findUniqueMonoscriptList.Add(new MonoscriptType(){ monoscript = obj.monoscript,});
						}

					}else{
						findUniqueMonoscriptList.Clear();
						findedObjectList.Clear();
					}
				}

				EditorGUILayout.EndHorizontal();
			}

			currentScroll = EditorGUILayout.BeginScrollView(currentScroll);
			EditorGUILayout.BeginVertical("box");

			if( findedObjectList.Count == 0 )
			{
				EditorGUILayout.LabelField("not found");
			}


			foreach( var monoscript in findUniqueMonoscriptList)
			{
				var list = findedObjectList.FindAll( item => item.monoscript == monoscript.monoscript );
				if( list.Count == 0 )
					continue;

				EditorGUI.indentLevel = 0;

				EditorGUILayout.BeginHorizontal();

				monoscript.isOpen = EditorGUILayout.Toggle(monoscript.isOpen, EditorStyles.foldout, GUILayout.Width(12));
				EditorGUILayout.ObjectField(list[0].monoscript, typeof(MonoScript));

				EditorGUILayout.EndHorizontal();

				if( monoscript.isOpen )
				{
					EditorGUI.indentLevel = 1;
					foreach( var obj in list)
					{
						EditorGUILayout.ObjectField(obj.component.gameObject, typeof( GameObject ));
					}
					EditorGUI.indentLevel = 0;
				}
			}

			EditorGUILayout.EndVertical();
			EditorGUILayout.EndScrollView();
		}
			
			
		class CalledObject
		{
			public MonoScript monoscript;
			public MonoBehaviour component;
			public string code;
		}

		class MonoscriptType
		{
			public MonoScript monoscript;
			public bool isOpen = false;
		}
	}

}
