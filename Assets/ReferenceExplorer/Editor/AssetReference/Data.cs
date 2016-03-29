using System.Collections.Generic;
using UnityEngine;

namespace ReferenceViewer
{
    [System.Serializable]
    public class Data : ScriptableObject
    {
        public List<AssetData> assetData = new List<AssetData>();
    }

    [System.Serializable]
    public class SceneData
    {
        public string guid;
        public string typeName;
        public string name;
    }

    [System.Serializable]
    public class AssetData
    {
        public string path = "";
        public string guid = "";
        public List<string> reference = new List<string>();
        public List<SubAssetData> subAssets = new List<SubAssetData>();
        public List<SceneData> sceneData = new List<SceneData>();
    }

    [System.Serializable]
    public class SubAssetData
    {
        public string guid = "";
        public string name = "";
        public string typeName = "";
    }
}