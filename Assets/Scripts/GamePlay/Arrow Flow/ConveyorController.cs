using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Dreamteck.Splines;
using UnityEngine;

public class ConveyorController : Singleton<ConveyorController>
{
    [SerializeField] private SplineComputer _splineComputer;
    [SerializeField] private float _cubeSize = 1f;
    [SerializeField] private int _walkAroundSpeed = 15;
    [SerializeField] private float _baseOffsetAmount = 0.25f;
    
    public SplineComputer SplineComputer => _splineComputer;
    public float BaseOffsetAmount => _baseOffsetAmount;
    
    private List<PathSlot> _lstPaths = new(); // Giống _lstPaths trong ArrowGameManager
    private Queue<CubeLine> _waitingToEnterQueue = new(); // Giống _waitingToEnterPathQueue
    private int _totalPathSlotTaken; // Giống ArrowGameManager
    
    private Coroutine _cycleRoutine;

    // PathSlot tương tự PathPosition trong ArrowGameManager
    private class PathSlot
    {
        public Vector3 Position;
        public CubeLine CubeSlot;
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

    public void SetupFromLoop(List<Vector3> loopPoints)
    {
        Clear();
        _lstPaths.Clear();

        if (loopPoints == null || loopPoints.Count < 2)
            return;

        float distancePerCube = Mathf.Max(0.01f, _cubeSize);
        
        List<Vector3> slotPositions = new();
        BuildSlotPositions(loopPoints, distancePerCube, slotPositions);

        for (int i = 0; i < slotPositions.Count; i++)
        {
            _lstPaths.Add(new PathSlot
            {
                Position = slotPositions[i],
                CubeSlot = null
            });
        }

        EnsureCycleRunning();
    }

    public bool TryEnqueueInsertAtIndex(int index, CubeLine cube)
    {
        if (cube == null) return false;
        if (_lstPaths.Count == 0) return false;
        if (_totalPathSlotTaken >= _lstPaths.Count) return false;

        _waitingToEnterQueue.Enqueue(cube);
        return true;
    }

    private void OnAddToPath()
    {
        _totalPathSlotTaken++;
    }

    private void OnRemoveFromPath()
    {
        _totalPathSlotTaken--;
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
        
        while (true)
        {
            // LOGIC GIỐNG HỆT ArrowGameManager.MoveGuestsAround()
            
            bool cubeEnter = _waitingToEnterQueue.Count > 0 && _totalPathSlotTaken < _lstPaths.Count;
            int indexClosestPath = 0;

            if (cubeEnter)
            {
                // Tìm slot gần nhất để insert
                var cubeWaitingPos = _waitingToEnterQueue.Peek().transform.position;
                float closestDistance = float.MaxValue;
                for (int i = 0; i < _lstPaths.Count - 1; i++)
                {
                    float distance = Vector3.Distance(_lstPaths[i].Position, cubeWaitingPos);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        indexClosestPath = i;
                    }
                }
                indexClosestPath = (indexClosestPath + 1) % _lstPaths.Count;
                if (indexClosestPath == _lstPaths.Count - 1)
                    indexClosestPath = 0;
            }

            CubeLine tempCubeSlot = null;

            // DUYỆT NGƯỢC GIỐNG ARROW GAME
            for (int i = _lstPaths.Count - 1; i >= 0; i--)
            {
                int curIndex = i;
                int prevIndex = (i - 1 + _lstPaths.Count) % _lstPaths.Count;
                int nextIndex = (i + 1) % _lstPaths.Count;

                // CUBE ENTERING
                if (cubeEnter)
                {
                    if (i == indexClosestPath)
                    {
                        _lstPaths[curIndex].CubeSlot = _waitingToEnterQueue.Dequeue();
                        var cube = _lstPaths[curIndex].CubeSlot;
                        if (cube != null)
                        {
                            // Apply offset ngay khi insert
                            Vector3 pos = _lstPaths[curIndex].Position;
                            Vector3 dir;
                            if (curIndex < _lstPaths.Count - 1)
                                dir = (_lstPaths[curIndex + 1].Position - _lstPaths[curIndex].Position).normalized;
                            else
                                dir = (_lstPaths[curIndex].Position - _lstPaths[curIndex - 1].Position).normalized;
                            
                            if (dir.sqrMagnitude < 0.0001f)
                                dir = Vector3.forward;
                            
                            Vector3 normal = new Vector3(-dir.z, 0f, dir.x);
                            if (normal.sqrMagnitude < 0.0001f)
                                normal = Vector3.right;
                            normal.Normalize();
                            
                            Vector3 targetPos = pos + normal * _baseOffsetAmount;
                            cube.transform.DOMove(targetPos, timePerCycle);
                        }
                        OnAddToPath();
                        continue;
                    }

                    if (i == _lstPaths.Count - 1) // handle temp slot
                    {
                        bool emptySlotAhead = false;
                        for (int j = indexClosestPath - 1; j >= 0; j--)
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
                else // handle cube moving around
                {
                    if (i == _lstPaths.Count - 1) // handle temp slot
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

                // Move cube to new position
                if (_lstPaths[curIndex].CubeSlot != null)
                {
                    if (!standStill)
                    {
                        CubeMoving(curIndex, timePerCycle);
                    }
                }
            }

            yield return new WaitForSeconds(timePerCycle);
        }
    }

    private void CubeMoving(int idx, float time)
    {
        var pos = _lstPaths[idx].Position;
        
        // Apply offset theo hướng normal như setup
        Vector3 dir;
        if (idx < _lstPaths.Count - 1)
            dir = (_lstPaths[idx + 1].Position - _lstPaths[idx].Position).normalized;
        else
            dir = (_lstPaths[idx].Position - _lstPaths[idx - 1].Position).normalized;
        
        if (dir.sqrMagnitude < 0.0001f)
            dir = Vector3.forward;
        
        Vector3 normal = new Vector3(-dir.z, 0f, dir.x);
        if (normal.sqrMagnitude < 0.0001f)
            normal = Vector3.right;
        normal.Normalize();
        
        Vector3 targetPos = pos + normal * _baseOffsetAmount;
        _lstPaths[idx].CubeSlot.transform.DOMove(targetPos, time);
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