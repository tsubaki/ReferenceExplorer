using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ReferenceViewer
{
	public class Generate
	{
		private static float progress;

		public static void Build (string[] assetPaths, Action<AssetData[]> callback = null)
		{
			try {
				var result = new AssetData[0];

				EditorApplication.LockReloadAssemblies ();
				assetPaths = assetPaths.OrderBy (path => Path.GetExtension (path)).ToArray ();
				for (int i = 0; i < assetPaths.Length; i++) {
					var assetPath = assetPaths [i];

					if (assetPath.StartsWith ("Assets/") == false)
						continue;

					var assetData = new AssetData
                {
                    path = assetPath,
                    guid = AssetDatabase.AssetPathToGUID(assetPath)
                };
					progress = (float)i / assetPaths.Length;

					var obj = AssetDatabase.LoadAssetAtPath (assetPath, typeof(Object));
					DisplayProgressBar (assetData.path, progress);
					switch (Path.GetExtension (assetPath)) {
					case ".prefab":
						AddReferenceForPrefab (assetData, obj);
						break;
					case ".unity":

						if (EditorApplication.OpenScene (assetPath)) {
							foreach (GameObject go in Object.FindObjectsOfType(typeof(GameObject))) {
								if (go.transform.parent != null)
									continue;

								DisplayProgressBar (assetData.path + " - " + go.name, progress);
								AddReferenceForObject (assetData, go);
								AddReferenceForObject (assetData, go, true);
							}
						}
						break;
					case ".controller":
						var animatorController = AssetDatabase.LoadAssetAtPath (assetPath, typeof(AnimatorController)) as AnimatorController;
						foreach (var animationClip in animatorController.animationClips) {
							AddReferenceForObject (assetData, animationClip);
						}

						foreach (var animatorControllerLayer in animatorController.layers) {
							AddReferenceForStateMachine (assetData, animatorControllerLayer.stateMachine);
						}
						break;
					default:

						AddReferenceForObject (assetData, obj);
						break;
					}
					ArrayUtility.Add (ref result, assetData);
				}
				callback (result);
				EditorApplication.UnlockReloadAssemblies ();
			} finally {
				EditorUtility.ClearProgressBar ();
			}
		}

		private static void AddReferenceForStateMachine (AssetData assetData, AnimatorStateMachine stateMachine)
		{
//            foreach (var stateMachineBehaviourInfo in stateMachine.states
//                .SelectMany(childAnimatorState => childAnimatorState.state.behaviours))
//            {
//                AddReferenceForObject(assetData, stateMachineBehaviourInfo.stateMachineBehaviour);
//            }
//
//            foreach (var stateMachineBehaviourInfo in stateMachine.behaviours)
//            {
//                AddReferenceForObject(assetData, stateMachineBehaviourInfo.stateMachineBehaviour);
//            }

			foreach (var childAnimatorStateMachine in stateMachine.stateMachines) {
				AddReferenceForStateMachine (assetData, childAnimatorStateMachine.stateMachine);
			}
		}

		private static void AddReferenceForPrefab (AssetData assetData, Object obj, int depth = 0)
		{
			foreach (var o in EditorUtility.CollectDependencies(new[] { obj })) {
				if (o == null)
					continue;
				DisplayProgressBar (assetData.path + " - " + o.name, progress);
				AddReference (assetData, o, false);
			}
		}

		private static void DisplayProgressBar (string path, float progress)
		{
			EditorUtility.DisplayProgressBar (Path.GetFileName (path), Mathf.FloorToInt (progress * 100) + "% - " + Path.GetFileName (path), progress);
		}

		private static void AddReferenceForObject (AssetData assetData, Object obj, bool isSceneObject = false)
		{
			AddReference (assetData, obj, isSceneObject);
			foreach (var o in EditorUtility.CollectDependencies(new[] { obj })) {
				AddReference (assetData, o, isSceneObject);
			}
		}

		private static void AddReference (AssetData assetData, Object obj, bool isSceneObject)
		{
			var guid = AssetDatabase.AssetPathToGUID (AssetDatabase.GetAssetPath (obj));
			if (string.IsNullOrEmpty (guid))
				return;

			if (isSceneObject) {
				Transform transform = null;

				var go = obj as GameObject;

				if (go)
					transform = go.transform;
				else {
					//                        var component = o as Component;
					//                        if (component)
					//                            transform = component.transform;
				}

				if (transform == null)
					return;

				assetData.sceneData.Add (new SceneData
                {
                    guid = guid,
                    name = GetPathName(transform),
                    typeName = obj.GetType().FullName
                });
			} else {
				if (string.IsNullOrEmpty (guid))
					return;


				if (AssetDatabase.IsSubAsset (obj)) {
					var sub = new SubAssetData
                    {
                        name = obj.name,
                        guid = guid,
                        typeName = obj.GetType().FullName
                    };
					if (assetData.subAssets.Count (s => s.guid == sub.guid && s.name == sub.name && s.typeName == sub.typeName) == 0) {
						assetData.subAssets.Add (sub);
					}
				} else {
					if (assetData.reference.Contains (guid) == false)
						assetData.reference.Add (guid);
				}
			}
		}

		private static string GetPathName (Transform transform, string name = "")
		{
			while (true) {
				name = transform.name + name;
				if (!transform.parent)
					return name;
				transform = transform.parent;
				name = "/" + name;
			}
		}
	}
}