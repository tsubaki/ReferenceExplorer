using UnityEngine;
using System.Collections;
using System.Text;
using System.Collections.Generic;
using System.IO;
using UnityEditor;


namespace ReferenceExplorer
{
	public class ExportReferenceText {

		public enum ExportType
		{
			ComponentBase,
			ObjectBase,
		}

		public static void ExportText(ExportType type)
		{
			string exportText = string.Empty;

			switch( type )
			{
			case ExportType.ComponentBase:
				exportText = ComponentBaseGraph();
				break;
			case ExportType.ObjectBase:
				exportText = GameObjectBaseGraph();
				break;
			}

			string exportFile = Path.GetFileNameWithoutExtension( UnityEditor.EditorApplication.currentScene ) + ".dot";
			File.WriteAllText( exportFile, exportText);
		}

		static string ComponentBaseGraph()
		{
			List<string> uniqueStrings = new List<string>();
			StringBuilder exportText = new StringBuilder();
			Dictionary<string, List<ReferenceObject> > itemDirectry = new Dictionary<string, List<ReferenceObject>>();
			List<string> ignoreType = new List<string>{"GameObject", "Transform"};

			List<MonoBehaviour> monobehaviourList = new List<MonoBehaviour>();

			SceneObjectUtility.UpdateGlovalReferenceList();
			
			exportText.AppendLine("digraph sample {");
			exportText.AppendLine("graph [rankdir=\"LR\"]");
			exportText.AppendLine("node [ shape = record , style=filled, fillcolor=\"#efefef\",fontname=Helvetica, fontsize=10.5, fontcolor=\"#2b2b2b\", height=0.25, width=1, penwidth=0.1 ];");
			exportText.AppendLine("edge [arrowhead=normal,arrowsize=0.5,len=0.5, color=\"#bfbfbf\"];");


			foreach( var obj in SceneObjectUtility.GetAllObjectsInScene(false))
			{
				foreach( var comp in obj.GetComponents<MonoBehaviour>())
				{
					monobehaviourList.Add(comp);
				}
			}

			///-------------------


			foreach( var obj in SceneObjectUtility.SceneReferenceObjects)
			{
				var componentType = obj.referenceComponent.GetType().Name;
				
				if( obj.referenceComponent.gameObject.CompareTag("EditorOnly") )
					continue;

				if(! itemDirectry.ContainsKey(componentType) )
				{
					itemDirectry.Add( componentType, new List<ReferenceObject>());
				}
				var list = itemDirectry[componentType];
				list.Add(obj);
			}

			foreach( var dic in itemDirectry.Keys )
			{
				if( dic == null )
					continue;
				
				var list = itemDirectry[dic];
				list.RemoveAll( item => SceneObjectUtility.GetGameObject( item.value ) == item.referenceComponent.gameObject );
				if( list.Count == 0 )
					continue;

				foreach( var obj in list)
				{
					var baseObject = obj.referenceComponent.GetType();
					var toObject = obj.value.GetType();
					
					if( toObject == null )
						continue;

					if(ignoreType.Contains( toObject.Name ) )
						continue;


					string text = string.Format("\"{0}\" -> \"{1}\";", baseObject.Name, toObject.Name);
					
					if( uniqueStrings.Contains(text) )
						continue;
					
					exportText.AppendLine(text);
					uniqueStrings.Add(text);
				}
			}

			///-------------------


			List<ToReferenceWindow.PerhapsReferenceObject> perhapsList = new List<ToReferenceWindow.PerhapsReferenceObject>();
			foreach( var monobehaviour in monobehaviourList )
			{
				ToReferenceWindow.UpdatePerahpsReferenceObjectList(monobehaviour, perhapsList );
			}


			foreach( var obj in perhapsList )
			{
				if( ignoreType.Contains( obj.referenceMonobehaviourName ) )
					continue;


				string text = string.Format("\"{0}\" -> \"{1}\";", obj.compType.Name, obj.referenceMonobehaviourName);

				if( uniqueStrings.Contains(text) )
					continue;

				exportText.AppendLine(text);
				uniqueStrings.Add(text);
			}


			///-------------------


			List<CallbackCallObject> callbackObjectList = new List<CallbackCallObject>();
			foreach( var monobehaviour in monobehaviourList )
			{
				if( monobehaviour == null )
					continue;

				foreach (var text in MonoScript.FromMonoBehaviour(monobehaviour).text.Split(';')) {
					if (SceneObjectUtility.AddMatchMethod (text, monobehaviour, "SendMessage\\((?<call>.*?),.*\\)", callbackObjectList))
						continue;
					if (SceneObjectUtility.AddMatchMethod (text, monobehaviour, "SendMessage\\((?<call>.*?)\\)", callbackObjectList))
						continue;
					if (SceneObjectUtility.AddMatchMethod (text, monobehaviour, "BroadcastMessage\\((?<call>.*?)\\)", callbackObjectList))
						continue;
					if (SceneObjectUtility.AddMatchMethod (text, monobehaviour, "BroadcastMessage\\((?<call>.*?)\\)", callbackObjectList))
						continue;
				}
			}

			foreach( var callback in callbackObjectList )
			{
				foreach (var item in monobehaviourList) {
					if( item == null )
						continue;
					var method = item.GetType ().GetMethod (callback.method, 
					                                        System.Reflection.BindingFlags.NonPublic | 
					                                        System.Reflection.BindingFlags.Public |
					                                        System.Reflection.BindingFlags.Instance);
					if (method != null) {
						foreach( var comp in callback.callComponent )
						{
							if(ignoreType.Contains( item.GetType().Name ) )
								continue;

							string text = string.Format("\"{0}\" -> \"{1}\" [style = dotted];", comp.GetType().Name, item.GetType().Name);
							if( uniqueStrings.Contains(text) )
								continue;
							exportText.AppendLine(text);
							uniqueStrings.Add(text);
						}

					}
				}
			}

			List<ANimationCallbackObject> animCallbackObjectList = new List<ANimationCallbackObject>();

			foreach( var obj in SceneObjectUtility.GetAllObjectsInScene(false) )
			{
				if( obj.GetComponent<Animator>() != null )
					SceneObjectUtility.GetAnimationEvents( obj.GetComponent<Animator>(), animCallbackObjectList );
			}
			
			foreach( var callback in animCallbackObjectList )
			{
				foreach (var item in monobehaviourList) {
					var method = item.GetType ().GetMethod (callback.method, 
					                                        System.Reflection.BindingFlags.NonPublic | 
					                                        System.Reflection.BindingFlags.Public |
					                                        System.Reflection.BindingFlags.Instance);
					if (method != null) {
						if(ignoreType.Contains( item.GetType().Name ) )
							continue;
						
						string text = string.Format("\"AnimClip({0})\" -> \"{1}\" [style = dotted];", callback.clip.name, item.GetType().Name);
						if( uniqueStrings.Contains(text) )
							continue;
						exportText.AppendLine(text);
						uniqueStrings.Add(text);
					}
				}
			}



			//---------------


			List<CallbackObject> co = new List<CallbackObject>();

			if( GameObject.FindObjectOfType<Collider2D>() != null)
			{
				co.Add(new CallbackObject(){ method = "OnCollisionEnter2D", callComponenttype = typeof(Collider2D) });
				co.Add(new CallbackObject(){ method = "OnCollisionExit2D", callComponenttype = typeof(Collider2D) });
				co.Add(new CallbackObject(){ method = "OnCollisionStay2D", callComponenttype = typeof(Collider2D) });
				co.Add(new CallbackObject(){ method = "OnTriggerEnter2D", callComponenttype = typeof(Collider2D) });
				co.Add(new CallbackObject(){ method = "OnTriggerExit2D", callComponenttype = typeof(Collider2D) });
				co.Add(new CallbackObject(){ method = "OnTriggerStay2D", callComponenttype = typeof(Collider2D) });
			}

			if( GameObject.FindObjectOfType<Collider>() != null )
			{
				co.Add(new CallbackObject(){ method = "OnCollisionEnter", callComponenttype = typeof(Collider) });
				co.Add(new CallbackObject(){ method = "OnCollisionExit", callComponenttype = typeof(Collider) });
				co.Add(new CallbackObject(){ method = "OnCollisionStay", callComponenttype = typeof(Collider) });
				co.Add(new CallbackObject(){ method = "OnTriggerEnter", callComponenttype = typeof(Collider) });
				co.Add(new CallbackObject(){ method = "OnTriggerExit", callComponenttype = typeof(Collider) });
				co.Add(new CallbackObject(){ method = "OnTriggerStay", callComponenttype = typeof(Collider) });
			}

			if( GameObject.FindObjectOfType<Animator>() != null )
			{
				co.Add(new CallbackObject(){ method = "OnAnimatorMove", callComponenttype = typeof(Animator) });
			}

			if( GameObject.FindObjectOfType<Camera>() != null )
			{
				var type = typeof(Camera);
				co.Add(new CallbackObject(){ method = "OnPostRender", callComponenttype = type });
				co.Add(new CallbackObject(){ method = "OnPreCull", callComponenttype = type });
				co.Add(new CallbackObject(){ method = "OnPreRender", callComponenttype = type });
				co.Add(new CallbackObject(){ method = "OnRenderImage", callComponenttype = type });
				co.Add(new CallbackObject(){ method = "OnRenderObject", callComponenttype = type });
				co.Add(new CallbackObject(){ method = "OnWillRenderObject ", callComponenttype = type });
			}

			List<System.Type> uniqueMonobehaviourType = new List<System.Type>();
			foreach( var monobehaviour in monobehaviourList )
			{
				if( monobehaviour == null )
					continue;
				var type =  monobehaviour.GetType();
				if(! uniqueMonobehaviourType.Contains( type ))
					uniqueMonobehaviourType.Add(type);
			}

			foreach( var callback in co ){
				foreach( var monobehaviourType in uniqueMonobehaviourType )
				{
					var method = monobehaviourType.GetMethod (callback.method, 
					                                                          System.Reflection.BindingFlags.NonPublic | 
					                                                          System.Reflection.BindingFlags.Public |
					                                                          System.Reflection.BindingFlags.Instance);

					if(method  != null )
					{
						string text = string.Format("\"{0}\" -> \"{1}\" [style = dotted];", callback.callComponenttype.Name, monobehaviourType.Name);
						if( uniqueStrings.Contains(text) )
							continue;
						exportText.AppendLine(text);
						uniqueStrings.Add(text);
					}
				}
			}


			exportText.AppendLine("}");

			return exportText.ToString();


		}
		
