using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Linq;

public class ObjectOrganize
{
	public static void DisappearObjects ()
	{
		ReferenceExplorerData.RestoreAllData ();
		ReferenceExplorerData.RestoreComponentReferenceData ();

		var objects = ReferenceExplorerData.allObjects;
		foreach (var obj in objects) {
			obj.hideFlags = HideFlags.HideInHierarchy;
		}

		foreach (var component in ReferenceExplorerData.allReferenceInfo) {
			var fromObject = ReferenceExplorerUtility.GetGameObject (component.fromObject);
			var referencetarget = ReferenceExplorerUtility.GetGameObject (component.referenceTarget);

			if (fromObject != null)
				Appear (fromObject.transform);

			if (referencetarget != null)
				Appear (referencetarget.transform);
		}
	}

	public static void DisappearObjectsWithFamillyReference ()
	{
		ReferenceExplorerData.RestoreAllData ();
		ReferenceExplorerData.RestoreComponentReferenceData ();
		
		var objects = ReferenceExplorerData.allObjects;
		foreach (var obj in objects) {
			obj.hideFlags = HideFlags.HideInHierarchy;
		}
		
		foreach (var component in ReferenceExplorerData.allReferenceInfo) {

			var fromObject = ReferenceExplorerUtility.GetGameObject (component.fromObject);
			var referencetarget = ReferenceExplorerUtility.GetGameObject (component.referenceTarget);

			if (fromObject != null) {
				if (fromObject.transform.GetComponentsInChildren<Component> ().Any (item => item.gameObject == referencetarget) == false && 
					fromObject.transform.GetComponentsInParent<Component> ().Any (item => item.gameObject == referencetarget) == false) {
					Appear (fromObject.transform);
				}
			}

			if (referencetarget != null) {
				if (referencetarget.transform.GetComponentsInChildren<Component> ().Any (item => item.gameObject == fromObject) == false &&
					referencetarget.transform.GetComponentsInParent<Component> ().Any (item => item.gameObject == fromObject) == false) {
					Appear (referencetarget.transform);
				}
			}
		}
	}

	public static void AppearObjects ()
	{
		ReferenceExplorerData.RestoreAllData ();

		var objects = ReferenceExplorerData.allObjects;
		foreach (var obj in objects) {
			obj.hideFlags = HideFlags.None;
		}
	}

	public static void Appear (Transform transform)
	{
		if (transform == null)
			return;
		transform.gameObject.hideFlags = HideFlags.None;
		Appear (transform.transform.parent);
	}
}
