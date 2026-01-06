using System.Collections.Generic;
using Dreamteck.Splines;
using Sirenix.OdinInspector;
using UnityEngine;

public class ConveyorController : Singleton<ConveyorController>
{
    [SerializeField] private SplineComputer _conveyor;
    [SerializeField] private float _speed = 0.15f;

    private class CubeEntry
    {
        public CubeLine Cube;
        public float DistanceUnwrapped;
        public bool IsHandingOff; // Đang trong quá trình handoff
        public float HandoffTimeRemaining;
    }

    private readonly List<CubeEntry> _entries = new();
    private float _headDistanceUnwrapped;
    private bool _hasHead;

    private readonly Dictionary<Line, CubeLine> _pendingReconnect = new();

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

    private float GetDesiredOffsetForEntry(int entryIndex)
    {
        if (entryIndex <= 0) return 0f;
        CubeLine cube = _entries[entryIndex].Cube;
        if (cube == null) return 0f;
        if (cube.isEngine) return 0f;
        return cube.offset;
    }

    private int FindEntryIndex(CubeLine cube)
    {
        if (cube == null) return -1;
        for (int i = 0; i < _entries.Count; i++)
        {
            if (_entries[i].Cube == cube) return i;
        }
        return -1;
    }

    private int FindInsertIndexByDistance(float distanceUnwrapped)
    {
        int insertIndex = 0;
        while (insertIndex < _entries.Count && _entries[insertIndex].DistanceUnwrapped > distanceUnwrapped)
            insertIndex++;
        return insertIndex;
    }

    private int FindFrontEntryIndexByUnwrapped(float entryUnwrapped)
    {
        int bestIndex = -1;
        float bestGap = float.PositiveInfinity;
        for (int i = 0; i < _entries.Count; i++)
        {
            float gap = _entries[i].DistanceUnwrapped - entryUnwrapped;
            if (gap < 0f) continue; // behind entry point
            if (gap < bestGap)
            {
                bestGap = gap;
                bestIndex = i;
            }
        }
        return bestIndex;
    }

    private void SortEntriesByDistanceDesc()
    {
        _entries.Sort((a, b) => b.DistanceUnwrapped.CompareTo(a.DistanceUnwrapped));
    }

    private float GetMaxDistanceUnwrapped()
    {
        float max = float.NegativeInfinity;
        for (int i = 0; i < _entries.Count; i++)
        {
            if (_entries[i].DistanceUnwrapped > max) max = _entries[i].DistanceUnwrapped;
        }
        return max;
    }

    private static CubeLine GetHeadCube(CubeLine cube)
    {
        CubeLine current = cube;
        while (current != null && current.front != null) current = current.front;
        return current;
    }

    private static CubeLine GetTailCube(CubeLine head)
    {
        CubeLine current = head;
        while (current != null && current.Back != null) current = current.Back;
        return current;
    }

