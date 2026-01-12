using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Dreamteck.Splines;
using UnityEngine;

public class ConveyorController : Singleton<ConveyorController>
{
    [SerializeField] private SplineComputer _splineComputer;
    [SerializeField] private float _cubeSize;
    [SerializeField] private int _walkAroundSpeed;
    [SerializeField] private float _baseOffsetAmount;

    public SplineComputer SplineComputer => _splineComputer;

    private List<PathSlot> _lstPaths = new();
    private Queue<EnterRequest> _waitingToEnterQueue = new();
    private int _totalPathSlotTaken;

    private Coroutine _cycleRoutine;
    private bool _isPaused = false;
    private bool _isRunning = true;

    private void LoseGame()
    {
        if (GameManagerInGame.Instance.CurrentGameStateInGame == GameStateInGame.Result)
            return;

        StopConveyor();
        GameManagerInGame.Instance.SetState(GameStateInGame.Result);
        GameManagerInGame.Instance.SetLose();
        DOVirtual.DelayedCall(1.0f, () => GameUI.Instance.Get<UILose>().Show());
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

        int idx = (nearest + 1) % _lstPaths.Count;
        if (idx == _lstPaths.Count - 1)
            idx = 0;
        return idx;
    }

    public void SetupFromSpline()
    {
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

                return true;
            }
        }

        return false;
    }


    private bool TryCollectInsertRequests(out Dictionary<int, EnterRequest> insertsByIndex)
    {
        insertsByIndex = new Dictionary<int, EnterRequest>();

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

            yield return new WaitForSeconds(timePerCycle);
        }
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

    private static void BuildSlotPositions(List<Vector3> loop, float step, List<Vector3> result)
    {
        result.Clear();
        if (loop == null || loop.Count < 2) return;
        step = Mathf.Max(0.01f, step);

        float totalLen = 0f;
        for (int i = 0; i < loop.Count; i++)
        {
            Vector3 a = loop[i];
            Vector3 b = loop[(i + 1) % loop.Count];
            totalLen += Vector3.Distance(a, b);
        }
        if (totalLen <= 0.0001f) return;

        int slotCount = Mathf.Max(1, Mathf.FloorToInt(totalLen / step));

        Vector3 SampleAt(float d)
        {
            float acc = 0f;
            for (int i = 0; i < loop.Count; i++)
            {
                Vector3 a = loop[i];
                Vector3 b = loop[(i + 1) % loop.Count];
                float seg = Vector3.Distance(a, b);
                if (seg <= 0.0001f) continue;
                if (acc + seg >= d)
                {
                    float t = (d - acc) / seg;
                    return Vector3.Lerp(a, b, t);
                }
                acc += seg;
            }
            return loop[0];
        }

        for (int i = 0; i < slotCount; i++)
        {
            float dist = i * step;
            result.Add(SampleAt(dist));
        }
    }

    private void Clear()
    {
        if (_cycleRoutine != null)
        {
            StopCoroutine(_cycleRoutine);
            _cycleRoutine = null;
        }

        _waitingToEnterQueue.Clear();
        _totalPathSlotTaken = 0;
        _lstPaths.Clear();
    }
}