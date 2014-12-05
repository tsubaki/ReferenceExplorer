#if !UNITY_5
using System;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Reflection;
using System.Linq;
using Object = UnityEngine.Object;

namespace ReferenceViewer
{
	public class Generate
	{
		const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
		private static Dictionary<object, int> depths = new Dictionary<object, int>();
		
		private static readonly string[] ignoreTypes =
		{
			"Rigidbody",
			"Rigidbody2D",
			"Transform",
			"Object"
		};
		
		private static bool isUnity41
		{
			get
			{
				return Application.unityVersion.StartsWith("4.1");
			}
		}
		
		public static void Build(string[] assetPaths, Action<AssetData[]> callback = null)
		{
			var result = new AssetData[0];
			for (var i = 0; i < assetPaths.Length; i++)
			{
				var assetPath = assetPaths[i];
				var assetData = new AssetData
				{
					path = assetPath,
					guid = AssetDatabase.AssetPathToGUID(assetPath)
				};
				
				var progress = (float)i / assetPaths.Length;
				switch (Path.GetExtension(assetPath))
				{
				case ".prefab":
				{
					DisplayProgressBar(assetData.path, progress);
					var prefab = AssetDatabase.LoadAssetAtPath(assetPath, typeof(GameObject)) as GameObject;
					
					var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
					go.hideFlags = HideFlags.HideAndDontSave;
					
					SearchGameObject(go, assetData);
					Object.DestroyImmediate(go);
				}
					break;
				case ".unity":
					DisplayProgressBar(assetData.path, progress);
					if (EditorApplication.OpenScene(assetPath))
					{
						foreach (GameObject go in Object.FindObjectsOfType(typeof(GameObject)))
						{
							SearchGameObject(go, assetData, true);
						}
					}
					break;
				case ".controller":

#if UNITY_5
					var anim = (UnityEditor.Animations.AnimatorController)AssetDatabase.LoadAssetAtPath(assetPath, typeof(UnityEditor.Animations.AnimatorController));
#else
					var anim = (AnimatorController)AssetDatabase.LoadAssetAtPath(assetPath, typeof(AnimatorController));
					for (int r=0; r<anim.layerCount; r++) {
						var layer = anim.GetLayer (r);
						for (int f=0; f< layer.stateMachine.stateCount; f++) {
							var state = layer.stateMachine.GetState (f);
							var clip = state.GetMotion () as AnimationClip;
							
							if (clip == null)
								continue;
							
							AddAttachedAsset(anim, clip, assetData, false);
						}
					}
					break;
#endif
				case ".mat":
					
					var material = AssetDatabase.LoadAssetAtPath(assetPath, typeof(Material)) as Material;
					
					AddAttachedAsset(material, material.shader, assetData, false);
					
					var propertyCount = ShaderUtil.GetPropertyCount(material.shader);
					
					for (var j = 0; j < propertyCount; j++)
					{
						if (ShaderUtil.GetPropertyType(material.shader, j) != ShaderUtil.ShaderPropertyType.TexEnv)
							continue;
						var propertyName = ShaderUtil.GetPropertyName(material.shader, j);
						AddAttachedAsset(material, material.GetTexture(propertyName), assetData, false);
					}
					break;
				default:
					SearchFieldAndProperty(AssetDatabase.LoadAssetAtPath(assetPath, typeof(Object)), assetData);
					break;
				}
				ArrayUtility.Add(ref result, assetData);
			}
			callback(result);
			depths.Clear();
			EditorUtility.ClearProgressBar();
		}

		private static void DisplayProgressBar(string path, float progress)
		{
			EditorUtility.DisplayProgressBar(Path.GetFileName(path), Mathf.FloorToInt(progress * 100) + "% - " + Path.GetFileName(path), progress);
		}
		
		private static void SearchGameObject(GameObject go, AssetData assetData, bool isScene = false)
		{
			foreach (var obj in go.GetComponentsInChildren<Component>().Where(obj => obj))
			{
				AddAttachedAsset(obj, obj, assetData, isScene);
				
				SearchFieldAndProperty(obj, assetData, isScene);
			}
		}
		
		private static void SearchFieldAndProperty(Object obj, AssetData assetData, bool isScene = false)
		{
			SearchFieldAndProperty(obj, obj, assetData, isScene);
		}
		
		private static void SearchFieldAndProperty(Object obj, object val, AssetData assetData, bool isScene = false)
		{
			if (!obj || obj is NavMeshAgent || ignoreTypes.Contains(obj.GetType().Name))
				return;
			
			SearchField(obj, val, assetData, isScene);
			SearchProperty(obj, val, assetData, isScene);
		}
		
