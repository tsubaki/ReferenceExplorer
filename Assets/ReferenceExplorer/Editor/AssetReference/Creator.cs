using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEditor;
using System.IO;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ReferenceViewer
{
    public class Creator
    {
        public static void Build(Action callback = null)
        {
            if (!EditorApplication.SaveCurrentSceneIfUserWantsTo()) return;

            var currentScene = EditorApplication.currentScene;

           
           
            Generate.Build(AssetDatabase.GetAllAssetPaths(), assetData =>
            {
                var data = ScriptableObject.CreateInstance<Data>();
               
                data.assetData.AddRange(assetData);
                Export(data);
                if (string.IsNullOrEmpty(currentScene))
                    EditorApplication.NewScene();
                else
                    EditorApplication.OpenScene(currentScene);
               
               
                EditorUtility.UnloadUnusedAssets();
                if (callback != null)
                    callback();
            });
        }

        private static void Export(Data data)
        {
            data.assetData = data.assetData.OrderBy(d => Path.GetExtension(d.path)).ToList();
            const string directory = "build/ReferenceViewer";

            Directory.CreateDirectory(directory);

            foreach (var assetData in data.assetData.Where(assetData => assetData.sceneData.Count != 0))
            {
                assetData.sceneData =
                    assetData.sceneData.Distinct(new CompareSelector<SceneData, string>(s => s.name + s.guid)).ToList();
            }
            File.Delete(directory + "/data.dat");
            UnityEditorInternal.InternalEditorUtility.SaveToSerializedFileAndForget(new Object[] { data }, directory + "/data.dat", true);
        }

        static byte[] ObjectToByteArray(object obj)
        {
            if (obj == null)
                return null;
            var bf = new BinaryFormatter();
            var ms = new MemoryStream();
            bf.Serialize(ms, obj);
            return ms.ToArray();
        }
    }
    public static class Extensions
    {
        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> source)
        {
            return new HashSet<T>(source);
        }
    }
}