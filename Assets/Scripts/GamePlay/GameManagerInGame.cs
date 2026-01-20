using System;
using System.Collections;
using Sirenix.OdinInspector;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

using System.Collections;
using System.Collections.Generic;

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
    public UserData userData { get; private set; }
    public bool InitLevel = true;
    private Coroutine _playRoutine;

    [SerializeField] private List<ParticleSystem> _winEffect;

    private new void Awake()
    {
        base.Awake();

        if (!Game.IsLaunched)
            Game.Launch();

        userData = Game.Data.Load<UserData>();
        if (userData != null && !userData.isDefaultData)
        {
            userData.SetDefaultData();
            userData.Save();
        }
        if (!InitLevel)
        {
            LoadData();
        }
        StartGame(CurrentLevel);

        // SetUpNotification();
    }

    public void PlayVfxWin(){
        foreach (var effect in _winEffect)
        {
            effect.gameObject.SetActive(true);
            effect.Play();
        }
    }

    public void ClearVfx()
    {
        foreach (var effect in _winEffect)
        {
            effect.gameObject.SetActive(false);
        }
    }


    public void SetWin()
    {
        CurrentLevel = Mathf.Max(1, CurrentLevel + 1);
        MaxLevel = Mathf.Max(MaxLevel, CurrentLevel);
        SaveData();
        PlayVfxWin();
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
        level = Mathf.Max(1, level);
        CurrentLevel = level;
        MaxLevel = Mathf.Max(MaxLevel, level);
        SaveData();

        if (_playRoutine != null)
        {
            StopCoroutine(_playRoutine);
            _playRoutine = null;
        }
        _playRoutine = StartCoroutine(PlayGame(level));

        SpawnUI();
        ClearVfx();
    }

    public void SpawnUI()
    {
        GameUI.Instance.Get<UITopInGame>().Show();
        GameUI.Instance.Get<UIBottomInGame>().Show();
    }

    public void RestartLevel()
    {
        StartGame(CurrentLevel);
    }

    public void StartNextLevel()
    {
        StartGame(CurrentLevel);
    }

    private IEnumerator PlayGame(int level)
    {
        CurrentLevel = level;
        ResourceRequest soRequest = Resources.LoadAsync<LevelConfig>("Levels/SO/Level " + level);
        yield return soRequest;

        LevelConfig config = soRequest.asset as LevelConfig;

#if UNITY_EDITOR
        if (config == null)
        {
            soRequest = Resources.LoadAsync<LevelConfig>("Levels/SO/Level " + level);
            yield return soRequest;
            config = soRequest.asset as LevelConfig;
        }
#endif

        if (config == null)
        {
            Debug.LogError($"Failed to load level {level}. Missing `Resources/Levels/SO/Level {level}.asset`.");
            _playRoutine = null;
            yield break;
        }
        if (Board.Instance == null)
        {
            Debug.LogError("Board.Instance is null; cannot setup level.");
            _playRoutine = null;
            yield break;
        }

        Board.Instance.SetupLevel(config);
        _playRoutine = null;
    }
    public void SaveData()
    {
        if (userData == null) return;
        userData.maxLevel = Mathf.Max(1, MaxLevel);
        userData.currentLevel = Mathf.Max(1, CurrentLevel);
        userData.Save();
    }
    public void LoadData()
    {
        if (userData == null)
        {
            MaxLevel = 1;
            CurrentLevel = 1;
            return;
        }

        MaxLevel = Mathf.Max(1, userData.maxLevel);
        CurrentLevel = Mathf.Clamp(userData.currentLevel, 1, MaxLevel);
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

    // private void SetUpNotification()
    // {
    //     API.Initialize();
    //     API.SendNotification("HEXACOIN!", "Come get your free coins", new System.TimeSpan(1, 0, 0, 0), "icon_1", "icon_0");
    //     API.SendNotification("HEXACOIN!", "Come get your free coins", new System.TimeSpan(3, 0, 0, 0), "icon_1", "icon_0");
    //     API.SendNotification("HEXACOIN!", "Come get your free coins", new System.TimeSpan(6, 0, 0, 0), "icon_1", "icon_0");
    //     API.SendNotification("HEXACOIN!", "Come get your free coins", new System.TimeSpan(9, 0, 0, 0), "icon_1", "icon_0");
    // }
}
