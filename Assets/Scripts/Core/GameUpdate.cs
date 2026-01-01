using System;
using System.Threading;
using System.Collections.Generic;
using UnityEngine;

public class GameUpdate : MonoBehaviour
{
    private int elapsed;
    private int timeUnix;
    private Action onUpdate;
    private List<Action> tasks = new List<Action>();
    private DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
    public int Current => timeUnix + elapsed;
    private void Update() => onUpdate?.Invoke();
    private void Start()
    {
        Thread timer = new Thread(UpdateTimer);
        timer.IsBackground = true;
        timer.Start();
        DateTime current = DateTime.Now.ToUniversalTime();
        timeUnix = (int)current.Subtract(epoch).TotalSeconds;
    }
    private void UpdateTimer()
    {
        while (elapsed < int.MaxValue)
        {
            elapsed++;
            Thread.Sleep(1000);
        }
    }
    public void RemoveAllTask()
    {
        onUpdate = null;
        tasks.Clear();
    }
    public void AddTask(Action task)
    {
        if (tasks.Contains(task)) return;
        onUpdate += task;
        tasks.Add(task);
    }
    public void RemoveTask(Action task)
    {
        if (tasks.Remove(task)) onUpdate -= task;
    }
}