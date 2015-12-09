using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System;

/// <summary>
/// Object collector.
/// </summary>
public class ObjectCollector
{
	/// <summary>
	/// Finds the reference info.
	/// </summary>
	/// <returns>The reference info.</returns>
	/// <param name="component">Component.</param>
	public static ReferenceInfo[] FindReferenceInfo (System.Object component, System.Object baseObject = null)
	{
		if (baseObject == null)
			baseObject = component;

		List<ReferenceInfo> referenceInfoList = new List<ReferenceInfo> ();
		
		CollectFieldReference (component, ref referenceInfoList, baseObject);
		CollectEventReference (component, ref referenceInfoList);
		CollectUnityEventReference (component, ref referenceInfoList, baseObject);

		return referenceInfoList.ToArray ();
	}
	
	/// <summary>
	/// Collects the field reference.
	/// </summary>
	/// <param name="component">Component.</param>
	/// <param name="referenceInfoList">Reference info list.</param>
	static void CollectFieldReference (System.Object component, ref List<ReferenceInfo> referenceInfoList, System.Object baseObject)
	{
		Type type = component.GetType ();
		
		foreach (var field in type.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance |
		                                     BindingFlags.Static | BindingFlags.DeclaredOnly)) {
			var value = ReferenceExplorerUtility.GetObject (field.GetValue (component));
			if (value == null) {
				continue;
			}
			
			if (value is Component || value is GameObject) {
				
				referenceInfoList.Add (new ReferenceInfo (){
					fromObject = (System.Object)baseObject,
					referenceName = field.Name,
					referenceTarget = (System.Object)value
				});
				continue;
			}
			
			if (value is IEnumerable) {
				foreach (var element in value as IEnumerable) {
					
					if (element is GameObject || element is Component) {
						referenceInfoList.Add (new ReferenceInfo (){
							fromObject = (System.Object)baseObject,
							referenceName = field.Name,
							referenceTarget = (System.Object)element
						});
					} else if (IgnoreComponents.IsNotAnalyticsTypes (element)) {
						referenceInfoList.AddRange (FindReferenceInfo (element, baseObject));
					}
				}
				continue;
			} else if (value is System.Object) {
				referenceInfoList.AddRange (FindReferenceInfo (value, baseObject));
				continue;
			}
		}
	}

	/// <summary>
	/// Collects the event reference.
	/// </summary>
	/// <param name="component">Component.</param>
	/// <param name="referenceInfoList">Reference info list.</param>
	static void CollectEventReference (System.Object component, ref List<ReferenceInfo> referenceInfoList)
	{
		Type type = component.GetType ();

		foreach (var eventType in type.GetEvents()) {
			var eventField = type.GetField (eventType.Name, 
			                        BindingFlags.Static | 
				BindingFlags.NonPublic | 
				BindingFlags.Instance | 
				BindingFlags.Public | 
				BindingFlags.FlattenHierarchy);

			var eventValue = (System.Delegate)eventField.GetValue (component);

			if (eventValue == null)
				continue;
			var invocationList = eventValue.GetInvocationList ();
			
			foreach (var ev in invocationList) {

				var connectedMethod = ReferenceExplorerUtility.GetObject (ev.Target);
				if (connectedMethod == null || connectedMethod is Component == false || connectedMethod is GameObject == false) {
					continue;
				}

				referenceInfoList.Add (new ReferenceInfo (){
					fromObject = (System.Object)component,
					referenceName = eventType.Name,
					referenceTarget = (System.Object)connectedMethod
				});
			}
		}
	}

	/// <summary>
	/// Collects the unity event reference.
	/// </summary>
	/// <param name="component">Component.</param>
	/// <param name="referenceInfoList">Reference info list.</param>
	/// <param name="baseObject">Base object.</param>
	static void CollectUnityEventReference (System.Object component, ref List<ReferenceInfo> referenceInfoList, System.Object baseObject)
	{
		Type type = component.GetType ();
		
		foreach (var field in type.GetFields(
								BindingFlags.NonPublic | 
								BindingFlags.Public | 
								BindingFlags.Instance |
		                        BindingFlags.Static | 
								BindingFlags.DeclaredOnly)) {
			var value = field.GetValue (component);
			if (value is UnityEngine.Events.UnityEventBase) {

				var unityEventListener = (UnityEngine.Events.UnityEventBase)value;
				
				for (int i=0; i<unityEventListener.GetPersistentEventCount(); i++) {
					var target = unityEventListener.GetPersistentTarget (i);
					
					referenceInfoList.Add (new ReferenceInfo (){
						fromObject = (System.Object)baseObject,
						referenceName = field.Name,
						referenceTarget = (System.Object)target
					});
				}
			}
		}
	}
}