    private bool TryAutoSpliceTrains(Dictionary<CubeLine, CubeEntry> entryByCube)
    {
        // Find any head that is about to touch a cube in a different train.
        // When it does, splice the whole train right after the touched cube.
        for (int i = 0; i < _entries.Count; i++)
        {
            CubeLine head2 = _entries[i].Cube;
            if (head2 == null) continue;
            if (head2.front != null) continue;
            if (!entryByCube.TryGetValue(head2, out CubeEntry head2Entry)) continue;

            CubeLine bestContact = null;
            float bestAbsGap = float.PositiveInfinity;
            float bestSignedGap = 0f;

            for (int j = 0; j < _entries.Count; j++)
            {
                CubeLine candidate = _entries[j].Cube;
                if (candidate == null) continue;
                if (candidate == head2) continue;
                if (GetHeadCube(candidate) == head2) continue; // same train

                if (!entryByCube.TryGetValue(candidate, out CubeEntry candEntry)) continue;
                float gap = candEntry.DistanceUnwrapped - head2Entry.DistanceUnwrapped;
                float absGap = Mathf.Abs(gap);

                if (absGap < bestAbsGap)
                {
                    bestAbsGap = absGap;
                    bestSignedGap = gap;
                    bestContact = candidate;
                }
            }

            if (bestContact == null) continue;
            if (!entryByCube.TryGetValue(bestContact, out CubeEntry bestContactEntry)) continue;

            float threshold = head2.offset;
            if (threshold < 0f) threshold = 0f;

            // Only splice when the head is close enough to the contact cube.
            if (bestAbsGap > threshold) continue;

            // Splice: contact(B) + train2(head2..tail2) + remainder(A)
            CubeLine remainder = bestContact.Back;

            // If contact already directly links to this head, or train2 is already connected, skip.
            if (bestContact.Back == head2 && head2.front == bestContact) return false;
            if (head2.front != null) continue;

            // Cut remainder from contact
            bestContact.Back = null;
            if (remainder != null) remainder.front = null;

            // Insert train2 after contact
            bestContact.Back = head2;
            head2.front = bestContact;

            // Reconnect tail2 to remainder
            CubeLine tail2 = GetTailCube(head2);
            tail2.Back = remainder;
            if (remainder != null) remainder.front = tail2;

            return true;
        }

        return false;
    }

    private int FindNearestEntryIndexByWrapped(float entryWrapped, float splineLength)
    {
        if (_entries.Count == 0 || splineLength <= 0f) return -1;
        int bestIndex = 0;
        float best = float.PositiveInfinity;
        for (int i = 0; i < _entries.Count; i++)
        {
            float wrapped = NormalizeDistance(_entries[i].DistanceUnwrapped, splineLength);
            float diff = Mathf.Abs(wrapped - entryWrapped);
            diff = Mathf.Min(diff, splineLength - diff);
            if (diff < best)
            {
                best = diff;
                bestIndex = i;
            }
        }
        return bestIndex;
    }

    private static float WrappedDelta(float aWrapped, float bWrapped, float splineLength)
    {
        float diff = Mathf.Abs(aWrapped - bWrapped);
        return splineLength > 0f ? Mathf.Min(diff, splineLength - diff) : diff;
    }

    private void SnapChainToOffsets()
    {
        if (_entries.Count == 0) return;

        Dictionary<CubeLine, CubeEntry> entryByCube = new Dictionary<CubeLine, CubeEntry>(_entries.Count);
        for (int i = 0; i < _entries.Count; i++)
        {
            CubeEntry e = _entries[i];
            if (e.Cube != null) entryByCube[e.Cube] = e;
        }

        for (int i = 0; i < _entries.Count; i++)
        {
            CubeLine headCube = _entries[i].Cube;
            if (headCube == null) continue;
            if (headCube.front != null) continue;

            CubeLine front = headCube;
            CubeLine current = headCube.Back;
            while (current != null)
            {
                if (current.front != front || front.Back != current) break;
                if (!entryByCube.TryGetValue(current, out CubeEntry entry)) break;
                if (!entryByCube.TryGetValue(front, out CubeEntry frontEntry)) break;

                float desiredOffset = current.isEngine ? 0f : current.offset;
                if (desiredOffset >= 0f)
                {
                    entry.DistanceUnwrapped = frontEntry.DistanceUnwrapped - desiredOffset;
                }

                front = current;
                current = current.Back;
            }
        }

        // Auto-splice trains when they touch at an arbitrary cube (e.g. A B C D + E F G touches at B -> A B E F G C D)
        if (TryAutoSpliceTrains(entryByCube))
        {
            SnapChainToOffsets();
            SortEntriesByDistanceDesc();
        }
    }

