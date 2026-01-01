using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class DataContainer
{
    public List<string> Keys;
    public List<string> Datas;
    public DataContainer()
    {
        Keys = new List<string>();
        Datas = new List<string>();
    }
}
public abstract class DataSave : IData
{
    public abstract void Load();
    public abstract void Save();
}
public abstract class SavePlayerPrefs : DataSave
{
    public override void Load()
    {
        string keyData = GetType().Name;
        if (PlayerPrefs.HasKey(keyData))
        {
            string jsonData = PlayerPrefs.GetString(keyData);
            JsonUtility.FromJsonOverwrite(jsonData, this);
        }
        else Save();
    }
    public override void Save()
    {
        string keyData = GetType().Name;
        string jsonData = JsonUtility.ToJson(this);
        PlayerPrefs.SetString(keyData, jsonData);
    }
}
public class GameData : DataProvider<DataSave>
{
    public void Reset()
    {
        foreach (KeyValuePair<Type, DataSave> data in dataMap)
            dataMap[data.Key] = CreateInstance(data.Key);
    }
    public void SaveAll()
    {
        foreach (KeyValuePair<Type, DataSave> data in dataMap)
            data.Value.Save();
    }
    public void Save<T>() where T : DataSave
    {
        Type type = typeof(T);
        if (dataMap.ContainsKey(type))
            dataMap[type].Save();
    }
    public void DeleteAllKeys()
    {
        foreach (KeyValuePair<Type, DataSave> data in dataMap)
            PlayerPrefs.DeleteKey(data.Key.Name);
    }
    public bool Import(string jsonData)
    {
        try
        {
            DataContainer dataContainer = new DataContainer();
            JsonUtility.FromJsonOverwrite(jsonData, dataContainer);
            Dictionary<Type, DataSave> dataMap = new Dictionary<Type, DataSave>();
            for (int i = 0; i < dataContainer.Keys.Count; i++)
            {
                try
                {
                    Type type = Type.GetType(dataContainer.Keys[i]);
                    object data = JsonUtility.FromJson(dataContainer.Datas[i], type);
                    dataMap.Add(type, data as DataSave);
                }
                catch (Exception exception)
                {
                    Debug.Log(exception.Message);
                    continue;
                }
            }
            base.dataMap = dataMap;
            return true;
        }
        catch (Exception exception)
        {
            Debug.Log(exception.Message);
            return false;
        }
    }
    public string Export()
    {
        DataContainer dataContainer = new DataContainer();
        foreach (KeyValuePair<Type, DataSave> data in dataMap)
        {
            dataContainer.Keys.Add(data.Key.Name);
            dataContainer.Datas.Add(JsonUtility.ToJson(data.Value));
        }
        return JsonUtility.ToJson(dataContainer);
    }
}