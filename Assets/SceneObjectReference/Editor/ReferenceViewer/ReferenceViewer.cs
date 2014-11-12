using System;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using Object = UnityEngine.Object;

namespace ReferenceViewer
{
    public class ReferenceViewer : EditorWindow
    {

        private List<Item> items = new List<Item>();
        private Vector2 pos = Vector2.zero;
        private int selectedFilter;

        static Dictionary<string, List<GUIContent>> sceneReference = new Dictionary<string, List<GUIContent>>();

        static Dictionary<string, bool> foldouts = new Dictionary<string, bool>();

        [MenuItem("Window/ReferenceViewer")]
        private static void Open()
        {
            GetWindow<ReferenceViewer>();
        }

        [MenuItem("Assets/Find References In Project", true)]
        private static bool FindValidate()
        {
            return Selection.objects.Length != 0;
        }

        [MenuItem("Assets/Find References In Project")]
        private static void Find()
        {
            sceneReference.Clear();
            var path = "build/ReferenceViewer/data.dat";

            var guids = Selection.objects.Select(obj => AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(obj))).ToArray();

            Action find = () =>
            {
                var text = File.ReadAllBytes(path);
                var data = ByteArrayToObject<Data>(text);

                Find(data, guids);
            };

            if (File.Exists(path))
            {
                find();
            }
            else
            {
                if (EditorUtility.DisplayDialog("必要なデータがありません", "データを作成します。\nデータ作成に時間がかかりますがよろしいですか？", "はい", "いいえ"))
                {
                    Creator.Build(find);
                }
            }
        }

