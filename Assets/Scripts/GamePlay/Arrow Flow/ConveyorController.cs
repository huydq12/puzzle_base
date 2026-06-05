using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Dreamteck.Splines;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

public class ConveyorController : Singleton<ConveyorController>
{
    private const int TrackIndexShift = 20;
    private const int TrackSlotMask = (1 << TrackIndexShift) - 1;

    [SerializeField] private Color _warningColor;
    [SerializeField] private MeshRenderer _renderer;
    [SerializeField] private SplineComputer _splineComputer;
    [SerializeField] private GameObject _arrow;
    public float _cubeSize;
    [SerializeField] private int _walkAroundSpeed;
    [SerializeField] private float _baseOffsetAmount;
    private int _totalLineMoved;

    public SplineComputer SplineComputer => _splineComputer;

    private readonly List<ConveyorTrack> _tracks = new();
    private readonly List<SplineComputer> _extraSplineComputers = new();
    private readonly List<GameObject> _spawnedArrows = new();
    private int _totalPathSlotTaken;

    private Coroutine _cycleRoutine;
    private bool _isPaused;
    private bool _isRunning = true;
    private bool _loseTriggered;
    private Tween _loseShowTween;
    private bool _isBlinking;
    private Tween _blinkTween;
    private WaitForSeconds _cycleWait;
    private float _cycleWaitSeconds = -1f;

    public bool BringToTop
    {
        set
        {
            int layer = value ? LayerMask.NameToLayer("Top") : LayerMask.NameToLayer("Cube");
            for (int i = 0; i < _tracks.Count; i++)
            {
                ConveyorTrack track = _tracks[i];
                if (track?.Spline == null) continue;
                track.Spline.gameObject.layer = layer;
            }
        }
    }

    private class PathSlot
    {
        public Vector3 Position;
        public Vector3 Forward;
        public Vector3 Left;
        public CubeLine CubeSlot;
    }

    private class EnterRequest
    {
        public CubeLine Cube;
        public Line Line;
        public int PreferredIndex;
        public System.Action OnInserted;
    }

    private class ConveyorTrack
    {
        public SplineComputer Spline;
        public MeshRenderer Renderer;
        public List<PathSlot> Paths = new();
        public Queue<EnterRequest> WaitingToEnterQueue = new();
        public Dictionary<int, EnterRequest> InsertsByIndex = new();
    }

    public readonly struct TrackSplineSetup
    {
        public readonly SplinePoint[] Points;
        public readonly bool IsClosedLoop;

        public TrackSplineSetup(SplinePoint[] points, bool isClosedLoop)
        {
            Points = points;
            IsClosedLoop = isClosedLoop;
        }
    }

    private void Start()
    {
        GameManagerInGame.Instance.OnEndLevel += () =>
        {
            _totalLineMoved = 0;
            SetWalkAroundSpeed(12, "OnEndLevel");
        };

        GameManagerInGame.Instance.OnStartLevel += () =>
        {
            _loseTriggered = false;
            if (_loseShowTween != null)
            {
                _loseShowTween.Kill();
                _loseShowTween = null;
            }
        };
    }

    public void ResetLoseStateForContinue()
    {
        _loseTriggered = false;
        if (_loseShowTween != null)
        {
            _loseShowTween.Kill();
            _loseShowTween = null;
        }
    }

    private void LoseGame()
    {
        if (_loseTriggered)
            return;

        if (GameManagerInGame.Instance.CurrentGameStateInGame == GameStateInGame.Result)
            return;

        _loseTriggered = true;

        StopConveyor();
        GameManagerInGame.Instance.SetState(GameStateInGame.Result);
        GameManagerInGame.Instance.SetLose();
        AudioManager.Instance.PlaySFX(SFXType.LevelFailed);
        VibrateManager.Instance.PlayHaptic(HapticType.LevelFailed);
        if (_loseShowTween != null) _loseShowTween.Kill();
        _loseShowTween = DOVirtual.DelayedCall(1f, () => UIManager.Instance.ShowUI<UIPauseLose>());
    }

