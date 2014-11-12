using System.Collections.Generic;

namespace ReferenceViewer
{
    [System.Serializable]
    public class Data
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
        public List<SceneData> sceneData = new List<SceneData>();
    }
}