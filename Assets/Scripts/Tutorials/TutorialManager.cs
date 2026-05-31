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
	Bomb = 10,
	Lock = 11,
	LineDoor = 12

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
	[SerializeField] private bool _debugLogs;
	[ReadOnly] public bool TutorialControlWaitTapLine;
	private Dictionary<TutorialType, TutorialBase> _tutorialMap;
	public TutorialBase CurrentTutorial { get; private set; }
	public bool IsInTutorial { get; private set; }

	private UITutorial _uiTutorial;
	private UITutorialBotter _uiTutorialBotter;
	private UIBottomInGame _uiBottomInGame;
	private UIManager _gameUI;
	private GameManagerInGame _gameManagerInGame;
	private bool _wasAnyPopupVisible;
	private bool _bottomHiddenByPopup;
	private int _pendingBoosterDropType;
	private int _pendingBoosterDropCount;
	private int _pendingBoosterGiftLevel;

	private int _currentLevel;

	private void Log(string message)
	{
		if (!_debugLogs) return;
		Debug.Log($"[TutorialManager] {message}");
	}

	protected override void Awake()
	{
		base.Awake();
		_tutorialMap = new Dictionary<TutorialType, TutorialBase>();
		if (_tutorialEntries == null)
		{
			Log("Awake: _tutorialEntries is null");
			return;
		}
		Log($"Awake: entries={_tutorialEntries.Count}");

		for (int i = 0; i < _tutorialEntries.Count; i++)
		{
			var entry = _tutorialEntries[i];
			if (entry == null || entry.Tutorial == null) continue;
			if (_tutorialMap.ContainsKey(entry.Type)) continue;
			_tutorialMap.Add(entry.Type, entry.Tutorial);
		}
		Log($"Awake: mapCount={_tutorialMap.Count}");
	}

	private void Update()
	{
		if (_gameUI == null) _gameUI = UIManager.Instance;
		if (_gameUI == null) return;
		if (!AllowTutorialPopups())
		{
			if (IsUIVisible(_uiTutorial)) _gameUI.HideUI<UITutorial>(UIAnimType.None);
			if (IsUIVisible(_uiTutorialBotter)) _gameUI.HideUI<UITutorialBotter>(UIAnimType.None);
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
			if (_uiBottomInGame != null && _pendingBoosterDropType > 0 && _pendingBoosterDropCount > 0)
			{
				int giftLevel = _pendingBoosterGiftLevel;
				_uiBottomInGame.PlayTutorialDropToBoosterButton(_pendingBoosterDropType, _pendingBoosterDropCount, () =>
				{
					if (giftLevel > 0)
					{
						BoosterUnlockService.TryGrantUnlockGift(giftLevel);
					}
					if (_uiBottomInGame != null)
					{
						_uiBottomInGame.RefreshBoosterQuantity();
					}
				});
			}
			_pendingBoosterDropType = 0;
			_pendingBoosterDropCount = 0;
			_pendingBoosterGiftLevel = 0;
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
		bool hasTutorial = HasTutorial(levelIndex);
		Log($"TryShowTutorial: level={levelIndex} hasTutorial={hasTutorial}");
		if (!hasTutorial) return;
		SetupTutorial(levelIndex);
		ShowTutorial();
	}
	public void ShowTutorialPopup(Sprite icon, string title, string description)
	{
		bool allow = AllowTutorialPopups();
		Log($"ShowTutorialPopup: allow={allow} title='{title}'");
		if (!allow) return;
		if (_gameUI == null) _gameUI = UIManager.Instance;
		if (_gameUI == null) return;
		EnsureUIRefs(_gameUI);
		if (_uiTutorial == null) _uiTutorial = _gameUI.Get<UITutorial>();

		if (_uiTutorial == null)
		{
			Log("ShowTutorialPopup: _uiTutorial null");
			return;
		}
		_uiTutorial.ShowBoosterTutorial(icon, title, description);
		HideBottomForPopup();
		_wasAnyPopupVisible = true;
	}
	public void ShowBoosterUnlockTutorial(int boosterType)
	{
		bool allow = AllowTutorialPopups();
		Log($"ShowBoosterUnlockTutorial: allow={allow} boosterType={boosterType}");
		if (!allow) return;
		if (_gameUI == null) _gameUI = UIManager.Instance;
		if (_gameUI == null) return;
		EnsureUIRefs(_gameUI);
		if (_uiTutorialBotter == null) _uiTutorialBotter = _gameUI.Get<UITutorialBotter>();

		if (_uiTutorialBotter == null)
		{
			Log("ShowBoosterUnlockTutorial: _uiTutorialBotter null");
			return;
		}
		_uiTutorialBotter.ShowForBooster(boosterType);
		_pendingBoosterDropType = boosterType;
		_pendingBoosterDropCount = 1;
		_pendingBoosterGiftLevel = _gameManagerInGame != null ? _gameManagerInGame.CurrentLevel : 0;
		var cfg = BoosterUnlockService.Config;
		if (cfg != null)
		{
			var entry = cfg.GetEntry(boosterType);
			if (entry != null)
			{
				_pendingBoosterDropCount = Mathf.Max(1, entry.giftAmount);
			}
		}
		HideBottomForPopup();
		_wasAnyPopupVisible = true;
	}

	public void SetupTutorial(int currentLevel)
	{
		_currentLevel = Mathf.Max(1, currentLevel);
		Log($"SetupTutorial: level={_currentLevel}");

		if (!HasTutorial(_currentLevel))
		{
			Log($"SetupTutorial: HasTutorial=false for level={_currentLevel}");
			if (CurrentTutorial != null) CurrentTutorial.Hide();
			CurrentTutorial = null;
			IsInTutorial = false;
			return;
		}

		var type = (TutorialType)_currentLevel;
		Log($"SetupTutorial: type={type}");
		if (_tutorialMap != null && _tutorialMap.TryGetValue(type, out var tutorial) && tutorial != null)
		{
			CurrentTutorial = tutorial;
			CurrentTutorial.Setup();
			IsInTutorial = true;
			Log($"SetupTutorial: mapped tutorial found for type={type} name={tutorial.name}");
			return;
		}
		Debug.LogWarning($"[TutorialManager] Tutorial not found for type: {type} level={_currentLevel}");
		CurrentTutorial = null;
		IsInTutorial = false;
	}

	private void EnsureUIRefs(UIManager gameUI)
	{
		if (_uiBottomInGame == null) _uiBottomInGame = gameUI.Get<UIBottomInGame>();
	}

	private bool AllowTutorialPopups()
	{
		if (_gameManagerInGame == null) _gameManagerInGame = GameManagerInGame.Instance;
		if (_gameManagerInGame == null)
		{
			Log("AllowTutorialPopups: GameManagerInGame null -> false");
			return false;
		}
		if (_gameManagerInGame.CurrentGameStateInGame == GameStateInGame.Result)
		{
			Log($"AllowTutorialPopups: state=Result level={_gameManagerInGame.CurrentLevel} -> false");
			return false;
		}
		bool allow = _gameManagerInGame.CurrentLevel > 1;
		Log($"AllowTutorialPopups: level={_gameManagerInGame.CurrentLevel} allow={allow}");
		return allow;
	}

	private bool IsAnyTutorialPopupVisible()
	{
		return IsUIVisible(_uiTutorial) || IsUIVisible(_uiTutorialBotter);
	}

	private static bool IsUIVisible(BaseUIElement element)
	{
		if (element == null) return false;
		if (element.holder != null) return element.holder.activeSelf;
		return element.gameObject.activeSelf;
	}

	private void HideBottomForPopup()
	{
		if (_uiBottomInGame == null) return;
		bool wasVisible = IsUIVisible(_uiBottomInGame);
		if (!wasVisible)
		{
			_bottomHiddenByPopup = false;
			return;
		}
		_gameUI?.HideUI<UIBottomInGame>(UIAnimType.None);
		_bottomHiddenByPopup = true;
	}

	private void ShowBottomIfHiddenByPopup()
	{
		if (!_bottomHiddenByPopup) return;
		_bottomHiddenByPopup = false;
		if (_uiBottomInGame == null) return;
		_gameUI?.ShowUI<UIBottomInGame>(null, true, false, UIAnimType.None);
	}

	private void HideTutorialPopups()
	{
		if (IsUIVisible(_uiTutorial)) _gameUI?.HideUI<UITutorial>(UIAnimType.None);
		if (IsUIVisible(_uiTutorialBotter)) _gameUI?.HideUI<UITutorialBotter>(UIAnimType.None);
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
		if (!System.Enum.IsDefined(typeof(TutorialType), level))
		{
			Log($"HasTutorial: level={level} not defined in TutorialType -> false");
			return false;
		}

		var type = (TutorialType)level;
		string key = type.ToString();
		int pref = PlayerPrefs.GetInt(key, 0);
		bool result = pref == 0;
		Log($"HasTutorial: level={level} type={type} key={key} pref={pref} -> {result}");
		return result;
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
