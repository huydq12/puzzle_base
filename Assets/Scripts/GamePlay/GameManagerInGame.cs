using System;
using System.Collections;
using Sirenix.OdinInspector;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using Gley.Notifications;

public enum GameStateInGame
{
    Init,
    Home,
    Playing,
    Pause,
    Result
}

public class GameManagerInGame : Singleton<GameManagerInGame>
{
    public int MaxLevel = 1;
    public int CurrentLevel = 1;
    [ReadOnly] public GameStateInGame CurrentGameStateInGame = GameStateInGame.Init;
    [HideInInspector] public Action OnEndLevel;
    [HideInInspector] public Action OnStartLevel;

    private new void Awake()
    {
        base.Awake();

        API.Initialize();
    }
    void Start()
    {
        StartGame(CurrentLevel);
    }

    public void SetWin()
    {
        SetState(GameStateInGame.Result);
    }
    public void SetLose()
    {
        SetState(GameStateInGame.Result);
    }
    public void SetState(GameStateInGame state)
    {
        CurrentGameStateInGame = state;
        switch (state)
        {
            case GameStateInGame.Result:
                {
                    OnEndLevel?.Invoke();
                    break;
                }
            case GameStateInGame.Init:
                {
                    OnStartLevel?.Invoke();
                    break;
                }
            default:
                break;
        }
    }
    public void StartGame()
    {
        StartGame(CurrentLevel);
    }
    public void StartGame(int level)
    {
      
    }
    public void SaveData()
    {
      
    }
    public void LoadData()
    {
      
    }
#if UNITY_EDITOR
    new void OnApplicationQuit()
    {
        base.OnApplicationQuit();
        SaveData();
    }
#else
    void OnApplicationPause(bool pause)
    {
        if (pause)
        {
            SaveData();
        }
    }
#endif

    void OnApplicationFocus(bool focus)
    {
        if (!focus)
        {
            API.SendNotification("Arrow Shooter!", "Come get your free coins", new System.TimeSpan(0, 5, 0), "icon_0", "icon_1");
            API.SendNotification("Arrow Shooter!", "Come get your free coins", new System.TimeSpan(1, 0, 0, 0), "icon_0", "icon_1");
            API.SendNotification("Arrow Shooter!", "Come get your free coins", new System.TimeSpan(3, 0, 0, 0), "icon_0", "icon_1");
            API.SendNotification("Arrow Shooter!", "Come get your free coins", new System.TimeSpan(6, 0, 0, 0), "icon_0", "icon_1");
            API.SendNotification("Arrow Shooter!", "Come get your free coins", new System.TimeSpan(9, 0, 0, 0), "icon_0", "icon_1");
        }
        else
        {
            API.CancelAllNotifications();
        }
    }

}