    private void EnforceSpacingFromIndex(int startIndex)
    {
        for (int i = Mathf.Max(startIndex, 0) + 1; i < _entries.Count; i++)
        {
            float desiredOffset = GetDesiredOffsetForEntry(i);
            float allowed = desiredOffset >= 0f ? _entries[i - 1].DistanceUnwrapped - desiredOffset : float.PositiveInfinity;
            if (_entries[i].DistanceUnwrapped > allowed)
                _entries[i].DistanceUnwrapped = allowed;
        }
    }

public void AddCube(CubeLine cube, Vector3 entryWorldPosition, float trainSpacing, float handoffDuration)
{
    if (cube == null || cube.Positioner == null || _conveyor == null) return;

    cube.Positioner.spline = _conveyor;
    cube.Positioner.motion.applyRotation = false;

    float splineLength = _conveyor.CalculateLength();
    if (splineLength <= 0f) return;

    SplineSample entrySample = _conveyor.Project(entryWorldPosition);
    float entryWrapped = NormalizeDistance(_conveyor.CalculateLength(0.0, entrySample.percent), splineLength);

    // Tránh add trùng
    for (int i = 0; i < _entries.Count; i++)
    {
        if (_entries[i].Cube == cube) return;
    }

    // Lần đầu tiên có cube
    if (!_hasHead)
    {
        _hasHead = true;
        _headDistanceUnwrapped = entryWrapped;
        _entries.Clear();
    }

    // Keep entries sorted so selection/insert is stable
    if (_entries.Count > 1) SortEntriesByDistanceDesc();

    // Determine desired unwrapped distance for this cube based on its front and offset.
    float reference = _entries.Count > 0 ? GetMaxDistanceUnwrapped() : 0f;
    float entryUnwrapped = _entries.Count > 0 ? ClosestUnwrappedToReference(entryWrapped, reference, splineLength) : entryWrapped;

    bool doSplice = false;
    int frontIndexForSplice = -1;
    CubeLine frontCubeForSplice = null;
    CubeLine reconnectA = null;

    if (cube.isEngine && cube.Line != null && !_pendingReconnect.ContainsKey(cube.Line) && _entries.Count > 0)
    {
        frontIndexForSplice = FindFrontEntryIndexByUnwrapped(entryUnwrapped);
        frontCubeForSplice = frontIndexForSplice >= 0 ? _entries[frontIndexForSplice].Cube : null;
        reconnectA = frontCubeForSplice != null ? frontCubeForSplice.Back : null;

        if (frontIndexForSplice >= 0 && frontCubeForSplice != null)
        {
            float gapAhead = _entries[frontIndexForSplice].DistanceUnwrapped - entryUnwrapped;
            float threshold = cube.offset;
            if (threshold < 0f) threshold = 0f;
            doSplice = gapAhead <= threshold;
        }
    }

    if (doSplice && frontCubeForSplice != null)
    {
        // Cut
        frontCubeForSplice.Back = null;
        if (reconnectA != null) reconnectA.front = null;

        // Splice start: nearest -> cube
        cube.front = frontCubeForSplice;
        frontCubeForSplice.Back = cube;

        // This cube now has a front, so it must have a meaningful offset
        if (cube.offset < 0f) cube.offset = 0f;

        _pendingReconnect[cube.Line] = reconnectA;

        float frontDist = _entries[frontIndexForSplice].DistanceUnwrapped;
        entryUnwrapped = frontDist - cube.offset;
    }
    else if (cube.front != null)
    {
        int frontIndex = FindEntryIndex(cube.front);
        if (frontIndex >= 0)
        {
            float frontDist = _entries[frontIndex].DistanceUnwrapped;
            entryUnwrapped = frontDist - cube.offset;
        }
        else
        {
            float referenceUnwrapped = _entries.Count > 0 ? _entries[^1].DistanceUnwrapped : _headDistanceUnwrapped;
            entryUnwrapped = ClosestUnwrappedToReference(entryWrapped, referenceUnwrapped, splineLength);
        }
    }
    // else: independent head -> keep entryUnwrapped = entryWrapped

    // ============ COLLISION CHECK ============
    // Tìm tất cả cube CÙNG LINE với cube mới
    List<CubeEntry> sameLineEntries = new List<CubeEntry>();
    if (cube.Line != null)
    {
        for (int i = 0; i < _entries.Count; i++)
        {
            if (_entries[i].Cube != null && _entries[i].Cube.Line == cube.Line)
            {
                sameLineEntries.Add(_entries[i]);
            }
        }
    }
    
    float safeDistance = entryUnwrapped;
    
    if (cube.front != null)
    {
        // Cube có front - phải đi sau front với đúng offset
        int frontIndex = FindEntryIndex(cube.front);
        if (frontIndex >= 0)
        {
            float frontDist = _entries[frontIndex].DistanceUnwrapped;
            float desiredOffset = cube.offset > 0f ? cube.offset : 1f;
            safeDistance = frontDist - desiredOffset;
        }
    }
    else if (cube.Line != null && sameLineEntries.Count > 0)
    {
        // Cube là head của line, nhưng line đã có cube khác trên conveyor
        // Tìm cube xa nhất (head hiện tại) của line
        float maxDist = float.NegativeInfinity;
        foreach (var entry in sameLineEntries)
        {
            if (entry.DistanceUnwrapped > maxDist)
                maxDist = entry.DistanceUnwrapped;
        }
        
        // Head mới phải đi trước tất cả cube cùng line
        // Nhưng cũng phải tránh đụng các line khác
        float minGapToOtherLines = 0.4f;
        safeDistance = Mathf.Max(entryUnwrapped, maxDist + 0.1f);
        
        // Check collision với cube của line KHÁC
        for (int i = 0; i < _entries.Count; i++)
        {
            if (_entries[i].Cube == null) continue;
            if (_entries[i].Cube.Line == cube.Line) continue; // Skip cùng line
            
            float existingDist = _entries[i].DistanceUnwrapped;
            float gap = Mathf.Abs(existingDist - safeDistance);
            
            if (gap < minGapToOtherLines)
            {
                // Đẩy ra xa
                if (existingDist > safeDistance)
                    safeDistance = existingDist - minGapToOtherLines;
                else
                    safeDistance = existingDist + minGapToOtherLines;
            }
        }
    }
    else
    {
        // Line hoàn toàn mới - chỉ cần tránh đụng line khác
        float minGapToOtherLines = 0.4f;
        
        for (int i = 0; i < _entries.Count; i++)
        {
            if (_entries[i].Cube == null) continue;
            
            float existingDist = _entries[i].DistanceUnwrapped;
            float gap = Mathf.Abs(existingDist - entryUnwrapped);
            
            if (gap < minGapToOtherLines)
            {
                if (existingDist > entryUnwrapped)
                    safeDistance = Mathf.Min(safeDistance, existingDist - minGapToOtherLines);
                else
                    safeDistance = Mathf.Max(safeDistance, existingDist + minGapToOtherLines);
            }
        }
    }
    
    entryUnwrapped = safeDistance;
    // ============ END COLLISION CHECK ============

    int insertIndex = FindInsertIndexByDistance(entryUnwrapped);

    CubeEntry newEntry = new CubeEntry
    {
        Cube = cube,
        DistanceUnwrapped = entryUnwrapped,
        IsHandingOff = handoffDuration > 0f,
        HandoffTimeRemaining = handoffDuration
    };

    // Respect offset vs cube in front (if any)
    if (insertIndex > 0)
    {
        float maxAllowed = _entries[insertIndex - 1].DistanceUnwrapped - cube.offset;
        if (newEntry.DistanceUnwrapped > maxAllowed)
            newEntry.DistanceUnwrapped = maxAllowed;
    }

    // Không vượt head
    // no global head clamp (multi-train)

    _entries.Insert(insertIndex, newEntry);

    SortEntriesByDistanceDesc();

    // If this is the tail of a spliced line, reconnect to the detached part (A).
    if (cube.Line != null && cube.Back == null && _pendingReconnect.TryGetValue(cube.Line, out CubeLine reconnectB))
    {
        cube.Back = reconnectB;
        if (reconnectB != null) reconnectB.front = cube;
        _pendingReconnect.Remove(cube.Line);
    }

    SnapChainToOffsets();

    // KHÔNG snap ngay - để cube giữ nguyên vị trí hiện tại
    // Positioner sẽ được update trong frame tiếp theo qua Update()
    float currentWrapped = NormalizeDistance(entryUnwrapped, splineLength);
    double percent = _conveyor.Travel(0.0, currentWrapped, out _, Spline.Direction.Forward);
    cube.Positioner.SetPercent(percent);
}