        private static void Find(Data data, params string[] guids)
        {
            var items = guids
               .Select(guid => new
               {
                   type = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guid), typeof(Object)).GetType(),
                   searched = GetGUIContent(guid),
                   referenced =
                       data.assetData.Where(assetData => assetData.reference.Contains(guid))
                           .Select(assetData => GetGUIContent(assetData.guid))
                           .Where(c => c.image && guid != AssetDatabase.AssetPathToGUID(c.tooltip))
                           .OrderBy(c => c.image.name)
                           .ToList(),
                   reference =
                       data.assetData.Find(item => item.guid == guid)
                           .reference.Where(g => g != guid)
                           .Select(g => GetGUIContent(g))
                           .Where(c => c.image)
                           .OrderBy(c => c.image.name)
                           .ToList()
               })
               .Where(item => (item.referenced.Count != 0 || item.reference.Count != 0) && item.searched.image)
               .OrderBy(item => item.searched.image.name)
               .Select(item => new Item
               {
                   type = item.type,
                   searchedGUIContent = item.searched,
                   referencedGUIContents = item.referenced,
                   referenceGUIContents = item.reference
               })
               .Distinct(new CompareSelector<Item, string>(i => i.searchedGUIContent.tooltip))
               .ToList();
            foreach (var item in items)
            {
                foreach (var i in item.referencedGUIContents)
                {
                    if (Path.GetExtension(i.tooltip) == ".unity")
                    {
                        var d = data.assetData.Find(asset => asset.path == i.tooltip).sceneData;
                        var key = item.searchedGUIContent.tooltip + " - " + i.tooltip;
                        if (sceneReference.ContainsKey(key))
                        {
                            sceneReference[key].AddRange(d.Select(s => new GUIContent(s.name, AssetDatabase.GUIDToAssetPath(s.guid))).ToList());
                        }
                        else
                        {
                            sceneReference.Add(key, d.Select(s => new GUIContent(s.name, AssetDatabase.GUIDToAssetPath(s.guid))).ToList());
                        }


                    }
                }
            }

            GetWindow<ReferenceViewer>().Results(items);
        }

        private void Results(List<Item> items)
        {
            this.items = items;
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Update", EditorStyles.toolbarButton))
            {
                Creator.Build();
				EditorGUIUtility.ExitGUI();
            }

            EditorGUI.BeginChangeCheck();
            var types = items.Select(item => item.type).ToArray();
            var display = types.Select(t => t.Name).ToArray();
            for (var i = 0; i < display.Length; i++)
            {
                switch (display[i])
                {
                    case "Object":
                        display[i] = "Scene";
                        break;
                    case "GameObject":
                        display[i] = "Prefab";
                        break;
                }
            }
            ArrayUtility.Insert(ref display, 0, "All");
            var selected = EditorGUILayout.Popup(selectedFilter, display, EditorStyles.toolbarPopup);
            if (EditorGUI.EndChangeCheck())
            {
                selectedFilter = selected;
            }
            EditorGUILayout.EndHorizontal();

            if (items.Count == 0) return;

            pos = EditorGUILayout.BeginScrollView(pos);

            foreach (var item in items)
            {
                if (selectedFilter != 0 && item.type != types[selectedFilter - 1])
                {
                    continue;
                }

                EditorGUILayout.BeginHorizontal("box", GUILayout.Width(Screen.width * 0.96f));
                DrawGUIContents(item.searchedGUIContent, item.referenceGUIContents);
                var iconSize = EditorGUIUtility.GetIconSize();
                EditorGUIUtility.SetIconSize(Vector2.one * 32);
                GUILayout.Label(item.searchedGUIContent, GUILayout.Width(Screen.width * 0.3f), GUILayout.ExpandWidth(false));
                EditorGUIUtility.SetIconSize(iconSize);
                PingObjectIfOnMouseDown(item.searchedGUIContent.tooltip);

                DrawGUIContents(item.searchedGUIContent, item.referencedGUIContents);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space();
            }
            EditorGUILayout.EndScrollView();
        }

        private static void DrawGUIContents(GUIContent searched, List<GUIContent> contents)
        {
            if (contents.Count != 0)
            {
                EditorGUILayout.BeginVertical(GUILayout.Width(Screen.width * 0.3f));

                foreach (var content in contents)
                {
                    if (IsScene(content))
                    {
                        var key = searched.tooltip + " - " + content.tooltip;

                        if (!foldouts.ContainsKey(key))
                        {
                            foldouts.Add(key, false);
                        }

                        foldouts[key] = EditorGUILayout.Foldout(foldouts[key], content);

                        if (foldouts[key])
                        {
                            if (sceneReference.ContainsKey(key))
                            {
                                EditorGUI.indentLevel++;
                                foreach (var sceneData in sceneReference[key])
                                {
                                    if (searched.tooltip == sceneData.tooltip)
                                        EditorGUILayout.LabelField(sceneData, EditorStyles.miniLabel, GUILayout.Width(Screen.width * 0.3f), GUILayout.ExpandWidth(true));
                                }
                                EditorGUI.indentLevel--;
                            }
                        }
                    }
                    else
                    {
                        EditorGUILayout.LabelField(content, GUILayout.Width(Screen.width * 0.3f), GUILayout.ExpandWidth(true));
                    }

                    PingObjectIfOnMouseDown(content.tooltip);
                }
                EditorGUILayout.EndVertical();
            }
            else
            {
                GUILayout.Space(Screen.width * 0.3f + 16);
            }
        }

        

        private static bool IsScene(GUIContent content)
        {
            return Path.GetExtension(content.tooltip) == ".unity";
        }

        private static void PingObjectIfOnMouseDown(string path)
        {
            if (Event.current.type != EventType.MouseDown) return;
            if (!GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition)) return;

            var obj = AssetDatabase.LoadAssetAtPath(path, typeof(Object));
            Selection.activeObject = obj;
            EditorGUIUtility.PingObject(obj);
        }

        private static GUIContent GetGUIContent(string guidOrAssetPath)
        {
            var assetPath = File.Exists(guidOrAssetPath) ? guidOrAssetPath : AssetDatabase.GUIDToAssetPath(guidOrAssetPath);

            var asset = AssetDatabase.LoadAssetAtPath(assetPath, typeof(Object));

            return GetGUIContent(asset);
        }

        private static GUIContent GetGUIContent(Object obj)
        {
            var content = new GUIContent(EditorGUIUtility.ObjectContent(obj, obj.GetType()));

            var type = PrefabUtility.GetPrefabType(obj);

            if (type == PrefabType.Prefab)
            {
                var icon = EditorGUIUtility.Load("Icons/Generated/PrefabNormal Icon.asset") as Texture2D;
                content.image = icon;
            }

            content.tooltip = AssetDatabase.GetAssetPath(obj);

            return content;
        }

        private static T ByteArrayToObject<T>(byte[] arrBytes)
        {
            T obj;
            using (var memStream = new MemoryStream())
            {
                var binForm = new BinaryFormatter();
                memStream.Write(arrBytes, 0, arrBytes.Length);
                memStream.Seek(0, SeekOrigin.Begin);
                obj = (T)binForm.Deserialize(memStream);
            }

            return obj;
        }
        private class Item
        {
            public Type type;
            public GUIContent searchedGUIContent;
            public List<GUIContent> referencedGUIContents = new List<GUIContent>();
            public List<GUIContent> referenceGUIContents = new List<GUIContent>();
        }
    }
}