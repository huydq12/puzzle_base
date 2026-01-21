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
    private int _levelInPlay = 1;
    private int _queuedNextLevel = 1;
    private float _lastStartRequestTime = -999f;
    private const float StartRequestCooldownSeconds = 0.25f;

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

        // SetUpNotification();
    }
    void Start()
    {
        StartGame(CurrentLevel);
    }
    public void PlayVfxWin()
    {
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
        int completedLevel = Mathf.Max(1, _levelInPlay);
        CurrentLevel = completedLevel + 1;
        _queuedNextLevel = CurrentLevel;
        MaxLevel = Mathf.Max(MaxLevel, CurrentLevel);
        SaveData();
        // Grant unlock gift for the newly reached level (config unlockLevel matches StartGame level).
        BoosterUnlockService.TryGrantUnlockGift(CurrentLevel);
        var bottom = GameUI.Instance != null ? GameUI.Instance.Get<UIBottomInGame>() : null;
        if (bottom != null) bottom.RefreshBoosterQuantity();
        PlayVfxWin();
        SetState(GameStateInGame.Result);
    }
    public void SetLose()
    {
        _queuedNextLevel = Mathf.Max(1, _levelInPlay);
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
        // Prevent spam-tapping Replay/Restart from interrupting setup and leaving pooled objects mid-state.
        if (_playRoutine != null) return;
        if (Time.unscaledTime - _lastStartRequestTime < StartRequestCooldownSeconds) return;
        _lastStartRequestTime = Time.unscaledTime;

        _levelInPlay = level;
        _queuedNextLevel = level;
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
        if (TutorialManager.Instance != null) TutorialManager.Instance.TryShowTutorial(CurrentLevel);
        BoosterUnlockService.TryShowUnlockTutorialAtLevelStart(CurrentLevel);
        TutorialPopupService.TryShowAtLevelStart(CurrentLevel);
        ClearVfx();
    }

    public void SpawnUI()
    {
        GameUI.Instance.Get<UITopInGame>().Show();
        GameUI.Instance.Get<UIBottomInGame>().Show();
    }

    public void RestartLevel()
    {
        ReplayLevel();
    }

    public void StartNextLevel()
    {
        StartGame(_queuedNextLevel);
    }

    public void ReplayLevel()
    {
        StartGame(_levelInPlay);
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
        userData.maxLevel = Mathf.Max(1, Mathf.Max(userData.maxLevel, MaxLevel));
        userData.currentLevel = Mathf.Max(1, Mathf.Max(userData.currentLevel, CurrentLevel));
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
