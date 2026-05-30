using System;
using System.Collections.Generic;

public interface IData
{
    void Load();
}
public class DataProvider<T> where T : IData
{
    protected Dictionary<Type, T> dataMap;
    protected DataProvider()
    {
        dataMap = new Dictionary<Type, T>();
    }
    protected T CreateInstance(Type type)
    {
        T instance = (T)Activator.CreateInstance(type);
        instance.Load();
        return instance;
    }
    public Data Load<Data>() where Data : T
    {
        Type type = typeof(Data);
        if (!dataMap.ContainsKey(type))
        {
            T data = CreateInstance(type);
            dataMap.Add(type, data);
        }
        return (Data)dataMap[type];
    }
}