using System.Collections;
using System.Collections.Generic;
using Dreamteck.Splines;
using Sirenix.OdinInspector;
using UnityEngine;

public class ConveyorController : Singleton<ConveyorController>
{
    [SerializeField] private Base _basePrefab;
    [SerializeField] private SplineComputer _conveyor;
    [SerializeField] private float _speed = 0.15f; 
    [SerializeField] private float _cubeSize = 1f;
    [SerializeField] private float _spacing = 0.1f;    
    [ReadOnly] public List<Base> Entries = new();

    private Dictionary<Line, Queue<CubeLine>> _displacedByLine = new Dictionary<Line, Queue<CubeLine>>();
    private Dictionary<Line, HashSet<CubeLine>> _displacedSetByLine = new Dictionary<Line, HashSet<CubeLine>>();
    private HashSet<Line> _processingLines = new HashSet<Line>();
   

   IEnumerator Start()
    {
        yield return null;
        AutoSpawn();
    }

    public void DisplaceBackwardUntilEmpty(Line ownerLine, Base startBase)
    {
        if (ownerLine == null) return;
        if (startBase == null) return;
        if (Entries == null || Entries.Count == 0) return;

        List<CubeLine> removed = new List<CubeLine>();
        Base current = startBase;

        int safety = 0;
        int safetyMax = Entries.Count;

        while (current != null && safety < safetyMax)
        {
            safety++;

            if (!current.IsOccupied) break;
            if (current.CubeOnBase != null && current.CubeOnBase.Line == ownerLine) break;

            CubeLine cube = current.CubeOnBase;
            current.RemoveCube();
            if (cube != null) removed.Add(cube);

            current = GetPrevBaseRaw(current);
        }

        for (int i = removed.Count - 1; i >= 0; i--)
        {
            QueueDisplacedCube(ownerLine, removed[i]);
        }
    }

    public Base GetBaseBefore(Base baseRef, Line ownerLine)
    {
        if (baseRef == null) return null;
        if (Entries == null || Entries.Count == 0) return null;

        int currentIndex = Entries.IndexOf(baseRef);
        if (currentIndex == -1) return null;

        for (int offset = 1; offset < Entries.Count; offset++)
        {
            int prevIndex = (currentIndex - offset + Entries.Count) % Entries.Count;
            Base prevBase = Entries[prevIndex];

            if (ownerLine != null && prevBase.IsOccupied && prevBase.CubeOnBase != null && prevBase.CubeOnBase.Line == ownerLine)
            {
                continue;
            }

            return prevBase;
        }

        return null;
    }

    public Base GetBaseAfter(Base baseRef, Line ownerLine)
    {
        if (baseRef == null) return null;
        if (Entries == null || Entries.Count == 0) return null;

        int currentIndex = Entries.IndexOf(baseRef);
        if (currentIndex == -1) return null;

        for (int offset = 0; offset < Entries.Count; offset++)
        {
            int nextIndex = (currentIndex + offset) % Entries.Count;
            Base nextBase = Entries[nextIndex];

            if (ownerLine != null && nextBase.IsOccupied && nextBase.CubeOnBase != null && nextBase.CubeOnBase.Line == ownerLine)
            {
                continue;
            }

            return nextBase;
        }

        return null;
    }

    [Button]
    public void AutoSpawn()
    {
        Clear();

        float splineLength = _conveyor.CalculateLength();
        float distancePerCube = _cubeSize + _spacing;
        int total = Mathf.CeilToInt(splineLength / distancePerCube);
        SpawnCubes(total);
    }

    private void SpawnCubes(int total)
    {
        for (int i = 0; i < total; i++)
        {
            var cube = Instantiate(_basePrefab, transform);
            cube.Positioner.spline = _conveyor;
            cube.Positioner.motion.applyRotation = false;

            Entries.Add(cube);
        }
    }

