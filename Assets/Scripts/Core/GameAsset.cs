using UnityEngine;
using System.Collections.Generic;

public class GameAsset
{
    private const string folderPath = "Asset/";
    private Dictionary<string, AssetReference> assetMap;
    public GameAsset() => assetMap = new Dictionary<string, AssetReference>();
    public AssetReference Get(string name)
    {
        if (assetMap.ContainsKey(name)) return assetMap[name];
        AssetReference assetReference = Resources.Load<AssetReference>(folderPath + name);
        if (assetReference != null) assetMap.Add(name, assetReference);
        return assetReference ?? default;
    }
}