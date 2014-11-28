using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ReferenceExplorer
{
	public class SearchSceneComponentCode : EditorWindow
	{
		
//		[MenuItem("Window/Referenced/Search Scene Code")]
//		static void Init ()
//		{
//			var window = GetWindow<SearchSceneComponentCode> ("code ref");
//			window.Show ();
//		}
		
		List<CalledObject> calledObjectList = new List<CalledObject> ();
		bool isRegex = true;
		public bool selected = false;
		
		public void UpdateCalledObjectList ()
		{
			calledObjectList.Clear ();


			Component[] components = null;
			if(selected)
			{
				List<Component> tempComponentList = new List<Component>();
				foreach( var obj in Selection.gameObjects )
				{
					tempComponentList.AddRange((Component[])obj.GetComponents<MonoBehaviour>());
				}
				components = tempComponentList.ToArray();
			}else{
				components = SceneObjectUtility.SceneComponents;
			}

			foreach( var component in components)
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

				if( isRegex ){
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

		public void OnFocus()
		{
			SceneObjectUtility.UpdateReferenceList();
			UpdateCalledObjectList();
		}

		public void UpdateSearchComponent()
		{
			ComponentSearch(searchText);
			
			foreach( var obj in findedObjectList )
			{
				if( !findUniqueMonoscriptList.Exists( item => item.monoscript == obj.monoscript ) )
					findUniqueMonoscriptList.Add(new MonoscriptType(){ monoscript = obj.monoscript,});
			}

		}

		public void OnGUI()
		{
			{
				EditorGUILayout.BeginHorizontal();



				EditorGUI.BeginChangeCheck();
				isRegex =  GUILayout.Toggle(isRegex, "Regex", EditorStyles.toolbarButton, GUILayout.Width(45));
				searchText = EditorGUILayout.TextField(searchText);

				if( EditorGUI.EndChangeCheck() )
				{
					if(! string.IsNullOrEmpty( searchText ) )
					{
						UpdateSearchComponent();
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