    public void OnLineMoved()
    {
        _totalLineMoved++;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[Conveyor.Speed] OnLineMoved totalLineMoved={_totalLineMoved} initLine={(Board.Instance != null ? Board.Instance.InitLine : -1)} currentSpeed={_walkAroundSpeed} pending={GetPendingRequestCount()} onConveyor={_totalPathSlotTaken}", this);
#endif
    }

    public void ActivateSpeedUpForClearBoard()
    {
        SetWalkAroundSpeed(20, "ActivateSpeedUpForClearBoard");
    }

    private void SetWalkAroundSpeed(int speed, string reason)
    {
        int oldSpeed = _walkAroundSpeed;
        _walkAroundSpeed = speed;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[Conveyor.Speed] {oldSpeed} -> {_walkAroundSpeed} reason={reason} totalLineMoved={_totalLineMoved} initLine={(Board.Instance != null ? Board.Instance.InitLine : -1)} pending={GetPendingRequestCount()} onConveyor={_totalPathSlotTaken}", this);
#endif
    }

    public void WinGame()
    {
        if (GameManagerInGame.Instance.CurrentGameStateInGame == GameStateInGame.Result)
            return;

        StopConveyor();
        GameManagerInGame.Instance.SetWin();
        DOVirtual.DelayedCall(1.0f, () => UIManager.Instance.ShowUI<UIWin>());
    }

    public void SetupTracks(IReadOnlyList<TrackSplineSetup> trackSetups)
    {
        Clear();

        // A previous win/lose stops the conveyor loop. Starting a new level must
        // restore the runtime state so queued inserts and slot movement resume.
        _isRunning = true;
        _isPaused = false;
        _loseTriggered = false;

        if (trackSetups == null || trackSetups.Count == 0)
        {
            EnsureTrackObjects(0);
            return;
        }

        EnsureTrackObjects(trackSetups.Count);
        _tracks.Clear();

        for (int i = 0; i < trackSetups.Count; i++)
        {
            TrackSplineSetup setup = trackSetups[i];
            SplineComputer spline = GetSplineComputerForTrack(i);
            if (spline == null || setup.Points == null || setup.Points.Length < 2)
                continue;

            spline.SetPoints(setup.Points);
            if (setup.IsClosedLoop)
                spline.Close();
            else
                spline.Break();
            spline.RebuildImmediate(true, true);

            ConveyorTrack track = new ConveyorTrack
            {
                Spline = spline,
                Renderer = spline.GetComponent<MeshRenderer>() ?? (i == 0 ? _renderer : null)
            };

            BuildPathSlots(track);
            if (track.Paths.Count == 0)
                continue;

            _tracks.Add(track);
            SpawnArrowAlongSpline(track.Spline);
        }

        EnsureCycleRunning();
        UpdatePercent();
    }

    public void SetupFromSpline()
    {
        if (_splineComputer == null)
            return;

        SplinePoint[] points = _splineComputer.GetPoints();
        bool isClosedLoop = _splineComputer.isClosed;
        SetupTracks(new[] { new TrackSplineSetup(points, isClosedLoop) });
    }

    public void StopConveyor()
    {
        _isRunning = false;
        _isPaused = true;

        if (_cycleRoutine != null)
        {
            StopCoroutine(_cycleRoutine);
            _cycleRoutine = null;
        }

        for (int trackIndex = 0; trackIndex < _tracks.Count; trackIndex++)
        {
            List<PathSlot> paths = _tracks[trackIndex].Paths;
            for (int i = 0; i < paths.Count; i++)
            {
                if (paths[i].CubeSlot != null)
                {
                    paths[i].CubeSlot.SetRuntimeHidden(false);
                    paths[i].CubeSlot.transform.DOKill();
                }
            }
        }
    }

