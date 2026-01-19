using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Dreamteck.Splines;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

public class ConveyorController : Singleton<ConveyorController>
{
    [SerializeField] private TextMeshProUGUI _percent;
    [SerializeField] private Color _warningColor;
    [SerializeField] private MeshRenderer _renderer;
    [SerializeField] private SplineComputer _splineComputer;
    [SerializeField] private GameObject _arrow;
    [SerializeField] private float _cubeSize;
    [SerializeField] private int _walkAroundSpeed;
    [SerializeField] private float _baseOffsetAmount;
    private int _totalLineMoved;

    public SplineComputer SplineComputer => _splineComputer;

    private List<PathSlot> _lstPaths = new();
    private Queue<EnterRequest> _waitingToEnterQueue = new();
    private readonly Dictionary<int, EnterRequest> _insertsByIndex = new();
    private int _totalPathSlotTaken;

    private Coroutine _cycleRoutine;
    private bool _isPaused = false;
    private bool _isRunning = true;
    private bool _isBlinking;
    private Tween _blinkTween;
    private WaitForSeconds _cycleWait;
    private float _cycleWaitSeconds = -1f;
    public bool BringToTop
    {
        set
        {
            SplineComputer.gameObject.layer = value ? LayerMask.NameToLayer("Top") : LayerMask.NameToLayer("Cube");
        }
    }
    void Start()
    {
        GameManagerInGame.Instance.OnEndLevel += () =>
        {
            Time.timeScale = 1;
            _totalLineMoved = 0;
        };
    }
    private void LoseGame()
    {
        if (GameManagerInGame.Instance.CurrentGameStateInGame == GameStateInGame.Result)
            return;

        StopConveyor();
        GameManagerInGame.Instance.SetState(GameStateInGame.Result);
        GameManagerInGame.Instance.SetLose();
        AudioManager.Instance.PlaySFX(SFXType.ConveyorFull);
        DOVirtual.DelayedCall(1.0f, () => GameUI.Instance.Get<UILose>().Show());
    }
    public void OnLineMoved()
    {
        _totalLineMoved++;
        if (_totalLineMoved >= Board.Instance.InitLine)
        {
            Time.timeScale = 2;
        }
    }

    void SpawnArrowAlongSpline()
    {
        float splineLength = _splineComputer.CalculateLength();

        int total = Mathf.Max(2, Mathf.RoundToInt(splineLength * 0.35f));

        SplineSample sample = new SplineSample();

        for (int i = 0; i < total - 1; i++)
        {
            double percent = i / (double)(total - 1);

            _splineComputer.Evaluate(percent, ref sample);

            if (PoolManager.Instance == null) continue;
            GameObject arrow = PoolManager.Instance.Get(_arrow);
            if (arrow == null) continue;
            arrow.transform.SetParent(Board.Instance.transform, false);
            arrow.transform.SetPositionAndRotation(sample.position, Quaternion.LookRotation(sample.forward, sample.up));
        }
    }
    public void WinGame()
    {
        if (GameManagerInGame.Instance.CurrentGameStateInGame == GameStateInGame.Result)
            return;

        StopConveyor();
        GameManagerInGame.Instance.SetWin();
        DOVirtual.DelayedCall(1.0f, () => GameUI.Instance.Get<UIWin>().Show());
    }

    private class PathSlot
    {
        public Vector3 Position;
        public CubeLine CubeSlot;
    }

    private class EnterRequest
    {
        public CubeLine Cube;
        public Line Line;
        public int PreferredIndex;
        public System.Action OnInserted;
    }
    private void UpdatePercent()
    {
        if (_percent == null) return;

        if (_lstPaths == null || _lstPaths.Count == 0)
        {
            _percent.text = "0%";
            return;
        }

        float percent = (_totalPathSlotTaken / (float)_lstPaths.Count) * 100f;
        percent = Mathf.Clamp(percent, 0f, 100f);

        _percent.text = Mathf.RoundToInt(percent) + "%";
        bool shouldBlink = percent >= 70f;

        if (shouldBlink && !_isBlinking)
        {
            StartBlink();
        }
        else if (!shouldBlink && _isBlinking)
        {
            StopBlink();
        }
    }

