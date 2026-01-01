using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Random = UnityEngine.Random;

public class GameManager : Singleton<GameManager>
{
    public enum GameState
    {
        None,
        Home,
        InGame,
        Pause,
        Complete,
        GameOver
    }

    public event Action<GameState, GameState> OnGameStateChanged;

    public GameState currentState;
    public GameState CurrentState
    {
        get => currentState;
        private set
        {
            if (currentState != value)
            {
                GameState previousState = currentState;
                currentState = value;
                OnGameStateChanged?.Invoke(previousState, currentState);
                Debug.Log($"GameState changed: {previousState} -> {currentState}");
            }
        }
    }

    public UserData userData { get; private set; }

    public bool IsPlaying => CurrentState == GameState.InGame;
    public bool IsPaused => CurrentState == GameState.Pause;
    public bool IsGameOver => CurrentState == GameState.GameOver;


    public void SetGameState(GameState newState)
    {
        if (!IsValidStateTransition(CurrentState, newState))
        {
            Debug.LogWarning($"Invalid state transition: {CurrentState} -> {newState}");
            return;
        }
        CurrentState = newState;
    }

    private bool IsValidStateTransition(GameState from, GameState to)
    {
        if (from == to) return false;

        switch (from)
        {
            case GameState.Home:
                return to == GameState.InGame;
            
            case GameState.InGame:
                return to == GameState.Pause || to == GameState.Complete || to == GameState.GameOver || to == GameState.Home;
            
            case GameState.Pause:
                return to == GameState.InGame || to == GameState.Home;
            
            case GameState.GameOver:
                return to == GameState.Home || to == GameState.Complete;
            
            default:
                return true;
        }
    }

    protected override void Awake()
    {
        base.Awake();
        Game.Launch();
        currentState = GameState.None;
    }

    void Start()
    {
        userData = Game.Data.Load<UserData>();
        if (!userData.isDefaultData)
        {
            userData.SetDefaultData();
            userData.Save();
            Debug.Log("Set default data");
        }

        HeatManager.Instance.Initialize(userData);

        SetGameState(GameState.Home);
        GameUI.Instance.Get<UIHome>().Show();
        GameUI.Instance.Get<UIShop>().Hide();
        GameUI.Instance.Get<UIPopupRank>().Hide();
        GameUI.Instance.Get<UIMenuBar>().Show();

    }

    public void StartGame()
    {
        if (!HeatManager.Instance.CanPlay())
        {
            GameUI.Instance.Get<UIPopupNoHeat>().Show();
            return;
        }
        
        SetGameState(GameState.InGame);
    }

    public void PauseGame()
    {
        if (IsPlaying)
        {
            SetGameState(GameState.Pause);
        }
    }

    public void ResumeGame()
    {
        if (IsPaused)
        {
            SetGameState(GameState.InGame);
        }
    }

    public void EndGame()
    {
        if (IsPlaying)
        {
            HeatManager.Instance.ConsumeHeat();
            SetGameState(GameState.GameOver);
            UpdateValueData();
        }
    }

    public void ReturnToHome()
    {
        SetGameState(GameState.Home);
    }

    public void UpdateValueData()
    {
        var UI_Home = GameUI.Instance.Get<UIHome>();
        if (UI_Home != null)
        {
            UI_Home.UpdateCash(userData.playerCash);
            UI_Home.UpdateHeatDisplay();
        }
    }

    // void Update()
    // {
    //     if (Input.GetKey(KeyCode.A))
    //     {
    //         GameUI.Instance.Get<UIDailyReward>().Show();
    //     }

    //     if (Input.GetKey(KeyCode.S))
    //     {
    //         GameUI.Instance.Get<UIWin>().Show();
    //     }

    //     if (Input.GetKey(KeyCode.D))
    //     {
    //         GameUI.Instance.Get<UIProfile>().Show();
    //     }

    //     if (Input.GetKey(KeyCode.F))
    //     {
    //         GameUI.Instance.Get<UISettingInGame>().Show();
    //     }
        
    // }

}
