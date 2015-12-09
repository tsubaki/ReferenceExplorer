using UnityEngine;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

public class ExplortReferenceMap 
{

	static readonly Type[] ignoreTypes = new Type[]{
		typeof( GameObject), typeof( System.Object), typeof(UnityEngine.Object), typeof(Behaviour), typeof( Debug),
		typeof( MonoBehaviour), typeof( Renderer ), typeof(GUI),
		typeof( SerializeField), typeof( LayerMask), typeof( GUILayout), typeof( Layout), typeof( RaycastHit ),
		typeof( Quaternion), typeof(Vector3), typeof(Mathf), typeof( Gizmos ), typeof( Color), typeof( Texture ),
		typeof( Time), typeof( Graphics ), typeof( Vector4 ), typeof( Vector2 ),  typeof( Matrix4x4),
		typeof( TextureFormat ), typeof( Component ), typeof( Shader ), typeof( RenderTextureFormat ),
		typeof(UnityEngine.EventSystems.IEventSystemHandler)
	};

	static readonly string[] ignoreAssemblys = new string[]{
		"System.", "Mono.", "UnityEditor.", "mscorlib.", "Boo.", "UnityScript.",
		"ICSharpCode.", "Unity.DataContract.", "Unity.PackageManager.", 
		"Assembly-CSharp-Editor.", "Unity.IvyParser.", "Unity.SerializationLogic."
	};

	public static void Export(bool isContainReferences, bool isContainCallbacks, bool isOnlyComponent)
	{
		try{
			EditorUtility.DisplayProgressBar ("Export Comonent Graph", "Collection All Types", 0);

			ReferenceExplorerData.RestoreAllData();

			List<ReferenceInfomation> refList = new List<ReferenceInfomation>();
			List<Type> refTypes = new List<Type>();

			if( isContainReferences )
			{
				Collection( refList, refTypes, isOnlyComponent);
			}

			if( isContainCallbacks ){
				foreach( var callback in CallbackData.callbackList ){
					var result = CallbackData.buidinCallback.Where(item => item.callbacks.Contains(callback.callback) );
					if( result.Count() == 0)
						continue;
					var buldinCallback =result.First();
					
					if( buldinCallback != null ){
						refTypes.Add( buldinCallback.senderComponent );
					}else{
						refTypes.AddRange( callback.senderTypeList );
						refTypes.AddRange( callback.recieverTypeList );
					}
				}
				
			}

			var uniqueTypes = refTypes.Where( item => item != null).Distinct();


			var uniqueRefList = refList.Where( item => item != null).Distinct();

			EditorUtility.DisplayProgressBar ("Export Comonent Graph", "writing", 1);

			StringBuilder exportBuilder = new StringBuilder();
			exportBuilder.AppendLine("graph");
			exportBuilder.AppendLine("[");


			foreach( var type in uniqueTypes ){

				exportBuilder.AppendLine("	node");
				exportBuilder.AppendLine("	[");
				exportBuilder.AppendLine("			id	" + type.GetHashCode());
				exportBuilder.AppendLine("			label	\"" + type.Name + "\"");
				exportBuilder.AppendLine("		graphics");
				exportBuilder.AppendLine("		[");
				exportBuilder.AppendLine("			w	" + type.Name.Length * 10);
				if( type.IsSubclassOf( typeof( MonoBehaviour ))){
					exportBuilder.AppendLine("			fill	\"#FFFF99\"");
				}else if( type.IsSubclassOf( typeof( Component ) )){
					exportBuilder.AppendLine("			fill	\"#ccccff\"");
				}else{
					exportBuilder.AppendLine("			fill	\"#ff99cc\"");
				}

				exportBuilder.AppendLine("		]");
				exportBuilder.AppendLine("	]");
			}

			foreach( var refType in uniqueRefList )
			{
				exportBuilder.AppendLine("	edge");
				exportBuilder.AppendLine("	[");
				exportBuilder.AppendLine("			source	" + refType.from.GetHashCode());
				exportBuilder.AppendLine("			target	" + refType.to.GetHashCode());
				exportBuilder.AppendLine("	]");
			}
			
			foreach( var callback in CallbackData.callbackList )
			{
				foreach( var sender in callback.senderTypeList ){
					foreach( var reciever in callback.recieverTypeList ){
						exportBuilder.AppendLine("	edge");
						exportBuilder.AppendLine("	[");
						exportBuilder.AppendLine("			source	" + reciever.GetHashCode());
						exportBuilder.AppendLine("			target	" + sender.GetHashCode());
						exportBuilder.AppendLine("			graphics");
						exportBuilder.AppendLine("			[");
						exportBuilder.AppendLine("					sourceArrow	\"white_delta\"");
						exportBuilder.AppendLine("					style	\"dashed\"");
						exportBuilder.AppendLine("			]");
						exportBuilder.AppendLine("	]");
					}
				}

				var buidinSender = System.Array.Find<UnityengineCallback>(CallbackData.buidinCallback, item => item.callbacks.Contains(callback.callback) );
				if( buidinSender != null && buidinSender.senderComponent != null){
					foreach( var reciever in callback.recieverTypeList ){
						exportBuilder.AppendLine("	edge");
						exportBuilder.AppendLine("	[");
						exportBuilder.AppendLine("			source	" + reciever.GetHashCode());
						exportBuilder.AppendLine("			target	" + buidinSender.senderComponent.GetHashCode());
						exportBuilder.AppendLine("			graphics");
						exportBuilder.AppendLine("			[");
						exportBuilder.AppendLine("					sourceArrow	\"white_delta\"");
						exportBuilder.AppendLine("					style	\"dashed\"");
						exportBuilder.AppendLine("			]");
						exportBuilder.AppendLine("	]");
					}
				}
			}
			
			exportBuilder.AppendLine("]");

			EditorUtility.DisplayProgressBar ("Export Comonent Graph", "exporting", 1);
			System.IO.File.WriteAllText( System.DateTime.Now.ToFileTime() + ".gml", exportBuilder.ToString());
		}finally{

			EditorUtility.ClearProgressBar();
		}
	}


