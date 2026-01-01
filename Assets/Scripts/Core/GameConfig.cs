using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;

public abstract class Field
{
    public abstract int ID
    {
        get; protected set;
    }
    public void Read(string[] rowDatas)
    {
        PropertyInfo[] propertyInfos = GetType().GetProperties();
        int length = Mathf.Min(rowDatas.Length, propertyInfos.Length);
        for (int i = 0; i < length; i++)
        {
            Type propertyType = propertyInfos[i].PropertyType;
            string stringValue = rowDatas[i].Replace("\"\"\"", "\"");
            object value = Convert.ChangeType(stringValue, propertyType);
            propertyInfos[i].SetValue(this, value);
        }
    }
}
public abstract class Table : IData
{
    public abstract void Load();
    protected string folderPath = "Config/";
    protected string GetConfigFromResources()
    {
        string dataPath = folderPath + GetType().Name;
        TextAsset textAsset = Resources.Load<TextAsset>(dataPath);
        return textAsset != null ? textAsset.text : string.Empty;
    }
}
public abstract class Table<T> : Table where T : Field
{
    public List<T> listData
    {
        get; private set;
    }
    public T this[int id]
    {
        get
        {
            if (indexMap.ContainsKey(id))
                return listData[indexMap[id]];
            return default;
        }
    }
    private Dictionary<int, int> indexMap;
    public override void Load()
    {
        listData = new List<T>();
        indexMap = new Dictionary<int, int>();
        string configData = GetConfigFromResources();
        if (string.IsNullOrWhiteSpace(configData)) return;
        CSVReader reader = new CSVReader(configData);
        for (int i = 0; i < reader.TotalRow; i++)
        {
            T config = Activator.CreateInstance<T>();
            config.Read(reader.GetRowData(i));
            indexMap.Add(config.ID, i);
            listData.Add(config);
        }
    }
}
public class GameConfig : DataProvider<Table>
{
}