    public void PauseConveyor()
    {
        _isPaused = true;

        for (int trackIndex = 0; trackIndex < _tracks.Count; trackIndex++)
        {
            List<PathSlot> paths = _tracks[trackIndex].Paths;
            for (int i = 0; i < paths.Count; i++)
            {
                if (paths[i].CubeSlot != null)
                    paths[i].CubeSlot.transform.DOPause();
            }
        }
    }

    public void StartConveyor()
    {
        _isRunning = true;
        _isPaused = false;
        EnsureCycleRunning();
    }

    public void ResumeConveyor()
    {
        _isPaused = false;

        for (int trackIndex = 0; trackIndex < _tracks.Count; trackIndex++)
        {
            List<PathSlot> paths = _tracks[trackIndex].Paths;
            for (int i = 0; i < paths.Count; i++)
            {
                if (paths[i].CubeSlot != null)
                    paths[i].CubeSlot.transform.DOPlay();
            }
        }
    }

    public int GetInsertIndexForWorldPosition(Vector3 worldPos)
    {
        int bestTrackIndex = -1;
        int bestSlotIndex = -1;
        float bestSqr = float.PositiveInfinity;

        for (int trackIndex = 0; trackIndex < _tracks.Count; trackIndex++)
        {
            List<PathSlot> paths = _tracks[trackIndex].Paths;
            for (int i = 0; i < paths.Count; i++)
            {
                float sqr = (paths[i].Position - worldPos).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    bestTrackIndex = trackIndex;
                    bestSlotIndex = i;
                }
            }
        }

        if (bestTrackIndex < 0 || bestSlotIndex < 0)
            return -1;

        Vector3 conveyorDir = _tracks[bestTrackIndex].Paths[bestSlotIndex].Forward;
        if (conveyorDir.sqrMagnitude < 0.0001f) conveyorDir = Vector3.forward;

        Vector3 toWorldPos = (worldPos - _tracks[bestTrackIndex].Paths[bestSlotIndex].Position).normalized;
        float dot = Vector3.Dot(toWorldPos, conveyorDir);

        int idx = dot > 0f ? (bestSlotIndex + 1) % _tracks[bestTrackIndex].Paths.Count : bestSlotIndex;
        if (idx == _tracks[bestTrackIndex].Paths.Count - 1)
            idx = 0;