	private static void Collection( List<ReferenceInfomation> refList,  List<Type> refTypes, bool isOnlyComponent)
	{
		
		List<Type> allTypes = new List<Type>();
		foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
		{
			if( ignoreAssemblys.Any( item => assembly.ManifestModule.ScopeName.IndexOf( item ) != -1 ) ){
				continue;
			}
			
			var types = Assembly.Load(assembly.GetName()).GetExportedTypes();
			allTypes.AddRange(types);
		}
		
		var allMonoScript = new List<MonoScript>();
		
		
		allMonoScript.AddRange( ReferenceExplorerData.allComponents
		                       .Where( item => null != item as MonoBehaviour)
		                       .Select( item => MonoScript.FromMonoBehaviour( (MonoBehaviour)item ) )
		                       .ToList());
		allMonoScript.AddRange( ReferenceExplorerData.animatorBehaviourList
		                       .Where( item => item != null )
		                       .Select( item => MonoScript.FromScriptableObject(item.behaviour )));
		var allUniqueMonoscript = allMonoScript.Distinct();
		
		
		EditorUtility.DisplayProgressBar ("Export Comonent Graph", "Collection All Callbacks", 0);
		
		CallbackData.UpdateSenderRecieverlist();
		CallbackData.UpdateCallbacklist(false, null);
		


		int currentTypeCount=0;
		int max = allTypes.Count;
		foreach( var type in allTypes ){
			
			if( ignoreTypes.Contains( type )){
				currentTypeCount ++;
				continue;
			}
			
			if( isOnlyComponent == true && type.IsSubclassOf(typeof( Component )) == false){
				continue;
			}
			
			EditorUtility.DisplayProgressBar ("Export Comonent Graph", "Lists reference class" + type.FullName, (float)(++currentTypeCount) / max);
			
			string pattern = string.Format("[\\.\\s\\<)]+{0}[\\.\\s\\)>]",type.Name);
			
			foreach( var monoscript in allUniqueMonoscript ){
				if( monoscript.GetClass() == type ||
				   ignoreTypes.Contains(monoscript.GetClass()))
					continue;
				
				var text= Regex.Replace(monoscript.text, "//.*\\n", "");
				text = text.Replace("\\n", " ");
				text = Regex.Replace(text, "/\\*.*\\*/", " ");
				
				var match = Regex.Match(text, pattern);
				
				if( match.Success == false )
					continue;
				
				refList.Add(new ReferenceInfomation(){
					from = monoscript.GetClass(),
					to = type
				});
				
				refTypes.Add(monoscript.GetClass());
				refTypes.Add(type);
			}
		}

	}