    void Update()
    {
        if (_entries.Count == 0 || _conveyor == null) return;

        float splineLength = _conveyor.CalculateLength();
        if (splineLength <= 0f) return;

        float forwardDelta = _speed * Time.deltaTime;

        // Dọn null
        for (int i = _entries.Count - 1; i >= 0; i--)
        {
            if (_entries[i].Cube == null || _entries[i].Cube.Positioner == null) 
                _entries.RemoveAt(i);
        }
        if (_entries.Count == 0) return;


        // Chỉ di chuyển khi chuỗi kết nối đúng
        bool stopMoving = false;
        for (int i = 0; i < _entries.Count; i++)
        {
            var cube = _entries[i].Cube;
            if (stopMoving)
                continue;
            // Nếu cube bị mất kết nối (front/back không đúng), dừng di chuyển các cube sau
            if (cube != null)
            {
                if ((cube.front != null && cube.front.Back != cube) || (cube.Back != null && cube.Back.front != cube))
                {
                    stopMoving = true;
                    continue;
                }
            }
            _entries[i].DistanceUnwrapped += forwardDelta;
        }

        SortEntriesByDistanceDesc();

        // Build quick lookup
        Dictionary<CubeLine, CubeEntry> entryByCube = new Dictionary<CubeLine, CubeEntry>(_entries.Count);
        for (int i = 0; i < _entries.Count; i++)
        {
            CubeEntry e = _entries[i];
            if (e.Cube != null) entryByCube[e.Cube] = e;
        }

        // Enforce offsets along each independent chain
        for (int i = 0; i < _entries.Count; i++)
        {
            CubeLine headCube = _entries[i].Cube;
            if (headCube == null) continue;
            if (headCube.front != null) continue;

            CubeLine front = headCube;
            CubeLine current = headCube.Back;
            while (current != null)
            {
                if (current.front != front || front.Back != current) break;
                if (!entryByCube.TryGetValue(current, out CubeEntry entry)) break;
                if (!entryByCube.TryGetValue(front, out CubeEntry frontEntry)) break;

                if (entry.IsHandingOff)
                {
                    entry.HandoffTimeRemaining -= Time.deltaTime;
                    if (entry.HandoffTimeRemaining <= 0f)
                    {
                        entry.IsHandingOff = false;
                        entry.HandoffTimeRemaining = 0f;
                    }
                }

                float desiredOffset = current.isEngine ? 0f : current.offset;
                float allowed = desiredOffset >= 0f ? frontEntry.DistanceUnwrapped - (entry.IsHandingOff ? desiredOffset * 0.5f : desiredOffset) : float.PositiveInfinity;
                if (entry.DistanceUnwrapped > allowed) entry.DistanceUnwrapped = allowed;

                front = current;
                current = current.Back;
            }
        }

        // Apply to spline
        for (int i = 0; i < _entries.Count; i++)
        {
            CubeEntry entry = _entries[i];
            if (entry.Cube == null || entry.Cube.Positioner == null) continue;

            float targetWrapped = NormalizeDistance(entry.DistanceUnwrapped, splineLength);
            double percent = _conveyor.Travel(0.0, targetWrapped, out _, Spline.Direction.Forward);
            entry.Cube.Positioner.SetPercent(percent);
        }
    }
}