		private static void SearchField(Object component, object val, AssetData assetData, bool isScene)
		{
			var fields = val.GetType().GetFields(flags);
			foreach (var info in fields)
			{
				
				var isObject = info.FieldType.IsSubclassOf(typeof(Object)) ||
					info.FieldType == typeof(Object) ||
						info.FieldType.IsSerializable &&
						info.FieldType.IsClass;
				
				
				if (!isObject || Ignore(val.GetType(), info.Name)) continue;
				
				var value = info.GetValue(val);
				if (value != null)
				{
					
					AddAttachedAssets(component, value, assetData, isScene);
					
					if (!depths.ContainsKey(value))
					{
						depths.Add(value, 0);
					}
					
					if (!ignoreTypes.Contains(value.GetType().Name) && depths[value]++ <= 100)
					{
						SearchField(component, value, assetData, isScene);
					}
				}
			}
		}
		
		private static void SearchProperty(Object component, object val, AssetData assetData, bool isScene)
		{
			var properties = val.GetType().GetProperties(flags);
			
			foreach (var info in properties.Where(info => info.CanRead))
			{
				var isObject = info.PropertyType.IsSubclassOf(typeof(Object)) ||
					info.PropertyType == typeof(Object);
				
				if (!isObject || Ignore(val.GetType(), info.Name)) continue;
				
				var value = info.GetValue(val, new object[0]);
				if (value != null)
				{
					AddAttachedAssets(component, value, assetData, isScene);
				}
			}
		}
		
		private static bool Ignore(Type type, string name)
		{
			var ignores = new[]
			{
				new {name = "mesh", type = typeof (MeshFilter)},
				new {name = "material", type = typeof (Renderer)},
				new {name = "material", type = typeof (WheelCollider)},
				new {name = "material", type = typeof (TerrainCollider)},
				new {name = "material", type = typeof (GUIElement)}
			};
			
			return ignores.Any(ignore => Ignore(type, name, ignore.type, ignore.name));
		}
		
		private static bool Ignore(Type type, string name, Type ignoreType, string ignoreName)
		{
			var isIgnoreType = type == ignoreType || type.IsSubclassOf(ignoreType);
			
			return isIgnoreType && name == ignoreName;
		}
		
		private static void AddAttachedAssets(Object component, object value, AssetData assetData, bool isScene)
		{
			var values = new List<Object>();
			if (value.GetType().IsArray)
			{
				values.AddRange(((Array)value).Cast<object>()
				                .Where(v => v != null)
				                .Select(v => v as Object));
			}
			else
			{
				values.Add(value as Object);
			}
			
			foreach (var v in values)
			{
				AddAttachedAsset(component, v, assetData, isScene);
			}
		}
		
		private static void AddAttachedAsset(Object component, Object value, AssetData assetData, bool isScene)
		{
			if (!value) return;
			
			if (value as MonoBehaviour)
			{
				
				value = MonoScript.FromMonoBehaviour(value as MonoBehaviour);
				
				if (isScene)
				{
					AddSceneData(component, value, assetData);
				}
				
			}
			else if (value as ScriptableObject)
			{
				value = MonoScript.FromScriptableObject(value as ScriptableObject);
				
				if (isScene)
				{
					AddSceneData(component, value, assetData);
				}
			}
			else if (isScene && PrefabUtility.GetPrefabType(value) == PrefabType.PrefabInstance)
			{
				var name = "";
				var gameObject = GetGameObject(component, value);
				
				if (gameObject)
					name = GetName(PrefabUtility.FindPrefabRoot(gameObject).transform);
				
				value = PrefabUtility.GetPrefabParent(value);
				if (string.IsNullOrEmpty(name))
					name = value.name;
				
				assetData.sceneData.Add(new SceneData
				                        {
					name = name,
					typeName = value.GetType().FullName,
					guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(value))
				});
			}
			else if (isScene)
			{
				AddSceneData(component, value, assetData);
			}
			
			var path = AssetDatabase.GetAssetPath(value);
			
			if (string.IsNullOrEmpty(path)) return;
			
			var guid = AssetDatabase.AssetPathToGUID(path);
			if (!assetData.reference.Contains(guid))
				assetData.reference.Add(guid);
		}
		
		private static void AddSceneData(Object component, Object value, AssetData assetData)
		{
			var _path = AssetDatabase.GetAssetPath(value);
			if (!string.IsNullOrEmpty(_path))
			{
				var name = value.name;
				var gameObject = GetGameObject(component, value);
				
				if (gameObject)
					name = GetName(gameObject.transform);
				
				assetData.sceneData.Add(new SceneData
				                        {
					name = name,
					typeName = value.GetType().FullName,
					guid = AssetDatabase.AssetPathToGUID(_path)
				});
			}
		}
		
		private static GameObject GetGameObject(Object component, Object gameObject)
		{
			GameObject _gameObject = null;
			
			if (component as Component)
			{
				_gameObject = (component as Component).gameObject;
			}
			
			if (!gameObject && gameObject as GameObject)
			{
				_gameObject = gameObject as GameObject;
			}
			return _gameObject;
		}
		
		private static string GetName(Transform transform, string name = "")
		{
			while (true)
			{
				name = transform.name + name;
				if (!transform.parent) return name;
				transform = transform.parent;
				name = "/" + name;
			}
		}
		
		
	}
}
#endif