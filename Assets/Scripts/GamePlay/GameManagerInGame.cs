using System;
using System.Collections;
using Sirenix.OdinInspector;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

using System.Collections;
using System.Collections.Generic;
using Gley.Notifications;
using TMPro;
using UnityEngine.UI;

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
    public static GameManagerInGame intance => Instance;
    public int MaxLevel = 1;
    public int CurrentLevel = 1;
    [ReadOnly] public GameStateInGame CurrentGameStateInGame = GameStateInGame.Init;
    [HideInInspector] public Action OnEndLevel;
    [HideInInspector] public Action OnStartLevel;
    public UserData userData { get; private set; }
    public bool InitLevel = true;
    private Coroutine _playRoutine;
    private Coroutine _hideLoadingRoutine;
    private int _levelInPlay = 1;
    private int _contentLevelInPlay = 1;
    private int _queuedNextLevel = 1;
    private float _lastStartRequestTime = -999f;
    private const float StartRequestCooldownSeconds = 0.25f;

    private int _lastTrackedStartLevel = -1;
    private int _lastTrackedFinishLevel = -1;
    private bool _lastTrackedFinishWasWin;

    private bool _isFirstSceneStart = true;
    private bool _nextStartIsAfterWin;
    private bool _pendingAutoHideLoading;
    private float _pendingAutoHideSeconds;

    private const int LoopStartLevel = 30;
    private const int LoopEndLevel = 292;

    [SerializeField] private float CONST_TIME_HIDE_LOADING = 2f;
    [SerializeField] private float CONST_TIME_HIDE_LOADING_FIRST = 3f;

    [SerializeField] private int CONST_LEVEL_SHOW_LOADING = 121;

    [SerializeField] private List<ParticleSystem> _winEffect;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    [SerializeField] private int _debugAddCoinAmount = 100;
#endif

    [SerializeField] private Button nextLevel;
    [SerializeField] private Button backLevel;
    [SerializeField] private TMP_InputField selectlevel;

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
        API.Initialize();
    }

    private void OnEnable()
    {
        if (nextLevel != null)
            nextLevel.onClick.AddListener(NextLevelButton);

        if (backLevel != null)
            backLevel.onClick.AddListener(BackLevelButton);

        if (selectlevel != null)
            selectlevel.onEndEdit.AddListener(SelectLevelEndEdit);
    }

    private void OnDisable()
    {
        if (nextLevel != null)
            nextLevel.onClick.RemoveListener(NextLevelButton);

        if (backLevel != null)
            backLevel.onClick.RemoveListener(BackLevelButton);

        if (selectlevel != null)
            selectlevel.onEndEdit.RemoveListener(SelectLevelEndEdit);
    }

    void Start()
    {
        StartGame(CurrentLevel);
    }

    private void Update()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (Input.GetKeyDown(KeyCode.A))
        {
            InventoryManager.Instance?.AddCoin(_debugAddCoinAmount);
        }
