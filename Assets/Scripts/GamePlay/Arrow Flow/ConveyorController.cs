using System;
using System.Collections.Generic;
using Dreamteck.Splines;
using Sirenix.OdinInspector;
using UnityEngine;

[Serializable]
public class CubeEntry
{
    public CubeLine Cube;
    public float DistanceUnwrapped;
}

public class ConveyorController : Singleton<ConveyorController>
{
    [SerializeField] private SplineComputer _conveyor;
    [SerializeField] private float _speed = 0.15f;
    public List<CubeEntry> Entries = new();

    private static float NormalizeDistance(float distance, float splineLength)
    {
        if (splineLength <= 0f) return 0f;
        distance %= splineLength;
        if (distance < 0f) distance += splineLength;
        return distance;
    }

    private static float ClosestUnwrappedToReference(float wrappedDistance, float referenceUnwrapped, float splineLength)
    {
        if (splineLength <= 0f) return wrappedDistance;

        float candidate = wrappedDistance;
        while (candidate < referenceUnwrapped - splineLength * 0.5f) candidate += splineLength;
        while (candidate > referenceUnwrapped + splineLength * 0.5f) candidate -= splineLength;
        return candidate;
    }

    private int FindEntryIndex(CubeLine cube)
    {
        if (cube == null) return -1;
        for (int i = 0; i < Entries.Count; i++)
        {
            if (Entries[i].Cube == cube) return i;
        }
        return -1;
    }

    private int FindInsertIndexByDistance(float distanceUnwrapped)
    {
        int insertIndex = 0;
        while (insertIndex < Entries.Count && Entries[insertIndex].DistanceUnwrapped > distanceUnwrapped)
            insertIndex++;
        return insertIndex;
    }

    private void SortEntriesByDistanceDesc()
    {
        Entries.Sort((a, b) => b.DistanceUnwrapped.CompareTo(a.DistanceUnwrapped));
    }

    private float GetMaxDistanceUnwrapped()
    {
        float max = float.NegativeInfinity;
        for (int i = 0; i < Entries.Count; i++)
        {
            if (Entries[i].DistanceUnwrapped > max) max = Entries[i].DistanceUnwrapped;
        }
        return max;
    }

    public void AddCube(CubeLine cube, Vector3 entryWorldPosition, float handoffDuration)
    {
        if (cube == null || cube.Positioner == null || _conveyor == null) return;
        
        cube.transform.SetParent(_conveyor.transform);
        cube.Positioner.spline = _conveyor;
        cube.Positioner.motion.applyRotation = false;

        float splineLength = _conveyor.CalculateLength();
        if (splineLength <= 0f) return;

        SplineSample entrySample = _conveyor.Project(entryWorldPosition);
        float entryWrapped = NormalizeDistance(_conveyor.CalculateLength(0.0, entrySample.percent), splineLength);

        // Tránh add trùng
        for (int i = 0; i < Entries.Count; i++)
        {
            if (Entries[i].Cube == cube) return;
        }

        // Keep entries sorted
        if (Entries.Count > 1) SortEntriesByDistanceDesc();

        float reference = Entries.Count > 0 ? GetMaxDistanceUnwrapped() : 0f;
        float entryUnwrapped = Entries.Count > 0 ? ClosestUnwrappedToReference(entryWrapped, reference, splineLength) : entryWrapped;

        int insertIndex = FindInsertIndexByDistance(entryUnwrapped);

        CubeEntry newEntry = new CubeEntry
        {
            Cube = cube,
            DistanceUnwrapped = entryUnwrapped,
        };

        Entries.Insert(insertIndex, newEntry);
        SortEntriesByDistanceDesc();

        // Set vị trí ban đầu trên spline
        float currentWrapped = NormalizeDistance(entryUnwrapped, splineLength);
        double percent = _conveyor.Travel(0.0, currentWrapped, out _, Spline.Direction.Forward);
        cube.Positioner.SetPercent(percent);
    }

    void Update()
    {
        if (Entries.Count == 0 || _conveyor == null) return;

        float splineLength = _conveyor.CalculateLength();
        if (splineLength <= 0f) return;

        float forwardDelta = _speed * Time.deltaTime;

        // Dọn null
        for (int i = Entries.Count - 1; i >= 0; i--)
        {
            if (Entries[i].Cube == null || Entries[i].Cube.Positioner == null)
                Entries.RemoveAt(i);
        }
        if (Entries.Count == 0) return;

        // Di chuyển tất cả cube về phía trước
        for (int i = 0; i < Entries.Count; i++)
        {
            Entries[i].DistanceUnwrapped += forwardDelta;
        }

        SortEntriesByDistanceDesc();

        // Build lookup
        Dictionary<CubeLine, CubeEntry> entryByCube = new Dictionary<CubeLine, CubeEntry>(Entries.Count);
        for (int i = 0; i < Entries.Count; i++)
        {
            CubeEntry e = Entries[i];
            if (e.Cube != null) entryByCube[e.Cube] = e;
        }

        // Apply to spline
        for (int i = 0; i < Entries.Count; i++)
        {
            CubeEntry entry = Entries[i];
            if (entry.Cube == null || entry.Cube.Positioner == null) continue;

            float targetWrapped = NormalizeDistance(entry.DistanceUnwrapped, splineLength);
            double percent = _conveyor.Travel(0.0, targetWrapped, out _, Spline.Direction.Forward);
            entry.Cube.Positioner.SetPercent(percent);
        }
    }
}