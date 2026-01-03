using System;
using System.Collections;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using Sirenix.Serialization;
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
    public int CurrentLevel = 2;
    [ReadOnly] public GameStateInGame CurrentGameStateInGame = GameStateInGame.Init;
    [HideInInspector] public Action OnEndLevel;
    [HideInInspector] public Action OnStartLevel;
    public UserData userData { get; private set; }
    private new void Awake()
    {
        base.Awake();
        // LoadData();
        StartGame(MaxLevel);
    }
    public void SetWin()
    {
        CurrentLevel++;
        if (CurrentLevel > MaxLevel)
        {
            MaxLevel = CurrentLevel;
        }
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
        StartCoroutine(PlayGame(CurrentLevel));
    }
    public void StartGame(int level)
    {
        if (level > MaxLevel)
        {
            MaxLevel = level;
        }
        StartCoroutine(PlayGame(level));
    }
    private IEnumerator PlayGame(int level)
    {
        CurrentLevel = level;
        ResourceRequest request = Resources.LoadAsync<LevelConfig>("Levels/SO/Level " + level);

        yield return request;

        LevelConfig config = request.asset as LevelConfig;
        Board.Instance.SetupLevel(config);
    }
    public void SaveData()
    {
        PlayerPrefs.SetInt("MaxLevel", MaxLevel);
    }
    public void LoadData()
    {
        MaxLevel = PlayerPrefs.GetInt("MaxLevel", 1);
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
}
