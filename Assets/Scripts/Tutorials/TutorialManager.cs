using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;


public enum TutorialType
{
	Control = 1,
	Tunnel = 2,
	HiddenShooter = 3,
	FrozenArrow = 4,
	LayeredArrow = 5,
	Elevator = 6,
	RunwayBlocker = 7,
	TiedShooters = 8,
	FrozenTunnel = 9,
	Bomb = 10
	//Ice = 33,
	//   Gate = 6,
	//   LockItem = 15,
	//   LockItemColor = 21,
	//   Screw = 25
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

	private UITutorial _uiTutorial;
	private UITutorialBotter _uiTutorialBotter;
	private UIBottomInGame _uiBottomInGame;
	private GameUI _gameUI;
	private GameManagerInGame _gameManagerInGame;
	private bool _wasAnyPopupVisible;
	private bool _bottomHiddenByPopup;

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

	private void Update()
	{
		if (_gameUI == null) _gameUI = GameUI.Instance;
		if (_gameUI == null) return;
		if (!AllowTutorialPopups())
		{
			if (IsUIElementVisible(_uiTutorial)) _uiTutorial.Hide();
			if (IsUIElementVisible(_uiTutorialBotter)) _uiTutorialBotter.Hide();
			ShowBottomIfHiddenByPopup();
			_wasAnyPopupVisible = false;
			return;
		}

		EnsureUIRefs(_gameUI);

		bool anyPopupVisible = IsAnyTutorialPopupVisible();

		if (anyPopupVisible && IsTapDown())
		{
			HideTutorialPopups();
			ShowBottomIfHiddenByPopup();
			anyPopupVisible = false;
		}

		if (anyPopupVisible != _wasAnyPopupVisible)
		{
			_wasAnyPopupVisible = anyPopupVisible;
			if (anyPopupVisible)
			{
				HideBottomForPopup();
			}
			else
			{
				ShowBottomIfHiddenByPopup();
			}
		}
	}

	public void TryShowTutorial(int levelIndex)
	{
		if (!HasTutorial(levelIndex)) return;
		SetupTutorial(levelIndex);
		ShowTutorial();
	}
	public void ShowTutorialPopup(Sprite icon, string title, string description)
	{
		if (!AllowTutorialPopups()) return;
		if (_gameUI == null) _gameUI = GameUI.Instance;
		if (_gameUI == null) return;
		EnsureUIRefs(_gameUI);
		if (_uiTutorial == null) _uiTutorial = _gameUI.Get<UITutorial>();

		if (_uiTutorial == null) return;
		_uiTutorial.ShowBoosterTutorial(icon, title, description);
		HideBottomForPopup();
		_wasAnyPopupVisible = true;
	}
	public void ShowBoosterUnlockTutorial(int boosterType)
	{
		if (!AllowTutorialPopups()) return;
		if (_gameUI == null) _gameUI = GameUI.Instance;
		if (_gameUI == null) return;
		EnsureUIRefs(_gameUI);
		if (_uiTutorialBotter == null) _uiTutorialBotter = _gameUI.Get<UITutorialBotter>();

		if (_uiTutorialBotter == null) return;
		_uiTutorialBotter.ShowForBooster(boosterType);
		HideBottomForPopup();
		_wasAnyPopupVisible = true;
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

	private void EnsureUIRefs(GameUI gameUI)
	{
		if (_uiBottomInGame == null) _uiBottomInGame = gameUI.Get<UIBottomInGame>();
	}

	private bool AllowTutorialPopups()
	{
		if (_gameManagerInGame == null) _gameManagerInGame = GameManagerInGame.Instance;
		if (_gameManagerInGame == null) return false;
		if (_gameManagerInGame.CurrentGameStateInGame == GameStateInGame.Result) return false;
		return _gameManagerInGame.CurrentLevel > 1;
	}

	private bool IsAnyTutorialPopupVisible()
	{
		return IsUIElementVisible(_uiTutorial) || IsUIElementVisible(_uiTutorialBotter);
	}

	private static bool IsUIElementVisible(UIElement element)
	{
		if (element == null) return false;
		if (element.holder != null) return element.holder.activeSelf;
		return element.gameObject.activeSelf;
	}

	private void HideBottomForPopup()
	{
		if (_uiBottomInGame == null) return;
		bool wasVisible = IsUIElementVisible(_uiBottomInGame);
		if (!wasVisible)
		{
			_bottomHiddenByPopup = false;
			return;
		}
		_uiBottomInGame.Hide();
		_bottomHiddenByPopup = true;
	}

	private void ShowBottomIfHiddenByPopup()
	{
		if (!_bottomHiddenByPopup) return;
		_bottomHiddenByPopup = false;
		if (_uiBottomInGame == null) return;
		_uiBottomInGame.Show();
	}

	private void HideTutorialPopups()
	{
		if (IsUIElementVisible(_uiTutorial)) _uiTutorial.Hide();
		if (IsUIElementVisible(_uiTutorialBotter)) _uiTutorialBotter.Hide();
	}

	private static bool IsTapDown()
	{
		return Input.GetMouseButtonDown(0);
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