    void Update()
    {
        if (Entries.Count == 0) return;

        float basePercent = (Time.time * _speed) % 1f;
        int count = Entries.Count;

        for (int i = 0; i < count; i++)
        {
            float offset = (float)i / count;
            Entries[i].Positioner.SetPercent((basePercent + offset) % 1f);
        }
    }

    private void Clear()
    {
        foreach (var c in Entries)
        {
            if (c != null) DestroyImmediate(c.gameObject);
        }
        Entries.Clear();
        _displacedByLine.Clear();
        _displacedSetByLine.Clear();
        _processingLines.Clear();
    }

    /// <summary>
    /// Thêm cube bị displaced vào queue
    /// </summary>
    public void QueueDisplacedCube(Line ownerLine, CubeLine cube)
    {
        if (ownerLine == null || cube == null) return;

        if (!_displacedByLine.TryGetValue(ownerLine, out var q))
        {
            q = new Queue<CubeLine>();
            _displacedByLine.Add(ownerLine, q);
        }

        if (!_displacedSetByLine.TryGetValue(ownerLine, out var set))
        {
            set = new HashSet<CubeLine>();
            _displacedSetByLine.Add(ownerLine, set);
        }

        if (set.Add(cube))
        {
            q.Enqueue(cube);
            Debug.Log($"Cube {cube.name} đã bị displaced và được thêm vào queue");
        }
    }

    /// <summary>
    /// Sau khi line đưa hết cube lên conveyor, nối các cube bị displaced vào các base kế tiếp phía sau line.
    /// </summary>
    public void ProcessDisplacedCubesForLine(Line ownerLine, Base lastAssignedBase)
    {
        if (ownerLine == null) return;
        if (lastAssignedBase == null) return;
        if (_processingLines.Contains(ownerLine)) return;

        if (!_displacedByLine.TryGetValue(ownerLine, out var q) || q == null || q.Count == 0) return;
        if (!_displacedSetByLine.TryGetValue(ownerLine, out var set) || set == null) return;

        _processingLines.Add(ownerLine);

        Base current = lastAssignedBase;
        int safety = 0;
        int safetyMax = Entries.Count * 4;

        while (q.Count > 0 && safety < safetyMax)
        {
            safety++;
            CubeLine cubeToAppend = q.Dequeue();
            if (cubeToAppend != null) set.Remove(cubeToAppend);

            if (cubeToAppend == null) continue;

            Base nextBase = FindNextBaseOnConveyor(current, ownerLine);
            if (nextBase == null) break;

            if (nextBase.IsOccupied)
            {
                CubeLine newlyDisplaced = nextBase.CubeOnBase;
                nextBase.RemoveCube();
                QueueDisplacedCube(ownerLine, newlyDisplaced);
            }

            nextBase.AssignCube(cubeToAppend);
            current = nextBase;
        }

        _processingLines.Remove(ownerLine);
    }

    private Base FindNextBaseOnConveyor(Base currentBase, Line ownerLine)
    {
        var entries = Entries;
        int currentIndex = entries.IndexOf(currentBase);

        if (currentIndex == -1)
        {
            Debug.LogError("Current base không có trong danh sách conveyor");
            return null;
        }

        for (int offset = 1; offset < entries.Count; offset++)
        {
            int prevIndex = (currentIndex - offset + entries.Count) % entries.Count;
            Base prevBase = entries[prevIndex];

            if (prevBase.IsOccupied && prevBase.CubeOnBase != null && prevBase.CubeOnBase.Line == ownerLine)
            {
                continue;
            }

            return prevBase;
        }

        return null;
    }

    private Base GetPrevBaseRaw(Base currentBase)
    {
        var entries = Entries;
        int currentIndex = entries.IndexOf(currentBase);

        if (currentIndex == -1) return null;

        int prevIndex = (currentIndex - 1 + entries.Count) % entries.Count;
        return entries[prevIndex];
    }
}