    public int GetInsertIndexForWorldPosition(Vector3 worldPos)
    {
        if (_lstPaths == null || _lstPaths.Count == 0) return -1;

        int nearest = 0;
        float bestSqr = float.PositiveInfinity;

        for (int i = 0; i < _lstPaths.Count; i++)
        {
            float sqr = (_lstPaths[i].Position - worldPos).sqrMagnitude;
            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                nearest = i;
            }
        }

        Vector3 conveyorDir;
        if (nearest < _lstPaths.Count - 1)
            conveyorDir = (_lstPaths[nearest + 1].Position - _lstPaths[nearest].Position).normalized;
        else
            conveyorDir = (_lstPaths[nearest].Position - _lstPaths[nearest - 1].Position).normalized;

        Vector3 toWorldPos = (worldPos - _lstPaths[nearest].Position).normalized;

        float dot = Vector3.Dot(toWorldPos, conveyorDir);

        int idx;
        if (dot > 0)
        {
            idx = (nearest + 1) % _lstPaths.Count;
        }
        else
        {
            idx = nearest;
        }

        if (idx == _lstPaths.Count - 1)
            idx = 0;

        return idx;
    }

    public void SetupFromSpline()
    {
        SpawnArrowAlongSpline();
        _isRunning = true;
        _isPaused = false;

        Clear();
        _lstPaths.Clear();

        float length = _splineComputer.CalculateLength();
        if (length <= 0.0001f)
        {
            Debug.LogWarning("Spline length is too small.");
            return;
        }

        float distancePerCube = Mathf.Max(0.01f, _cubeSize);
        int slotCount = Mathf.FloorToInt(length / distancePerCube);
        slotCount = Mathf.Max(2, slotCount);

        SplineSample sample = new SplineSample();

        for (int i = 0; i < slotCount; i++)
        {
            float percent = i / (float)slotCount;
            _splineComputer.Evaluate(percent, ref sample);

            _lstPaths.Add(new PathSlot
            {
                Position = sample.position,
                CubeSlot = null
            });
        }

        EnsureCycleRunning();
        UpdatePercent();
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

        foreach (var pathSlot in _lstPaths)
        {
            if (pathSlot.CubeSlot != null)
            {
                pathSlot.CubeSlot.transform.DOKill();
            }
        }
    }

    public void PauseConveyor()
    {
        _isPaused = true;

        foreach (var pathSlot in _lstPaths)
        {
            if (pathSlot.CubeSlot != null)
            {
                pathSlot.CubeSlot.transform.DOPause();
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

        foreach (var pathSlot in _lstPaths)
        {
            if (pathSlot.CubeSlot != null)
            {
                pathSlot.CubeSlot.transform.DOPlay();
            }
        }
    }

    public bool TryRequestEnter(CubeLine cube, int preferredIndex, System.Action onInserted)
    {
        if (cube == null) return false;
        if (_lstPaths.Count == 0) return false;
        if (_totalPathSlotTaken >= _lstPaths.Count)
        {
            LoseGame();
            return false;
        }

        _waitingToEnterQueue.Enqueue(new EnterRequest
        {
            Cube = cube,
            Line = cube.Line,
            PreferredIndex = preferredIndex,
            OnInserted = onInserted
        });
        return true;
    }

    private int NormalizeInsertIndex(int idx)
    {
        if (_lstPaths == null || _lstPaths.Count == 0) return 0;
        if (idx < 0) idx = 0;
        if (idx >= _lstPaths.Count) idx %= _lstPaths.Count;
        if (idx == _lstPaths.Count - 1) idx = 0;
        return idx;
    }
    public bool HasAnyCubeOnConveyor()
    {
        return _totalPathSlotTaken > 0;
    }

    public void SetAllCubesBringToTop(bool value)
    {
        for (int i = 0; i < _lstPaths.Count; i++)
        {
            if (_lstPaths[i].CubeSlot != null)
            {
                _lstPaths[i].CubeSlot.BringToTop = value;
            }
        }
    }

    public List<CubeLine> GetConsecutiveCubesByColor(CubeLine clickedCube)
    {
        List<CubeLine> result = new List<CubeLine>();
        if (clickedCube == null || _lstPaths == null || _lstPaths.Count == 0)
            return result;

        int clickedIndex = -1;
        for (int i = 0; i < _lstPaths.Count; i++)
        {
            if (_lstPaths[i].CubeSlot == clickedCube)
            {
                clickedIndex = i;
                break;
            }
        }

        if (clickedIndex < 0) return result;

        ObjectColor targetColor = clickedCube.Color;
        result.Add(clickedCube);

        for (int i = clickedIndex - 1; i >= 0; i--)
        {
            CubeLine cube = _lstPaths[i].CubeSlot;
            if (cube == null || cube.Color != targetColor) break;
            result.Add(cube);
        }

        for (int i = clickedIndex + 1; i < _lstPaths.Count; i++)
        {
            CubeLine cube = _lstPaths[i].CubeSlot;
            if (cube == null || cube.Color != targetColor) break;
            result.Add(cube);
        }

        return result;
    }

    public void DestroyConsecutiveCubes(List<CubeLine> cubes)
    {
        if (cubes == null || cubes.Count == 0) return;

        foreach (var cube in cubes)
        {
            if (cube == null) continue;
            RemoveCubeFromPath(cube);
            cube.OnHit();
        }
    }

    public bool RemoveCubeFromPath(CubeLine cube)
    {
        if (cube == null) return false;

        for (int i = 0; i < _lstPaths.Count; i++)
        {
            if (_lstPaths[i].CubeSlot == cube)
            {
                _lstPaths[i].CubeSlot = null;
                _totalPathSlotTaken--;

                cube.transform.DOKill();
                UpdatePercent();
                return true;
            }
        }

        return false;
    }


    private bool TryCollectInsertRequests(out Dictionary<int, EnterRequest> insertsByIndex)
    {
        insertsByIndex = _insertsByIndex;
        insertsByIndex.Clear();

        if (_waitingToEnterQueue.Count == 0 || _lstPaths == null || _lstPaths.Count == 0)
            return false;

        int countInQueue = _waitingToEnterQueue.Count;

        for (int i = 0; i < countInQueue; i++)
        {
            var request = _waitingToEnterQueue.Dequeue();

            int idx = NormalizeInsertIndex(request.PreferredIndex);

            if (!insertsByIndex.ContainsKey(idx))
            {
                insertsByIndex[idx] = request;
            }
            else
            {
                _waitingToEnterQueue.Enqueue(request);
            }
        }

        return insertsByIndex.Count > 0;
    }

    private void OnAddToPath()
    {
        _totalPathSlotTaken++;
        UpdatePercent();
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
        float timePerCycle = GetCycleTime();

        while (_isRunning)
        {
            timePerCycle = GetCycleTime();
            if (_isPaused)
            {
                yield return null;
                continue;
            }
            if (_waitingToEnterQueue.Count > 0 && _lstPaths != null && _lstPaths.Count > 0 && _totalPathSlotTaken >= _lstPaths.Count)
            {
                LoseGame();
                yield break;
            }

            bool canEnter = _waitingToEnterQueue.Count > 0 && _totalPathSlotTaken < _lstPaths.Count;
            Dictionary<int, EnterRequest> insertsByIndex = null;
            bool cubeEnter = canEnter && TryCollectInsertRequests(out insertsByIndex);

            int minInsertIndex = 0;
            if (cubeEnter)
            {
                minInsertIndex = int.MaxValue;
                foreach (var kv in insertsByIndex)
                {
                    if (kv.Key < minInsertIndex)
                        minInsertIndex = kv.Key;
                }
                if (minInsertIndex == int.MaxValue)
                    minInsertIndex = 0;
            }

            CubeLine tempCubeSlot = null;

            for (int i = _lstPaths.Count - 1; i >= 0; i--)
            {
                int curIndex = i;
                int prevIndex = (i - 1 + _lstPaths.Count) % _lstPaths.Count;

                if (cubeEnter)
                {
                    if (insertsByIndex != null && insertsByIndex.TryGetValue(i, out var enteringRequest))
                    {
                        if (_lstPaths[curIndex].CubeSlot == null)
                        {
                            _lstPaths[curIndex].CubeSlot = enteringRequest.Cube;
                            var cube = _lstPaths[curIndex].CubeSlot;

                            if (cube != null)
                            {
                                enteringRequest.Line?.NotifyEnteredConveyor();
                                enteringRequest.OnInserted?.Invoke();

                                Vector3 pos = _lstPaths[curIndex].Position;
                                Vector3 dir;
                                if (curIndex < _lstPaths.Count - 1)
                                    dir = (_lstPaths[curIndex + 1].Position - _lstPaths[curIndex].Position).normalized;
                                else
                                    dir = (_lstPaths[curIndex].Position - _lstPaths[curIndex - 1].Position).normalized;

                                if (dir.sqrMagnitude < 0.0001f) dir = Vector3.forward;
                                Vector3 normal = new Vector3(-dir.z, 0f, dir.x);
                                if (normal.sqrMagnitude < 0.0001f) normal = Vector3.right;
                                normal.Normalize();

                                Vector3 targetPos = pos + normal * _baseOffsetAmount;
                                cube.transform.DOMove(targetPos, timePerCycle);
                                cube.transform.LookAt(targetPos + dir);
                            }

                            OnAddToPath();
                            continue;
                        }
                        else
                        {
                            _waitingToEnterQueue.Enqueue(enteringRequest);
                        }
                    }

                    if (i == _lstPaths.Count - 1)
                    {
                        bool emptySlotAhead = false;
                        for (int j = minInsertIndex - 1; j >= 0; j--)
                        {
                            if (_lstPaths[j].CubeSlot == null)
                            {
                                emptySlotAhead = true;
                                break;
                            }
                        }

                        if (emptySlotAhead)
                        {
                            tempCubeSlot = _lstPaths[curIndex].CubeSlot;
                            _lstPaths[curIndex].CubeSlot = null;
                        }
                    }
                }
                else
                {
                    if (i == _lstPaths.Count - 1)
                    {
                        tempCubeSlot = _lstPaths[curIndex].CubeSlot;
                        _lstPaths[curIndex].CubeSlot = null;
                    }
                }

                bool standStill = false;

                if (_lstPaths[curIndex].CubeSlot == null)
                {
                    if (curIndex == 0)
                    {
                        _lstPaths[curIndex].CubeSlot = tempCubeSlot;
                        tempCubeSlot = null;
                    }
                    else
                    {
                        _lstPaths[curIndex].CubeSlot = _lstPaths[prevIndex].CubeSlot;
                        _lstPaths[prevIndex].CubeSlot = null;
                    }
                }
                else
                {
                    standStill = true;
                }

                if (_lstPaths[curIndex].CubeSlot != null && !standStill)
                {
                    CubeMoving(curIndex, timePerCycle);
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

    private void CubeMoving(int idx, float time)
    {
        var slot = _lstPaths[idx];
        if (slot.CubeSlot == null) return;

        var pos = slot.Position;

        Vector3 dir;
        if (idx < _lstPaths.Count - 1)
            dir = (_lstPaths[idx + 1].Position - _lstPaths[idx].Position).normalized;
        else
            dir = (_lstPaths[idx].Position - _lstPaths[idx - 1].Position).normalized;

        if (dir.sqrMagnitude < 0.0001f) dir = Vector3.forward;

        Vector3 normal = new Vector3(-dir.z, 0f, dir.x).normalized;
        Vector3 targetPos = pos + normal * _baseOffsetAmount;

        slot.CubeSlot.transform.DOMove(targetPos, time).SetEase(Ease.Linear);
        slot.CubeSlot.transform.LookAt(targetPos + dir);
    }
    private void StartBlink()
    {
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

        _renderer.material.color = Color.white;
    }

    private void Clear()
    {
        if (_cycleRoutine != null)
        {
            StopCoroutine(_cycleRoutine);
            _cycleRoutine = null;
        }

        _waitingToEnterQueue.Clear();
        _insertsByIndex.Clear();
        _totalPathSlotTaken = 0;
        _lstPaths.Clear();
        UpdatePercent();
    }
}
