using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;


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
    [SerializeField] private List<TutorialEntry> _tutorialEntries;
    [ReadOnly] public bool TutorialControlWaitTapLine;
    private Dictionary<TutorialType, TutorialBase> _tutorialMap;
    public TutorialBase CurrentTutorial { get; private set; }
    public bool IsInTutorial { get; private set; }

    private int _currentLevel;

    protected override void Awake()
    {
        base.Awake();
        _tutorialMap = new Dictionary<TutorialType, TutorialBase>();
        if (_tutorialEntries == null) return;

        for (int i = 0; i < _tutorialEntries.Count; i++)
        {
            var entry = _tutorialEntries[i];
            if (entry == null || entry.Tutorial == null) continue;
            if (_tutorialMap.ContainsKey(entry.Type)) continue;
            _tutorialMap.Add(entry.Type, entry.Tutorial);
        }
    }

    public void TryShowTutorial(int levelIndex)
    {
        if (!HasTutorial(levelIndex)) return;
        SetupTutorial(levelIndex);
        ShowTutorial();
    }

    public void SetupTutorial(int currentLevel)
    {
        _currentLevel = Mathf.Max(1, currentLevel);

        if (!HasTutorial(_currentLevel))
        {
            if (CurrentTutorial != null) CurrentTutorial.Hide();
            CurrentTutorial = null;
            IsInTutorial = false;
            return;
        }

        var type = (TutorialType)_currentLevel;
        if (_tutorialMap != null && _tutorialMap.TryGetValue(type, out var tutorial) && tutorial != null)
        {
            CurrentTutorial = tutorial;
            CurrentTutorial.Setup();
            IsInTutorial = true;
            return;
        }
        Debug.LogWarning($"[TutorialManager] Tutorial not found for type: {type} level={_currentLevel}");
        CurrentTutorial = null;
        IsInTutorial = false;
    }

    public void ShowTutorial()
    {
        if (CurrentTutorial == null)
        {
            IsInTutorial = false;
            return;
        }

        IsInTutorial = true;
        CurrentTutorial.Show();
        CurrentTutorial.GoNextStep();
    }

    public bool HasTutorial(int level)
    {
        level = Mathf.Max(1, level);
        if (!System.Enum.IsDefined(typeof(TutorialType), level)) return false;

        var type = (TutorialType)level;
        string key = type.ToString();
        return PlayerPrefs.GetInt(key, 0) == 0;
    }

    public void TutorialFinish()
    {
        if (CurrentTutorial != null) CurrentTutorial.Hide();

        var type = (TutorialType)Mathf.Max(1, _currentLevel);
        PlayerPrefs.SetInt(type.ToString(), 1);

        CurrentTutorial = null;
        IsInTutorial = false;
    }
    public void HandleNextStep()
    {
        if (_tutorialMap.TryGetValue(CurrentTutorial.Type, out var tutorial))
        {
            tutorial.GoNextStep();
        }
        else
        {
            Debug.LogWarning($"No current tutorial to handle next step for type: {CurrentTutorial.Type}");
        }
    }

}