	public static void ExportObjectReference(bool isContainFamilly, bool isContainCallbacks)
	{
		try{


			ReferenceExplorerData.RestoreAllData();

			EditorUtility.DisplayProgressBar ("Export GameObject Graph", "get all objects", 0);



			var objectNames = ReferenceExplorerData.allReferenceInfo
				.Where( item => 
				   ReferenceExplorerUtility.GetGameObject( item.referenceTarget ) != null &&
			       ReferenceExplorerUtility.GetGameObject( item.referenceTarget ) != null)
				.SelectMany( item => new GameObject[]{ 
					ReferenceExplorerUtility.GetGameObject( item.referenceTarget ), 
					ReferenceExplorerUtility.GetGameObject(	item.fromObject) 
				}).Distinct();

			EditorUtility.DisplayProgressBar ("Export GameObject Graph", "check reference", 0.2f);


			List<ReferenceInfoObject> referenceInfoList = new List<ReferenceInfoObject>();
			foreach( var referenceInfo in ReferenceExplorerData.allReferenceInfo ){
				var fromObject = ReferenceExplorerUtility.GetGameObject( referenceInfo.fromObject );
				var targetObject = ReferenceExplorerUtility.GetGameObject( referenceInfo.referenceTarget );
				
				if( targetObject == null || fromObject == null )
					continue;

				if( isContainFamilly == false && ReferenceExplorerUtility.IsFamilly( fromObject, targetObject ) ){
					continue;
				}
				
				if( referenceInfoList.Any( item => 	ReferenceExplorerUtility.GetGameObject(item.fromObject) == fromObject ||
				                          			ReferenceExplorerUtility.GetGameObject(item.targetObject) == targetObject) == true ||
				   fromObject == targetObject)
				{
					continue;
				}
				referenceInfoList.Add( new ReferenceInfoObject(){
					targetObject = fromObject,
					fromObject = targetObject,
				});
			}

			EditorUtility.DisplayProgressBar ("Export GameObject Graph", "exporting", 0.7f);

			StringBuilder exportBuilder = new StringBuilder();
			exportBuilder.AppendLine("graph");
			exportBuilder.AppendLine("[");
			foreach( var objName in objectNames ){
				
				if( referenceInfoList.Any( item => item.fromObject == objName || item.targetObject == objName ) == false )
					continue;

				var isPrefab = PrefabUtility.GetPrefabObject( objName ) == null;

				exportBuilder.AppendLine("	node");
				exportBuilder.AppendLine("	[");
				exportBuilder.AppendLine("			id	" + objName.GetHashCode());
				exportBuilder.AppendLine("			label	\"" + objName.name + "\"");
				exportBuilder.AppendLine("		graphics");
				exportBuilder.AppendLine("		[");
				if( isPrefab ){
					exportBuilder.AppendLine("			fill	\"#FFFF99\"");
				}
				exportBuilder.AppendLine("			w	" + objName.name.Length * 10);
				exportBuilder.AppendLine("		]");
				exportBuilder.AppendLine("	]");
			}
			
			foreach( var referenceInfo in referenceInfoList ){
				
				exportBuilder.AppendLine("	edge");
				exportBuilder.AppendLine("	[");
				exportBuilder.AppendLine("			target	" + referenceInfo.fromObject.GetHashCode());
				exportBuilder.AppendLine("			source	" +  referenceInfo.targetObject.GetHashCode());
				exportBuilder.AppendLine("	]");
			}
			
			exportBuilder.AppendLine("]");


			EditorUtility.DisplayProgressBar ("Export GameObject Graph", "done", 0.7f);

			System.IO.File.WriteAllText( System.DateTime.Now.ToFileTime() + ".gml", exportBuilder.ToString());

		}finally{
			EditorUtility.ClearProgressBar();
		}

	}


	class ReferenceInfoObject
	{
		public GameObject fromObject;
		public GameObject targetObject;
	}

	class ReferenceInfomation{
		public Type from;
		public Type to;

		public override int GetHashCode ()
		{
			return from.GetHashCode() + to.GetHashCode();
		}
		public override bool Equals (object obj)
		{
			var item = (ReferenceInfomation) obj;
			return from == item.from && to == item.to;
		}
	}
}