		static string GameObjectBaseGraph()
		{
			List<string> uniqueStrings = new List<string>();
			StringBuilder exportText = new StringBuilder();
			Dictionary<string, List<ReferenceObject> > itemDirectry = new Dictionary<string, List<ReferenceObject>>();
			
			SceneObjectUtility.UpdateGlovalReferenceList();
			
			exportText.AppendLine("digraph sample {");
			exportText.AppendLine("graph [rankdir=\"LR\"]");
			exportText.AppendLine("node [ shape = record , style=filled, fillcolor=\"#efefef\",fontname=Helvetica, fontsize=10.5, fontcolor=\"#2b2b2b\", height=0.25, width=1, penwidth=0 ];");
			exportText.AppendLine("edge [arrowhead=normal,arrowsize=0.5,len=0.5, color=\"#bfbfbf\"];");
			
			foreach( var obj in SceneObjectUtility.SceneReferenceObjects)
			{
				var root = obj.referenceComponent.gameObject.transform.root;
				
				if( root.CompareTag("EditorOnly") )
					continue;
				
				if(! itemDirectry.ContainsKey(root.name) )
				{
					itemDirectry.Add( root.name, new List<ReferenceObject>());
				}
				var list = itemDirectry[root.name];
				list.Add(obj);
			}
			
			foreach( var dic in itemDirectry.Keys )
			{
				if( dic == null )
					continue;
				
				var list = itemDirectry[dic];
				list.RemoveAll( item => SceneObjectUtility.GetGameObject( item.value ) == item.referenceComponent.gameObject );
				if( list.Count == 0 )
					continue;

				foreach( var obj in list)
				{
					var baseObject = obj.referenceComponent.gameObject;
					var toObject = SceneObjectUtility.GetGameObject(obj.value);
					
					if( toObject == null )
						continue;
					
					if( toObject.name.Equals( baseObject.name ) &&  toObject.GetInstanceID() == baseObject.GetInstanceID())
						continue;
					
					string text = string.Format("\"{0}\" -> \"{2}\";", baseObject.name, baseObject.GetInstanceID(), toObject.name, toObject.GetInstanceID());
					
					if( uniqueStrings.Contains(text) )
						continue;
					
					exportText.AppendLine(text);
					uniqueStrings.Add(text);
				}
			}






			exportText.AppendLine("}");
			
			return exportText.ToString();
		}
	}

}

