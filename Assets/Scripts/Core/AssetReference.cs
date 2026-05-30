using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
[CreateAssetMenu()]
public class AssetReference : ScriptableObject
{
    public List<Object> listAsset;
    private Dictionary<string, int> indexMap;
    private void AutoInit()
    {
        if (indexMap != null && indexMap.Count > 0) return;
        indexMap = new Dictionary<string, int>();
        for (int index = 0; index < listAsset.Count; index++)
            indexMap.Add(listAsset[index].name, index);
    }
    public Asset Get<Asset>(string name) where Asset : Object
    {
        AutoInit();
        if (indexMap.ContainsKey(name))
            return listAsset[indexMap[name]] as Asset;
        return default;
    }
#if UNITY_EDITOR
    [ContextMenu("ConvertTextureToSprite")]
    private void ConvertTextureToSprite()
    {

        int numberObjectConverted = 0;
        if (listAsset is null || listAsset.Count == 0) return;
        List<Sprite> sprites = new List<Sprite>();
        foreach (Object asset in listAsset)
        {
            string assetPath = AssetDatabase.GetAssetPath(asset);
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite == null) continue;
            numberObjectConverted++;
            sprites.Add(sprite);
        }
        if (listAsset.Count != numberObjectConverted) return;
        listAsset = new List<Object>(sprites);
    }
#endif
}