#endif
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
        int nextLevel = completedLevel + 1;
        CurrentLevel = nextLevel;
        _queuedNextLevel = nextLevel;
        MaxLevel = Mathf.Max(MaxLevel, nextLevel);
        SaveData();
        // Grant unlock gift for the newly reached level (config unlockLevel matches StartGame level).
        // If an unlock tutorial will be shown at level start, we defer the gift until the tutorial FX completes.
        int contentLevel = NormalizeLoopLevel(CurrentLevel);
        if (!BoosterUnlockService.ShouldDeferGift(contentLevel))
        {
            BoosterUnlockService.TryGrantUnlockGift(contentLevel);
        }
        var bottom = UIManager.Instance != null ? UIManager.Instance.Get<UIBottomInGame>() : null;
        if (bottom != null) bottom.RefreshBoosterQuantity();
        PlayVfxWin();
        TrackLevelFinished(true, completedLevel);
        SetState(GameStateInGame.Result);
        _nextStartIsAfterWin = true;
    }
    public void SetLose()
    {
        int failedLevel = Mathf.Max(1, _levelInPlay);
        _queuedNextLevel = failedLevel;
        TrackLevelFinished(false, failedLevel);
        SetState(GameStateInGame.Result);

        // if (Board.Instance != null)
        //     Board.Instance.SpawnLoseRainbowShooter();
    }

    private void TrackLevelStarted(int level)
    {
        if (level <= 0) return;
        if (_lastTrackedStartLevel == level) return;
        _lastTrackedStartLevel = level;

        int cash = userData != null ? userData.playerCash : 0;

        TinySauce.OnGameStarted(level);
        TinySauce.TrackCustomEvent("level_start", new Dictionary<string, object> { { "level", level }, { "player_cash", cash } });
    }

    private void TrackLevelFinished(bool win, int level)
    {
        if (level <= 0) return;
        if (_lastTrackedFinishLevel == level && _lastTrackedFinishWasWin == win) return;
        _lastTrackedFinishLevel = level;
        _lastTrackedFinishWasWin = win;

        int cash = userData != null ? userData.playerCash : 0;

        TinySauce.OnGameFinished(win, 0f, level);
        TinySauce.TrackCustomEvent(win ? "level_win" : "level_lose", new Dictionary<string, object> { { "level", level }, { "player_cash", cash } });
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
        _contentLevelInPlay = NormalizeLoopLevel(level);
        _queuedNextLevel = level;
        CurrentLevel = level;
        MaxLevel = Mathf.Max(MaxLevel, level);
        RefreshSelectLevelInput(level);
        SaveData();

        TrackLevelStarted(level);

        if (_playRoutine != null)
        {
            StopCoroutine(_playRoutine);
            _playRoutine = null;
        }

        if (_hideLoadingRoutine != null)
        {
            StopCoroutine(_hideLoadingRoutine);
            _hideLoadingRoutine = null;
        }

        bool isFirstStart = _isFirstSceneStart;
        _pendingAutoHideLoading = _isFirstSceneStart || (_nextStartIsAfterWin && level > CONST_LEVEL_SHOW_LOADING);
        _pendingAutoHideSeconds = isFirstStart ? Mathf.Max(CONST_TIME_HIDE_LOADING, CONST_TIME_HIDE_LOADING_FIRST) : CONST_TIME_HIDE_LOADING;
        _isFirstSceneStart = false;
        _nextStartIsAfterWin = false;
        _playRoutine = StartCoroutine(PlayGame(level));
        SetState(GameStateInGame.Init);

        SpawnUI();
        var tutorialManager = TutorialManager.Instance;
        if (tutorialManager != null) tutorialManager.TryShowTutorial(_contentLevelInPlay);
        BoosterUnlockService.TryShowUnlockTutorialAtLevelStart(_contentLevelInPlay);
        TutorialPopupService.TryShowAtLevelStart(_contentLevelInPlay);
        ClearVfx();
    }

    public void SpawnUI()
    {
        UIManager.Instance.Get<UITopInGame>().Show();
        UIManager.Instance.Get<UIBottomInGame>().Show();
        if (UILoadingInGame.Instance == null) return;

        if (_pendingAutoHideLoading)
            UILoadingInGame.Instance.Show();
        else
            UILoadingInGame.Instance.Hide();
    }

    public void RestartLevel()
    {
        ReplayLevel();
    }

    public void StartNextLevel()
    {
        StartGame(_queuedNextLevel);
    }

    private void NextLevelButton()
    {
        int baseLevel = Mathf.Max(1, _levelInPlay, CurrentLevel);
        StartGame(baseLevel + 1);
    }

    private void BackLevelButton()
    {
        int baseLevel = Mathf.Max(1, _levelInPlay, CurrentLevel);
        StartGame(Mathf.Max(1, baseLevel - 1));
    }

    private void SelectLevelEndEdit(string value)
    {
        if (!int.TryParse(value, out int level))
        {
            RefreshSelectLevelInput(CurrentLevel);
            return;
        }

        StartGame(Mathf.Max(1, level));
    }

    private void RefreshSelectLevelInput(int level)
    {
        if (selectlevel == null) return;
        selectlevel.SetTextWithoutNotify(Mathf.Max(1, level).ToString());
    }

    public void ReplayLevel()
    {
        StartGame(_levelInPlay);
    }

    private IEnumerator PlayGame(int level)
    {
        CurrentLevel = level;
        int contentLevel = NormalizeLoopLevel(level);
        LevelConfig config = null;
        yield return LevelDatabase.LoadLevelAsync(contentLevel, c => config = c);

        if (config == null)
        {
#if UNITY_EDITOR
            config = AssetDatabase.LoadAssetAtPath<LevelConfig>($"Assets/Levels/SO/Level {contentLevel}.asset");
#endif
            if (config == null)
            {
                ResourceRequest soRequest = Resources.LoadAsync<LevelConfig>("Levels/SO/Level " + contentLevel);
                yield return soRequest;
                config = soRequest.asset as LevelConfig;
            }
        }

        if (config == null)
        {
            Debug.LogError($"Failed to load content level {contentLevel} (display level {level}). Missing secure `StreamingAssets/levels.dat` entry and no fallback asset at `Assets/Levels/SO/Level {contentLevel}.asset` (or `Resources/Levels/SO/Level {contentLevel}.asset`).");
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

        if (_pendingAutoHideLoading)
        {
            _pendingAutoHideLoading = false;
            _hideLoadingRoutine = StartCoroutine(HideLoadingAfterDelay(_pendingAutoHideSeconds));
        }

        _playRoutine = null;
    }

    private IEnumerator HideLoadingAfterDelay(float seconds)
    {
        while (CurrentGameStateInGame != GameStateInGame.Playing)
            yield return null;

        yield return new WaitForEndOfFrame();
        yield return new WaitForSeconds(seconds);
        if (UILoadingInGame.Instance != null) UILoadingInGame.Instance.Hide();
        _hideLoadingRoutine = null;
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

    private static int NormalizeLoopLevel(int level)
    {
        if (level <= LoopEndLevel) return level;
        if (level < LoopStartLevel) return level;
        int loopLen = LoopEndLevel - LoopStartLevel + 1;
        int offset = (level - LoopStartLevel) % loopLen;
        return LoopStartLevel + offset;
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
