using System;
using System.Collections;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.SceneManagement;
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

    [SerializeField] private MeshRenderer _bg;
    [SerializeField] private List<Material> _materials;

    public UserData userData { get; private set; }

    private new void Awake()
    {
        base.Awake();
        Game.Launch();

        userData = Game.Data.Load<UserData>();

        MaxLevel = Mathf.Max(1, userData != null ? userData.maxLevel : 1);
        CurrentLevel = Mathf.Max(1, userData != null ? userData.currentLevel : 1);
        if (CurrentLevel > MaxLevel) CurrentLevel = MaxLevel;

        int startLevel = CurrentLevel;
        startLevel = Mathf.Max(1, startLevel);
        StartGame(startLevel);
        SetBackgroundMaterial();
    }
    
    private void SetBackgroundMaterial()
    {
        if (_bg == null || _materials == null || _materials.Count == 0)
            return;
            
        // Get material index based on current level (0-based index)
        int materialIndex = (CurrentLevel - 1) % _materials.Count;
        _bg.material = _materials[materialIndex];
    }
    public void SetWin()
    {
        CurrentLevel++;
        if (CurrentLevel > MaxLevel)
        {
            MaxLevel = CurrentLevel;
        }
        if (userData != null)
        {
            userData.currentLevel = CurrentLevel;
            userData.maxLevel = MaxLevel;
            userData.Save();
        }
        SetBackgroundMaterial();
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
                    if (CurrentLevel > 1)
                    {
                        GameUI.Instance.Get<UIBottomInGame>().Show();
                    }
                    GameUI.Instance.Get<UITopInGame>().Show();
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
        if (userData != null)
        {
            userData.currentLevel = Mathf.Max(1, level);
            userData.maxLevel = Mathf.Max(1, MaxLevel);
            userData.Save();
        }
        StartCoroutine(PlayGame(level));
    }
    private IEnumerator PlayGame(int level)
    {
        CurrentLevel = level;
        if (userData != null)
        {
            userData.currentLevel = CurrentLevel;
        }
        yield return null;
    }
#if UNITY_EDITOR
    new void OnApplicationQuit()
    {
        base.OnApplicationQuit();
        if (userData != null)
        {
            userData.maxLevel = Mathf.Max(1, MaxLevel);
            userData.currentLevel = Mathf.Max(1, CurrentLevel);
            userData.Save();
        }
    }
#else
    void OnApplicationPause(bool pause)
    {
        if (pause)
        {
            if (userData != null)
            {
                userData.maxLevel = Mathf.Max(1, MaxLevel);
                userData.currentLevel = Mathf.Max(1, CurrentLevel);
                userData.Save();
            }
        }
    }
#endif
}
