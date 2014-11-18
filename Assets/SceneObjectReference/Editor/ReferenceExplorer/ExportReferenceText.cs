using UnityEngine;
using System.Collections;
using System.Text;
using UnityEngine;
using System.Collections;
using System.Text;
using System.Collections.Generic;
using System.IO;

namespace ReferenceExplorer
{
	public class ExportReferenceText {
		
		public static void ExportText()
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
				
				string subgroaph = ("subgraph " + dic.Replace("(","").Replace(")", "").Replace(" ","") + " {"  );
				
				exportText.AppendLine(subgroaph );
				exportText.AppendLine("label = \"" + dic + "\";" );
				
				
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
				
				exportText.AppendLine("}");
				
			}
			
			exportText.AppendLine("}");
			
			
			File.WriteAllText("export.txt", exportText.ToString());
		}
	}
}