        return EncodeTrackSlot(bestTrackIndex, idx);
    }

    public bool TryRequestEnter(CubeLine cube, int preferredIndex, System.Action onInserted)
    {
        if (cube == null) return false;
        if (!TryDecodeTrackSlot(preferredIndex, out int trackIndex, out int localIndex)) return false;
        if (trackIndex < 0 || trackIndex >= _tracks.Count) return false;

        ConveyorTrack track = _tracks[trackIndex];
        if (track.Paths.Count == 0) return false;
        if (GetTrackOccupiedCount(track) >= track.Paths.Count)
        {
            LoseGame();
            return false;
        }

        track.WaitingToEnterQueue.Enqueue(new EnterRequest
        {
            Cube = cube,
            Line = cube.Line,
            PreferredIndex = localIndex,
            OnInserted = onInserted
        });
        return true;
    }

    public bool HasAnyCubeOnConveyor()
    {
        return _totalPathSlotTaken > 0;
    }

    public bool HasAnyCubePendingOrOnConveyor()
    {
        return _totalPathSlotTaken > 0 || GetPendingRequestCount() > 0;
    }

    public void SetAllCubesBringToTop(bool value)
    {
        for (int trackIndex = 0; trackIndex < _tracks.Count; trackIndex++)
        {
            List<PathSlot> paths = _tracks[trackIndex].Paths;
            for (int i = 0; i < paths.Count; i++)
            {
                if (paths[i].CubeSlot != null)
                    paths[i].CubeSlot.BringToTop = value;
            }
        }
    }

    public List<CubeLine> GetConsecutiveCubesByColor(CubeLine clickedCube)
    {
        if (clickedCube == null) return new List<CubeLine>();

        HashSet<CubeLine> seen = new();
        List<CubeLine> result = new();

        if (seen.Add(clickedCube))
            result.Add(clickedCube);

        for (int trackIndex = 0; trackIndex < _tracks.Count; trackIndex++)
        {
            List<PathSlot> paths = _tracks[trackIndex].Paths;
            for (int i = 0; i < paths.Count; i++)
            {
                CubeLine cube = paths[i].CubeSlot;
                if (cube == null || cube.Color != clickedCube.Color) continue;
                if (seen.Add(cube))
                    result.Add(cube);
            }
        }

        return result;
    }

    public Dictionary<ObjectColor, int> DestroyConsecutiveCubes(List<CubeLine> cubes)
    {
        Dictionary<ObjectColor, int> destroyedByColor = new();
        if (cubes == null || cubes.Count == 0) return destroyedByColor;

        Transform source = null;

        foreach (CubeLine cube in cubes)
        {
            if (cube == null) continue;
            if (source == null) source = cube.transform;

            bool willRevealTwoLayer = cube.ElementType == 3 && !cube.IsElementType3Revealed;
            ObjectColor preHitColor = cube.Color;
            ObjectColor outerColor = cube.OriginalColor;

            bool destroyed = cube.OnHit();
            if (destroyed)
            {
                if (!destroyedByColor.ContainsKey(preHitColor))
                    destroyedByColor[preHitColor] = 0;
                destroyedByColor[preHitColor]++;
            }
            else if (willRevealTwoLayer)
            {
                if (!destroyedByColor.ContainsKey(outerColor))
                    destroyedByColor[outerColor] = 0;
                destroyedByColor[outerColor]++;
            }
        }

        if (Board.Instance != null)
        {
            foreach (var kvp in destroyedByColor)
                Board.Instance.NotifyLineDoorHit(kvp.Key, kvp.Value, source);
        }

        return destroyedByColor;
    }

    public bool RemoveCubeFromPath(CubeLine cube)
    {
        if (cube == null) return false;

        for (int trackIndex = 0; trackIndex < _tracks.Count; trackIndex++)
        {
            List<PathSlot> paths = _tracks[trackIndex].Paths;
            for (int i = 0; i < paths.Count; i++)
            {
                if (paths[i].CubeSlot != cube) continue;

                paths[i].CubeSlot = null;
                _totalPathSlotTaken--;
                cube.transform.DOKill();
                UpdatePercent();
                return true;
            }
        }

        return false;
    }

    public int GetTotalPathSlotTaken()
    {
        return _totalPathSlotTaken;
    }

    private static int EncodeTrackSlot(int trackIndex, int slotIndex)
    {
        return (trackIndex << TrackIndexShift) | (slotIndex & TrackSlotMask);
    }

    private static bool TryDecodeTrackSlot(int encoded, out int trackIndex, out int slotIndex)
    {
        if (encoded < 0)
        {
            trackIndex = -1;
            slotIndex = -1;
            return false;
        }

        trackIndex = encoded >> TrackIndexShift;
        slotIndex = encoded & TrackSlotMask;
        return true;
    }

    private void SpawnArrowAlongSpline(SplineComputer spline)
    {
        if (_arrow == null || spline == null) return;

        float splineLength = spline.CalculateLength();
        int total = Mathf.Max(2, Mathf.RoundToInt(splineLength * 0.35f));
        SplineSample sample = new();
        float stepDistance = splineLength / (total - 1);
        double percent = 0.0;

        for (int i = 0; i < total - 1; i++)
        {
            spline.Evaluate(percent, ref sample);

            GameObject arrow = Instantiate(_arrow);
            if (arrow == null) continue;

            arrow.transform.SetParent(Board.Instance.transform, false);
            arrow.transform.SetPositionAndRotation(sample.position, Quaternion.LookRotation(sample.forward, sample.up));
            _spawnedArrows.Add(arrow);

            float moved;
            percent = spline.Travel(percent, stepDistance, out moved);
            if (percent >= 1.0) percent -= 1.0;
        }
    }

    private void EnsureTrackObjects(int count)
    {
        if (_splineComputer != null)
            _splineComputer.gameObject.SetActive(count > 0);

        while (_extraSplineComputers.Count < Mathf.Max(0, count - 1))
        {
            GameObject clone = Instantiate(_splineComputer.gameObject, _splineComputer.transform.parent);
            clone.name = $"{_splineComputer.gameObject.name}_Extra{_extraSplineComputers.Count + 1}";
            SplineComputer spline = clone.GetComponent<SplineComputer>();
            if (spline != null)
                _extraSplineComputers.Add(spline);
        }

        for (int i = 0; i < _extraSplineComputers.Count; i++)
        {
            bool active = i < count - 1;
            if (_extraSplineComputers[i] != null)
                _extraSplineComputers[i].gameObject.SetActive(active);
        }
    }

    private SplineComputer GetSplineComputerForTrack(int trackIndex)
    {
        if (trackIndex == 0)
            return _splineComputer;

        int extraIndex = trackIndex - 1;
        return extraIndex >= 0 && extraIndex < _extraSplineComputers.Count ? _extraSplineComputers[extraIndex] : null;
    }

    private void BuildPathSlots(ConveyorTrack track)
    {
        track.Paths.Clear();
        if (track.Spline == null) return;
        if (track.Spline.sampleMode != SplineComputer.SampleMode.Uniform)
            track.Spline.sampleMode = SplineComputer.SampleMode.Uniform;

        float length = track.Spline.CalculateLength();
        if (length <= 0.0001f)
        {
            Debug.LogWarning("Spline length is too small.");
            return;
        }

        float distancePerCube = Mathf.Max(0.01f, _cubeSize);
        int slotCount = Mathf.RoundToInt(length / distancePerCube);
        slotCount = Mathf.Max(2, slotCount);
        float stepDistance = length / slotCount;
        SplineSample sample = new();
        double percent = 0.0;

        for (int i = 0; i < slotCount; i++)
        {
            track.Spline.Evaluate(percent, ref sample);

            Vector3 forward = sample.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f) forward = Vector3.forward;
            forward.Normalize();

            Vector3 left = -Vector3.Cross(Vector3.up, forward);
            if (left.sqrMagnitude < 0.0001f) left = Vector3.right;
            left.Normalize();

            track.Paths.Add(new PathSlot
            {
                Position = sample.position,
                Forward = forward,
                Left = left,
                CubeSlot = null
            });

            float moved;
            percent = track.Spline.Travel(percent, stepDistance, out moved);
            if (percent >= 1.0) percent -= 1.0;
        }
    }

    private int NormalizeInsertIndex(ConveyorTrack track, int idx)
    {
        if (track == null || track.Paths.Count == 0) return 0;
        if (idx < 0) idx = 0;
        if (idx >= track.Paths.Count) idx %= track.Paths.Count;
        if (idx == track.Paths.Count - 1) idx = 0;
        return idx;
    }

    private bool TryCollectInsertRequests(ConveyorTrack track)
    {
        track.InsertsByIndex.Clear();

        if (track.WaitingToEnterQueue.Count == 0 || track.Paths.Count == 0)
            return false;

        int countInQueue = track.WaitingToEnterQueue.Count;
        for (int i = 0; i < countInQueue; i++)
        {
            EnterRequest request = track.WaitingToEnterQueue.Dequeue();
            int idx = NormalizeInsertIndex(track, request.PreferredIndex);

            if (!track.InsertsByIndex.ContainsKey(idx))
                track.InsertsByIndex[idx] = request;
            else
                track.WaitingToEnterQueue.Enqueue(request);
        }

        return track.InsertsByIndex.Count > 0;
    }

    private void OnAddToPath()
    {
        _totalPathSlotTaken++;
        UpdatePercent();
    }

    private int GetTrackOccupiedCount(ConveyorTrack track)
    {
        int count = 0;
        for (int i = 0; i < track.Paths.Count; i++)
        {
            if (track.Paths[i].CubeSlot != null)
                count++;
        }
        return count;
    }

    private int GetPendingRequestCount()
    {
        int total = 0;
        for (int i = 0; i < _tracks.Count; i++)
            total += _tracks[i].WaitingToEnterQueue.Count;
        return total;
    }

    private float GetCycleTime()
    {
        int speed = Mathf.Max(1, _walkAroundSpeed);
        return 1f / speed;
    }

    private void EnsureCycleRunning()
    {
        if (_cycleRoutine != null)
            return;
        _cycleRoutine = StartCoroutine(CycleLoop());
    }

    private IEnumerator CycleLoop()
    {
        while (_isRunning)
        {
            float timePerCycle = GetCycleTime();

            if (_isPaused)
            {
                yield return null;
                continue;
            }

            for (int trackIndex = 0; trackIndex < _tracks.Count; trackIndex++)
            {
                ConveyorTrack track = _tracks[trackIndex];
                if (track.Paths.Count == 0) continue;

                if (track.WaitingToEnterQueue.Count > 0 && GetTrackOccupiedCount(track) >= track.Paths.Count)
                {
                    LoseGame();
                    yield break;
                }

                bool canEnter = track.WaitingToEnterQueue.Count > 0 && GetTrackOccupiedCount(track) < track.Paths.Count;
                bool cubeEnter = canEnter && TryCollectInsertRequests(track);

                int minInsertIndex = 0;
                if (cubeEnter)
                {
                    minInsertIndex = int.MaxValue;
                    foreach (var kv in track.InsertsByIndex)
                    {
                        if (kv.Key < minInsertIndex)
                            minInsertIndex = kv.Key;
                    }
                    if (minInsertIndex == int.MaxValue)
                        minInsertIndex = 0;
                }

                CubeLine tempCubeSlot = null;

                for (int i = track.Paths.Count - 1; i >= 0; i--)
                {
                    int curIndex = i;
                    int prevIndex = (i - 1 + track.Paths.Count) % track.Paths.Count;

                    if (cubeEnter)
                    {
                        if (track.InsertsByIndex.TryGetValue(i, out EnterRequest enteringRequest))
                        {
                            if (track.Paths[curIndex].CubeSlot == null)
                            {
                                track.Paths[curIndex].CubeSlot = enteringRequest.Cube;
                                CubeLine cube = track.Paths[curIndex].CubeSlot;

                                if (cube != null)
                                {
                                    cube.SetConveyorVisual();
                                    enteringRequest.Line?.NotifyEnteredConveyor();
                                    enteringRequest.OnInserted?.Invoke();

                                    PathSlot slot = track.Paths[curIndex];
                                    Vector3 dir = slot.Forward.sqrMagnitude < 0.0001f ? Vector3.forward : slot.Forward;
                                    Vector3 left = slot.Left.sqrMagnitude < 0.0001f ? Vector3.right : slot.Left;

                                    Vector3 targetPos = slot.Position + left * _baseOffsetAmount;
                                    cube.transform.DOMove(targetPos, timePerCycle);
                                    cube.transform.LookAt(targetPos + dir);
                                }

                                OnAddToPath();
                                continue;
                            }

                            track.WaitingToEnterQueue.Enqueue(enteringRequest);
                        }

                        if (i == track.Paths.Count - 1)
                        {
                            bool emptySlotAhead = false;
                            for (int j = minInsertIndex - 1; j >= 0; j--)
                            {
                                if (track.Paths[j].CubeSlot == null)
                                {
                                    emptySlotAhead = true;
                                    break;
                                }
                            }

                            if (emptySlotAhead)
                            {
                                tempCubeSlot = track.Paths[curIndex].CubeSlot;
                                track.Paths[curIndex].CubeSlot = null;
                            }
                        }
                    }
                    else if (i == track.Paths.Count - 1)
                    {
                        tempCubeSlot = track.Paths[curIndex].CubeSlot;
                        track.Paths[curIndex].CubeSlot = null;
                    }

                    bool standStill = false;
                    bool hideDuringMove = false;

                    if (track.Paths[curIndex].CubeSlot == null)
                    {
                        if (curIndex == 0)
                        {
                            hideDuringMove = tempCubeSlot != null;
                            track.Paths[curIndex].CubeSlot = tempCubeSlot;
                            tempCubeSlot = null;
                        }
                        else
                        {
                            track.Paths[curIndex].CubeSlot = track.Paths[prevIndex].CubeSlot;
                            track.Paths[prevIndex].CubeSlot = null;
                        }
                    }
                    else
                    {
                        standStill = true;
                    }

                    if (track.Paths[curIndex].CubeSlot != null && !standStill)
                        CubeMoving(track.Paths[curIndex], timePerCycle, hideDuringMove);
                }
            }

            yield return GetCycleWait(timePerCycle);
        }
    }

    private WaitForSeconds GetCycleWait(float seconds)
    {
        if (_cycleWait == null || Mathf.Abs(_cycleWaitSeconds - seconds) > 0.0001f)
        {
            _cycleWaitSeconds = seconds;
            _cycleWait = new WaitForSeconds(seconds);
        }
        return _cycleWait;
    }

    private void CubeMoving(PathSlot slot, float time, bool hideUntilArrived = false)
    {
        if (slot?.CubeSlot == null) return;

        Vector3 dir = slot.Forward.sqrMagnitude < 0.0001f ? Vector3.forward : slot.Forward;
        Vector3 left = slot.Left.sqrMagnitude < 0.0001f ? Vector3.right : slot.Left;
        Vector3 targetPos = slot.Position + left * _baseOffsetAmount;

        CubeLine cube = slot.CubeSlot;
        cube.transform.DOKill();

        if (hideUntilArrived)
            cube.SetRuntimeHidden(true);

        cube.transform.DOMove(targetPos, time)
            .SetEase(Ease.Linear)
            .OnComplete(() =>
            {
                if (cube != null)
                    cube.SetRuntimeHidden(false);
            });
        cube.transform.LookAt(targetPos + dir);
    }

    private void StartBlink()
    {
        if (_renderer == null) return;

        _isBlinking = true;

        _blinkTween?.Kill();
        _renderer.material.color = Color.white;

        _blinkTween = _renderer.material
            .DOColor(_warningColor, 0.3f)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);
    }

    private void StopBlink()
    {
        _isBlinking = false;

        _blinkTween?.Kill();
        _blinkTween = null;

        if (_renderer != null)
            _renderer.material.color = Color.white;
    }

    private void UpdatePercent()
    {
        int totalSlots = 0;
        for (int i = 0; i < _tracks.Count; i++)
            totalSlots += _tracks[i].Paths.Count;

        float percent = 0f;
        if (totalSlots > 0)
        {
            percent = (_totalPathSlotTaken / (float)totalSlots) * 100f;
            percent = Mathf.Clamp(percent, 0f, 100f);
        }

        var ui = UIManager.Instance.Get<UIBottomInGame>();
        if (ui != null)
            ui.SetConveyorPercent(percent);

        bool shouldBlink = percent >= 70f;
        var topUi = UIManager.Instance.Get<UITopInGame>();
        if (topUi != null)
            topUi.SetConveyorWarning(shouldBlink);

        if (shouldBlink && !_isBlinking)
            StartBlink();
        else if (!shouldBlink && _isBlinking)
            StopBlink();
    }

    private void Clear()
    {
        if (_cycleRoutine != null)
        {
            StopCoroutine(_cycleRoutine);
            _cycleRoutine = null;
        }

        for (int i = 0; i < _tracks.Count; i++)
        {
            ConveyorTrack track = _tracks[i];
            track.WaitingToEnterQueue.Clear();
            track.InsertsByIndex.Clear();
            track.Paths.Clear();
        }

        _tracks.Clear();
        _totalPathSlotTaken = 0;

        for (int i = 0; i < _spawnedArrows.Count; i++)
        {
            if (_spawnedArrows[i] != null)
                Destroy(_spawnedArrows[i]);
        }
        _spawnedArrows.Clear();

        UpdatePercent();
    }
}
