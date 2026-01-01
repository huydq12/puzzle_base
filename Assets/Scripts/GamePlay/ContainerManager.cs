using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

public class ContainerManager : Singleton<ContainerManager>
{
    [SerializeField] private Container _container1;
    [SerializeField] private Container _container2;
    [SerializeField] private ParticleSystem _collectEffect;

    [SerializeField] private List<ParticleSystem> _winEffect;

    [ReadOnly] public List<ObjectColor> Containers;
    public Container CurrentContainer { get; private set; }
    public bool IsContainerReady { get; private set; }

    public int _currentIndex;
    private Camera _camera;
    private bool _pendingTryCollect;
    private bool _isTryCollectRunning;

    private const float MOVE_DURATION = 0.25f;
    private const float SNAP_DURATION = 0.15f;
    private const float OFFSET = 2.5f;

    new void Awake()
    {
        base.Awake();
        _camera = Camera.main;
    }

#if UNITY_EDITOR
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            UnityEditor.EditorApplication.isPaused = true;
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            DOTween.KillAll();
            GameManagerInGame.Instance.StartGame();
        }
    }
#endif

    private void LateUpdate()
    {
        if (!_pendingTryCollect)
        {
            return;
        }
        if (GameManagerInGame.Instance.CurrentGameStateInGame == GameStateInGame.Result)
        {
            _pendingTryCollect = false;
            return;
        }
        if (_isTryCollectRunning)
        {
            return;
        }
        if (!IsContainerReady)
        {
            return;
        }
        if (GameController.Instance == null)
        {
            return;
        }
        if (GameController.Instance.IsCollecting)
        {
            return;
        }
        if (GameController.Instance.ActiveMergeCount != 0)
        {
            return;
        }

        _pendingTryCollect = false;
        StartCoroutine(TryCollectButtonsWrapper());
    }
    public void Clear()
    {
        StopAllCoroutines();

        DOTween.Kill(_container1);

        DOTween.Kill(_container2);

        _container1?.Clear();
        _container2?.Clear();

        Containers?.Clear();
        CurrentContainer = null;
        IsContainerReady = false;
        _currentIndex = 0;
        _pendingTryCollect = false;
        _isTryCollectRunning = false;

        foreach (var effect in _winEffect)
        {
            effect.gameObject.SetActive(false);
        }
    }

    public ContainerManagerSnapshot CreateSnapshot()
    {
        ContainerManagerSnapshot snapshot = new ContainerManagerSnapshot();

        if (Containers != null)
        {
            snapshot.Containers = new List<ObjectColor>(Containers);
        }

        snapshot.CurrentIndex = _currentIndex;
        snapshot.IsContainerReady = IsContainerReady;
        snapshot.IsCurrentContainer1 = CurrentContainer == _container1;

        if (_container1 != null)
        {
            snapshot.Container1 = _container1.CreateSnapshot();
        }

        if (_container2 != null)
        {
            snapshot.Container2 = _container2.CreateSnapshot();
        }

        return snapshot;
    }

    public void ApplySnapshot(ContainerManagerSnapshot snapshot)
    {
        if (snapshot == null)
        {
            return;
        }

        StopAllCoroutines();
        DOTween.Kill(_container1);
        DOTween.Kill(_container2);

        if (snapshot.Containers != null)
        {
            Containers = new List<ObjectColor>(snapshot.Containers);
        }
        else
        {
            Containers = new List<ObjectColor>();
        }

        UpdateContainerVisibility();

        _currentIndex = snapshot.CurrentIndex;
        IsContainerReady = snapshot.IsContainerReady;
        _pendingTryCollect = false;
        _isTryCollectRunning = false;

        if (_container1 != null && snapshot.Container1 != null)
        {
            _container1.ApplySnapshot(snapshot.Container1);
        }

        if (_container2 != null && snapshot.Container2 != null)
        {
            _container2.ApplySnapshot(snapshot.Container2);
        }

        bool canUseLeftContainer = Containers != null && Containers.Count > 1;
        CurrentContainer = snapshot.IsCurrentContainer1 && canUseLeftContainer ? _container1 : _container2;
    }


    #region Public Methods
    public void SetupContainers(List<ObjectColor> containers)
    {
        Containers = containers != null ? new List<ObjectColor>(containers) : new List<ObjectColor>();
        _currentIndex = 0;

        UpdateContainerVisibility();

        SetupCenterContainer();
        SetupLeftContainer();

        // Container ở giữa đã sẵn sàng ngay từ đầu
        OnContainerReady();
    }

    public bool CanSwapCurrentAndNextContainers()
    {
        if (!IsContainerReady) return false;
        if (Containers == null || Containers.Count <= 1) return false;
        if (_currentIndex < 0 || _currentIndex >= Containers.Count - 1) return false;
        if (_container1 == null || _container2 == null) return false;
        if (!_container1.gameObject.activeInHierarchy) return false;
        if (CurrentContainer == null) return false;
        return true;
    }

    public void SwapCurrentAndNextContainers()
    {
        if (!CanSwapCurrentAndNextContainers())
        {
            return;
        }

        IsContainerReady = false;
        _pendingTryCollect = false;

        var current = CurrentContainer;
        var other = GetOtherContainer(current);
        if (other == null)
        {
            IsContainerReady = true;
            return;
        }

        float xA = current.transform.position.x;
        float xB = other.transform.position.x;

        DOTween.Kill(_container1.transform);
        DOTween.Kill(_container2.transform);

        Sequence sq = DOTween.Sequence();
        sq.Join(current.CloseContainer(true));
        sq.Join(other.CloseContainer(true));
        sq.Join(current.transform.DOMoveX(xB, MOVE_DURATION).SetEase(Ease.InOutQuad));
        sq.Join(other.transform.DOMoveX(xA, MOVE_DURATION).SetEase(Ease.InOutQuad));
        sq.Join(current.transform.DOPunchScale(Vector3.one * 0.12f, MOVE_DURATION, 6, 0.85f));
        sq.Join(other.transform.DOPunchScale(Vector3.one * 0.12f, MOVE_DURATION, 6, 0.85f));
        sq.AppendCallback(() =>
        {
            var tmp = Containers[_currentIndex];
            Containers[_currentIndex] = Containers[_currentIndex + 1];
            Containers[_currentIndex + 1] = tmp;
            CurrentContainer = other;
        });
        sq.Append(other.OpenContainer(true));
        sq.OnComplete(OnContainerReady);
    }

    public void CollectCurrentContainer()
    {
        if (!CanCollect())
        {
            return;
        }
        GameController.Instance.ClearSnapshots();
        ProcessCollection();
    }

    public void RequestTryCollect()
    {
        if (GameManagerInGame.Instance.CurrentGameStateInGame == GameStateInGame.Result)
        {
            return;
        }
        if (_isTryCollectRunning)
        {
            _pendingTryCollect = true;
            return;
        }
        if (!IsContainerReady)
        {
            _pendingTryCollect = true;
            return;
        }
        if (GameController.Instance.IsCollecting)
        {
            _pendingTryCollect = true;
            return;
        }
        if (GameController.Instance.ActiveMergeCount == 0)
        {
            StartCoroutine(TryCollectButtonsWrapper());
        }
        else
        {
            _pendingTryCollect = true;
        }
    }
    #endregion

    private void UpdateContainerVisibility()
    {
        bool hasLeftContainer = Containers != null && Containers.Count > 1;
        if (_container1 != null)
        {
            if (!hasLeftContainer)
            {
                _container1.Clear();
            }
            _container1.gameObject.SetActive(hasLeftContainer);
        }
        if (_container2 != null)
        {
            _container2.gameObject.SetActive(true);
        }
    }

    #region Private Setup Methods
    private void SetupCenterContainer()
    {
        CurrentContainer = _container2;
        _container2.transform.position = _container2.transform.position.With(x: 0);
        if (Containers.Count > 0)
        {
            _container2.SetColor(Containers[0]);
        }
        CurrentContainer.OpenContainer(false);
    }

    private void SetupLeftContainer()
    {
        if (Containers.Count <= 1) return;

        float leftX = GetScreenEdgeX(isLeft: true);
        _container1.transform.position = _container1.transform.position.With(x: leftX);
        _container1.SetColor(Containers[1]);
    }
    #endregion

    private bool CanCollect()
    {
        return _currentIndex < Containers.Count;
    }

    private void ProcessCollection()
    {
        Container currentContainer = CurrentContainer;
        Container nextContainer = GetOtherContainer(currentContainer);
        bool isLastContainer = IsLastContainer();

        AnimateContainerTransition(currentContainer, nextContainer, isLastContainer);

        if (!isLastContainer)
        {
            CurrentContainer = nextContainer;
        }

        _currentIndex++;
    }

    private bool IsLastContainer()
    {
        return _currentIndex == Containers.Count - 1;
    }

    private void AnimateContainerTransition(Container currentContainer, Container nextContainer, bool isLastContainer)
    {
        if (isLastContainer)
        {
            GameManagerInGame.Instance.SetWin();
        }

        float rightEdgeX = GetRightEdgeX();
        Sequence sq = DOTween.Sequence();
        sq.Append(currentContainer.CloseContainer(true));
        sq.AppendCallback(delegate
        {
            _collectEffect.Play();
        });
        sq.Append(currentContainer.transform.DOPunchScale(new Vector3(0.5f, 0, 0), 0.35f, 0, 0));
        sq.AppendCallback(delegate
        {
            AudioManager.Instance.PlaySFX(SFXType.ContainerMove);
        });
        sq.Append(currentContainer.transform.DOMoveX(rightEdgeX, MOVE_DURATION).SetEase(Ease.InBack));
        sq.OnComplete(() => OnCurrentContainerExited(currentContainer, nextContainer, isLastContainer));
    }

    private void OnCurrentContainerExited(Container exitedContainer, Container nextContainer, bool isLastContainer)
    {
        exitedContainer.Clear();

        if (isLastContainer)
        {
            // UIManager.Instance.ShowWinScreen();
            // UIManager.Instance.CloseSettingsPopup();

            foreach (var effect in _winEffect)
            {
                effect.gameObject.SetActive(true);
                effect.Play();
            }
            // Debug.Log("[ContainerManager] Win sequence triggered");
            Time.timeScale = 1f;
            DOVirtual.DelayedCall(1f, () =>
            {
                GameUI.Instance.Get<UIWin>().Show();
            });
        }
        else
        {
            PrepareNextContainer(exitedContainer, nextContainer);
        }
    }



    private void PrepareNextContainer(Container exitedContainer, Container nextContainer)
    {
        RepositionExitedContainer(exitedContainer);
        MoveContainerToCenter(nextContainer);
    }

    private void RepositionExitedContainer(Container exitedContainer)
    {
        float leftEdgeX = GetLeftEdgeX();

        exitedContainer.transform.position = exitedContainer.transform.position.With(x: leftEdgeX);
        exitedContainer.CloseContainer(false);

        SetupNextColor(exitedContainer);
        AnimateContainerSnap(exitedContainer, leftEdgeX);
    }

    private void SetupNextColor(Container container)
    {
        int nextColorIndex = _currentIndex + 1;

        if (nextColorIndex < Containers.Count)
        {
            container.SetColor(Containers[nextColorIndex]);
        }
    }

    private void AnimateContainerSnap(Container container, float fromX)
    {
        int nextColorIndex = _currentIndex + 1;

        if (nextColorIndex < Containers.Count)
        {
            container.transform.DOMoveX(fromX + OFFSET, SNAP_DURATION)
                .SetEase(Ease.OutBack);
        }
    }

    private void MoveContainerToCenter(Container container)
    {
        Sequence sq = DOTween.Sequence();
        sq.Append(container.transform.DOMoveX(0, MOVE_DURATION).SetEase(Ease.OutBack));
        sq.Append(container.OpenContainer(true));
        sq.OnComplete(delegate
        {
            OnContainerReady();
        });
    }
    [Button]
    private void OnContainerReady()
    {
        IsContainerReady = true;
        if (GameController.Instance.IsCollecting || _isTryCollectRunning)
        {
            _pendingTryCollect = true;
            return;
        }
        if (GameController.Instance.ActiveMergeCount == 0)
        {
            StartCoroutine(TryCollectButtonsWrapper());
        }
        else
        {
            _pendingTryCollect = true;
        }
    }

    private IEnumerator TryCollectButtonsWrapper()
    {
        if (GameManagerInGame.Instance.CurrentGameStateInGame == GameStateInGame.Result)
        {
            _pendingTryCollect = false;
            yield break;
        }
        _isTryCollectRunning = true;
        try
        {
            while (true)
            {
                if (GameManagerInGame.Instance.CurrentGameStateInGame == GameStateInGame.Result)
                {
                    _pendingTryCollect = false;
                    yield break;
                }

                GameController.Instance.IsCollecting = true;
                yield return TryCollectButtons();
                yield return Tray.Instance.TryCollectButtons();
                GameController.Instance.IsCollecting = false;

                if (_pendingTryCollect && IsContainerReady && GameManagerInGame.Instance.CurrentGameStateInGame != GameStateInGame.Result && GameController.Instance.ActiveMergeCount == 0)
                {
                    _pendingTryCollect = false;
                    continue;
                }

                break;
            }

            yield return null;
            if (GameController.Instance.IsAnimating)
            {
                _pendingTryCollect = true;
                yield break;
            }
            GameController.Instance.CheckLoseByNoValidMove();
        }
        finally
        {
            if (GameController.Instance != null)
            {
                GameController.Instance.IsCollecting = false;
            }
            _isTryCollectRunning = false;
        }
    }

    public IEnumerator TryCollectButtons()
    {
        if (GameManagerInGame.Instance.CurrentGameStateInGame == GameStateInGame.Result)
        {
            yield break;
        }
        if (!IsContainerReady)
        {
            _pendingTryCollect = true;
            yield break;
        }
        if (CurrentContainer == null || CurrentContainer.EmptySlotCount < Defines.COLLECT_COUNT)
        {
            _pendingTryCollect = true;
            yield break;
        }
        var matchingSlotsInTray = new List<Slot>();
        if (Tray.Instance != null && Tray.Instance.Slots != null)
        {
            int totalSlot = Tray.Instance.TotalSlot;
            var slots = Tray.Instance.Slots;
            for (int i = 0; i < totalSlot && i < slots.Length; i++)
            {
                var slot = slots[i];
                if (slot != null && slot.IsOccupied && slot.ButtonInSlot != null && slot.ButtonInSlot.Color == CurrentContainer.Color)
                {
                    matchingSlotsInTray.Add(slot);
                }
            }
        }
        // collect tray to container
        if (matchingSlotsInTray.Count >= Defines.COLLECT_COUNT)
        {
            IsContainerReady = false;
            yield return CollectFromTray(matchingSlotsInTray).WaitForCompletion();
        }
        //collect board to container
        else
        {
            GridCell cellMove = null;
            foreach (var cell in Board.Instance.Cells)
            {
                if (cell == null || !cell.IsOccupied || cell.Stack.TopButton.Color != CurrentContainer.Color) continue;
                if (Board.Instance != null && Board.Instance.IsCellLocked(cell)) continue;
                if (Board.Instance != null && Board.Instance.IsCellFrozen(cell)) continue;
                if (Board.Instance != null && Board.Instance.IsCellScrewed(cell)) continue;
                if (cell.Stack.TopColors().Count >= Defines.COLLECT_COUNT)
                {
                    cellMove = cell;
                    break;
                }
            }
            if (cellMove == null)
            {
                yield break;
            }
            IsContainerReady = false;
            yield return cellMove.Stack.MoveToContainer().WaitForCompletion();
            yield return null;
            if (cellMove.IsOccupied)
            {
                GameController.Instance.StackPlaceCallBack(cellMove);
            }
        }
        CollectCurrentContainer();
    }

    private Sequence CollectFromTray(List<Slot> slots)
    {
        Sequence move = DOTween.Sequence();
        for (int i = 0; i < Defines.COLLECT_COUNT; i++)
        {
            var btn = slots[i].ButtonInSlot;
            slots[i].ButtonInSlot = null;
            move.Join(btn.MoveToContainer(CurrentContainer));
        }
        move.OnComplete(() =>
        {
            Tray.Instance.CheckAndUpdateWarnings();
        });
        return move;
    }


    private Container GetOtherContainer(Container container)
    {
        return container == _container2 ? _container1 : _container2;
    }

    private float GetScreenEdgeX(bool isLeft)
    {
        float distanceFromCamera = Vector3.Distance(_camera.transform.position, CurrentContainer.transform.position);
        Vector3 screenPos = new Vector3(isLeft ? 0 : Screen.width, Screen.height / 2f, distanceFromCamera);
        return _camera.ScreenToWorldPoint(screenPos).x;
    }

    private float GetLeftEdgeX()
    {
        return GetScreenEdgeX(isLeft: true) - OFFSET;
    }

    private float GetRightEdgeX()
    {
        return -GetScreenEdgeX(isLeft: true) + OFFSET;
    }
}

[System.Serializable]
public class ContainerManagerSnapshot
{
    public List<ObjectColor> Containers;
    public int CurrentIndex;
    public bool IsContainerReady;
    public bool IsCurrentContainer1;
    public ContainerSnapshot Container1;
    public ContainerSnapshot Container2;
}