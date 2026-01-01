using System.Collections.Generic;
using UnityEngine;

public class HexPathfinder : Singleton<HexPathfinder>
{
    private readonly List<GridCell> _openSet = new();
    private readonly HashSet<GridCell> _openSetHash = new();
    private readonly HashSet<GridCell> _closedSet = new();
    private readonly Dictionary<GridCell, GridCell> _cameFrom = new();
    private readonly Dictionary<GridCell, int> _gScore = new();
    private readonly Dictionary<GridCell, int> _fScore = new();

    public List<GridCell> FindPath(GridCell start, GridCell target)
    {
        if (start == null || target == null) return null;

        _openSet.Clear();
        _openSetHash.Clear();
        _closedSet.Clear();
        _cameFrom.Clear();
        _gScore.Clear();
        _fScore.Clear();

        _openSet.Add(start);
        _openSetHash.Add(start);

        _gScore[start] = 0;
        _fScore[start] = Heuristic(start, target);

        while (_openSet.Count > 0)
        {
            GridCell current = null;
            int bestF = int.MaxValue;
            int bestIndex = -1;
            for (int i = 0; i < _openSet.Count; i++)
            {
                var c = _openSet[i];
                if (!_fScore.TryGetValue(c, out int fs)) fs = int.MaxValue;
                if (fs < bestF)
                {
                    bestF = fs;
                    current = c;
                    bestIndex = i;
                }
            }

            if (current == null)
            {
                return null;
            }

            if (current == target)
                return RetracePath(_cameFrom, start, target);

            int lastIndex = _openSet.Count - 1;
            _openSet[bestIndex] = _openSet[lastIndex];
            _openSet.RemoveAt(lastIndex);

            _openSetHash.Remove(current);
            _closedSet.Add(current);

            if (current.Neighbors == null || current.Neighbors.Count == 0)
            {
                continue;
            }

            if (!_gScore.TryGetValue(current, out int currentG))
            {
                continue;
            }

            int tentativeG = currentG + 1;

            foreach (var neighbor in current.Neighbors)
            {
                if (neighbor == null) continue;
                if (_closedSet.Contains(neighbor)) continue;
                if (neighbor.IsOccupied && neighbor != target)
                    continue;

                if (!_gScore.TryGetValue(neighbor, out int neighborG) || tentativeG < neighborG)
                {
                    _cameFrom[neighbor] = current;
                    _gScore[neighbor] = tentativeG;
                    _fScore[neighbor] = tentativeG + Heuristic(neighbor, target);
                    if (!_openSetHash.Contains(neighbor))
                    {
                        _openSet.Add(neighbor);
                        _openSetHash.Add(neighbor);
                    }
                }
            }
        }

        return null; 
    }

    private int Heuristic(GridCell a, GridCell b)
    {
        Vector3Int ac = OffsetToCube(a.Position);
        Vector3Int bc = OffsetToCube(b.Position);
        int dx = Mathf.Abs(ac.x - bc.x);
        int dy = Mathf.Abs(ac.y - bc.y);
        int dz = Mathf.Abs(ac.z - bc.z);
        return (dx + dy + dz) / 2;
    }

    private Vector3Int OffsetToCube(Vector2Int offset)
    {
        int col = offset.x;
        int row = offset.y;

        int x = col;
        int z = row - (col - (col & 1)) / 2;
        int y = -x - z;

        return new Vector3Int(x, y, z);
    }

    private List<GridCell> RetracePath(Dictionary<GridCell, GridCell> cameFrom, GridCell start, GridCell end)
    {
        List<GridCell> path = new List<GridCell>();
        GridCell current = end;

        while (current != start)
        {
            path.Add(current);
            if (!cameFrom.TryGetValue(current, out current))
            {
                return null;
            }
        }
        path.Reverse();
        return path;
    }
}
