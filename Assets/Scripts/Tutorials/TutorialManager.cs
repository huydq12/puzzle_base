using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;


public enum TutorialType
{
    Control = 1,
    Ice = 33,
    Gate = 6,
    LockItem = 15,
    LockItemColor = 21,
    Screw = 25
}
[System.Serializable]
public class TutorialEntry
{
    public TutorialType Type;
    public TutorialBase Tutorial;
}
public class TutorialManager : Singleton<TutorialManager>
{
    private Dictionary<TutorialType, TutorialBase> _tutorialMap;
    [SerializeField, TableList] private List<TutorialEntry> _tutorialEntries;
    [ReadOnly] public TutorialBase CurrentTutorial;
    [ReadOnly] public bool TutorialControlWaitMoveButton;

    public TutorialType CurrentTutorialType => (TutorialType)_currentLevel;
    public bool IsInTutorial { get; private set; }

    private int _currentLevel;
    private new void Awake()
    {
        base.Awake();
        _tutorialMap = new Dictionary<TutorialType, TutorialBase>();
        foreach (var entry in _tutorialEntries)
        {
            if (!_tutorialMap.ContainsKey(entry.Type))
            {
                _tutorialMap.Add(entry.Type, entry.Tutorial);
            }
        }
    }
    public void SetupTutorial(int currentLevel)
    {
        _currentLevel = currentLevel;

        if (!HasTutorial(currentLevel))
        {
            if (CurrentTutorial != null)
            {
                CurrentTutorial.Hide();
            }
            CurrentTutorial = null;
            IsInTutorial = false;
            return;
        }

        if (_tutorialMap.TryGetValue((TutorialType)currentLevel, out var tutorial))
        {
            CurrentTutorial = tutorial;
            CurrentTutorial.Setup();
            IsInTutorial = true;
        }
        else
        {
            Debug.LogWarning($"Tutorial not found in map: {(TutorialType)currentLevel}");
            CurrentTutorial = null;
            IsInTutorial = false;
        }
    }
    public void TryShowTutorial(int levelIndex)
    {
        if (!HasTutorial(levelIndex)) return;
        SetupTutorial(levelIndex);
        ShowTutorial();
    }

    public void ShowTutorial()
    {
        if (CurrentTutorial != null)
        {
            IsInTutorial = true;
            CurrentTutorial.GoNextStep();
        }
        else
        {
            IsInTutorial = false;
        }
    }

    public bool HasTutorial(int currentLevel)
    {
        if (!System.Enum.IsDefined(typeof(TutorialType), currentLevel))
            return false;

        var type = (TutorialType)currentLevel;
        string key = type.ToString();

        return PlayerPrefs.GetInt(key, 0) == 0;
    }

    public void TutorialFinish()
    {
        if (!IsInTutorial || CurrentTutorial == null)
            return;

        var type = (TutorialType)_currentLevel;
        PlayerPrefs.SetInt(type.ToString(), 1);

        CurrentTutorial = null;
        IsInTutorial = false;
    }

    public void HandleNextStep()
    {
        if (_tutorialMap.TryGetValue(CurrentTutorialType, out var tutorial))
        {
            tutorial.GoNextStep();
        }
        else
        {
            Debug.LogWarning($"No current tutorial to handle next step for type: {CurrentTutorialType}");
        }
    }
}

